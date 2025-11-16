using System.Runtime.CompilerServices;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace VulkanTest;

internal struct QueueFamilyIndices
{
    public uint? GraphicsFamily { get; set; }
    public uint? PresentFamily { get; set; }

    public bool IsComplete()
    {
        return GraphicsFamily.HasValue && PresentFamily.HasValue;
    }
}

public class VkDevice
{
    private PhysicalDevice _physicalDevice;
    private Device _device;
    private Queue _graphicsQueue;
    private Queue _presentQueue;
    private KhrSurface? _khrSurface;
    private SurfaceKHR _surface;


    public VkDevice(VkInstance vkInstance)
    {
        CreateSurface(vkInstance);
        PickPhysicalDevice(vkInstance);
        CreateLogicalDevice(_physicalDevice, vkInstance);
    }

    private unsafe void CreateLogicalDevice(PhysicalDevice physicalDevice, VkInstance vkInstance)
    {
        var indices = FindQueueFamilies(physicalDevice, vkInstance);
        var uniqueQueueFamilies = new[] { indices.GraphicsFamily!.Value, indices.PresentFamily!.Value };
        uniqueQueueFamilies = uniqueQueueFamilies.Distinct().ToArray();

        using var mem = GlobalMemory.Allocate(uniqueQueueFamilies.Length * sizeof(DeviceQueueCreateInfo));
        // This seems to be automatically deallocated if it's in a method scope? That or the DeviceFeatures reference?
        var queueCreateInfos = (DeviceQueueCreateInfo*)Unsafe.AsPointer(ref mem.GetPinnableReference());

        float queuePriority = 1.0f;
        for (int i = 0; i < uniqueQueueFamilies.Length; i++)
        {
            queueCreateInfos[i] = new DeviceQueueCreateInfo
            {
                SType = StructureType.DeviceQueueCreateInfo,
                QueueFamilyIndex = uniqueQueueFamilies[i],
                QueueCount = 1,
                PQueuePriorities = &queuePriority
            };
        }

        // No features currently
        PhysicalDeviceFeatures deviceFeatures = new();

        var createInfo = new DeviceCreateInfo
        {
            SType = StructureType.DeviceCreateInfo,
            QueueCreateInfoCount = (uint)uniqueQueueFamilies.Length,
            PQueueCreateInfos = queueCreateInfos,

            PEnabledFeatures = &deviceFeatures,

            EnabledExtensionCount = 0
        };

        if (VkInstance.EnableValidationLayers)
        {
            createInfo.EnabledLayerCount = (uint)vkInstance.ValidationLayers!.ValidationLayers.Length!;
            createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(vkInstance.ValidationLayers!.ValidationLayers);
        }
        else
        {
            createInfo.EnabledLayerCount = 0;
        }

        if (vkInstance.Vk.CreateDevice(physicalDevice, in createInfo, null, out _device) != Result.Success)
        {
            throw new Exception("failed to create logical device!");
        }

        vkInstance.Vk.GetDeviceQueue(_device, indices.GraphicsFamily!.Value, 0, out _graphicsQueue);
        vkInstance.Vk.GetDeviceQueue(_device, indices.PresentFamily!.Value, 0, out _presentQueue);

        if (VkInstance.EnableValidationLayers)
        {
            SilkMarshal.Free((nint)createInfo.PpEnabledLayerNames);
        }
    }
    
    private unsafe void CreateSurface(VkInstance vkInstance)
    {
        if (!vkInstance.Vk.TryGetInstanceExtension<KhrSurface>(vkInstance.Instance, out _khrSurface))
        {
            throw new NotSupportedException("KHR_surface extension not found.");
        }

        _surface = vkInstance.Window.Window.VkSurface!.Create<AllocationCallbacks>(vkInstance.Instance.ToHandle(), null).ToSurface();
    }
    
    private void PickPhysicalDevice(VkInstance vkInstance)
    {
        var devices = vkInstance.Vk.GetPhysicalDevices(vkInstance.Instance);

        foreach (var device in devices)
        {
            if (!IsDeviceSuitable(device, vkInstance)) 
                continue;
            
            _physicalDevice = device;
            break;
        }

        if (_physicalDevice.Handle == 0)
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
            {
                indices.GraphicsFamily = i;
            }

            _khrSurface!.GetPhysicalDeviceSurfaceSupport(device, i, _surface, out var presentSupport);

            if (presentSupport)
            {
                indices.PresentFamily = i;
            }

            if (indices.IsComplete())
            {
                break;
            }

            i++;
        }

        return indices;
    }
}