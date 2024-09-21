using Nodus.Core.Extensions;
using Nodus.RenderEngine.Vulkan.Components;
using Nodus.RenderEngine.Vulkan.DI;
using Nodus.RenderEngine.Vulkan.Extensions;
using Nodus.RenderEngine.Vulkan.Meta;
using Nodus.RenderEngine.Vulkan.Presentation;
using Nodus.RenderEngine.Vulkan.Sync;
using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan.Rendering;

public interface IVkMultiBufferRenderContext : IVkRenderContext
{
    int MaxConcurrentFrames { get; }
    IVkRenderPass RenderPass { get; }
    IVkRenderSupplier RenderSupplier { get; }
}

public record VkMultiBufferRenderContext(
    int MaxConcurrentFrames,
    IVkRenderPresenter Presenter,
    IVkRenderPass RenderPass,
    IVkRenderSupplier RenderSupplier,
    IVkRenderComponent[]? Components = null) : IVkMultiBufferRenderContext;

public class VkMultiBufferRenderer : VkGraphRendererBase
{
    // Rendering strategy:
    // The renderer creates N task graphs, where N - maximum concurrent frames being rendered.
    // Each task graph is executed in subsequent mode, s
    
    protected IVkCommandPool? CommandPool { get; private set; }
    protected int FrameIndex { get; private set; }
    protected int MaxConcurrentFrames { get; private set; }

    private IVkMultiBufferRenderContext? renderContext;
    private IVkRenderComponent[]? explicitComponents;
    private IVkFence[]? inFlightFences;
    private IVkTask[][]? frameRenderTasks;
    private VkQueueInfo? queueInfo;
    private IVkTaskGraph[]? frameGraphs;
    private Queue? graphicsQueue;

    private IVkSemaphore[]? renderFinishedSemaphores;
    
    protected override void Initialize(IVkRenderContext context)
    {
        renderContext = context.MustBe<IVkMultiBufferRenderContext>();
        explicitComponents = Components?.Where(x => x.SubmitSeparately).ToArray() ?? [];
        MaxConcurrentFrames = renderContext.MaxConcurrentFrames;
        
        queueInfo = Context!.RenderServices.Devices.PhysicalDevice.QueueInfo;
        
        CommandPool = new VkCommandPool(Context, Device!, queueInfo.Value, (uint)renderContext.MaxConcurrentFrames,
            CommandPoolCreateFlags.ResetCommandBufferBit);
        
        inFlightFences = new IVkFence[renderContext.MaxConcurrentFrames];
        renderFinishedSemaphores = new IVkSemaphore[renderContext.MaxConcurrentFrames];

        for (var i = 0; i < renderContext.MaxConcurrentFrames; i++)
        {
            inFlightFences[i] = new VkFence(Context!, Device!, true);
            renderFinishedSemaphores[i] = new VkSemaphore(Context, Device!);
        }

        graphicsQueue = Device!.RequireGraphicsQueue(queueInfo.Value);
        
        InitializeRenderTasks();
    }

    private void InitializeRenderTasks()
    {
        frameRenderTasks = new IVkTask[MaxConcurrentFrames][];
        
        for (var i = 0; i < renderContext!.MaxConcurrentFrames; i++)
        {
            var presenterPreparation = Presenter!.CreateFramePreparationTask((uint)i);
            
            var mainRendering = new VkCommandTask(
                PipelineStageFlags.None,
                Context!, Device!.RequireGraphicsQueue(queueInfo!.Value), CommandPool!.GetBuffer(i), ProcessFrame)
                {
                    Name = $"Main Rendering (Frame {i})",
                    SignalSemaphore = renderFinishedSemaphores![i],
                    CompletionFence = inFlightFences![i]
                };
            
            var presentation = Presenter.CreatePresentationTask(graphicsQueue!.Value);
            var frameIndexSwitch = new VkTask(PipelineStageFlags.None, (_, _) =>
            {
                FrameIndex = (FrameIndex + 1) % MaxConcurrentFrames;
                return VkTaskResult.Success;
            });

            mainRendering.Dependencies.Add(presenterPreparation);
            presentation.Dependencies.Add(mainRendering);
            frameIndexSwitch.Dependencies.Add(presentation);
            
            frameRenderTasks[i] =
            [
                presenterPreparation,
                mainRendering,
                presentation,
                frameIndexSwitch
            ];
        }
    }

