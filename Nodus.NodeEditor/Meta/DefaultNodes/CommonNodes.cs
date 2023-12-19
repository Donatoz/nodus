using System.Collections.Generic;
using Nodus.NodeEditor.Models;

namespace Nodus.NodeEditor.Meta;

[NodeTemplatesContainer]
public static class CommonNodes
{
    [NodeTemplateProvider]
    public static IEnumerable<NodeTemplate> GetNodes()
    {
        yield return new NodeTemplate(new NodeData("Test Node", default, new []
        {
            new PortData("Input Val", PortType.Input, PortCapacity.Single)
        }) {Group = NodeGroups.FlowGroup});
        
        yield return new NodeTemplate(new NodeData("Test Node 2", default, new []
        {
            new PortData("Input Val", PortType.Input, PortCapacity.Single)
        }) {Group = NodeGroups.FlowGroup});
        
        yield return new NodeTemplate(new NodeData("Test Node 3", default, new []
        {
            new PortData("Input Val", PortType.Input, PortCapacity.Single)
        }) {Group = NodeGroups.FlowGroup});
    }
}