using Nodus.NodeEditor.Meta;
using Nodus.NodeEditor.Models;

namespace Nodus.NodeEditor.Factories;

public interface IPortModelFactory
{
    IPortModel CreatePort(PortData data);
}

public class PortModelFactory : IPortModelFactory
{
    public IPortModel CreatePort(PortData data)
    {
        return CreateBase(data);
    }

    protected virtual IPortModel CreateBase(PortData data)
    {
        return new PortModel(data.PortHeader, data.Type, data.Capacity, data.PortId);
    }
}