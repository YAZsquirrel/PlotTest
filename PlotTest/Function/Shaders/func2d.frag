#version 450 core
layout (location = 0) out vec4 FragColor;

in vec3 ocolor;
//uniform sampler1D color;

void main()
{
	FragColor = vec4(ocolor, 1);//vec4(0.5);// texture(color, val);
}