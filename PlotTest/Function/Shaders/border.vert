#version 450 core

layout (location = 0) in vec2 aPosition;
layout (location = 3) in mat4 position;

out vec4 color;

uniform mat4 projection;
uniform mat4 model;
uniform vec4 aColor;

void main()
{
   color = aColor;
   gl_Position = projection * model * position * vec4(aPosition, 0, 1.0);
}
