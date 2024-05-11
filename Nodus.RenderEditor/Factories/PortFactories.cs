using Ninject.Parameters;
using Nodus.Core.Extensions;
using Nodus.DI.Factories;
using Nodus.DI.Runtime;
using Nodus.NodeEditor.Factories;
using Nodus.NodeEditor.Meta;
using Nodus.NodeEditor.Models;
using Nodus.NodeEditor.ViewModels;
using Nodus.RenderEditor.Models;
using Nodus.RenderEditor.ViewModels;

namespace Nodus.RenderEditor.Factories;

public class RenderPortModelFactory : PortModelFactory
{
    protected override IPortModel CreateBase(PortData data)
    {
        var port = new RenderPortModel(data.PortHeader, data.Type, data.Capacity, data.PortId);
        
        if (data is TypedPortData { ValueType: not null } d)
        {
            port.ChangeValueType(d.ValueType);
        }
        
        return port;
    }
}

public class RenderPortViewModelFactory : RuntimeElementConsumer, IPortViewModelFactory
{
    public RenderPortViewModelFactory(IRuntimeElementProvider elementProvider) : base(elementProvider)
    {
    }
    
    public PortViewModel Create(IPortModel model)
    {
        return ElementProvider.GetRuntimeElement<RenderPortViewModel>(
            new TypeMatchingConstructorArgument(typeof(IRenderPortModel), (_, _) => model.MustBe<IRenderPortModel>()));
    }
}