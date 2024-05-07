#version 300 es

precision mediump float;

in vec3 vPos;
in vec2 vTexCoord;

out vec2 texCoord;
out vec4 vertexPosition;

void main()
{
    vertexPosition = vec4(vPos, 1.0);
    texCoord = vTexCoord;
    
    gl_Position = vertexPosition;
}