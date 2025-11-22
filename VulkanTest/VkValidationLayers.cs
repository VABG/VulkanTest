using System.Runtime.InteropServices;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;

namespace VulkanTest;

public unsafe class VkValidationLayers : IDisposable
{
    private readonly VkInstance _vkInstance;
    private ExtDebugUtils? _debugUtils;
    private DebugUtilsMessengerEXT _debugMessenger;
    
    public readonly string[] ValidationLayers = ["VK_LAYER_KHRONOS_validation"];

    public VkValidationLayers(VkInstance vkInstance)
    {
        _vkInstance = vkInstance;
    }
    
    
    public void Dispose()
    {
        _debugUtils?.DestroyDebugUtilsMessenger(_vkInstance.Instance, _debugMessenger, null);
        _debugUtils?.Dispose();
    }
    
    public void SetupDebugMessenger()
    {
        //TryGetInstanceExtension equivalent to method CreateDebugUtilsMessengerEXT from original tutorial.
        if (!_vkInstance.Vk!.TryGetInstanceExtension(_vkInstance.Instance, out _debugUtils)) return;

        DebugUtilsMessengerCreateInfoEXT createInfo = new();
        PopulateDebugMessengerCreateInfo(ref createInfo);

        if (_debugUtils!.CreateDebugUtilsMessenger(_vkInstance.Instance, in createInfo, null, out _debugMessenger) != Result.Success)
        {
            throw new Exception("failed to set up debug messenger!");
        }
    }
    
    public void PopulateDebugMessengerCreateInfo(ref DebugUtilsMessengerCreateInfoEXT createInfo)
    {
        createInfo.SType = StructureType.DebugUtilsMessengerCreateInfoExt;
        createInfo.MessageSeverity = DebugUtilsMessageSeverityFlagsEXT.VerboseBitExt |
                                     DebugUtilsMessageSeverityFlagsEXT.WarningBitExt |
                                     DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt;
        createInfo.MessageType = DebugUtilsMessageTypeFlagsEXT.GeneralBitExt |
                                 DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt |
                                 DebugUtilsMessageTypeFlagsEXT.ValidationBitExt;
        createInfo.PfnUserCallback = (DebugUtilsMessengerCallbackFunctionEXT)DebugCallback;
    }
    
    public bool CheckValidationLayerSupport()
    {
        uint layerCount = 0;
        _vkInstance.Vk.EnumerateInstanceLayerProperties(ref layerCount, null);
        var availableLayers = new LayerProperties[layerCount];
        fixed (LayerProperties* availableLayersPtr = availableLayers)
        {
            _vkInstance.Vk.EnumerateInstanceLayerProperties(ref layerCount, availableLayersPtr);
        }

        var availableLayerNames = availableLayers.Select(layer => Marshal.PtrToStringAnsi((IntPtr)layer.LayerName)).ToHashSet();

        return ValidationLayers.All(availableLayerNames.Contains);
    }
    
    private uint DebugCallback(DebugUtilsMessageSeverityFlagsEXT messageSeverity, DebugUtilsMessageTypeFlagsEXT messageTypes, DebugUtilsMessengerCallbackDataEXT* pCallbackData, void* pUserData)
    {
        System.Diagnostics.Debug.WriteLine($"validation layer:" + Marshal.PtrToStringAnsi((nint)pCallbackData->PMessage));
        return Vk.False;
    }
}