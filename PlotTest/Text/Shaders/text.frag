#version 450 core

layout (location = 0) out vec4 FragColor;

uniform vec4 textColor;
uniform sampler2D Texture;

in vec2 uv;
in vec4 aaaa;
void main()
{
	vec4 tex = texture(Texture, uv);
	FragColor = tex * textColor;
}