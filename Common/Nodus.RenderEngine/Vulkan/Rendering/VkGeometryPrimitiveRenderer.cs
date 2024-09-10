using System.Collections.Concurrent;
using Nodus.Core.Extensions;
using Nodus.RenderEngine.Common;
using Nodus.RenderEngine.Vulkan.Components;
using Nodus.RenderEngine.Vulkan.Convention;
using Nodus.RenderEngine.Vulkan.DI;
using Nodus.RenderEngine.Vulkan.Extensions;
using Nodus.RenderEngine.Vulkan.Memory;
using Nodus.RenderEngine.Vulkan.Meta;
using Nodus.RenderEngine.Vulkan.Presentation;
using Nodus.RenderEngine.Vulkan.Primitives;
using Nodus.RenderEngine.Vulkan.Sync;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Nodus.RenderEngine.Vulkan.Rendering;

public interface IVkGeometryPrimitiveRenderContext : IRenderContext
{
    IGeometryPrimitive Primitive { get; }
    IVkLogicalDevice LogicalDevice { get; }
    IVkPhysicalDevice PhysicalDevice { get; }
    VkQueueInfo QueueInfo { get; }
    IVkRenderPass RenderPass { get; }
    IVkGraphicsPipelineContext PipelineContext { get; }
    IScreenViewer Viewer { get; }
    ITexture<Rgba32>[] Textures { get; }
    IVkRenderPresenter Presenter { get; }
    IVkRenderSupplier Supplier { get; }
    
    int ConcurrentRenderedFrames { get; }
    
    IVkRenderComponent[]? Components { get; }
}

public record VkGeometryPrimitiveRenderContext(
    IGeometryPrimitive Primitive,
    IVkLogicalDevice LogicalDevice,
    IVkPhysicalDevice PhysicalDevice,
    VkQueueInfo QueueInfo,
    IVkRenderPass RenderPass,
    IScreenViewer Viewer,
    IVkGraphicsPipelineContext PipelineContext,
    ITexture<Rgba32>[] Textures,
    IVkRenderPresenter Presenter,
    IVkRenderSupplier Supplier,
    int ConcurrentRenderedFrames,
    IVkRenderComponent[]? Components = null)
    : IVkGeometryPrimitiveRenderContext;

public sealed unsafe class VkGeometryPrimitiveRenderer : IRenderer, IDisposable
{
    private IVkContext? vkContext;
    private IVkRenderPass? renderPass;
    private IVkLogicalDevice? device;
    private IGeometryPrimitive? primitive;
    private IScreenViewer? viewer;
    private IVkRenderPresenter? presenter;
    private IVkRenderSupplier? supplier;
    
    private IVkCommandPool? commandPool;
    private IVkCommandPool? transientPool;
    private IVkGraphicsPipeline? pipeline;
    private VkQueueInfo? queueInfo;
    private IVkPhysicalDevice? physicalDevice;
    private PhysicalDeviceProperties? deviceFeatures;
    private IVkDescriptorPool? descriptorPool;

    private IVkMemoryLease? primitiveMemory;
    private IVkBoundBuffer? primitiveBuffer;

    private IVkMemory? imageMemory;
    private IVkImage? textureImage;

    private IVkSemaphore[]? imageAvailabilitySemaphores;
    private IVkSemaphore[]? renderFinishedSemaphores;

    private bool isInitialized;
    private int maxConcurrentFrames;
    private int frameIndex;
    private ulong[]? uniformBufferOffsets;

    private readonly ConcurrentQueue<Action> workerQueue;
    
    private readonly SortedSet<IVkRenderComponent> inlineComponents;
    private readonly SortedSet<IVkRenderComponent> explicitComponents;

    #region Initialization
    
    public VkGeometryPrimitiveRenderer()
    {
        workerQueue = new ConcurrentQueue<Action>();
        inlineComponents = new SortedSet<IVkRenderComponent>(Comparer<IVkRenderComponent>.Create((a, b) => a.Priority.CompareTo(b.Priority)));
        explicitComponents = new SortedSet<IVkRenderComponent>(Comparer<IVkRenderComponent>.Create((a, b) => a.Priority.CompareTo(b.Priority)));
    }

