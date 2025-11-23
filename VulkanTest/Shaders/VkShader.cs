using Silk.NET.Core.Native;
using Silk.NET.OpenGL;
using Silk.NET.Vulkan;

namespace VulkanTest.Shaders;

public class VkShader : IDisposable
{
    public readonly ShaderModule ShaderModule;
    private readonly ShaderType _shaderType;
    public PipelineShaderStageCreateInfo PipelineShaderStageCreateInfo => GetCreateInfo();

    public VkShader(ShaderModule shaderModule, ShaderType shaderType)
    {
        ShaderModule = shaderModule;
        _shaderType = shaderType;
    }

    private unsafe PipelineShaderStageCreateInfo GetCreateInfo() =>
        new()
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = GetShaderStageFlags(),
            Module = ShaderModule,
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
        VkUtil.Vk.DestroyShaderModule(VkUtil.Device, ShaderModule, null);
        SilkMarshal.Free((nint)PipelineShaderStageCreateInfo.PName);
    }
}