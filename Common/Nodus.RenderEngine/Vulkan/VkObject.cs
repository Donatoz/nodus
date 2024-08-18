using Nodus.RenderEngine.Common;

namespace Nodus.RenderEngine.Vulkan;

/// <summary>
/// Represents a base Vulkan managed object, as well as a dependency tree component.
/// </summary>
public class VkObject : RenderContextObject<IVkContext>, IVkUnmanagedHook
{
    public bool IsPresent => !IsDisposing;

    protected IVkContext VkContext { get; private set; } = null!;
    protected bool IsDisposing { get; private set; }

    private IDisposable contextDestructionContract = null!;
    private readonly ISet<IVkUnmanagedHook> dependantObjects;
    
    public VkObject(IVkContext vkContext) : base(vkContext)
    {
        dependantObjects = new HashSet<IVkUnmanagedHook>();

        Retarget(vkContext);
    }

    /// <summary>
    /// Add a dependency to the current VkObject dependency list. Each dependency has to be present during this object disposal process.
    /// </summary>
    /// <param name="another">The IVkUnmanagedHook object representing the dependency to be added.</param>
    public void AddDependency(IVkUnmanagedHook another)
    {
        dependantObjects.Add(another);
    }

    /// <summary>
    /// Remove a dependency from the current VkObject dependency list. This dependency will no longer be required during the object disposal process.
    /// </summary>
    /// <param name="another">The IVkUnmanagedHook object representing the dependency to be removed.</param>
    public void RemoveDependency(IVkUnmanagedHook another)
    {
        dependantObjects.Remove(another);
    }

    public void Retarget(IVkContext newContext)
    {
        contextDestructionContract?.Dispose();
        VkContext?.UnbindObject(this);

        VkContext = newContext;
        contextDestructionContract = VkContext.BindObject(this, OnContextDestruction);
        
        OnContextChanged();
    }

    protected virtual void OnContextChanged() { }

    private void OnContextDestruction()
    {
        if (!IsDisposing)
        {
            throw new Exception($"The context of {this} is being disposed prior to the bound object." +
                                $"{Environment.NewLine}Bound objects:{Environment.NewLine}{VkContext.GetBoundObjectsTrace()}");
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            contextDestructionContract.Dispose();
            VkContext.UnbindObject(this);
        }
    }

    public void Dispose()
    {
        foreach (var dependantObject in dependantObjects)
        {
            if (!dependantObject.IsPresent)
            {
                throw new Exception($"Failed to dispose {this}: a dependant object ({dependantObject}) was already discarded.");
            }
        }
        
        IsDisposing = true;
        Dispose(IsDisposing);
        GC.SuppressFinalize(this);
    }
}