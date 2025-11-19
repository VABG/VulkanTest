using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace VulkanTest;

public struct SwapChainSupportDetails
{
    public SurfaceCapabilitiesKHR Capabilities;
    public SurfaceFormatKHR[] Formats;
    public PresentModeKHR[] PresentModes;
}

public class VkSwapChain : IDisposable
{
    private readonly VkInstance _instance;
    public KhrSwapchain? KhrSwapChain { get; private set; }
    public SwapchainKHR SwapChain { get; private set; }
    public Image[]? SwapChainImages { get; private set; }
    public Format SwapChainImageFormat { get; private set; }
    public Extent2D SwapChainExtent { get; private set; }
    public ImageView[]? SwapChainImageViews { get; private set; }

    public VkSwapChain(VkInstance instance)
    {
        _instance = instance;
        CreateSwapChain(instance);
        CreateImageViews(instance);
    }

    public Result AcquireNextImage(ref uint imageIndex, Semaphore imageSemaphore)
    { 
        return KhrSwapChain!.AcquireNextImage(_instance.Device.Device, SwapChain, ulong.MaxValue, imageSemaphore, default, ref imageIndex);
    }
    

    private unsafe void CreateSwapChain(VkInstance instance)
    {
        var swapChainSupport = instance.Device.QuerySwapChainSupport(instance.Device.PhysicalDevice, instance);
        var surfaceFormat = ChooseSwapSurfaceFormat(swapChainSupport.Formats);
        var presentMode = ChoosePresentMode(swapChainSupport.PresentModes);
        var extent = ChooseSwapExtent(swapChainSupport.Capabilities, instance.Window.Window);

        var imageCount = swapChainSupport.Capabilities.MinImageCount + 1;
        if (swapChainSupport.Capabilities.MaxImageCount > 0 && imageCount > swapChainSupport.Capabilities.MaxImageCount)
        {
            imageCount = swapChainSupport.Capabilities.MaxImageCount;
        }

        SwapchainCreateInfoKHR createInfo = new()
        {
            SType = StructureType.SwapchainCreateInfoKhr,
            Surface = instance.Device.Surface,

            MinImageCount = imageCount,
            ImageFormat = surfaceFormat.Format,
            ImageColorSpace = surfaceFormat.ColorSpace,
            ImageExtent = extent,
            ImageArrayLayers = 1,
            ImageUsage = ImageUsageFlags.ColorAttachmentBit,
        };

        var indices = instance.Device.QueueFamilyIndices;
        var queueFamilyIndices = stackalloc[] { indices.GraphicsFamily!.Value, indices.PresentFamily!.Value };

        if (indices.GraphicsFamily != indices.PresentFamily)
        {
            createInfo = createInfo with
            {
                ImageSharingMode = SharingMode.Concurrent,
                QueueFamilyIndexCount = 2,
                PQueueFamilyIndices = queueFamilyIndices,
            };
        }
        else
        {
            createInfo.ImageSharingMode = SharingMode.Exclusive;
        }

        createInfo = createInfo with
        {
            PreTransform = swapChainSupport.Capabilities.CurrentTransform,
            CompositeAlpha = CompositeAlphaFlagsKHR.OpaqueBitKhr,
            PresentMode = presentMode,
            Clipped = true,

            OldSwapchain = default
        };

        if (!instance.Vk.TryGetDeviceExtension(instance.Instance, instance.Device.Device, out KhrSwapchain khrSwapChain))
        {
            throw new NotSupportedException("VK_KHR_swapchain extension not found.");
        }

        KhrSwapChain = khrSwapChain;
        

        if (KhrSwapChain!.CreateSwapchain(instance.Device.Device, in createInfo, null, out var swapChain) !=
            Result.Success)
        {
            throw new Exception("failed to create swap chain!");
        }

        SwapChain = swapChain;

        KhrSwapChain.GetSwapchainImages(instance.Device.Device, SwapChain, ref imageCount, null);
        SwapChainImages = new Image[imageCount];
        fixed (Image* swapChainImagesPtr = SwapChainImages)
        {
            KhrSwapChain.GetSwapchainImages(instance.Device.Device, SwapChain, ref imageCount, swapChainImagesPtr);
        }

        SwapChainImageFormat = surfaceFormat.Format;
        SwapChainExtent = extent;
    }

    private SurfaceFormatKHR ChooseSwapSurfaceFormat(IReadOnlyList<SurfaceFormatKHR> availableFormats)
    {
        foreach (var availableFormat in availableFormats)
        {
            if (availableFormat is { Format: Format.B8G8R8A8Srgb, ColorSpace: ColorSpaceKHR.SpaceSrgbNonlinearKhr })
            {
                return availableFormat;
            }
        }

        return availableFormats[0];
    }

    private PresentModeKHR ChoosePresentMode(IReadOnlyList<PresentModeKHR> availablePresentModes)
    {
        foreach (var availablePresentMode in availablePresentModes)
        {
            if (availablePresentMode == PresentModeKHR.MailboxKhr)
            {
                return availablePresentMode;
            }
        }

        return PresentModeKHR.FifoKhr;
    }

    private Extent2D ChooseSwapExtent(SurfaceCapabilitiesKHR capabilities, IWindow window)
    {
        if (capabilities.CurrentExtent.Width != uint.MaxValue)
            return capabilities.CurrentExtent;

        var framebufferSize = window.FramebufferSize;

        Extent2D actualExtent = new()
        {
            Width = (uint)framebufferSize.X,
            Height = (uint)framebufferSize.Y
        };

        actualExtent.Width = Math.Clamp(actualExtent.Width, capabilities.MinImageExtent.Width,
            capabilities.MaxImageExtent.Width);
        actualExtent.Height = Math.Clamp(actualExtent.Height, capabilities.MinImageExtent.Height,
            capabilities.MaxImageExtent.Height);

        return actualExtent;
    }
    
    private unsafe void CreateImageViews(VkInstance instance)
    {
        SwapChainImageViews = new ImageView[SwapChainImages!.Length];

        for (int i = 0; i < SwapChainImages.Length; i++)
        {
            ImageViewCreateInfo createInfo = new()
            {
                SType = StructureType.ImageViewCreateInfo,
                Image = SwapChainImages[i],
                ViewType = ImageViewType.Type2D,
                Format = SwapChainImageFormat,
                Components =
                {
                    R = ComponentSwizzle.Identity,
                    G = ComponentSwizzle.Identity,
                    B = ComponentSwizzle.Identity,
                    A = ComponentSwizzle.Identity,
                },
                SubresourceRange =
                {
                    AspectMask = ImageAspectFlags.ColorBit,
                    BaseMipLevel = 0,
                    LevelCount = 1,
                    BaseArrayLayer = 0,
                    LayerCount = 1,
                }

            };

            if (instance.Vk.CreateImageView(instance.Device.Device, in createInfo, null, out SwapChainImageViews[i]) != Result.Success)
            {
                throw new Exception("Failed to create image views!");
            }
        }
    }

    public unsafe void Dispose()
    {
        foreach (var imageView in SwapChainImageViews!)
        {
            _instance.Vk.DestroyImageView(_instance.Device.Device, imageView, null);
        }
        KhrSwapChain!.DestroySwapchain(_instance.Device.Device, SwapChain, null);
    }
}