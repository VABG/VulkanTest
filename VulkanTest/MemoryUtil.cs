using Silk.NET.Vulkan;

namespace VulkanTest;

public class MemoryUtil
{
    private readonly VkInstance _instance;

    public MemoryUtil(VkInstance instance)
    {
        _instance = instance;
    }
    
    public uint FindMemoryType(uint typeFilter, MemoryPropertyFlags properties)
    {
        _instance.Vk.GetPhysicalDeviceMemoryProperties(_instance.Device.PhysicalDevice, out var memProperties);

        for (int i = 0; i < memProperties.MemoryTypeCount; i++)
            if ((typeFilter & (1 << i)) != 0 && (memProperties.MemoryTypes[i].PropertyFlags & properties) == properties)
                return (uint)i;

        throw new Exception("failed to find suitable memory type!");
    }
}