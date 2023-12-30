using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FlowEditor.Meta;
using FlowEditor.Models.Contexts;
using Nodus.NodeEditor.Meta;
using Nodus.NodeEditor.Models;

namespace FlowEditor.Models.Templates;

[NodeTemplatesContainer]
public static class DefaultNodes
{
    public const string DebugNodeContextId = "DebugNode";
    public const string ConstantNodeContextId = "ConstantNode";
    
    [NodeTemplateProvider]
    public static IEnumerable<NodeTemplate> GetDefaultTemplates()
    {
        yield return new NodeTemplate(new NodeData("Start", new NodeTooltip("Start", "This is where the flow starts"), 
            new []
        {
            FlowUtility.FlowPort(PortType.Output)
        }) {Group = NodeGroups.FlowGroup});
        
        yield return new NodeTemplate(new NodeData("Debug", new NodeTooltip("Debug", "Print a value into console"), 
            new []
        {
            FlowUtility.FlowPort(PortType.Input),
            FlowUtility.FlowPort(PortType.Output),
            FlowUtility.Port("Message", PortType.Input)
        }) { Group = NodeGroups.FlowGroup, ContextId = DebugNodeContextId });

        yield return new NodeTemplate(new NodeData("Constant", new NodeTooltip("Constant", "Contains a constant value"),
            new[]
            {
                FlowUtility.Port<string>("Value", PortType.Output)
            }) { Group = NodeGroups.FlowGroup, ContextId = ConstantNodeContextId });
    }
}