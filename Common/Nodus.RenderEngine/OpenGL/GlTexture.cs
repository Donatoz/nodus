using System.Diagnostics;
using System.Reactive.Linq;
using Nodus.RenderEngine.Common;
using ReactiveUI;
using Silk.NET.OpenGL;
using SixLabors.ImageSharp.PixelFormats;

namespace Nodus.RenderEngine.OpenGL;

/// <summary>
/// Represents an OpenGL texture.
/// </summary>
public interface IGlTexture : IUnmanagedHook<uint>
{
    /// <summary>
    /// Loads an OpenGL texture using the provided texture source.
    /// The texture loading process happens at a background thread, posting the result on the provided render dispatcher.
    /// </summary>
    /// <param name="source">The texture source.</param>
    void Load(ITextureSource source);

    /// <summary>
    /// Tries to bind the OpenGL texture.
    /// </summary>
    void TryBind();

    /// <summary>
    /// Changes the target and unit of the OpenGL texture.
    /// </summary>
    /// <param name="unit">The texture unit to bind the texture to.</param>
    /// <param name="target">The target of the texture.</param>
    void Retarget(TextureUnit unit, TextureTarget target);
}

/// <summary>
/// Represents the specification for an OpenGL texture.
/// </summary>
public interface IGlTextureSpecification
{
    /// <summary>
    /// Interface for an OpenGL texture.
    /// </summary>
    TextureUnit Unit { get; }

    /// <summary>
    /// Represents the target of an OpenGL texture.
    /// </summary>
    TextureTarget Target { get; }

    /// <summary>
    /// An OpenGL texture.
    /// The texture loading process happens at a background thread, posting the result on the provided render dispatcher.
    /// </summary>
    ITextureDataProvider<Rgba32> DataProvider { get; }

    /// <summary>
    /// Represents the wrap mode of an OpenGL texture.
    /// </summary>
    TextureWrapMode WrapMode { get; }

    /// <summary>
    /// Specifies the minification filter for an OpenGL texture.
    /// </summary>
    TextureMinFilter MinFilter { get; }

    /// <summary>
    /// The magnification filter for an OpenGL texture.
    /// </summary>
    TextureMagFilter MagFilter { get; }

    /// <summary>
    /// Specifies whether to generate mipmaps for an OpenGL texture.
    /// </summary>
    /// <value>
    /// <c>true</c> if mipmaps should be generated; otherwise, <c>false</c>.
    /// </value>
    bool GenerateMipMaps { get; }

    /// <summary>
    /// Specifies the mip map filter for an OpenGL texture.
    /// </summary>
    /// <remarks>
    /// The mip map filter determines how the GPU selects the mip level to sample from when rendering the texture at different distances.
    /// </remarks>
    TextureMinFilter MipMapFilter { get; }
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
    private bool generateMipMaps;
    private TextureMinFilter mipFilter;
    
    private readonly IRenderDispatcher dispatcher;
    private readonly ITextureDataProvider<Rgba32> dataProvider;
    
    private ITextureSource? source;

    public GlTexture(GL context, ITextureSource source, IGlTextureSpecification specification, IRenderDispatcher dispatcher, IRenderTracer? tracer = null) 
        : base(context, tracer)
    {
        this.dispatcher = dispatcher;
        dataProvider = specification.DataProvider;
        
        Retarget(specification.Unit, specification.Target);
        
        wrapMode = specification.WrapMode;
        minFilter = specification.MinFilter;
        magFilter = specification.MagFilter;
        generateMipMaps = specification.GenerateMipMaps;
        mipFilter = specification.MipMapFilter;
        
        Handle = Context.GenTexture();
        
        Load(source);
    }

    /// <summary>
    /// Load an OpenGL texture using the provided texture source.
    /// The texture loading process happens at a background thread, posting the result on the provided render dispatcher.
    /// </summary>
    /// <param name="source">The texture source.</param>
    public void Load(ITextureSource source)
    {
        this.source = source;
        readyToBind = false;

        dataProvider.ProvideTextureData(source)
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
            
            TryThrowTracedGlError($"{GetType()}:ProcessLoadedTexture", $"Failed to process loaded texture: {source}");
            
            tex.Dispose();
            
            if (generateMipMaps)
            {
                Context.GenerateMipmap(target);
            }
            
            Context.TexParameterI(target, GLEnum.TextureWrapS, (int) wrapMode);
            Context.TexParameterI(target, GLEnum.TextureWrapT, (int) wrapMode);
            Context.TexParameterI(target, GLEnum.TextureMinFilter, (int) (generateMipMaps ? mipFilter : minFilter));
            Context.TexParameterI(target, GLEnum.TextureMagFilter, (int) magFilter);
        
            Context.BindTexture(target, 0);
        });
    }

    /// <summary>
    /// Try to bind the OpenGL texture.
    /// </summary>
    public void TryBind()
    {
        if (!readyToBind) return;
        
        Context.ActiveTexture(unit);
        Context.BindTexture(target, Handle);
        
        TryThrowTracedGlError($"{GetType()}:TryBind", $"Failed to bind texture: {source}");
    }

    /// <summary>
    /// Change the target and unit of the OpenGL texture.
    /// </summary>
    /// <param name="unit">The texture unit to bind the texture to.</param>
    /// <param name="target">The target of the texture.</param>
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

public record GlTextureSpecification(
    TextureUnit Unit, 
    TextureTarget Target, 
    ITextureDataProvider<Rgba32> DataProvider, 
    TextureWrapMode WrapMode, 
    TextureMinFilter MinFilter,
    TextureMagFilter MagFilter,
    bool GenerateMipMaps, 
    TextureMinFilter MipMapFilter) : IGlTextureSpecification
{
    public GlTextureSpecification(TextureUnit unit, TextureTarget target) 
        : this(unit, target, TextureProviders.RgbaProvider, TextureWrapMode.Repeat, 
            TextureMinFilter.Nearest, TextureMagFilter.Nearest, true, TextureMinFilter.NearestMipmapNearest)
    {
    }
}