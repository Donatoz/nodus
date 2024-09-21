using System.Runtime.InteropServices;

namespace Nodus.VisualTests.Materials;

[StructLayout(LayoutKind.Sequential)]
public readonly record struct SolidUniformBlock
{
    public required float UvSize { get; init; }
    public required float DistortionAmount { get; init; }
}