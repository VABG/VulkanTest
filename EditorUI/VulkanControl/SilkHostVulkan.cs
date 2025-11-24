using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform;
using Vulkan;
using Vulkan.ResourceManagement.Compile;

namespace EditorUI.VulkanControl;

public class SilkHostVulkan : NativeControlHost
{
    private VkInstance? _vulkanInstance;
    private Task? _vulkanLoop;

    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        // Get parent window handle for some reason (never used?)
        var parentHandle = parent.Handle;
        ShaderCompiler.CompileShadersInDirectory(
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets"), 
            true);
        
        //TODO: Get parent size
        
        _vulkanInstance = new VkInstance(1024, 768, true);
        // Get handle of child window
        var childHandle = _vulkanInstance.Hwnd;
        _vulkanLoop =  Task.Factory.StartNew(() => _vulkanInstance?.Run());
        return GetPlatformHandle(childHandle);
    }

    private IPlatformHandle GetPlatformHandle(IntPtr handle)
    {
        if (OperatingSystem.IsWindows())
            return new PlatformHandle(handle, "HWND");
        if (OperatingSystem.IsLinux())
            return new PlatformHandle(handle, "XID");
        if (OperatingSystem.IsMacOS())
            return new PlatformHandle(handle, "NSView");
        throw new PlatformNotSupportedException();
    }

    protected override void DestroyNativeControlCore(IPlatformHandle control)
    {
        _vulkanInstance?.Dispose();
        _vulkanLoop?.Dispose();
        base.DestroyNativeControlCore(control);
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        if (_vulkanInstance != null)
        {
            var scaling = TopLevel.GetTopLevel(this)!.RenderScaling;

            var renderWidth = (int)(Bounds.Width * scaling);
            var renderHeight = (int)(Bounds.Height * scaling);
        
            _vulkanInstance.UpdateResolution(renderWidth, renderHeight);
        }

        base.OnSizeChanged(e);
    }
    }