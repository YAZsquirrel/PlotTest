#version 450 core
layout (location = 0) in vec2 aPosition;

out vec3 fColor;

void main()
{
   gl_Position = vec4(aPosition, 0, 1.0);
}
