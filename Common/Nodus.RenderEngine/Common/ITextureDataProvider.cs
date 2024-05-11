using System.Diagnostics;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using Nodus.Core.Extensions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Nodus.RenderEngine.Common;

public interface ITextureDataProvider<T> where T : unmanaged, IPixel<T>
{
    IObservable<ITexture<T>> ProvideTextureData(ITextureSource source);
}

public class TextureDataProvider<T> : ITextureDataProvider<T> where T : unmanaged, IPixel<T>
{
    private readonly bool flipVertically;
    
    public TextureDataProvider(bool flipVertically = true)
    {
        this.flipVertically = flipVertically;
    }

    public IObservable<ITexture<T>> ProvideTextureData(ITextureSource source)
    {
        return Observable.FromAsync(async () =>
        {
            using var stream = new MemoryStream(source.FetchBytes());
            var img = await Image.LoadAsync<T>(stream);

            if (flipVertically)
            {
                img.Mutate(x => x.Flip(FlipMode.Vertical));
            }
            
            return new Texture<T>(img.Width, img.Height, img);
        }).Select(x => x.MustBe<ITexture<T>>());
    }
}