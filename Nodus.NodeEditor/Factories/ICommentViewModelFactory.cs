using Ninject.Parameters;
using Nodus.DI.Factories;
using Nodus.DI.Runtime;
using Nodus.NodeEditor.Models;
using Nodus.NodeEditor.ViewModels;

namespace Nodus.NodeEditor.Factories;

public interface ICommentViewModelFactory
{
    CommentViewModel Create(ICommentModel model);
}

public class CommentViewModelFactory : RuntimeElementConsumer, ICommentViewModelFactory
{
    public CommentViewModelFactory(IRuntimeElementProvider elementProvider) : base(elementProvider)
    {
    }
    
    public virtual CommentViewModel Create(ICommentModel model)
    {
        return ElementProvider.GetRuntimeElement<CommentViewModel>(new TypeMatchingConstructorArgument(typeof(ICommentModel), (_, _) => model));
    }
}