using Nodus.RenderEngine.Vulkan.Extensions;
using Nodus.RenderEngine.Vulkan.Meta;
using Nodus.RenderEngine.Vulkan.Presentation;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan;

/// <summary>
/// Represents a managed Vulkan logical device.
/// </summary>
public interface IVkLogicalDevice : IVkUnmanagedHook
{
    /// <summary>
    /// Logical device queues.
    /// </summary>
    IReadOnlyDictionary<uint, Queue> Queues { get; }
    /// <summary>
    /// The wrapped logical device object.
    /// </summary>
    Device WrappedDevice { get; }
}

public unsafe class VkLogicalDevice : VkObject, IVkLogicalDevice
{
    public IReadOnlyDictionary<uint, Queue> Queues => queues;
    public Device WrappedDevice { get; }

    private readonly Dictionary<uint, Queue> queues;
    
    public VkLogicalDevice(IVkContext context, IVkPhysicalDevice physicalDevice, IVkKhrSurface surface) : base(context)
    {
        var queueInfo = VkQueueInfo.GetFromDevice(physicalDevice.WrappedDevice, Context.Api, surface);

        queueInfo.ThrowIfIncomplete();

        var priority = 1.0f;
        var familyIndices = queueInfo.GetFamilies().Distinct().ToArray();
        var queueInfos = new DeviceQueueCreateInfo[familyIndices.Length];

        for (var i = 0; i < familyIndices.Length; i++)
        {
            queueInfos[i] = new DeviceQueueCreateInfo
            {
                SType = StructureType.DeviceQueueCreateInfo,
                QueueFamilyIndex = familyIndices[i],
                QueueCount = 1,
                PQueuePriorities = &priority
            };
        }

        var features = new PhysicalDeviceFeatures
        {
            SamplerAnisotropy = Vk.True
        };
        var firstInfo = queueInfos[0];
        
        using var createChain = Chain.Create
        <
            DeviceCreateInfo, 
            PhysicalDeviceDescriptorIndexingFeatures,
            PhysicalDeviceDynamicRenderingFeatures,
            PhysicalDeviceSynchronization2Features
        >();
        
        createChain.HeadRef.PQueueCreateInfos = &firstInfo;
        createChain.HeadRef.QueueCreateInfoCount = (uint)queueInfos.Length;
        createChain.HeadRef.PEnabledFeatures = &features;
        createChain.HeadRef.EnabledExtensionCount = (uint)Context.ExtensionsInfo.RequiredDeviceExtensions.Length;
        createChain.HeadRef.PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(Context.ExtensionsInfo.RequiredDeviceExtensions);

        createChain.Item1Ref.RuntimeDescriptorArray = Vk.True;
        createChain.Item1Ref.ShaderSampledImageArrayNonUniformIndexing = Vk.True;
        createChain.Item1Ref.DescriptorBindingVariableDescriptorCount = Vk.True;
        createChain.Item1Ref.DescriptorBindingPartiallyBound = Vk.True;

        createChain.Item2Ref.DynamicRendering = Vk.True;
        
        createChain.Item3Ref.Synchronization2 = Vk.True;

        if (Context.LayerInfo != null)
        {
            createChain.HeadRef.EnabledLayerCount = Context.LayerInfo.Value.EnabledLayersCount;
            createChain.HeadRef.PpEnabledLayerNames = Context.LayerInfo.Value.EnabledLayerNames;
        }
        else
        {
            createChain.HeadRef.EnabledLayerCount = 0;
        }
        
        Context.Api.CreateDevice(physicalDevice.WrappedDevice, createChain.HeadRef, null, out var device)
            .TryThrow("Failed to create logical device.");

        queues = familyIndices.ToDictionary(x => x, x => Context.Api.GetDeviceQueue(device, x, 0));
        
        WrappedDevice = device;

        SilkMarshal.Free((nint)createChain.HeadRef.PpEnabledExtensionNames);
    }

    public static implicit operator Device(VkLogicalDevice logicalDevice)
    {
        return logicalDevice.WrappedDevice;
    }
    
    protected override void Dispose(bool isDisposing)
    {
        if (IsDisposing)
        {
            Context.Api.DestroyDevice(this, null);
        }
        
        base.Dispose(isDisposing);
    }
}