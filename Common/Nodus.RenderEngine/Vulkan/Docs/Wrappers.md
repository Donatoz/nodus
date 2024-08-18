# Vulkan object wrappers architecture considerations

In the provided API, unmanaged Vulkan objects are typically wrapped by managed reference
types that provide explicit discard functionality and lifetime tracking.

### Wrapped objects encapsulation

Each **wrapper** has to expose the wrapped object itself. As long as most of the Vulkan
object are of a value-type - accessing them from an external points would introduce a
minor overhead caused by copying the structure. As long as the wrapped object represents
a single `ulong` handle (in the context of **Silk.NET**), the overhead is insignificant in most cases.

In case of wrapped objects larger than `32` bytes, the wrapper has to provide an interface
to access the wrapped object by reference. Exposing direct pointers to the wrapped objects
is highly discouraged.

### Lifetime

Each **wrapper** has to be bound to a specific Vulkan context object that represents
current Vulkan context state. The wrapper has to be discarded **before** discarding
Vulkan context.

### Dependencies

Each **wrapper** can have any number of dependencies which point to any object that
encapsulates its lifetime state. Any of the dependencies has to be discarded **after**
the dependant object.