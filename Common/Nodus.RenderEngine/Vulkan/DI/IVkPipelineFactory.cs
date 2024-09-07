using Nodus.DI.Factories;
using Nodus.RenderEngine.Vulkan.Utility;
using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan.DI;

/// <summary>
/// Represents an interface for creating Vulkan pipeline state information.
/// </summary>
public interface IVkPipelineFactory
{
    /// <summary>
    /// Create a new instance of PipelineDynamicStateCreateInfo based on the specified dynamic states.
    /// </summary>
    /// <param name="stateCount">The number of dynamic states.</param>
    /// <param name="states">A pointer to an array of dynamic states.</param>
    unsafe PipelineDynamicStateCreateInfo CreateDynamicState(uint stateCount, DynamicState* states);

    /// <summary>
    /// Create a vertex input state info.
    /// </summary>
    /// <returns>The vertex input state.</returns>
    unsafe PipelineVertexInputStateCreateInfo CreateVertexInputState(uint bindingDescCount, VertexInputBindingDescription* bindingDescriptions,
        uint attribDescCount, VertexInputAttributeDescription* attributeDescriptions);

    /// <summary>
    /// Create Vulkan input assembly state info.
    /// </summary>
    PipelineInputAssemblyStateCreateInfo CreateInputAssembly();

    /// <summary>
    /// Create viewport state info.
    /// </summary>
    /// <param name="viewport">A pointer to the desired viewport state.</param>
    /// <param name="scissors">A pointer to the desired scissors state.</param>
    unsafe PipelineViewportStateCreateInfo CreateViewport(Viewport* viewport, Rect2D* scissors);

    /// <summary>
    /// Create rasterization state info.
    /// </summary>
    /// <returns>
    /// The rasterization state for the pipeline.
    /// </returns>
    PipelineRasterizationStateCreateInfo CreateRasterization();

    /// <summary>
    /// Create multisampling state info.
    /// </summary>
    PipelineMultisampleStateCreateInfo CreateMultisampling();

    PipelineColorBlendAttachmentState CreateColorBlendAttachment();
    /// <summary>
    /// Create color blend state info with the specified attachments.
    /// </summary>
    /// <param name="attachments">A pointer to the array of color blend attachment states.</param>
    unsafe PipelineColorBlendStateCreateInfo CreateColorBlend(PipelineColorBlendAttachmentState* attachments);

    PipelineDepthStencilStateCreateInfo CreateDepthStencil();

    VertexInputBindingDescription[] CreateVertexInputDescriptions();
    VertexInputAttributeDescription[] CreateVertexInputAttributeDescriptions();
}

public class VkPipelineFactory : IVkPipelineFactory
{
    public static VkPipelineFactory DefaultPipelineFactory { get; } = new();
    
    public Func<uint, nint, PipelineDynamicStateCreateInfo>? DynamicStateFactory { get; set; }
    public Func<uint, nint, uint, nint, PipelineVertexInputStateCreateInfo>? VertexInputFactory { get; set; }
    public Func<PipelineInputAssemblyStateCreateInfo>? InputAssemblyFactory { get; set; }
    public Func<nint, nint, PipelineViewportStateCreateInfo>? ViewportFactory { get; set; }
    public Func<PipelineRasterizationStateCreateInfo>? RasterizationFactory { get; set; }
    public Func<PipelineMultisampleStateCreateInfo>? MultisamplingFactory { get; set; }
    public Func<PipelineColorBlendAttachmentState>? ColorBlendAttachmentFactory { get; set; }
    public Func<nint, PipelineColorBlendStateCreateInfo>? ColorBlendFactory { get; set; }
    public Func<PipelineDepthStencilStateCreateInfo>? DepthStencilFactory { get; set; }
    public Func<VertexInputBindingDescription[]>? VertexInputDescriptionsFactory { get; set; }
    public Func<VertexInputAttributeDescription[]>? VertexInputAttributeDescriptionsFactory { get; set; }

    #region Default Factories

    public static Func<PipelineDepthStencilStateCreateInfo> DisabledDepthStencil { get; } = () => new()
    {
        SType = StructureType.PipelineDepthStencilStateCreateInfo
    };

    #endregion
    
    public unsafe PipelineDynamicStateCreateInfo CreateDynamicState(uint stateCount, DynamicState* states)
    {
        return DynamicStateFactory?.Invoke(stateCount, (nint)states) ?? new PipelineDynamicStateCreateInfo
        {
            SType = StructureType.PipelineDynamicStateCreateInfo,
            DynamicStateCount = stateCount,
            PDynamicStates = states
        };
    }

