using Nodus.Core.Entities;
using Nodus.NodeEditor.Meta;
using Nodus.NodeEditor.Models;

namespace Nodus.NodeEditor.Factories;

public interface ICommentModelFactory
{
    ICommentModel CreateComment(CommentData data);
}

public class CommentModelFactory : ICommentModelFactory
{
    public ICommentModel CreateComment(CommentData data)
    {
        var comment = CreateBase(data);
        comment.Attach(data);

        return comment;
    }

    protected virtual ICommentModel CreateBase(CommentData data)
    {
        return new CommentModel(data.Content, data.ElementId);
    }
}