using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using Nodus.Common;
using Nodus.Core.Extensions;
using Nodus.RenderEngine.Common;
using Nodus.RenderEngine.Vulkan.Convention;
using Nodus.RenderEngine.Vulkan.DI;
using Nodus.RenderEngine.Vulkan.Extensions;
using Nodus.RenderEngine.Vulkan.Memory;
using Nodus.RenderEngine.Vulkan.Meta;
using Nodus.RenderEngine.Vulkan.Rendering;
using Nodus.RenderEngine.Vulkan.Utility;
using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan.Components;

public interface IVkImGuiComponentContext
{
    IEnumerable<IShaderDefinition> Shaders { get; }
    IVkRenderSupplier RenderSupplier { get; }
    Format ColorFormat { get; }
    Format DepthFormat { get; }
    int ConcurrentRenderedFrames { get; }
    Action GuiContext { get; }
    
    // TODO: Improvement
    // This should be provided by some external synchronizer of rendering components.
    VkImageLayoutTransition AttachmentLayoutTransition { get; }
}

public readonly struct VkImGuiComponentContext(
    IEnumerable<IShaderDefinition> shaders,
    IVkRenderSupplier renderSupplier,
    Format colorFormat,
    Format depthFormat,
    int concurrentRenderedFrames,
    VkImageLayoutTransition attachmentLayoutTransition,
    Action renderingContext)
    : IVkImGuiComponentContext
{
    public IEnumerable<IShaderDefinition> Shaders { get; } = shaders;
    public IVkRenderSupplier RenderSupplier { get; } = renderSupplier;
    public Format ColorFormat { get; } = colorFormat;
    public Format DepthFormat { get; } = depthFormat;
    public int ConcurrentRenderedFrames { get; } = concurrentRenderedFrames;
    public Action GuiContext { get; } = renderingContext;
    public VkImageLayoutTransition AttachmentLayoutTransition { get; } = attachmentLayoutTransition;
}

public class VkImGuiComponent : VkObject, IVkRenderComponent
{
    public uint Priority => 1000;
    public bool SubmitSeparately => true;

    private readonly IVkLogicalDevice device;
    private readonly IVkImGuiComponentContext componentContext;
    private readonly ImGuiIOPtr io;
    private readonly IVkDescriptorPool descriptorPool;
    private readonly IVkCommandPool commandPool;
    private readonly IVkImage fontImage;
    private readonly IVkRenderPass renderPass;
    private readonly IVkPipeline pipeline;

    private readonly IVkMemory imageMemory;
    private readonly UnmanagedContainer<Sampler> fontSamplerReference;

    private IVkMemoryLease?[] drawMemories;
    private IVkBoundBuffer?[] drawBuffers;
    
    public unsafe VkImGuiComponent(IVkContext vkContext, IVkImGuiComponentContext componentContext) : base(vkContext)
    {
        this.componentContext = componentContext;
        
        io = ImGui.GetIO();

        io.Fonts.AddFontDefault();

        device = vkContext.RenderServices.Devices.LogicalDevice;
        var physicalDevice = vkContext.RenderServices.Devices.PhysicalDevice;

        commandPool = new VkCommandPool(vkContext, device, physicalDevice.QueueInfo, 1,
            CommandPoolCreateFlags.ResetCommandBufferBit);
        
        io.Fonts.GetTexDataAsRGBA32(out nint pixels, out var fontImageWidth, out var fontImageHeight, out var fontImagePixelStride);
        
        fontImage = new VkImage(vkContext, device,
            new VkImageSpecification(ImageType.Type2D, new Extent3D((uint)fontImageWidth, (uint)fontImageHeight, 1), Format.R8G8B8A8Unorm,
                ImageUsageFlags.SampledBit | ImageUsageFlags.TransferDstBit, ImageViewType.Type2D,
                physicalDevice.Properties.Limits.MaxSamplerAnisotropy));

        imageMemory = new VkMemory(vkContext, device, physicalDevice, MemoryPropertyFlags.DeviceLocalBit);
        imageMemory.AllocateForImage(vkContext, fontImage.WrappedImage, device);
        
        fontImage.BindToMemory(imageMemory);
        fontImage.CreateView();
        fontImage.CreateSampler();

        fontSamplerReference = new UnmanagedContainer<Sampler>(fontImage.Sampler!.Value);
        
        var pixelData = new Span<byte>((void*)pixels, fontImageWidth * fontImageHeight * fontImagePixelStride);
        fontImage.UploadData(vkContext, commandPool.GetBuffer(0), pixelData, ImageLayout.ShaderReadOnlyOptimal);

        renderPass = new VkRenderPass(vkContext, device,
            new VkRenderPassContext(componentContext.ColorFormat, componentContext.DepthFormat, CreatePassFactory()));
        pipeline = new VkGraphicsPipeline(vkContext, device,
            new VkGraphicsPipelineContext([DynamicState.Viewport, DynamicState.Scissor], componentContext.Shaders,
                renderPass, componentContext.RenderSupplier, CreatePipelineFactory(), CreateDescriptorsFactory()));

        using var imgWriteToken = new VkImageSamplerWriteToken(1, 0, fontImage.Views[0], fontImage.Sampler!.Value);

        descriptorPool = new VkDescriptorPool(vkContext, device, new VkDescriptorPoolContext(
            [new VkDescriptorInfo { Type = DescriptorType.CombinedImageSampler, Count = 1000 }],
            new VkDescriptorWriter([imgWriteToken])), (uint)componentContext.ConcurrentRenderedFrames, pipeline.DescriptorSetLayouts[0]);
        
        descriptorPool.UpdateSets();
        io.Fonts.SetTexID((nint)fontImage.WrappedImage.Handle);
        
        UpdateFrameData();

        drawBuffers = new IVkBoundBuffer[componentContext.ConcurrentRenderedFrames];
        drawMemories = new IVkMemoryLease[componentContext.ConcurrentRenderedFrames];
    }
    
