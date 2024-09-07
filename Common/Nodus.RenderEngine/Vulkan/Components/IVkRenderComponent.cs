using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan.Components;

public interface IVkRenderComponent
{
    /// <summary>
    /// The priority of the component that affects its place in a renderer queue.
    /// </summary>
    uint Priority { get; }
    
    /// <summary>
    /// <para>
    /// Does the component require a separate place in a renderer's submission context?
    /// That is, does the component have its own render pass?
    /// </para>
    /// <para>
    /// If true - the component render commands will be submitted after the primary render passes.
    /// The component has to begin and end its own render pass.
    /// </para>
    /// <para>
    /// If false - the component render commands will be submitted inside the primary render passes.
    /// The component mustn't begin nor end any render passes, while it has to have its pipeline compatible with
    /// the primary render pass of the renderer.
    /// </para>
    /// <remarks>
    /// Note that (<see cref="Priority"/> affects the order amongst only those components that have similar requirement.
    /// </remarks>
    /// </summary>
    bool SubmitSeparately { get; }
    
    void SubmitCommands(CommandBuffer buffer, Framebuffer framebuffer, int frameIndex);
}