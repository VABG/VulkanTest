using System.Runtime.CompilerServices;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace VulkanTest;

public unsafe class VkModelBuffer : IDisposable
{
    private readonly Model _model;

    public Buffer VertexBuffer;
    private readonly DeviceMemory _vertexBufferMemory;

    public Buffer IndexBuffer;
    private readonly DeviceMemory _indexBufferMemory;

    public VkModelBuffer(VkRender render, Model model)
    {
        _model = model;
        CreateBuffer(render, _model.Vertices, ref VertexBuffer, ref _vertexBufferMemory);
        CreateBuffer(render, _model.Indices, ref IndexBuffer, ref _indexBufferMemory);
        // CreateVertexBuffer(render);
        // CreateIndexBuffer(render);
    }

    public uint GetIndicesCount() => (uint)_model.Indices.Length;

    private void CreateBuffer<T>(VkRender render, T[] input, ref Buffer buffer, ref DeviceMemory memory)
    {
        ulong bufferSize = (ulong)(Unsafe.SizeOf<T>() * input.Length);

        Buffer stagingBuffer = default;
        DeviceMemory stagingBufferMemory = default;
        render.CommandBufferUtil.CreateBuffer(bufferSize,
            BufferUsageFlags.TransferSrcBit,
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit,
            ref stagingBuffer,
            ref stagingBufferMemory);

        void* data;
        VkUtil.Vk.MapMemory(VkUtil.Device, stagingBufferMemory, 0, bufferSize, 0, &data);
        input.AsSpan().CopyTo(new Span<T>(data, input.Length));
        VkUtil.Vk.UnmapMemory(VkUtil.Device, stagingBufferMemory);

        render.CommandBufferUtil.CreateBuffer(bufferSize,
            BufferUsageFlags.TransferDstBit | BufferUsageFlags.IndexBufferBit,
            MemoryPropertyFlags.DeviceLocalBit,
            ref buffer,
            ref memory);

        render.CommandBufferUtil.CopyBuffer(stagingBuffer, buffer, bufferSize, render.Commands.CommandPool);

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