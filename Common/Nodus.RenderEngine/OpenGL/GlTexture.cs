using System.Diagnostics;
using System.Reactive.Linq;
using Nodus.RenderEngine.Common;
using ReactiveUI;
using Silk.NET.OpenGL;
using SixLabors.ImageSharp.PixelFormats;

namespace Nodus.RenderEngine.OpenGL;

public interface IGlTexture : IUnmanagedHook
{
    void Load(ITextureSource source);
    void TryBind();
    void Retarget(TextureUnit unit, TextureTarget target);
}

public interface IGlTextureSpecification
{
    TextureUnit Unit { get; }
    TextureTarget Target { get; }
    ITextureProvider<Rgba32> Provider { get; }
    TextureWrapMode WrapMode { get; }
    TextureMinFilter MinFilter { get; }
    TextureMagFilter MagFilter { get; }
}

/// <summary>
/// An OpenGL texture.
/// The texture loading process happens at a background thread, posting the result on the provided render dispatcher.
/// </summary>
public class GlTexture : GlObject, IGlTexture
{
    private TextureUnit unit;
    private TextureTarget target;
    private TextureWrapMode wrapMode;
    private TextureMinFilter minFilter;
    private TextureMagFilter magFilter;
    private bool readyToBind;

    private readonly IRenderDispatcher dispatcher;
    private readonly ITextureProvider<Rgba32> provider;
    
    public GlTexture(GL context, ITextureSource source, IGlTextureSpecification specification, IRenderDispatcher dispatcher) : base(context)
    {
        this.dispatcher = dispatcher;
        provider = specification.Provider;
        
        Retarget(specification.Unit, specification.Target);
        
        wrapMode = specification.WrapMode;
        minFilter = specification.MinFilter;
        magFilter = specification.MagFilter;
        
        Handle = Context.GenTexture();
        
        Load(source);
    }
    
    public void Load(ITextureSource source)
    {
        readyToBind = false;

        provider.ProvideTexture(source)
            .Do(x =>
            {
                ProcessLoadedTexture(x);
                readyToBind = true;
            })
            .Subscribe();
    }

    private unsafe void ProcessLoadedTexture(ITexture<Rgba32> tex)
    {
        dispatcher.Enqueue(() =>
        {
            TryBind();
            
            Context.TexImage2D(target, 0, InternalFormat.Rgba8, (uint) tex.Width, (uint) tex.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, null);
            
            tex.ManagedImage.ProcessPixelRows(accessor =>
            {
                for (var y = 0; y < accessor.Height; y++)
                {
                    fixed (void* data = accessor.GetRowSpan(y))
                    {
                        Context.TexSubImage2D(target, 0, 0, y, (uint) accessor.Width, 1, PixelFormat.Rgba, PixelType.UnsignedByte, data);
                    }
                }
            });
            
            tex.Dispose();
            
            Context.TexParameterI(target, GLEnum.TextureWrapS, (int) wrapMode);
            Context.TexParameterI(target, GLEnum.TextureWrapT, (int) wrapMode);
            Context.TexParameterI(target, GLEnum.TextureMinFilter, (int) minFilter);
            Context.TexParameterI(target, GLEnum.TextureMagFilter, (int) magFilter);
        
            Context.BindTexture(target, 0);
        }, RenderWorkPriority.High);
    }

    public void TryBind()
    {
        if (!readyToBind) return;
        
        Context.ActiveTexture(unit);
        Context.BindTexture(target, Handle);
    }

    public void Retarget(TextureUnit unit, TextureTarget target)
    {
        this.unit = unit;
        this.target = target;
    }

    public void Dispose()
    {
        Context.DeleteTexture(Handle);
    }
}

public readonly record struct GlTextureSpecification(TextureUnit Unit, TextureTarget Target, ITextureProvider<Rgba32> Provider, 
    TextureWrapMode WrapMode, TextureMinFilter MinFilter, TextureMagFilter MagFilter) : IGlTextureSpecification
{
    public GlTextureSpecification(TextureUnit unit, TextureTarget target) 
        : this(unit, target, TextureProviders.RgbaProvider, TextureWrapMode.Repeat, 
            TextureMinFilter.Nearest, TextureMagFilter.Nearest)
    {
    }
}