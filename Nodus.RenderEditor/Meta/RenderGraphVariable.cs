using System;

namespace Nodus.RenderEditor.Meta;

public record RenderGraphVariable(string Name, Type Type, object? Value);