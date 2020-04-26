#version 420 core
out vec4 fragColor;

in vec2 texCoord;

uniform sampler2D tex;

void main() {
	float v = texture2D(tex, texCoord).r;
	fragColor = vec4(v);
}
