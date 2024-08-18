using Nodus.Core.Extensions;
using Nodus.RenderEngine.Common;
using Nodus.RenderEngine.Vulkan.Extensions;
using Nodus.RenderEngine.Vulkan.Memory;
using Nodus.RenderEngine.Vulkan.Meta;
using Nodus.RenderEngine.Vulkan.Primitives;
using Nodus.RenderEngine.Vulkan.Sync;
using Silk.NET.Maths;
using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan.Renderers;

public interface IVkGeometryPrimitiveRenderContext : IRenderContext
{
    IGeometryPrimitive Primitive { get; }
    IVkLogicalDevice LogicalDevice { get; }
    PhysicalDevice PhysicalDevice { get; }
    VkQueueInfo QueueInfo { get; }
    IVkSwapChain SwapChain { get; }
    IVkKhrSurface Surface { get; }
    IVkPipelineContext PipelineContext { get; }
    IScreenViewer Viewer { get; }
    int ConcurrentRenderedFrames { get; }
}

public record VkGeometryPrimitiveRenderContext(
    IGeometryPrimitive Primitive,
    IEnumerable<IShaderDefinition> CoreShaders,
    IVkLogicalDevice LogicalDevice,
    PhysicalDevice PhysicalDevice,
    VkQueueInfo QueueInfo,
    IVkSwapChain SwapChain,
    IVkKhrSurface Surface,
    IScreenViewer Viewer,
    IVkPipelineContext PipelineContext,
    int ConcurrentRenderedFrames)
    : IVkGeometryPrimitiveRenderContext;

public unsafe class VkGeometryPrimitiveRenderer : IRenderer, IDisposable
{
    private IVkContext? vkContext;
    private IVkSwapChain? swapChain;
    private IVkLogicalDevice? device;
    private IVkKhrSurface? surface;
    private IGeometryPrimitive? primitive;
    private IScreenViewer? viewer;
    
    private VkCommandPool? commandPool;
    private IVkCommandPool? transientPool;
    private IVkPipeline? pipeline;
    private VkQueueInfo? queueInfo;
    private PhysicalDevice? physicalDevice;
    private IVkDescriptorPool? descriptorPool;

    private IVkMemory? primitiveMemory;
    private IVkBoundBuffer? primitiveBuffer;

    private IVkSemaphore[]? imageAvailabilitySemaphores;
    private IVkSemaphore[]? renderFinishedSemaphores;
    private IVkFence[]? inFlightFences;

    private bool isInitialized;
    private int maxConcurrentFrames;
    private int frameIndex;
    private ulong[]? uniformBufferOffsets;
    
