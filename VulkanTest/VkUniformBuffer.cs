using System.Runtime.CompilerServices;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace VulkanTest;

public class VkUniformBuffer : IDisposable
{
    private readonly VkInstance _instance;
    public Buffer[] UniformBuffers;
    private DeviceMemory[]? _uniformBuffersMemory;
    
    public VkUniformBuffer(VkInstance instance)
    {
        _instance = instance;
        CreateUniformBuffers();
    }

    private void CreateUniformBuffers()
    {
        var bufferSize = (ulong)Unsafe.SizeOf<UniformBufferObject>();

        var swapChainImagesLength = _instance.SwapChain.SwapChainImages!.Length;
        UniformBuffers = new Buffer[swapChainImagesLength];
        _uniformBuffersMemory = new DeviceMemory[swapChainImagesLength];

        for (int i = 0; i < swapChainImagesLength; i++)
            _instance.CommandBufferUtil.CreateBuffer(bufferSize,
                BufferUsageFlags.UniformBufferBit,
                MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit,
                ref UniformBuffers[i],
                ref _uniformBuffersMemory[i]);
    }

    public unsafe void Dispose()
    {
        for (int i = 0; i < _instance.SwapChain.SwapChainImages!.Length; i++)
        {
            _instance.Vk.DestroyBuffer(_instance.Device.Device, UniformBuffers[i], null);
            _instance.Vk.FreeMemory(_instance.Device.Device, _uniformBuffersMemory![i], null);
        }
    }
    
    public unsafe void UpdateUniformBuffer(uint currentImage)
    {
        //Silk Window has timing information so we are skipping the time code.
        var time = (float)_instance.Window.Window.Time;

        var extents = _instance.SwapChain.SwapChainExtent;
        
        UniformBufferObject ubo = new()
        {
            Model = Matrix4X4<float>.Identity * Matrix4X4.CreateFromAxisAngle(new Vector3D<float>(0, 0, 1), time * Scalar.DegreesToRadians(30.0f)),
            View = Matrix4X4.CreateLookAt(new Vector3D<float>(2, 2, 2), new Vector3D<float>(0, 0, 0), new Vector3D<float>(0, 0, 1)),
            Proj = Matrix4X4.CreatePerspectiveFieldOfView(Scalar.DegreesToRadians(45.0f), (float)extents.Width / extents.Height, 0.1f, 10.0f),
        };
        ubo.Proj.M22 *= -1;

        void* data;
        _instance.Vk.MapMemory(_instance.Device.Device, _uniformBuffersMemory![currentImage], 0, (ulong)Unsafe.SizeOf<UniformBufferObject>(), 0, &data);
        new Span<UniformBufferObject>(data, 1)[0] = ubo;
        _instance.Vk.UnmapMemory(_instance.Device.Device, _uniformBuffersMemory![currentImage]);
    }
}