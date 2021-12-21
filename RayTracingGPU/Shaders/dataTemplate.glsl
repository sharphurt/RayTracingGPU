#version 330

struct Material
{
    vec3 emmitance;
    vec3 reflectance;
    float roughness;
    float opacity;
};

struct Box
{
    Material material;
    vec3 halfSize;
    mat3 rotation;
    vec3 position;
};

struct Sphere
{
    Material material;
    vec3 position;
    float radius;
};

#define BOX_COUNT %box_count%

Box boxes[BOX_COUNT];