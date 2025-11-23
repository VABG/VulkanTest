using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace VulkanTest.Rendering;

public struct SwapChainSupportDetails
{
    public SurfaceCapabilitiesKHR Capabilities;
    public SurfaceFormatKHR[] Formats;
    public PresentModeKHR[] PresentModes;
}

public class VkSwapChain : IDisposable
{
    public KhrSwapchain? KhrSwapChain { get; private set; }
    public SwapchainKHR SwapChain { get; private set; }
    public Image[]? SwapChainImages { get; private set; }
    public Format SwapChainImageFormat { get; private set; }
    public Extent2D SwapChainExtent { get; private set; }
    public ImageView[]? SwapChainImageViews { get; private set; }

    public VkSwapChain(VkDevice device, VkWindow window)
    {
        CreateSwapChain(device, window);
        CreateImageViews();
    }

    public Result AcquireNextImage(ref uint imageIndex, Semaphore imageSemaphore)
    { 
        return KhrSwapChain!.AcquireNextImage(VkUtil.Device, SwapChain, ulong.MaxValue, imageSemaphore, default, ref imageIndex);
    }

    private unsafe void CreateSwapChain(VkDevice device, VkWindow window)
    {
        var swapChainSupport = device.QuerySwapChainSupport(VkUtil.PhysicalDevice);
        var surfaceFormat = ChooseSwapSurfaceFormat(swapChainSupport.Formats);
        var presentMode = ChoosePresentMode(swapChainSupport.PresentModes);
        var extent = ChooseSwapExtent(swapChainSupport.Capabilities, window.Window);

        var imageCount = swapChainSupport.Capabilities.MinImageCount;
        if (swapChainSupport.Capabilities.MaxImageCount > 0 && imageCount > swapChainSupport.Capabilities.MaxImageCount)
        {
            imageCount = swapChainSupport.Capabilities.MaxImageCount;
        }

        SwapchainCreateInfoKHR createInfo = new()
        {
            SType = StructureType.SwapchainCreateInfoKhr,
            Surface = device.Surface,

            MinImageCount = imageCount,
            ImageFormat = surfaceFormat.Format,
            ImageColorSpace = surfaceFormat.ColorSpace,
            ImageExtent = extent,
            ImageArrayLayers = 1,
            ImageUsage = ImageUsageFlags.ColorAttachmentBit,
        };

        var indices = device.QueueFamilyIndices;
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

        if (!VkUtil.Vk.TryGetDeviceExtension(VkUtil.Instance, VkUtil.Device, out KhrSwapchain khrSwapChain))
        {
            throw new NotSupportedException("VK_KHR_swapchain extension not found.");
        }

        KhrSwapChain = khrSwapChain;
        

        if (KhrSwapChain!.CreateSwapchain(VkUtil.Device, in createInfo, null, out var swapChain) !=
            Result.Success)
        {
            throw new Exception("failed to create swap chain!");
        }

        SwapChain = swapChain;

        KhrSwapChain.GetSwapchainImages(VkUtil.Device, SwapChain, ref imageCount, null);
        SwapChainImages = new Image[imageCount];
        fixed (Image* swapChainImagesPtr = SwapChainImages)
        {
            KhrSwapChain.GetSwapchainImages(VkUtil.Device, SwapChain, ref imageCount, swapChainImagesPtr);
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
    
    private void CreateImageViews()
    {
        SwapChainImageViews = new ImageView[SwapChainImages!.Length];

        for (int i = 0; i < SwapChainImages.Length; i++)
        {
            SwapChainImageViews[i] = CreateImageView(SwapChainImages[i], SwapChainImageFormat);
        }
    }
    
    private unsafe ImageView CreateImageView(Image image, Format format)
    {
        ImageViewCreateInfo createInfo = new()
        {
            SType = StructureType.ImageViewCreateInfo,
            Image = image,
            ViewType = ImageViewType.Type2D,
            Format = format,
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


        if (VkUtil.Vk.CreateImageView(VkUtil.Device, in createInfo, null, out ImageView imageView) != Result.Success)
        {
            throw new Exception("failed to create image views!");
        }

        return imageView;
    }

    public unsafe void Dispose()
    {
        foreach (var imageView in SwapChainImageViews!)
        {
            VkUtil.Vk.DestroyImageView(VkUtil.Device, imageView, null);
        }
        KhrSwapChain!.DestroySwapchain(VkUtil.Device, SwapChain, null);
    }
}