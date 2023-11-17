#version 450 core
layout (location = 0) in vec2 aPosition; // same per instance


void main()
{
   gl_Position = vec4(aPosition, 0, 1.0);
}
