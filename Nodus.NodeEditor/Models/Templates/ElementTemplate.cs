using Nodus.NodeEditor.Meta;

namespace Nodus.NodeEditor.Models;

public interface IGraphElementTemplate<out T> : IGraphElementTemplate
{
    T Data { get; }
}

public readonly struct ElementTemplate<T> : IGraphElementTemplate<T> where T : IGraphElementData
{
    public T Data { get; }

    public ElementTemplate(T data)
    {
        Data = data;
    }
}