using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace VulkanTest;

public unsafe class VkFrameDrawer : IDisposable
{
    private readonly VkInstance _instance;
    private Semaphore[]? _imageAvailableSemaphores;
    private Semaphore[]? _renderFinishedSemaphores;
    private Fence[]? _inFlightFences;
    private Fence[]? _imagesInFlight;
    private int _currentFrame = 0;
    const int MAX_FRAMES_IN_FLIGHT = 2;
    private bool _frameBufferResized = false;
    
    public VkFrameDrawer(VkInstance instance)
    {
        _instance = instance;
        _instance.Window.Window.Resize += WindowOnResize;
        CreateSyncObjects(instance);
    }

    private void WindowOnResize(Vector2D<int> obj)
    {
        _frameBufferResized = true;
    }

    private void CreateSyncObjects(VkInstance instance)
    {
        _imageAvailableSemaphores = new Semaphore[MAX_FRAMES_IN_FLIGHT];
        _renderFinishedSemaphores = new Semaphore[MAX_FRAMES_IN_FLIGHT];
        _inFlightFences = new Fence[MAX_FRAMES_IN_FLIGHT];
        _imagesInFlight = new Fence[ instance.SwapChain.SwapChainImageViews!.Length];

        SemaphoreCreateInfo semaphoreInfo = new()
        {
            SType = StructureType.SemaphoreCreateInfo,
        };

        FenceCreateInfo fenceInfo = new()
        {
            SType = StructureType.FenceCreateInfo,
            Flags = FenceCreateFlags.SignaledBit,
        };

        var device = instance.Device.Device;

        for (var i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
        {
            if (instance.Vk.CreateSemaphore(device, in semaphoreInfo, null, out _imageAvailableSemaphores[i]) !=
                Result.Success ||
                instance.Vk.CreateSemaphore(device, in semaphoreInfo, null, out _renderFinishedSemaphores[i]) !=
                Result.Success ||
                instance.Vk.CreateFence(device, in fenceInfo, null, out _inFlightFences[i]) != Result.Success)
            {
                throw new Exception("failed to create synchronization objects for a frame!");
            }
        }
    }

    public void DrawFrame(double delta, VkInstance instance)
    {
        instance.Vk.WaitForFences(instance.Device.Device, 1, in _inFlightFences![_currentFrame], true, ulong.MaxValue);

        uint imageIndex = 0;
        var result = instance.SwapChain.AcquireNextImage(ref imageIndex, _imageAvailableSemaphores![_currentFrame]);
        
        if (result == Result.ErrorOutOfDateKhr)
        {
            _instance.RecreateSwapChain();
            return;
        }
        else if (result != Result.Success && result != Result.SuboptimalKhr)
        {
            throw new Exception("failed to acquire swap chain image!");
        }
        
        _instance.UniformBuffer.UpdateUniformBuffer(imageIndex);
        
        if (_imagesInFlight![imageIndex].Handle != default)
        {
            instance.Vk.WaitForFences(instance.Device.Device, 1, in _imagesInFlight[imageIndex], true, ulong.MaxValue);
        }

        _imagesInFlight[imageIndex] = _inFlightFences[_currentFrame];

        SubmitInfo submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
        };

        var waitSemaphores = stackalloc[] { _imageAvailableSemaphores[_currentFrame] };
        var waitStages = stackalloc[] { PipelineStageFlags.ColorAttachmentOutputBit };

        var buffer = instance.Commands.CommandBuffers![imageIndex];

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

        instance.Vk.ResetFences(instance.Device.Device, 1, in _inFlightFences[_currentFrame]);

        if (instance.Vk.QueueSubmit(instance.Device.GraphicsQueue, 1, in submitInfo, _inFlightFences[_currentFrame]) !=
            Result.Success)
        {
            throw new Exception("failed to submit draw command buffer!");
        }

        var swapChains = stackalloc[] { instance.SwapChain.SwapChain };
        PresentInfoKHR presentInfo = new()
        {
            SType = StructureType.PresentInfoKhr,

            WaitSemaphoreCount = 1,
            PWaitSemaphores = signalSemaphores,

            SwapchainCount = 1,
            PSwapchains = swapChains,

            PImageIndices = &imageIndex
        };

        result = instance.SwapChain.KhrSwapChain!.QueuePresent(instance.Device.PresentQueue, in presentInfo);

        if (result == Result.ErrorOutOfDateKhr || result == Result.SuboptimalKhr || _frameBufferResized)
        {
            _frameBufferResized = false;
            _instance.RecreateSwapChain();
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
            _instance.Vk.DestroySemaphore(_instance.Device.Device, _renderFinishedSemaphores![i], null);
            _instance.Vk.DestroySemaphore(_instance.Device.Device, _imageAvailableSemaphores![i], null);
            _instance.Vk.DestroyFence(_instance.Device.Device, _inFlightFences![i], null);
        }
    }
}