    public unsafe PipelineVertexInputStateCreateInfo CreateVertexInputState(uint bindingDescCount, VertexInputBindingDescription* bindingDescriptions,
        uint attribDescCount, VertexInputAttributeDescription* attributeDescriptions)
    {
        return VertexInputFactory?.Invoke(bindingDescCount, (nint)bindingDescriptions, attribDescCount, (nint)attributeDescriptions) 
               ?? new PipelineVertexInputStateCreateInfo
        {
            SType = StructureType.PipelineVertexInputStateCreateInfo,
            VertexBindingDescriptionCount = bindingDescCount,
            VertexAttributeDescriptionCount = attribDescCount,
            PVertexBindingDescriptions = bindingDescriptions,
            PVertexAttributeDescriptions = attributeDescriptions
        };
    }

    public PipelineInputAssemblyStateCreateInfo CreateInputAssembly()
    {
        return InputAssemblyFactory?.Invoke() ?? new PipelineInputAssemblyStateCreateInfo
        {
            SType = StructureType.PipelineInputAssemblyStateCreateInfo,
            Topology = PrimitiveTopology.TriangleList,
            PrimitiveRestartEnable = Vk.False
        };
    }

    public unsafe PipelineViewportStateCreateInfo CreateViewport(Viewport* viewport, Rect2D* scissors)
    {
        return ViewportFactory?.Invoke((nint)viewport, (nint)scissors) ?? new PipelineViewportStateCreateInfo
        {
            SType = StructureType.PipelineViewportStateCreateInfo,
            ViewportCount = 1,
            ScissorCount = 1,
            PViewports = viewport,
            PScissors = scissors
        };
    }

    public PipelineRasterizationStateCreateInfo CreateRasterization()
    {
        return RasterizationFactory?.Invoke() ?? new PipelineRasterizationStateCreateInfo
        {
            SType = StructureType.PipelineRasterizationStateCreateInfo,
            DepthClampEnable = Vk.False,
            RasterizerDiscardEnable = Vk.False,
            PolygonMode = PolygonMode.Fill,
            LineWidth = 1,
            CullMode = CullModeFlags.BackBit,
            FrontFace = FrontFace.CounterClockwise,
            DepthBiasEnable = Vk.False
        };
    }

    public PipelineMultisampleStateCreateInfo CreateMultisampling()
    {
        return MultisamplingFactory?.Invoke() ?? new PipelineMultisampleStateCreateInfo
        {
            SType = StructureType.PipelineMultisampleStateCreateInfo,
            SampleShadingEnable = Vk.False,
            RasterizationSamples = SampleCountFlags.Count1Bit
        };
    }

    public PipelineColorBlendAttachmentState CreateColorBlendAttachment()
    {
        return ColorBlendAttachmentFactory?.Invoke() ?? new PipelineColorBlendAttachmentState
        {
            ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit |
                             ColorComponentFlags.ABit,
            BlendEnable = Vk.False
        };
    }

    public unsafe PipelineColorBlendStateCreateInfo CreateColorBlend(PipelineColorBlendAttachmentState* attachments)
    {
        return ColorBlendFactory?.Invoke((nint)attachments) ?? new PipelineColorBlendStateCreateInfo
        {
            SType = StructureType.PipelineColorBlendStateCreateInfo,
            LogicOpEnable = Vk.False,
            AttachmentCount = 1,
            PAttachments = attachments
        };
    }

    public PipelineDepthStencilStateCreateInfo CreateDepthStencil()
    {
        return DepthStencilFactory?.Invoke() ?? new PipelineDepthStencilStateCreateInfo
        {
            SType = StructureType.PipelineDepthStencilStateCreateInfo,
            DepthTestEnable = Vk.True,
            DepthWriteEnable = Vk.True,
            DepthCompareOp = CompareOp.Less,
            DepthBoundsTestEnable = Vk.False,
            StencilTestEnable = Vk.False
        };
    }

    public VertexInputBindingDescription[] CreateVertexInputDescriptions()
    {
        return VertexInputDescriptionsFactory?.Invoke() ?? [VertexUtility.GetVertexBindingDescription()];
    }

    public VertexInputAttributeDescription[] CreateVertexInputAttributeDescriptions()
    {
        return VertexInputAttributeDescriptionsFactory?.Invoke() ?? VertexUtility.GetVertexAttributeDescriptions();
    }
}