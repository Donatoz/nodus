namespace Nodus.RenderEngine.Vulkan.Memory;

/// <summary>
/// Represents a contiguous memory region in an allocated Vulkan memory.
/// </summary>
public readonly struct VkMemoryRegion(ulong offset, ulong size) : IEquatable<VkMemoryRegion>
{
    public ulong Offset { get; } = offset;
    public ulong Size { get; } = size;
    public ulong End => Offset + Size - 1;

    public bool Equals(VkMemoryRegion other)
    {
        return Offset == other.Offset;
    }

    public override bool Equals(object? obj)
    {
        return obj is VkMemoryRegion other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Offset.GetHashCode();
    }

    public static bool operator ==(VkMemoryRegion left, VkMemoryRegion right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(VkMemoryRegion left, VkMemoryRegion right)
    {
        return !(left == right);
    }

    public bool Overlaps(VkMemoryRegion other)
    {
        return Offset >= other.Offset && Offset <= other.End 
               || End >= other.Offset && End <= other.End;
    }

    public override string ToString()
    {
        return $"[Offset={Offset}; End={End}; Size={Size}]";
    }
}