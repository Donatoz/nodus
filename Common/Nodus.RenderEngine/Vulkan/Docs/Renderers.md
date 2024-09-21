# Renderers overview

A Vulkan renderer defines the core mechanism of assembling render pass command blocks, as well
as the primary descriptors' layout. A renderer comprises and internally operates buffers such as 
uniform, storage, vertex, and index. 

Each Vulkan renderer has to be supplied with a rendering context, which serves as
the main configuration interface.

## Graph-based renderers

A graph-based renderer (`VkGraphRendererBase`) has it's rendering logic split into separate `IVkTask`'s, which execute the
primary rendering logic of the renderer. Some tasks may be executed in parallel if they do
not depend on any other tasks.

### Multi-buffer renderers

A multi-buffer renderer (`VkMultiBufferRenderer`) branches the rendering process into separate render graphs. Since only one
graph can be executed per frame - the renderer switches the active graph each frame.

## Renderer components

A renderer component represents a stateful object that submits commands to the provided command buffer.

The solution provides a set of rendering components that are used to externally submit
Vulkan commands into the provided command buffer. A component can serve as a starting
point for the primary render pass, or succeed the existing one (or even extend it).

When using components as secondary render passes, they have to be compatible with the
primary one.

### Examples

Here are some examples of the default renderer components.

#### ImGui component

An ImGui component typically serves as a post-processing component that draws
immediate-mode GUI on top of the input color attachment. The component should be
placed at the end of the rendering chain to render the UI atop everything else.
