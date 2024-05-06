using System;
using Avalonia;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Threading;
using Nodus.Core.Extensions;
using Nodus.RenderEngine.Common;
using Nodus.RenderEngine.OpenGL;
using Silk.NET.OpenGL;

namespace Nodus.RenderEngine.Avalonia;

public class RenderSurface : OpenGlControlBase
{
    public static readonly AvaloniaProperty<GlRendererFactory> RendererFactoryProperty;
    public static readonly AvaloniaProperty<string> VertexShaderSourceProperty;
    public static readonly AvaloniaProperty<string> FragmentShaderSourceProperty;
    
    public string VertexShaderSource
    {
        get => (string) GetValue(VertexShaderSourceProperty).NotNull();
        set => SetValue(VertexShaderSourceProperty, value);
    }
    
    public string FragmentShaderSource
    {
        get => (string) GetValue(FragmentShaderSourceProperty).NotNull();
        set => SetValue(FragmentShaderSourceProperty, value);
    }
    
    public GlRendererFactory RendererFactory
    {
        get => (GlRendererFactory) GetValue(RendererFactoryProperty).NotNull();
        set => SetValue(RendererFactoryProperty, value);
    }
    
    private GL? gl;
    private IRenderer<GL>? renderer;
    
    static RenderSurface()
    {
        RendererFactoryProperty = AvaloniaProperty.Register<RenderSurface, GlRendererFactory>(nameof(RendererFactory));
        VertexShaderSourceProperty = AvaloniaProperty.Register<RenderSurface, string>(nameof(VertexShaderSource));
        FragmentShaderSourceProperty = AvaloniaProperty.Register<RenderSurface, string>(nameof(FragmentShaderSource));
    }
    
    protected override void OnOpenGlInit(GlInterface gli)
    {
        base.OnOpenGlInit(gli);
        
        gl = GL.GetApi(gli.GetProcAddress);
        gl.IterateErrors();

        renderer = RendererFactory.Invoke();
        renderer.Initialize(gl);
        UpdateShaders();
        
        gl.TryThrowNextError();
    }

    protected override void OnOpenGlDeinit(GlInterface gli)
    {
        if (renderer is IDisposable d)
        {
            d.Dispose();
        }
        
        base.OnOpenGlDeinit(gli);
    }

    protected override void OnOpenGlRender(GlInterface gli, int fb)
    {
        gl.NotNull().Viewport(0, 0, (uint)Bounds.Width, (uint)Bounds.Height);
        renderer?.RenderFrame();
        
        Dispatcher.UIThread.Post(RequestNextFrameRendering, DispatcherPriority.Background);
    }

    public void UpdateShaders()
    {
        renderer?.UpdateShaders(new IShaderDefinition[]
        {
            new ShaderDefinition(GetSource(VertexShaderSource), ShaderSourceType.Vertex),
            new ShaderDefinition(GetSource(FragmentShaderSource), ShaderSourceType.Fragment)
        });
    }

    private IShaderSource GetSource(string uriPath)
    {
        return uriPath.StartsWith("avares") ? new ShaderUriSource(new Uri(uriPath)) : new ShaderFileSource(uriPath);
    }
}