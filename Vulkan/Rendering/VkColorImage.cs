using Silk.NET.Vulkan;

namespace Vulkan.Rendering;

public class VkColorImage : IDisposable
{
    private readonly Image _colorImage;
    private readonly DeviceMemory _colorImageMemory;
    public ImageView ColorImageView {get; }

    public VkColorImage(VkRender render)
    {
        Format colorFormat = render.SwapChain.SwapChainImageFormat;
        var swapChainExtent = render.SwapChain.SwapChainExtent;
        
        render.ImageUtil.CreateImage(swapChainExtent.Width,
            swapChainExtent.Height,
            1,
            render.MsaaSamples,
            colorFormat,
            ImageTiling.Optimal,
            ImageUsageFlags.TransientAttachmentBit | ImageUsageFlags.ColorAttachmentBit,
            MemoryPropertyFlags.DeviceLocalBit,
            ref _colorImage,
            ref _colorImageMemory);
        ColorImageView = render.ImageUtil.CreateImageView(_colorImage, colorFormat, ImageAspectFlags.ColorBit, 1);
    }

    public unsafe void Dispose()
    {
        VkUtil.Vk.DestroyImageView(VkUtil.Device, ColorImageView, null);
        VkUtil.Vk.DestroyImage(VkUtil.Device, _colorImage, null);
        VkUtil.Vk.FreeMemory(VkUtil.Device, _colorImageMemory, null);
    }
}