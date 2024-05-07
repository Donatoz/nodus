using Nodus.RenderEngine.Common;
using SixLabors.ImageSharp.PixelFormats;

namespace Nodus.RenderEngine.OpenGL;

public static class TextureProviders
{
    public static readonly ITextureProvider<Rgba32> RgbaProvider = new Rgba32TextureProvider();
}