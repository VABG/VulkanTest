using System.Runtime.InteropServices;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;

namespace VulkanTest;

public unsafe class VkInstance : IDisposable
{
#if DEBUG
    private const bool EnableValidationLayers = true;
#else
    private const bool EnableValidationLayers = false;
#endif

    public Vk Vk { get; }
    public Instance Instance { get; private set; }
    private VkValidationLayers? _validationLayers;
    private readonly VkWindow _window;
    private readonly VkDevice _device;

    public VkInstance(int resolutionWidth, int resolutionHeight)
    {
        _window = new VkWindow(resolutionWidth, resolutionHeight);
        Vk = Vk.GetApi();
        CreateInstance();
        _device = new VkDevice(this);
    }

    private void CreateInstance()
    {
        if (EnableValidationLayers)
        {
            _validationLayers = new VkValidationLayers(this);
            if (!_validationLayers.CheckValidationLayerSupport())
                throw new Exception("validation layers requested, but not available!");
        }

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

        if (EnableValidationLayers)
        {
            createInfo.EnabledLayerCount = (uint)_validationLayers.ValidationLayers.Length;
            createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(_validationLayers.ValidationLayers);

            // Needs to be created here to stay alive during CreateInstance due to & referencing
            DebugUtilsMessengerCreateInfoEXT debugCreateInfo = new();
            _validationLayers?.PopulateDebugMessengerCreateInfo(ref debugCreateInfo);
            createInfo.PNext = &debugCreateInfo;
        }
        else
        {
            createInfo.EnabledLayerCount = 0;
            createInfo.PNext = null;
        }

        if (Vk.CreateInstance(in createInfo, null, out var instance) != Result.Success)
        {
            throw new Exception("failed to create instance!");
        }

        Instance = instance;
        _validationLayers?.SetupDebugMessenger();

        Marshal.FreeHGlobal((IntPtr)appInfo.PApplicationName);
        Marshal.FreeHGlobal((IntPtr)appInfo.PEngineName);
        SilkMarshal.Free((nint)createInfo.PpEnabledExtensionNames);

        if (EnableValidationLayers)
            SilkMarshal.Free((nint)createInfo.PpEnabledLayerNames);
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
        _window.Run();
    }

    public void Dispose()
    {
        _validationLayers?.Dispose();
        Vk.DestroyInstance(Instance, null);
        Vk.Dispose();
        _window.Dispose();
    }
}