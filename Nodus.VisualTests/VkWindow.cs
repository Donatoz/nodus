using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;
using DynamicData;
using ImGuiNET;
using Nodus.RenderEngine.Common;
using Nodus.RenderEngine.Serialization;
using Nodus.RenderEngine.Vulkan;
using Nodus.RenderEngine.Vulkan.Components;
using Nodus.RenderEngine.Vulkan.Computing;
using Nodus.RenderEngine.Vulkan.Convention;
using Nodus.RenderEngine.Vulkan.DI;
using Nodus.RenderEngine.Vulkan.Memory;
using Nodus.RenderEngine.Vulkan.Meta;
using Nodus.RenderEngine.Vulkan.Presentation;
using Nodus.RenderEngine.Vulkan.Rendering;
using Nodus.RenderEngine.Vulkan.Utility;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Input;
using Silk.NET.Input.Extensions;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;
using SixLabors.ImageSharp.PixelFormats;

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
    private IVkPhysicalDevice? physicalDevice;
    private IVkLogicalDevice? logicalDevice;
    private IVkKhrSurface? surface;
    private IVkSwapChain? swapChain;
    private IVkRenderPass? renderPass;
    private IScreenViewer? viewer;
    private IVkRenderPresenter? presenter;
    private VkComputeDispatcher? computeDispatcher;
    private IVkMemoryLessor? memoryLessor;
    private VkImGuiComponent? imGuiComponent;

    private VkLayerInfo? layerInfo;
    private VkExtensionsInfo? extensionsInfo;
    private VkContext? vkContext;
    private IVkExtensionProvider? extensionProvider;
    private IInputContext? input;
    private VkRenderSupplier? renderSupplier;
    private nint? imGuiContext;

    private bool isInitialized;
    private double fps;
    private uint frame;
    private double refreshTime;
    
    private readonly VkGeometryPrimitiveRenderer renderer;

    public VkWindow(bool useValidationLayers = true)
    {
        this.useValidationLayers = useValidationLayers;
        var options = WindowOptions.DefaultVulkan with
        {
            Size = new Vector2D<int>(1440, 810),
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
        logicalDevice = CreateLogicalDevice(physicalDevice);

        var bufferHeapAllocator = new BufferHeapMemoryAllocator(vkContext, logicalDevice);
        
        var depthFormat = ImageUtility.GetSupportedFormat(
            [Format.D32Sfloat, Format.D32SfloatS8Uint, Format.D24UnormS8Uint],
            physicalDevice!, ImageTiling.Optimal, FormatFeatureFlags.DepthStencilAttachmentBit);
        
        memoryLessor = new VkMemoryHeapLessor(vkContext, logicalDevice, physicalDevice, [
            
            new VkMemoryHeapInfo(MemoryGroups.RgbaSampledImageMemory, 
                1024 * 1024 * 64, 
                MemoryPropertyFlags.DeviceLocalBit,
                new ImageHeapMemoryAllocator(vkContext, logicalDevice, Format.R8G8B8A8Srgb, ImageUsageFlags.SampledBit | ImageUsageFlags.TransferDstBit)),
            
            new VkMemoryHeapInfo(MemoryGroups.DepthImageMemory, 
                1024 * 1024 * 16, 
                MemoryPropertyFlags.DeviceLocalBit,
                new ImageHeapMemoryAllocator(vkContext, logicalDevice, depthFormat, ImageUsageFlags.DepthStencilAttachmentBit)),
            
            // TODO: Object buffer memory shall be device local.
            new VkMemoryHeapInfo(MemoryGroups.ObjectBufferMemory, 
                1024 * 1024 * 16,
                MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit,
                bufferHeapAllocator),
            
            new VkMemoryHeapInfo(MemoryGroups.ComputeStorageMemory, 
                1024 * 1024 * 16,
                MemoryPropertyFlags.DeviceLocalBit,
                bufferHeapAllocator),
            
            new VkMemoryHeapInfo(MemoryGroups.StagingStorageMemory, 
                1024 * 1024 * 8,
                MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit,
                bufferHeapAllocator)
        ]);

        var serviceContainer = new VkRenderServiceContainer
        {
            MemoryLessor = memoryLessor,
            Devices = new VkDeviceContainer(logicalDevice, physicalDevice),
            Dispatcher = renderer
        };
        
        vkContext.BindServices(serviceContainer);

        var queueInfo = VkQueueInfo.GetFromDevice(physicalDevice.WrappedDevice, vk, surface);
        var geoFactory = new AssimpGeometryFactory();

        var cube = geoFactory.CreateFromFile(@"G:\CG\3D\Common\Sphere.obj").First();

        extensionProvider = new VkExtensionProvider(vkContext, instance, logicalDevice);
        
        swapChain = CreateSwapChain();
        renderPass = new VkRenderPass(vkContext, logicalDevice, new VkRenderPassContext(swapChain.SurfaceFormat.Format, swapChain.DepthFormat, CreatePassFactory()));
        
        swapChain.CreateFrameBuffers(renderPass.WrappedPass);
        renderSupplier = new VkRenderSupplier(() => swapChain.Extent, 
            () => new Vector2((float)window.FramebufferSize.X / window.Size.X, (float)window.FramebufferSize.Y / window.Size.Y));

        imGuiContext = ImGui.CreateContext();
        ImGui.SetCurrentContext(imGuiContext.Value);
        ImGui.StyleColorsDark();

        imGuiComponent = new VkImGuiComponent(vkContext,
            new VkImGuiComponentContext([
                new ShaderDefinition(
                    new ShaderFileSource(@"C:\Users\Donatoz\RiderProjects\Nodus\Nodus.VisualTests\Assets\Shaders\compiled\imgui.vert.spv"),
                    ShaderSourceType.Vertex),
                new ShaderDefinition(
                    new ShaderFileSource(@"C:\Users\Donatoz\RiderProjects\Nodus\Nodus.VisualTests\Assets\Shaders\compiled\imgui.frag.spv"),
                    ShaderSourceType.Fragment)
            ], renderSupplier, swapChain.SurfaceFormat.Format, swapChain.DepthFormat, 2, 
                new VkImageLayoutTransition(ImageLayout.ColorAttachmentOptimal, ImageLayout.PresentSrcKhr), RenderGui));

        var pipelineContext = new VkGraphicsPipelineContext(
            [DynamicState.Scissor ,DynamicState.Viewport],
            [
                new ShaderDefinition(
                    new ShaderFileSource(@"C:\Users\Donatoz\RiderProjects\Nodus\Nodus.VisualTests\Assets\Shaders\compiled\standard2.vert.spv"),
                    ShaderSourceType.Vertex),
                new ShaderDefinition(
                    new ShaderFileSource(@"C:\Users\Donatoz\RiderProjects\Nodus\Nodus.VisualTests\Assets\Shaders\compiled\solid.frag.spv"),
                    ShaderSourceType.Fragment)
            ], 
            renderPass, renderSupplier);

        viewer = new Viewer(new Vector2D<float>(swapChain.Extent.Width, swapChain.Extent.Height))
        {
            Position = Vector3D<float>.UnitZ * -5
        };

        var textureSource =
            new TextureFileSource(
                @"C:\Users\Donatoz\RiderProjects\Nodus\Common\Nodus.RenderEngine.Avalonia\Assets\Textures\Noise_008.png");
        var textureSource2 =
            new TextureFileSource(
                @"C:\Users\Donatoz\RiderProjects\Nodus\Common\Nodus.RenderEngine.Avalonia\Assets\Textures\Noise_077.png");
        var textureDataProvider = new TextureDataProvider<Rgba32>();

        using var texture = textureDataProvider.FetchTexture(textureSource);
        using var texture2 = textureDataProvider.FetchTexture(textureSource2);
        //presenter = new VkImagePresenter(vkContext, logicalDevice, physicalDevice, rendererSupplier, renderPass, queueInfo, 2);
        presenter = new VkSwapchainRenderPresenter(vkContext, logicalDevice, swapChain, surface, renderPass, 2);
        
        renderer.Initialize(
            new VkGeometryPrimitiveRenderContext(cube, logicalDevice, physicalDevice, queueInfo, 
                renderPass, viewer, pipelineContext, [texture, texture2], presenter, renderSupplier, 2, 
                [imGuiComponent]),
            new VkRenderBackendProvider(vkContext)
        );
        
        computeDispatcher = new VkComputeDispatcher(vkContext, logicalDevice,
            new VkComputeDispatcherContext(
                new ShaderDefinition(
                    new ShaderFileSource(
                        @"C:\Users\Donatoz\RiderProjects\Nodus\Nodus.VisualTests\Assets\Shaders\compiled\test.comp.spv"),
                    ShaderSourceType.Compute), physicalDevice, queueInfo));
        
        isInitialized = true;

        //computeDispatcher.Dispatch();
    }

    private List<IVkMemoryLease> debugLeases = new();

    private void OnKeyDown(IKeyboard keyboard, Key key, int n)
    {
        if (key == Key.Escape)
        {
            Exit();
        }

        if (key == Key.L)
        {
            debugLeases.Add(memoryLessor!.LeaseMemory(MemoryGroups.StagingStorageMemory, (ulong)Random.Shared.Next(2000) * 1024));
        }

        if (key == Key.O)
        {
            debugLeases[Random.Shared.Next(debugLeases.Count)].Dispose();
        }

        if (key == Key.H)
        {
            HeapVisualizer.Visualize(memoryLessor!.AllocatedHeaps.ToArray(), @"J:\heaps.png");
        }
    }

    private void RenderGui()
    {
        if (ImGui.Begin("Debug"))
        {

            ImGui.SeparatorText("Stats");

            ImGui.BeginGroup();
            ImGui.Text($"FPS: {fps:0.00}");
            ImGui.EndGroup();

            ImGui.SeparatorText("Memory");

            ImGui.Text("Heaps");

            if (ImGui.BeginTable("heaps", 3, ImGuiTableFlags.Borders))
            {
                ImGui.TableHeadersRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.TextUnformatted("Heap Id");
                ImGui.TableSetColumnIndex(1);
                ImGui.TextUnformatted("Occupied (%)");
                ImGui.TableSetColumnIndex(2);
                ImGui.TextUnformatted("Fragmentation (%)");

                foreach (var heap in memoryLessor!.AllocatedHeaps)
                {
                    ImGui.TableNextRow();

                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text(heap.Meta.HeapId);
                    if (ImGui.Button($"Debug##{heap.Meta.HeapId}"))
                    {
                        (heap as VkFixedMemoryHeap)?.DebugMemory();
                    }
                    ImGui.SameLine();
                    if (ImGui.Button($"Defrag##{heap.Meta.HeapId}"))
                    {
                        (heap as VkFixedMemoryHeap)?.Defragment();
                    }
                    
                    ImGui.SetWindowFontScale(1.2f);
                    ImGui.TableSetColumnIndex(1);
                    ImGui.TextUnformatted(((double)heap.GetOccupiedMemory() / heap.Meta.Size * 100).ToString("0.00"));
                    
                    ImGui.TableSetColumnIndex(2);
                    ImGui.TextUnformatted((heap.GetCurrentFragmentation() * 100).ToString("0.00"));
                    ImGui.SetWindowFontScale(1);
                }

                ImGui.EndTable();
            }
            
            ImGui.End();
        }
    }

    private void OnRender(double delta)
    {
        if (!isInitialized || window.FramebufferSize.X == 0 || window.FramebufferSize.Y == 0) return;
        
        renderer.RenderFrame();
        frame++;
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
        
        refreshTime += delta;

        if (refreshTime > 1.0)
        {
            fps = frame / refreshTime;

            frame = 0;
            refreshTime = 0;
        }
        
        UpdateImGuiInput();
    }

    private void UpdateImGuiInput()
    {
        if (input is not { Keyboards.Count: > 0, Mice.Count: > 0 }) return;
        
        var io = ImGui.GetIO();
        var keyboard = input.Keyboards[0];
        var mouse = input.Mice[0];

        io.MouseDown[0] = mouse.IsButtonPressed(MouseButton.Left);
        io.MouseDown[1] = mouse.IsButtonPressed(MouseButton.Right);
        io.MouseDown[2] = mouse.IsButtonPressed(MouseButton.Middle);
        
        io.MousePos = new Vector2(mouse.Position.X, mouse.Position.Y);

        var wheel = mouse.ScrollWheels[0];
        io.MouseWheel = wheel.Y;
        io.MouseWheelH = wheel.X;
    }
    
    private void OnFrameBufferResize(Vector2D<int> size)
    {
        if (viewer != null)
        {
            viewer.ScreenSize = new Vector2D<float>(size.X, size.Y);
        }
        
        renderSupplier?.UpdateFrameBufferSize(size);
    }
    
    private void OnClosing()
    {
        Cleanup();
    }

    private IVkRenderPassFactory CreatePassFactory()
    {
        return new VkRenderPassFactory
        {
            DescriptionsFactory = (colFormat, depthFormat) => [
                new AttachmentDescription
                {
                    Format = colFormat,
                    Samples = SampleCountFlags.Count1Bit,
                    LoadOp = AttachmentLoadOp.Clear,
                    StoreOp = AttachmentStoreOp.Store,
                    StencilLoadOp = AttachmentLoadOp.DontCare,
                    StencilStoreOp = AttachmentStoreOp.DontCare,
                    InitialLayout = ImageLayout.Undefined,
                    FinalLayout = ImageLayout.ColorAttachmentOptimal
                },
                new AttachmentDescription
                {
                    Format = depthFormat,
                    Samples = SampleCountFlags.Count1Bit,
                    LoadOp = AttachmentLoadOp.Clear,
                    StoreOp = AttachmentStoreOp.DontCare,
                    StencilLoadOp = AttachmentLoadOp.DontCare,
                    StencilStoreOp = AttachmentStoreOp.DontCare,
                    InitialLayout = ImageLayout.Undefined,
                    FinalLayout = ImageLayout.DepthStencilAttachmentOptimal
                }
            ]
        };
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

    private IVkPhysicalDevice GetFirstSuitablePhysicalDevice()
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
        
        return new VkPhysicalDevice(vkContext!, device, surface);
    }
    
    private bool IsDeviceSuitable(PhysicalDevice device)
    {
        var indices = VkQueueInfo.GetFromDevice(device, vk!, surface!);

        var extensionsSupported = CheckDeviceExtensions(device);
        var features = vk!.GetPhysicalDeviceFeatures(device);

        return indices.IsComplete() && extensionsSupported && features.SamplerAnisotropy;
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

    private IVkLogicalDevice CreateLogicalDevice(IVkPhysicalDevice device)
    {
        return new VkLogicalDevice(vkContext!, device, surface!);
    }

    private IVkSwapChain CreateSwapChain()
    {
        var supplier = new VkSwapChainContextSupplier(
            () => new VkSurfaceInfo(window.FramebufferSize, physicalDevice!.WrappedDevice, surface!),
            () => VkQueueInfo.GetFromDevice(physicalDevice!.WrappedDevice, vk!, surface!));

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
        
        imGuiComponent?.Dispose();
        renderer.Dispose();
        presenter?.Dispose();
        computeDispatcher?.Dispose();
        
        renderPass?.Dispose();
        swapChain?.Dispose();
        surface?.Dispose();
        
        layerInfo?.Dispose();
        memoryLessor?.Dispose();
        logicalDevice?.Dispose();
        renderSupplier?.Dispose();
        
        instance?.Dispose();
        vkContext?.Dispose();

        vk!.Dispose();

        if (imGuiContext != null)
        {
            ImGui.DestroyContext(imGuiContext.Value);
        }
    }
}