    public void Initialize(IRenderContext context, IRenderBackendProvider backendProvider)
    {
        TryDiscardCurrentState();

        isInitialized = false;
        
        var primitiveContext = context.MustBe<IVkGeometryPrimitiveRenderContext>();
        vkContext = backendProvider.GetBackend<IVkContext>();
        
        queueInfo = primitiveContext.QueueInfo;
        renderPass = primitiveContext.RenderPass;
        device = primitiveContext.LogicalDevice;
        maxConcurrentFrames = primitiveContext.ConcurrentRenderedFrames;
        primitive = primitiveContext.Primitive;
        physicalDevice = primitiveContext.PhysicalDevice;
        viewer = primitiveContext.Viewer;
        deviceFeatures = vkContext.Api.GetPhysicalDeviceProperties(physicalDevice.WrappedDevice);
        presenter = primitiveContext.Presenter;
        supplier = primitiveContext.Supplier;
        
        // Create pipeline
        pipeline = new VkGraphicsPipeline(vkContext, device, primitiveContext.PipelineContext);
        presenter.EventStream.Subscribe(x =>
        {
            if (x == RenderPresentEvent.PresenterUpdated)
            {
                pipeline.UpdateViewport();
                pipeline.UpdateScissors();
            }
        });
        
        // Create pools
        commandPool = new VkCommandPool(vkContext, device, queueInfo.Value, (uint)maxConcurrentFrames, CommandPoolCreateFlags.ResetCommandBufferBit);
        transientPool = new VkCommandPool(vkContext, device, queueInfo.Value, 1, CommandPoolCreateFlags.ResetCommandBufferBit);
        
        // Allocate memory & buffers

        // Primitive buffer layout:
        // Primitive Vertices <---> Primitive Indices <---> Minimally each 64 bytes: Uniform Buffers
        
        var primitiveVertSize = (uint)(sizeof(Vertex) * primitive.Vertices.Length);
        var indicesSize = (uint)(sizeof(uint) * primitive.Indices.Length);
        var uniformSize = (uint)sizeof(MvpUniformBufferObject);
        
        var uniformBufferBaseOffset = primitiveVertSize + indicesSize;
        uniformBufferOffsets = new ulong[maxConcurrentFrames];

        // The uniform buffer objects have to be aligned based on the physical device limits.
        var uboAlignment = physicalDevice.Properties.Limits.MinUniformBufferOffsetAlignment;

        for (var i = 0; i < maxConcurrentFrames; i++)
        {
            uniformBufferOffsets[i] = (uniformBufferBaseOffset + uniformSize + uboAlignment - 1) & ~(uboAlignment - 1);
        }

        var primitiveBufferSize = uniformBufferOffsets.Max() + uniformSize;
        primitiveMemory = vkContext.RenderServices.MemoryLessor.LeaseMemory(MemoryGroups.ObjectBufferMemory, primitiveBufferSize);

        primitiveBuffer = new VkBoundBuffer(vkContext, device,
            new VkBoundBufferContext(primitiveBufferSize,
                BufferUsageFlags.VertexBufferBit | BufferUsageFlags.IndexBufferBit | BufferUsageFlags.UniformBufferBit, 
                SharingMode.Exclusive));
        
        primitiveBuffer.BindToMemory(primitiveMemory);
        
        primitiveBuffer.UpdateData(primitive.Vertices.AsSpan(), 0);
        primitiveBuffer.UpdateData(primitive.Indices.AsSpan(), primitiveVertSize);
        
        // Create sync objects
        imageAvailabilitySemaphores = new IVkSemaphore[maxConcurrentFrames];
        renderFinishedSemaphores = new IVkSemaphore[maxConcurrentFrames];
        
        for (var i = 0; i < maxConcurrentFrames; i++)
        {
            imageAvailabilitySemaphores[i] = new VkSemaphore(vkContext, device);
            renderFinishedSemaphores[i] = new VkSemaphore(vkContext, device);
        }
        
        // Create images
        
        CreateTextureImages(primitiveContext.Textures);
        
        // Populate descriptors
        
        PopulateDescriptorSets();
        
        // Provide dependencies
        pipeline.AddDependency(device);
        commandPool.AddDependency(device);
        descriptorPool!.AddDependency(device);
        descriptorPool.AddDependency(pipeline);

        primitiveContext.Components?.ForEach(x => (x.SubmitSeparately ? explicitComponents : inlineComponents).Add(x));
        
        isInitialized = true;
    }

