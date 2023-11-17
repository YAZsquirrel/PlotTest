#version 450 core

layout (location = 0) in vec2 aPosition;
layout (location = 2) in vec3 color;
layout (location = 3) in mat4 position;

uniform mat4 projection;
uniform mat4 model;
out vec3 ocolor;

void main()
{
   ocolor = color;
   gl_Position = projection * model * position * vec4(aPosition, 0, 1.0);
   //gl_Position =  vec4(aPosition, 0, 1.0);
}
