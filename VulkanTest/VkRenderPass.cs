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
        
        AttachmentDescription depthAttachment = new()
        {
            Format = _instance.DepthFormatUtil.FindDepthFormat(),
            Samples = SampleCountFlags.Count1Bit,
            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.DontCare,
            StencilLoadOp = AttachmentLoadOp.DontCare,
            StencilStoreOp = AttachmentStoreOp.DontCare,
            InitialLayout = ImageLayout.Undefined,
            FinalLayout = ImageLayout.DepthStencilAttachmentOptimal,
        };

        AttachmentReference colorAttachmentRef = new()
        {
            Attachment = 0,
            Layout = ImageLayout.ColorAttachmentOptimal,
        };
        
        AttachmentReference depthAttachmentRef = new()
        {
            Attachment = 1,
            Layout = ImageLayout.DepthStencilAttachmentOptimal,
        };

        SubpassDescription subpass = new()
        {
            PipelineBindPoint = PipelineBindPoint.Graphics,
            ColorAttachmentCount = 1,
            PColorAttachments = &colorAttachmentRef,
            PDepthStencilAttachment = &depthAttachmentRef,
        };
        
        SubpassDependency dependency = new()
        {
            SrcSubpass = Vk.SubpassExternal,
            DstSubpass = 0,
            SrcStageMask = PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.EarlyFragmentTestsBit,
            SrcAccessMask = 0,
            DstStageMask = PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.EarlyFragmentTestsBit,
            DstAccessMask = AccessFlags.ColorAttachmentWriteBit | AccessFlags.DepthStencilAttachmentWriteBit
        };
        
        var attachments = new[] { colorAttachment, depthAttachment };
        
        fixed (AttachmentDescription* attachmentsPtr = attachments)
        {
            RenderPassCreateInfo renderPassInfo = new()
            {
                SType = StructureType.RenderPassCreateInfo,
                AttachmentCount = (uint)attachments.Length,
                PAttachments = attachmentsPtr,
                SubpassCount = 1,
                PSubpasses = &subpass,
                DependencyCount = 1,
                PDependencies = &dependency,
            };
            
            if (instance.Vk.CreateRenderPass(instance.Device.Device, in renderPassInfo, null, out var renderPass) !=
                Result.Success)
            {
                throw new Exception("failed to create render pass!");
            }

            RenderPass = renderPass;
        }
    }


    public unsafe void Dispose()
    {
        _instance.Vk.DestroyRenderPass(_instance.Device.Device, RenderPass, null);
    }
}