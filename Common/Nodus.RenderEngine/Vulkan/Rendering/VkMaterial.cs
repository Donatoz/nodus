using Nodus.RenderEngine.Common;
using Nodus.RenderEngine.Vulkan.DI;
using Nodus.RenderEngine.Vulkan.Meta;
using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan.Rendering;

public interface IVkMaterialContext
{
    IMaterialDefinition Definition { get; }
    IVkRenderSupplier RenderSupplier { get; }
    VkDescriptorRequest[] Descriptors { get; }
    VkPushConstantRequest[] PushConstants { get; }
    uint MaximumUniformSize { get; }
}

public record VkMaterialContext(
    IMaterialDefinition Definition,
    IVkRenderSupplier RenderSupplier,
    VkDescriptorRequest[] Descriptors,
    VkPushConstantRequest[] PushConstants,
    uint MaximumUniformSize = 128) : IVkMaterialContext;

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
    public VkMaterial(IVkContext vkContext, IVkMaterialContext materialContext) : base(vkContext)
    {
        this.materialContext = materialContext;
        
        MaterialId = this.materialContext.Definition.MaterialId;
        MaximumUniformSize = materialContext.MaximumUniformSize;
        Definition = materialContext.Definition;

        descriptorSetFactory = CreateDescriptorSetFactory();
    }

    public IVkPipeline CreatePipeline(IVkRenderPass renderPass)
    {
        var pipelineContext = new VkGraphicsPipelineContext(
            [DynamicState.Scissor, DynamicState.Viewport], Definition.Shaders, renderPass, materialContext.RenderSupplier,
            PipelineFactory, descriptorSetFactory);

        return new VkGraphicsPipeline(Context, Context.RenderServices.Devices.LogicalDevice, pipelineContext);
    }

    public IVkMaterialInstance CreateInstance()
    {
        var instance = CreateInstanceImpl();
        
        instance.AddDependency(this);

        return instance;
    }

    private IVkDescriptorSetFactory CreateDescriptorSetFactory()
    {
        return new VkDescriptorSetFactory
        {
            BindingsFactory = () => materialContext.Descriptors.Select(x => new DescriptorSetLayoutBinding
            {
                Binding = x.Binding,
                DescriptorType = x.Type,
                DescriptorCount = x.Count,
                StageFlags = x.Stages
            }).ToArray(),
            PushConstantsFactory = () => materialContext.PushConstants.Select(x => x.Range).ToArray()
        };
    }

    protected virtual IVkMaterialInstance CreateInstanceImpl()
    {
        return new VkMaterialInstance(Context, this);
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