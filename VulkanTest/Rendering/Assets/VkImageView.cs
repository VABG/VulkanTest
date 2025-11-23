using Silk.NET.Vulkan;

namespace VulkanTest.Rendering.Assets;

public class VkImageView : IDisposable
{
    public ImageView TextureImageView { get; private set; }
    public Sampler TextureSampler;
    public readonly VkTexture Texture; 

    public VkImageView(VkRender render)
    {
        Texture = new VkTexture(render);
        TextureImageView = render.ImageUtil.CreateImageView(Texture.Image, Format.R8G8B8A8Srgb,  ImageAspectFlags.ColorBit, Texture.MipLevels);
        CreateTextureSampler();
    }
    
    private unsafe void CreateTextureSampler()
    {
        VkUtil.Vk.GetPhysicalDeviceProperties(VkUtil.PhysicalDevice, out PhysicalDeviceProperties properties);

        SamplerCreateInfo samplerInfo = new()
        {
            SType = StructureType.SamplerCreateInfo,
            MagFilter = Filter.Linear,
            MinFilter = Filter.Linear,
            AddressModeU = SamplerAddressMode.Repeat,
            AddressModeV = SamplerAddressMode.Repeat,
            AddressModeW = SamplerAddressMode.Repeat,
            AnisotropyEnable = true,
            MaxAnisotropy = properties.Limits.MaxSamplerAnisotropy,
            BorderColor = BorderColor.IntOpaqueBlack,
            UnnormalizedCoordinates = false,
            CompareEnable = false,
            CompareOp = CompareOp.Always,
            MipmapMode = SamplerMipmapMode.Linear,
            MinLod = 0,
            MaxLod = Texture.MipLevels,
            MipLodBias = 0,
        };

        fixed (Sampler* textureSamplerPtr = &TextureSampler)
        {
            if (VkUtil.Vk.CreateSampler(VkUtil.Device, in samplerInfo, null, textureSamplerPtr) != Result.Success)
            {
                throw new Exception("failed to create texture sampler!");
            }
        }
    }

    public unsafe void Dispose()
    {
        Texture.Dispose();
        VkUtil.Vk.DestroySampler(VkUtil.Device, TextureSampler, null);
        VkUtil.Vk.DestroyImageView(VkUtil.Device, TextureImageView, null);
    }
}