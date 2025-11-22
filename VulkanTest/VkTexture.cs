using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace VulkanTest;

public unsafe class VkTexture : IDisposable
{
    private readonly VkInstance _instance;
    private DeviceMemory _textureImageMemory;
    public Image Image;
    
    public VkTexture(VkInstance instance)
    {
        _instance = instance;
        CreateTextureImage();
    }
    
    private void CreateTextureImage()
    {
        using var img = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>("textures/texture.jpg");

        ulong imageSize = (ulong)(img.Width * img.Height * img.PixelType.BitsPerPixel / 8);

        Buffer stagingBuffer = default;
        DeviceMemory stagingBufferMemory = default;
        _instance.CommandBufferUtil.CreateBuffer(imageSize, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, ref stagingBuffer, ref stagingBufferMemory);

        void* data;
        _instance.Vk.MapMemory(_instance.Device.Device, stagingBufferMemory, 0, imageSize, 0, &data);
        img.CopyPixelDataTo(new Span<byte>(data, (int)imageSize));
        _instance.Vk.UnmapMemory(_instance.Device.Device, stagingBufferMemory);

        _instance.ImageUtil.CreateImage((uint)img.Width,
            (uint)img.Height,
            Format.R8G8B8A8Srgb,
            ImageTiling.Optimal,
            ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit,
            MemoryPropertyFlags.DeviceLocalBit,
            ref Image,
            ref _textureImageMemory);

        TransitionImageLayout(Image, Format.R8G8B8A8Srgb, ImageLayout.Undefined, ImageLayout.TransferDstOptimal);
        CopyBufferToImage(stagingBuffer, Image, (uint)img.Width, (uint)img.Height);
        TransitionImageLayout(Image, Format.R8G8B8A8Srgb, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal);

        _instance.Vk.DestroyBuffer(_instance.Device.Device, stagingBuffer, null);
        _instance.Vk.FreeMemory(_instance.Device.Device, stagingBufferMemory, null);
    }
    
     

    private void TransitionImageLayout(Image image, Format format, ImageLayout oldLayout, ImageLayout newLayout)
    {
        CommandBuffer commandBuffer = _instance.CommandBufferUtil.BeginSingleTimeCommands();

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
                LevelCount = 1,
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

        _instance.Vk.CmdPipelineBarrier(commandBuffer,
            sourceStage,
            destinationStage,
            0,
            0,
            null,
            0,
            null,
            1,
            in barrier);

        _instance.CommandBufferUtil.EndSingleTimeCommands(commandBuffer);
    }

    private void CopyBufferToImage(Buffer buffer, Image image, uint width, uint height)
    {
        CommandBuffer commandBuffer = _instance.CommandBufferUtil.BeginSingleTimeCommands();

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

        _instance.Vk.CmdCopyBufferToImage(commandBuffer,
            buffer,
            image,
            ImageLayout.TransferDstOptimal,
            1,
            in region);

        _instance.CommandBufferUtil.EndSingleTimeCommands(commandBuffer);
    }
    
    public void Dispose()
    {
        _instance.Vk.DestroyImage(_instance.Device.Device, Image, null);
        _instance.Vk.FreeMemory(_instance.Device.Device, _textureImageMemory, null);
    }
}