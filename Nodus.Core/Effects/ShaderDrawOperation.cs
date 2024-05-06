using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Nodus.Core.Extensions;
using SkiaSharp;

namespace Nodus.Core.Effects;

public class ShaderDrawOperation : ICustomDrawOperation
{
    public Rect Bounds => container.Bounds;
    
    /// <summary>
    /// Object screen-space position. Typically the position of object <see cref="Bounds"/>.
    /// </summary>
    public Vector2 ObjectOffset { get; set; }
    /// <summary>
    /// Object screen-space size. Typically the size of object <see cref="Bounds"/>.
    /// </summary>
    public Vector2 ObjectSize { get; set; }
    public bool IsOpaque { get; set; }
    public float Time { get; set; }
    public IEnumerable<IShaderUniform>? UserUniforms { get; set; }

    private Bitmap? targetBitmap;
    public Bitmap? TargetBitmap
    {
        get => targetBitmap;
        set { targetBitmap = value; UpdateChildren(); }
    }

    private readonly Visual container;
    private readonly CompositeDisposable disposables;
    
    private readonly SKRuntimeEffect effect;
    private readonly float[] offsetBuffer;
    private readonly float[] sizeBuffer;
    
    protected SKRuntimeEffectUniforms Uniforms { get; }
    protected SKRuntimeEffectChildren Children { get; }
    
    private SKShader? targetShader;

    public ShaderDrawOperation(Visual container, string shaderSource)
    {
        disposables = new CompositeDisposable();
        
        effect = SKRuntimeEffect.Create(shaderSource, out var err);
        
        if (err != null)
        {
            throw new Exception(err);
        }
        
        disposables.Add(effect);
        
        Uniforms = new SKRuntimeEffectUniforms(effect);
        Children = new SKRuntimeEffectChildren(effect);

        offsetBuffer = new float[2];
        sizeBuffer = new float[2];
        
        this.container = container;
    }

    public void Render(ImmediateDrawingContext context)
    {
        var leaseFeat = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();

        if (leaseFeat != null)
        {
            using var lease = leaseFeat.Lease();
            RenderSurface(lease);
        }
    }

    private void UpdateChildren()
    {
        Children.Reset();

        if (TargetBitmap != null)
        {
            var width = TargetBitmap.PixelSize.Width;
            var height = TargetBitmap.PixelSize.Height;
            
            using var skb = new SKBitmap(width, height);
            var stride = (width * (TargetBitmap.Format?.BitsPerPixel ?? 32) + 7) / 8;
            
            TargetBitmap.CopyPixels(new PixelRect(0, 0, width, height), 
                skb.GetPixels(), stride * height, stride);
            
            targetShader?.Dispose();
            targetShader = skb.ToShader(SKShaderTileMode.Repeat, SKShaderTileMode.Clamp);
            
            Children.Add("input", targetShader);
        }
    }
    
    private void RenderSurface(ISkiaSharpApiLease lease)
    {
        UpdateUniforms();
        
        using var mainShader = effect.ToShader(IsOpaque, Uniforms, Children);
        
        using var p = new SKPaint();
        p.Shader = mainShader;
        
        RenderObject(p, lease);
    }

    /// <summary>
    /// Update the uniforms for the shader.
    /// </summary>
    protected virtual void UpdateUniforms()
    {
        UserUniforms?.ForEach(x => Uniforms[x.UniformName] = x.UniformValueGetter.Invoke());

        offsetBuffer[0] = ObjectOffset.X;
        offsetBuffer[1] = ObjectOffset.Y;
        sizeBuffer[0] = ObjectSize.X;
        sizeBuffer[1] = ObjectSize.Y;

        Uniforms["objectPosition"] = offsetBuffer;
        Uniforms["objectSize"] = sizeBuffer;

        if (Time > 0)
        {
            Uniforms["time"] = Time;
        }
    }

    /// <summary>
    /// Render the object with the specified paint and API lease.
    /// </summary>
    /// <param name="paint">The <see cref="SKPaint"/> object used to render the object.</param>
    /// <param name="lease">The <see cref="ISkiaSharpApiLease"/> object used to access the SkiaSharp API.</param>
    protected virtual void RenderObject(SKPaint paint, ISkiaSharpApiLease lease)
    {
        lease.SkCanvas.DrawRect(Bounds.ToSKRect(), paint);
    }

    public bool HitTest(Point p) => container.Bounds.Contains(p);

    public bool Equals(ICustomDrawOperation? other) => this == other;
    
    public virtual void Dispose()
    {
        disposables.Dispose();
        targetShader?.Dispose();
        effect.Dispose();
        Uniforms.Reset();
        Children.Reset();
    }
}