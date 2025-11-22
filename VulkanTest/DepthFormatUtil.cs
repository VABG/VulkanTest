using Silk.NET.Vulkan;

namespace VulkanTest;

public class DepthFormatUtil
{
    private readonly VkInstance _instance;

    public DepthFormatUtil(VkInstance instance)
    {
        _instance = instance;
    }
    
    public Format FindDepthFormat()
    {
        return FindSupportedFormat(new[]
            {
                Format.D32Sfloat,
                Format.D32SfloatS8Uint,
                Format.D24UnormS8Uint
            },
            ImageTiling.Optimal,
            FormatFeatureFlags.DepthStencilAttachmentBit);
    }
    
    private Format FindSupportedFormat(IEnumerable<Format> candidates, ImageTiling tiling, FormatFeatureFlags features)
    {
        foreach (var format in candidates)
        {
            //TODO: Pre-process this info in a class after creating device and instance and save it somewhere
            _instance.Vk.GetPhysicalDeviceFormatProperties(_instance.Device.PhysicalDevice, format, out var props);

            if (tiling == ImageTiling.Linear && (props.LinearTilingFeatures & features) == features)
            {
                return format;
            }
            else if (tiling == ImageTiling.Optimal && (props.OptimalTilingFeatures & features) == features)
            {
                return format;
            }
        }

        throw new Exception("failed to find supported format!");
    }
}