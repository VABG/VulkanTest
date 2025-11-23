using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Maths;
using Silk.NET.Vulkan;

namespace VulkanTest;

public struct Vertex : IEquatable<Vertex>
{
    public Vector3D<float> Pos;
    public Vector3D<float> Color;
    public Vector2D<float> TexCoord;

    public static VertexInputBindingDescription GetBindingDescription()
    {
        VertexInputBindingDescription bindingDescription = new()
        {
            Binding = 0,
            Stride = (uint)Unsafe.SizeOf<Vertex>(),
            InputRate = VertexInputRate.Vertex,
        };

        return bindingDescription;
    }

    public static VertexInputAttributeDescription[] GetAttributeDescriptions()
    {
        var attributeDescriptions = new[]
        {
            new VertexInputAttributeDescription()
            {
                Binding = 0,
                Location = 0,
                Format = Format.R32G32B32Sfloat,
                Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(Pos)),
            },
            new VertexInputAttributeDescription()
            {
                Binding = 0,
                Location = 1,
                Format = Format.R32G32B32Sfloat,
                Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(Color)),
            },
            new VertexInputAttributeDescription()
            {
            Binding = 0,
            Location = 2,
            Format = Format.R32G32Sfloat,
            Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(TexCoord)),
            }
        };

        return attributeDescriptions;
    }

    public bool Equals(Vertex other)
    {
        return Pos.Equals(other.Pos) && Color.Equals(other.Color) && TexCoord.Equals(other.TexCoord);
    }

    public override bool Equals(object? obj)
    {
        return obj is Vertex other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Pos, Color, TexCoord);
    }
}