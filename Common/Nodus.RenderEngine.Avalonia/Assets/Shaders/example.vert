#version 310 es

precision mediump float;

in vec3 vPos;
in vec3 vNormal;
in vec2 vTexCoord;

out vec2 texCoord;
out vec3 vertexNormal;
out vec4 vertexPosition;

uniform mat4 transform;

void main()
{
    vertexPosition = vec4(vPos, 1.0);
    texCoord = vTexCoord;
    vertexNormal = vNormal;
    
    gl_Position = transform * vertexPosition;
}