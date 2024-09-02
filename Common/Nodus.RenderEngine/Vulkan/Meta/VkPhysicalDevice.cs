using Silk.NET.Core.Native;
using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan.Meta;

public record VkPhysicalDeviceFeatures
{
    public bool TesselationShader { get; init; }
    public bool GeometryShader { get; init; }
}

public record VkPhysicalDeviceLimits
{
    public float MaxSamplerAnisotropy { get; init; }
}

public record VkPhysicalDeviceProperties
{
    public uint ApiVersion { get; init; }
    public uint DriverVersion { get; init; }
    public uint VendorId { get; init; }
    public uint DeviceId { get; init; }
    public string DeviceName { get; init; } = string.Empty;
    public PhysicalDeviceType Type { get; init; }
    public VkPhysicalDeviceLimits Limits { get; init; } = new();
}

public interface IVkPhysicalDevice
{ 
    PhysicalDevice WrappedDevice { get; } 
    VkPhysicalDeviceProperties Properties { get; } 
    VkPhysicalDeviceFeatures Features { get; }
    PhysicalDeviceMemoryProperties MemoryProperties { get; }
}

public record VkPhysicalDevice : IVkPhysicalDevice
{
    public PhysicalDevice WrappedDevice { get; }
    public VkPhysicalDeviceProperties Properties { get; }
    public VkPhysicalDeviceFeatures Features { get; }
    public PhysicalDeviceMemoryProperties MemoryProperties { get; }

    public unsafe VkPhysicalDevice(IVkContext context, PhysicalDevice physicalDevice)
    {
        WrappedDevice = physicalDevice;

        context.Api.GetPhysicalDeviceFeatures(WrappedDevice, out var features);
        context.Api.GetPhysicalDeviceProperties(WrappedDevice, out var props);
        context.Api.GetPhysicalDeviceMemoryProperties(WrappedDevice, out var memoryProperties);
        
        MemoryProperties = memoryProperties;
        
        Features = new VkPhysicalDeviceFeatures
        {
            TesselationShader = features.TessellationShader,
            GeometryShader = features.GeometryShader
        };

        Properties = new VkPhysicalDeviceProperties
        {
            ApiVersion = props.ApiVersion,
            DriverVersion = props.DriverVersion,
            VendorId = props.VendorID,
            DeviceId = props.DeviceID,
            Type = props.DeviceType,
            DeviceName = SilkMarshal.PtrToString((nint)props.DeviceName) ?? string.Empty,
            Limits = new VkPhysicalDeviceLimits
            {
                MaxSamplerAnisotropy = props.Limits.MaxSamplerAnisotropy
            }
        };
    }
}