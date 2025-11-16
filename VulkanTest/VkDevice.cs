using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace VulkanTest;

public struct QueueFamilyIndices
{
    public uint? GraphicsFamily { get; set; }
    public uint? PresentFamily { get; set; }

    public bool IsComplete()
    {
        return GraphicsFamily.HasValue && PresentFamily.HasValue;
    }
}

public unsafe class VkDevice : IDisposable
{
    private readonly VkInstance _vkInstance;
    private readonly string[] _deviceExtensions = [KhrSwapchain.ExtensionName];
    
    public PhysicalDevice PhysicalDevice { get; private set; }
    public Device Device { get; private set; }
    private Queue _graphicsQueue;
    private Queue _presentQueue;
    public KhrSurface? KhrSurface { get; private set; }
    public SurfaceKHR Surface { get; private set; }
    public QueueFamilyIndices QueueFamilyIndices { get; private set; }

    public VkDevice(VkInstance vkInstance)
    {
        _vkInstance = vkInstance;
        CreateSurface(vkInstance);
        PickPhysicalDevice(vkInstance);
        CreateLogicalDevice(PhysicalDevice, vkInstance);
    }

    private void CreateLogicalDevice(PhysicalDevice physicalDevice, VkInstance vkInstance)
    {
        QueueFamilyIndices = FindQueueFamilies(physicalDevice, vkInstance);
        var uniqueQueueFamilies = new[] { QueueFamilyIndices.GraphicsFamily!.Value, QueueFamilyIndices.PresentFamily!.Value };
        // Both values can be the same
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

            EnabledExtensionCount = (uint)_deviceExtensions.Length,
            PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(_deviceExtensions)
        };

        if (VkInstance.EnableValidationLayers)
        {
            createInfo.EnabledLayerCount = (uint)vkInstance.ValidationLayers!.ValidationLayers.Length;
            createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(vkInstance.ValidationLayers!.ValidationLayers);
        }
        else
        {
            createInfo.EnabledLayerCount = 0;
        }

        if (vkInstance.Vk.CreateDevice(physicalDevice, in createInfo, null, out var device) != Result.Success)
        {
            throw new Exception("failed to create logical device!");
        }

        Device = device;

        vkInstance.Vk.GetDeviceQueue(Device, QueueFamilyIndices.GraphicsFamily!.Value, 0, out _graphicsQueue);
        vkInstance.Vk.GetDeviceQueue(Device, QueueFamilyIndices.PresentFamily!.Value, 0, out _presentQueue);

        if (VkInstance.EnableValidationLayers)
        {
            SilkMarshal.Free((nint)createInfo.PpEnabledLayerNames);
        }
    }
    
    private void CreateSurface(VkInstance vkInstance)
    {
        if (!vkInstance.Vk.TryGetInstanceExtension<KhrSurface>(vkInstance.Instance, out var khrSurface))
        {
            throw new NotSupportedException("KHR_surface extension not found.");
        }
        KhrSurface = khrSurface;
        Surface = vkInstance.Window.Window.VkSurface!.Create<AllocationCallbacks>(vkInstance.Instance.ToHandle(), null).ToSurface();
    }
    
    private void PickPhysicalDevice(VkInstance vkInstance)
    {
        var devices = vkInstance.Vk.GetPhysicalDevices(vkInstance.Instance);

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
        if (!indices.IsComplete())
            return false;
        
        if (!CheckDeviceExtensionsSupport(device, vkInstance))
            return false;

        var swapChainSupport = QuerySwapChainSupport(device, vkInstance);
        return swapChainSupport.Formats.Any() && swapChainSupport.PresentModes.Any();
    }

    private QueueFamilyIndices FindQueueFamilies(PhysicalDevice device, VkInstance vkInstance)
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

            KhrSurface!.GetPhysicalDeviceSurfaceSupport(device, i, Surface, out var presentSupport);

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
    
    private bool CheckDeviceExtensionsSupport(PhysicalDevice device, VkInstance instance)
    {
        uint extensionCount = 0;
        instance.Vk.EnumerateDeviceExtensionProperties(device, (byte*)null, ref extensionCount, null);

        var availableExtensions = new ExtensionProperties[extensionCount];
        fixed (ExtensionProperties* availableExtensionsPtr = availableExtensions)
        {
            instance.Vk.EnumerateDeviceExtensionProperties(device, (byte*)null, ref extensionCount, availableExtensionsPtr);
        }

        var availableExtensionNames = availableExtensions.Select(extension => Marshal.PtrToStringAnsi((IntPtr)extension.ExtensionName)).ToHashSet();

        return _deviceExtensions.All(availableExtensionNames.Contains);
    }
    
    public SwapChainSupportDetails QuerySwapChainSupport(PhysicalDevice physicalDevice, VkInstance instance)
    {
        var details = new SwapChainSupportDetails();
        
        KhrSurface!.GetPhysicalDeviceSurfaceCapabilities(physicalDevice, Surface, out details.Capabilities);

        uint formatCount = 0;
        KhrSurface.GetPhysicalDeviceSurfaceFormats(physicalDevice, Surface, ref formatCount, null);

        if (formatCount != 0)
        {
            details.Formats = new SurfaceFormatKHR[formatCount];
            fixed (SurfaceFormatKHR* formatsPtr = details.Formats)
            {
                KhrSurface.GetPhysicalDeviceSurfaceFormats(physicalDevice, Surface, ref formatCount, formatsPtr);
            }
        }
        else
        {
            details.Formats = Array.Empty<SurfaceFormatKHR>();
        }

        uint presentModeCount = 0;
        KhrSurface.GetPhysicalDeviceSurfacePresentModes(physicalDevice, Surface, ref presentModeCount, null);

        if (presentModeCount != 0)
        {
            details.PresentModes = new PresentModeKHR[presentModeCount];
            fixed (PresentModeKHR* formatsPtr = details.PresentModes)
            {
                KhrSurface.GetPhysicalDeviceSurfacePresentModes(physicalDevice, Surface, ref presentModeCount, formatsPtr);
            }
        }
        else
        {
            details.PresentModes = Array.Empty<PresentModeKHR>();
        }

        return details;
    }

    public void Dispose()
    {
        KhrSurface!.DestroySurface(_vkInstance.Instance, Surface, null);
        _vkInstance.Vk.DestroyDevice(Device, null);
    }
}