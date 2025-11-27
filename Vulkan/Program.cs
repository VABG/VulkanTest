using Vulkan.ResourceManagement.Compile;

namespace Vulkan;

public class Program
{
    private const int Width = 1280;
    private const int Height = 720;

    private VkInstance? _instance;

    public static void Main(string[] args)
    {
        var app = new Program();
        app.Run();
    }

    private void Run()
    {
        ShaderCompiler.CompileShadersInDirectory(
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets"), 
            true);
        
        _instance = new VkInstance(Width, Height, false);
        MainLoop();
        CleanUp();
    }

    private void MainLoop()
    {
        _instance?.Run();
        VkUtil.Vk.DeviceWaitIdle(VkUtil.Device);
    }

    private void CleanUp()
    {
        _instance?.Dispose();
    }
}