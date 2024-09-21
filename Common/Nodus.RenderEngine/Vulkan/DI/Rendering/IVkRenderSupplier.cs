using System.Numerics;
using System.Reactive.Subjects;
using Silk.NET.Maths;
using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan.DI;

public interface IVkRenderSupplier
{
    IObservable<Vector2D<int>> FrameBufferSizeStream { get; }
    
    Extent2D CurrentRenderExtent { get; }
    Vector2 CurrentFrameBufferScale { get; }
    float FrameTime { get; }
}

public record VkRenderSupplier : IVkRenderSupplier, IDisposable
{
    public IObservable<Vector2D<int>> FrameBufferSizeStream => frameBufferSizeSubject;
    
    public Extent2D CurrentRenderExtent => extentGetter.Invoke();
    public Vector2 CurrentFrameBufferScale => frameBufferScaleGetter.Invoke();
    public float FrameTime { get; private set; }

    private readonly Func<Extent2D> extentGetter;
    private readonly Func<Vector2> frameBufferScaleGetter;
    private readonly Subject<Vector2D<int>> frameBufferSizeSubject;

    public VkRenderSupplier(Func<Extent2D> extentGetter, Func<Vector2> frameBufferScaleGetter)
    {
        this.extentGetter = extentGetter;
        this.frameBufferScaleGetter = frameBufferScaleGetter;
        frameBufferSizeSubject = new Subject<Vector2D<int>>();
    }

    public void UpdateFrameBufferSize(Vector2D<int> size)
    {
        frameBufferSizeSubject.OnNext(size);
    }

    public void UpdateTime(float time)
    {
        FrameTime = time;
    }

    public void Dispose()
    {
        frameBufferSizeSubject.Dispose();
    }
}