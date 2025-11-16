using Silk.NET.Vulkan;

namespace VulkanTest;

struct QueueFamilyIndices
{
    public uint? GraphicsFamily { get; set; }
    public bool IsComplete()
    {
        return GraphicsFamily.HasValue;
    }
}

public class VkDevice
{
    public PhysicalDevice PhysicalDevice { get; private set; }

    public VkDevice(VkInstance vkInstance)
    {
        PickPhysicalDevice(vkInstance);
    }
    
    private void PickPhysicalDevice(VkInstance vkInstance)
    {
        var devices = vkInstance.Vk!.GetPhysicalDevices(vkInstance.Instance);

        foreach (var device in devices)
        {
            if (!IsDeviceSuitable(device, vkInstance)) 
                continue;
            
            PhysicalDevice = device;
            break;
        }

        if (PhysicalDevice.Handle == 0)
        {
            throw new Exception("failed to find a suitable GPU!");
        }
    }

    private bool IsDeviceSuitable(PhysicalDevice device, VkInstance vkInstance)
    {
        var indices = FindQueueFamilies(device, vkInstance);

        return indices.IsComplete();
    }

    private unsafe QueueFamilyIndices FindQueueFamilies(PhysicalDevice device, VkInstance vkInstance)
    {
        var indices = new QueueFamilyIndices();

        uint queueFamilyPropertyCount = 0;
        vkInstance.Vk.GetPhysicalDeviceQueueFamilyProperties(device, ref queueFamilyPropertyCount, null);

        var queueFamilies = new QueueFamilyProperties[queueFamilyPropertyCount];
        fixed (QueueFamilyProperties* queueFamiliesPtr = queueFamilies)
        {
            vkInstance.Vk.GetPhysicalDeviceQueueFamilyProperties(device, ref queueFamilyPropertyCount, queueFamiliesPtr);
        }

        uint i = 0;
        foreach (var queueFamily in queueFamilies)
        {
            if (queueFamily.QueueFlags.HasFlag(QueueFlags.GraphicsBit))
                indices.GraphicsFamily = i;

            if (indices.IsComplete())
                break;
            i++;
        }

        return indices;
    }
}