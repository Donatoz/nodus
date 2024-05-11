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
    ITextureDataProvider<Rgba32> DataProvider { get; }
    TextureWrapMode WrapMode { get; }
    TextureMinFilter MinFilter { get; }
    TextureMagFilter MagFilter { get; }
    bool GenerateMipMaps { get; }
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
    
    public GlTexture(GL context, ITextureSource source, IGlTextureSpecification specification, IRenderDispatcher dispatcher) : base(context)
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
    
    public void Load(ITextureSource source)
    {
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

public readonly record struct GlTextureSpecification(
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