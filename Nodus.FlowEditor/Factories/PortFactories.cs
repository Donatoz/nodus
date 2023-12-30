using System.Diagnostics;
using FlowEditor.Meta;
using FlowEditor.Models;
using FlowEditor.ViewModels;
using Ninject.Parameters;
using Nodus.Core.Extensions;
using Nodus.DI.Factories;
using Nodus.DI.Runtime;
using Nodus.NodeEditor.Factories;
using Nodus.NodeEditor.Meta;
using Nodus.NodeEditor.Models;
using Nodus.NodeEditor.ViewModels;

namespace FlowEditor.Factories;

public class FlowPortModelFactory : PortModelFactory
{
    protected override IPortModel CreateBase(PortData data)
    {
        var port = new FlowPortModel(data.PortHeader, data.Type, data.Capacity, data.PortId);
        
        if (data is FlowPortData { ValueType: not null } d)
        {
            port.ChangeValueType(d.ValueType);
        }
        
        return port;
    }
}

public class FlowPortViewModelFactory : RuntimeElementConsumer, IPortViewModelFactory
{
    public FlowPortViewModelFactory(IRuntimeElementProvider elementProvider) : base(elementProvider)
    {
    }
    
    public PortViewModel Create(IPortModel model)
    {
        return ElementProvider.GetRuntimeElement<FlowPortViewModel>(
            new TypeMatchingConstructorArgument(typeof(IFlowPortModel), (_, _) => model.MustBe<IFlowPortModel>()));
    }
}