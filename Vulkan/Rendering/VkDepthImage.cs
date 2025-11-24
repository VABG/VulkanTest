using Silk.NET.Vulkan;

namespace Vulkan.Rendering;

public class VkDepthImage : IDisposable
{
    private Image _depthImage;
    private DeviceMemory _depthImageMemory;
    public ImageView DepthImageView { get; private set; }
    
    public VkDepthImage(VkRender render)
    {
        CreateDepthResources(render);
    }
    
    private void CreateDepthResources(VkRender render)
    {
        Format depthFormat = render.DepthFormatUtil.FindDepthFormat();
        var swapChainExtent = render.SwapChain.SwapChainExtent;
        render.ImageUtil.CreateImage(swapChainExtent.Width,
            swapChainExtent.Height,
            1,
            render.MsaaSamples,
            depthFormat,
            ImageTiling.Optimal,
            ImageUsageFlags.DepthStencilAttachmentBit,
            MemoryPropertyFlags.DeviceLocalBit,
            ref _depthImage,
            ref _depthImageMemory);
        DepthImageView = render.ImageUtil.CreateImageView(_depthImage, depthFormat, ImageAspectFlags.DepthBit, 1);
    }
    

    public unsafe void Dispose()
    {
        VkUtil.Vk.DestroyImageView(VkUtil.Device, DepthImageView, null);
        VkUtil.Vk.DestroyImage(VkUtil.Device, _depthImage, null);
        VkUtil.Vk.FreeMemory(VkUtil.Device, _depthImageMemory, null);
    }
}