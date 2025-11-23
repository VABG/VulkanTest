using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace VulkanTest;

public unsafe class VkFrameDrawer : IDisposable
{
    private readonly VkDevice _device;
    private readonly VkRender _render;
    private Semaphore[]? _imageAvailableSemaphores;
    private Semaphore[]? _renderFinishedSemaphores;
    private Fence[]? _inFlightFences;
    private Fence[]? _imagesInFlight;
    private int _currentFrame;
    const int MAX_FRAMES_IN_FLIGHT = 2;
    private bool _frameBufferResized;
    
    public VkFrameDrawer(VkWindow window, VkDevice device, VkRender render)
    {
        _device = device;
        _render = render;
        window.Window.Resize += WindowOnResize;
        CreateSyncObjects();
    }

    private void WindowOnResize(Vector2D<int> obj)
    {
        _frameBufferResized = true;
    }

    private void CreateSyncObjects()
    {
        _imageAvailableSemaphores = new Semaphore[MAX_FRAMES_IN_FLIGHT];
        _renderFinishedSemaphores = new Semaphore[MAX_FRAMES_IN_FLIGHT];
        _inFlightFences = new Fence[MAX_FRAMES_IN_FLIGHT];
        _imagesInFlight = new Fence[ _render.SwapChain.SwapChainImageViews!.Length];

        SemaphoreCreateInfo semaphoreInfo = new()
        {
            SType = StructureType.SemaphoreCreateInfo,
        };

        FenceCreateInfo fenceInfo = new()
        {
            SType = StructureType.FenceCreateInfo,
            Flags = FenceCreateFlags.SignaledBit,
        };


        for (var i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
        {
            if (VkUtil.Vk.CreateSemaphore(VkUtil.Device, in semaphoreInfo, null, out _imageAvailableSemaphores[i]) !=
                Result.Success ||
                VkUtil.Vk.CreateSemaphore(VkUtil.Device, in semaphoreInfo, null, out _renderFinishedSemaphores[i]) !=
                Result.Success ||
                VkUtil.Vk.CreateFence(VkUtil.Device, in fenceInfo, null, out _inFlightFences[i]) != Result.Success)
            {
                throw new Exception("failed to create synchronization objects for a frame!");
            }
        }
    }

    public void DrawFrame(double delta, float time)
    {
        VkUtil.Vk.WaitForFences(VkUtil.Device, 1, in _inFlightFences![_currentFrame], true, ulong.MaxValue);

        uint imageIndex = 0;
        var result = _render.SwapChain.AcquireNextImage(ref imageIndex, _imageAvailableSemaphores![_currentFrame]);
        
        if (result == Result.ErrorOutOfDateKhr)
        {
            _render.RecreateSwapChain();
            return;
        }
        else if (result != Result.Success && result != Result.SuboptimalKhr)
        {
            throw new Exception("failed to acquire swap chain image!");
        }
        
        _render.UniformBuffer.UpdateUniformBuffer(imageIndex, time, _render);
        
        if (_imagesInFlight![imageIndex].Handle != default)
        {
            VkUtil.Vk.WaitForFences(VkUtil.Device, 1, in _imagesInFlight[imageIndex], true, ulong.MaxValue);
        }

        _imagesInFlight[imageIndex] = _inFlightFences[_currentFrame];

        SubmitInfo submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
        };

        var waitSemaphores = stackalloc[] { _imageAvailableSemaphores[_currentFrame] };
        var waitStages = stackalloc[] { PipelineStageFlags.ColorAttachmentOutputBit };

        var buffer = _render.Commands.CommandBuffers![imageIndex];

        submitInfo = submitInfo with
        {
            WaitSemaphoreCount = 1,
            PWaitSemaphores = waitSemaphores,
            PWaitDstStageMask = waitStages,

            CommandBufferCount = 1,
            PCommandBuffers = &buffer
        };

        var signalSemaphores = stackalloc[] { _renderFinishedSemaphores![_currentFrame] };
        submitInfo = submitInfo with
        {
            SignalSemaphoreCount = 1,
            PSignalSemaphores = signalSemaphores,
        };

        VkUtil.Vk.ResetFences(VkUtil.Device, 1, in _inFlightFences[_currentFrame]);

        if (VkUtil.Vk.QueueSubmit(_device.GraphicsQueue, 1, in submitInfo, _inFlightFences[_currentFrame]) !=
            Result.Success)
        {
            throw new Exception("failed to submit draw command buffer!");
        }

        var swapChains = stackalloc[] { _render.SwapChain.SwapChain };
        PresentInfoKHR presentInfo = new()
        {
            SType = StructureType.PresentInfoKhr,

            WaitSemaphoreCount = 1,
            PWaitSemaphores = signalSemaphores,

            SwapchainCount = 1,
            PSwapchains = swapChains,

            PImageIndices = &imageIndex
        };

        result = _render.SwapChain.KhrSwapChain!.QueuePresent(_device.PresentQueue, in presentInfo);

        if (result == Result.ErrorOutOfDateKhr || result == Result.SuboptimalKhr || _frameBufferResized)
        {
            _frameBufferResized = false;
            _render.RecreateSwapChain();
        }
        else if (result != Result.Success)
        {
            throw new Exception("failed to present swap chain image!");
        }
        
        _currentFrame = (_currentFrame + 1) % MAX_FRAMES_IN_FLIGHT;
    }

    public void Dispose()
    {
        _currentFrame = 0;
        for (int i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
        {
            VkUtil.Vk.DestroySemaphore(VkUtil.Device, _renderFinishedSemaphores![i], null);
            VkUtil.Vk.DestroySemaphore(VkUtil.Device, _imageAvailableSemaphores![i], null);
            VkUtil.Vk.DestroyFence(VkUtil.Device, _inFlightFences![i], null);
        }
    }
}