using System.Buffers;
using Nodus.RenderEngine.Vulkan.Memory;

namespace Nodus.RenderEngine.Vulkan.Rendering;

/// <summary>
/// Represents a uniform buffer mutator
/// </summary>
public interface IVkMaterialInstance : IVkUnmanagedHook
{
    IVkMaterial Parent { get; }
    int UniformSize { get; }

    void TryApplyUniforms(IVkBuffer uniformBuffer, ulong offset);
    void UpdateUniformSet<T>(T set) where T : unmanaged;
}

public sealed class VkMaterialInstance : VkObject, IVkMaterialInstance
{
    public IVkMaterial Parent { get; }
    public int UniformSize { get; private set; }

    private MemoryHandle? uniformHandle;
    private object? currentSetObject;
    
    private readonly HashSet<ulong> cleanOffsets;
    
    public VkMaterialInstance(IVkContext vkContext, IVkMaterial parent) : base(vkContext)
    {
        Parent = parent;
        cleanOffsets = [];
    }
    
    public unsafe void TryApplyUniforms(IVkBuffer uniformBuffer, ulong offset)
    {
        if (uniformHandle == null || cleanOffsets.Contains(offset)) return;
        
        uniformBuffer.UpdateData(new Span<byte>(uniformHandle.Value.Pointer, UniformSize / sizeof(byte)), offset);
        cleanOffsets.Add(offset);
    }

    public unsafe void UpdateUniformSet<T>(T set) where T : unmanaged
    {
        uniformHandle?.Dispose();

        var size = sizeof(T);

        if (size > Parent.MaximumUniformSize)
        {
            throw new VulkanRenderingException($"Uniform block ({set}) of size ({size}) is too large. Maximum size allowed: {Parent.MaximumUniformSize}");
        }

        if (currentSetObject != null && currentSetObject.Equals(set))
        {
            return;
        }
        
        UniformSize = size;
        uniformHandle = new Memory<T>([set]).Pin();
        cleanOffsets.Clear();
        currentSetObject = set;
    }

    protected override void Dispose(bool disposing)
    {
        uniformHandle?.Dispose();
        
        base.Dispose(disposing);
    }
}