    private void CreateTextureImages(ITexture<Rgba32>[] textures)
    {
        if (textures.Length == 0) return;
        
        var cmdBuffer = commandPool!.GetBuffer(0);
        
        cmdBuffer.BeginBuffer(vkContext!);

        var baseTexture = textures[0];
        var textureSize = (uint)(textures[0].Width * textures[0].Height * 4);
        
        using var stagingBuffer = new VkAllocatedBuffer<byte>(vkContext!, device!, physicalDevice!,
            new VkBufferContext(textureSize * (ulong)textures.Length, BufferUsageFlags.TransferSrcBit, SharingMode.Exclusive,
                MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit));
        stagingBuffer.Allocate();
        
        imageMemory = new VkMemory(vkContext!, device!, physicalDevice!, MemoryPropertyFlags.DeviceLocalBit);
        
        textureImage = new VkImage(vkContext!, device!, new VkImageSpecification(ImageType.Type2D,
            new Extent3D((uint)textures[0].Width, (uint)textures[0].Height, 1), Format.R8G8B8A8Srgb,
            ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit, ImageViewType.Type2DArray, deviceFeatures!.Value.Limits.MaxSamplerAnisotropy,
            ArrayLayers: (uint)textures.Length));

        vkContext!.Api.GetImageMemoryRequirements(device!.WrappedDevice, textureImage.WrappedImage, out var requirements);
        imageMemory.Allocate(requirements.Size, requirements.MemoryTypeBits);
        
        textureImage.BindToMemory(imageMemory);
        textureImage.CreateSampler();
        textureImage.CreateView();
        
        textureImage.CmdTransitionLayout(cmdBuffer, ImageLayout.TransferDstOptimal);

        for (var i = 0u; i < textures.Length; i++)
        {
            var texture = textures[i];
            var textureData = new byte[textureSize];

            if (texture.Width != baseTexture.Width || texture.Height != baseTexture.Height)
            {
                using var clone = texture.ManagedImage.Clone();
                clone.Mutate(x => x.Resize(new Size(baseTexture.Width, baseTexture.Height)));
                clone.CopyPixelDataTo(textureData);
            }
            else
            {
                texture.ManagedImage.CopyPixelDataTo(textureData);
            }

            var offset = textureSize * i;
            stagingBuffer.UpdateData(textureData, offset);
        }

        for (var i = 0u; i < textures.Length; i++)
        {
            stagingBuffer.CmdCopyToImage(vkContext!, cmdBuffer, textureImage, imageCopyRange: new VkImageCopyRange(i, 1, textureSize * i));
        }

        textureImage.CmdTransitionLayout(cmdBuffer, ImageLayout.ShaderReadOnlyOptimal);

        cmdBuffer.SubmitBuffer(vkContext!, device!.TryGetGraphicsQueue(queueInfo!.Value)!.Value);
    }

    private void PopulateDescriptorSets()
    {
        var descriptorWriteTokens = new IVkDescriptorWriteToken[]
        {
            new VkUniformBufferWriteToken(0, 0, primitiveBuffer!.WrappedBuffer, uniformBufferOffsets!,
                (uint)sizeof(MvpUniformBufferObject)),
            new VkImageSamplerWriteToken(1, 0, textureImage!.Views[0], textureImage.Sampler!.Value)
        };
        
        var descPoolContext = new VkDescriptorPoolContext(
        [
            new VkDescriptorInfo { Type = DescriptorType.UniformBuffer, Count = (uint)maxConcurrentFrames},
            new VkDescriptorInfo { Type = DescriptorType.CombinedImageSampler, Count = (uint)maxConcurrentFrames}
        ], new VkDescriptorWriter(descriptorWriteTokens));
        
        descriptorPool = new VkDescriptorPool(vkContext!, device!, descPoolContext, (uint)maxConcurrentFrames,
            pipeline!.DescriptorSetLayouts[0]);
        
        descriptorPool.UpdateSets();
        descriptorWriteTokens.OfType<IDisposable>().DisposeAll();
    }
    
    #endregion

    #region Render Loop
    
    // TODO: Synchronization must not depend on the presenter.
    public void RenderFrame()
    {
        var fence = presenter!.GetPresentationFence((uint)frameIndex);
        fence.Await();
        
        ValidateRenderState();
        ExecuteRenderWorkerQueue();

        if (!presenter.TryPrepareNewFrame(imageAvailabilitySemaphores![frameIndex], (uint)frameIndex))
        {
            return;
        }
        
        commandPool!.Reset(frameIndex, CommandBufferResetFlags.None);
        
        var framebuffer = presenter.GetAvailableFramebuffer();
        
        UpdateUniforms();
        
        fence.Reset();

        RecordCommands(framebuffer, frameIndex);
        
        var waitSemaphores = stackalloc [] { imageAvailabilitySemaphores[frameIndex].WrappedSemaphore };
        var signalSemaphores = stackalloc [] { renderFinishedSemaphores![frameIndex].WrappedSemaphore };
        var waitStages = stackalloc [] { PipelineStageFlags.ColorAttachmentOutputBit };
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
        
        presenter!.ProcessRenderQueue(queue, renderFinishedSemaphores![frameIndex], fence);

        frameIndex = (frameIndex + 1) % maxConcurrentFrames;
    }
    
