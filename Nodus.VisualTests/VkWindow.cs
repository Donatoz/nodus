using System.Numerics;
using System.Runtime.InteropServices;
using DynamicData;
using Nodus.RenderEngine.Common;
using Nodus.RenderEngine.Serialization;
using Nodus.RenderEngine.Vulkan;
using Nodus.RenderEngine.Vulkan.DI;
using Nodus.RenderEngine.Vulkan.Meta;
using Nodus.RenderEngine.Vulkan.Renderers;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
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

    private readonly string[] deviceExtensions =
    [
        KhrSwapchain.ExtensionName
    ];
    
    private Vk? vk;
    private VkInstance? instance;
    private ExtDebugUtils? debugUtils;
    private DebugUtilsMessengerEXT debugMessenger;
    private PhysicalDevice? physicalDevice;
    private IVkLogicalDevice? logicalDevice;
    private IVkKhrSurface? surface;
    private IVkSwapChain? swapChain;
    private IScreenViewer? viewer;

    private VkLayerInfo? layerInfo;
    private VkExtensionsInfo? extensionsInfo;
    private VkContext? vkContext;
    private IVkExtensionProvider? extensionProvider;
    private IInputContext? input;
    
    public static readonly Vertex[] QuadVertices =
    {
        new(new Vector3(-0.5f, -0.5f, 0.0f), Vector3.Zero, new Vector2(0, 1)),
        new(new Vector3(0.5f, -0.5f, 0.0f), Vector3.Zero, new Vector2(1, 1)),
        new(new Vector3(0.5f, 0.5f, 0.0f), Vector3.Zero, new Vector2(1, 0)),
        new(new Vector3(-0.5f, 0.5f, 0.0f), Vector3.Zero, new Vector2(0, 0))
    };

    public static readonly uint[] QuadIndices =
    {
        0, 1, 2, 2, 3, 0
    };
    
    private readonly VkGeometryPrimitiveRenderer renderer;

    public VkWindow(bool useValidationLayers = true)
    {
        this.useValidationLayers = useValidationLayers;
        var options = WindowOptions.DefaultVulkan with
        {
            Size = new Vector2D<int>(800, 600),
            Title = "Nodus Renderer (Vulkan)"
        };
        
        renderer = new VkGeometryPrimitiveRenderer();

        window = Window.Create(options);
        window.Load += OnLoad;
        window.Closing += OnClosing;
        window.Render += OnRender;
        window.Update += OnUpdate;
        window.FramebufferResize += OnFrameBufferResize;
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

        layerInfo = new VkLayerInfo(valLayers);
        extensionsInfo = new VkExtensionsInfo(window.VkSurface!, layerInfo, deviceExtensions);
        vkContext = new VkContext(vk, extensionsInfo.Value, layerInfo);
        input = window.CreateInput();
        
        input.Keyboards.First().KeyDown += OnKeyDown;
        
        instance = CreateVkInstance();
        SetupDebugMessenger();
        surface = CreateSurface();
        
        physicalDevice = GetFirstSuitablePhysicalDevice();
        logicalDevice = CreateLogicalDevice(physicalDevice.Value);

        var queueInfo = VkQueueInfo.GetFromDevice(physicalDevice.Value, vk, surface);
        var geoFactory = new AssimpGeometryFactory();

        var cube = geoFactory.CreateFromFile(@"G:\CG\3D\Common\Cube.obj").First();

        extensionProvider = new VkExtensionProvider(vkContext, instance, logicalDevice);
        
        swapChain = CreateSwapChain();
        
        var pipelineContext = new VkPipelineContext(
            [DynamicState.Scissor ,DynamicState.Viewport], 
            new IShaderDefinition[]
            {
                new ShaderDefinition(
                    new ShaderFileSource(@"C:\Users\Donatoz\RiderProjects\Nodus\Nodus.VisualTests\Assets\Shaders\compiled\standard2.spv"),
                    ShaderSourceType.Vertex),
                new ShaderDefinition(
                    new ShaderFileSource(@"C:\Users\Donatoz\RiderProjects\Nodus\Nodus.VisualTests\Assets\Shaders\compiled\solid.spv"),
                    ShaderSourceType.Fragment)
            }, 
            swapChain!, queueInfo);

        var primitive = new GeometryPrimitive(QuadVertices, QuadIndices);
        viewer = new Viewer(new Vector2D<float>(swapChain.Extent.Width, swapChain.Extent.Height))
        {
            Position = Vector3D<float>.UnitZ * -5
        };
        
        renderer.Initialize(
            new VkGeometryPrimitiveRenderContext(cube, Enumerable.Empty<IShaderDefinition>(), logicalDevice, physicalDevice.Value, queueInfo, 
                swapChain, surface, viewer, pipelineContext, 2), 
            new VkRenderBackendProvider(vkContext)
        );
    }

    private void OnKeyDown(IKeyboard keyboard, Key key, int n)
    {
        if (key == Key.Escape)
        {
            window.Close();
        }
    }

    private void OnRender(double delta)
    {
        if (window.FramebufferSize.X == 0 || window.FramebufferSize.Y == 0) return;
        
        renderer.RenderFrame();
    }
    
    private void OnUpdate(double delta)
    {
        if (viewer != null && input?.Keyboards[0] is { } k)
        {
            const float movementDelta = 0.001f;
            
            var leftFactor = (k.IsKeyPressed(Key.A) ? 1 : 0) * movementDelta * Vector3D<float>.UnitX;
            var rightFactor = (k.IsKeyPressed(Key.D) ? 1 : 0) * movementDelta * -Vector3D<float>.UnitX;
            var forwardFactor = (k.IsKeyPressed(Key.W) ? 1 : 0) * movementDelta * Vector3D<float>.UnitZ;
            var backwardFactor = (k.IsKeyPressed(Key.S) ? 1 : 0) * movementDelta * -Vector3D<float>.UnitZ;
            var upFactor = (k.IsKeyPressed(Key.E) ? 1 : 0) * movementDelta * Vector3D<float>.UnitY;
            var downFactor = (k.IsKeyPressed(Key.Q) ? 1 : 0) * movementDelta * -Vector3D<float>.UnitY;

            viewer!.Position += leftFactor + rightFactor + forwardFactor + backwardFactor + upFactor + downFactor;
        }
    }
    
    private void OnFrameBufferResize(Vector2D<int> size)
    {
        if (viewer != null)
        {
            viewer.ScreenSize = new Vector2D<float>(size.X, size.Y);
        }
    }
    
    private void OnClosing()
    {
        Cleanup();
    }
    
    private void SetupDebugMessenger()
    {
        if (!useValidationLayers) return;

        if (!vk!.TryGetInstanceExtension(instance!.WrappedInstance, out debugUtils)) return;

        var createInfo = new DebugUtilsMessengerCreateInfoEXT();
        instance.PopulateDebugMessengerCreateInfo(ref createInfo);

        if (debugUtils!.CreateDebugUtilsMessenger(instance.WrappedInstance, in createInfo, null, out debugMessenger) != Result.Success)
        {
            throw new Exception("Failed to create debug messenger.");
        }
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
        var devices = vk!.GetPhysicalDevices(instance!);

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("Available devices:");

        for (var i = 0; i < devices.Count; i++)
        {
            var props = vk.GetPhysicalDeviceProperties(devices.ElementAt(i));
            Console.WriteLine($"[{i}] {SilkMarshal.PtrToString((nint)props.DeviceName)}, Driver={props.DriverVersion}");
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
        var indices = VkQueueInfo.GetFromDevice(device, vk!, surface!);

        var extensionsSupported = CheckDeviceExtensions(device);

        return indices.IsComplete() && extensionsSupported;
    }

    private bool CheckDeviceExtensions(PhysicalDevice device)
    {
        var extensionCount = 0u;
        vk!.EnumerateDeviceExtensionProperties(device, (byte*)null, ref extensionCount, null);
        
        var availableExtensions = new ExtensionProperties[extensionCount];

        fixed (ExtensionProperties* p = availableExtensions)
        {
            vk.EnumerateDeviceExtensionProperties(device, (byte*)null, ref extensionCount, p);
        }

        var availableExtensionNames = new HashSet<string>();

        foreach (var ext in availableExtensions)
        {
            var name = Marshal.PtrToStringAnsi((nint)ext.ExtensionName);
            
            if (name != null)
            {
                availableExtensionNames.Add(name);
            }
        }

        return deviceExtensions.All(availableExtensionNames.Contains);
    }
    
    private VkInstance CreateVkInstance()
    {
        return new VkInstance(vkContext!, new VkInstanceContext("Nodus Renderer",
            new Version32(1, 0, 0),
            "No Engine",
            new Version32(1, 0, 0),
            Vk.Version13));
    }

    private IVkKhrSurface CreateSurface()
    {
        return new VkKhrSurface(vkContext!, instance!, () => window.VkSurface!);
    }

    private IVkLogicalDevice CreateLogicalDevice(PhysicalDevice device)
    {
        return new VkLogicalDevice(vkContext!, device, surface!);
    }

    private IVkSwapChain CreateSwapChain()
    {
        var supplier = new VkSwapChainContextSupplier(
            () => new VkSurfaceInfo(window.FramebufferSize, physicalDevice!.Value, surface!),
            () => VkQueueInfo.GetFromDevice(physicalDevice!.Value, vk!, surface!));

        var sc = new VkSwapChain(vkContext!,
            new VkSwapChainContext(logicalDevice!, surface!,
                new VkSurfaceFormatRequest(Format.B8G8R8A8Srgb, ColorSpaceKHR.PaceSrgbNonlinearKhr),
                PresentModeKHR.MailboxKhr, supplier), extensionProvider!);
        
        sc.AddDependency(logicalDevice!);

        return sc;
    }
    
    private void Exit()
    {
        window.Close();
    }
    
    private void Cleanup()
    {
        Console.WriteLine("Cleaning the environment up.");
        
        if (useValidationLayers)
        {
            debugUtils!.DestroyDebugUtilsMessenger(instance!, debugMessenger, null);
        }
        
        renderer.Dispose();
        
        swapChain?.Dispose();
        logicalDevice?.Dispose();
        surface?.Dispose();
        
        instance?.Dispose();
        
        layerInfo?.Dispose();
        vkContext?.Dispose();

        vk!.Dispose();
    }
}