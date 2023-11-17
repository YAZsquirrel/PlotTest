#version 450 core
layout (location = 0) in vec2 aPosition; // same per instance
uniform mat4 projection;

void main()
{
   gl_Position = projection * vec4(aPosition, 0, 1.0);
}
