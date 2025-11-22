using Silk.NET.Vulkan;

namespace VulkanTest;

public class VkImageView : IDisposable
{
    private readonly VkInstance _instance;
    public ImageView TextureImageView { get; private set; }
    public Sampler TextureSampler;
    public readonly VkTexture Texture; 

    public VkImageView(VkInstance instance)
    {
        _instance = instance;
        Texture = new VkTexture(instance);
        TextureImageView = _instance.ImageUtil.CreateImageView(Texture.Image, Format.R8G8B8A8Srgb,  ImageAspectFlags.ColorBit, Texture.MipLevels);
        CreateTextureSampler();
    }
    
    private unsafe void CreateTextureSampler()
    {
        _instance.Vk.GetPhysicalDeviceProperties(_instance.Device.PhysicalDevice, out PhysicalDeviceProperties properties);

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
            if (_instance.Vk.CreateSampler(_instance.Device.Device, in samplerInfo, null, textureSamplerPtr) != Result.Success)
            {
                throw new Exception("failed to create texture sampler!");
            }
        }
    }

    public unsafe void Dispose()
    {
        Texture.Dispose();
        _instance.Vk.DestroySampler(_instance.Device.Device, TextureSampler, null);
        _instance.Vk.DestroyImageView(_instance.Device.Device, TextureImageView, null);
    }
}