using Nodus.RenderEngine.Common;
using SixLabors.ImageSharp.PixelFormats;

namespace Nodus.RenderEngine.OpenGL;

public static class TextureProviders
{
    public static readonly ITextureDataProvider<Rgba32> RgbaProvider = new TextureDataProvider<Rgba32>();
}