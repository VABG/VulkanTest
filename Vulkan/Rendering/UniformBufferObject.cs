using Silk.NET.Maths;

namespace Vulkan.Rendering;

struct UniformBufferObject
{
    public Matrix4X4<float> Model;
    public Matrix4X4<float> View;
    public Matrix4X4<float> Proj;
}