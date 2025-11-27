using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using EditorUI.VulkanControl;
using Vulkan;

namespace EditorUI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public VulkanViewModel VulkanView { get; }
    
    public MainWindowViewModel()
    {
        VulkanView = new VulkanViewModel();
    }

    public void StartVulkan()
    {

    }
}