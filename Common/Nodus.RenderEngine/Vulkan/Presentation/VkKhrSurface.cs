using Silk.NET.Core.Contexts;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace Nodus.RenderEngine.Vulkan.Presentation;

/// <summary>
/// Represents a combination of the wrapped, unmanaged Vulkan surface (KHR) and its extension interface.
/// </summary>
public interface IVkKhrSurface : IDisposable
{
    /// <summary>
    /// The extension interface.
    /// </summary>
    KhrSurface Extension { get; }

    /// <summary>
    /// The wrapped, unmanaged Vulkan surface (KHR).
    /// </summary>
    SurfaceKHR SurfaceKhr { get; }

    void Update();
}

public class VkKhrSurface : VkObject, IVkKhrSurface
{
    public KhrSurface Extension { get; }
    public SurfaceKHR SurfaceKhr { get; private set; }
    
    private readonly IVkInstance instance;
    private readonly Func<IVkSurface> surfaceProvider;
    
    public VkKhrSurface(IVkContext vkContext, IVkInstance instance, Func<IVkSurface> surfaceProvider) : base(vkContext)
    {
        this.instance = instance;
        this.surfaceProvider = surfaceProvider;

        if (!Context.Api.TryGetInstanceExtension<KhrSurface>(instance.WrappedInstance, out var kSurface))
        {
            throw new Exception("Failed to retrieve KHR_surface extension.");
        }
        
        Extension = kSurface;

        Update();
    }

    public unsafe void Update()
    {
        Extension.DestroySurface(instance.WrappedInstance, SurfaceKhr, null);
        
        var surfaceK = surfaceProvider.Invoke().Create<AllocationCallbacks>(instance.WrappedInstance.ToHandle(), null).ToSurface();
        SurfaceKhr = surfaceK;
    }

    protected override unsafe void Dispose(bool disposing)
    {
        if (IsDisposing)
        {
            Extension.DestroySurface(instance.WrappedInstance, SurfaceKhr, null);
        }
        
        base.Dispose(disposing);
    }
}