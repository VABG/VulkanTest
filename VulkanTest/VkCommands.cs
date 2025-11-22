using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace VulkanTest;

public unsafe class VkCommands : IDisposable
{
    private readonly VkInstance _instance;
    public CommandPool CommandPool;
    public CommandBuffer[]? CommandBuffers { get; private set; }

    public VkCommands(VkInstance instance)
    {
        _instance = instance;
        CreateCommandPool(instance);
    }

    private void CreateCommandPool(VkInstance instance)
    {
        var queueFamilyIndices = instance.Device.QueueFamilyIndices;

        CommandPoolCreateInfo poolInfo = new()
        {
            SType = StructureType.CommandPoolCreateInfo,
            QueueFamilyIndex = queueFamilyIndices.GraphicsFamily!.Value,
        };

        if (instance.Vk.CreateCommandPool(instance.Device.Device, in poolInfo, null, out CommandPool) != Result.Success)
        {
            throw new Exception("failed to create command pool!");
        }
    }

    public void CreateCommandBuffers()
    {
        var swapChainFramebuffersLength = _instance.GraphicsPipeline.SwapChainFramebuffers!.Length;

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
            if (_instance.Vk.AllocateCommandBuffers(_instance.Device.Device,
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

            if (_instance.Vk.BeginCommandBuffer(CommandBuffers[i],
                    in beginInfo) != Result.Success)
            {
                throw new Exception("failed to begin recording command buffer!");
            }

            RenderPassBeginInfo renderPassInfo = new()
            {
                SType = StructureType.RenderPassBeginInfo,
                RenderPass = _instance.RenderPass.RenderPass,
                Framebuffer = _instance.GraphicsPipeline.SwapChainFramebuffers[i],
                RenderArea =
                {
                    Offset = { X = 0, Y = 0 },
                    Extent = _instance.SwapChain.SwapChainExtent,
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

                _instance.Vk.CmdBeginRenderPass(CommandBuffers[i], &renderPassInfo, SubpassContents.Inline);
            }

            _instance.Vk.CmdBindPipeline(CommandBuffers[i],
                PipelineBindPoint.Graphics,
                _instance.GraphicsPipeline.Pipeline);


            var vertexBuffers = new[] { _instance.VertexBuffer.VertexBuffer };
            var offsets = new ulong[] { 0 };

            fixed (ulong* offsetsPtr = offsets)
            fixed (Buffer* vertexBuffersPtr = vertexBuffers)
            {
                _instance.Vk.CmdBindVertexBuffers(CommandBuffers[i],
                    0,
                    1,
                    vertexBuffersPtr,
                    offsetsPtr);
            }

            _instance.Vk.CmdBindIndexBuffer(CommandBuffers![i],
                _instance.VertexBuffer.IndexBuffer,
                0,
                IndexType.Uint16);
            
            _instance.Vk.CmdBindDescriptorSets(CommandBuffers[i],
                PipelineBindPoint.Graphics,
                _instance.GraphicsPipeline.PipelineLayout,
                0,
                1,
                in _instance.DescriptorPool.DescriptorSets![i],
                0,
                null);

            _instance.Vk.CmdDrawIndexed(CommandBuffers[i],
                _instance.VertexBuffer.GetIndicesCount(),
                1,
                0,
                0,
                0);


            _instance.Vk.CmdEndRenderPass(CommandBuffers[i]);

            if (_instance.Vk.EndCommandBuffer(CommandBuffers[i]) != Result.Success)
            {
                throw new Exception("failed to record command buffer!");
            }
        }
    }

    public void Dispose()
    {
        _instance.Vk.DestroyCommandPool(_instance.Device.Device, CommandPool, null);
    }
}