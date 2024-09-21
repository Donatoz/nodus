using Nodus.RenderEngine.Common;
using Nodus.RenderEngine.Vulkan;
using Nodus.RenderEngine.Vulkan.DI;
using Nodus.RenderEngine.Vulkan.Meta;
using Nodus.RenderEngine.Vulkan.Primitives;
using Nodus.RenderEngine.Vulkan.Rendering;
using Silk.NET.Vulkan;

namespace Nodus.VisualTests.Materials;

public class CommonMaterialsFactory
{
    private readonly IVkContext vkContext;
    private readonly IVkRenderSupplier renderSupplier;

    public CommonMaterialsFactory(IVkContext vkContext, IVkRenderSupplier renderSupplier)
    {
        this.vkContext = vkContext;
        this.renderSupplier = renderSupplier;
    }
    
    public unsafe IVkMaterial CreateSolidMaterial(uint renderPriority = 0)
    {
        return new VkMaterial(vkContext, new VkMaterialContext(new VkMaterialDefinition(Guid.NewGuid().ToString(), renderPriority, [
            new ShaderDefinition(
                new ShaderFileSource(@"C:\Users\Donatoz\RiderProjects\Nodus\Nodus.VisualTests\Assets\Shaders\compiled\standard2.vert.spv"),
                ShaderSourceType.Vertex),
            new ShaderDefinition(
                new ShaderFileSource(@"C:\Users\Donatoz\RiderProjects\Nodus\Nodus.VisualTests\Assets\Shaders\compiled\solid.frag.spv"),
                ShaderSourceType.Fragment)
            ]),
            renderSupplier,
            [
                new VkDescriptorRequest(DescriptorType.UniformBuffer, 1, 0, ShaderStageFlags.VertexBit, VkDescriptorContent.Transformations),
                new VkDescriptorRequest(DescriptorType.CombinedImageSampler, 1, 1, ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit, VkDescriptorContent.PrimaryTextures),
                new VkDescriptorRequest(DescriptorType.UniformBuffer, 1, 2, ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit)
            ],
            [
                new VkPushConstantRequest(new PushConstantRange(ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit, 0, sizeof(float)), 
                    VkPushConstantsContent.FrameTime)
            ],
            (uint)sizeof(SolidUniformBlock)));
    }
}