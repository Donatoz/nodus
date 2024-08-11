using System.Runtime.InteropServices;
using DynamicData;
using Nodus.Core.Extensions;
using Nodus.RenderEngine.Vulkan;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Windowing;

namespace Nodus.VisualTests;

public unsafe class VkWindow
{
    private readonly IWindow window;
    private readonly bool useValidationLayers;
    
    private readonly string[] valLayers =
    [
        "VK_LAYER_KHRONOS_validation"
    ];
    
    private Vk? vk;
    private Instance instance;
    private ExtDebugUtils? debugUtils;
    private DebugUtilsMessengerEXT debugMessenger;
    private PhysicalDevice? physicalDevice;
    
    public VkWindow(bool useValidationLayers = true)
    {
        this.useValidationLayers = useValidationLayers;
        var options = WindowOptions.DefaultVulkan with
        {
            Size = new Vector2D<int>(800, 600),
            Title = "Nodus Renderer (Vulkan)"
        };

        window = Window.Create(options);
        window.Load += OnLoad;
        window.Closing += OnClosing;
        window.Initialize();

        if (window.VkSurface is null)
        {
            throw new Exception("Failed to create Vk window: Vulkan is not supported");
        }
    }
    public void Run()
    {
        window.Run();
        window.Dispose();
    }
    
    private void OnLoad()
    {
        vk = Vk.GetApi();

        if (useValidationLayers && !CheckValidationLayerSupport())
        {
            throw new Exception("Failed to enable validation layers: not supported.");
        }
        
        CreateVkInstance();
        SetupDebugMessenger();

        physicalDevice = GetFirstSuitablePhysicalDevice();
    }
    
    private void OnClosing()
    {
        Cleanup();
    }

    private void CreateVkInstance()
    {
        var appInfo = new ApplicationInfo
        {
            SType = StructureType.ApplicationInfo,
            PApplicationName = (byte*)Marshal.StringToHGlobalAnsi("Nodus Renderer"),
            ApplicationVersion = new Version32(1, 0, 0),
            PEngineName = (byte*)Marshal.StringToHGlobalAnsi("No Engine"),
            EngineVersion = new Version32(1, 0, 0),
            ApiVersion = Vk.Version13
        };

        var createInfo = new InstanceCreateInfo
        {
            SType = StructureType.InstanceCreateInfo,
            PApplicationInfo = &appInfo
        };

        var extensions = GetRequiredExtensions();

        createInfo.EnabledExtensionCount = (uint)extensions.Length;
        createInfo.PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(extensions);

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("Extensions loaded:");
        extensions.ForEach(Console.WriteLine);
        Console.ResetColor();

        if (useValidationLayers)
        {
            createInfo.EnabledLayerCount = (uint)valLayers.Length;
            createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(valLayers);

            var debugCreateInfo = new DebugUtilsMessengerCreateInfoEXT();
            PopulateDebugMessengerCreateInfo(ref debugCreateInfo);

            createInfo.PNext = &debugCreateInfo;
        }
        else
        {
            createInfo.EnabledLayerCount = 0;
            createInfo.PNext = null;
        }

        if (vk!.CreateInstance(createInfo, null, out instance) != Result.Success)
        {
            throw new Exception("Failed to create Vk instance.");
        }
        
        Marshal.FreeHGlobal((IntPtr)appInfo.PApplicationName);
        Marshal.FreeHGlobal((IntPtr)appInfo.PEngineName);
        SilkMarshal.Free((nint)createInfo.PpEnabledExtensionNames);

        if (useValidationLayers)
        {
            SilkMarshal.Free((nint)createInfo.PpEnabledLayerNames);
        }
    }

    private void Exit()
    {
        Cleanup();
        window.Close();
    }
    
    private void SetupDebugMessenger()
    {
        if (!useValidationLayers) return;

        if (!vk!.TryGetInstanceExtension(instance, out debugUtils)) return;

        var createInfo = new DebugUtilsMessengerCreateInfoEXT();
        PopulateDebugMessengerCreateInfo(ref createInfo);

        if (debugUtils!.CreateDebugUtilsMessenger(instance, in createInfo, null, out debugMessenger) != Result.Success)
        {
            throw new Exception("Failed to create debug messenger.");
        }
    }
    
    private void PopulateDebugMessengerCreateInfo(ref DebugUtilsMessengerCreateInfoEXT createInfo)
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
    
    private uint DebugCallback(DebugUtilsMessageSeverityFlagsEXT messageSeverity, DebugUtilsMessageTypeFlagsEXT messageTypes, DebugUtilsMessengerCallbackDataEXT* pCallbackData, void* pUserData)
    {
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine($"Validation: {Marshal.PtrToStringAnsi((nint)pCallbackData->PMessage)}");
        Console.ResetColor();
        
        return Vk.False;
    }
    
    private string[] GetRequiredExtensions()
    {
        var windowExtensions = window.VkSurface!.GetRequiredExtensions(out var glfwExtensionCount);
        var extensions = SilkMarshal.PtrToStringArray((nint)windowExtensions, (int)glfwExtensionCount);

        if (useValidationLayers)
        {
            extensions = extensions.Append(ExtDebugUtils.ExtensionName).ToArray();
        }

        return extensions;
    }
    
    private bool CheckValidationLayerSupport()
    {
        var layerCount = 0u;
        vk!.EnumerateInstanceLayerProperties(ref layerCount, null);
        var availableLayers = new LayerProperties[layerCount];
        fixed (LayerProperties* availableLayersPtr = availableLayers)
        {
            vk!.EnumerateInstanceLayerProperties(ref layerCount, availableLayersPtr);
        }

        var availableLayerNames = availableLayers.Select(layer =>
        {
            return Marshal.PtrToStringAnsi((IntPtr)layer.LayerName);
        }).ToHashSet();

        return valLayers.All(availableLayerNames.Contains);
    }

    private PhysicalDevice GetFirstSuitablePhysicalDevice()
    {
        var devices = vk!.GetPhysicalDevices(instance);

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("Available devices:");

        for (var i = 0; i < devices.Count; i++)
        {
            var props = vk.GetPhysicalDeviceProperties(devices.ElementAt(i));
            Console.WriteLine($"[{i}] {SilkMarshal.PtrToString((IntPtr)props.DeviceName)}, Driver={props.DriverVersion}");
        }
        
        var device = devices.FirstOrDefault(IsDeviceSuitable);

        if (device.Handle == 0)
        {
            throw new Exception("Failed to find suitable physical device.");
        }

        Console.WriteLine($"Picked device: [{devices.IndexOf(device)}]");
        Console.ResetColor();
        
        return device;
    }
    
    private bool IsDeviceSuitable(PhysicalDevice device)
    {
        var indices = VkQueueFamily.GetFromDevice(device, vk!);

        return indices.IsComplete();
    }
    
    private void Cleanup()
    {
        if (useValidationLayers)
        {
            debugUtils!.DestroyDebugUtilsMessenger(instance, debugMessenger, null);
        }
        
        vk!.DestroyInstance(instance, null);
        
        vk.Dispose();
    }
}