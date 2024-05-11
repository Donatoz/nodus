using System.Collections.Generic;
using System.Linq;
using FlowEditor;
using FlowEditor.Models;
using FlowEditor.Models.Primitives;
using Nodus.NodeEditor.Meta;
using Nodus.NodeEditor.Models;

namespace Nodus.FlowLibraries.Common;

public sealed class StringFormatContext : FlowContextBase
{
    private string formatString;

    private readonly ValueDescriptor formatStringDescriptor;

    public StringFormatContext()
    {
        formatString = string.Empty;

        formatStringDescriptor =
            new ValueDescriptor(x => formatString = x?.ToString() ?? string.Empty, () => formatString)
            {
                Name = nameof(formatString),
                DisplayName = "Format",
                Value = formatString
            };
    }

    public override void Bind(INodeModel node)
    {
        base.Bind(node);
        
        var inPorts = Node!.GetFlowPorts().Where(x => x.Type == PortType.Input);
        
        if (!inPorts.Any()) return;
        
        TryBindFirstOutPort(x => string.Format(formatString, inPorts.Select(p => Node?.GetPortValue(p.Id, x)).ToArray()));
    }

    protected override IEnumerable<ValueDescriptor> GetDescriptors()
    {
        yield return formatStringDescriptor;
    }

    public override NodeContextData Serialize()
    {
        return new StringFormatContextData(formatString);
    }

    public override void Deserialize(NodeContextData data)
    {
        if (data is not StringFormatContextData d) return;

        formatString = d.Format;
    }
}

internal record StringFormatContextData(string Format) : NodeContextData;