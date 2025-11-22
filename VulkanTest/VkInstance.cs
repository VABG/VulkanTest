using System.Runtime.InteropServices;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Maths;
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
    public readonly VkWindow Window;
    public readonly VkDevice Device;
    public readonly MemoryUtil MemoryUtil;
    public readonly ImageUtil ImageUtil;
    public readonly DepthFormatUtil DepthFormatUtil;
    public VkSwapChain SwapChain { get; private set; }
    public VkRenderPass RenderPass { get; private set; }
    public VkGraphicsPipeline GraphicsPipeline { get; private set; }
    public VkCommands Commands { get; private set; }
    private VkFrameDrawer _vkFrameDrawer;
    
    public VkDepthImage DepthImage { get; private set; }
    public VkImageView ImageView { get; private set; }
    
    public CommandBufferUtil CommandBufferUtil { get; private set; }
    public VkVertexBuffer VertexBuffer { get; private set; }
    public VkUniformBuffer UniformBuffer { get; private set; } 
    public VkDescriptorPool DescriptorPool { get; private set; }
    
    public VkInstance(int resolutionWidth, int resolutionHeight)
    {
        Window = new VkWindow(resolutionWidth, resolutionHeight);
        Vk = Vk.GetApi();
        CreateInstance();
        ValidationLayers?.SetupDebugMessenger();
        Device = new VkDevice(this);
        
        MemoryUtil = new MemoryUtil(this);
        ImageUtil = new ImageUtil(this);
        DepthFormatUtil = new DepthFormatUtil(this);
        
        InitializeSwapChain();
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
        Window.Window.Render += WindowOnRender;
        Window.Run();
    }

    private void WindowOnRender(double obj)
    {
        _vkFrameDrawer.DrawFrame(0, this);
    }
    
    public void RecreateSwapChain()
    {
        Vector2D<int> framebufferSize = Window.Window.FramebufferSize;

        while (framebufferSize.X == 0 || framebufferSize.Y == 0)
        {
            framebufferSize = Window.Window.FramebufferSize;
            Window.Window.DoEvents();
        }

        Vk.DeviceWaitIdle(Device.Device);
        ResetSwapChain();
    }
    
    private void InitializeSwapChain()
    {
        SwapChain = new VkSwapChain(this);
        
        DepthImage = new VkDepthImage(this);
        RenderPass = new VkRenderPass(this);
        GraphicsPipeline = new VkGraphicsPipeline(this);
        Commands = new VkCommands(this);
        CommandBufferUtil = new CommandBufferUtil(this);

        ImageView = new VkImageView(this);
        VertexBuffer = new VkVertexBuffer(this);
        UniformBuffer = new VkUniformBuffer(this);
        DescriptorPool = new VkDescriptorPool(this);
        Commands.CreateCommandBuffers();
        _vkFrameDrawer = new VkFrameDrawer(this);
    }

    private void ResetSwapChain()
    {
        DisposeSwapChain();
        SwapChain = new VkSwapChain(this);
        GraphicsPipeline = new VkGraphicsPipeline(this);
        Commands = new VkCommands(this);
        Commands.CreateCommandBuffers();
        _vkFrameDrawer = new VkFrameDrawer(this);
    }
    
    public void Dispose()
    {
        DisposeSwapChain();
        ImageView.Dispose();
        DisposeDeviceAndWindow();
    }
    
    private void DisposeSwapChain()
    {
        DepthImage.Dispose();
        UniformBuffer.Dispose();
        VertexBuffer.Dispose();
        _vkFrameDrawer.Dispose();
        Commands.Dispose();
        RenderPass.Dispose();
        GraphicsPipeline.Dispose();
        SwapChain.Dispose(); 
        DescriptorPool.Dispose();
    }

    private void DisposeDeviceAndWindow()
    {
        ValidationLayers?.Dispose();
        Device.Dispose();
        Vk.DestroyInstance(Instance, null);
        Vk.Dispose();
        Window.Dispose();
    }
}