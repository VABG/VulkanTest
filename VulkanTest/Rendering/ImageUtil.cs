using Silk.NET.Vulkan;

namespace VulkanTest;

public class ImageUtil
{
    private readonly MemoryUtil _memoryUtil;

    public ImageUtil(MemoryUtil memoryUtil)
    {
        _memoryUtil = memoryUtil;
    }
    
    public unsafe ImageView CreateImageView(Image image, Format format, ImageAspectFlags aspectFlags, uint mipLevels)
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
                AspectMask = aspectFlags,
                BaseMipLevel = 0,
                LevelCount = mipLevels,
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
    
    public unsafe void CreateImage(uint width, uint height, uint mipLevels, SampleCountFlags numSamples,  Format format, ImageTiling tiling, ImageUsageFlags usage, MemoryPropertyFlags properties, ref Image image, ref DeviceMemory imageMemory)
    {
        ImageCreateInfo imageInfo = new()
        {
            SType = StructureType.ImageCreateInfo,
            ImageType = ImageType.Type2D,
            Extent =
            {
                Width = width,
                Height = height,
                Depth = 1,
            },
            MipLevels = mipLevels,
            ArrayLayers = 1,
            Format = format,
            Tiling = tiling,
            InitialLayout = ImageLayout.Undefined,
            Usage = usage,
            Samples = numSamples,
            SharingMode = SharingMode.Exclusive,
        };

        fixed (Image* imagePtr = &image)
        {
            if (VkUtil.Vk.CreateImage(VkUtil.Device, in imageInfo, null, imagePtr) != Result.Success)
            {
                throw new Exception("failed to create image!");
            }
        }

        VkUtil.Vk.GetImageMemoryRequirements(VkUtil.Device, image, out MemoryRequirements memRequirements);

        MemoryAllocateInfo allocInfo = new()
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memRequirements.Size,
            MemoryTypeIndex = _memoryUtil.FindMemoryType(memRequirements.MemoryTypeBits, properties),
        };

        fixed (DeviceMemory* imageMemoryPtr = &imageMemory)
        {
            if (VkUtil.Vk.AllocateMemory(VkUtil.Device, in allocInfo, null, imageMemoryPtr) != Result.Success)
            {
                throw new Exception("failed to allocate image memory!");
            }
        }

        VkUtil.Vk.BindImageMemory(VkUtil.Device, image, imageMemory, 0);
    }
}