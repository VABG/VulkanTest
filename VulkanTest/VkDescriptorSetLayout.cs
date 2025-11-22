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
        
        DescriptorSetLayoutBinding samplerLayoutBinding = new()
        {
            Binding = 1,
            DescriptorCount = 1,
            DescriptorType = DescriptorType.CombinedImageSampler,
            PImmutableSamplers = null,
            StageFlags = ShaderStageFlags.FragmentBit,
        };
        
        var bindings = new[] { uboLayoutBinding, samplerLayoutBinding };

        fixed (DescriptorSetLayoutBinding* bindingsPtr = bindings)
        fixed (DescriptorSetLayout* descriptorSetLayoutPtr = &DescriptorSetLayout)
        {
            DescriptorSetLayoutCreateInfo layoutInfo = new()
            {
                SType = StructureType.DescriptorSetLayoutCreateInfo,
                BindingCount = (uint)bindings.Length,
                PBindings = bindingsPtr,
            };
            
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