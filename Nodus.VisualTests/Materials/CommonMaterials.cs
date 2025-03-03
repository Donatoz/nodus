using Nodus.RenderEngine.Common;
using Nodus.RenderEngine.SPIRV;
using Nodus.RenderEngine.Vulkan;
using Nodus.RenderEngine.Vulkan.DI;
using Nodus.RenderEngine.Vulkan.Meta;
using Nodus.RenderEngine.Vulkan.Primitives;
using Nodus.RenderEngine.Vulkan.Rendering;
using Silk.NET.SPIRV.Reflect;
using Silk.NET.Vulkan;
using DescriptorType = Silk.NET.Vulkan.DescriptorType;

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
    
    public unsafe IVkMaterial CreateSolidMaterial(uint renderPriority = 0, VkRenderAttachmentsInfo? renderAttachments = null)
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
            ShaderDescriptor.Default,
            (uint)sizeof(SolidUniformBlock), renderAttachments));
    }
}