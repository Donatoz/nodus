using FlowEditor;
using FlowEditor.Models.Templates;
using Nodus.NodeEditor.Meta;
using Nodus.NodeEditor.Models;

namespace Nodus.FlowLibraries.Common;

[NodeTemplatesContainer]
public static class CommonNodes
{
    public const string DebugNodeContextId = "DebugNode";
    public const string ConstantNodeContextId = "ConstantNode";
    public const string ArithmeticsNodeContextId = "ArithmeticsNode";
    public const string FormatNodeContextId = "StringFormatNode";
    public const string BranchNodeContextId = "BranchNode";
    public const string WaitNodeContextId = "WaitNode";
    public const string CompareNodeContextId = "CompareNode";
    public const string RandomBitNodeContextId = "RandomBit";
    public const string RandomRangeNodeContextId = "RandomRange";
    public const string LoopNodeContextId = "Loop";
    public const string ParallelNodeContextId = "Parallel";
    public const string LogicNodeContextId = "Logic";
    
    [NodeTemplateProvider]
    public static IEnumerable<NodeTemplate> GetDefaultTemplates()
    {
        yield return new NodeTemplateBuilder("Start", "This is where the flow starts",
            FlowUtility.FlowPort(PortType.Output))
            .WithGroup(NodeGroups.FlowGroup)
            .Build();
        
        yield return new NodeTemplateBuilder("Debug", "Prints a value into console",
                FlowUtility.FlowPort(PortType.Input),
                FlowUtility.FlowPort(PortType.Output),
                FlowUtility.Port("Message", PortType.Input))
            .WithGroup(NodeGroups.FlowGroup)
            .WithContextId(DebugNodeContextId)
            .Build();
        
        yield return new NodeTemplateBuilder("Constant", "Contains a constant value",
                FlowUtility.Port<object>("Out", PortType.Output))
            .WithGroup(NodeGroups.ConstGroup)
            .WithContextId(ConstantNodeContextId)
            .Build();
        
        yield return new NodeTemplateBuilder("Arithmetics", "Performs arithmetical operation",
                FlowUtility.Port<float>("A", PortType.Input),
                FlowUtility.Port<float>("B", PortType.Input),
                FlowUtility.Port<float>("Result", PortType.Output))
            .WithGroup(NodeGroups.ConstGroup)
            .WithContextId(ArithmeticsNodeContextId)
            .Build();
        
        yield return new NodeTemplateBuilder("Format", "Formats a string",
                FlowUtility.Port<string>("In", PortType.Input),
                FlowUtility.Port<string>("Out", PortType.Output))
            .WithGroup(NodeGroups.ConstGroup)
            .WithContextId(FormatNodeContextId)
            .Build();
        
        yield return new NodeTemplateBuilder("Branch", "Splits the flow",
                FlowUtility.FlowPort(PortType.Input),
                FlowUtility.Port<bool>("Condition", PortType.Input),
                FlowUtility.FlowPort(PortType.Output, "True"),
                FlowUtility.FlowPort(PortType.Output, "False"))
            .WithGroup(NodeGroups.FlowGroup)
            .WithContextId(BranchNodeContextId)
            .Build();
        
        yield return new NodeTemplateBuilder("Wait", "Blocks the flow for specified amount time",
                FlowUtility.FlowPort(PortType.Input),
                FlowUtility.FlowPort(PortType.Output))
            .WithGroup(NodeGroups.FlowGroup)
            .WithContextId(WaitNodeContextId)
            .Build();
        
        yield return new NodeTemplateBuilder("Compare", "Compares the specified values",
                FlowUtility.Port<float>("A", PortType.Input),
                FlowUtility.Port<float>("B", PortType.Input),
                FlowUtility.Port<bool>("Result", PortType.Output))
            .WithGroup(NodeGroups.ConstGroup)
            .WithContextId(CompareNodeContextId)
            .Build();
        
        yield return new NodeTemplateBuilder("Random Bit", "Outputs randomly true or false",
                FlowUtility.Port<bool>("Result", PortType.Output))
            .WithGroup(NodeGroups.ConstGroup)
            .WithContextId(RandomBitNodeContextId)
            .Build();
        
        yield return new NodeTemplateBuilder("Random Range", "Outputs random number in range",
            FlowUtility.Port<float>("Min", PortType.Input),
                FlowUtility.Port<float>("Max", PortType.Input),
                FlowUtility.Port<float>("Result", PortType.Output))
            .WithGroup(NodeGroups.ConstGroup)
            .WithContextId(RandomRangeNodeContextId)
            .Build();
        
        yield return new NodeTemplateBuilder("Loop", "Loops the consequent flow",
                FlowUtility.FlowPort(PortType.Input),
                FlowUtility.Port<float>("Iterations", PortType.Input),
                FlowUtility.FlowPort(PortType.Output, "Exit"),
                FlowUtility.FlowPort(PortType.Output, "Loop"))
            .WithGroup(NodeGroups.FlowGroup)
            .WithContextId(LoopNodeContextId)
            .Build();
        
        yield return new NodeTemplateBuilder("Parallel", "Runs the consequent flow in parallel",
                FlowUtility.FlowPort(PortType.Input),
                FlowUtility.FlowPort(PortType.Output, "Subsequent"),
                FlowUtility.FlowPort(PortType.Output, "Parallel"))
            .WithGroup(NodeGroups.FlowGroup)
            .WithContextId(ParallelNodeContextId)
            .Build();
        
        yield return new NodeTemplateBuilder("Logic", "Performs logical operation",
                FlowUtility.Port<bool>("A", PortType.Input),
                FlowUtility.Port<bool>("B", PortType.Input),
                FlowUtility.Port<bool>("Result", PortType.Output))
            .WithGroup(NodeGroups.ConstGroup)
            .WithContextId(LogicNodeContextId)
            .Build();
    }
}