using Silk.NET.Vulkan;

namespace VulkanTest;

public class ImageViewUtil
{
    private readonly VkInstance _instance;

    public ImageViewUtil(VkInstance instance)
    {
        _instance = instance;
    }
    
    public unsafe ImageView CreateImageView(Image image, Format format)
    {
        ImageViewCreateInfo createInfo = new()
        {
            SType = StructureType.ImageViewCreateInfo,
            Image = image,
            ViewType = ImageViewType.Type2D,
            Format = format,
            //Components =
            //    {
            //        R = ComponentSwizzle.Identity,
            //        G = ComponentSwizzle.Identity,
            //        B = ComponentSwizzle.Identity,
            //        A = ComponentSwizzle.Identity,
            //    },
            SubresourceRange =
            {
                AspectMask = ImageAspectFlags.ColorBit,
                BaseMipLevel = 0,
                LevelCount = 1,
                BaseArrayLayer = 0,
                LayerCount = 1,
            }

        };


        if (_instance.Vk.CreateImageView(_instance.Device.Device, in createInfo, null, out ImageView imageView) != Result.Success)
        {
            throw new Exception("failed to create image views!");
        }

        return imageView;
    }
}