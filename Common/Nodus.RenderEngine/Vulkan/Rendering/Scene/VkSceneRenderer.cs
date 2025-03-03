using Nodus.Core.Extensions;
using Nodus.RenderEngine.Common;
using Nodus.RenderEngine.Vulkan.Components;
using Nodus.RenderEngine.Vulkan.Convention;
using Nodus.RenderEngine.Vulkan.DI;
using Nodus.RenderEngine.Vulkan.Memory;
using Nodus.RenderEngine.Vulkan.Meta;
using Nodus.RenderEngine.Vulkan.Presentation;
using Nodus.RenderEngine.Vulkan.Primitives;
using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan.Rendering;

public interface IVkSceneRendererContext : IVkMultiBufferRenderContext
{
    IRenderScene Scene { get; }
    IVkMaterialInstance[] Materials { get; }
}

public record VkSceneRenderContext(
    int MaxConcurrentFrames,
    IVkRenderPresenter Presenter,
    IVkRenderPass RenderPass,
    IVkRenderSupplier RenderSupplier,
    IVkRenderComponent[]? Components,
    IRenderScene Scene,
    IVkMaterialInstance[] Materials)
    : VkMultiBufferRenderContext(MaxConcurrentFrames, Presenter, RenderPass, RenderSupplier, Components),
        IVkSceneRendererContext;

public class VkSceneRenderer : VkMultiBufferRenderer
{
    // Renderer will only use these fields after the initialization.
    private IList<IRenderedObject> renderedObjects = null!;
    
    private Dictionary<string, IVkMaterialInstance> materials;
    private Dictionary<IVkMaterialInstance, MaterialState> materialStates;
    
    private IVkMemoryLease objectsMemory = null!;
    private IVkBoundBuffer objectsBuffer = null!;
    private IVkDescriptorWriter descriptorWriter = null!;
    private IVkDescriptorPool descriptorPool = null!;

    private bool isInitialized;
    
    public VkSceneRenderer()
    {
        materials = new Dictionary<string, IVkMaterialInstance>();
        materialStates = new Dictionary<IVkMaterialInstance, MaterialState>();
    }

    protected override void Initialize(IVkRenderContext context)
    {
        base.Initialize(context);
        
        TryDiscardCurrentState();

        isInitialized = false;

        var sceneContext = context.MustBe<IVkSceneRendererContext>();
        renderedObjects = sceneContext.Scene.RenderedObjects;
        sceneContext.Materials.ForEach(x =>
        {
            materials[x.Parent.MaterialId] = x;
            materialStates[x] = new MaterialState(x, sceneContext.RenderPass);
        });

        descriptorWriter = new VkDescriptorWriter([]);
        var descPoolContext = new VkDescriptorPoolContext(
        [
            new VkDescriptorInfo { Type = DescriptorType.UniformBuffer, Count = (uint)MaxConcurrentFrames}
        ], descriptorWriter);
        //descriptorPool = new VkDescriptorPool(Context!, Device!, descPoolContext, (uint)MaxConcurrentFrames, )
        
        PopulateObjectBuffer();
        PopulateDescriptors();

        isInitialized = true;
    }

    private unsafe void PopulateObjectBuffer()
    {
        // Calculate verts and indices size
        var objectsVertSize = (ulong)renderedObjects.Sum(x => (uint)(sizeof(Vertex) * x.Mesh.Vertices.Length));
        var objectsIdxSize = (ulong)renderedObjects.Sum(x => (uint)(sizeof(uint) * x.Mesh.Indices.Length));
        
        // Calculate uniforms size
        var baseUniformSize = sizeof(MvpUniformBufferObject);
        var baseUniformOffset = objectsVertSize + objectsIdxSize;
        var uniformAlignment = Context!.RenderServices.Devices.PhysicalDevice.Properties.Limits
            .MinUniformBufferOffsetAlignment;
        
        // Object buffer layout:
        // [Vertices] <objectsVertSize> [Indices] <uniformOffset> [Mat1_Uniform, Mat2_Uniform, etc.]
        
        // Material uniform layout:
        // MVP (binding 0) <baseUniformSize> 
        // Uniforms (binding 2) <material.maxUniformSize>
        
        var bufferSize = objectsVertSize + objectsIdxSize + (ulong)materials.Values.Sum(x => x.Parent.MaximumUniformSize + (uint)uniformAlignment);
        
        objectsMemory = Context!.RenderServices.MemoryLessor.LeaseMemory(MemoryGroups.ObjectBufferMemory, bufferSize);
        
        objectsBuffer = new VkBoundBuffer(Context!, 
            new VkBufferContext(bufferSize, BufferUsageFlags.VertexBufferBit | BufferUsageFlags.IndexBufferBit | BufferUsageFlags.UniformBufferBit,
                SharingMode.Exclusive));
        
        objectsBuffer.BindToMemory(objectsMemory);
        objectsBuffer.MapToHost();

        var vertOffset = 0ul;
        var idxOffset = objectsVertSize;
        
        renderedObjects.ForEach(x =>
        {
            var vertSize = (ulong)(sizeof(Vertex) * x.Mesh.Vertices.Length);
            var idxSize = (ulong)(sizeof(uint) * x.Mesh.Indices.Length);
            
            fixed (Vertex* v = x.Mesh.Vertices)
            {
                objectsBuffer.SetMappedMemory(v, vertSize, vertOffset);
            }
            
            fixed (uint* v = x.Mesh.Indices)
            {
                objectsBuffer.SetMappedMemory(v, idxSize, idxOffset);
            }
            
            vertOffset += vertSize;
            idxOffset += idxSize;
        });

        var uOffset = baseUniformOffset;
        
        materials.Values.ForEach(x =>
        {
            var uniformSize = (uint)sizeof(MvpUniformBufferObject) + (uint)x.UniformSize;
            var alignedOffset = (uOffset + uniformAlignment - 1) & ~(uniformAlignment - 1);

            materialStates[x].UniformOffset = alignedOffset;
            
            uOffset = alignedOffset + uniformSize;
        });
        
        objectsBuffer.Unmap();
    }

    private void PopulateDescriptors()
    {
        
    }

    protected override unsafe void RecordCommandsToBuffer(Framebuffer framebuffer, CommandBuffer commandBuffer)
    {
        base.RecordCommandsToBuffer(framebuffer, commandBuffer);
    }

    public override bool IsReadyToRender()
    {
        return base.IsReadyToRender() && isInitialized;
    }

    private void TryDiscardCurrentState()
    {
        if (!isInitialized) return;

        materialStates.Values.ForEach(x => x.Dispose());
        materialStates.Clear();
        materials.Clear();
        
        objectsMemory.Dispose();
        objectsBuffer.Dispose();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            TryDiscardCurrentState();
        }
        
        base.Dispose(disposing);
    }

    protected class MaterialState : IDisposable
    {
        public IVkPipeline Pipeline { get; }
        public ulong UniformOffset { get; set; }

        public MaterialState(IVkMaterialInstance instance, IVkRenderPass renderPass)
        {
            Pipeline = instance.Parent.CreatePipeline(renderPass);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Pipeline.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}