    public void Initialize(IRenderContext context, IRenderBackendProvider backendProvider)
    {
        TryDiscardCurrentState();

        isInitialized = false;
        
        var primitiveContext = context.MustBe<IVkGeometryPrimitiveRenderContext>();
        vkContext = backendProvider.GetBackend<IVkContext>();
        
        swapChain = primitiveContext.SwapChain;
        queueInfo = primitiveContext.QueueInfo;
        device = primitiveContext.LogicalDevice;
        surface = primitiveContext.Surface;
        maxConcurrentFrames = primitiveContext.ConcurrentRenderedFrames;
        primitive = primitiveContext.Primitive;
        physicalDevice = primitiveContext.PhysicalDevice;
        viewer = primitiveContext.Viewer;
        
        // Create pipeline
        pipeline = new VkPipeline(vkContext, device, primitiveContext.PipelineContext);
        
        // Create framebuffers according to the render pass
        swapChain.CreateFrameBuffers(pipeline.RenderPass);
        
        // Create pools
        commandPool = new VkCommandPool(vkContext, device, queueInfo.Value, (uint)maxConcurrentFrames, CommandPoolCreateFlags.ResetCommandBufferBit);
        transientPool = new VkCommandPool(vkContext, device, queueInfo.Value, 1, CommandPoolCreateFlags.ResetCommandBufferBit);
        descriptorPool = new VkDescriptorPool(vkContext, device, DescriptorType.UniformBuffer, (uint)maxConcurrentFrames,
            pipeline.DescriptorSetLayout);
        
        // Allocate memory & buffers

        // Primitive buffer layout:
        // Primitive vertices <...> Primitive Indices <...> Each 64 bytes : Uniform Buffers
        
        primitiveMemory = new VkMemory(vkContext, device, physicalDevice.Value,
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);
        
        var primitiveVertSize = (uint)(sizeof(Vertex) * primitive.Vertices.Length);
        var indicesSize = (uint)(sizeof(uint) * primitive.Indices.Length);
        var uniformSize = (uint)(sizeof(UniformBufferObject));

        var uniformBufferBaseOffset = primitiveVertSize + indicesSize;
        uniformBufferOffsets = new ulong[maxConcurrentFrames];

        for (var i = 0; i < maxConcurrentFrames; i++)
        {
            uniformBufferOffsets[i] = ((uniformBufferBaseOffset + uniformSize * (ulong)i) / 64 + 1) * 64;
        }

        var primitiveBufferSize = uniformBufferOffsets.Max() + uniformSize;
        
        primitiveBuffer = new VkBoundBuffer(vkContext, device,
            new VkBoundBufferContext(primitiveBufferSize,
                BufferUsageFlags.VertexBufferBit | BufferUsageFlags.IndexBufferBit | BufferUsageFlags.UniformBufferBit, SharingMode.Exclusive,
                primitiveMemory));
        
        primitiveMemory.AllocateForBuffer(vkContext, primitiveBuffer.WrappedBuffer, device);
        primitiveBuffer.BindToMemory();
        
        primitiveBuffer.UpdateData(primitive.Vertices.AsSpan(), 0);
        primitiveBuffer.UpdateData(primitive.Indices.AsSpan(), primitiveVertSize);
        
        // Create sync objects
        imageAvailabilitySemaphores = new IVkSemaphore[maxConcurrentFrames];
        renderFinishedSemaphores = new IVkSemaphore[maxConcurrentFrames];
        inFlightFences = new IVkFence[maxConcurrentFrames];

        for (var i = 0; i < maxConcurrentFrames; i++)
        {
            imageAvailabilitySemaphores[i] = new VkSemaphore(vkContext, device);
            renderFinishedSemaphores[i] = new VkSemaphore(vkContext, device);
            inFlightFences[i] = new VkFence(vkContext, device, true);
        }
        
        // Provide dependencies
        pipeline.AddDependency(device);
        commandPool.AddDependency(device);
        descriptorPool.AddDependency(device);
        descriptorPool.AddDependency(pipeline);
        
        descriptorPool.PopulateDescriptors(primitiveBuffer.WrappedBuffer, (uint)sizeof(UniformBufferObject), uniformBufferOffsets);
        
        isInitialized = true;
    }

    public void RenderFrame()
    {
        ValidateRenderState();

        var fence = inFlightFences![frameIndex];
        
        fence.Await();
        commandPool!.Reset(frameIndex, CommandBufferResetFlags.None);

        var acquireResult = swapChain!.AcquireNextImage(out var imgIndex, imageAvailabilitySemaphores![frameIndex]);

        if (acquireResult == Result.ErrorOutOfDateKhr)
        {
            RecreateSwapChain();
            return;
        }
        if (acquireResult != Result.Success && acquireResult != Result.SuboptimalKhr)
        {
            throw new Exception("Failed to acquire next swapchain image.");
        }
        
        UpdateUniforms();
        
        fence.Reset();
        
        RecordCommands(imgIndex, frameIndex);
        
        var waitSemaphores = stackalloc [] { imageAvailabilitySemaphores[frameIndex].WrappedSemaphore };
        var signalSemaphores = stackalloc [] { renderFinishedSemaphores![frameIndex].WrappedSemaphore };
        var waitStages = stackalloc[] { PipelineStageFlags.ColorAttachmentOutputBit };
        var cmdBuffer = commandPool.GetBuffer(frameIndex);

        var submitInfo = new SubmitInfo
        {
            SType = StructureType.SubmitInfo,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = waitSemaphores,
            PWaitDstStageMask = waitStages,
            CommandBufferCount = 1,
            PCommandBuffers = &cmdBuffer,
            SignalSemaphoreCount = 1,
            PSignalSemaphores = signalSemaphores
        };

        var queue = device!.Queues[queueInfo!.Value.GraphicsFamily!.Value];

        vkContext!.Api.QueueSubmit(queue, 1, in submitInfo, fence.WrappedFence);

        var swapChainKhr = swapChain.WrappedSwapChain;

        var presentInfo = new PresentInfoKHR
        {
            SType = StructureType.PresentInfoKhr,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = signalSemaphores,
            SwapchainCount = 1,
            PSwapchains = &swapChainKhr,
            PImageIndices = &imgIndex
        };

        swapChain.SwapChainExtension.QueuePresent(queue, in presentInfo);

        frameIndex = (frameIndex + 1) % maxConcurrentFrames;
    }
    
