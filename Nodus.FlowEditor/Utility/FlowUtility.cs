using System;
using FlowEditor.Meta;
using Nodus.NodeEditor.Models;

namespace FlowEditor;

public static class FlowUtility
{
    public static FlowPortData Port(string title, PortType type, Type? valueType = null)
    {
        return new FlowPortData(title, type, type == PortType.Input ? PortCapacity.Single : PortCapacity.Multiple,
            valueType ?? typeof(object));
    }

    public static FlowPortData Port<T>(string title, PortType type) => Port(title, type, typeof(T));
    
    public static FlowPortData FlowPort(PortType type)
    {
        return new FlowPortData("Flow", type, PortCapacity.Single, typeof(FlowType));
    }
}