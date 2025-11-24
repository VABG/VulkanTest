using Silk.NET.Vulkan;
using Vulkan.Data;
using Vulkan.Shaders;

namespace Vulkan.ResourceManagement.Import;

public class Shaders
{
    private readonly Dictionary<string, ShaderData> _loadedShaders = [];
    
    public void ImportShader(string path, ShaderType shaderType, bool forceReload = false)
    {
        if (!forceReload && _loadedShaders.ContainsKey(path))
            return;
        
        var vertShaderCode = File.ReadAllBytes(path);
        _loadedShaders[path] = new ShaderData(vertShaderCode, shaderType);
    }

    private VkShader GetShader(string path)
    {
        if (!_loadedShaders.ContainsKey(path))
            throw new Exception($"Shader not loaded: {path}");
        var data = _loadedShaders[path];
        
        return new VkShader(CreateShaderModule(data.ByteCode), data.ShaderType);
    }

    public VkShader GetShader(string path, ShaderType shaderType, bool forceReload = false)
    {
        // TODO: automatically look for compiled by path (although probably need different location for compiled shaders)
        // Technically, could load all into memory as well (essentially, check open "scenes" (future way to handle collections of graphics)
        // and grab what's necessary, also check what's getting referenced (dynamically loaded) somehow?
        // TODO: If not already compiled, or out of  compared to source, compile
        ImportShader(path, shaderType, forceReload);
        return GetShader(path);
    }
    
    private unsafe ShaderModule CreateShaderModule(byte[] code)
    {
        ShaderModuleCreateInfo createInfo = new()
        {
            SType = StructureType.ShaderModuleCreateInfo,
            CodeSize = (nuint)code.Length,
        };

        ShaderModule shaderModule;

        fixed (byte* codePtr = code)
        {
            createInfo.PCode = (uint*)codePtr;

            if (VkUtil.Vk.CreateShaderModule(VkUtil.Device, in createInfo, null, out shaderModule) !=
                Result.Success)
            {
                throw new Exception();
            }
        }

        return shaderModule;
    }
}