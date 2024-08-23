using Nodus.Core.Extensions;
using Nodus.RenderEngine.Common;
using Nodus.RenderEngine.Vulkan.Convention;
using Nodus.RenderEngine.Vulkan.DI;
using Nodus.RenderEngine.Vulkan.Extensions;
using Nodus.RenderEngine.Vulkan.Meta;
using Nodus.RenderEngine.Vulkan.Utility;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan;

public interface IVkGraphicsPipelineContext
{
    DynamicState[] DynamicStates { get; }
    IEnumerable<IShaderDefinition> Shaders { get; }
    IVkSwapChain SwapChain { get; }
    VkQueueInfo QueueInfo { get; }
    
    IVkPipelineFactory? PipelineFactory { get; }
    IVkRenderPassFactory? RenderPassFactory { get; }
    IVkDescriptorSetFactory? DescriptorFactory { get; }
}

public readonly struct VkGraphicsPipelineContext(
    DynamicState[] dynamicStates, 
    IEnumerable<IShaderDefinition> shaders, 
    IVkSwapChain swapChain,
    VkQueueInfo queueInfo,
    IVkPipelineFactory? pipelineFactory = null,
    IVkRenderPassFactory? renderPassFactory = null,
    IVkDescriptorSetFactory? descriptorFactory = null) : IVkGraphicsPipelineContext
{
    public DynamicState[] DynamicStates { get; } = dynamicStates;
    public IEnumerable<IShaderDefinition> Shaders { get; } = shaders;
    public IVkSwapChain SwapChain { get; } = swapChain;
    public VkQueueInfo QueueInfo { get; } = queueInfo;
    public IVkPipelineFactory? PipelineFactory { get; } = pipelineFactory;
    public IVkRenderPassFactory? RenderPassFactory { get; } = renderPassFactory;
    public IVkDescriptorSetFactory? DescriptorFactory { get; } = descriptorFactory;
}

public interface IVkGraphicsPipeline : IVkUnmanagedHook
{
    PipelineLayout Layout { get; }
    Pipeline WrappedPipeline { get; }
    RenderPass RenderPass { get; }
    Viewport Viewport { get; }
    Rect2D Scissors { get; }
    DescriptorSetLayout DescriptorSetLayout { get; }

    void CmdBind(CommandBuffer buffer, PipelineBindPoint bindPoint);
    void UpdateViewport();
    void UpdateScissors();
}

public class VkGraphicsPipeline : VkObject, IVkGraphicsPipeline
{
    public PipelineLayout Layout { get; }
    public RenderPass RenderPass { get; }
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
        var passFactory = pipelineContext.RenderPassFactory ?? new VkRenderPassFactory();
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
            Width = pipelineContext.SwapChain.Extent.Width,
            Height = pipelineContext.SwapChain.Extent.Height,
            MinDepth = 0,
            MaxDepth = 1
        };
        
        var scissors = new Rect2D
        {
            Offset = new Offset2D(0, 0),
            Extent = pipelineContext.SwapChain.Extent
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

        var attachments = passFactory.CreateAttachments(this.pipelineContext.SwapChain.SurfaceFormat.Format);
        var colorAttachmentRef = new AttachmentReference
        {
            Attachment = 0,
            Layout = ImageLayout.ColorAttachmentOptimal
        };
        var subPasses = passFactory.CreateSubPasses([
            new VkSubPassScheme(PipelineBindPoint.Graphics, 1, &colorAttachmentRef)
        ]);
        var dependencies = passFactory.CreateDependencies();

        RenderPass pass;

        fixed (void* pAttachments = attachments, pSubPasses = subPasses, pDeps = dependencies)
        {
            var renderPass = new RenderPassCreateInfo
            {
                SType = StructureType.RenderPassCreateInfo,
                AttachmentCount = (uint)attachments.Length,
                PAttachments = (AttachmentDescription*)pAttachments,
                SubpassCount = (uint)subPasses.Length,
                PSubpasses = (SubpassDescription*)pSubPasses,
                DependencyCount = (uint)dependencies.Length,
                PDependencies = (SubpassDependency*)pDeps
            };
            
            Context.Api.CreateRenderPass(device.WrappedDevice, in renderPass, null, &pass)
                .TryThrow("Failed to create render pass.");
        }

        RenderPass = pass;
        
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
                RenderPass = RenderPass,
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

    public void CmdBind(CommandBuffer buffer, PipelineBindPoint bindPoint)
    {
        Context.Api.CmdBindPipeline(buffer, bindPoint, WrappedPipeline);
    }

    public void UpdateViewport()
    {
        Viewport = new Viewport
        {
            X = 0,
            Y = 0,
            Width = pipelineContext.SwapChain.Extent.Width,
            Height = pipelineContext.SwapChain.Extent.Height,
            MinDepth = 0,
            MaxDepth = 1
        };
    }

    public void UpdateScissors()
    {
        Scissors = new Rect2D
        {
            Offset = new Offset2D(0, 0),
            Extent = pipelineContext.SwapChain.Extent
        };
    }

    protected override unsafe void Dispose(bool disposing)
    {
        if (IsDisposing)
        {
            Shaders.Values.DisposeAll();
            Context.Api.DestroyPipeline(device.WrappedDevice, WrappedPipeline, null);
            Context.Api.DestroyPipelineLayout(device.WrappedDevice, Layout,  null);
            Context.Api.DestroyRenderPass(device.WrappedDevice, RenderPass, null);
            Context.Api.DestroyDescriptorSetLayout(device.WrappedDevice, descriptorSetLayout, null);
        }
        
        base.Dispose(disposing);
    }
}