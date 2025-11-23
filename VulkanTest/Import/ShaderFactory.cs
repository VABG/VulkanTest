using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using VulkanTest.Shaders;

namespace VulkanTest.Import;

public class ShaderFactory
{
    private Dictionary<string, ShaderData> _loadedShaders = [];

    public ShaderFactory()
    {
        
    }
    
    public void ImportShader(string path, ShaderType shaderType, bool forceReload = false)
    {
        if (!forceReload && _loadedShaders.ContainsKey(path))
            return;
        
        var vertShaderCode = File.ReadAllBytes(path);
        _loadedShaders[path] = new ShaderData(vertShaderCode, shaderType);
    }

    public VkShader GetShader(string path)
    {
        if (!_loadedShaders.ContainsKey(path))
            throw new Exception($"Shader not loaded: {path}");
        var data = _loadedShaders[path];
        
        return new VkShader(CreateShaderModule(data.ByteCode), data.ShaderType);
    }

    public VkShader ImportAndGetShader(string path, ShaderType shaderType, bool forceReload = false)
    {
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