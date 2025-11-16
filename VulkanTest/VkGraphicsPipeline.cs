using Silk.NET.Core.Native;
using Silk.NET.Vulkan;

namespace VulkanTest;

public unsafe class VkGraphicsPipeline : IDisposable
{
    private readonly VkInstance _instance;
    private PipelineLayout _pipelineLayout;
    public RenderPass RenderPass { get; private set; }
    public Pipeline Pipeline { get; private set; }
    public Framebuffer[]? SwapChainFramebuffers { get; private set; }
    public VkGraphicsPipeline(VkInstance instance)
    {
        _instance = instance;
        CreateRenderPass(instance);
        CreateGraphicsPipeline(instance);
        CreateFramebuffers(instance);
    }
    
    private void CreateRenderPass(VkInstance instance)
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

        if (instance.Vk.CreateRenderPass(instance.Device.Device, in renderPassInfo, null, out var renderPass) != Result.Success)
        {
            throw new Exception("failed to create render pass!");
        }

        RenderPass = renderPass;
    }
    
    
    private void CreateGraphicsPipeline(VkInstance instance)
    {
        var vertShaderCode = File.ReadAllBytes("Shaders/vert.spv");
        var fragShaderCode = File.ReadAllBytes("Shaders/frag.spv");

        var vertShaderModule = CreateShaderModule(vertShaderCode, instance);
        var fragShaderModule = CreateShaderModule(fragShaderCode, instance);

        PipelineShaderStageCreateInfo vertShaderStageInfo = new()
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderStageFlags.VertexBit,
            Module = vertShaderModule,
            PName = (byte*)SilkMarshal.StringToPtr("main")
        };

        PipelineShaderStageCreateInfo fragShaderStageInfo = new()
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderStageFlags.FragmentBit,
            Module = fragShaderModule,
            PName = (byte*)SilkMarshal.StringToPtr("main")
        };

        var shaderStages = stackalloc[]
        {
            vertShaderStageInfo,
            fragShaderStageInfo
        };
        
        PipelineVertexInputStateCreateInfo vertexInputInfo = new()
        {
            SType = StructureType.PipelineVertexInputStateCreateInfo,
            VertexBindingDescriptionCount = 0,
            VertexAttributeDescriptionCount = 0,
        };

        PipelineInputAssemblyStateCreateInfo inputAssembly = new()
        {
            SType = StructureType.PipelineInputAssemblyStateCreateInfo,
            Topology = PrimitiveTopology.TriangleList,
            PrimitiveRestartEnable = false,
        };
        
        Viewport viewport = new()
        {
            X = 0,
            Y = 0,
            Width = instance.SwapChain.SwapChainExtent.Width,
            Height = instance.SwapChain.SwapChainExtent.Height,
            MinDepth = 0,
            MaxDepth = 1,
        };

        Rect2D scissor = new()
        {
            Offset = { X = 0, Y = 0 },
            Extent = instance.SwapChain.SwapChainExtent,
        };

        PipelineViewportStateCreateInfo viewportState = new()
        {
            SType = StructureType.PipelineViewportStateCreateInfo,
            ViewportCount = 1,
            PViewports = &viewport,
            ScissorCount = 1,
            PScissors = &scissor,
        };

        PipelineRasterizationStateCreateInfo rasterizer = new()
        {
            SType = StructureType.PipelineRasterizationStateCreateInfo,
            DepthClampEnable = false,
            RasterizerDiscardEnable = false,
            PolygonMode = PolygonMode.Fill,
            LineWidth = 1,
            CullMode = CullModeFlags.BackBit,
            FrontFace = FrontFace.Clockwise,
            DepthBiasEnable = false,
        };

        PipelineMultisampleStateCreateInfo multisampling = new()
        {
            SType = StructureType.PipelineMultisampleStateCreateInfo,
            SampleShadingEnable = false,
            RasterizationSamples = SampleCountFlags.Count1Bit,
        };

        PipelineColorBlendAttachmentState colorBlendAttachment = new()
        {
            ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit | ColorComponentFlags.ABit,
            BlendEnable = false,
        };

        PipelineColorBlendStateCreateInfo colorBlending = new()
        {
            SType = StructureType.PipelineColorBlendStateCreateInfo,
            LogicOpEnable = false,
            LogicOp = LogicOp.Copy,
            AttachmentCount = 1,
            PAttachments = &colorBlendAttachment,
        };
        
        colorBlending.BlendConstants[0] = 0;
        colorBlending.BlendConstants[1] = 0;
        colorBlending.BlendConstants[2] = 0;
        colorBlending.BlendConstants[3] = 0;

        PipelineLayoutCreateInfo pipelineLayoutInfo = new()
        {
            SType = StructureType.PipelineLayoutCreateInfo,
            SetLayoutCount = 0,
            PushConstantRangeCount = 0,
        };

        if (instance.Vk.CreatePipelineLayout(instance.Device.Device, in pipelineLayoutInfo, null, out _pipelineLayout) != Result.Success)
        {
            throw new Exception("failed to create pipeline layout!");
        }
        
        GraphicsPipelineCreateInfo pipelineInfo = new()
        {
            SType = StructureType.GraphicsPipelineCreateInfo,
            StageCount = 2,
            PStages = shaderStages,
            PVertexInputState = &vertexInputInfo,
            PInputAssemblyState = &inputAssembly,
            PViewportState = &viewportState,
            PRasterizationState = &rasterizer,
            PMultisampleState = &multisampling,
            PColorBlendState = &colorBlending,
            Layout = _pipelineLayout,
            RenderPass = RenderPass,
            Subpass = 0,
            BasePipelineHandle = default
        };

        if (instance.Vk.CreateGraphicsPipelines(instance.Device.Device, default, 1, in pipelineInfo, null, out var pipeline) != Result.Success)
        {
            throw new Exception("failed to create graphics pipeline!");
        }

        Pipeline = pipeline;

        instance.Vk.DestroyShaderModule(instance.Device.Device, fragShaderModule, null);
        instance.Vk.DestroyShaderModule(instance.Device.Device, vertShaderModule, null);

        SilkMarshal.Free((nint)vertShaderStageInfo.PName);
        SilkMarshal.Free((nint)fragShaderStageInfo.PName);
    }

    private ShaderModule CreateShaderModule(byte[] code, VkInstance instance)
    {
        ShaderModuleCreateInfo createInfo = new()
        {
            SType = StructureType.ShaderModuleCreateInfo,
            CodeSize = (nuint)code.Length,
        };

        ShaderModule shaderModule;

        fixed (byte* codePtr = code)
        {
            createInfo.PCode = (uint*)codePtr;

            if (instance.Vk.CreateShaderModule(instance.Device.Device, in createInfo, null, out shaderModule) != Result.Success)
            {
                throw new Exception();
            }
        }

        return shaderModule;
    }

    private void CreateFramebuffers(VkInstance instance)
    {
        var imageViews = instance.SwapChain.SwapChainImageViews;
        var imageViewsLength = imageViews!.Length;
        var swapChainExtent = instance.SwapChain.SwapChainExtent;
        SwapChainFramebuffers = new Framebuffer[imageViewsLength];

        for (int i = 0; i < imageViewsLength; i++)
        {
            var attachment = imageViews[i];

            FramebufferCreateInfo framebufferInfo = new()
            {
                SType = StructureType.FramebufferCreateInfo,
                RenderPass = RenderPass,
                AttachmentCount = 1,
                PAttachments = &attachment,
                Width = swapChainExtent.Width,
                Height = swapChainExtent.Height,
                Layers = 1,
            };

            if (instance.Vk.CreateFramebuffer(instance.Device.Device, in framebufferInfo, null, out SwapChainFramebuffers[i]) != Result.Success)
            {
                throw new Exception("failed to create framebuffer!");
            }
        }
    }

    public void Dispose()
    {
        foreach (var framebuffer in SwapChainFramebuffers!)
            _instance.Vk.DestroyFramebuffer(_instance.Device.Device, framebuffer, null);
        
        _instance.Vk.DestroyPipeline(_instance.Device.Device, Pipeline, null);
        _instance.Vk.DestroyPipelineLayout(_instance.Device.Device, _pipelineLayout, null);
        _instance.Vk.DestroyRenderPass(_instance.Device.Device, RenderPass, null);
    }
}