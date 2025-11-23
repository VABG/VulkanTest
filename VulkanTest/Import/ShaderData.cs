using VulkanTest.Shaders;

namespace VulkanTest.Import;

public class ShaderData(byte[] byteCode, ShaderType shaderType)
{
    public byte[] ByteCode { get; } = byteCode;
    public ShaderType ShaderType { get; } = shaderType;
}