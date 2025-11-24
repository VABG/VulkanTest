using Glslang.NET;

namespace Vulkan.ResourceManagement.Compile;

public static class ShaderCompiler
{
    private static readonly string[] AllowedFileExtensions = [".vert", ".frag", ".ps", ".vs"];
    private static readonly string[] HlslExtensions = [".ps", ".vs"];
    private static readonly string[] GlslExtensions = [".vert", ".frag"];
    
    public static void CompileShadersInDirectory(string location, bool recursive)
    {
        DirectoryInfo directoryInfo = new DirectoryInfo(location);
        CompileShadersInDirectory(directoryInfo, recursive);
    }
    
    public static void CompileShader(string inputFile)
    {
        var fileInfo = new FileInfo(inputFile);
        if (fileInfo.Exists)
            return;
        
        CompileShader(fileInfo);
    }
    
    private static void CompileShadersInDirectory(DirectoryInfo directoryInfo, bool recursive)
    {
        if (!directoryInfo.Exists)
            return;
        
        var files = directoryInfo.GetFiles().Where(fi => AllowedFileExtensions.Contains(fi.Extension.ToLower()));

        foreach (var file in files)
            CompileShader(file);

        if (!recursive) 
            return;
        
        var subDirectories =  directoryInfo.GetDirectories();
        foreach (var subDirectory in subDirectories)
            CompileShadersInDirectory(subDirectory.FullName, recursive);
    }

    private static void CompileShader(FileInfo inputFile)
    {
        var code = ReadCodeFile(inputFile, out var shaderStage, out var sourceType, out var outputFile);
        if (code == null)
            return;

        var input = new CompilationInput()
        {
            language = sourceType,
            stage = shaderStage,
            client = ClientType.Vulkan,
            clientVersion = TargetClientVersion.Vulkan_1_3,
            targetLanguage = TargetLanguage.SPV,
            targetLanguageVersion = TargetLanguageVersion.SPV_1_5,
            code = code,
            sourceEntrypoint = "main",
            defaultVersion = 100,
            defaultProfile = ShaderProfile.None,
            forceDefaultVersionAndProfile = false,
            forwardCompatible = false,
            fileIncluder = IncludeFunction,
            messages = GetMessageType(sourceType),
        };

        Compile(input, outputFile);
    }

    private static MessageType GetMessageType(SourceType sourceType)
    {
        switch (sourceType)
        {
            case SourceType.GLSL:
                return MessageType.Enhanced;
            case SourceType.HLSL:
                return MessageType.Enhanced | MessageType.ReadHlsl | MessageType.HlslLegalization;
            default:
                throw new Exception("Unsupported source type: " + sourceType);
        }
    }

    private static IncludeResult IncludeFunction(string headerName, string includerName, uint depth, bool isSystemFile)
    {
        Console.WriteLine(
            $"Including a {(isSystemFile ? "system" : "local")} file, `{headerName}` from `{includerName}` at depth {depth}.");
        IncludeResult result;

        result.headerData = "// Nothing to see here";
        result.headerName = headerName;

        return result;
    }

    private static string? ReadCodeFile(FileInfo fileInfo, out ShaderStage stage, out SourceType sourceType,
        out string outputFile)
    {
        stage = ShaderStage.Miss;
        sourceType = GetSourceType(fileInfo);
        outputFile = string.Empty;
        
        outputFile = Path.Combine(fileInfo.Directory!.FullName, fileInfo.Name[..^fileInfo.Extension.Length]);
        switch (fileInfo.Extension)
        {
            case ".vert" or ".vs":
                stage = ShaderStage.Vertex;
                outputFile += "_vert.spv";
                break;
            case ".frag" or ".ps":
                stage = ShaderStage.Fragment;
                outputFile += "_frag.spv";
                break;
            default:
                throw new Exception("Unsupported file type: " + fileInfo.Extension);
        }
        

        return FileIsUpToDate(outputFile, fileInfo) ? null : File.ReadAllText(fileInfo.FullName);
    }

    private static SourceType GetSourceType(FileInfo fileInfo)
    {
        if (HlslExtensions.Contains(fileInfo.Extension.ToLower()))
            return SourceType.HLSL;
        if (GlslExtensions.Contains(fileInfo.Extension.ToLower()))
            return SourceType.GLSL;
        return SourceType.None;
    }

    private static bool FileIsUpToDate(string outputFile, FileInfo fileInfo)
    {
        var outputFileInfo = new FileInfo(outputFile);
        if (!outputFileInfo.Exists)
            return false;

        return fileInfo.LastWriteTimeUtc <= outputFileInfo.LastWriteTimeUtc;
    }

    private static void Compile(CompilationInput input, string outputPath)
    {
        Shader shader = new Shader(input);

        shader.SetOptions(GetShaderOptions(input.language));

        if (!shader.Preprocess())
        {
            Console.WriteLine("Shader preprocessing failed");
            Console.WriteLine(shader.GetInfoLog());
            Console.WriteLine(shader.GetDebugLog());
            return;
        }

        if (!shader.Parse())
        {
            Console.WriteLine("Shader parsing failed");
            Console.WriteLine(shader.GetInfoLog());
            Console.WriteLine(shader.GetDebugLog());
            Console.WriteLine(shader.GetPreprocessedCode());
            return;
        }

        using var program = new Glslang.NET.Program();

        program.AddShader(shader);

        if (!program.Link(MessageType.SpvRules | MessageType.VulkanRules | MessageType.ReadHlsl))
        {
            Console.WriteLine("Shader linking failed");
            Console.WriteLine(program.GetInfoLog());
            Console.WriteLine(program.GetDebugLog());
            return;
        }

        program.GenerateSPIRV(out uint[] words, input.stage);
        Console.WriteLine($"Generated {words.Length} bytes of SPIR-V");

        string messages = program.GetSPIRVMessages();

        if (!string.IsNullOrWhiteSpace(messages))
            Console.WriteLine(messages);

        var memStream = new MemoryStream();
        BinaryWriter binaryWriter = new BinaryWriter(memStream);
        foreach (var word in words)
            binaryWriter.Write(word);
        Span<byte> bytes = memStream.ToArray();

        File.WriteAllBytes(outputPath, bytes);
    }

    private static ShaderOptions GetShaderOptions(SourceType sourceType)
    {
        switch (sourceType)
        {
            case SourceType.GLSL:
                return ShaderOptions.AutoMapBindings | ShaderOptions.AutoMapLocations |
                       ShaderOptions.MapUnusedUniforms;
            case SourceType.HLSL:
                return ShaderOptions.AutoMapBindings | ShaderOptions.AutoMapLocations |
                       ShaderOptions.MapUnusedUniforms | ShaderOptions.UseHLSLIOMapper;
            default:
                throw new ArgumentOutOfRangeException(nameof(sourceType), sourceType, null);
        }
    }
}