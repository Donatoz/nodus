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
using Nodus.RenderEngine.Vulkan.Utility;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using SixLabors.ImageSharp.PixelFormats;
using Image = Silk.NET.Vulkan.Image;

namespace Nodus.RenderEngine.Vulkan.Rendering;

public interface IVkGeometryPrimitiveRenderContext : IVkMultiBufferRenderContext
{
    IGeometryPrimitive Primitive { get; }
    IVkPhysicalDevice PhysicalDevice { get; }
    VkQueueInfo QueueInfo { get; }
    IVkMaterialInstance Material { get; }
    IScreenViewer Viewer { get; }
    ITexture<Rgba32>[] Textures { get; }
    IVkRenderSupplier Supplier { get; }
}

public record VkGeometryPrimitiveRenderContext(
    IGeometryPrimitive Primitive,
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
    : VkMultiBufferRenderContext(ConcurrentRenderedFrames, Presenter, RenderPass, Supplier, Components), IVkGeometryPrimitiveRenderContext;

public sealed unsafe class VkGeometryPrimitiveRenderer : VkMultiBufferRenderer
{
    private IVkRenderPass? renderPass;
    private IGeometryPrimitive? primitive;
    private IScreenViewer? viewer;
    private IVkRenderSupplier? supplier;
    
    private VkCommandPool? transientPool;
    private IVkPipeline? pipeline;
    private VkQueueInfo? queueInfo;
    private IVkPhysicalDevice? physicalDevice;
    private PhysicalDeviceProperties? deviceFeatures;
    private VkDescriptorPool? descriptorPool;
    private VkDescriptorWriter? descriptorWriter;
    private IVkMaterialInstance? material;

    private IVkMemoryLease? primitiveMemory;
    private VkBoundBuffer? primitiveBuffer;

    private IVkTexture? combinedTexture;
    
    private ulong[]? uniformBufferOffsets;
    private ulong[]? materialUniformOffsets;
    private uint baseUniformSize;
    
    private IVkRenderComponent[] inlineComponents;

    #region Initialization
    
    public VkGeometryPrimitiveRenderer()
    {
        inlineComponents = [];
    }

    protected override void Initialize(IVkRenderContext context)
    {
        base.Initialize(context);
        
        TryDiscardCurrentState();
        
        var primitiveContext = context.MustBe<IVkGeometryPrimitiveRenderContext>();
        
        queueInfo = primitiveContext.QueueInfo;
        renderPass = primitiveContext.RenderPass;
        primitive = primitiveContext.Primitive;
        physicalDevice = primitiveContext.PhysicalDevice;
        viewer = primitiveContext.Viewer;
        deviceFeatures = Context!.Api.GetPhysicalDeviceProperties(physicalDevice.WrappedDevice);
        supplier = primitiveContext.Supplier;
        material = primitiveContext.Material;
        
        // Create pipeline
        pipeline = material.Parent.CreatePipeline(renderPass);
        
        // Create pools
        transientPool = new VkCommandPool(Context, Device!, queueInfo.Value, 1, CommandPoolCreateFlags.ResetCommandBufferBit);
        
        // Allocate memory & buffers

        // Primitive buffer layout:
        // Primitive Vertices <---> Primitive Indices <---> Uniform Buffers
        
        var primitiveVertSize = (uint)(sizeof(Vertex) * primitive.Vertices.Length);
        var indicesSize = (uint)(sizeof(uint) * primitive.Indices.Length);
        baseUniformSize = (uint)sizeof(MvpUniformBufferObject);
        
        var uniformBufferBaseOffset = primitiveVertSize + indicesSize;
        uniformBufferOffsets = new ulong[MaxConcurrentFrames];
        materialUniformOffsets = new ulong[MaxConcurrentFrames];

        // The uniform buffer objects have to be aligned based on the physical device limits.
        var uboAlignment = physicalDevice.Properties.Limits.MinUniformBufferOffsetAlignment;

        for (var i = 0ul; i < (ulong)MaxConcurrentFrames; i++)
        {
            uniformBufferOffsets[i] = (uniformBufferBaseOffset + baseUniformSize * i + uboAlignment - 1) & ~(uboAlignment - 1);
        }

        var lastUniformOffset = uniformBufferOffsets[^1];

        for (var i = 0ul; i < (ulong)MaxConcurrentFrames; i++)
        {
            materialUniformOffsets[i] = (lastUniformOffset + baseUniformSize 
                                                           + material.Parent.MaximumUniformSize * i 
                                                           + uboAlignment - 1)
                                        & ~(uboAlignment - 1);
        }

        var primitiveBufferSize = uniformBufferOffsets.Max()
                                  + baseUniformSize * (ulong)MaxConcurrentFrames 
                                  + material.Parent.MaximumUniformSize * (ulong)MaxConcurrentFrames;
        primitiveMemory = Context.RenderServices.MemoryLessor.LeaseMemory(MemoryGroups.ObjectBufferMemory, primitiveBufferSize);

        primitiveBuffer = new VkBoundBuffer(Context,
            new VkBufferContext(primitiveBufferSize,
                BufferUsageFlags.VertexBufferBit | BufferUsageFlags.IndexBufferBit | BufferUsageFlags.UniformBufferBit, 
                SharingMode.Exclusive));
        
        primitiveBuffer.BindToMemory(primitiveMemory);
        // Reduce mapping overhead
        primitiveBuffer.MapToHost();
        
        primitiveBuffer.UpdateData(primitive.Vertices.AsSpan(), 0);
        primitiveBuffer.UpdateData(primitive.Indices.AsSpan(), primitiveVertSize);
        
        // Create images
        
        CreateTextureImages(primitiveContext.Textures);
        
        // Populate descriptors

        descriptorWriter = new VkDescriptorWriter([]);
        var descPoolContext = new VkDescriptorPoolContext(
        [
            new VkDescriptorInfo { Type = DescriptorType.UniformBuffer, Count = (uint)MaxConcurrentFrames},
            new VkDescriptorInfo { Type = DescriptorType.CombinedImageSampler, Count = (uint)MaxConcurrentFrames}
        ], descriptorWriter);
        
        descriptorPool = new VkDescriptorPool(Context!, Device!, descPoolContext, (uint)MaxConcurrentFrames,
            pipeline!.DescriptorSetLayouts[0]);
        
        WriteDescriptorSets();
        
        // Provide dependencies
        pipeline.AddDependency(Device!);
        descriptorPool!.AddDependency(Device!);
        descriptorPool.AddDependency(pipeline);

        inlineComponents = primitiveContext.Components?.Where(x => !x.SubmitSeparately).ToArray() ?? [];
    }

    private void CreateTextureImages(ITexture<Rgba32>[] textures)
    {
        if (textures.Length == 0) return;

        using var imageReadFence = new VkFence(Context!, Device!);
        
        var cmdBuffer = CommandPool!.GetBuffer(0);
        
        cmdBuffer.BeginBuffer(Context!);

        combinedTexture = new VkTexture(Context!, new VkImageSpecification(ImageType.Type2D,
            new Extent3D((uint)textures[0].Width, (uint)textures[0].Height, 1), 
            Format.R8G8B8A8Srgb,
            ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit, 
            ImageViewType.Type2DArray,
            deviceFeatures!.Value.Limits.MaxSamplerAnisotropy,
            ArrayLayers: (uint)textures.Length));
        
        combinedTexture.CmdReadFrom(cmdBuffer, textures);

        cmdBuffer.SubmitBuffer(Context!, Device!.TryGetGraphicsQueue(queueInfo!.Value)!.Value, imageReadFence.WrappedFence);
        imageReadFence.Await();
    }

    private void WriteDescriptorSets()
    {
        using var uniformWriter = new VkUniformBufferWriteToken(0, 0, 
            primitiveBuffer!.WrappedBuffer, uniformBufferOffsets!, (uint)sizeof(MvpUniformBufferObject));
        
        using var samplerWriter = new VkImageSamplerWriteToken(1, 0, 
            combinedTexture!.TextureImage!.Views[0], combinedTexture.TextureImage.Sampler!.Value);
        
        using var materialUniformWriter = new VkUniformBufferWriteToken(2, 0, 
            primitiveBuffer!.WrappedBuffer, materialUniformOffsets!, material!.Parent.MaximumUniformSize);
        
        descriptorWriter!.UpdateTokens([uniformWriter, samplerWriter, materialUniformWriter]);
        descriptorPool!.UpdateSets();
    }
    
    #endregion

    #region Render Loop

    protected override void PrepareFrame()
    {
        base.PrepareFrame();
        UpdateUniforms();
    }

    protected override void RecordCommandsToBuffer(Framebuffer frameBuffer, CommandBuffer commandBuffer)
    {
        var currentImage = Presenter!.GetCurrentImage();
        var currentDepthImage = Presenter.TryGetCurrentDepthImage();

        var colAttachment = new RenderingAttachmentInfo
        {
            SType = StructureType.RenderingAttachmentInfo,
            ImageView = currentImage.Views[0],
            ImageLayout = ImageLayout.ColorAttachmentOptimal,
            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.Store,
            ClearValue = new ClearValue(new ClearColorValue(0, 0, 0, 1.0f))
        };
        
        var depthAttachment = currentDepthImage != null 
            ? new RenderingAttachmentInfo 
            {
                SType = StructureType.RenderingAttachmentInfo,
                ImageLayout = ImageLayout.DepthStencilAttachmentOptimal,
                LoadOp = AttachmentLoadOp.Clear,
                StoreOp = AttachmentStoreOp.DontCare,
                ClearValue = new ClearValue(depthStencil: new ClearDepthStencilValue(1.0f, 0))
            } 
            : default;

        var renderInfo = new RenderingInfo
        {
            SType = StructureType.RenderingInfo,
            RenderArea = new Rect2D(new Offset2D(0, 0),
                new Extent2D(supplier!.CurrentRenderExtent.Width, supplier.CurrentRenderExtent.Height)),
            LayerCount = 1,
            ColorAttachmentCount = 1,
            PColorAttachments = &colAttachment
        };

        if (currentDepthImage != null)
        {
            depthAttachment.ImageView = currentDepthImage.Views[0];
            renderInfo.PDepthAttachment = &depthAttachment;
            renderInfo.PStencilAttachment = &depthAttachment;
        }
        
        PrepareAttachments(commandBuffer, currentImage.WrappedImage, currentDepthImage?.WrappedImage);
        
        Context!.Api.CmdBeginRendering(commandBuffer, in renderInfo);
        
        var dataBuffer = primitiveBuffer!.WrappedBuffer;
        
        Context.Api.CmdBindVertexBuffers(commandBuffer, 0, 1, &dataBuffer, 0);
        Context.Api.CmdBindIndexBuffer(commandBuffer, dataBuffer, (ulong)(sizeof(Vertex) * primitive!.Vertices.Length), IndexType.Uint32);

        var viewPort = new Viewport
        {
            X = 0, Y = 0, MinDepth = 0, MaxDepth = 1,
            Height = supplier.CurrentRenderExtent.Height,
            Width = supplier.CurrentRenderExtent.Width
        };
        
        var scissorRect = new Rect2D { Offset = new Offset2D(0, 0), Extent = supplier.CurrentRenderExtent };
        
        Context.Api.CmdSetViewport(commandBuffer, 0, 1, viewPort);
        Context.Api.CmdSetScissor(commandBuffer, 0, 1, scissorRect);
        
        Context.Api.CmdBindPipeline(commandBuffer, PipelineBindPoint.Graphics, pipeline!.WrappedPipeline);

        var descriptorSet = descriptorPool!.GetSet(FrameIndex);

        var pushConst = supplier.FrameTime;
        
        Context.Api.CmdPushConstants(commandBuffer, pipeline.Layout, ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit, 0, 
            sizeof(float), &pushConst);
        Context.Api.CmdBindDescriptorSets(commandBuffer, PipelineBindPoint.Graphics, pipeline.Layout, 0, 
            1, &descriptorSet, 0, null);
        
        Context.Api.CmdDrawIndexed(commandBuffer, (uint)primitive!.Indices.Length, 1, 0, 0, 0);
        
        for (var i = 0; i < inlineComponents.Length; i++)
        {
            inlineComponents[i].RecordCommands(commandBuffer, frameBuffer, FrameIndex);
        }
        
        Context.Api.CmdEndRendering(commandBuffer);
        
        ImageUtility.CmdTransitionLayoutManual(commandBuffer, Context, currentImage.WrappedImage, ImageLayout.ColorAttachmentOptimal, ImageLayout.PresentSrcKhr, 
            ImageUtility.SingleColorAttachmentRange, VkImageMemoryBarrierMasks.ColorAttachmentPrePresent);
    }

    private void PrepareAttachments(CommandBuffer commandBuffer,Image presenterImage, Image? depthImage = null)
    {
        ImageUtility.CmdTransitionLayoutManual(commandBuffer, Context!, presenterImage, ImageLayout.Undefined, ImageLayout.AttachmentOptimal, 
            ImageUtility.SingleColorAttachmentRange, VkImageMemoryBarrierMasks.ColorAttachmentPreRender);
        
        if (depthImage != null)
        {
            ImageUtility.CmdTransitionLayoutManual(commandBuffer, Context!, depthImage.Value, ImageLayout.Undefined, ImageLayout.AttachmentOptimal,
                ImageUtility.SingleDepthStencilAttachmentRange, VkImageMemoryBarrierMasks.DepthStencilAttachmentPreRender);
        }
    }

    private void UpdateUniforms()
    {
        var ubo = CreateUbo();
        
        primitiveBuffer!.UpdateData(new Span<MvpUniformBufferObject>(ref ubo), uniformBufferOffsets![FrameIndex]);

        if (material!.UniformSize > 0)
        {
            material.TryApplyUniforms(primitiveBuffer, materialUniformOffsets![FrameIndex]);
        }
    }
    
    #endregion

    private MvpUniformBufferObject CreateUbo()
    {
        return new MvpUniformBufferObject
        {
            Model = Matrix4X4<float>.Identity,
            View = viewer!.GetView(),
            Projection = viewer.GetProjection(true)
        };
    }

    private void TryDiscardCurrentState()
    {
        Context!.Api.DeviceWaitIdle(Device!.WrappedDevice);
        
        primitiveBuffer?.Dispose();
        combinedTexture?.Dispose();
        
        primitiveMemory?.Dispose();

        transientPool?.Dispose();
        descriptorPool?.Dispose();
        
        pipeline?.Dispose();
    }

    protected override void Dispose(bool disposing)
    {
        TryDiscardCurrentState();
        base.Dispose(disposing);
    }
}