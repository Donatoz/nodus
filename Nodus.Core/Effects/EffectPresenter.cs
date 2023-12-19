using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Nodus.Core.Extensions;

namespace Nodus.Core.Effects;

public class EffectPresenter : Control
{
    public static readonly StyledProperty<Uri?> ShaderSourceProperty;
    public static readonly StyledProperty<bool> IsOpaqueProperty; 
    public static readonly StyledProperty<Bitmap?> BitmapProperty;
    public static readonly StyledProperty<bool> IsUpdatableProperty;
    public static readonly StyledProperty<float> UpdateSpeedProperty;

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
    
    private ShaderDrawOperation? drawOperation;
    private const float RenderTimerTick = 3f;
    private float time;
    
    protected static DispatcherTimer EffectRenderTimer { get; }
    
    static EffectPresenter()
    {
        ShaderSourceProperty = AvaloniaProperty.Register<EffectPresenter, Uri?>(nameof(ShaderSource));
        BitmapProperty = AvaloniaProperty.Register<EffectPresenter, Bitmap?>(nameof(Bitmap));
        IsOpaqueProperty = AvaloniaProperty.Register<EffectPresenter, bool>(nameof(IsOpaque));
        IsUpdatableProperty = AvaloniaProperty.Register<EffectPresenter, bool>(nameof(IsUpdatable));
        UpdateSpeedProperty = AvaloniaProperty.Register<EffectPresenter, float>(nameof(UpdateSpeed), 300);
        
        EffectRenderTimer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(RenderTimerTick)
        };
        EffectRenderTimer.Start();
        
        AffectsRender<EffectPresenter>(ShaderSourceProperty, IsOpaqueProperty, IsUpdatableProperty);
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        
        TryUpdateSource();

        if (IsUpdatable)
        {
            EffectRenderTimer.Tick += OnEffectRenderTimerTick;
        }
    }

    private void TryUpdateSource()
    {
        drawOperation?.Dispose();

        if (ShaderSource != null)
        {
            if (!AssetLoader.Exists(ShaderSource))
            {
                throw new Exception($"Shader source asset ({ShaderSource.PathAndQuery}) was not found");
            }

            using var stream = AssetLoader.Open(ShaderSource);
            using var reader = new StreamReader(stream);
            
            drawOperation = CreateOperation(reader.ReadToEnd());
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
        time += RenderTimerTick / UpdateSpeed % 100;
        InvalidateVisual();
    }

    protected virtual ShaderDrawOperation CreateOperation(string shaderSource)
    {
        return new ShaderDrawOperation(this, shaderSource);
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        
        drawOperation?.Dispose();

        if (IsUpdatable)
        {
            EffectRenderTimer.Tick -= OnEffectRenderTimerTick;
        }
    }
}