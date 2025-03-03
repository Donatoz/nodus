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
    // Each graph is being populated during the initialization and executes rendering logic in subsequent mode. 
    
    // Rendering logic consists of 3 primary parts:
    // 1. Presentation preparation: the presenter must prepare its state before rendering and signal whether the next
    //    rendering steps may be executed.
    // 2. Main rendering pass: all the frame rendering logic parts combined.
    // 3. Presentation: graphics queue presentation upon main rendering pass completion.
    
    // Additional step: frame index switch. This step mutates the current frame index so that the next graphs can render
    // next frames in parallel (for example, in double buffering scenarios).
    
    /// <summary>
    /// Primary command pool.
    /// </summary>
    protected IVkCommandPool? CommandPool { get; private set; }
    /// <summary>
    /// Current frame index, capped by <see cref="MaxConcurrentFrames"/>.
    /// </summary>
    protected int FrameIndex { get; private set; }
    /// <summary>
    /// Maximum concurrently rendered frames.
    /// </summary>
    protected int MaxConcurrentFrames { get; private set; }
    /// <summary>
    /// Render supplier.
    /// </summary>
    protected IVkRenderSupplier? RenderSupplier { get; private set; }

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
        RenderSupplier = renderContext.RenderSupplier;
        
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
                    CompletionSemaphore = renderFinishedSemaphores![i],
                    CompletionFence = inFlightFences![i]
                };
            
            
            // Even though those tasks are frame-independent - their graph dependencies have to comply with the current
            // frame rendering task. Multiple graphs can share fully independent tasks only.
            var presentation = Presenter.CreatePresentationTask(graphicsQueue!.Value);
            var frameIndexSwitch = new VkHostTask(PipelineStageFlags.None, () =>
            {
                FrameIndex = (FrameIndex + 1) % MaxConcurrentFrames;
                return VkTaskResult.Success;
            });
            
            // The dependency chain looks like:
            // Prepare Presentation → Main Rendering → Presentation → Frame Index Switch
            
            // The presenter must prepare its state BEFORE rendering, since it might not be ready to present
            // the rendering results (for example, swapchain is out of date).
            // If it is not ready - the preparation task outputs a failure and the executing graph aborts the execution chain. 

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

    /// <summary>
    /// Acquire a set of render tasks for the specified frame index.
    /// </summary>
    /// <param name="frameIndex">Corresponding frame index.</param>
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
        
        // Create a task graph for each frame.
        for (var i = 0; i < renderContext.MaxConcurrentFrames; i++)
        {
            var member = new VkTaskGraph(Context!, VkTaskGraphExecutionStrategy.Subsequent);

            // Each member graph has a similar set of tasks,
            // while each set uses different state base on the frame index.
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

    /// <summary>
    /// Record rendering commands to the provided command buffer.
    /// The base functionality of this method performs a basic empty render pass.
    /// </summary>
    /// <param name="framebuffer">Currently used frame buffer, acquired from <see cref="VkGraphRendererBase.Presenter"/>.</param>
    /// <param name="commandBuffer">Currently used command buffer, acquired from <see cref="CommandPool"/>.</param>
    protected virtual unsafe void RecordCommandsToBuffer(Framebuffer framebuffer, CommandBuffer commandBuffer)
    {
        // A fallback logic that performs empty render pass which ensures the color attachment to have the desired layout.
        
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

        var renderAttachInfo = new RenderingAttachmentInfo
        {
            SType = StructureType.RenderingAttachmentInfo,
            ImageView = Presenter!.GetCurrentImage().Views[0],
            ImageLayout = ImageLayout.ColorAttachmentOptimal,
            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.Store,
            ClearValue = new ClearValue(new ClearColorValue(0, 0, 0, 1.0f))
        };

        var renderInfo = new RenderingInfo
        {
            SType = StructureType.RenderingInfo,
            RenderArea = new Rect2D(new Offset2D(0, 0), RenderSupplier!.CurrentRenderExtent),
            LayerCount = 1,
            ColorAttachmentCount = 1,
            PColorAttachments = &renderAttachInfo
        };
        
        
        
        Context!.Api.CmdBeginRendering(commandBuffer, in renderInfo);
        
        Context.Api.CmdEndRendering(commandBuffer);
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