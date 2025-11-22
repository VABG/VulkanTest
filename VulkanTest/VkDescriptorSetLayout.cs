using Silk.NET.Vulkan;

namespace VulkanTest;

public class VkDescriptorSetLayout : IDisposable
{
    private readonly VkInstance _instance;
    public DescriptorSetLayout DescriptorSetLayout;

    public VkDescriptorSetLayout(VkInstance instance)
    {
        _instance = instance;
        CreateDescriptorSetLayout();
    }

    private unsafe void CreateDescriptorSetLayout()
    {
        DescriptorSetLayoutBinding uboLayoutBinding = new()
        {
            Binding = 0,
            DescriptorCount = 1,
            DescriptorType = DescriptorType.UniformBuffer,
            PImmutableSamplers = null,
            StageFlags = ShaderStageFlags.VertexBit,
        };

        DescriptorSetLayoutCreateInfo layoutInfo = new()
        {
            SType = StructureType.DescriptorSetLayoutCreateInfo,
            BindingCount = 1,
            PBindings = &uboLayoutBinding,
        };

        fixed (DescriptorSetLayout* descriptorSetLayoutPtr = &DescriptorSetLayout)
        {
            if (_instance.Vk.CreateDescriptorSetLayout(_instance.Device.Device,
                    in layoutInfo,
                    null,
                    descriptorSetLayoutPtr) != Result.Success)
            {
                throw new Exception("failed to create descriptor set layout!");
            }
        }
    }

    public unsafe void Dispose()
    {
        _instance.Vk.DestroyDescriptorSetLayout(_instance.Device.Device, DescriptorSetLayout, null);
    }
}