using Nodus.RenderEngine.Common;
using Nodus.RenderEngine.Vulkan.Meta;
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

        var createInfo = new DeviceCreateInfo
        {
            SType = StructureType.DeviceCreateInfo,
            PQueueCreateInfos = &firstInfo,
            QueueCreateInfoCount = (uint)queueInfos.Length,
            PEnabledFeatures = &features,
            EnabledExtensionCount = (uint)Context.ExtensionsInfo.RequiredDeviceExtensions.Length,
            PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(Context.ExtensionsInfo.RequiredDeviceExtensions)
        };

        if (Context.LayerInfo != null)
        {
            createInfo.EnabledLayerCount = Context.LayerInfo.Value.EnabledLayersCount;
            createInfo.PpEnabledLayerNames = Context.LayerInfo.Value.EnabledLayerNames;
        }
        else
        {
            createInfo.EnabledLayerCount = 0;
        }

        if (Context.Api.CreateDevice(physicalDevice.WrappedDevice, &createInfo, null, out var device) != Result.Success)
        {
            throw new Exception("Failed to create logical device.");
        }

        queues = familyIndices.ToDictionary(x => x, x => Context.Api.GetDeviceQueue(device, x, 0));
        
        WrappedDevice = device;

        SilkMarshal.Free((nint)createInfo.PpEnabledExtensionNames);
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