using System.Reactive.Linq;
using Nodus.Core.Extensions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Nodus.RenderEngine.Common;

public interface ITextureDataProvider<T> where T : unmanaged, IPixel<T>
{
    IObservable<ITexture<T>> ProvideTextureData(ITextureSource source);
    ITexture<T> FetchTexture(ITextureSource source);
}

public class TextureDataProvider<T> : ITextureDataProvider<T> where T : unmanaged, IPixel<T>
{
    private readonly bool flipVertically;
    
    public TextureDataProvider(bool flipVertically = true)
    {
        this.flipVertically = flipVertically;
    }

    private void PostProcessImage(Image<T> img)
    {
        if (flipVertically)
        {
            img.Mutate(x => x.Flip(FlipMode.Vertical));
        }
    }

    public IObservable<ITexture<T>> ProvideTextureData(ITextureSource source)
    {
        return Observable.FromAsync(async () =>
        {
            using var stream = new MemoryStream(source.FetchBytes());
            var img = await Image.LoadAsync<T>(stream);

            PostProcessImage(img);
            
            return new Texture<T>(img.Width, img.Height, img);
        }).Select(x => x.MustBe<ITexture<T>>());
    }

    public ITexture<T> FetchTexture(ITextureSource source)
    {
        var img = Image.Load<T>(source.FetchBytes());
        
        PostProcessImage(img);

        return new Texture<T>(img.Width, img.Height, img);
    }
}