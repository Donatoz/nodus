namespace Nodus.RenderEngine.Common;

public interface IShaderSource
{
    string FetchSource();
}

public interface IShaderDefinition
{
    IShaderSource Source { get; }
    ShaderSourceType Type { get; }
}

public enum ShaderSourceType
{
    Vertex,
    Fragment
}

public readonly struct ShaderDefinition : IShaderDefinition
{
    public IShaderSource Source { get; }
    public ShaderSourceType Type { get; }

    public ShaderDefinition(IShaderSource source, ShaderSourceType type)
    {
        Source = source;
        Type = type;
    }
}

public readonly struct ShaderStaticSource : IShaderSource
{
    private readonly string source;

    public ShaderStaticSource(string source)
    {
        this.source = source;
    }

    public string FetchSource() => source;
}

public readonly struct ShaderFileSource : IShaderSource
{
    private readonly string source;

    public ShaderFileSource(string path)
    {
        source = File.ReadAllText(path);
    }

    public string FetchSource() => source;
}