using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace VulkanTest;

public unsafe class VertexBuffer : IDisposable
{
    private readonly VkInstance _instance;
    public Buffer Buffer;
    
    private DeviceMemory _vertexBufferMemory;

    public VertexBuffer(VkInstance instance) 
    {
        _instance = instance;
        CreateVertexBuffer(instance);
    }
    
    private Vertex[] _vertices =
    [
        new Vertex { Pos = new Vector2D<float>(0.0f,-0.5f), Color = new Vector3D<float>(1.0f, 0.0f, 0.0f) },
        new Vertex { Pos = new Vector2D<float>(0.5f,0.5f), Color = new Vector3D<float>(0.0f, 1.0f, 0.0f) },
        new Vertex { Pos = new Vector2D<float>(-0.5f,0.5f), Color = new Vector3D<float>(0.0f, 0.0f, 1.0f) }
    ];

    
    private void CreateVertexBuffer(VkInstance instance)
    {
        BufferCreateInfo bufferInfo = new()
        {
            SType = StructureType.BufferCreateInfo,
            Size = (ulong)(sizeof(Vertex) * _vertices.Length),
            Usage = BufferUsageFlags.VertexBufferBit,
            SharingMode = SharingMode.Exclusive,
        };

        fixed (Buffer* vertexBufferPtr = &Buffer)
        {
            if (instance.Vk.CreateBuffer(instance.Device.Device, in bufferInfo, null, vertexBufferPtr) != Result.Success)
            {
                throw new Exception("failed to create vertex buffer!");
            }
        }

        instance.Vk.GetBufferMemoryRequirements(instance.Device.Device, Buffer, out var memRequirements);

        MemoryAllocateInfo allocateInfo = new()
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memRequirements.Size,
            MemoryTypeIndex = FindMemoryType(memRequirements.MemoryTypeBits, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit),
        };

        fixed (DeviceMemory* vertexBufferMemoryPtr = &_vertexBufferMemory)
        {
            if (instance.Vk.AllocateMemory(_instance.Device.Device, in allocateInfo, null, vertexBufferMemoryPtr) != Result.Success)
            {
                throw new Exception("failed to allocate vertex buffer memory!");
            }
        }

        _instance.Vk.BindBufferMemory(_instance.Device.Device, Buffer, _vertexBufferMemory, 0);

        void* data;
        _instance.Vk.MapMemory(_instance.Device.Device, _vertexBufferMemory, 0, bufferInfo.Size, 0, &data);
        _vertices.AsSpan().CopyTo(new Span<Vertex>(data, _vertices.Length));
        _instance.Vk.UnmapMemory(_instance.Device.Device, _vertexBufferMemory);
    }
    
    private uint FindMemoryType(uint typeFilter, MemoryPropertyFlags properties)
    {
        _instance.Vk.GetPhysicalDeviceMemoryProperties(_instance.Device.PhysicalDevice, out var memProperties);

        for (int i = 0; i < memProperties.MemoryTypeCount; i++)
            if ((typeFilter & (1 << i)) != 0 && (memProperties.MemoryTypes[i].PropertyFlags & properties) == properties)
                return (uint)i;

        throw new Exception("failed to find suitable memory type!");
    }

    public void Dispose()
    {
        _instance.Vk.DestroyBuffer(_instance.Device.Device, Buffer, null);
        _instance.Vk.FreeMemory(_instance.Device.Device, _vertexBufferMemory, null);
    }
}