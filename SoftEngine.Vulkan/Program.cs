/*****
 * Built following the base official Vulkan tutorial,
 * found here:
 * https://vulkan-tutorial.com/Introduction
 * While also ~~stealing~~ borrowing bits from this repo:
 * https://github.com/dfkeenan/SilkVulkanTutorial
 * when I'm not sure how to properly translate the C++ bits. 
 *****/

using System.Runtime.InteropServices;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Windowing;
using SoftEngine.Vulkan;

const int WIDTH = 800;
const int HEIGHT = 600;

var app = new HelloTriangleApplication();
var enableValidations = false;

#if DEBUG
enableValidations = true;
#endif

app.Run(WIDTH, HEIGHT, enableValidations);

unsafe class HelloTriangleApplication
{
    readonly string[] validationLayers = 
    { "VK_LAYER_KHRONOS_validation"
    };

    bool CheckValidationLayerSupport(Vk vk)
    {
        uint layerCount = 0;
        vk.EnumerateInstanceLayerProperties(ref layerCount, null);

        var availableLayers = new LayerProperties[layerCount];
        fixed (LayerProperties* pAvailableLayers = availableLayers)
        {
            vk.EnumerateInstanceLayerProperties(ref layerCount, pAvailableLayers);
        }

        var availableLayerNames = availableLayers
            .Select(layer => Marshal.PtrToStringAnsi((IntPtr)layer.LayerName))
            .ToHashSet();

        return validationLayers.All(availableLayerNames.Contains);
    }

    string[] GetRequiredExtensions(IWindow window, bool enableValidationLayers)
    {
        if (window.VkSurface == null)
            throw new InvalidOperationException(
                "Window has no Vulkan surface to use!");
        
        var glfwExtensions = (char**)window.VkSurface.GetRequiredExtensions(out var glfwExtensionCount);
        var extensions = SilkMarshal.PtrToStringArray((nint)glfwExtensions, (int)glfwExtensionCount);
        if (enableValidationLayers)
        {
            return extensions
                .Append(ExtDebugUtils.ExtensionName)
                .ToArray();
        }

        return extensions;
    }

    uint DebugCallback(
      DebugUtilsMessageSeverityFlagsEXT severity
    , DebugUtilsMessageTypeFlagsEXT messageType
    , DebugUtilsMessengerCallbackDataEXT* pCallbackData
    , void* pUserData)
    {
        Console.Error.Write($"Validation Layer: {Marshal.PtrToStringAnsi((IntPtr)pCallbackData->PMessage)}");

        // The debug callback should return a boolean/value that
        // indicates if the Vulkan call that triggered it should
        // be aborted.
        // If True, the Vulkan aborts with VK_ERROR_VALIDATION_FAILED_EXT.
        // Generally, this should always be False unless testing validation layers.
        return Vk.False;
    }

    DebugUtilsMessengerCreateInfoEXT ConstructDebugCreateInfo() =>
        new()
        { SType = StructureType.DebugUtilsMessengerCreateInfoExt
            , MessageSeverity
                = DebugUtilsMessageSeverityFlagsEXT.VerboseBitExt
                  | DebugUtilsMessageSeverityFlagsEXT.WarningBitExt
                  | DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt
            , MessageType
                = DebugUtilsMessageTypeFlagsEXT.GeneralBitExt
                  | DebugUtilsMessageTypeFlagsEXT.ValidationBitExt
                  | DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt
            , PfnUserCallback = (DebugUtilsMessengerCallbackFunctionEXT)DebugCallback
        };
    
    IWindow InitWindow(int width, int height)
    {
        var windowOptions = WindowOptions.DefaultVulkan with
        {
            Size  = new Vector2D<int>(width, height),
            Title = "Vulkan Tutorial"
        };

        var window = Window.Create(windowOptions);
        window.Initialize();

        if (window.VkSurface is null)
            throw new InvalidOperationException("Window platform doesn't exist.");

        return window;
    }

    (Vk api, Instance instance) CreateInstance(IWindow window, bool enableValidationLayers)
    {
        var vulkanApi = Vk.GetApi();
        if (enableValidationLayers && !CheckValidationLayerSupport(vulkanApi!))
            throw new InvalidOperationException(
                "Validation layers were requested, but none were available!");
        
        ApplicationInfo vkAppInfo = new()
        { SType              = StructureType.ApplicationInfo
                                // Marshal is the thing to work with unmanaged memory.
        , PApplicationName   = (byte*)Marshal.StringToHGlobalAnsi("Hello Triangle")
        , ApplicationVersion = 1
        , PEngineName        = (byte*)Marshal.StringToHGlobalAnsi("No Engine")
        , EngineVersion      = new Version32(1, 0, 0)
        , ApiVersion         = Vk.Version10
        };
        
        InstanceCreateInfo vkCreateInfo = new()
        { SType            = StructureType.InstanceCreateInfo
        , PApplicationInfo = &vkAppInfo
        };

        var extensions = GetRequiredExtensions(window, enableValidationLayers);
        vkCreateInfo.EnabledExtensionCount   = (uint)extensions.Length;
        vkCreateInfo.PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(extensions);
        
        if (enableValidationLayers)
        {
            vkCreateInfo.EnabledLayerCount   = (uint)validationLayers.Length;
            vkCreateInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(validationLayers);
            
            var debugCreateInfo = ConstructDebugCreateInfo();
            vkCreateInfo.PNext  = &debugCreateInfo;
        }
        else
        {
            vkCreateInfo.EnabledLayerCount = 0;
            vkCreateInfo.PNext             = null;
        }

        if (vulkanApi.CreateInstance(&vkCreateInfo, null, out var vulkanInstance) != Result.Success)
        {
            throw new ApplicationException(
                "Failed to create Vulkan instance!");
        }
        
        // Since we allocated unmanaged memory,
        // we need to free it now that we're done with it.
        Marshal.FreeHGlobal((IntPtr)vkAppInfo.PApplicationName);
        Marshal.FreeHGlobal((IntPtr)vkAppInfo.PEngineName);

        return (vulkanApi, vulkanInstance);
    }

