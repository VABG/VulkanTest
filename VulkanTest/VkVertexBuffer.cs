using System.Runtime.CompilerServices;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace VulkanTest;

public unsafe class VkVertexBuffer : IDisposable
{
    private readonly VkInstance _instance;
    private readonly Model _model;

    //TODO: Generalize buffer (at least data and creation)
    public Buffer VertexBuffer;
    private DeviceMemory _vertexBufferMemory;

    public Buffer IndexBuffer;
    private DeviceMemory _indexBufferMemory;

    public VkVertexBuffer(VkInstance instance, Model model)
    {
        _instance = instance;
        _model = model;
        CreateVertexBuffer();
        CreateIndexBuffer();
    }

    public uint GetIndicesCount() => (uint)_model.Indices.Length;

    private void CreateVertexBuffer()
    {
        var vertices = _model.Vertices;

        ulong bufferSize = (ulong)(Unsafe.SizeOf<Vertex>() * vertices.Length);

        Buffer stagingBuffer = default;
        DeviceMemory stagingBufferMemory = default;
        _instance.CommandBufferUtil.CreateBuffer(bufferSize,
            BufferUsageFlags.TransferSrcBit,
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit,
            ref stagingBuffer,
            ref stagingBufferMemory);

        
        void* data;
        _instance.Vk.MapMemory(_instance.Device.Device, stagingBufferMemory, 0, bufferSize, 0, &data);
        vertices.AsSpan().CopyTo(new Span<Vertex>(data, vertices.Length));
        _instance.Vk.UnmapMemory(_instance.Device.Device, stagingBufferMemory);

        _instance.CommandBufferUtil.CreateBuffer(bufferSize,
            BufferUsageFlags.TransferDstBit | BufferUsageFlags.VertexBufferBit,
            MemoryPropertyFlags.DeviceLocalBit,
            ref VertexBuffer,
            ref _vertexBufferMemory);

        _instance.CommandBufferUtil.CopyBuffer(stagingBuffer, VertexBuffer, bufferSize);

        _instance.Vk.DestroyBuffer(_instance.Device.Device, stagingBuffer, null);
        _instance.Vk.FreeMemory(_instance.Device.Device, stagingBufferMemory, null);
    }

    private void CreateIndexBuffer()
    {
        var indices = _model.Indices;
        ulong bufferSize = (ulong)(Unsafe.SizeOf<uint>() * indices.Length);

        Buffer stagingBuffer = default;
        DeviceMemory stagingBufferMemory = default;
        _instance.CommandBufferUtil.CreateBuffer(bufferSize,
            BufferUsageFlags.TransferSrcBit,
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit,
            ref stagingBuffer,
            ref stagingBufferMemory);

        void* data;
        _instance.Vk.MapMemory(_instance.Device.Device, stagingBufferMemory, 0, bufferSize, 0, &data);
        indices.AsSpan().CopyTo(new Span<uint>(data, indices.Length));
        _instance.Vk.UnmapMemory(_instance.Device.Device, stagingBufferMemory);

        _instance.CommandBufferUtil.CreateBuffer(bufferSize,
            BufferUsageFlags.TransferDstBit | BufferUsageFlags.IndexBufferBit,
            MemoryPropertyFlags.DeviceLocalBit,
            ref IndexBuffer,
            ref _indexBufferMemory);

        _instance.CommandBufferUtil.CopyBuffer(stagingBuffer, IndexBuffer, bufferSize);

        _instance.Vk.DestroyBuffer(_instance.Device.Device, stagingBuffer, null);
        _instance.Vk.FreeMemory(_instance.Device.Device, stagingBufferMemory, null);
    }

    public void Dispose()
    {
        _instance.Vk.DestroyBuffer(_instance.Device.Device, VertexBuffer, null);
        _instance.Vk.FreeMemory(_instance.Device.Device, _vertexBufferMemory, null);
        _instance.Vk.DestroyBuffer(_instance.Device.Device, IndexBuffer, null);
        _instance.Vk.FreeMemory(_instance.Device.Device, _indexBufferMemory, null);
    }
}