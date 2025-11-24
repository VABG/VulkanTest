using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Vulkan.Rendering.Assets;

public unsafe class VkTexture : IDisposable
{
    private DeviceMemory _textureImageMemory;
    public Image Image;
    public uint MipLevels { get; private set; }

    public VkTexture(VkRender render)
    {
        CreateTextureImage(render);
    }

    private void CreateTextureImage(VkRender render)
    {
        using var img =
            SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(@"Assets\viking_room.png");

        ulong imageSize = (ulong)(img.Width * img.Height * img.PixelType.BitsPerPixel / 8);
        MipLevels = (uint)(Math.Floor(Math.Log2(Math.Max(img.Width, img.Height))) + 1);

        Buffer stagingBuffer = default;
        DeviceMemory stagingBufferMemory = default;
        render.CommandBufferUtil.CreateBuffer(imageSize, BufferUsageFlags.TransferSrcBit,
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, ref stagingBuffer,
            ref stagingBufferMemory);

        void* data;
        VkUtil.Vk.MapMemory(VkUtil.Device, stagingBufferMemory, 0, imageSize, 0, &data);
        img.CopyPixelDataTo(new Span<byte>(data, (int)imageSize));
        VkUtil.Vk.UnmapMemory(VkUtil.Device, stagingBufferMemory);

        render.ImageUtil.CreateImage(
            (uint)img.Width,
            (uint)img.Height,
            MipLevels,
            SampleCountFlags.Count1Bit,
            Format.R8G8B8A8Srgb,
            ImageTiling.Optimal,
            ImageUsageFlags.TransferSrcBit | ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit,
            MemoryPropertyFlags.DeviceLocalBit,
            ref Image,
            ref _textureImageMemory);

        TransitionImageLayout(Image, render, Format.R8G8B8A8Srgb, ImageLayout.Undefined,
            ImageLayout.TransferDstOptimal, MipLevels);
        CopyBufferToImage(stagingBuffer, render, Image, (uint)img.Width, (uint)img.Height);
        //TransitionImageLayout(Image, Format.R8G8B8A8Srgb, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal);

        VkUtil.Vk.DestroyBuffer(VkUtil.Device, stagingBuffer, null);
        VkUtil.Vk.FreeMemory(VkUtil.Device, stagingBufferMemory, null);

        GenerateMipMaps(Image, render,Format.R8G8B8A8Srgb, (uint)img.Width, (uint)img.Height, MipLevels);
    }

    private void GenerateMipMaps(Image image, VkRender render, Format imageFormat, uint width, uint height,
        uint mipLevels)
    {
        VkUtil.Vk.GetPhysicalDeviceFormatProperties(VkUtil.PhysicalDevice, imageFormat,
            out var formatProperties);

        if ((formatProperties.OptimalTilingFeatures & FormatFeatureFlags.SampledImageFilterLinearBit) == 0)
        {
            throw new Exception("texture image format does not support linear blitting!");
        }

        var commandBuffer = render.CommandBufferUtil.BeginSingleTimeCommands(render.Commands.CommandPool);

        ImageMemoryBarrier barrier = new()
        {
            SType = StructureType.ImageMemoryBarrier,
            Image = image,
            SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
            DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
            SubresourceRange =
            {
                AspectMask = ImageAspectFlags.ColorBit,
                BaseArrayLayer = 0,
                LayerCount = 1,
                LevelCount = 1,
            }
        };

        var mipWidth = width;
        var mipHeight = height;

        for (uint i = 1; i < mipLevels; i++)
        {
            barrier.SubresourceRange.BaseMipLevel = i - 1;
            barrier.OldLayout = ImageLayout.TransferDstOptimal;
            barrier.NewLayout = ImageLayout.TransferSrcOptimal;
            barrier.SrcAccessMask = AccessFlags.TransferWriteBit;
            barrier.DstAccessMask = AccessFlags.TransferReadBit;

            VkUtil.Vk.CmdPipelineBarrier(commandBuffer,
                PipelineStageFlags.TransferBit,
                PipelineStageFlags.TransferBit,
                0,
                0, null,
                0, null,
                1, in barrier);

            ImageBlit blit = new()
            {
                SrcOffsets =
                {
                    Element0 = new Offset3D(0, 0, 0),
                    Element1 = new Offset3D((int)mipWidth, (int)mipHeight, 1),
                },
                SrcSubresource =
                {
                    AspectMask = ImageAspectFlags.ColorBit,
                    MipLevel = i - 1,
                    BaseArrayLayer = 0,
                    LayerCount = 1,
                },
                DstOffsets =
                {
                    Element0 = new Offset3D(0, 0, 0),
                    Element1 = new Offset3D((int)(mipWidth > 1 ? mipWidth / 2 : 1),
                        (int)(mipHeight > 1 ? mipHeight / 2 : 1), 1),
                },
                DstSubresource =
                {
                    AspectMask = ImageAspectFlags.ColorBit,
                    MipLevel = i,
                    BaseArrayLayer = 0,
                    LayerCount = 1,
                },
            };

            VkUtil.Vk.CmdBlitImage(commandBuffer,
                image, ImageLayout.TransferSrcOptimal,
                image, ImageLayout.TransferDstOptimal,
                1, in blit,
                Filter.Linear);

            barrier.OldLayout = ImageLayout.TransferSrcOptimal;
            barrier.NewLayout = ImageLayout.ShaderReadOnlyOptimal;
            barrier.SrcAccessMask = AccessFlags.TransferReadBit;
            barrier.DstAccessMask = AccessFlags.ShaderReadBit;

            VkUtil.Vk.CmdPipelineBarrier(commandBuffer, PipelineStageFlags.TransferBit,
                PipelineStageFlags.FragmentShaderBit, 0,
                0, null,
                0, null,
                1, in barrier);

            if (mipWidth > 1) mipWidth /= 2;
            if (mipHeight > 1) mipHeight /= 2;
        }

        barrier.SubresourceRange.BaseMipLevel = mipLevels - 1;
        barrier.OldLayout = ImageLayout.TransferDstOptimal;
        barrier.NewLayout = ImageLayout.ShaderReadOnlyOptimal;
        barrier.SrcAccessMask = AccessFlags.TransferWriteBit;
        barrier.DstAccessMask = AccessFlags.ShaderReadBit;

        VkUtil.Vk.CmdPipelineBarrier(commandBuffer, PipelineStageFlags.TransferBit,
            PipelineStageFlags.FragmentShaderBit, 0,
            0, null,
            0, null,
            1, in barrier);

        render.CommandBufferUtil.EndSingleTimeCommands(commandBuffer, render.Commands.CommandPool);
    }

