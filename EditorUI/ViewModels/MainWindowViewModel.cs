using EditorUI.VulkanControl;

namespace EditorUI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public string Greeting { get; } = "Welcome to Avalonia!";

    public SilkHostVulkan VulkanControlContent { get; }
    
    public MainWindowViewModel()
    {
        VulkanControlContent = new SilkHostVulkan();;
    }
}