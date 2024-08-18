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
    /// Create a Vulkan input assembly state info.
    /// </summary>
    PipelineInputAssemblyStateCreateInfo CreateInputAssembly();

    /// <summary>
    /// Create a viewport state info.
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

    /// <summary>
    /// Create a color blend state info with the specified attachments.
    /// </summary>
    /// <param name="attachments">A pointer to the array of color blend attachment states.</param>
    unsafe PipelineColorBlendStateCreateInfo CreateColorBlend(PipelineColorBlendAttachmentState* attachments);
}

public class VkPipelineFactory : IVkPipelineFactory
{
    public IFactory<uint, nint, PipelineDynamicStateCreateInfo>? DynamicStateFactory { get; set; }
    public IFactory<uint, nint, uint, nint, PipelineVertexInputStateCreateInfo>? VertexInputFactory { get; set; }
    public IFactory<PipelineInputAssemblyStateCreateInfo>? InputAssemblyFactory { get; set; }
    public IFactory<nint, nint, PipelineViewportStateCreateInfo>? ViewportFactory { get; set; }
    public IFactory<PipelineRasterizationStateCreateInfo>? RasterizationFactory { get; set; }
    public IFactory<PipelineMultisampleStateCreateInfo>? MultisamplingFactory { get; set; }
    public IFactory<nint, PipelineColorBlendStateCreateInfo>? ColorBlendFactory { get; set; }
    
    public unsafe PipelineDynamicStateCreateInfo CreateDynamicState(uint stateCount, DynamicState* states)
    {
        return DynamicStateFactory?.Create(stateCount, (nint)states) ?? new PipelineDynamicStateCreateInfo
        {
            SType = StructureType.PipelineDynamicStateCreateInfo,
            DynamicStateCount = stateCount,
            PDynamicStates = states
        };
    }

    public unsafe PipelineVertexInputStateCreateInfo CreateVertexInputState(uint bindingDescCount, VertexInputBindingDescription* bindingDescriptions,
        uint attribDescCount, VertexInputAttributeDescription* attributeDescriptions)
    {
        return VertexInputFactory?.Create(bindingDescCount, (nint)bindingDescriptions, attribDescCount, (nint)attributeDescriptions) 
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
        return InputAssemblyFactory?.Create() ?? new PipelineInputAssemblyStateCreateInfo
        {
            SType = StructureType.PipelineInputAssemblyStateCreateInfo,
            Topology = PrimitiveTopology.TriangleList,
            PrimitiveRestartEnable = Vk.False
        };
    }

    public unsafe PipelineViewportStateCreateInfo CreateViewport(Viewport* viewport, Rect2D* scissors)
    {
        return ViewportFactory?.Create((nint)viewport, (nint)scissors) ?? new PipelineViewportStateCreateInfo
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
        return RasterizationFactory?.Create() ?? new PipelineRasterizationStateCreateInfo
        {
            SType = StructureType.PipelineRasterizationStateCreateInfo,
            DepthClampEnable = Vk.False,
            RasterizerDiscardEnable = Vk.False,
            PolygonMode = PolygonMode.Fill,
            LineWidth = 1,
            CullMode = CullModeFlags.BackBit,
            FrontFace = FrontFace.Clockwise,
            DepthBiasEnable = Vk.False
        };
    }

    public PipelineMultisampleStateCreateInfo CreateMultisampling()
    {
        return MultisamplingFactory?.Create() ?? new PipelineMultisampleStateCreateInfo
        {
            SType = StructureType.PipelineMultisampleStateCreateInfo,
            SampleShadingEnable = Vk.False,
            RasterizationSamples = SampleCountFlags.Count1Bit
        };
    }

    public unsafe PipelineColorBlendStateCreateInfo CreateColorBlend(PipelineColorBlendAttachmentState* attachments)
    {
        return ColorBlendFactory?.Create((nint)attachments) ?? new PipelineColorBlendStateCreateInfo
        {
            SType = StructureType.PipelineColorBlendStateCreateInfo,
            LogicOpEnable = Vk.False,
            AttachmentCount = 1,
            PAttachments = attachments
        };
    }
}