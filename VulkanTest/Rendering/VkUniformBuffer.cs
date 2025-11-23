using System.Runtime.CompilerServices;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace VulkanTest;

public class VkUniformBuffer : IDisposable
{
    public Buffer[] UniformBuffers;
    private DeviceMemory[]? _uniformBuffersMemory;
    
    public VkUniformBuffer(VkRender render)
    {
        CreateUniformBuffers(render);
    }

    private void CreateUniformBuffers(VkRender render)
    {
        var bufferSize = (ulong)Unsafe.SizeOf<UniformBufferObject>();

        var swapChainImagesLength = render.SwapChain.SwapChainImages!.Length;
        UniformBuffers = new Buffer[swapChainImagesLength];
        _uniformBuffersMemory = new DeviceMemory[swapChainImagesLength];

        for (int i = 0; i < swapChainImagesLength; i++)
            render.CommandBufferUtil.CreateBuffer(bufferSize,
                BufferUsageFlags.UniformBufferBit,
                MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit,
                ref UniformBuffers[i],
                ref _uniformBuffersMemory[i]);
    }
    
    public unsafe void UpdateUniformBuffer(uint currentImage, float time, VkRender render)
    {
        var extents = render.SwapChain.SwapChainExtent;
        
        UniformBufferObject ubo = new()
        {
            Model = Matrix4X4<float>.Identity * Matrix4X4.CreateFromAxisAngle(new Vector3D<float>(0, 0, 1), time * Scalar.DegreesToRadians(30.0f)),
            View = Matrix4X4.CreateLookAt(new Vector3D<float>(2, 2, 2), new Vector3D<float>(0, 0, 0), new Vector3D<float>(0, 0, 1)),
            Proj = Matrix4X4.CreatePerspectiveFieldOfView(Scalar.DegreesToRadians(45.0f), (float)extents.Width / extents.Height, 0.1f, 10.0f),
        };
        ubo.Proj.M22 *= -1;

        void* data;
        VkUtil.Vk.MapMemory(VkUtil.Device, _uniformBuffersMemory![currentImage], 0, (ulong)Unsafe.SizeOf<UniformBufferObject>(), 0, &data);
        new Span<UniformBufferObject>(data, 1)[0] = ubo;
        VkUtil.Vk.UnmapMemory(VkUtil.Device, _uniformBuffersMemory![currentImage]);
    }
    
    public unsafe void Dispose()
    {
        foreach (var buffer in UniformBuffers)
        {
            VkUtil.Vk.DestroyBuffer(VkUtil.Device, buffer, null);
        }
        
        foreach (var bufferMemory in _uniformBuffersMemory)
        {
            VkUtil.Vk.FreeMemory(VkUtil.Device, bufferMemory, null);
        }

    }
}