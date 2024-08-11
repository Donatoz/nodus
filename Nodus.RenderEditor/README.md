# Render Editor

The render graph editor represents an extension of a generic graph editor. A render graph
describes the process of creating the full render context for an arbitrary renderer, including
shaders composition, texture definitions setup, vertex stream descriptions, etc...

### Render context assembly

In order to create a context for a renderer, render graph assemblers are used to analyze
the graph and construct a render context from the graph. Each assembly process has to be
provided with a render master node which serves as a descriptor for a construction process.

### Render nodes

Render node canvas (RenderGraph model) comprises render nodes which describe different
render assembly stages. For example, there are fragment/vertex stage nodes that pack the
calculated values into a meta-object that contains all the needed information to describe
the fragment/vertex stage of the graphical pipeline.

Other nodes, such as constant group nodes, represent a shader logic and are being used
for the shader assembly. Each node of a shader stage provides a shader assembly feature(s) 
that are being transpiled to the actual shader code.


#### Master node
A master node provides a render descriptor for a render context factory. The descriptor contains
the fetched render metadata from the ports withing given graph context.

#### Shader stage node
A shader stage node provides a render metadata that is build using shader assemblers. The
assembler is given a tokenized portion of the graph which is connected to the stage node.