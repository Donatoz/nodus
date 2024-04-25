using System.Collections.Generic;
using Nodus.NodeEditor.Meta;
using Nodus.NodeEditor.Models;

namespace FlowEditor.Models.Templates;

public class NodeTemplateBuilder
{
    private string title;
    private string tooltip;
    private IList<PortData> ports;
    private string? group;
    private string? contextId;
    
    public NodeTemplateBuilder(string title, string tooltip, params PortData[] ports)
    {
        this.title = title;
        this.tooltip = tooltip;
        this.ports = new List<PortData>(ports);
    }

    public NodeTemplateBuilder WithGroup(string group)
    {
        this.group = group;
        return this;
    }

    public NodeTemplateBuilder WithContextId(string ctxId)
    {
        contextId = ctxId;
        return this;
    }
    
    public NodeTemplate Build()
    {
        return new NodeTemplate(new NodeData(title, new NodeTooltip(title, tooltip), ports)
        {
            Group = group,
            ContextId = contextId
        });
    }
}