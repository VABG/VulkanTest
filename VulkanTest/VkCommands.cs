using Silk.NET.Vulkan;

namespace VulkanTest;

public unsafe class VkCommands : IDisposable
{
    private readonly VkInstance _instance;
    private CommandPool _commandPool;
    public CommandBuffer[]? CommandBuffers { get; private set; }

    public VkCommands(VkInstance instance)
    {
        _instance = instance;
        CreateCommandPool(instance);
        CreateCommandBuffers(instance);
    }
    
    private void CreateCommandPool(VkInstance instance)
    {
        var queueFamilyIndices = instance.Device.QueueFamilyIndices;

        CommandPoolCreateInfo poolInfo = new()
        {
            SType = StructureType.CommandPoolCreateInfo,
            QueueFamilyIndex = queueFamilyIndices.GraphicsFamily!.Value,
        };

        if (instance.Vk.CreateCommandPool(instance.Device.Device, in poolInfo, null, out _commandPool) != Result.Success)
        {
            throw new Exception("failed to create command pool!");
        }
    }
    
     private void CreateCommandBuffers(VkInstance instance)
     {
         var swapChainFramebuffersLength = instance.GraphicsPipeline.SwapChainFramebuffers!.Length;
        
        CommandBuffers = new CommandBuffer[swapChainFramebuffersLength];

        CommandBufferAllocateInfo allocInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = _commandPool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = (uint)CommandBuffers.Length,
        };

        fixed (CommandBuffer* commandBuffersPtr = CommandBuffers)
        {
            if (instance.Vk.AllocateCommandBuffers(instance.Device.Device, in allocInfo, commandBuffersPtr) != Result.Success)
            {
                throw new Exception("failed to allocate command buffers!");
            }
        }

        for (int i = 0; i < CommandBuffers.Length; i++)
        {
            CommandBufferBeginInfo beginInfo = new()
            {
                SType = StructureType.CommandBufferBeginInfo,
            };

            if (instance.Vk.BeginCommandBuffer(CommandBuffers[i], in beginInfo) != Result.Success)
            {
                throw new Exception("failed to begin recording command buffer!");
            }
            
            RenderPassBeginInfo renderPassInfo = new()
            {
                SType = StructureType.RenderPassBeginInfo,
                RenderPass = instance.GraphicsPipeline.RenderPass,
                Framebuffer = instance.GraphicsPipeline.SwapChainFramebuffers[i],
                RenderArea =
                {
                    Offset = { X = 0, Y = 0 },
                    Extent = instance.SwapChain.SwapChainExtent,
                }
            };

            ClearValue clearColor = new()
            {
                Color = new() { Float32_0 = 0, Float32_1 = 0, Float32_2 = 0, Float32_3 = 1 },
            };

            renderPassInfo.ClearValueCount = 1;
            renderPassInfo.PClearValues = &clearColor;

            instance.Vk.CmdBeginRenderPass(CommandBuffers[i], &renderPassInfo, SubpassContents.Inline);

            instance.Vk.CmdBindPipeline(CommandBuffers[i], PipelineBindPoint.Graphics, instance.GraphicsPipeline.Pipeline);

            instance.Vk.CmdDraw(CommandBuffers[i], 3, 1, 0, 0);

            instance.Vk.CmdEndRenderPass(CommandBuffers[i]);

            if (instance.Vk.EndCommandBuffer(CommandBuffers[i]) != Result.Success)
            {
                throw new Exception("failed to record command buffer!");
            }

        }
    }

     public void Dispose()
     {
         _instance.Vk.DestroyCommandPool(_instance.Device.Device, _commandPool, null);
     }
}