using System.Runtime.InteropServices;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;

namespace Vulkan;

public unsafe class VkValidationLayers : IDisposable
{
    private readonly ExtDebugUtils _debugUtils;
    private DebugUtilsMessengerEXT _debugMessenger;
    
    public readonly string[] ValidationLayers = ["VK_LAYER_KHRONOS_validation"];

    public VkValidationLayers(Vk vk, Instance instance)
    {
        _debugUtils = SetupDebugMessenger(vk, instance);
        CheckValidationLayerSupport(vk);
    }

    private ExtDebugUtils SetupDebugMessenger(Vk vk, Instance instance)
    {
        if (!vk.TryGetInstanceExtension(instance, out ExtDebugUtils extDebugUtils))
            throw new Exception("Failed to setup debug messenger!");

        DebugUtilsMessengerCreateInfoEXT createInfo = new();
        PopulateDebugMessengerCreateInfo(ref createInfo);

        if (extDebugUtils!.CreateDebugUtilsMessenger(instance, in createInfo, null, out _debugMessenger) != Result.Success)
        {
            throw new Exception("failed to set up debug messenger!");
        }

        return extDebugUtils;
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
    
    private void CheckValidationLayerSupport(Vk vk)
    {
        uint layerCount = 0;
        vk.EnumerateInstanceLayerProperties(ref layerCount, null);
        var availableLayers = new LayerProperties[layerCount];
        fixed (LayerProperties* availableLayersPtr = availableLayers)
        {
            vk.EnumerateInstanceLayerProperties(ref layerCount, availableLayersPtr);
        }

        var availableLayerNames = availableLayers.Select(layer => Marshal.PtrToStringAnsi((IntPtr)layer.LayerName)).ToHashSet();

        if (!ValidationLayers.All(availableLayerNames.Contains))
            throw new Exception("validation layers requested, but not available!");
    }
    
    private uint DebugCallback(DebugUtilsMessageSeverityFlagsEXT messageSeverity, DebugUtilsMessageTypeFlagsEXT messageTypes, DebugUtilsMessengerCallbackDataEXT* pCallbackData, void* pUserData)
    {
        System.Diagnostics.Debug.WriteLine($"validation layer:" + Marshal.PtrToStringAnsi((nint)pCallbackData->PMessage));
        return Vk.False;
    }
    
    public void Dispose()
    {
        _debugUtils.DestroyDebugUtilsMessenger(VkUtil.Instance, _debugMessenger, null);
        _debugUtils.Dispose();
    }
}