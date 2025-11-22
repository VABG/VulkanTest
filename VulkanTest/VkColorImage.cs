using Silk.NET.Vulkan;

namespace VulkanTest;

public class VkColorImage : IDisposable
{
    private readonly VkInstance _instance;
    private Image _colorImage;
    private DeviceMemory _colorImageMemory;
    public ImageView ColorImageView {get; private set;}

    public VkColorImage(VkInstance instance)
    {
        _instance = instance;
        Format colorFormat = instance.SwapChain.SwapChainImageFormat;
        var swapChainExtent = instance.SwapChain.SwapChainExtent;
        
        instance.ImageUtil.CreateImage(swapChainExtent.Width,
            swapChainExtent.Height,
            1,
            instance.Device.MaxMsaaSamples,
            colorFormat,
            ImageTiling.Optimal,
            ImageUsageFlags.TransientAttachmentBit | ImageUsageFlags.ColorAttachmentBit,
            MemoryPropertyFlags.DeviceLocalBit,
            ref _colorImage,
            ref _colorImageMemory);
        ColorImageView = instance.ImageUtil.CreateImageView(_colorImage, colorFormat, ImageAspectFlags.ColorBit, 1);
    }

    public unsafe void Dispose()
    {
        _instance.Vk.DestroyImageView(_instance.Device.Device, ColorImageView, null);
        _instance.Vk.DestroyImage(_instance.Device.Device, _colorImage, null);
        _instance.Vk.FreeMemory(_instance.Device.Device, _colorImageMemory, null);
    }
}