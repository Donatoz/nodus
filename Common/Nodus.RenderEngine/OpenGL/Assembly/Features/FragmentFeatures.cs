using Nodus.Core.Extensions;
using Nodus.RenderEngine.Assembly;

namespace Nodus.RenderEngine.OpenGL.Assembly;

public class GlFragmentBodyFeature : IGlShaderBodyFeature
{
    public uint AssemblyPriority => 100;
    public string BodyName { get; }
    public string? OutputType { get; }
    
    public IEnumerable<IGlShaderVariableDefinition> Arguments { get; }

    private readonly IEnumerable<IGlShaderBodyToken> bodyTokens;

    public GlFragmentBodyFeature(string bodyName, IEnumerable<IGlShaderVariableDefinition> arguments, IEnumerable<IGlShaderBodyToken> bodyTokens, string? outputType = null)
    {
        BodyName = bodyName;
        OutputType = outputType;
        Arguments = arguments;
        this.bodyTokens = bodyTokens;
    }

    public IShaderAssemblyToken GetToken(IShaderAssemblyContext context)
    {
        return new GenericShaderAssemblyToken(d =>
        {
            var outputType = OutputType ?? "void";
            d.SourceBuilder.Append($"{outputType} {BodyName}(");
            var args = string.Join(", ", Arguments.Select(x => $"{x.Type} {x.Name}"));
            d.SourceBuilder.Append(args);
            d.SourceBuilder.Append(") {");
            d.SourceBuilder.AppendLine();

            bodyTokens.ForEach(x => x.AlterBody(context));

            d.SourceBuilder.AppendLine();
            d.SourceBuilder.AppendLine("}");
        });
    }
}