    protected sealed override IEnumerable<IVkTask> GetRenderTasks()
    {
        var tasks = new List<IVkTask>();
        
        for (var i = 0; i < MaxConcurrentFrames; i++)
        {
            tasks.AddRange(GetRenderTasksForFrame(i));
        }

        return tasks;
    }

    protected virtual IEnumerable<IVkTask> GetRenderTasksForFrame(int frameIndex)
    {
        for (var i = 0; i < frameRenderTasks![frameIndex].Length; i++)
        {
            yield return frameRenderTasks[frameIndex][i];
        }
    }

    protected sealed override IVkTaskGraph CreateRenderGraph()
    {
        frameGraphs?.DisposeAll();

        frameGraphs = new IVkTaskGraph[renderContext!.MaxConcurrentFrames];
        
        for (var i = 0; i < renderContext.MaxConcurrentFrames; i++)
        {
            var member = new VkTaskGraph(Context!, VkTaskGraphExecutionStrategy.Subsequent);

            GetRenderTasksForFrame(i).ForEach(x => member.AddTask(x));

            frameGraphs[i] = member;
        }

        var graph = new VkCompositeTaskGraph(Context!, frameGraphs);
        graph.Bake();

        return graph;
    }

    protected override void PrepareFrame()
    {
        base.PrepareFrame();
        
        var fence = inFlightFences![FrameIndex];
        
        fence.Await();
        
        (RenderGraph as VkCompositeTaskGraph)?.SwitchActiveGraph(FrameIndex);
    }

    private bool ProcessFrame(CommandBuffer cmdBuffer)
    {
        CommandPool!.Reset(FrameIndex, CommandBufferResetFlags.None);
        inFlightFences![FrameIndex].Reset();
        
        var framebuffer = Presenter!.GetAvailableFramebuffer();
        
        RecordCommands(framebuffer, cmdBuffer);

        return true;
    }
    
    private void RecordCommands(Framebuffer framebuffer, CommandBuffer commandBuffer)
    {
        commandBuffer.BeginBuffer(Context!);
        
        RecordCommandsToBuffer(framebuffer, commandBuffer);
        
        for (var i = 0; i < explicitComponents!.Length; i++)
        {
            explicitComponents[i].RecordCommands(commandBuffer, framebuffer, FrameIndex);
        }

        Context!.Api.EndCommandBuffer(commandBuffer);
    }

    protected virtual unsafe void RecordCommandsToBuffer(Framebuffer framebuffer, CommandBuffer commandBuffer)
    {
        var viewPort = new Viewport
        {
            X = 0, Y = 0, MinDepth = 0, MaxDepth = 1,
            Height = 1920, Width = 1080
        };
        
        Context!.Api.CmdSetViewport(commandBuffer, 0, 1, viewPort);
        
        var clearValues = stackalloc[]
        {
            new ClearValue(new ClearColorValue(0, 0, 0, 1.0f)),
            new ClearValue(depthStencil: new ClearDepthStencilValue(1.0f, 0))
        };

        var beginInfo = new RenderPassBeginInfo
        {
            SType = StructureType.RenderPassBeginInfo,
            RenderPass = renderContext!.RenderPass.WrappedPass,
            Framebuffer = framebuffer,
            RenderArea = new Rect2D
            {
                Offset = new Offset2D(0, 0),
                Extent = renderContext.RenderSupplier.CurrentRenderExtent
            },
            ClearValueCount = 2,
            PClearValues = clearValues
        };
        
        Context!.Api.CmdBeginRenderPass(commandBuffer, in beginInfo, SubpassContents.Inline);
        
        Context.Api.CmdEndRenderPass(commandBuffer);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            frameGraphs?.DisposeAll();
            CommandPool?.Dispose();
            inFlightFences?.DisposeAll();
            renderFinishedSemaphores?.DisposeAll();
        }
    }
}