    #region Configuration
    
    private IVkPipelineFactory CreatePipelineFactory()
    {
        return new VkPipelineFactory
        {
            VertexInputDescriptionsFactory = () => [
                new VertexInputBindingDescription(0, (uint)Marshal.SizeOf<ImDrawVert>(), VertexInputRate.Vertex)
            ],
            VertexInputAttributeDescriptionsFactory = () => [
                new VertexInputAttributeDescription(0, 0, Format.R32G32Sfloat, (uint)Marshal.OffsetOf<ImDrawVert>(nameof(ImDrawVert.pos))),
                new VertexInputAttributeDescription(1, 0, Format.R32G32Sfloat, (uint)Marshal.OffsetOf<ImDrawVert>(nameof(ImDrawVert.uv))),
                new VertexInputAttributeDescription(2, 0, Format.R8G8B8A8Unorm, (uint)Marshal.OffsetOf<ImDrawVert>(nameof(ImDrawVert.col)))
            ],
            RasterizationFactory = () => new PipelineRasterizationStateCreateInfo
            {
                SType = StructureType.PipelineRasterizationStateCreateInfo,
                DepthClampEnable = Vk.False,
                RasterizerDiscardEnable = Vk.False,
                PolygonMode = PolygonMode.Fill,
                LineWidth = 1,
                CullMode = CullModeFlags.None,
                DepthBiasEnable = Vk.False
            },
            ColorBlendAttachmentFactory = () => new PipelineColorBlendAttachmentState
            {
                ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit |
                                 ColorComponentFlags.ABit,
                BlendEnable = true,
                SrcColorBlendFactor = BlendFactor.SrcAlpha,
                DstColorBlendFactor = BlendFactor.OneMinusSrcAlpha,
                ColorBlendOp = BlendOp.Add,
                SrcAlphaBlendFactor = BlendFactor.One,
                DstAlphaBlendFactor = BlendFactor.Zero,
                AlphaBlendOp = BlendOp.Add
            },
            DepthStencilFactory = VkPipelineFactory.DisabledDepthStencil
        };
    }

    private IVkRenderPassFactory CreatePassFactory()
    {
        return new VkRenderPassFactory
        {
            DescriptionsFactory = (colFormat, depthFormat) => [
                new AttachmentDescription
                {
                    Format = colFormat,
                    Samples = SampleCountFlags.Count1Bit,
                    LoadOp = AttachmentLoadOp.Load,
                    StoreOp = AttachmentStoreOp.Store,
                    StencilLoadOp = AttachmentLoadOp.DontCare,
                    StencilStoreOp = AttachmentStoreOp.DontCare,
                    InitialLayout = componentContext.AttachmentLayoutTransition.From,
                    FinalLayout = componentContext.AttachmentLayoutTransition.To
                },
                new AttachmentDescription
                {
                    Format = depthFormat,
                    Samples = SampleCountFlags.Count1Bit,
                    LoadOp = AttachmentLoadOp.DontCare,
                    StoreOp = AttachmentStoreOp.DontCare,
                    StencilLoadOp = AttachmentLoadOp.DontCare,
                    StencilStoreOp = AttachmentStoreOp.DontCare,
                    InitialLayout = ImageLayout.DepthStencilAttachmentOptimal,
                    FinalLayout = ImageLayout.DepthStencilAttachmentOptimal
                }
            ]
        };
    }

