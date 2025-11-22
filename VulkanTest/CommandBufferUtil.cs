using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace VulkanTest;

public unsafe class CommandBufferUtil
{
    private readonly VkInstance _instance;

    public CommandBufferUtil(VkInstance instance)
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
            MemoryTypeIndex = _instance.MemoryUtil.FindMemoryType(memRequirements.MemoryTypeBits, properties),
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
        CommandBuffer commandBuffer = BeginSingleTimeCommands();

        BufferCopy copyRegion = new() { Size = size, };

        _instance.Vk.CmdCopyBuffer(commandBuffer, srcBuffer, dstBuffer, 1, in copyRegion);

        EndSingleTimeCommands(commandBuffer);
    }
    
    public CommandBuffer BeginSingleTimeCommands()
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

        return commandBuffer;
    }

    public void EndSingleTimeCommands(CommandBuffer commandBuffer)
    {
        _instance.Vk.EndCommandBuffer(commandBuffer);

        SubmitInfo submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &commandBuffer,
        };

        _instance.Vk.QueueSubmit(_instance.Device.GraphicsQueue, 1, in submitInfo, default);
        _instance.Vk.QueueWaitIdle(_instance.Device.GraphicsQueue);

        _instance.Vk.FreeCommandBuffers(_instance.Device.Device,_instance.Commands.CommandPool, 1, in commandBuffer);
    }
}