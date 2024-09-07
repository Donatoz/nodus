using System.Reactive.Linq;
using System.Reactive.Subjects;
using Nodus.Core.Extensions;
using Nodus.RenderEngine.Vulkan.DI;
using Nodus.RenderEngine.Vulkan.Meta;
using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan;

/// <summary>
/// Represents a Vulkan context.
/// </summary>
public interface IVkContext : IDisposable
{
    /// <summary>
    /// Bound Vulkan API.
    /// </summary>
    Vk Api { get; }

    /// <summary>
    /// Represents Vulkan layer information.
    /// </summary>
    VkLayerInfo? LayerInfo { get; }

    /// <summary>
    /// The extensions' information.
    /// </summary>
    VkExtensionsInfo ExtensionsInfo { get; }
    IVkServiceContainer ServiceContainer { get; }

    /// <summary>
    /// A collection of bound Vulkan objects in a Vulkan context.
    /// </summary>
    /// <remarks>
    /// This property provides access to the collection of Vulkan objects that are currently bound to the Vulkan context.
    /// </remarks>
    IReadOnlyCollection<VkObject> BoundObjects { get; }

    /// <summary>
    /// Bind a VkObject to the VkContext and registers an action to be invoked when the VkContext is being destroyed.
    /// </summary>
    /// <param name="vkObject">The VkObject to be bound.</param>
    /// <param name="onContextDestruction">The action to be invoked when the VkContext is being destroyed.</param>
    /// <returns>An IDisposable object representing the binding. When disposed, the action will be unregistered.</returns>
    IDisposable BindObject(VkObject vkObject, Action onContextDestruction);

    /// <summary>
    /// Unbinds the specified VkObject from the VkContext.
    /// </summary>
    /// <param name="vkObject">The VkObject to unbind.</param>
    void UnbindObject(VkObject vkObject);
}

public class VkContext : IVkContext
{
    public Vk Api { get; }
    public VkLayerInfo? LayerInfo { get; }
    public VkExtensionsInfo ExtensionsInfo { get; }
    public IReadOnlyCollection<VkObject> BoundObjects => boundObjects;
    public IVkServiceContainer ServiceContainer => serviceContainer.NotNull("Vulkan service container was not initialized. " +
                                                                            "Ensure that it was initialized right after the devices.");

    private readonly Subject<bool> lifetimeSubject;
    private readonly HashSet<VkObject> boundObjects;
    private IVkServiceContainer? serviceContainer;

    public VkContext(Vk api, VkExtensionsInfo extensionsInfo, VkLayerInfo? layerInfo = null)
    {
        lifetimeSubject = new Subject<bool>();
        boundObjects = new HashSet<VkObject>();

        Api = api;
        ExtensionsInfo = extensionsInfo;
        LayerInfo = layerInfo;
    }

    public void BindServices(IVkServiceContainer container)
    {
        serviceContainer = container;
    }
    
    public IDisposable BindObject(VkObject vkObject, Action onContextDestruction)
    {
        boundObjects.Add(vkObject);
        return lifetimeSubject.Where(x => x).Subscribe(_ => onContextDestruction.Invoke());
    }

    public void UnbindObject(VkObject vkObject)
    {
        boundObjects.Remove(vkObject);
    }

    public void Dispose()
    {
        lifetimeSubject.OnNext(true);
        lifetimeSubject.Dispose();
    }
}

public static class VkContextExtensions
{
    public static string GetBoundObjectsTrace(this IVkContext vkContext)
    {
        return string.Join(Environment.NewLine, vkContext.BoundObjects);
    }
}