    private unsafe IVkDescriptorSetFactory CreateDescriptorsFactory()
    {
        return new VkDescriptorSetFactory
        {
            BindingsFactory = () =>
            [
                new DescriptorSetLayoutBinding
                {
                    Binding = 1,
                    DescriptorType = DescriptorType.CombinedImageSampler,
                    DescriptorCount = 1,
                    StageFlags = ShaderStageFlags.FragmentBit,
                    PImmutableSamplers = fontSamplerReference.Data
                }
            ],
            PushConstantsFactory = () =>
            [
                new PushConstantRange
                {
                    Offset = 0,
                    Size = sizeof(float) * 2 * 2, // sizeof(vec2) * 2
                    StageFlags = ShaderStageFlags.VertexBit
                }
            ]
        };
    }
    
    #endregion
    
    #region Record Logic

    public unsafe void RecordCommands(CommandBuffer commandBuffer, Framebuffer framebuffer, int frameIndex)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(frameIndex, componentContext.ConcurrentRenderedFrames - 1);
        
        UpdateFrameData();

        ImGui.NewFrame();
        
        componentContext.GuiContext.Invoke();
        
        ImGui.Render();
        
        var drawData = ImGui.GetDrawData();

        var frameBufferWidth = (int)drawData.DisplaySize.X * drawData.FramebufferScale.X;
        var frameBufferHeight = (int)drawData.DisplaySize.Y * drawData.FramebufferScale.Y;
        
        if (frameBufferWidth <= 0 || frameBufferHeight <= 0) return;

        var vertSize = (ulong)drawData.TotalVtxCount * (ulong)sizeof(ImDrawVert);
        var idxSize = (ulong)drawData.TotalIdxCount * sizeof(ushort);
        var idxGlobalOffset = vertSize;

        if (drawData.TotalVtxCount > 0)
        {
            var drawBuffer = drawBuffers[frameIndex];
            var drawMemory = drawMemories[frameIndex];

            if (drawBuffer == null || drawMemory!.Region.Size < vertSize + idxSize)
            {
                UpdateDrawResources(frameIndex, vertSize + idxSize);
                drawBuffer = drawBuffers[frameIndex];
            }
            
            var vertOffset = 0ul;
            var idxOffset = 0ul;

            for (var i = 0; i < drawData.CmdListsCount; i++)
            {
                ref var cmdList = ref drawData.CmdLists[i];

                var vertRegionSize = cmdList.VtxBuffer.Size * (uint)sizeof(ImDrawVert);
                var idxRegionSize = cmdList.IdxBuffer.Size * sizeof(ushort);
                
                drawBuffer!.SetMappedMemory(cmdList.VtxBuffer.Data.ToPointer(), (ulong)vertRegionSize, vertOffset);
                drawBuffer.SetMappedMemory(cmdList.IdxBuffer.Data.ToPointer(), (ulong)idxRegionSize, idxGlobalOffset + idxOffset);
                
                vertOffset += (ulong)vertRegionSize;
                idxOffset += (ulong)idxRegionSize;
            }
        }

        var beginInfo = new RenderPassBeginInfo
        {
            SType = StructureType.RenderPassBeginInfo,
            Framebuffer = framebuffer,
            RenderArea = new Rect2D
            {
                Offset = new Offset2D(0, 0),
                Extent = componentContext.RenderSupplier.CurrentRenderExtent
            },
            RenderPass = renderPass.WrappedPass
        };
        
        Context.Api.CmdBeginRenderPass(commandBuffer, in beginInfo, SubpassContents.Inline);
        
        Context.Api.CmdBindPipeline(commandBuffer, PipelineBindPoint.Graphics, pipeline.WrappedPipeline);

        if (drawData.TotalVtxCount > 0)
        {
            var drawBuffer = drawBuffers[frameIndex]!;

            Context.Api.CmdBindVertexBuffers(commandBuffer, 0, 1, drawBuffer.WrappedBuffer, 0);
            Context.Api.CmdBindIndexBuffer(commandBuffer, drawBuffer.WrappedBuffer, idxGlobalOffset, IndexType.Uint16);
        }
        
        var viewPort = new Viewport(0, 0, frameBufferWidth, frameBufferHeight, 0, 1);
        Context.Api.CmdSetViewport(commandBuffer, 0, 1, in viewPort);
        
        var transform = stackalloc[]
        {
            2f / drawData.DisplaySize.X,
            2f / drawData.DisplaySize.Y,
            -1f - drawData.DisplayPos.X * 2f / drawData.DisplaySize.X,
            -1f - drawData.DisplayPos.Y * 2f / drawData.DisplaySize.Y
        };
        
