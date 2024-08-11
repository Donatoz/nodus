namespace Nodus.RenderEngine.Common;

public interface ITextureSource
{
    byte[] FetchBytes();
}

public readonly struct TextureFileSource : ITextureSource
{
    private readonly string filePath;

    public TextureFileSource(string filePath)
    {
        this.filePath = filePath;
    }

    public byte[] FetchBytes() => File.ReadAllBytes(filePath);

    public override string ToString()
    {
        return $"[Path={filePath}]";
    }
}