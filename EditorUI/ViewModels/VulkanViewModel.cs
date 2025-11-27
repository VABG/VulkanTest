using System;
using System.IO;
using Avalonia.Rendering.Composition;
using EditorUI.VulkanControl;
using Vulkan;
using Vulkan.ResourceManagement.Compile;

namespace EditorUI.ViewModels;

public class VulkanViewModel : ViewModelBase
{
    public SilkHostVulkan? VulkanControlContent
    {
        get;
        set => SetProperty(ref field, value);
    }

    public CompositionSurfaceVisual CompositionSurfaceVisual { get; private set; }

    public int Width
    {
        get;
        set => SetProperty(ref field, value);
    }

    public int Height
    {
        get;
        set => SetProperty(ref field, value);
    }

    private readonly VkInstance? _vkInstance;

    public VulkanViewModel()
    {
        
        
        Width = 512;
        Height = 512;
        ShaderCompiler.CompileShadersInDirectory(
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets"),
            true);

        _vkInstance = new VkInstance(Width, Height, true);
        VulkanControlContent = new SilkHostVulkan(_vkInstance.Hwnd);
    }

    public void Draw()
    {
        _vkInstance?.Render();
    }
}