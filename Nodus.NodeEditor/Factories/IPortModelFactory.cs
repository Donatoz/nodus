using System.Diagnostics;
using Nodus.NodeEditor.Meta;
using Nodus.NodeEditor.Models;

namespace Nodus.NodeEditor.Factories;

public interface IPortModelFactory
{
    IPortModel CreatePort(PortData data);

    public static IPortModelFactory Default { get; } = new PortModelFactory();
}

public class PortModelFactory : IPortModelFactory
{
    public IPortModel CreatePort(PortData data)
    {
        var b = CreateBase(data);
        
        Trace.WriteLine($"------- Created port: {b}");
        
        return b;
    }

    protected virtual IPortModel CreateBase(PortData data)
    {
        return new PortModel(data.PortHeader, data.Type, data.Capacity, data.PortId);
    }
}