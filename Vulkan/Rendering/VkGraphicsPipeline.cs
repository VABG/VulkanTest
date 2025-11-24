using Silk.NET.Vulkan;
using Vulkan.Data;
using Vulkan.MeshData;
using Vulkan.Shaders;

namespace Vulkan.Rendering;

public unsafe class VkGraphicsPipeline : IDisposable
{
    public PipelineLayout PipelineLayout { get; private set; }
    public Pipeline Pipeline { get; private set; }
    public Framebuffer[]? SwapChainFramebuffers { get; private set; }
    public VkDescriptorSetLayout DescriptorSetLayout { get; private set; }

    public VkGraphicsPipeline(VkRender render)
    {
        DescriptorSetLayout = new VkDescriptorSetLayout();
        CreateGraphicsPipeline(render);
        CreateFramebuffers(render);
    }

    private void CreateGraphicsPipeline(VkRender render)
    {
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
            Width = render.SwapChain.SwapChainExtent.Width,
            Height = render.SwapChain.SwapChainExtent.Height,
            MinDepth = 0,
            MaxDepth = 1,
        };

        Rect2D scissor = new()
        {
            Offset = { X = 0, Y = 0 },
            Extent = render.SwapChain.SwapChainExtent,
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
            FrontFace = FrontFace.CounterClockwise,
            DepthBiasEnable = false,
        };

        PipelineMultisampleStateCreateInfo multisampling = new()
        {
            SType = StructureType.PipelineMultisampleStateCreateInfo,
            SampleShadingEnable = false,
            RasterizationSamples = render.MsaaSamples,
        };

        PipelineDepthStencilStateCreateInfo depthStencil = new()
        {
            SType = StructureType.PipelineDepthStencilStateCreateInfo,
            DepthTestEnable = true,
            DepthWriteEnable = true,
            DepthCompareOp = CompareOp.Less,
            DepthBoundsTestEnable = false,
            StencilTestEnable = false,
        };

        PipelineColorBlendAttachmentState colorBlendAttachment = new()
        {
            ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit |
                             ColorComponentFlags.ABit,
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
        fixed (DescriptorSetLayout* descriptorSetLayoutPtr = &DescriptorSetLayout.DescriptorSetLayout)
        {
            PipelineLayoutCreateInfo pipelineLayoutInfo = new()
            {
                SType = StructureType.PipelineLayoutCreateInfo,
                PushConstantRangeCount = 0,
                SetLayoutCount = 1,
                PSetLayouts = descriptorSetLayoutPtr
            };

            if (VkUtil.Vk.CreatePipelineLayout(VkUtil.Device, in pipelineLayoutInfo, null,
                    out var pipelineLayout) != Result.Success)
            {
                throw new Exception("failed to create pipeline layout!");
            }

            PipelineLayout = pipelineLayout;

            var bindingDescription = Vertex.GetBindingDescription();
            var attributeDescriptions = Vertex.GetAttributeDescriptions();
            
            using var vertexShader = render.Shaders.GetShader("Assets/Shaders/shader_vert.spv", ShaderType.Vertex);
            using var pixelShader = render.Shaders.GetShader("Assets/Shaders/shader_frag.spv", ShaderType.Fragment);

            var shaderStages = 
                stackalloc PipelineShaderStageCreateInfo[]{vertexShader.PipelineShaderStageCreateInfo, pixelShader.PipelineShaderStageCreateInfo};
            
            fixed (VertexInputAttributeDescription* attributeDescriptionsPtr = attributeDescriptions)
            {
                PipelineVertexInputStateCreateInfo vertexInputInfo = new()
                {
                    SType = StructureType.PipelineVertexInputStateCreateInfo,
                    VertexBindingDescriptionCount = 1,
                    VertexAttributeDescriptionCount = (uint)attributeDescriptions.Length,
                    PVertexBindingDescriptions = &bindingDescription,
                    PVertexAttributeDescriptions = attributeDescriptionsPtr,
                };
                
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
                    PDepthStencilState = &depthStencil,
                    PColorBlendState = &colorBlending,
                    Layout = PipelineLayout,
                    RenderPass = render.RenderPass.RenderPass,
                    Subpass = 0,
                    BasePipelineHandle = default
                };

                if (VkUtil.Vk.CreateGraphicsPipelines(
                        VkUtil.Device,
                        default,
                        1,
                        in pipelineInfo,
                        null,
                        out var pipeline) != Result.Success)
                {
                    throw new Exception("failed to create graphics pipeline!");
                }

                Pipeline = pipeline;
            }
        }
    }

    private void CreateFramebuffers(VkRender render)
    {
        var imageViews = render.SwapChain.SwapChainImageViews;
        var imageViewsLength = imageViews!.Length;
        var swapChainExtent = render.SwapChain.SwapChainExtent;
        SwapChainFramebuffers = new Framebuffer[imageViewsLength];

        for (int i = 0; i < imageViewsLength; i++)
        {
            var attachments = new[]
                { render.ColorImage.ColorImageView, render.DepthImage.DepthImageView, imageViews[i], };
            fixed (ImageView* attachmentsPtr = attachments)
            {
                FramebufferCreateInfo framebufferInfo = new()
                {
                    SType = StructureType.FramebufferCreateInfo,
                    RenderPass = render.RenderPass.RenderPass,
                    AttachmentCount = (uint)attachments.Length,
                    PAttachments = attachmentsPtr,
                    Width = swapChainExtent.Width,
                    Height = swapChainExtent.Height,
                    Layers = 1,
                };

                if (VkUtil.Vk.CreateFramebuffer(VkUtil.Device, in framebufferInfo, null,
                        out SwapChainFramebuffers[i]) != Result.Success)
                {
                    throw new Exception("failed to create framebuffer!");
                }
            }
        }
    }

    public void Dispose()
    {
        DescriptorSetLayout.Dispose();

        foreach (var framebuffer in SwapChainFramebuffers!)
            VkUtil.Vk.DestroyFramebuffer(VkUtil.Device, framebuffer, null);

        VkUtil.Vk.DestroyPipeline(VkUtil.Device, Pipeline, null);
        VkUtil.Vk.DestroyPipelineLayout(VkUtil.Device, PipelineLayout, null);
    }
}