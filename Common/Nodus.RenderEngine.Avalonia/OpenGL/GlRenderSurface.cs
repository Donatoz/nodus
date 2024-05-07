using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Threading;
using Nodus.Core.Extensions;
using Nodus.RenderEngine.Common;
using Nodus.RenderEngine.OpenGL;
using Silk.NET.OpenGL;

namespace Nodus.RenderEngine.Avalonia;

public class GlRenderSurface : OpenGlControlBase
{
    public static readonly AvaloniaProperty<GlRendererFactory> RendererFactoryProperty;
    public static readonly AvaloniaProperty<string> VertexShaderSourceProperty;
    public static readonly AvaloniaProperty<string> FragmentShaderSourceProperty;
    public static readonly AvaloniaProperty<IEnumerable<IGlShaderUniform>?> UniformsProperty;
    public static readonly AvaloniaProperty<IEnumerable<string>?> TextureSourcesProperty;
    
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
    
    public IEnumerable<IGlShaderUniform>? Uniforms
    {
        get => (IEnumerable<IGlShaderUniform>?) GetValue(UniformsProperty);
        set => SetValue(UniformsProperty, value);
    }
    
    public IEnumerable<string>? TextureSources
    {
        get => (IEnumerable<string>?) GetValue(TextureSourcesProperty);
        set => SetValue(TextureSourcesProperty, value);
    }
    
    private GL? gl;
    private IRenderer? renderer;
    
    static GlRenderSurface()
    {
        RendererFactoryProperty = AvaloniaProperty.Register<GlRenderSurface, GlRendererFactory>(nameof(RendererFactory));
        VertexShaderSourceProperty = AvaloniaProperty.Register<GlRenderSurface, string>(nameof(VertexShaderSource));
        FragmentShaderSourceProperty = AvaloniaProperty.Register<GlRenderSurface, string>(nameof(FragmentShaderSource));
        UniformsProperty = AvaloniaProperty.Register<GlRenderSurface, IEnumerable<IGlShaderUniform>?>(nameof(Uniforms));
        TextureSourcesProperty = AvaloniaProperty.Register<GlRenderSurface, IEnumerable<string>?>(nameof(TextureSources));
    }
    
    protected override void OnOpenGlInit(GlInterface gli)
    {
        base.OnOpenGlInit(gli);
        
        gl = GL.GetApi(gli.GetProcAddress);
        gl.IterateErrors();
        
        renderer = RendererFactory.Invoke();
        renderer.Initialize(CreateContextForRenderer());
        
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
        gl!.Viewport(0, 0, (uint)Bounds.Width, (uint)Bounds.Height);

        renderer?.RenderFrame();
        
        Dispatcher.UIThread.Post(RequestNextFrameRendering, DispatcherPriority.Background);
    }

    public void UpdateShaders()
    {
        renderer?.UpdateShaders(new IShaderDefinition[]
        {
            new ShaderDefinition(GetShaderSource(VertexShaderSource), ShaderSourceType.Vertex),
            new ShaderDefinition(GetShaderSource(FragmentShaderSource), ShaderSourceType.Fragment)
        });
    }
    
    private IEnumerable<IGlTextureDefinition> PackTextures()
    {
        if (TextureSources == null) yield break;

        var sources = TextureSources.ToArray();
        
        for (var i = 0; i < sources.Length; i++)
        {
            var d = new GlTextureDefinition(GetTextureSource(sources[i]), new GlTextureSpecification(TextureUnit.Texture0 + i, TextureTarget.Texture2D));
            
            yield return d;
        }
    }

    // TODO: Move this to a separate layer
    private IRenderContext CreateContextForRenderer()
    {
        return renderer switch
        {
            GlGeometryPrimitiveRenderer => new GlPrimitiveContext(gl.NotNull(), Uniforms ?? Enumerable.Empty<IGlShaderUniform>(), PackTextures().ToArray()),
            _ => throw new Exception($"This surface does not support the provided renderer ({renderer}).")
        };
    }
    
    private IShaderSource GetShaderSource(string uriPath)
    {
        return uriPath.StartsWith("avares") ? new ShaderUriSource(new Uri(uriPath)) : new ShaderFileSource(uriPath);
    }

    private ITextureSource GetTextureSource(string uriPath)
    {
        return uriPath.StartsWith("avares") ? new TextureUriSource(new Uri(uriPath)) : new TextureFileSource(uriPath);
    }
}