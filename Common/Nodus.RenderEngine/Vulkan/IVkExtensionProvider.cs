using Silk.NET.Core.Native;
using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan;

/// <summary>
/// Provides methods to retrieve Vulkan device and instance extensions.
/// </summary>
public interface IVkExtensionProvider
{
    /// <summary>
    /// Try to get the current device extension of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of the extension.</typeparam>
    /// <param name="extension">The output parameter that will store the extension if it is found.</param>
    /// <returns>True if the extension is found; otherwise, false.</returns>
    bool TryGetCurrentDeviceExtension<T>(out T extension) where T : NativeExtension<Vk>;

    /// <summary>
    /// Try to get the current instance extension of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of the extension to get.</typeparam>
    /// <param name="extension">When this method returns, contains the current instance extension of type T if it exists; otherwise, the default value.</param>
    /// <returns>true if the current instance extension of type T exists; otherwise, false.</returns>
    bool TryGetCurrentInstanceExtension<T>(out T extension) where T : NativeExtension<Vk>;
}

public class VkExtensionProvider : IVkExtensionProvider
{
    private readonly IVkContext context;
    private readonly IVkInstance instance;
    private readonly IVkLogicalDevice device;

    public VkExtensionProvider(IVkContext context, IVkInstance instance, IVkLogicalDevice device)
    {
        this.context = context;
        this.instance = instance;
        this.device = device;
    }
    
    public bool TryGetCurrentDeviceExtension<T>(out T extension) where T : NativeExtension<Vk>
    {
        return context.Api.TryGetDeviceExtension(instance.WrappedInstance, device.WrappedDevice, out extension);
    }

    public bool TryGetCurrentInstanceExtension<T>(out T extension) where T : NativeExtension<Vk>
    {
        return context.Api.TryGetInstanceExtension(instance.WrappedInstance, out extension);
    }
}