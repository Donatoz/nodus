using Ninject.Parameters;
using Nodus.DI.Factories;
using Nodus.DI.Runtime;
using Nodus.NodeEditor.Models;
using Nodus.NodeEditor.ViewModels;

namespace Nodus.NodeEditor.Factories;

public interface IPortViewModelFactory
{
    PortViewModel Create(IPortModel model);
}

public class PortViewModelFactory : RuntimeElementConsumer, IPortViewModelFactory
{
    public PortViewModelFactory(IRuntimeElementProvider elementProvider) : base(elementProvider)
    {
    }
    
    public virtual PortViewModel Create(IPortModel model)
    {
        return ElementProvider.GetRuntimeElement<PortViewModel>(new TypeMatchingConstructorArgument(typeof(IPortModel), (_, _) => model));
    }
}