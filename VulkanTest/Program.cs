namespace VulkanTest;

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
        _instance = new VkInstance(Width, Height);
        MainLoop();
        CleanUp();
    }

    private void MainLoop()
    {
        _instance?.Run();
    }

    private void CleanUp()
    {
        _instance?.Dispose();
    }
}