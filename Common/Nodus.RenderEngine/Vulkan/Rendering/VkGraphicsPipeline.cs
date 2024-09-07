using Nodus.Common;
using Nodus.Core.Extensions;
using Nodus.RenderEngine.Common;
using Nodus.RenderEngine.Vulkan.Convention;
using Nodus.RenderEngine.Vulkan.DI;
using Nodus.RenderEngine.Vulkan.Extensions;
using Nodus.RenderEngine.Vulkan.Utility;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan.Rendering;

public interface IVkGraphicsPipelineContext
{
    DynamicState[] DynamicStates { get; }
    IEnumerable<IShaderDefinition> Shaders { get; }
    IVkRenderSupplier Supplier { get; }
    IVkRenderPass RenderPass { get; }
    IVkPipelineFactory? PipelineFactory { get; }
    IVkDescriptorSetFactory? DescriptorFactory { get; }
}

public record VkGraphicsPipelineContext(
    DynamicState[] DynamicStates,
    IEnumerable<IShaderDefinition> Shaders,
    IVkRenderPass RenderPass,
    IVkRenderSupplier Supplier,
    IVkPipelineFactory? PipelineFactory = null,
    IVkDescriptorSetFactory? DescriptorFactory = null) : IVkGraphicsPipelineContext;

public interface IVkGraphicsPipeline : IVkPipeline
{
    Viewport Viewport { get; }
    Rect2D Scissors { get; }

    void UpdateViewport();
    void UpdateScissors();
}

public class VkGraphicsPipeline : VkObject, IVkGraphicsPipeline
{
    public PipelineLayout Layout { get; }
    public Pipeline WrappedPipeline { get; }
    public Viewport Viewport { get; private set; }
    public Rect2D Scissors { get; private set; }
    public DescriptorSetLayout[] DescriptorSetLayouts { get; }
    
    protected IDictionary<ShaderSourceType, IVkShader> Shaders { get; }

    private readonly IVkLogicalDevice device;
    private readonly IVkGraphicsPipelineContext pipelineContext;
    private readonly IVkDescriptorSetFactory descriptorFactory;

    public unsafe VkGraphicsPipeline(IVkContext vkContext, IVkLogicalDevice device, IVkGraphicsPipelineContext pipelineContext) : base(vkContext)
    {
        this.device = device;
        this.pipelineContext = pipelineContext;

        var factory = pipelineContext.PipelineFactory ?? VkPipelineFactory.DefaultPipelineFactory;
        descriptorFactory = pipelineContext.DescriptorFactory ?? VkDescriptorSetFactory.DefaultDescriptorSetFactory;
        
        Shaders = pipelineContext.Shaders.Select(x => new
        {
            Source = x,
            Module = new VkShader(Context, device, x) as IVkShader
        }).ToDictionary(x => x.Source.Type, x => x.Module);

        var stages = Shaders.Select(x => new PipelineShaderStageCreateInfo
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderUtility.SourceTypeToStage(x.Key),
            Module = x.Value.WrappedModule,
            PName = (byte*)SilkMarshal.StringToPtr(ShaderConvention.EntryMethodName)
        }).ToArray();
        
        var dynamicStates = stackalloc DynamicState[pipelineContext.DynamicStates.Length];
        
        for (var i = 0; i < pipelineContext.DynamicStates.Length; i++)
        {
            dynamicStates[i] = pipelineContext.DynamicStates[i];
        }
        
        var viewPort = new Viewport
        {
            X = 0,
            Y = 0,
            Width = pipelineContext.Supplier.CurrentRenderExtent.Width,
            Height = pipelineContext.Supplier.CurrentRenderExtent.Height,
            MinDepth = 0,
            MaxDepth = 1
        };
        
        var scissors = new Rect2D
        {
            Offset = new Offset2D(0, 0),
            Extent = pipelineContext.Supplier.CurrentRenderExtent
        };
        
        Viewport = viewPort;
        Scissors = scissors;

        var dynamicState = factory.CreateDynamicState((uint)pipelineContext.DynamicStates.Length, dynamicStates);
        var inputAssembly = factory.CreateInputAssembly();
        var viewPortState = factory.CreateViewport(&viewPort, &scissors);
        var rasterizer = factory.CreateRasterization();
        var multisampling = factory.CreateMultisampling();
        var colorBlendAttachment = factory.CreateColorBlendAttachment();
        var colorBlend = factory.CreateColorBlend(&colorBlendAttachment);
        var depthStencil = factory.CreateDepthStencil();
        
