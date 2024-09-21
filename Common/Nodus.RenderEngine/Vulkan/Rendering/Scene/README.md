# Scene renderer

### Legend

 - **Rendered Object** - an object with a defined transformation, geometry and material.
 - **Material** - an object that describes a shader set and the description of resources that shaders require.
 - **Scene** - a collection of rendered objects and global rendering parameters.

### Strategy

Before rendering a scene, the renderer has to be initialized with the primary render-loop components:
 - Command buffers
 - Vertex/Index/Uniform buffers (usually composed into one large buffer)
 - Synchronisation primitives