using Silk.NET.Vulkan;

namespace VulkanTest.Shaders;

public class ShaderPair(VkShader vertexShader, VkShader fragmentShader)
{
    public readonly VkShader VertexShader = vertexShader;
    public readonly VkShader FragmentShader = fragmentShader;

    public PipelineShaderStageCreateInfo[] ShaderStages => [VertexShader.PipelineShaderStageCreateInfo, FragmentShader.PipelineShaderStageCreateInfo];
    
}