    private void TransitionImageLayout(Image image, VkRender render, Format format, ImageLayout oldLayout,
        ImageLayout newLayout, uint mipLevels)
    {
        CommandBuffer commandBuffer = render.CommandBufferUtil.BeginSingleTimeCommands(render.Commands.CommandPool);

        ImageMemoryBarrier barrier = new()
        {
            SType = StructureType.ImageMemoryBarrier,
            OldLayout = oldLayout,
            NewLayout = newLayout,
            SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
            DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
            Image = image,
            SubresourceRange =
            {
                AspectMask = ImageAspectFlags.ColorBit,
                BaseMipLevel = 0,
                LevelCount = mipLevels,
                BaseArrayLayer = 0,
                LayerCount = 1,
            }
        };

        PipelineStageFlags sourceStage;
        PipelineStageFlags destinationStage;

        if (oldLayout == ImageLayout.Undefined && newLayout == ImageLayout.TransferDstOptimal)
        {
            barrier.SrcAccessMask = 0;
            barrier.DstAccessMask = AccessFlags.TransferWriteBit;

            sourceStage = PipelineStageFlags.TopOfPipeBit;
            destinationStage = PipelineStageFlags.TransferBit;
        }
        else if (oldLayout == ImageLayout.TransferDstOptimal && newLayout == ImageLayout.ShaderReadOnlyOptimal)
        {
            barrier.SrcAccessMask = AccessFlags.TransferWriteBit;
            barrier.DstAccessMask = AccessFlags.ShaderReadBit;

            sourceStage = PipelineStageFlags.TransferBit;
            destinationStage = PipelineStageFlags.FragmentShaderBit;
        }
        else
        {
            throw new Exception("unsupported layout transition!");
        }

        VkUtil.Vk.CmdPipelineBarrier(commandBuffer,
            sourceStage,
            destinationStage,
            0,
            0,
            null,
            0,
            null,
            1,
            in barrier);

        render.CommandBufferUtil.EndSingleTimeCommands(commandBuffer, render.Commands.CommandPool);
    }

    private void CopyBufferToImage(Buffer buffer, VkRender render, Image image, uint width, uint height)
    {
        CommandBuffer commandBuffer = render.CommandBufferUtil.BeginSingleTimeCommands(render.Commands.CommandPool);

        BufferImageCopy region = new()
        {
            BufferOffset = 0,
            BufferRowLength = 0,
            BufferImageHeight = 0,
            ImageSubresource =
            {
                AspectMask = ImageAspectFlags.ColorBit,
                MipLevel = 0,
                BaseArrayLayer = 0,
                LayerCount = 1,
            },
            ImageOffset = new Offset3D(0, 0, 0),
            ImageExtent = new Extent3D(width, height, 1),
        };

        VkUtil.Vk.CmdCopyBufferToImage(commandBuffer,
            buffer,
            image,
            ImageLayout.TransferDstOptimal,
            1,
            in region);

        render.CommandBufferUtil.EndSingleTimeCommands(commandBuffer, render.Commands.CommandPool);
    }

    public void Dispose()
    {
        VkUtil.Vk.DestroyImage(VkUtil.Device, Image, null);
        VkUtil.Vk.FreeMemory(VkUtil.Device, _textureImageMemory, null);
    }
}