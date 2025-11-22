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
        
        DescriptorPoolSize poolSize = new()
        {
            Type = DescriptorType.UniformBuffer,
            DescriptorCount = (uint)swapChainImagesLength,
        };


        DescriptorPoolCreateInfo poolInfo = new()
        {
            SType = StructureType.DescriptorPoolCreateInfo,
            PoolSizeCount = 1,
            PPoolSizes = &poolSize,
            MaxSets = (uint)swapChainImagesLength,
        };

        fixed (DescriptorPool* descriptorPoolPtr = &_descriptorPool)
        {
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

            WriteDescriptorSet descriptorWrite = new()
            {
                SType = StructureType.WriteDescriptorSet,
                DstSet = DescriptorSets[i],
                DstBinding = 0,
                DstArrayElement = 0,
                DescriptorType = DescriptorType.UniformBuffer,
                DescriptorCount = 1,
                PBufferInfo = &bufferInfo,
            };

            _instance.Vk.UpdateDescriptorSets(_instance.Device.Device,
                1,
                in descriptorWrite,
                0,
                null);
        }
    }

    public void Dispose()
    {
        
    }
}