namespace Nodus.RenderEngine.Assembly;

public abstract record SingleLineFeature : IShaderAssemblyFeature
{
    private readonly string line;
    
    protected SingleLineFeature(string line)
    {
        this.line = line;
    }

    public IShaderAssemblyToken GetToken(IShaderAssemblyContext context) => 
        new GenericShaderAssemblyToken(d => d.SourceBuilder.AppendLine(line));
}