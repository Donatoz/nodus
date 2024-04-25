using System;
using System.Threading.Tasks;
using Nodus.FlowEngine;
using Nodus.NodeEditor.Meta;

namespace FlowEditor.Models.Extensions;

public class WaitExtension : IFlowContextExtension
{
    public TimeSpan WaitTime { get; set; }
    
    public WaitExtension(TimeSpan waitTime)
    {
        WaitTime = waitTime;
    }

    public IFlowUnit CreateFlowUnit(GraphContext ctx)
    {
        return new FlowDelegate("Wait Extension", ct => Task.Delay(WaitTime, ct));
    }
}