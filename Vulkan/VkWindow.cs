using Silk.NET.GLFW;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Glfw;

namespace Vulkan;

public class VkWindow : IDisposable
{
    public IWindow Window { get; }
    private readonly GlfwNativeWindow? _glfwNativeWindow;
    private readonly Glfw? _glfw;
    public nint Hwnd => (IntPtr)_glfwNativeWindow?.Win32?.Hwnd!;
    
    
    public VkWindow(int  width, int height, bool subWindow)
    {
        var options = WindowOptions.DefaultVulkan with
        {
            Size = new Vector2D<int>(width, height),
            Title = "Vulkan"
        };
        if (subWindow)
        {
            options.IsVisible = false;
            options.WindowBorder = WindowBorder.Hidden;
            options.ShouldSwapAutomatically = true;
        }

        Window = Silk.NET.Windowing.Window.Create(options);
        if(subWindow)
            _glfw = GlfwWindowing.GetExistingApi(Window);
        Window.Initialize();

        if (Window.VkSurface is null)
        {
            throw new Exception("Windowing platform doesn't support Vulkan.");
        }

        if (!subWindow) 
            return;
        
        _glfwNativeWindow = CreateGlfwNativeWindow();
        if(_glfwNativeWindow == null)
            throw new Exception("Parent windowing platform doesn't support Vulkan.");
    }

    private unsafe GlfwNativeWindow CreateGlfwNativeWindow()
    {
        return new GlfwNativeWindow(_glfw, (WindowHandle*)Window.Handle);
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