using System;
using Nodus.NodeEditor.Meta;
using Nodus.NodeEditor.Models;

namespace Nodus.NodeEditor.Extensions;

public static class NodeTemplateBuilderExtensions
{
    public static NodeTemplateBuilder WithInputTypedPort(this NodeTemplateBuilder builder, string header, Type portValueType)
    {
        return builder.WithPort(new TypedPortData(header, PortType.Input, PortCapacity.Single, portValueType));
    }
    
    public static NodeTemplateBuilder WithInputTypedPort<T>(this NodeTemplateBuilder builder, string header)
    {
        return builder.WithInputTypedPort(header, typeof(T));
    }
    
    public static NodeTemplateBuilder WithOutputTypedPort(this NodeTemplateBuilder builder, string header, Type portValueType)
    {
        return builder.WithPort(new TypedPortData(header, PortType.Output, PortCapacity.Multiple, portValueType));
    }
    
    public static NodeTemplateBuilder WithOutputTypedPort<T>(this NodeTemplateBuilder builder, string header)
    {
        return builder.WithOutputTypedPort(header, typeof(T));
    }
}