        var bindings = descriptorFactory.CreateLayoutBindings();
        var dscSetLayouts = CreateDescriptorLayouts(bindings).ToArray();
        var pushConstants = descriptorFactory.CreatePushConstants();

        fixed (DescriptorSetLayout* pDesc = dscSetLayouts)
        fixed (PushConstantRange* pConst = pushConstants)
        {
            var layoutInfo = new PipelineLayoutCreateInfo
            {
                SType = StructureType.PipelineLayoutCreateInfo,
                SetLayoutCount = (uint)dscSetLayouts.Length,
                PSetLayouts = pDesc,
                PushConstantRangeCount = (uint)pushConstants.Length,
                PPushConstantRanges = pConst
            };
            
            Context.Api.CreatePipelineLayout(device.WrappedDevice, &layoutInfo, null, out var layout)
                .TryThrow("Failed to create pipeline layout.");
            
            Layout = layout;
        }
        
        DescriptorSetLayouts = dscSetLayouts;
        
        var vertexBindings = factory.CreateVertexInputDescriptions();
        var vertexAttribs = factory.CreateVertexInputAttributeDescriptions();

        fixed (VertexInputBindingDescription* pVertBindings = vertexBindings)
        fixed (VertexInputAttributeDescription* pVertAttribs = vertexAttribs)
        fixed (PipelineShaderStageCreateInfo* pStages = stages)
        {
            var vertexInput = factory.CreateVertexInputState((uint)vertexBindings.Length, pVertBindings, 
                (uint)vertexAttribs.Length, pVertAttribs);

            var pipelineInfo = new GraphicsPipelineCreateInfo
            {
                SType = StructureType.GraphicsPipelineCreateInfo,
                StageCount = (uint)stages.Length,
                PStages = pStages,
                PDynamicState = &dynamicState,
                PVertexInputState = &vertexInput,
                PInputAssemblyState = &inputAssembly,
                PViewportState = &viewPortState,
                PRasterizationState = &rasterizer,
                PMultisampleState = &multisampling,
                PColorBlendState = &colorBlend,
                PDepthStencilState = &depthStencil,
                Layout = Layout,
                RenderPass = pipelineContext.RenderPass.WrappedPass,
                Subpass = 0
            };

            Pipeline pipeline;
            
            Context.Api.CreateGraphicsPipelines(device.WrappedDevice, default, 1, &pipelineInfo, null, &pipeline)
                .TryThrow("Failed to create pipeline.");

            WrappedPipeline = pipeline;
        }
        
        stages.ForEach(x => SilkMarshal.Free((nint)x.PName));
    }

    private unsafe IList<DescriptorSetLayout> CreateDescriptorLayouts(DescriptorSetLayoutBinding[] bindings)
    {
        using var fixedBindings = bindings.ToFixedArray();

        var createInfos = descriptorFactory.CreateDescriptorSetLayouts(fixedBindings);
        var layouts = new List<DescriptorSetLayout>();

        foreach (var info in createInfos)
        {
            DescriptorSetLayout layout;
            Context.Api.CreateDescriptorSetLayout(device.WrappedDevice, in info, null, &layout)
                .TryThrow("Failed to create descriptor set layout.");
            
            layouts.Add(layout);
        }
        
        return layouts;
    }

    public void UpdateViewport()
    {
        Viewport = new Viewport
        {
            X = 0,
            Y = 0,
            Width = pipelineContext.Supplier.CurrentRenderExtent.Width,
            Height = pipelineContext.Supplier.CurrentRenderExtent.Height,
            MinDepth = 0,
            MaxDepth = 1
        };
    }

    public void UpdateScissors()
    {
        Scissors = new Rect2D
        {
            Offset = new Offset2D(0, 0),
            Extent = pipelineContext.Supplier.CurrentRenderExtent
        };
    }

    protected override unsafe void Dispose(bool disposing)
    {
        if (IsDisposing)
        {
            Shaders.Values.DisposeAll();
            Context.Api.DestroyPipeline(device.WrappedDevice, WrappedPipeline, null);
            Context.Api.DestroyPipelineLayout(device.WrappedDevice, Layout,  null);
            DescriptorSetLayouts.ForEach(x => Context.Api.DestroyDescriptorSetLayout(device.WrappedDevice, x, null));
        }
        
        base.Dispose(disposing);
    }
}