    (ExtDebugUtils? utils, DebugUtilsMessengerEXT? messenger) SetupDebugMessenger(Vk vk, Instance instance, bool enableValidationLayers)
    {
        if (!enableValidationLayers)
            return (null, null);

        if (!vk.TryGetInstanceExtension(instance, out ExtDebugUtils debugUtils))
            throw new InvalidOperationException(
                "Failed to set up the debug utils!");
        
        var createInfo = ConstructDebugCreateInfo();
        if (debugUtils.CreateDebugUtilsMessenger(instance, in createInfo, null, out var debugMessenger) != Result.Success)
            throw new InvalidOperationException(
                "Failed to set up the debug messenger!");

        return (debugUtils, debugMessenger);
    }

    bool IsDeviceSuitable(Vk api, PhysicalDevice device)
    {
        var isSuitable = true;
        
        // For querying properties.
        PhysicalDeviceProperties deviceProperties;
        api.GetPhysicalDeviceProperties(device, &deviceProperties);
        
        // For querying features.
        PhysicalDeviceFeatures deviceFeatures;
        api.GetPhysicalDeviceFeatures(device, &deviceFeatures);

        return isSuitable;
    }

    QueueFamilyIndices FindQueueFamilies(Vk api, PhysicalDevice device)
    {
        QueueFamilyIndices indices = new();

        uint queueFamilyCount;
        api.GetPhysicalDeviceQueueFamilyProperties(device, &queueFamilyCount, null);

        // Same with any other array that we need to pass as a pointer:
        // init the array, fix the array in memory, pass the fixed pointer. 
        var queueFamilies = new QueueFamilyProperties[queueFamilyCount];
        fixed (QueueFamilyProperties* queueFamiliesPtr = queueFamilies)
        {
            api.GetPhysicalDeviceQueueFamilyProperties(device, &queueFamilyCount, queueFamiliesPtr);
        }
        
        

        return indices;
    }
    
    void PickPhysicalDevice(Vk api, Instance instance)
    {
        PhysicalDevice physicalDevice = new();
        uint deviceCount = 0;
        api.EnumeratePhysicalDevices(instance, &deviceCount, null);
        if (deviceCount == 0)
            throw new Exception(
                "No GPUs found that support Vulkan!");

        // Create a new array with the size of the devices.
        // Next, fix the location of the array so we can pass 
        // it as a parameter to the enumerate method.
        var devices = new PhysicalDevice[deviceCount];
        fixed (PhysicalDevice* devicesPointer = devices)
        {
            api.EnumeratePhysicalDevices(instance, &deviceCount, devicesPointer);
        }

        foreach (var device in devices)
            if (IsDeviceSuitable(api, device))
            {
                physicalDevice = device;
                break;
            }

        if (physicalDevice.Handle == 0)
            throw new Exception(
                "Failed to find a suitable GPU!");
    }
    
    (Vk api, Instance instance, ExtDebugUtils? debugUtils, DebugUtilsMessengerEXT? debugMessenger) InitVulkan(IWindow window, bool enableValidationLayers)
    {
        var (api, instance) = CreateInstance(window, enableValidationLayers);
        var (debugUtils, debugMessenger) = SetupDebugMessenger(api, instance, enableValidationLayers);

        return (api, instance, debugUtils, debugMessenger);
    }

    void MainLoop(IWindow window)
    {
        window.Run();
    }

    void Cleanup(
      IWindow window
    , Instance instance
    , Vk vk
    , ExtDebugUtils? debugUtils
    , DebugUtilsMessengerEXT? debugMessenger
    , bool enableValidationLayers)
    {
        if (enableValidationLayers
        && debugUtils != null
        && debugMessenger.HasValue)
            debugUtils.DestroyDebugUtilsMessenger(
              instance
            , debugMessenger.Value
            , null
            );
        
        vk.DestroyInstance(instance, null);
        vk.Dispose();
        
        window.Dispose();
    }
    
    public void Run(int windowWidth, int windowHeight, bool enableValidation)
    {
        var window = InitWindow(windowWidth, windowHeight);
        var (vk, instance, debugUtils, debugMessenger) = 
            InitVulkan(window, enableValidation);
        
        MainLoop(window);
        Cleanup(
          window
        , instance
        , vk
        , debugUtils
        , debugMessenger
        , enableValidation
        );
    } 
}
