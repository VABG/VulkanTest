using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace VulkanTest;

public unsafe class BufferUtil
{
    private readonly VkInstance _instance;

    public BufferUtil(VkInstance instance)
    {
        _instance = instance;
    }

    public void CreateBuffer(ulong size, BufferUsageFlags usage, MemoryPropertyFlags properties, ref Buffer buffer,
        ref DeviceMemory bufferMemory)
    {
        BufferCreateInfo bufferInfo = new()
        {
            SType = StructureType.BufferCreateInfo,
            Size = size,
            Usage = usage,
            SharingMode = SharingMode.Exclusive,
        };

        fixed (Buffer* bufferPtr = &buffer)
        {
            if (_instance.Vk.CreateBuffer(_instance.Device.Device, in bufferInfo, null, bufferPtr) != Result.Success)
            {
                throw new Exception("failed to create vertex buffer!");
            }
        }

        _instance.Vk.GetBufferMemoryRequirements(_instance.Device.Device, buffer, out var memRequirements);

        MemoryAllocateInfo allocateInfo = new()
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memRequirements.Size,
            MemoryTypeIndex = FindMemoryType(memRequirements.MemoryTypeBits, properties),
        };

        fixed (DeviceMemory* bufferMemoryPtr = &bufferMemory)
        {
            if (_instance.Vk.AllocateMemory(_instance.Device.Device, in allocateInfo, null, bufferMemoryPtr) !=
                Result.Success)
            {
                throw new Exception("failed to allocate vertex buffer memory!");
            }
        }

        _instance.Vk.BindBufferMemory(_instance.Device.Device, buffer, bufferMemory, 0);
    }

    public void CopyBuffer(Buffer srcBuffer, Buffer dstBuffer, ulong size)
    {
        CommandBufferAllocateInfo allocateInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            Level = CommandBufferLevel.Primary,
            CommandPool = _instance.Commands.CommandPool,
            CommandBufferCount = 1,
        };

        _instance.Vk.AllocateCommandBuffers(_instance.Device.Device, in allocateInfo, out CommandBuffer commandBuffer);

        CommandBufferBeginInfo beginInfo = new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit,
        };

        _instance.Vk.BeginCommandBuffer(commandBuffer, in beginInfo);

        BufferCopy copyRegion = new()
        {
            Size = size,
        };

        _instance.Vk.CmdCopyBuffer(commandBuffer, srcBuffer, dstBuffer, 1, in copyRegion);

        _instance.Vk.EndCommandBuffer(commandBuffer);

        SubmitInfo submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &commandBuffer,
        };

        _instance.Vk.QueueSubmit(_instance.Device.GraphicsQueue, 1, in submitInfo, default);
        _instance.Vk.QueueWaitIdle(_instance.Device.GraphicsQueue);

        _instance.Vk.FreeCommandBuffers(_instance.Device.Device, _instance.Commands.CommandPool, 1, in commandBuffer);
    }


    private uint FindMemoryType(uint typeFilter, MemoryPropertyFlags properties)
    {
        _instance.Vk.GetPhysicalDeviceMemoryProperties(_instance.Device.PhysicalDevice, out var memProperties);

        for (int i = 0; i < memProperties.MemoryTypeCount; i++)
            if ((typeFilter & (1 << i)) != 0 && (memProperties.MemoryTypes[i].PropertyFlags & properties) == properties)
                return (uint)i;

        throw new Exception("failed to find suitable memory type!");
    }
}