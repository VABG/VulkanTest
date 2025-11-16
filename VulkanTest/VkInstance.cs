using System.Runtime.InteropServices;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;

namespace VulkanTest;

public unsafe class VkInstance : IDisposable
{
#if DEBUG
    public const bool EnableValidationLayers = true;
#else
    public const bool EnableValidationLayers = false;
#endif

    public Vk Vk { get; }
    public Instance Instance { get; private set; }
    public VkValidationLayers? ValidationLayers { get; private set; }
    public  VkWindow Window { get; }
    public readonly VkDevice Device;
    private readonly VkSwapChain _swapChain;


    public VkInstance(int resolutionWidth, int resolutionHeight)
    {
        Window = new VkWindow(resolutionWidth, resolutionHeight);
        Vk = Vk.GetApi();
        CreateInstance();
        ValidationLayers?.SetupDebugMessenger();
        Device = new VkDevice(this);
        _swapChain = new VkSwapChain(this);
    }

    private void CreateInstance()
    {
        if (EnableValidationLayers)
        {
            ValidationLayers = new VkValidationLayers(this);
            if (!ValidationLayers.CheckValidationLayerSupport())
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
            createInfo.EnabledLayerCount = (uint)ValidationLayers.ValidationLayers.Length;
            createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(ValidationLayers.ValidationLayers);

            // Needs to be created here to stay alive during CreateInstance due to & referencing
            DebugUtilsMessengerCreateInfoEXT debugCreateInfo = new();
            ValidationLayers?.PopulateDebugMessengerCreateInfo(ref debugCreateInfo);
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

        Marshal.FreeHGlobal((IntPtr)appInfo.PApplicationName);
        Marshal.FreeHGlobal((IntPtr)appInfo.PEngineName);
        SilkMarshal.Free((nint)createInfo.PpEnabledExtensionNames);

        if (EnableValidationLayers)
            SilkMarshal.Free((nint)createInfo.PpEnabledLayerNames);
    }

    private string[] GetRequiredExtensions()
    {
        var glfwExtensions = Window.Window.VkSurface!.GetRequiredExtensions(out var glfwExtensionCount);
        var extensions = SilkMarshal.PtrToStringArray((nint)glfwExtensions, (int)glfwExtensionCount);

        if (EnableValidationLayers)
            extensions = extensions.Append(ExtDebugUtils.ExtensionName).ToArray();

        return extensions;
    }
    

    public void Run()
    {
        Window.Run();
    }

    public void Dispose()
    {
        ValidationLayers?.Dispose();
        Device.Dispose();
        Vk.DestroyInstance(Instance, null);
        Vk.Dispose();
        Window.Dispose();
    }
}