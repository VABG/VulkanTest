using System.Runtime.CompilerServices;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace VulkanTest;

public unsafe class VkVertexBuffer : IDisposable
{
    private readonly Model _model;

    //TODO: Generalize buffer (at least data and creation)
    public Buffer VertexBuffer;
    private DeviceMemory _vertexBufferMemory;

    public Buffer IndexBuffer;
    private DeviceMemory _indexBufferMemory;

    public VkVertexBuffer(VkRender render, Model model)
    {
        _model = model;
        CreateVertexBuffer(render);
        CreateIndexBuffer(render);
    }

    public uint GetIndicesCount() => (uint)_model.Indices.Length;

    private void CreateVertexBuffer(VkRender render)
    {
        var vertices = _model.Vertices;

        ulong bufferSize = (ulong)(Unsafe.SizeOf<Vertex>() * vertices.Length);

        Buffer stagingBuffer = default;
        DeviceMemory stagingBufferMemory = default;
        render.CommandBufferUtil.CreateBuffer(bufferSize,
            BufferUsageFlags.TransferSrcBit,
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit,
            ref stagingBuffer,
            ref stagingBufferMemory);

        
        void* data;
        VkUtil.Vk.MapMemory(VkUtil.Device, stagingBufferMemory, 0, bufferSize, 0, &data);
        vertices.AsSpan().CopyTo(new Span<Vertex>(data, vertices.Length));
        VkUtil.Vk.UnmapMemory(VkUtil.Device, stagingBufferMemory);

        render.CommandBufferUtil.CreateBuffer(bufferSize,
            BufferUsageFlags.TransferDstBit | BufferUsageFlags.VertexBufferBit,
            MemoryPropertyFlags.DeviceLocalBit,
            ref VertexBuffer,
            ref _vertexBufferMemory);

        render.CommandBufferUtil.CopyBuffer(stagingBuffer, VertexBuffer, bufferSize, render.Commands.CommandPool);

        VkUtil.Vk.DestroyBuffer(VkUtil.Device, stagingBuffer, null);
        VkUtil.Vk.FreeMemory(VkUtil.Device, stagingBufferMemory, null);
    }

    private void CreateIndexBuffer(VkRender render)
    {
        var indices = _model.Indices;
        ulong bufferSize = (ulong)(Unsafe.SizeOf<uint>() * indices.Length);

        Buffer stagingBuffer = default;
        DeviceMemory stagingBufferMemory = default;
        render.CommandBufferUtil.CreateBuffer(bufferSize,
            BufferUsageFlags.TransferSrcBit,
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit,
            ref stagingBuffer,
            ref stagingBufferMemory);

        void* data;
        VkUtil.Vk.MapMemory(VkUtil.Device, stagingBufferMemory, 0, bufferSize, 0, &data);
        indices.AsSpan().CopyTo(new Span<uint>(data, indices.Length));
        VkUtil.Vk.UnmapMemory(VkUtil.Device, stagingBufferMemory);

        render.CommandBufferUtil.CreateBuffer(bufferSize,
            BufferUsageFlags.TransferDstBit | BufferUsageFlags.IndexBufferBit,
            MemoryPropertyFlags.DeviceLocalBit,
            ref IndexBuffer,
            ref _indexBufferMemory);

        render.CommandBufferUtil.CopyBuffer(stagingBuffer, IndexBuffer, bufferSize, render.Commands.CommandPool);

        VkUtil.Vk.DestroyBuffer(VkUtil.Device, stagingBuffer, null);
        VkUtil.Vk.FreeMemory(VkUtil.Device, stagingBufferMemory, null);
    }

    public void Dispose()
    {
        VkUtil.Vk.DestroyBuffer(VkUtil.Device, VertexBuffer, null);
        VkUtil.Vk.FreeMemory(VkUtil.Device, _vertexBufferMemory, null);
        VkUtil.Vk.DestroyBuffer(VkUtil.Device, IndexBuffer, null);
        VkUtil.Vk.FreeMemory(VkUtil.Device, _indexBufferMemory, null);
    }
}