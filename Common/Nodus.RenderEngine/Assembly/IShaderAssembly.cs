using System.Text;
using Nodus.Core.Extensions;

namespace Nodus.RenderEngine.Assembly;

public interface IShaderAssembly
{
    string EvaluateToSource();
}

public interface IShaderAssemblyDraft
{
    StringBuilder SourceBuilder { get; set; }
}

public interface IShaderAssemblyToken
{
    IEnumerable<IShaderAssemblyToken>? Children { get; }

    void AlterDraft(IShaderAssemblyDraft draft);
}

public class ShaderAssemblyDraft : IShaderAssemblyDraft
{
    public StringBuilder SourceBuilder { get; set; } = new();
}

public sealed record GenericShaderAssemblyToken : IShaderAssemblyToken
{
    public IEnumerable<IShaderAssemblyToken>? Children { get; init; }
    
    private readonly Action<IShaderAssemblyDraft> context;
    
    public GenericShaderAssemblyToken(Action<IShaderAssemblyDraft> alterationContext)
    {
        context = alterationContext;
    }

    public void AlterDraft(IShaderAssemblyDraft draft) => context.Invoke(draft);
}

public sealed class ShaderAssembly : IShaderAssembly
{
    private readonly IEnumerable<IShaderAssemblyToken> tokens;
    
    public ShaderAssembly(IEnumerable<IShaderAssemblyToken> tokens)
    {
        this.tokens = tokens;
    }
    
    public string EvaluateToSource()
    {
        var draft = new ShaderAssemblyDraft();

        tokens.ForEach(x => ProcessToken(x, draft));
        
        return draft.SourceBuilder.ToString();
    }

    private void ProcessToken(IShaderAssemblyToken token, IShaderAssemblyDraft draft)
    {
        token.AlterDraft(draft);
        token.Children?.ForEach(x => ProcessToken(x, draft));
    }
}