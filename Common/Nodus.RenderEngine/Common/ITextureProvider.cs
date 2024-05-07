using System.Diagnostics;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using Nodus.Core.Extensions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Nodus.RenderEngine.Common;

public interface ITextureProvider<T> where T : unmanaged, IPixel<T>
{
    IObservable<ITexture<T>> ProvideTexture(ITextureSource source);
}

public class Rgba32TextureProvider : ITextureProvider<Rgba32>
{
    private readonly bool flipVertically;
    
    public Rgba32TextureProvider(bool flipVertically = true)
    {
        this.flipVertically = flipVertically;
    }

    public IObservable<ITexture<Rgba32>> ProvideTexture(ITextureSource source)
    {
        return Observable.FromAsync(async () =>
        {
            using var stream = new MemoryStream(source.FetchBytes());
            var img = await Image.LoadAsync<Rgba32>(stream);

            if (flipVertically)
            {
                img.Mutate(x => x.Flip(FlipMode.Vertical));
            }
            
            return new Texture<Rgba32>(img.Width, img.Height, img);
        }).Select(x => x.MustBe<ITexture<Rgba32>>());
    }
}