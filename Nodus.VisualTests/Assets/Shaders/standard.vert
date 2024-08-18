#version 460

layout (location = 0) in vec3 vPos;
layout (location = 1) in vec3 vNormal;
layout (location = 2) in vec2 vTexCoord;

out vec2 texCoord;
out vec4 vertexPosition;
out vec3 vertexNormal;

layout (std140, binding = 0) uniform Matrices {
    mat4 view; // >> 0
    mat4 proj; // >> 64
};

uniform mat4 model;

void main()
{
    vertexPosition = vec4(vPos, 1.0);
    vertexNormal = vNormal;
    texCoord = vTexCoord;

    gl_Position = proj * view * model * vertexPosition;
}
