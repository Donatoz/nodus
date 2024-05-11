using System;
using Nodus.Core.Reactive;
using Nodus.RenderEditor.Meta;

namespace Nodus.RenderEditor.Models;

public interface IRenderCanvasVariableModel
{
    MutableReactiveProperty<string> Name { get; }
    MutableReactiveProperty<Type> Type { get; }
    MutableReactiveProperty<object?> Value { get; }

    RenderGraphVariable Serialize();
}

public class RenderCanvasVariableModel : IRenderCanvasVariableModel, IDisposable
{
    public MutableReactiveProperty<string> Name { get; }
    public MutableReactiveProperty<Type> Type { get; }
    public MutableReactiveProperty<object?> Value { get; }

    public RenderCanvasVariableModel(string name, Type type, object? initialValue = null)
    {
        Name = new MutableReactiveProperty<string>(name);
        Type = new MutableReactiveProperty<Type>(type);
        Value = new MutableReactiveProperty<object?>(initialValue);
    }
    
    public RenderGraphVariable Serialize()
    {
        return new RenderGraphVariable(Name.Value, Type.Value, Value.Value);
    }

    public void Dispose()
    {
        Name.Dispose();
        Type.Dispose();
        Value.Dispose();
    }
}