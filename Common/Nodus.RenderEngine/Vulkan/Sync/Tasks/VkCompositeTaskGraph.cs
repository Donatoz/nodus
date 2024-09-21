using Nodus.Core.Extensions;

namespace Nodus.RenderEngine.Vulkan.Sync;

public class VkCompositeTaskGraph : VkObject, IVkTaskGraph
{
    private readonly IVkTaskGraph[] memberGraphs;
    
    private int currentActiveGraph;
    
    public VkCompositeTaskGraph(IVkContext vkContext, IVkTaskGraph[] memberGraphs) : base(vkContext)
    {
        this.memberGraphs = memberGraphs;
    }

    public void SwitchActiveGraph(int graphIndex)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(graphIndex, memberGraphs.Length);

        currentActiveGraph = graphIndex;
    }

    public void Execute()
    {
        memberGraphs[currentActiveGraph].Execute();
    }

    public void Bake()
    {
        memberGraphs.ForEach(x => x.Bake());
    }

    public void AddTask(IVkTask task)
    {
        memberGraphs.ForEach(x => x.AddTask(task));
    }

    public void RemoveTask(IVkTask task)
    {
        memberGraphs.ForEach(x => x.AddTask(task));
    }
}