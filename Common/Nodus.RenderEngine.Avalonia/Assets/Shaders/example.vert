#version 330 core

layout (location = 0) in vec3 vPos;
layout (location = 1) in vec4 vColor;

out vec4 vertexColor;
out vec4 vertexPosition;

void main()
{
    vertexPosition = vec4(vPos, 1.0);
    vertexColor = vColor;

    gl_Position = vertexPosition;
}