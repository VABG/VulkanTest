using Silk.NET.Core.Native;
using Silk.NET.Vulkan;

namespace VulkanTest.Shaders;

public class VkShader : IDisposable
{
    private readonly ShaderModule _shaderModule;
    private readonly ShaderType _shaderType;
    public PipelineShaderStageCreateInfo PipelineShaderStageCreateInfo => GetCreateInfo();

    public VkShader(ShaderModule shaderModule, ShaderType shaderType)
    {
        _shaderModule = shaderModule;
        _shaderType = shaderType;
    }

    private unsafe PipelineShaderStageCreateInfo GetCreateInfo() =>
        new()
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = GetShaderStageFlags(),
            Module = _shaderModule,
            PName = (byte*)SilkMarshal.StringToPtr("main")
        };

    private ShaderStageFlags GetShaderStageFlags()
    {
        return _shaderType switch
        {
            ShaderType.Vertex => ShaderStageFlags.VertexBit,
            ShaderType.Fragment => ShaderStageFlags.FragmentBit,
            _ => throw new ArgumentOutOfRangeException(nameof(_shaderType), _shaderType, null)
        };
    }
    
    public unsafe void Dispose()
    {
        VkUtil.Vk.DestroyShaderModule(VkUtil.Device, _shaderModule, null);
        SilkMarshal.Free((nint)PipelineShaderStageCreateInfo.PName);
    }
}