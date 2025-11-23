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
    private readonly string[] _deviceExtensions = [KhrSwapchain.ExtensionName];
    
    public PhysicalDevice PhysicalDevice { get; private set; }
    public Device Device { get; private set; }
    public Queue GraphicsQueue { get; private set; }
    public Queue PresentQueue { get; private set; }
    public KhrSurface? KhrSurface { get; private set; }
    public SurfaceKHR Surface { get; private set; }
    public QueueFamilyIndices QueueFamilyIndices { get; private set; }
    public SampleCountFlags MaxMsaaSamples { get; private set; }

    public VkDevice(VkValidationLayers? validationLayers, Instance instance, Vk vk, VkWindow window)
    {
        CreateSurface(window, instance, vk);
        PickPhysicalDevice(vk, instance);
        CreateLogicalDevice(vk, validationLayers);
        MaxMsaaSamples = GetMaxUsableSampleCount(vk);
    }

    private void CreateLogicalDevice( Vk vk, VkValidationLayers? validationLayers = null)
    {
        QueueFamilyIndices = FindQueueFamilies(PhysicalDevice, vk);
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
        
        PhysicalDeviceFeatures deviceFeatures = new()
        {
            SamplerAnisotropy = true,
        };
        
        var createInfo = new DeviceCreateInfo
        {
            SType = StructureType.DeviceCreateInfo,
            QueueCreateInfoCount = (uint)uniqueQueueFamilies.Length,
            PQueueCreateInfos = queueCreateInfos,

            PEnabledFeatures = &deviceFeatures,

            EnabledExtensionCount = (uint)_deviceExtensions.Length,
            PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(_deviceExtensions)
        };

        if (validationLayers != null)
            createInfo.EnabledLayerCount = (uint)validationLayers.ValidationLayers.Length;
        else
            createInfo.EnabledLayerCount = 0;

        if (vk.CreateDevice(PhysicalDevice, in createInfo, null, out var device) != Result.Success)
        {
            throw new Exception("failed to create logical device!");
        }

        Device = device;

        vk.GetDeviceQueue(Device, QueueFamilyIndices.GraphicsFamily!.Value, 0, out var graphicsQueue);
        GraphicsQueue = graphicsQueue;
        vk.GetDeviceQueue(Device, QueueFamilyIndices.PresentFamily!.Value, 0, out var presentQueue);
        PresentQueue = presentQueue;
    }
    
    private void CreateSurface(VkWindow window, Instance instance, Vk vk)
    {
        if (!vk.TryGetInstanceExtension<KhrSurface>(instance, out var khrSurface))
        {
            throw new NotSupportedException("KHR_surface extension not found.");
        }
        KhrSurface = khrSurface;
        Surface = window.Window.VkSurface!.Create<AllocationCallbacks>(instance.ToHandle(), null).ToSurface();
    }
    
    private void PickPhysicalDevice(Vk vk, Instance instance)
    {
        var devices = vk.GetPhysicalDevices(instance);

        foreach (var device in devices)
        {
            if (!IsDeviceSuitable(device, vk)) 
                continue;
            
            PhysicalDevice = device;
            break;
        }

        if (PhysicalDevice.Handle == 0)
        {
            throw new Exception("failed to find a suitable GPU!");
        }
    }

    private bool IsDeviceSuitable(PhysicalDevice device, Vk vk)
    {
        var indices = FindQueueFamilies(device, vk);
        if (!indices.IsComplete())
            return false;
        
        if (!CheckDeviceExtensionsSupport(device, vk))
            return false;

        var swapChainSupport = QuerySwapChainSupport(device);
        return swapChainSupport.Formats.Any() && swapChainSupport.PresentModes.Any();
    }

    private QueueFamilyIndices FindQueueFamilies(PhysicalDevice device, Vk vk)
    {
        var indices = new QueueFamilyIndices();

        uint queueFamilyPropertyCount = 0;
        vk.GetPhysicalDeviceQueueFamilyProperties(device, ref queueFamilyPropertyCount, null);

        var queueFamilies = new QueueFamilyProperties[queueFamilyPropertyCount];
        fixed (QueueFamilyProperties* queueFamiliesPtr = queueFamilies)
        {
            vk.GetPhysicalDeviceQueueFamilyProperties(device, ref queueFamilyPropertyCount, queueFamiliesPtr);
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
    
    private bool CheckDeviceExtensionsSupport(PhysicalDevice device, Vk vk)
    {
        uint extensionCount = 0;
        vk.EnumerateDeviceExtensionProperties(device, (byte*)null, ref extensionCount, null);

        var availableExtensions = new ExtensionProperties[extensionCount];
        fixed (ExtensionProperties* availableExtensionsPtr = availableExtensions)
        {
            vk.EnumerateDeviceExtensionProperties(device, (byte*)null, ref extensionCount, availableExtensionsPtr);
        }

        var availableExtensionNames = availableExtensions.Select(extension => Marshal.PtrToStringAnsi((IntPtr)extension.ExtensionName)).ToHashSet();

        return _deviceExtensions.All(availableExtensionNames.Contains);
    }
    
    public SwapChainSupportDetails QuerySwapChainSupport(PhysicalDevice physicalDevice)
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
    
    private SampleCountFlags GetMaxUsableSampleCount(Vk vk)
    {
        vk.GetPhysicalDeviceProperties2(PhysicalDevice, out var physicalDeviceProperties);
        
        var counts = physicalDeviceProperties.Properties.Limits.FramebufferColorSampleCounts & physicalDeviceProperties.Properties.Limits.FramebufferDepthSampleCounts;

        return counts switch
        {
            var c when (c & SampleCountFlags.Count64Bit) != 0 => SampleCountFlags.Count64Bit,
            var c when (c & SampleCountFlags.Count32Bit) != 0 => SampleCountFlags.Count32Bit,
            var c when (c & SampleCountFlags.Count16Bit) != 0 => SampleCountFlags.Count16Bit,
            var c when (c & SampleCountFlags.Count8Bit) != 0 => SampleCountFlags.Count8Bit,
            var c when (c & SampleCountFlags.Count4Bit) != 0 => SampleCountFlags.Count4Bit,
            var c when (c & SampleCountFlags.Count2Bit) != 0 => SampleCountFlags.Count2Bit,
            _ => SampleCountFlags.Count1Bit
        };
    }


    public void Dispose()
    {
        VkUtil.Vk.DestroyDevice(Device, null);
        KhrSurface!.DestroySurface(VkUtil.Instance, Surface, null);
    }
}