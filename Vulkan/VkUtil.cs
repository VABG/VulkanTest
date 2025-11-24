using Silk.NET.Vulkan;

namespace Vulkan;

public static class VkUtil
{
    public static Device Device => GetDevice();
    private static Device? _device;

    public static PhysicalDevice PhysicalDevice => GetPhysicalDevice();
    private static PhysicalDevice? _physicalDevice;

    public static Instance Instance => GetInstance();
    private static Instance? _instance;

    public static Vk Vk => GetVk();
    private static Vk? _vk;

    public static void Populate(Vk vk, Instance instance, Device device, PhysicalDevice physicalDevice)
    {
        _vk = vk;
        _instance = instance;
        _device = device;
        _physicalDevice = physicalDevice;
    }

    private static Vk GetVk()
    {
        return _vk ?? throw new Exception("Static Vk not initialized");
    }

    private static Instance GetInstance()
    {
        return _instance ?? throw new Exception("Static Instance not initialized");
    }

    private static PhysicalDevice GetPhysicalDevice()
    {
        return _physicalDevice ?? throw new Exception("Static PhysicalDevice not initialized");
    }

    private static Device GetDevice()
    {
        return _device ?? throw new Exception("Static Device not initialized");
    }
}