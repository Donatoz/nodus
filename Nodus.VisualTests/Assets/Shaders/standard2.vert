#version 460

layout (location = 0) in vec3 vPosition;
layout (location = 1) in vec3 vNormal;
layout (location = 2) in vec2 vTexCoord;

layout (location = 0) out vec3 vertexColor;
layout (location = 1) out vec3 vertexNormal;
layout (location = 2) out vec2 texCoord;

layout (binding = 0) uniform UniformBufferObject {
    mat4 model;
    mat4 view;
    mat4 proj;
} ubo;

void main() {
    gl_Position = ubo.proj * ubo.view * ubo.model * vec4(vPosition, 1.0);
    vertexColor = vec3(1.0, 1.0, 1.0);

    vertexNormal = vNormal;
    texCoord = vTexCoord;
}