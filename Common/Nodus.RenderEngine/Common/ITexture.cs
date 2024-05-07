using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Nodus.RenderEngine.Common;

public interface ITexture<T> : IDisposable where T : unmanaged, IPixel<T>
{
    int Width { get; }
    int Height { get; }
    Image<T> ManagedImage { get; }
}

public readonly struct Texture<T> : ITexture<T> where T : unmanaged, IPixel<T>
{
    public int Width { get; }
    public int Height { get; }
    public Image<T> ManagedImage { get; }

    public Texture(int width, int height, Image<T> managedImage)
    {
        Width = width;
        Height = height;
        ManagedImage = managedImage;
    }

    public void Dispose()
    {
        ManagedImage.Dispose();
    }
}