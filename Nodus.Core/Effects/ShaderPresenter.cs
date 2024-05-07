using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Nodus.Core.Utility;
using Vector = Avalonia.Vector;

namespace Nodus.Core.Effects;

public class ShaderPresenter : Control
{
    public static readonly StyledProperty<Uri?> ShaderSourceProperty;
    public static readonly StyledProperty<bool> IsOpaqueProperty; 
    public static readonly StyledProperty<Bitmap?> BitmapProperty;
    public static readonly StyledProperty<bool> IsUpdatableProperty;
    public static readonly StyledProperty<float> UpdateSpeedProperty;
    public static readonly StyledProperty<Uri?> UniformsSchemeProperty;
    public static readonly StyledProperty<object?> UniformsOverrideProperty;
    public static readonly StyledProperty<Control?> SurfaceProperty;

    public Uri? ShaderSource
    {
        get => GetValue(ShaderSourceProperty);
        set => SetValue(ShaderSourceProperty, value);
    }

    public Bitmap? Bitmap
    {
        get => GetValue(BitmapProperty);
        set => SetValue(BitmapProperty, value);
    }

    public bool IsOpaque
    {
        get => GetValue(IsOpaqueProperty);
        set => SetValue(IsOpaqueProperty, value);
    }

    public bool IsUpdatable
    {
        get => GetValue(IsUpdatableProperty);
        set => SetValue(IsUpdatableProperty, value);
    }

    public float UpdateSpeed
    {
        get => GetValue(UpdateSpeedProperty);
        set => SetValue(UpdateSpeedProperty, value);
    }

    public Uri? UniformsScheme
    {
        get => GetValue(UniformsSchemeProperty);
        set => SetValue(UniformsSchemeProperty, value);
    }

    public object? UniformsOverride
    {
        get => GetValue(UniformsOverrideProperty);
        set => SetValue(UniformsOverrideProperty, value);
    }

    public Control? Surface
    {
        get => GetValue(SurfaceProperty);
        set => SetValue(SurfaceProperty, value);
    }

    private ShaderDrawOperation? drawOperation;
    private const float RenderTick = 3f;
    private float time;

    private readonly IShaderUniformFactory uniformFactory;
    private RenderTargetBitmap? surfaceBitmap;
    
    protected static DispatcherTimer EffectRenderTimer { get; }
    
    static ShaderPresenter()
    {
        ShaderSourceProperty = AvaloniaProperty.Register<ShaderPresenter, Uri?>(nameof(ShaderSource));
        BitmapProperty = AvaloniaProperty.Register<ShaderPresenter, Bitmap?>(nameof(Bitmap));
        IsOpaqueProperty = AvaloniaProperty.Register<ShaderPresenter, bool>(nameof(IsOpaque));
        IsUpdatableProperty = AvaloniaProperty.Register<ShaderPresenter, bool>(nameof(IsUpdatable));
        UpdateSpeedProperty = AvaloniaProperty.Register<ShaderPresenter, float>(nameof(UpdateSpeed), 300);
        UniformsSchemeProperty = AvaloniaProperty.Register<ShaderPresenter, Uri?>(nameof(UniformsScheme));
        UniformsOverrideProperty = AvaloniaProperty.Register<ShaderPresenter, object?>(nameof(UniformsOverride));
        SurfaceProperty = AvaloniaProperty.Register<ShaderPresenter, Control?>(nameof(Surface));
        
        EffectRenderTimer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(RenderTick)
        };
        EffectRenderTimer.Start();
        
        AffectsRender<ShaderPresenter>(ShaderSourceProperty, IsOpaqueProperty, IsUpdatableProperty);
    }

    public ShaderPresenter()
    {
        // TODO: Move to DI container
        uniformFactory = new ShaderUniformFactory();
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        
        TryUpdateSource();
        TryLoadUniformsScheme();
        TryUpdateUniforms(UniformsOverride);

        if (IsUpdatable)
        {
            EffectRenderTimer.Tick += OnEffectRenderTimerTick;
        }

        TryUpdateSurface();
    }

    private void TryUpdateSource()
    {
        drawOperation?.Dispose();

        if (ShaderSource != null)
        {
            drawOperation = CreateOperation(AssetUtility.ReadAsset(ShaderSource));
        }
    }

    private void TryUpdateSurface()
    {
        if (Surface != null)
        {
            var ps = new PixelSize((int) Surface.Width + 1, (int) Surface.Height + 1);
            var s = new Size(Surface.Width, Surface.Height);
            
            surfaceBitmap = new RenderTargetBitmap(ps, new Vector(96, 96));
            
            Surface.Measure(s);
            Surface.Arrange(new Rect(s));
            
            surfaceBitmap.Render(Surface);
            Bitmap = surfaceBitmap;
        }
    }

    private void TryLoadUniformsScheme()
    {
        if (UniformsScheme != null)
        {
            TryUpdateUniforms(AssetUtility.ReadAsset(UniformsScheme));
        }
    }

    private void TryUpdateUniforms(object? uniformsObject)
    {
        if (drawOperation == null) return;

        switch (uniformsObject)
        {
            case string s:
            {
                var spliced = s
                    .Replace(Environment.NewLine, string.Empty)
                    .Replace(" ", string.Empty)
                    .Split(';');

                drawOperation.UserUniforms ??= new HashSet<IShaderUniform>();
                drawOperation.UserUniforms = drawOperation.UserUniforms.Concat(spliced
                    .Select(x => uniformFactory.Create(x))
                    .Where(x => x != null)
                    .Cast<IShaderUniform>());
                break;
            }
            case IEnumerable<IShaderUniform> uniforms:
                drawOperation.UserUniforms = uniforms;
                break;
        }
    }

    public sealed override void Render(DrawingContext context)
    {
        base.Render(context);
        
        if (drawOperation == null) return;
        
        PrepareDraw();
        
        context.Custom(drawOperation);
    }

    protected virtual void PrepareDraw()
    {
        if (Bitmap != null && drawOperation!.TargetBitmap != Bitmap)
        {
            drawOperation.TargetBitmap = Bitmap;
        }

        if (IsUpdatable)
        {
            drawOperation!.Time = time;
        }

        drawOperation!.IsOpaque = IsOpaque;
        drawOperation.ObjectOffset = new Vector2((float) Bounds.X, (float) Bounds.Y);
        drawOperation.ObjectSize = new Vector2((float) Bounds.Width, (float) Bounds.Height);
    }

    private void OnEffectRenderTimerTick(object? sender, EventArgs e)
    {
        time += RenderTick / UpdateSpeed % 100;
        InvalidateVisual();
    }

    protected virtual ShaderDrawOperation CreateOperation(string shaderSource)
    {
        return new ShaderDrawOperation(this, shaderSource);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        
        drawOperation?.Dispose();
        surfaceBitmap?.Dispose();

        if (IsUpdatable)
        {
            EffectRenderTimer.Tick -= OnEffectRenderTimerTick;
        }
    }
}