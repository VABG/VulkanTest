using Silk.NET.Vulkan;

namespace VulkanTest;

public class VkRenderPass : IDisposable
{
    private readonly VkInstance _instance;
    public RenderPass RenderPass { get; private set; }
    
    public VkRenderPass(VkInstance instance)
    {
        _instance = instance;
        CreateRenderPass(instance);
    }
    
    private unsafe void CreateRenderPass(VkInstance instance)
    {
        AttachmentDescription colorAttachment = new()
        {
            Format = instance.SwapChain.SwapChainImageFormat,
            Samples = SampleCountFlags.Count1Bit,
            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.Store,
            StencilLoadOp = AttachmentLoadOp.DontCare,
            InitialLayout = ImageLayout.Undefined,
            FinalLayout = ImageLayout.PresentSrcKhr,
        };

        AttachmentReference colorAttachmentRef = new()
        {
            Attachment = 0,
            Layout = ImageLayout.ColorAttachmentOptimal,
        };

        SubpassDescription subpass = new()
        {
            PipelineBindPoint = PipelineBindPoint.Graphics,
            ColorAttachmentCount = 1,
            PColorAttachments = &colorAttachmentRef,
        };

        RenderPassCreateInfo renderPassInfo = new()
        {
            SType = StructureType.RenderPassCreateInfo,
            AttachmentCount = 1,
            PAttachments = &colorAttachment,
            SubpassCount = 1,
            PSubpasses = &subpass,
        };

        if (instance.Vk.CreateRenderPass(instance.Device.Device, in renderPassInfo, null, out var renderPass) !=
            Result.Success)
        {
            throw new Exception("failed to create render pass!");
        }

        RenderPass = renderPass;
    }


    public unsafe void Dispose()
    {
        _instance.Vk.DestroyRenderPass(_instance.Device.Device, RenderPass, null);
    }
}