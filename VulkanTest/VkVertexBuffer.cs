using System.Runtime.CompilerServices;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace VulkanTest;

public unsafe class VkVertexBuffer : IDisposable
{
    private readonly VkInstance _instance;
 
    //TODO: Generalize buffer (at least data and creation)
    public Buffer VertexBuffer;
    private DeviceMemory _vertexBufferMemory;

    public Buffer IndexBuffer;
    private DeviceMemory _indexBufferMemory;

    public VkVertexBuffer(VkInstance instance)
    {
        _instance = instance;
        CreateVertexBuffer();
        CreateIndexBuffer();
    }

    public uint GetIndicesCount() => (uint)_indices.Length;

    private Vertex[] _vertices =
    [
        new() { Pos = new Vector2D<float>(-0.5f,-0.5f), 
            Color = new Vector3D<float>(1.0f, 0.0f, 0.0f), 
            TexCoord = new Vector2D<float>(1.0f, 0.0f) },
        new() { Pos = new Vector2D<float>(0.5f,-0.5f), 
            Color = new Vector3D<float>(0.0f, 1.0f, 0.0f), 
            TexCoord = new Vector2D<float>(0.0f, 0.0f) },
        new() { Pos = new Vector2D<float>(0.5f,0.5f), 
            Color = new Vector3D<float>(0.0f, 0.0f, 1.0f), 
            TexCoord = new Vector2D<float>(0.0f, 1.0f) },
        new() { Pos = new Vector2D<float>(-0.5f,0.5f), 
            Color = new Vector3D<float>(1.0f, 1.0f, 1.0f), 
            TexCoord = new Vector2D<float>(1.0f, 1.0f) },
    ];
    
    private ushort[] _indices =
    [
        0, 2, 1, 3, 2, 0
    ];

    private void CreateVertexBuffer()
    {
        ulong bufferSize = (ulong)(Unsafe.SizeOf<Vertex>() * _vertices.Length);

        Buffer stagingBuffer = default;
        DeviceMemory stagingBufferMemory = default;
        _instance.CommandBufferUtil.CreateBuffer(bufferSize,
            BufferUsageFlags.TransferSrcBit,
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit,
            ref stagingBuffer,
            ref stagingBufferMemory);

        void* data;
        _instance.Vk.MapMemory(_instance.Device.Device, stagingBufferMemory, 0, bufferSize, 0, &data);
        _vertices.AsSpan().CopyTo(new Span<Vertex>(data, _vertices.Length));
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
        ulong bufferSize = (ulong)(Unsafe.SizeOf<ushort>() * _indices.Length);

        Buffer stagingBuffer = default;
        DeviceMemory stagingBufferMemory = default;
        _instance.CommandBufferUtil.CreateBuffer(bufferSize,
            BufferUsageFlags.TransferSrcBit,
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit,
            ref stagingBuffer,
            ref stagingBufferMemory);

        void* data;
        _instance.Vk.MapMemory(_instance.Device.Device, stagingBufferMemory, 0, bufferSize, 0, &data);
        _indices.AsSpan().CopyTo(new Span<ushort>(data, _indices.Length));
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