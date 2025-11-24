using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Vulkan.Rendering.Assets;
using Vulkan.Rendering.Utils;
using Vulkan.ResourceManagement;

namespace Vulkan.Rendering;

public class VkRender : IDisposable
{
    private readonly VkWindow _window;
    private readonly VkDevice _device;

    // Nothing below here should be here
    public VkSwapChain SwapChain { get; private set; }
    public VkRenderPass RenderPass { get; private set; }
    public VkGraphicsPipeline GraphicsPipeline { get; private set; }
    public VkCommands Commands { get; private set; }
    private VkFrameDrawer _vkFrameDrawer;
    
    public VkDepthImage DepthImage { get; private set; }
    public VkColorImage ColorImage { get; private set; }
    public VkImageView ImageView { get; private set; }
    
    public CommandBufferUtil CommandBufferUtil { get; private set; }
    public VkModel Model { get; private set; }
    public VkUniformBuffer UniformBuffer { get; private set; } 
    public VkDescriptorPool DescriptorPool { get; private set; }

    public readonly ResourceManagement.Import.Shaders Shaders;

    public readonly MemoryUtil MemoryUtil;
    public readonly ImageUtil ImageUtil;
    public readonly DepthFormatUtil DepthFormatUtil;

    public SampleCountFlags MsaaSamples => _device.MaxMsaaSamples;
    
    public VkRender(VkWindow window, VkDevice device)
    {
        _window = window;
        _device = device;
        MemoryUtil = new MemoryUtil();
        ImageUtil = new ImageUtil(MemoryUtil);
        DepthFormatUtil = new DepthFormatUtil();
        CommandBufferUtil = new CommandBufferUtil(MemoryUtil, _device);
        Shaders = new ResourceManagement.Import.Shaders();
        
        SwapChain = new VkSwapChain(_device, _window);
        DepthImage = new VkDepthImage(this);
        ColorImage = new VkColorImage(this);
        RenderPass = new VkRenderPass(this);
        GraphicsPipeline = new VkGraphicsPipeline(this);
        Commands = new VkCommands(_device);

        ImageView = new VkImageView(this);
        Model = new VkModel(this, Resources.Models.Get(@"Assets\viking_room.obj"));
        UniformBuffer = new VkUniformBuffer(this);
        DescriptorPool = new VkDescriptorPool(this);
        Commands.CreateCommandBuffers(this);
        _vkFrameDrawer  = new VkFrameDrawer(_window, _device, this);    }
    
    public void RecreateSwapChain()
    {
        Vector2D<int> framebufferSize = _window.Window.FramebufferSize;

        while (framebufferSize.X == 0 || framebufferSize.Y == 0)
        {
            framebufferSize = _window.Window.FramebufferSize;
            _window.Window.DoEvents();
        }

        VkUtil.Vk.DeviceWaitIdle( VkUtil.Device);
        ResetSwapChain();
    }

    public void Render()
    {
        var time = (float)_window.Window.Time;
        _vkFrameDrawer.DrawFrame(0, time);
    }

    private void ResetSwapChain()
    {
        DisposeSwapChain();
        SwapChain = new VkSwapChain(_device, _window);
        DepthImage = new VkDepthImage(this);
        ColorImage = new VkColorImage(this);
        RenderPass = new VkRenderPass(this);
        GraphicsPipeline = new VkGraphicsPipeline(this);
        Commands = new VkCommands(_device);
        ImageView = new VkImageView(this);
        UniformBuffer = new VkUniformBuffer(this);
        DescriptorPool = new VkDescriptorPool(this);
        
        Commands.CreateCommandBuffers(this);
        _vkFrameDrawer = new VkFrameDrawer(_window, _device, this);
    }
    
    public void Dispose()
    {
        DisposeSwapChain();
        ImageView.Dispose();
        Model.Dispose();
    }
    
    private void DisposeSwapChain()
    {
        DepthImage.Dispose();
        ColorImage.Dispose();
        UniformBuffer.Dispose();
        _vkFrameDrawer.Dispose();
        Commands.Dispose();
        RenderPass.Dispose();
        GraphicsPipeline.Dispose();
        SwapChain.Dispose(); 
        DescriptorPool.Dispose();
    }
}