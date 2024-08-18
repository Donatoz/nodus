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

public interface IVkPipelineContext
{
    DynamicState[] DynamicStates { get; }
    IEnumerable<IShaderDefinition> Shaders { get; }
    IVkSwapChain SwapChain { get; }
    VkQueueInfo QueueInfo { get; }
    IVkPipelineFactory? PipelineFactory { get; }
}

public readonly struct VkPipelineContext(
    DynamicState[] dynamicStates, 
    IEnumerable<IShaderDefinition> shaders, 
    IVkSwapChain swapChain,
    VkQueueInfo queueInfo,
    IVkPipelineFactory? pipelineFactory = null) : IVkPipelineContext
{
    public DynamicState[] DynamicStates { get; } = dynamicStates;
    public IEnumerable<IShaderDefinition> Shaders { get; } = shaders;
    public IVkSwapChain SwapChain { get; } = swapChain;
    public VkQueueInfo QueueInfo { get; } = queueInfo;
    public IVkPipelineFactory? PipelineFactory { get; } = pipelineFactory;
}

public interface IVkPipeline : IVkUnmanagedHook
{
    PipelineLayout Layout { get; }
    Pipeline WrappedPipeline { get; }
    RenderPass RenderPass { get; }
    Viewport Viewport { get; }
    Rect2D Scissors { get; }
    DescriptorSetLayout DescriptorSetLayout { get; }

    void Bind(CommandBuffer buffer, PipelineBindPoint bindPoint);
    void UpdateViewport();
    void UpdateScissors();
}

public class VkPipeline : VkObject, IVkPipeline
{
    public PipelineLayout Layout { get; }
    public RenderPass RenderPass { get; }
    public Pipeline WrappedPipeline { get; }
    public Viewport Viewport { get; private set; }
    public Rect2D Scissors { get; private set; }
    public DescriptorSetLayout DescriptorSetLayout { get; }


    protected IDictionary<ShaderSourceType, ShaderModule> Modules { get; }

    private DescriptorSetLayout descriptorSetLayout;
    private readonly IVkLogicalDevice device;
    private readonly IVkPipelineContext pipelineContext;

    public unsafe VkPipeline(IVkContext vkContext, IVkLogicalDevice device, IVkPipelineContext pipelineContext) : base(vkContext)
    {
        this.device = device;
        this.pipelineContext = pipelineContext;

        var factory = pipelineContext.PipelineFactory ?? new VkPipelineFactory();
        
        Modules = pipelineContext.Shaders.Select(x => new
        {
            Source = x,
            Module = CreateModule(x.Source)
        }).ToDictionary(x => x.Source.Type, x => x.Module);

        var stages = Modules.Select(x => new PipelineShaderStageCreateInfo
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderUtility.SourceTypeToStage(x.Key),
            Module = x.Value,
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


        var dscSetLayout = CreateDescriptorLayout();
        
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

        var colorAttachment = new AttachmentDescription
        {
            Format = pipelineContext.SwapChain.SurfaceFormat.Format,
            Samples = SampleCountFlags.Count1Bit,
            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.Store,
            StencilLoadOp = AttachmentLoadOp.DontCare,
            StencilStoreOp = AttachmentStoreOp.DontCare,
            InitialLayout = ImageLayout.Undefined,
            FinalLayout = ImageLayout.PresentSrcKhr
        };

        var colorAttachmentRef = new AttachmentReference
        {
            Attachment = 0,
            Layout = ImageLayout.ColorAttachmentOptimal
        };

        var subPass = new SubpassDescription
        {
            PipelineBindPoint = PipelineBindPoint.Graphics,
            ColorAttachmentCount = 1,
            PColorAttachments = &colorAttachmentRef
        };

        var subPassDependency = new SubpassDependency
        {
            SrcSubpass = Vk.SubpassExternal,
            DstSubpass = 0,
            SrcStageMask = PipelineStageFlags.ColorAttachmentOutputBit,
            SrcAccessMask = 0,
            DstStageMask = PipelineStageFlags.ColorAttachmentOutputBit,
            DstAccessMask = AccessFlags.ColorAttachmentWriteBit
        };

        var renderPass = new RenderPassCreateInfo
        {
            SType = StructureType.RenderPassCreateInfo,
            AttachmentCount = 1,
            PAttachments = &colorAttachment,
            SubpassCount = 1,
            PSubpasses = &subPass,
            DependencyCount = 1,
            PDependencies = &subPassDependency
        };

        RenderPass pass;
        
        Context.Api.CreateRenderPass(device.WrappedDevice, in renderPass, null, &pass)
            .TryThrow("Failed to create render pass.");

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
                StageCount = 2,
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

    protected unsafe ShaderModule CreateModule(IShaderSource source)
    {
        var content = FetchBytesFromSource(source);

        var createInfo = new ShaderModuleCreateInfo
        {
            SType = StructureType.ShaderModuleCreateInfo,
            CodeSize = (nuint)content.Length
        };

        ShaderModule module;

        fixed (byte* p = content)
        {
            createInfo.PCode = (uint*)p;

            if (Context.Api.CreateShaderModule(device.WrappedDevice, in createInfo, null, out module) != Result.Success)
            {
                throw new Exception($"Failed to create shader module from source: {source}");
            }
        }

        return module;
    }

    protected virtual byte[] FetchBytesFromSource(IShaderSource source)
    {
        if (source is not IShaderByteSource s)
        {
            throw new Exception(
                $"Failed to create shader module from source: {source}. Only byte-sources are supported.");
        }

        return s.FetchBytes();
    }

    private unsafe DescriptorSetLayout CreateDescriptorLayout()
    {
        var binding = new DescriptorSetLayoutBinding
        {
            Binding = 0,
            DescriptorType = DescriptorType.UniformBuffer,
            DescriptorCount = 1,
            StageFlags = ShaderStageFlags.VertexBit
        };

        var createInfo = new DescriptorSetLayoutCreateInfo
        {
            SType = StructureType.DescriptorSetLayoutCreateInfo,
            BindingCount = 1,
            PBindings = &binding
        };

        DescriptorSetLayout layout;
        Context.Api.CreateDescriptorSetLayout(device.WrappedDevice, in createInfo, null, &layout)
            .TryThrow("Failed to create descriptor set layout.");

        return layout;
    }

    public void Bind(CommandBuffer buffer, PipelineBindPoint bindPoint)
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
            Modules.Values.ForEach(x => Context.Api.DestroyShaderModule(device.WrappedDevice, x, null));
            Context.Api.DestroyPipeline(device.WrappedDevice, WrappedPipeline, null);
            Context.Api.DestroyPipelineLayout(device.WrappedDevice, Layout,  null);
            Context.Api.DestroyRenderPass(device.WrappedDevice, RenderPass, null);
            Context.Api.DestroyDescriptorSetLayout(device.WrappedDevice, descriptorSetLayout, null);
        }
        
        base.Dispose(disposing);
    }
}