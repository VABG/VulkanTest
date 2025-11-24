using Vulkan.Data;
using Vulkan.Shaders;

namespace Vulkan.ResourceManagement.Import;

public class ShaderData(byte[] byteCode, ShaderType shaderType)
{
    public byte[] ByteCode { get; } = byteCode;
    public ShaderType ShaderType { get; } = shaderType;
}