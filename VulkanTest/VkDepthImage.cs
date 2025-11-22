using Silk.NET.Vulkan;

namespace VulkanTest;

public class VkDepthImage : IDisposable
{
    private readonly VkInstance _instance;
    private Image _depthImage;
    private DeviceMemory _depthImageMemory;
    public ImageView DepthImageView { get; private set; }
    
    public VkDepthImage(VkInstance instance)
    {
        _instance = instance;
        CreateDepthResources();
    }
    
    private void CreateDepthResources()
    {
        Format depthFormat = _instance.DepthFormatUtil.FindDepthFormat();
        var swapChainExtent = _instance.SwapChain.SwapChainExtent;
        _instance.ImageUtil.CreateImage(swapChainExtent.Width,
            swapChainExtent.Height,
            depthFormat,
            ImageTiling.Optimal,
            ImageUsageFlags.DepthStencilAttachmentBit,
            MemoryPropertyFlags.DeviceLocalBit,
            ref _depthImage,
            ref _depthImageMemory);
        DepthImageView = _instance.ImageUtil.CreateImageView(_depthImage, depthFormat, ImageAspectFlags.DepthBit);
    }
    

    public unsafe void Dispose()
    {
        _instance.Vk.DestroyImageView(_instance.Device.Device, DepthImageView, null);
        _instance.Vk.DestroyImage(_instance.Device.Device, _depthImage, null);
        _instance.Vk.FreeMemory(_instance.Device.Device, _depthImageMemory, null);
    }
}