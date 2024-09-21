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
    IVkMaterialInstance Material { get; }
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
    IVkMaterialInstance Material,
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
    private IVkPipeline? pipeline;
    private VkQueueInfo? queueInfo;
    private IVkPhysicalDevice? physicalDevice;
    private PhysicalDeviceProperties? deviceFeatures;
    private IVkDescriptorPool? descriptorPool;
    private IVkMaterialInstance? material;

    private IVkMemoryLease? primitiveMemory;
    private IVkBoundBuffer? primitiveBuffer;

    private IVkMemory? imageMemory;
    private IVkImage? textureImage;

    private IVkSemaphore[]? renderFinishedSemaphores;
    private IVkFence[]? inFlightFences;

    private bool isInitialized;
    private int maxConcurrentFrames;
    private int frameIndex;
    private ulong[]? uniformBufferOffsets;
    private ulong[]? materialUniformOffsets;
    private uint baseUniformSize;

    private readonly ConcurrentQueue<Action> workerQueue;
    
    private IVkRenderComponent[] inlineComponents;
    private IVkRenderComponent[] explicitComponents;

    #region Initialization
    
    public VkGeometryPrimitiveRenderer()
    {
        workerQueue = new ConcurrentQueue<Action>();
        inlineComponents = explicitComponents = [];
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
        material = primitiveContext.Material;
        
        // Create pipeline
        pipeline = material.Parent.CreatePipeline(renderPass);
        
        // Create pools
        commandPool = new VkCommandPool(vkContext, device, queueInfo.Value, (uint)maxConcurrentFrames, CommandPoolCreateFlags.ResetCommandBufferBit);
        transientPool = new VkCommandPool(vkContext, device, queueInfo.Value, 1, CommandPoolCreateFlags.ResetCommandBufferBit);
        
        // Allocate memory & buffers

        // Primitive buffer layout:
        // Primitive Vertices <---> Primitive Indices <---> Uniform Buffers
        
        var primitiveVertSize = (uint)(sizeof(Vertex) * primitive.Vertices.Length);
        var indicesSize = (uint)(sizeof(uint) * primitive.Indices.Length);
        baseUniformSize = (uint)sizeof(MvpUniformBufferObject);
        
        var uniformBufferBaseOffset = primitiveVertSize + indicesSize;
        uniformBufferOffsets = new ulong[maxConcurrentFrames];
        materialUniformOffsets = new ulong[maxConcurrentFrames];

        // The uniform buffer objects have to be aligned based on the physical device limits.
        var uboAlignment = physicalDevice.Properties.Limits.MinUniformBufferOffsetAlignment;

        for (var i = 0ul; i < (ulong)maxConcurrentFrames; i++)
        {
            uniformBufferOffsets[i] = (uniformBufferBaseOffset + baseUniformSize * i + uboAlignment - 1) & ~(uboAlignment - 1);
        }

        var lastUniformOffset = uniformBufferOffsets[^1];

        for (var i = 0ul; i < (ulong)maxConcurrentFrames; i++)
        {
            materialUniformOffsets[i] = (lastUniformOffset + baseUniformSize 
                                                           + material.Parent.MaximumUniformSize * i 
                                                           + uboAlignment - 1)
                                        & ~(uboAlignment - 1);
        }

        var primitiveBufferSize = uniformBufferOffsets.Max()
                                  + baseUniformSize * (ulong)maxConcurrentFrames 
                                  + material.Parent.MaximumUniformSize * (ulong)maxConcurrentFrames;
        primitiveMemory = vkContext.RenderServices.MemoryLessor.LeaseMemory(MemoryGroups.ObjectBufferMemory, primitiveBufferSize);

        primitiveBuffer = new VkBoundBuffer(vkContext, device,
            new VkBufferContext(primitiveBufferSize,
                BufferUsageFlags.VertexBufferBit | BufferUsageFlags.IndexBufferBit | BufferUsageFlags.UniformBufferBit, 
                SharingMode.Exclusive));
        
        primitiveBuffer.BindToMemory(primitiveMemory);
        // Reduce mapping overhead
        primitiveBuffer.MapToHost();
        
        primitiveBuffer.UpdateData(primitive.Vertices.AsSpan(), 0);
        primitiveBuffer.UpdateData(primitive.Indices.AsSpan(), primitiveVertSize);
        
        // Create sync objects
        renderFinishedSemaphores = new IVkSemaphore[maxConcurrentFrames];
        inFlightFences = new IVkFence[maxConcurrentFrames];
        
        for (var i = 0; i < maxConcurrentFrames; i++)
        {
            renderFinishedSemaphores[i] = new VkSemaphore(vkContext, device);
            inFlightFences[i] = new VkFence(vkContext, device, true);
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

        inlineComponents = primitiveContext.Components?.Where(x => !x.SubmitSeparately).ToArray() ?? [];
        explicitComponents = primitiveContext.Components?.Where(x => x.SubmitSeparately).ToArray() ?? [];
        
        isInitialized = true;
    }

    private void CreateTextureImages(ITexture<Rgba32>[] textures)
    {
        if (textures.Length == 0) return;
        
        var cmdBuffer = commandPool!.GetBuffer(0);
        
        cmdBuffer.BeginBuffer(vkContext!);

        var baseTexture = textures[0];
        // Take the first texture as a dimension basis
        var textureSize = (uint)(textures[0].Width * textures[0].Height * 4);
        
        using var stagingBuffer = new VkAllocatedBuffer<byte>(vkContext!, device!, physicalDevice!,
            new VkAllocatedBufferContext(textureSize * (ulong)textures.Length, BufferUsageFlags.TransferSrcBit, SharingMode.Exclusive,
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

            // Since the textures are packed into a texture array - every single texture must have a uniform size.
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
        using var uniformWriter = new VkUniformBufferWriteToken(0, 0, 
            primitiveBuffer!.WrappedBuffer, uniformBufferOffsets!, (uint)sizeof(MvpUniformBufferObject));
        
        using var samplerWriter = new VkImageSamplerWriteToken(1, 0, 
            textureImage!.Views[0], textureImage.Sampler!.Value);
        
        using var materialUniformWriter = new VkUniformBufferWriteToken(2, 0, 
            primitiveBuffer!.WrappedBuffer, materialUniformOffsets!, material!.Parent.MaximumUniformSize);
        
        var descPoolContext = new VkDescriptorPoolContext(
        [
            new VkDescriptorInfo { Type = DescriptorType.UniformBuffer, Count = (uint)maxConcurrentFrames},
            new VkDescriptorInfo { Type = DescriptorType.CombinedImageSampler, Count = (uint)maxConcurrentFrames}
        ], new VkDescriptorWriter([uniformWriter, samplerWriter, materialUniformWriter]));
        
        descriptorPool = new VkDescriptorPool(vkContext!, device!, descPoolContext, (uint)maxConcurrentFrames,
            pipeline!.DescriptorSetLayouts[0]);
        
        descriptorPool.UpdateSets();
    }
    
    #endregion

    #region Render Loop
    
    // TODO: Synchronization must not depend on the presenter.
    public void RenderFrame()
    {
        var fence = inFlightFences![frameIndex];
        fence.Await();
        
        ValidateRenderState();
        ExecuteRenderWorkerQueue();

        var prepareTask = presenter!.CreateFramePreparationTask((uint)frameIndex);

        if (!prepareTask.Execute().IsSuccess())
        {
            return;
        }
        
        commandPool!.Reset(frameIndex, CommandBufferResetFlags.None);
        
        var framebuffer = presenter.GetAvailableFramebuffer();
        
        UpdateUniforms();
        
        fence.Reset();

        RecordCommands(framebuffer, frameIndex);

        var waitSemaphore = prepareTask.SignalSemaphore?.WrappedSemaphore ?? default;
        var signalSemaphore = renderFinishedSemaphores![frameIndex].WrappedSemaphore;
        var waitStages = PipelineStageFlags.ColorAttachmentOutputBit | prepareTask.WaitStageFlags;
        var cmdBuffer = commandPool.GetBuffer(frameIndex);

        var submitInfo = new SubmitInfo
        {
            SType = StructureType.SubmitInfo,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = &waitSemaphore,
            PWaitDstStageMask = &waitStages,
            CommandBufferCount = 1,
            PCommandBuffers = &cmdBuffer,
            SignalSemaphoreCount = 1,
            PSignalSemaphores = &signalSemaphore
        };

        var queue = device!.Queues[queueInfo!.Value.GraphicsFamily!.Value];

        vkContext!.Api.QueueSubmit(queue, 1, in submitInfo, fence.WrappedFence);

        presenter.CreatePresentationTask(queue).Execute([renderFinishedSemaphores![frameIndex].WrappedSemaphore]);

        frameIndex = (frameIndex + 1) % maxConcurrentFrames;
    }
    
    private void RecordCommands(Framebuffer framebuffer, int bufferIndex)
    {
        commandPool!.Begin(bufferIndex);

        var buffer = commandPool.GetBuffer(bufferIndex);
        
        RecordCommandsToBuffer(framebuffer, buffer);

        for (var i = 0; i < explicitComponents.Length; i++)
        {
            explicitComponents[i].RecordCommands(buffer, framebuffer, frameIndex);
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
        
        var dataBuffer = primitiveBuffer!.WrappedBuffer;
        
        vkContext.Api.CmdBindVertexBuffers(commandBuffer, 0, 1, &dataBuffer, 0);
        vkContext.Api.CmdBindIndexBuffer(commandBuffer, dataBuffer, (ulong)(sizeof(Vertex) * primitive!.Vertices.Length), IndexType.Uint32);

        var viewPort = new Viewport
        {
            X = 0, Y = 0, MinDepth = 0, MaxDepth = 1,
            Height = supplier.CurrentRenderExtent.Height,
            Width = supplier.CurrentRenderExtent.Width
        };
        
        var scissorRect = new Rect2D { Offset = new Offset2D(0, 0), Extent = supplier.CurrentRenderExtent };
        
        vkContext.Api.CmdSetViewport(commandBuffer, 0, 1, viewPort);
        vkContext.Api.CmdSetScissor(commandBuffer, 0, 1, scissorRect);
        
        vkContext.Api.CmdBindPipeline(commandBuffer, PipelineBindPoint.Graphics, pipeline!.WrappedPipeline);

        var descriptorSet = descriptorPool!.GetSet(frameIndex);

        var pushConst = supplier.FrameTime;
        
        vkContext.Api.CmdPushConstants(commandBuffer, pipeline.Layout, ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit, 0, 
            sizeof(float), &pushConst);
        vkContext.Api.CmdBindDescriptorSets(commandBuffer, PipelineBindPoint.Graphics, pipeline.Layout, 0, 
            1, &descriptorSet, 0, null);
        
        vkContext.Api.CmdDrawIndexed(commandBuffer, (uint)primitive!.Indices.Length, 1, 0, 0, 0);
        
        for (var i = 0; i < inlineComponents.Length; i++)
        {
            explicitComponents[i].RecordCommands(commandBuffer, frameBuffer, frameIndex);
        }
        
        vkContext.Api.CmdEndRenderPass(commandBuffer);
    }

    private void UpdateUniforms()
    {
        var ubo = CreateUbo();
        
        primitiveBuffer!.UpdateData(new Span<MvpUniformBufferObject>(ref ubo), uniformBufferOffsets![frameIndex]);

        if (material!.UniformSize > 0)
        {
            material.TryApplyUniforms(primitiveBuffer, materialUniformOffsets![frameIndex]);
        }
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
        
        renderFinishedSemaphores?.DisposeAll();
        inFlightFences?.DisposeAll();
        
        primitiveBuffer?.Dispose();
        textureImage?.Dispose();
        
        primitiveMemory?.Dispose();
        imageMemory?.Dispose();

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