    private void RecordCommands(Framebuffer framebuffer, int bufferIndex)
    {
        commandPool!.Begin(bufferIndex);

        var buffer = commandPool.GetBuffer(bufferIndex);
        
        RecordCommandsToBuffer(framebuffer, buffer);

        foreach (var component in explicitComponents)
        {
            component.RecordCommands(buffer, framebuffer, frameIndex);
        }

        commandPool.End(bufferIndex);
    }

    private void RecordCommandsToBuffer(Framebuffer frameBuffer, CommandBuffer commandBuffer)
    {
        var clearValues = stackalloc[]
        {
            new ClearValue(new ClearColorValue(0, 0, 0, 1.0f)),
            new ClearValue(depthStencil: new ClearDepthStencilValue(1.0f, 0))
        };

        var beginInfo = new RenderPassBeginInfo
        {
            SType = StructureType.RenderPassBeginInfo,
            RenderPass = renderPass!.WrappedPass,
            Framebuffer = frameBuffer,
            RenderArea = new Rect2D
            {
                Offset = new Offset2D(0, 0),
                Extent = supplier!.CurrentRenderExtent
            },
            ClearValueCount = 2,
            PClearValues = clearValues
        };
        
        vkContext!.Api.CmdBeginRenderPass(commandBuffer, in beginInfo, SubpassContents.Inline);
        
        vkContext.Api.CmdBindPipeline(commandBuffer, PipelineBindPoint.Graphics, pipeline!.WrappedPipeline);

        var dataBuffer = primitiveBuffer!.WrappedBuffer;
        var vertOffsets = stackalloc[] {0ul};
        
        vkContext.Api.CmdBindVertexBuffers(commandBuffer, 0, 1, &dataBuffer, vertOffsets);
        vkContext.Api.CmdBindIndexBuffer(commandBuffer, dataBuffer, (ulong)(sizeof(Vertex) * primitive!.Vertices.Length), IndexType.Uint32);
        
        vkContext.Api.CmdSetViewport(commandBuffer, 0, 1, pipeline.Viewport);
        vkContext.Api.CmdSetScissor(commandBuffer, 0, 1, pipeline.Scissors);

        var descriptorSet = descriptorPool!.GetSet(frameIndex);
        
        vkContext.Api.CmdBindDescriptorSets(commandBuffer, PipelineBindPoint.Graphics, pipeline.Layout, 0, 
            1, &descriptorSet, 0, null);
        
        vkContext.Api.CmdDrawIndexed(commandBuffer, (uint)primitive!.Indices.Length, 1, 0, 0, 0);
        
        foreach (var component in inlineComponents)
        {
            component.RecordCommands(commandBuffer, frameBuffer, frameIndex);
        }
        
        vkContext.Api.CmdEndRenderPass(commandBuffer);
    }

    private void UpdateUniforms()
    {
        var ubo = CreateUbo();
        
        primitiveBuffer!.UpdateData(new Span<MvpUniformBufferObject>(ref ubo), uniformBufferOffsets![frameIndex]);
    }
    
    private void ExecuteRenderWorkerQueue()
    {
        while (!workerQueue.IsEmpty)
        {
            if (workerQueue.TryDequeue(out var action))
            {
                action.Invoke();
            }
        }
    }
    
    #endregion
    
    public void Enqueue(Action workItem)
    {
        workerQueue.Enqueue(workItem);
    }
    
    private void ValidateRenderState()
    {
        if (!isInitialized)
        {
            throw new Exception("Render state is invalid: renderer was not initialized.");
        }
    }

    private MvpUniformBufferObject CreateUbo()
    {
        return new MvpUniformBufferObject
        {
            Model = Matrix4X4<float>.Identity,
            View = viewer!.GetView(),
            Projection = viewer.GetProjection(true)
        };
    }
    
    public void UpdateShaders(IEnumerable<IShaderDefinition> shaders)
    {
        throw new NotImplementedException();
    }

    private void TryDiscardCurrentState()
    {
        if (!isInitialized) return;
        
        vkContext!.Api.DeviceWaitIdle(device!.WrappedDevice);
        
        imageAvailabilitySemaphores?.DisposeAll();
        renderFinishedSemaphores?.DisposeAll();
        
        primitiveBuffer?.Dispose();
        textureImage?.Dispose();
        
        primitiveMemory?.Dispose();
        imageMemory?.Dispose();

        commandPool?.Dispose();
        transientPool?.Dispose();
        descriptorPool?.Dispose();
        
        pipeline?.Dispose();
        
        inlineComponents.Clear();
        explicitComponents.Clear();
    }
    
    public void Dispose()
    {
        TryDiscardCurrentState();
    }
}