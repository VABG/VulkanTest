using System.Runtime.InteropServices;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using VulkanTest.Rendering;

namespace VulkanTest;

public unsafe class VkInstance : IDisposable
{
#if DEBUG
    public const bool EnableValidationLayers = true;
#else
    public const bool EnableValidationLayers = false;
#endif
    
    private readonly Instance _instance;
    private readonly Vk _vk;
    private readonly VkValidationLayers? _validationLayers;
    
    private readonly VkWindow _window;
    private readonly VkDevice _device;
    private readonly VkRender _render;
    
    public VkInstance(int resolutionWidth, int resolutionHeight)
    {
        _window = new VkWindow(resolutionWidth, resolutionHeight);
        _vk = Vk.GetApi();
        _instance = CreateInstance(_vk);
        if (EnableValidationLayers)
            _validationLayers = new VkValidationLayers(_vk, _instance);
        _device = new VkDevice(_validationLayers, _instance, _vk, _window);
        VkUtil.Populate(_vk, _instance, _device.Device, _device.PhysicalDevice );
        
        _render = new VkRender(_window, _device);
    }
    
    private Instance  CreateInstance(Vk vk)
    {
        ApplicationInfo appInfo = new()
        {
            SType = StructureType.ApplicationInfo,
            PApplicationName = (byte*)Marshal.StringToHGlobalAnsi("Hello Triangle"),
            ApplicationVersion = new Version32(1, 0, 0),
            PEngineName = (byte*)Marshal.StringToHGlobalAnsi("No Engine"),
            EngineVersion = new Version32(1, 0, 0),
            ApiVersion = Vk.Version12
        };

        InstanceCreateInfo createInfo = new()
        {
            SType = StructureType.InstanceCreateInfo,
            PApplicationInfo = &appInfo
        };

        var extensions = GetRequiredExtensions();
        createInfo.EnabledExtensionCount = (uint)extensions.Length;
        createInfo.PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(extensions);

        if (_validationLayers != null)
        {
            createInfo.EnabledLayerCount = (uint)_validationLayers.ValidationLayers.Length;
            DebugUtilsMessengerCreateInfoEXT debugCreateInfo = new();
            _validationLayers?.PopulateDebugMessengerCreateInfo(ref debugCreateInfo);
            createInfo.PNext = &debugCreateInfo;
        }
        else
        {
            createInfo.EnabledLayerCount = 0;
            createInfo.PNext = null;
        }

        if (vk.CreateInstance(in createInfo, null, out var instance) != Result.Success)
        {
            throw new Exception("failed to create instance!");
        }
        
        Marshal.FreeHGlobal((IntPtr)appInfo.PApplicationName);
        Marshal.FreeHGlobal((IntPtr)appInfo.PEngineName);
        SilkMarshal.Free((nint)createInfo.PpEnabledExtensionNames);
        
        return instance;
    }

    private string[] GetRequiredExtensions()
    {
        var glfwExtensions = _window.Window.VkSurface!.GetRequiredExtensions(out var glfwExtensionCount);
        var extensions = SilkMarshal.PtrToStringArray((nint)glfwExtensions, (int)glfwExtensionCount);

        if (EnableValidationLayers)
            extensions = extensions.Append(ExtDebugUtils.ExtensionName).ToArray();

        return extensions;
    }

    public void Run()
    {
        _window.Window.Render += WindowOnRender;
        _window.Run();
    }

    private void WindowOnRender(double obj)
    {
        _render.Render();
    }

    public void Dispose()
    {
        _render.Dispose();
        DisposeDeviceAndWindow();
    }

    private void DisposeDeviceAndWindow()
    {
        _validationLayers?.Dispose();
        _device.Dispose();
        _vk.DestroyInstance(_instance, null);
        _vk.Dispose();
        _window.Dispose();
    }
}