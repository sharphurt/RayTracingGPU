#version 330

uniform sampler2D uImage;
uniform int uImageSamples;

void main()
{
    vec3 color = texture(uImage, gl_TexCoord[0].xy).rgb;
    color /= float(uImageSamples);
    color = color / (color + vec3(1.0));
    color = pow(color, vec3(1.0 / 2.2));
    gl_FragColor = vec4(color, 1.0);
}