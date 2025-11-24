using Silk.NET.Vulkan;
using Vulkan.Shaders;

namespace Vulkan.Data;

public class ShaderPair(VkShader vertexShader, VkShader fragmentShader)
{
    public readonly VkShader VertexShader = vertexShader;
    public readonly VkShader FragmentShader = fragmentShader;

    public PipelineShaderStageCreateInfo[] ShaderStages => [VertexShader.PipelineShaderStageCreateInfo, FragmentShader.PipelineShaderStageCreateInfo];
    
}