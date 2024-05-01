using Nodus.DI.Runtime;
using Nodus.NodeEditor.Models;

namespace Nodus.FlowLibraries.Common;

public static class CommonFlowLibrary
{
    public static void Register(INodeContextProvider provider, IRuntimeElementProvider elementProvider)
    {
        provider.RegisterFactory<DebugContext>(CommonNodes.DebugNodeContextId, elementProvider);
        provider.RegisterFactory<ArithmeticsContext>(CommonNodes.ArithmeticsNodeContextId, elementProvider);
        provider.RegisterFactory<ConstantContext>(CommonNodes.ConstantNodeContextId, elementProvider);
        provider.RegisterFactory<StringFormatContext>(CommonNodes.FormatNodeContextId, elementProvider);
        provider.RegisterFactory<BranchContext>(CommonNodes.BranchNodeContextId, elementProvider);
        provider.RegisterFactory<WaitContext>(CommonNodes.WaitNodeContextId, elementProvider);
        provider.RegisterFactory<CompareContext>(CommonNodes.CompareNodeContextId, elementProvider);
        provider.RegisterFactory<RandomBitContext>(CommonNodes.RandomBitNodeContextId, elementProvider);
        provider.RegisterFactory<RandomRangeContext>(CommonNodes.RandomRangeNodeContextId, elementProvider);
        provider.RegisterFactory<LoopContext>(CommonNodes.LoopNodeContextId, elementProvider);
        provider.RegisterFactory<ParallelContext>(CommonNodes.ParallelNodeContextId, elementProvider);
        provider.RegisterFactory<LogicContext>(CommonNodes.LogicNodeContextId, elementProvider);
    }
}