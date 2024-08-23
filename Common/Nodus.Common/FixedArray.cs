using System.Buffers;
using System.Collections;

namespace Nodus.Common;

public interface IFixedEnumerable<T> : IEnumerable<T> where T : unmanaged
{
    uint Length { get; }
    unsafe T* Data { get; }
}

/// <summary>
/// Represents a fixed-size array that can be accessed and modified using unsafe pointers.
/// </summary>
public sealed unsafe class FixedArray<T> : IFixedEnumerable<T>, IDisposable where T : unmanaged
{
    public T this[uint i]
    {
        get => GetValue(i);
        set => Set(i, value);
    }
    
    public uint Length { get; }
    public T* Data => Get(0u);

    private readonly IMemoryOwner<T> owner;
    private MemoryHandle dataHandle;

    public FixedArray(int length)
    {
        if (length <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }
        
        Length = (uint)length;

        owner = MemoryPool<T>.Shared.Rent(length);
        dataHandle = owner.Memory.Pin();
    }

    public void Set(uint index, T item)
    {
        ValidateIndex(index);
        
        *(Data + index) = item;
    }

    public T* Get(uint index)
    {
        ValidateIndex(index);
        
        return (T*)dataHandle.Pointer + index;
    }

    public T GetValue(uint index)
    {
        ValidateIndex(index);
        
        return *(Data + index);
    }

    public void Clear()
    {
        for (var i = 0u; i < Length; i++)
        {
            this[i] = default;
        }
    }

    private void ValidateIndex(uint index)
    {
        if (index >= Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (var i = 0u; i < Length; i++)
        {
            yield return GetValue(i);
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
    
    public void Dispose()
    {
        dataHandle.Dispose();
        owner.Dispose();
    }
}

public static class FixedArrayExtensions
{
    public static FixedArray<T> ToFixedArray<T>(this IEnumerable<T> e) where T : unmanaged
    {
        var original = e as T[] ?? e.ToArray();
        var array = new FixedArray<T>(original.Length);

        for (var i = 0u; i < original.Length; i++)
        {
            array[i] = original[i];
        }

        return array;
    }
}