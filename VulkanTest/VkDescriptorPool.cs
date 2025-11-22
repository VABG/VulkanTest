using System.Runtime.CompilerServices;
using Silk.NET.Vulkan;

namespace VulkanTest;

public unsafe class VkDescriptorPool : IDisposable
{
    private readonly VkInstance _instance;
    private DescriptorPool _descriptorPool;
    public DescriptorSet[]? DescriptorSets { get; private set; }
    
    public VkDescriptorPool(VkInstance instance)
    {
        _instance = instance;
        CreateDescriptorPool();
        CreateDescriptorSets();
    }
    
    private void CreateDescriptorPool()
    {
        var swapChainImagesLength = _instance.SwapChain.SwapChainImages!.Length;

        var poolSizes = new DescriptorPoolSize[]
        {
            new()
            {
                Type = DescriptorType.UniformBuffer,
                DescriptorCount = (uint)swapChainImagesLength,
            },
            new()
            {
                Type = DescriptorType.CombinedImageSampler,
                DescriptorCount = (uint)swapChainImagesLength,
            }
        };

        fixed (DescriptorPoolSize* poolSizesPtr = poolSizes)

        fixed (DescriptorPool* descriptorPoolPtr = &_descriptorPool)
        {
            DescriptorPoolCreateInfo poolInfo = new()
            {
                SType = StructureType.DescriptorPoolCreateInfo,
                PoolSizeCount = 1,
                PPoolSizes = poolSizesPtr,
                MaxSets = (uint)swapChainImagesLength,
            };
            
            if (_instance.Vk.CreateDescriptorPool(_instance.Device.Device, in poolInfo, null, descriptorPoolPtr) != Result.Success)
            {
                throw new Exception("failed to create descriptor pool!");
            }
        }
    }
    
    private void CreateDescriptorSets()
    {
        var swapChainImagesCount = _instance.SwapChain.SwapChainImages!.Length;
        var layouts = new DescriptorSetLayout[swapChainImagesCount];
        var descriptorSetLayout = _instance.GraphicsPipeline.DescriptorSetLayout.DescriptorSetLayout;
        
        Array.Fill(layouts,
            descriptorSetLayout);

        fixed (DescriptorSetLayout* layoutsPtr = layouts)
        {
            DescriptorSetAllocateInfo allocateInfo = new()
            {
                SType = StructureType.DescriptorSetAllocateInfo,
                DescriptorPool = _descriptorPool,
                DescriptorSetCount = (uint)swapChainImagesCount,
                PSetLayouts = layoutsPtr,
            };

            DescriptorSets = new DescriptorSet[swapChainImagesCount];
            fixed (DescriptorSet* descriptorSetsPtr = DescriptorSets)
            {
                if (_instance.Vk.AllocateDescriptorSets(_instance.Device.Device,
                        in allocateInfo,
                        descriptorSetsPtr) != Result.Success)
                {
                    throw new Exception("failed to allocate descriptor sets!");
                }
            }
        }

        for (int i = 0; i < swapChainImagesCount; i++)
        {
            DescriptorBufferInfo bufferInfo = new()
            {
                Buffer = _instance.UniformBuffer.UniformBuffers[i],
                Offset = 0,
                Range = (ulong)Unsafe.SizeOf<UniformBufferObject>(),

            };
            
            DescriptorImageInfo imageInfo = new()
            {
                ImageLayout = ImageLayout.ShaderReadOnlyOptimal,
                ImageView = _instance.ImageView.TextureImageView,
                Sampler = _instance.ImageView.TextureSampler,
            };

            var descriptorWrites = new WriteDescriptorSet[]
            {
                new()
                {
                    SType = StructureType.WriteDescriptorSet,
                    DstSet = DescriptorSets[i],
                    DstBinding = 0,
                    DstArrayElement = 0,
                    DescriptorType = DescriptorType.UniformBuffer,
                    DescriptorCount = 1,
                    PBufferInfo = &bufferInfo,
                },
                new()
                {
                    SType = StructureType.WriteDescriptorSet,
                    DstSet = DescriptorSets[i],
                    DstBinding = 1,
                    DstArrayElement = 0,
                    DescriptorType = DescriptorType.CombinedImageSampler,
                    DescriptorCount = 1,
                    PImageInfo = &imageInfo,
                }
            };

            fixed (WriteDescriptorSet* descriptorWritesPtr = descriptorWrites)
            {
                _instance.Vk.UpdateDescriptorSets(_instance.Device.Device,
                    (uint)descriptorWrites.Length,
                    descriptorWritesPtr,
                    0,
                    null);
            }
        }
    }

    public void Dispose()
    {
        _instance.Vk.DestroyDescriptorPool(_instance.Device.Device, _descriptorPool, null);
    }
}