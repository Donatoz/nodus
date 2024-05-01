using Nodus.NodeEditor.Meta;

namespace Nodus.NodeEditor.Models;

public interface IGraphElementModel
{
    string ElementId { get; }
}

public interface IPersistentElementModel : IGraphElementModel
{
    IGraphElementData Serialize();
}