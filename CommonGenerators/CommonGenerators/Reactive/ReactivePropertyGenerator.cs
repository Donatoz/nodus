using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CommonGenerators.Reactive;

[Generator]
public class ReactivePropertyGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        
    }

    public void Execute(GeneratorExecutionContext context)
    {
        var c = context.Compilation;
        
        foreach (var tree in c.SyntaxTrees)
        {
            var semantics = c.GetSemanticModel(tree);

            var types = tree.GetRoot().DescendantNodesAndSelf()
                .OfType<ClassDeclarationSyntax>()
                .Select(x => semantics.GetDeclaredSymbol(x))
                .OfType<ITypeSymbol>()
                .Where(x => x.Interfaces.Any(x => x.Name.Contains("Model")));
        }
    }
}