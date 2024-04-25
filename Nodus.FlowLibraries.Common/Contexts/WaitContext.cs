using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlowEditor.Models;
using FlowEditor.Models.Primitives;
using Ninject;
using Nodus.Core.Extensions;
using Nodus.FlowEngine;
using Nodus.NodeEditor.Meta;

namespace Nodus.FlowLibraries.Common;

public class WaitContext : FlowContextBase
{
    private uint waitTime;

    private readonly ValueDescriptor waitTimeDescriptor;

    public WaitContext()
    {
        waitTime = 0;
        
        waitTimeDescriptor = new ValueDescriptor(x => waitTime = x.MustBe<uint>(), () => waitTime)
        {
            Name = nameof(waitTime),
            DisplayName = "Wait Time",
            Value = waitTime
        };
    }

    protected override void AlterFlow(IFlow flow, GraphContext context, IFlowToken currentToken)
    {
        flow.Append(new FlowDelegate("Wait Context", ct => Task.Delay(TimeSpan.FromMilliseconds(waitTime), ct)));
    }

    protected override IEnumerable<ValueDescriptor> GetDescriptors()
    {
        yield return waitTimeDescriptor;
    }

    public override NodeContextData Serialize()
    {
        return new WaitContextData(waitTime);
    }

    public override void Deserialize(NodeContextData data)
    {
       if (data is not WaitContextData d) return;

       waitTime = d.WaitTime;
    }
}

internal record WaitContextData(uint WaitTime) : NodeContextData;