using System.Runtime.InteropServices;

namespace Nodus.Common;

public sealed unsafe class UnmanagedContainer<T> : IDisposable where T : unmanaged
{
    public T* Data { get; }
    
    public UnmanagedContainer()
    {
        Data = (T*)NativeMemory.Alloc((uint)sizeof(T));
    }
    
    public void Dispose()
    {
        NativeMemory.Free(Data);
    }
}