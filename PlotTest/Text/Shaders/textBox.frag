﻿#version 450 core

layout (location = 0) out vec4 FragColor;
uniform vec4 backColor;

void main()
{
	FragColor = backColor;
}