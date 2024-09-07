using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan;

public interface IVkPipeline : IVkUnmanagedHook
{
    Pipeline WrappedPipeline { get; }
    PipelineLayout Layout { get; }
    DescriptorSetLayout[] DescriptorSetLayouts { get; }
}