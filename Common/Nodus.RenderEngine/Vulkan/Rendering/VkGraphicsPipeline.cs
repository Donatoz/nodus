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

public readonly struct VkGraphicsPipelineContext(
    DynamicState[] dynamicStates, 
    IEnumerable<IShaderDefinition> shaders, 
    IVkRenderPass renderPass,
    IVkRenderSupplier supplier,
    IVkPipelineFactory? pipelineFactory = null,
    IVkDescriptorSetFactory? descriptorFactory = null) : IVkGraphicsPipelineContext
{
    public DynamicState[] DynamicStates { get; } = dynamicStates;
    public IEnumerable<IShaderDefinition> Shaders { get; } = shaders;
    public IVkRenderSupplier Supplier { get; } = supplier;
    public IVkRenderPass RenderPass { get; } = renderPass;
    public IVkPipelineFactory? PipelineFactory { get; } = pipelineFactory;
    public IVkDescriptorSetFactory? DescriptorFactory { get; } = descriptorFactory;
}

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
    public DescriptorSetLayout DescriptorSetLayout { get; }
    
    protected IDictionary<ShaderSourceType, IVkShader> Shaders { get; }

    private DescriptorSetLayout descriptorSetLayout;
    private readonly IVkLogicalDevice device;
    private readonly IVkGraphicsPipelineContext pipelineContext;

    public unsafe VkGraphicsPipeline(IVkContext vkContext, IVkLogicalDevice device, IVkGraphicsPipelineContext pipelineContext) : base(vkContext)
    {
        this.device = device;
        this.pipelineContext = pipelineContext;

        var factory = pipelineContext.PipelineFactory ?? new VkPipelineFactory();
        var descriptorFactory = pipelineContext.DescriptorFactory ?? new VkDescriptorSetFactory();
        
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
        
        var colorBlendAttachment = new PipelineColorBlendAttachmentState
        {
            ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit | ColorComponentFlags.ABit,
            BlendEnable = Vk.False
        };
        
        var dynamicState = factory.CreateDynamicState((uint)pipelineContext.DynamicStates.Length, dynamicStates);
        var inputAssembly = factory.CreateInputAssembly();
        var viewPortState = factory.CreateViewport(&viewPort, &scissors);
        var rasterizer = factory.CreateRasterization();
        var multisampling = factory.CreateMultisampling();
        var colorBlend = factory.CreateColorBlend(&colorBlendAttachment);


        var bindings = descriptorFactory.CreateLayoutBindings();
        var dscSetLayout = CreateDescriptorLayout(bindings);
        
        var layoutInfo = new PipelineLayoutCreateInfo
        {
            SType = StructureType.PipelineLayoutCreateInfo,
            SetLayoutCount = 1,
            PSetLayouts = &dscSetLayout
        };

        descriptorSetLayout = dscSetLayout;
        
        Context.Api.CreatePipelineLayout(device.WrappedDevice, &layoutInfo, null, out var layout)
            .TryThrow("Failed to create pipeline layout.");

        DescriptorSetLayout = descriptorSetLayout;

        Layout = layout;
        
        var vertexBinding = VertexUtility.GetVertexBindingDescription();
        var vertexAttribs = VertexUtility.GetVertexAttributeDescriptions();

        fixed (void* pStages = stages, vertexAttributes = vertexAttribs)
        {
            var vertexInput = factory.CreateVertexInputState(1, &vertexBinding, 
                (uint)vertexAttribs.Length, (VertexInputAttributeDescription*)vertexAttributes);

            var pipelineInfo = new GraphicsPipelineCreateInfo
            {
                SType = StructureType.GraphicsPipelineCreateInfo,
                StageCount = (uint)stages.Length,
                PStages = (PipelineShaderStageCreateInfo*)pStages,
                PDynamicState = &dynamicState,
                PVertexInputState = &vertexInput,
                PInputAssemblyState = &inputAssembly,
                PViewportState = &viewPortState,
                PRasterizationState = &rasterizer,
                PMultisampleState = &multisampling,
                PColorBlendState = &colorBlend,
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

    private unsafe DescriptorSetLayout CreateDescriptorLayout(DescriptorSetLayoutBinding[] bindings)
    {
        DescriptorSetLayout layout;

        fixed (DescriptorSetLayoutBinding* p = bindings)
        {
            var createInfo = new DescriptorSetLayoutCreateInfo
            {
                SType = StructureType.DescriptorSetLayoutCreateInfo,
                BindingCount = (uint)bindings.Length,
                PBindings = p
            };

            Context.Api.CreateDescriptorSetLayout(device.WrappedDevice, in createInfo, null, &layout)
                .TryThrow("Failed to create descriptor set layout.");
        }

        return layout;
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
            Context.Api.DestroyDescriptorSetLayout(device.WrappedDevice, descriptorSetLayout, null);
        }
        
        base.Dispose(disposing);
    }
}