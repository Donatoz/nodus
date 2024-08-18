#version 460

layout (location = 0) in vec3 vertexColor;
layout (location = 2) in vec2 texCoord;

layout (location = 0) out vec4 outColor;

void main() {
    outColor = vec4(texCoord, 0.0, 1.0);
}
