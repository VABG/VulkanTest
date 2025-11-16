using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace VulkanTest;

public class VkWindow : IDisposable
{
    public IWindow Window { get; } 
    
    public VkWindow(int  width, int height)
    {
        var options = WindowOptions.DefaultVulkan with
        {
            Size = new Vector2D<int>(width, height),
            Title = "Vulkan"
        };

        Window = Silk.NET.Windowing.Window.Create(options);
        Window.Initialize();

        if (Window.VkSurface is null)
        {
            throw new Exception("Windowing platform doesn't support Vulkan.");
        }
    }

    public void Run()
    {
        Window.Run();
    }

    public void Dispose()
    {
        Window.Dispose();
    }
}