using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Rendering.Composition;
using Avalonia.Vulkan;

namespace Nodus.RenderEngine.Avalonia.Vulkan;

public class VkRenderSurface : Control
{
    private CompositionDrawingSurface surface;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Initialize();
    }

    private async void Initialize()
    {
        var visual = ElementComposition.GetElementVisual(this)!;
        var compositor = visual.Compositor;

        surface = compositor.CreateDrawingSurface();
        var surfaceVisual = compositor.CreateSurfaceVisual();
        surfaceVisual.Size = new Vector(Bounds.Width, Bounds.Height);
        surfaceVisual.Surface = surface;
        
        ElementComposition.SetElementChildVisual(this, surfaceVisual);

        var interop = await compositor.TryGetCompositionGpuInterop();
        if (interop == null)
        {
            throw new Exception("Failed to initialize Vulkan surface: GPU interop is not available.");
        }
    }

    private void InitializeResources(ICompositionGpuInterop gpuInterop)
    {
    }
}