        Context.Api.CmdPushConstants(commandBuffer, pipeline.Layout, ShaderStageFlags.VertexBit, 0, sizeof(float) * 4, transform);
        
        var descriptorSet = descriptorPool.GetSet(frameIndex);
        
        Context.Api.CmdBindDescriptorSets(commandBuffer, PipelineBindPoint.Graphics, pipeline.Layout, 0, 
            1, &descriptorSet, 0, null);
        
        var clipOffset = drawData.DisplayPos;
        var clipScale = drawData.FramebufferScale;
        
        var vertexOffset = 0;
        var indexOffset = 0;
        
        for (var i = 0; i < drawData.CmdListsCount; i++)
        {
            ref var cmdList = ref drawData.CmdLists[i];

            for (var j = 0; j < cmdList.CmdBuffer.Size; j++)
            {
                var cmd = cmdList.CmdBuffer[j];

                var clipRect = new Vector4(
                    (cmd.ClipRect.X - clipOffset.X) * clipScale.X,
                    (cmd.ClipRect.Y - clipOffset.Y) * clipScale.Y,
                    (cmd.ClipRect.Z - clipOffset.X) * clipScale.X,
                    (cmd.ClipRect.W - clipOffset.Y) * clipScale.Y
                );
                
                if (clipRect.X < frameBufferWidth && clipRect.Y < frameBufferHeight && clipRect.Z >= 0 &&
                    clipRect.W >= 0)
                {
                    if (clipRect.X < 0) clipRect.X = 0;
                    if (clipRect.Y < 0) clipRect.Y = 0;

                    var scissor = new Rect2D(new Offset2D((int)clipRect.X, (int)clipRect.Y),
                        new Extent2D((uint)(clipRect.Z - clipRect.X), (uint)(clipRect.W - clipRect.Y)));
                    
                    Context.Api.CmdSetScissor(commandBuffer, 0, 1, in scissor);
                    
                    Context.Api.CmdDrawIndexed(commandBuffer, cmd.ElemCount, 1, 
                        cmd.IdxOffset + (uint)indexOffset, 
                        (int)(cmd.VtxOffset + vertexOffset), 
                        0);
                }
            }

            indexOffset += cmdList.IdxBuffer.Size;
            vertexOffset += cmdList.VtxBuffer.Size;
        }

        Context.Api.CmdEndRenderPass(commandBuffer);
    }

    #endregion

    private void UpdateDrawResources(int frameIndex, ulong drawBufferSize)
    {
        drawBuffers[frameIndex]?.Dispose();
        drawMemories[frameIndex]?.Dispose();

        drawBuffers[frameIndex] = new VkBoundBuffer(Context, device,
            new VkBufferContext(drawBufferSize, BufferUsageFlags.VertexBufferBit | BufferUsageFlags.IndexBufferBit, SharingMode.Exclusive));

        Context.Api.GetBufferMemoryRequirements(device.WrappedDevice, drawBuffers[frameIndex]!.WrappedBuffer,
            out var requirements);
        
        drawMemories[frameIndex] =
            Context.RenderServices.MemoryLessor.LeaseMemory(MemoryGroups.ObjectBufferMemory, requirements.Size, (uint)requirements.Alignment);
        
        drawBuffers[frameIndex]!.BindToMemory(drawMemories[frameIndex]!);
        drawBuffers[frameIndex]!.MapToHost();
    }
    
    private void UpdateFrameData(float dt = 1/60f)
    {
        var renderExtent = componentContext.RenderSupplier.CurrentRenderExtent;
        
        io.DisplaySize = new Vector2(renderExtent.Width, renderExtent.Height);
        
        if (renderExtent is { Width: > 0, Height: > 0 })
        {
            io.DisplayFramebufferScale = componentContext.RenderSupplier.CurrentFrameBufferScale;
        }

        io.DeltaTime = dt;
    }

    protected override void Dispose(bool disposing)
    {
        if (IsDisposing)
        {
            // TODO: Should be synchronised with external fences instead
            Context.Api.DeviceWaitIdle(device.WrappedDevice);
            
            pipeline.Dispose();
            renderPass.Dispose();
            fontSamplerReference.Dispose();
            fontImage.Dispose();
            
            drawBuffers.OfType<IVkBoundBuffer>().DisposeAll();
            drawMemories.OfType<IVkMemoryLease>().DisposeAll();
            
            imageMemory.Dispose();
            descriptorPool.Dispose();
            commandPool.Dispose();
        }
        
        base.Dispose(disposing);
    }
}