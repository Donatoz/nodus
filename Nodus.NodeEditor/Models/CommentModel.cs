using System;
using Nodus.Core.Entities;
using Nodus.Core.Reactive;
using Nodus.NodeEditor.Meta;

namespace Nodus.NodeEditor.Models;

public interface ICommentModel : IEntity, IPersistentElementModel, IDisposable
{
    IReactiveProperty<string> Content { get; }

    void SetContent(string content);
}

public class CommentModel : Entity, ICommentModel
{
    public string ElementId { get; }
    public override string EntityId => ElementId;

    public IReactiveProperty<string> Content => content;

    private readonly MutableReactiveProperty<string> content;

    public CommentModel(string content, string? elementId = null)
    {
        ElementId = elementId ?? Guid.NewGuid().ToString();
        this.content = new MutableReactiveProperty<string>(content);
    }

    public void SetContent(string content)
    {
        this.content.SetValue(content);
    }
    
    public IGraphElementData Serialize()
    {
        return new CommentData(Content.Value)
        {
            ElementId = ElementId
        };
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        
        if (!disposing) return;
        
        content.Dispose();
    }
}