    private void RecordCommands(uint imageIndex, int bufferIndex)
    {
        commandPool!.Begin(bufferIndex);

        var buffer = commandPool.GetBuffer(bufferIndex);
        
        RecordCommandsToBuffer(imageIndex, buffer);
        
        commandPool.End(bufferIndex);
    }

    protected virtual void RecordCommandsToBuffer(uint imageIndex, CommandBuffer buffer)
    {
        var clearValue = new ClearValue(new ClearColorValue(0, 0, 0, 0));

        var beginInfo = CreateRenderPassBeginInfo(imageIndex, &clearValue);
        
        vkContext!.Api.CmdBeginRenderPass(buffer, in beginInfo, SubpassContents.Inline);
        
        pipeline!.Bind(buffer, PipelineBindPoint.Graphics);

        var dataBuffer = primitiveBuffer!.WrappedBuffer;
        var vertOffsets = stackalloc[] {0ul};
        
        vkContext.Api.CmdBindVertexBuffers(buffer, 0, 1, &dataBuffer, vertOffsets);
        vkContext.Api.CmdBindIndexBuffer(buffer, dataBuffer, (ulong)(sizeof(Vertex) * primitive!.Vertices.Length), IndexType.Uint32);
        
        vkContext.Api.CmdSetViewport(buffer, 0, 1, pipeline.Viewport);
        vkContext.Api.CmdSetScissor(buffer, 0, 1, pipeline.Scissors);

        var descriptorSet = descriptorPool!.GetSet(frameIndex);
        
        vkContext.Api.CmdBindDescriptorSets(buffer, PipelineBindPoint.Graphics, pipeline.Layout, 0, 
            1, &descriptorSet, 0, null);
        
        vkContext.Api.CmdDrawIndexed(buffer, (uint)primitive!.Indices.Length, 1, 0, 0, 0);
        
        vkContext.Api.CmdEndRenderPass(buffer);
    }

    protected void RecreateSwapChain()
    {
        inFlightFences!
            .Where(x => x != inFlightFences![frameIndex])
            .ForEach(x => x.Await());
        
        swapChain!.DiscardCurrentState();
        surface!.Update();
        swapChain.RecreateState();
        swapChain.CreateFrameBuffers(pipeline!.RenderPass);
        
        pipeline!.UpdateViewport();
        pipeline!.UpdateScissors();
    }

    protected void UpdateUniforms()
    {
        var ubo = CreateUbo();
        
        primitiveBuffer!.UpdateData(new Span<UniformBufferObject>(ref ubo), uniformBufferOffsets![frameIndex]);
    }
    
    public void Enqueue(Action workItem)
    {
        throw new NotImplementedException();
    }
    
    private void ValidateRenderState()
    {
        if (!isInitialized)
        {
            throw new Exception("Render state is invalid: renderer was not initialized.");
        }
    }

    private RenderPassBeginInfo CreateRenderPassBeginInfo(uint image, ClearValue* clearColor)
    {
        return new RenderPassBeginInfo
        {
            SType = StructureType.RenderPassBeginInfo,
            RenderPass = pipeline!.RenderPass,
            Framebuffer = swapChain!.FrameBuffers![image],
            RenderArea = new Rect2D
            {
                Offset = new Offset2D(0, 0),
                Extent = swapChain!.Extent
            },
            ClearValueCount = 1,
            PClearValues = clearColor
        };
    }

    private UniformBufferObject CreateUbo()
    {
        return new UniformBufferObject
        {
            Model = Matrix4X4<float>.Identity,
            View = viewer!.GetView(),
            Projection = viewer.GetProjection()
        };
    }
    
    public void UpdateShaders(IEnumerable<IShaderDefinition> shaders)
    {
        throw new NotImplementedException();
    }

    protected virtual void TryDiscardCurrentState()
    {
        if (!isInitialized) return;
        
        vkContext!.Api.DeviceWaitIdle(device!.WrappedDevice);
        
        imageAvailabilitySemaphores?.DisposeAll();
        renderFinishedSemaphores?.DisposeAll();
        inFlightFences?.DisposeAll();
        
        primitiveBuffer?.Dispose();
        
        primitiveMemory?.Dispose();

        commandPool?.Dispose();
        transientPool?.Dispose();
        descriptorPool?.Dispose();
        
        pipeline?.Dispose();
    }
    
    public void Dispose()
    {
        TryDiscardCurrentState();
    }
}