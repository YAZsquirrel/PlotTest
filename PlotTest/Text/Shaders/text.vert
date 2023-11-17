#version 450 core

layout (location = 0) in vec2 aPosition; // same per instance
layout (location = 1) in vec2 uvStep;    // same per instance

layout (location = 3) in vec2 aUV;
layout (location = 4) in mat4 transform;

uniform mat4 projection;

out vec2 uv;

void main()
{
   uv = aUV + uvStep;
   gl_Position = projection * transform * vec4(aPosition, 0, 1.0);
}
