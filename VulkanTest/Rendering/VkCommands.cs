using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace VulkanTest.Rendering;

public unsafe class VkCommands : IDisposable
{
    public CommandPool CommandPool;
    public CommandBuffer[]? CommandBuffers { get; private set; }

    public VkCommands(VkDevice device)
    {
        CreateCommandPool(device);
    }

    private void CreateCommandPool(VkDevice device)
    {
        var queueFamilyIndices = device.QueueFamilyIndices;

        CommandPoolCreateInfo poolInfo = new()
        {
            SType = StructureType.CommandPoolCreateInfo,
            QueueFamilyIndex = queueFamilyIndices.GraphicsFamily!.Value,
        };

        if (VkUtil.Vk.CreateCommandPool(VkUtil.Device, in poolInfo, null, out CommandPool) != Result.Success)
        {
            throw new Exception("failed to create command pool!");
        }
    }

    public void CreateCommandBuffers(VkRender render)
    {
        var swapChainFramebuffersLength = render.GraphicsPipeline.SwapChainFramebuffers!.Length;

        CommandBuffers = new CommandBuffer[swapChainFramebuffersLength];

        CommandBufferAllocateInfo allocInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = CommandPool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = (uint)CommandBuffers.Length,
        };

        fixed (CommandBuffer* commandBuffersPtr = CommandBuffers)
        {
            if (VkUtil.Vk.AllocateCommandBuffers(VkUtil.Device,
                    in allocInfo,
                    commandBuffersPtr) !=
                Result.Success)
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

            if (VkUtil.Vk.BeginCommandBuffer(CommandBuffers[i],
                    in beginInfo) != Result.Success)
            {
                throw new Exception("failed to begin recording command buffer!");
            }

            RenderPassBeginInfo renderPassInfo = new()
            {
                SType = StructureType.RenderPassBeginInfo,
                RenderPass = render.RenderPass.RenderPass,
                Framebuffer = render.GraphicsPipeline.SwapChainFramebuffers[i],
                RenderArea =
                {
                    Offset = { X = 0, Y = 0 },
                    Extent = render.SwapChain.SwapChainExtent,
                }
            };

            var clearValues = new ClearValue[]
            {
                new()
                {
                    Color = new ClearColorValue { Float32_0 = 0, Float32_1 = 0, Float32_2 = 0, Float32_3 = 1 },
                },
                new()
                {
                    DepthStencil = new ClearDepthStencilValue { Depth = 1, Stencil = 0 }
                }
            };

            fixed (ClearValue* clearValuesPtr = clearValues)
            {
                renderPassInfo.ClearValueCount = (uint)clearValues.Length;
                renderPassInfo.PClearValues = clearValuesPtr;

                VkUtil.Vk.CmdBeginRenderPass(CommandBuffers[i], &renderPassInfo, SubpassContents.Inline);
            }

            VkUtil.Vk.CmdBindPipeline(CommandBuffers[i],
                PipelineBindPoint.Graphics,
                render.GraphicsPipeline.Pipeline);

            var vertexBuffers = new[] { render.Model.VertexBuffer };
            var offsets = new ulong[] { 0 };

            fixed (ulong* offsetsPtr = offsets)
            fixed (Buffer* vertexBuffersPtr = vertexBuffers)
            {
                VkUtil.Vk.CmdBindVertexBuffers(CommandBuffers[i],
                    0,
                    1,
                    vertexBuffersPtr,
                    offsetsPtr);
            }

            VkUtil.Vk.CmdBindIndexBuffer(CommandBuffers![i],
                render.Model.IndexBuffer,
                0,
                IndexType.Uint32);

            VkUtil.Vk.CmdBindDescriptorSets(CommandBuffers[i],
                PipelineBindPoint.Graphics,
                render.GraphicsPipeline.PipelineLayout,
                0,
                1,
                in render.DescriptorPool.DescriptorSets![i],
                0,
                null);

            VkUtil.Vk.CmdDrawIndexed(CommandBuffers[i],
                render.Model.GetIndicesCount(),
                1,
                0,
                0,
                0);


            VkUtil.Vk.CmdEndRenderPass(CommandBuffers[i]);

            if (VkUtil.Vk.EndCommandBuffer(CommandBuffers[i]) != Result.Success)
            {
                throw new Exception("failed to record command buffer!");
            }
        }
    }

    public void Dispose()
    {
        VkUtil.Vk.DestroyCommandPool(VkUtil.Device, CommandPool, null);
    }
}