using FlowEditor;
using FlowEditor.Models;
using FlowEditor.Models.Contexts;
using Nodus.NodeEditor.Models;

namespace Nodus.FlowLibraries.Common;

public class RandomBitContext : FlowContextBase
{
    public override void Bind(INodeModel node)
    {
        base.Bind(node);
        TryBindFirstOutPort(_ => new Random().Next(2) != 0);
    }
}

public class RandomRangeContext : CachedExposeContext
{
    private const string DefaultMinName = "DefaultMin";
    private const string DefaultMaxName = "DefaultMax";
    
    public RandomRangeContext()
    {
        ExposeValue(DefaultMinName, "Default Min", 0);
        ExposeValue(DefaultMaxName, "Default Max", 1);
    }
    
    public override void Bind(INodeModel node)
    {
        base.Bind(node);
        
        var inPorts = Node!.GetFlowPorts().Where(x => x.Type == PortType.Input && x.ValueType.Value == typeof(float));
        
        if (inPorts.Count() < 2) throw new Exception("A random range context must have at least 2 unique input float ports.");
        
        TryBindFirstOutPort(ctx => (float) new Random().Next(
            Convert.ToInt32(Node!.GetPortValue(inPorts.First().Id, ctx) ?? GetExposedValue<int>(DefaultMinName)),
            Convert.ToInt32(Node!.GetPortValue(inPorts.Last().Id, ctx) ?? GetExposedValue<int>(DefaultMaxName)) + 1
        ));
    }
}