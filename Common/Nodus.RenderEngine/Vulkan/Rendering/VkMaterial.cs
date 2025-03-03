using Nodus.RenderEngine.Common;
using Nodus.RenderEngine.SPIRV;
using Nodus.RenderEngine.Vulkan.DI;
using Nodus.RenderEngine.Vulkan.Meta;
using Nodus.RenderEngine.Vulkan.Utility;
using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan.Rendering;

public interface IVkMaterialContext
{
    IMaterialDefinition Definition { get; }
    IVkRenderSupplier RenderSupplier { get; }
    IShaderDescriptor ShaderDescriptor { get; }
    uint MaximumUniformSize { get; }
    VkRenderAttachmentsInfo? RenderAttachments { get; }
}

public record VkMaterialContext(
    IMaterialDefinition Definition,
    IVkRenderSupplier RenderSupplier,
    IShaderDescriptor ShaderDescriptor,
    uint MaximumUniformSize = 128,
    VkRenderAttachmentsInfo? RenderAttachments = null) : IVkMaterialContext;

/// <summary>
/// Represents a templated factory of graphical pipelines and uniform buffer mutators.
/// </summary>
public interface IVkMaterial : IVkUnmanagedHook
{
    string MaterialId { get; }
    uint MaximumUniformSize { get; }
    IMaterialDefinition Definition { get; }
    
    IVkPipeline CreatePipeline(IVkRenderPass renderPass);
    IVkMaterialInstance CreateInstance();
}

public class VkMaterial : VkObject, IVkMaterial
{
    public string MaterialId { get; }
    
    public IVkPipelineFactory? PipelineFactory { get; set; }
    public uint MaximumUniformSize { get; }
    public IMaterialDefinition Definition { get; }

    private readonly IVkMaterialContext materialContext;
    private readonly IVkDescriptorSetFactory descriptorSetFactory;
    private readonly IShaderMeta[] describedShaders;

    private readonly VkRenderAttachmentsInjector? attachmentsInjector;
    
    public VkMaterial(IVkContext vkContext, IVkMaterialContext materialContext) : base(vkContext)
    {
        this.materialContext = materialContext;
        
        MaterialId = this.materialContext.Definition.MaterialId;
        MaximumUniformSize = materialContext.MaximumUniformSize;
        Definition = materialContext.Definition;
        describedShaders = new IShaderMeta[materialContext.Definition.Shaders.Count];
        
        DescribeLayout();

        descriptorSetFactory = CreateDescriptorSetFactory();

        if (materialContext.RenderAttachments != null)
        {
            attachmentsInjector = new VkRenderAttachmentsInjector(materialContext.RenderAttachments.Value);
        }
    }

    public IVkPipeline CreatePipeline(IVkRenderPass renderPass)
    {
        var pipelineContext = new VkGraphicsPipelineContext(
            [DynamicState.Scissor, DynamicState.Viewport], Definition.Shaders, materialContext.RenderSupplier, null,
            PipelineFactory, descriptorSetFactory, attachmentsInjector != null ? [attachmentsInjector] : null);

        return new VkGraphicsPipeline(Context, Context.RenderServices.Devices.LogicalDevice, pipelineContext);
    }

    public IVkMaterialInstance CreateInstance()
    {
        var instance = CreateInstanceImpl();
        
        instance.AddDependency(this);

        return instance;
    }

    protected void DescribeLayout()
    {
        for (var i = 0; i < describedShaders.Length; i++)
        {
            describedShaders[i] = materialContext.ShaderDescriptor.Describe(materialContext.Definition.Shaders.ElementAt(i));
        }
    }

    private IVkDescriptorSetFactory CreateDescriptorSetFactory()
    {
        return new VkDescriptorSetFactory
        {
            BindingsFactory = AcquireDescriptorBindings,
            PushConstantsFactory = () => describedShaders
                .Select(x => new { Blocks = x.PushConstantBlocks, Stage = ShaderUtility.SourceTypeToStage(x.Definition.Type)})
                .Select(x => x.Blocks.Select(y => new PushConstantRange
                {
                    Offset = y.Offset,
                    Size = y.Size,
                    StageFlags = x.Stage
                })).SelectMany(x => x).ToArray()
        };
    }

    private DescriptorSetLayoutBinding[] AcquireDescriptorBindings()
    {
        return describedShaders
            .Select(x => new
                { Bindings = x.DescriptorBindings, Stage = ShaderUtility.SourceTypeToStage(x.Definition.Type) })
            .Select(x => x.Bindings.Select(y => new DescriptorSetLayoutBinding
            {
                Binding = y.Binding,
                DescriptorCount = y.Count,
                DescriptorType = y.Type,
                StageFlags = x.Stage
            })).SelectMany(x => x)
            .GroupBy(x => new { x.Binding, x.DescriptorType })
            .Select(x =>
            {
                var flags = x.Aggregate(ShaderStageFlags.None, (a, b) => a | b.StageFlags);

                return new DescriptorSetLayoutBinding
                {
                    Binding = x.Key.Binding,
                    DescriptorType = x.Key.DescriptorType,
                    DescriptorCount = x.First().DescriptorCount,
                    StageFlags = flags
                };
            }).ToArray();
    }

    protected virtual IVkMaterialInstance CreateInstanceImpl()
    {
        return new VkMaterialInstance(Context, this);
    }

    protected override void Dispose(bool disposing)
    {
        if (IsDisposing)
        {
            attachmentsInjector?.Dispose();
        }
        
        base.Dispose(disposing);
    }
}

public readonly struct VkMaterialDefinition(
    string materialId,
    uint renderPriority,
    IReadOnlyCollection<IShaderDefinition> shaders)
    : IMaterialDefinition
{
    public string MaterialId { get; } = materialId;
    public uint RenderPriority { get; } = renderPriority;
    public IReadOnlyCollection<IShaderDefinition> Shaders { get; } = shaders;
}