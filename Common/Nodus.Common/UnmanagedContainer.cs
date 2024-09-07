using System.Runtime.InteropServices;

namespace Nodus.Common;

public sealed unsafe class UnmanagedContainer<T> : IDisposable where T : unmanaged
{
    public T* Data { get; private set; }
    
    public UnmanagedContainer(T? initialData = null)
    {
        Data = (T*)NativeMemory.Alloc((uint)sizeof(T));

        if (initialData != null)
        {
            *Data = initialData.Value;
        }
    }
    
    public void Dispose()
    {
        NativeMemory.Free(Data);
    }
}