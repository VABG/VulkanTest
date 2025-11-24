using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Vulkan.Rendering.Utils;

public unsafe class CommandBufferUtil
{
    private readonly MemoryUtil _memoryUtil;
    private readonly VkDevice _device;

    public CommandBufferUtil(MemoryUtil memoryUtil, VkDevice device)
    {
        _memoryUtil = memoryUtil;
        _device = device;
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
            if (VkUtil.Vk.CreateBuffer(VkUtil.Device, in bufferInfo, null, bufferPtr) != Result.Success)
            {
                throw new Exception("failed to create vertex buffer!");
            }
        }

        VkUtil.Vk.GetBufferMemoryRequirements(VkUtil.Device, buffer, out var memRequirements);

        MemoryAllocateInfo allocateInfo = new()
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memRequirements.Size,
            MemoryTypeIndex = _memoryUtil.FindMemoryType(memRequirements.MemoryTypeBits, properties),
        };

        fixed (DeviceMemory* bufferMemoryPtr = &bufferMemory)
        {
            if (VkUtil.Vk.AllocateMemory(VkUtil.Device, in allocateInfo, null, bufferMemoryPtr) !=
                Result.Success)
            {
                throw new Exception("failed to allocate vertex buffer memory!");
            }
        }

        VkUtil.Vk.BindBufferMemory(VkUtil.Device, buffer, bufferMemory, 0);
    }

    public void CopyBuffer(Buffer srcBuffer, Buffer dstBuffer, ulong size, CommandPool commandPool)
    {
        CommandBuffer commandBuffer = BeginSingleTimeCommands(commandPool);

        BufferCopy copyRegion = new() { Size = size, };

        VkUtil.Vk.CmdCopyBuffer(commandBuffer, srcBuffer, dstBuffer, 1, in copyRegion);

        EndSingleTimeCommands(commandBuffer, commandPool);
    }
    
    public CommandBuffer BeginSingleTimeCommands(CommandPool commandPool)
    {
        CommandBufferAllocateInfo allocateInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            Level = CommandBufferLevel.Primary,
            CommandPool = commandPool,
            CommandBufferCount = 1,
        };

        VkUtil.Vk.AllocateCommandBuffers(VkUtil.Device, in allocateInfo, out CommandBuffer commandBuffer);

        CommandBufferBeginInfo beginInfo = new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit,
        };

        VkUtil.Vk.BeginCommandBuffer(commandBuffer, in beginInfo);

        return commandBuffer;
    }

    public void EndSingleTimeCommands(CommandBuffer commandBuffer, CommandPool commandPool)
    {
        VkUtil.Vk.EndCommandBuffer(commandBuffer);

        SubmitInfo submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &commandBuffer,
        };

        VkUtil.Vk.QueueSubmit(_device.GraphicsQueue, 1, in submitInfo, default);
        VkUtil.Vk.QueueWaitIdle(_device.GraphicsQueue);

        VkUtil.Vk.FreeCommandBuffers(VkUtil.Device, commandPool, 1, in commandBuffer);
    }
}