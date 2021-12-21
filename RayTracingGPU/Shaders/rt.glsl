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

struct Triangle
{
    Material material;
    vec3 A;
    vec3 B;
    vec3 C;
};


uniform vec2 uViewportSize;
uniform vec3 uPosition;
uniform vec3 uDirection;
uniform vec3 uUp;
uniform float uFOV;
uniform float uTime;
uniform int uSamples;
uniform vec2 uSeed1;
uniform vec2 uSeed2;

uniform sampler2D uSample;
uniform float uSamplePart;

#define PI 3.1415926535
#define HALF_PI (PI / 2.0)
#define FAR_DISTANCE 1000000.0

#define MAX_DEPTH 8
#define SPHERE_COUNT 3
#define BOX_COUNT 9
#define N_IN 0.99
#define N_OUT 1.0

vec3 light = normalize(vec3(-0.5, 0.75, -1.0));

Sphere spheres[SPHERE_COUNT];
Box boxes[BOX_COUNT];

void InitializeScene()
{
    spheres[0].position = vec3(2.5, 1.5, -1.5);
    spheres[1].position = vec3(-2.5, 2.5, -1.0);
    spheres[2].position = vec3(0.5, -4.0, 3.0);
    spheres[0].radius = 1.5;
    spheres[1].radius = 1.0;
    spheres[2].radius = 1.0;
    spheres[0].material.roughness = 1.0;
    spheres[1].material.roughness = 0.8;
    spheres[2].material.roughness = 1.0;
    spheres[0].material.opacity = 0.0;
    spheres[1].material.opacity = 0.0;
    spheres[2].material.opacity = 0.8;
    spheres[0].material.reflectance = vec3(1.0, 0.0, 0.0);
    spheres[1].material.reflectance = vec3(1.0, 0.4, 0.0);
    spheres[2].material.reflectance = vec3(1.0, 1.0, 1.0);
    spheres[0].material.emmitance = vec3(0.0);
    spheres[1].material.emmitance = vec3(0.0);
    spheres[2].material.emmitance = vec3(0.0);

    // up
    boxes[0].material.roughness = 0.0;
    boxes[0].material.emmitance = vec3(0.0);
    boxes[0].material.reflectance = vec3(1.0, 1.0, 1.0);
    boxes[0].halfSize = vec3(5.0, 0.5, 5.0);
    boxes[0].position = vec3(0.0, 5.5, 0.0);
    boxes[0].rotation = mat3(
        1.0, 0.0, 0.0,
        0.0, 1.0, 0.0,
        0.0, 0.0, 1.0
    );

    // down
    boxes[1].material.roughness = 0.3;
    boxes[1].material.opacity = 0.0;
    boxes[1].material.emmitance = vec3(0.0);
    boxes[1].material.reflectance = vec3(1.0, 1.0, 1.0);
    boxes[1].halfSize = vec3(5.0, 0.5, 5.0);
    boxes[1].position = vec3(0.0, -5.5, 0.0);
    boxes[1].rotation = mat3(
        1.0, 0.0, 0.0,
        0.0, 1.0, 0.0,
        0.0, 0.0, 1.0
    );

    // right
    boxes[2].material.roughness = 0.0;
    boxes[2].material.opacity = 0.0;
    boxes[2].material.emmitance = vec3(0.0);
    boxes[2].material.reflectance = vec3(0.0, 1.0, 0.0);
    boxes[2].halfSize = vec3(5.0, 0.5, 5.0);
    boxes[2].position = vec3(5.5, 0.0, 0.0);
    boxes[2].rotation = mat3(
        0.0, 1.0, 0.0,
        -1.0, 0.0, 0.0,
        0.0, 0.0, 1.0
    );

    // left
    boxes[3].material.roughness = 0.0;
    boxes[3].material.opacity = 0.0;
    boxes[3].material.emmitance = vec3(0.0);
    boxes[3].material.reflectance = vec3(1.0, 0.0, 0.0);
    boxes[3].halfSize = vec3(5.0, 0.5, 5.0);
    boxes[3].position = vec3(-5.5, 0.0, 0.0);
    boxes[3].rotation = mat3(
        0.0, 1.0, 0.0,
        -1.0, 0.0, 0.0,
        0.0, 0.0, 1.0
    );

    // back
    boxes[4].material.roughness = 0.0;
    boxes[4].material.opacity = 0.0;
    boxes[4].material.emmitance = vec3(0.0);
    boxes[4].material.reflectance = vec3(1.0, 1.0, 1.0);
    boxes[4].halfSize = vec3(5.0, 0.5, 5.0);
    boxes[4].position = vec3(0.0, 0.0, -5.5);
    boxes[4].rotation = mat3(
        1.0, 0.0, 0.0,
        0.0, 0.0, 1.0,
        0.0, 1.0, 0.0
    );
    
        // front
    boxes[8].material.roughness = 0.0;
    boxes[8].material.opacity = 0.0;
    boxes[8].material.emmitance = vec3(0.0);
    boxes[8].material.reflectance = vec3(1.0, 1.0, 1.0);
    boxes[8].halfSize = vec3(5.0, 0.5, 5.0);
    boxes[8].position = vec3(0.0, 0.0, -5.5);
    boxes[8].rotation = mat3(
        1.0, 0.0, 0.0,
        0.0, 0.0, 1.0,
        0.0, 1.0, 0.0
    );

    // light source
    boxes[5].material.roughness = 0.0;
    boxes[5].material.opacity = 0.0;
    boxes[5].material.emmitance = vec3(6.0);
    boxes[5].material.reflectance = vec3(1.0);
    boxes[5].halfSize = vec3(2.5, 0.2, 2.5);
    boxes[5].position = vec3(0.0, 4.8, 0.0);
    boxes[5].rotation = mat3(
        1.0, 0.0, 0.0,
        0.0, 1.0, 0.0,
        0.0, 0.0, 1.0
    );

    // boxes
    boxes[6].material.roughness = 0.0;
    boxes[6].material.opacity = 0.0;
    boxes[6].material.emmitance = vec3(0.0);
    boxes[6].material.reflectance = vec3(1.0);
    boxes[6].halfSize = vec3(1.5, 3.0, 1.5);
    boxes[6].position = vec3(-2.0, -2.0, -0.0);
    boxes[6].rotation = mat3(
        0.7, 0.0, 0.7,
        0.0, 1.0, 0.0,
        -0.7, 0.0, 0.7
    );
    // boxes
    boxes[7].material.roughness = 0.0;
    boxes[7].material.opacity = 0.0;
    boxes[7].material.emmitance = vec3(0.0);
    boxes[7].material.reflectance = vec3(1.0);
    boxes[7].halfSize = vec3(1.0, 1.5, 1.0);
    boxes[7].position = vec3(2.5, -3.5, -0.0);
    boxes[7].rotation = mat3(
        0.7, 0.0, 0.7,
        0.0, 1.0, 0.0,
        -0.7, 0.0, 0.7
    );
}


uvec4 R_STATE;

uint TausStep(uint z, int S1, int S2, int S3, uint M)
{
	uint b = (((z << S1) ^ z) >> S2);
	return (((z & M) << S3) ^ b);	
}

uint LCGStep(uint z, uint A, uint C)
{
	return (A * z + C);	
}

vec2 hash22(vec2 p)
{
	p += uSeed1.x;
	vec3 p3 = fract(vec3(p.xyx) * vec3(.1031, .1030, .0973));
	p3 += dot(p3, p3.yzx + 33.33);
	return fract((p3.xx + p3.yz) * p3.zy);
}

float random()
{
	R_STATE.x = TausStep(R_STATE.x, 13, 19, 12, uint(4294967294));
	R_STATE.y = TausStep(R_STATE.y, 2, 25, 4, uint(4294967288));
	R_STATE.z = TausStep(R_STATE.z, 3, 11, 17, uint(4294967280));
	R_STATE.w = LCGStep(R_STATE.w, uint(1664525), uint(1013904223));
	return 2.3283064365387e-10 * float((R_STATE.x ^ R_STATE.y ^ R_STATE.z ^ R_STATE.w));
}

vec3 RandomHemispherePoint()
{
	vec3 rand = vec3(random(), random(), random());
	float theta = rand.x * 2.0 * 3.14159265;
	float v = rand.y;
	float phi = acos(2.0 * v - 1.0);
	float r = pow(rand.z, 1.0 / 3.0);
	float x = r * sin(phi) * cos(theta);
	float y = r * sin(phi) * sin(theta);
	float z = r * cos(phi);
	return vec3(x, y, z);
}

vec3 NormalOrientedHemispherePoint(vec3 n)
{
    vec3 v = RandomHemispherePoint();
    return dot(v, n) < 0.0 ? -v : v;
}

float FresnelSchlick(float nIn, float nOut, vec3 direction, vec3 normal)
{
    float R0 = ((nOut - nIn) * (nOut - nIn)) / ((nOut + nIn) * (nOut + nIn));
    float fresnel = R0 + (1.0 - R0) * pow((1.0 - abs(dot(direction, normal))), 5.0);
    return fresnel;
}

vec3 IdealRefract(vec3 direction, vec3 normal, float nIn, float nOut)
{
    bool fromOutside = dot(normal, direction) < 0.0;
    float ratio = fromOutside ? nOut / nIn : nIn / nOut;

    vec3 refraction, reflection;

    refraction = fromOutside ? refract(direction, normal, ratio) : -refract(-direction, normal, ratio);
    reflection = reflect(direction, normal);

    return refraction == vec3(0.0) ? reflection : refraction;
}

vec3 GetRayDirection(vec2 texcoord, vec2 viewportSize, float fov, vec3 direction, vec3 up)
{
    vec2 texDiff = 0.5 * vec2(1.0 - 2.0 * texcoord.x, 2.0 * texcoord.y - 1.0);
    vec2 angleDiff = texDiff * vec2(viewportSize.x / viewportSize.y, 1.0) * tan(fov * 0.5);

    vec3 rayDirection = normalize(vec3(angleDiff, 1.0f));

    vec3 right = normalize(cross(up, direction));
    mat3 viewToWorld = mat3(
        right,
        up,
        direction
    );

    return viewToWorld * rayDirection;
}

bool IntersectRaySphere(vec3 origin, vec3 direction, Sphere sphere, out float fraction, out vec3 normal)
{
    vec3 L = origin - sphere.position;
    float a = dot(direction, direction);
    float b = 2.0 * dot(L, direction);
    float c = dot(L, L) - sphere.radius * sphere.radius;
    float D = b * b - 4 * a * c;

    if (D < 0.0) return false;

    float r1 = (-b - sqrt(D)) / (2.0 * a);
    float r2 = (-b + sqrt(D)) / (2.0 * a);
        
    if (r1 > 0.0)
        fraction = r1;
    else if (r2 > 0.0)
        fraction = r2;
    else
        return false;

    normal = normalize(direction * fraction + L);

    return true;
}

bool IntersectRayBox(vec3 origin, vec3 direction, Box box, out float fraction, out vec3 normal)
{
    vec3 rd = box.rotation * direction;
    vec3 ro = box.rotation * (origin - box.position);

    vec3 m = vec3(1.0) / rd; 

    vec3 s = vec3((rd.x < 0.0) ? 1.0 : -1.0,
                  (rd.y < 0.0) ? 1.0 : -1.0,
                  (rd.z < 0.0) ? 1.0 : -1.0);
    vec3 t1 = m * (-ro + s * box.halfSize);
    vec3 t2 = m * (-ro - s * box.halfSize);

    float tN = max(max(t1.x, t1.y), t1.z);
    float tF = min(min(t2.x, t2.y), t2.z);

    if (tN > tF || tF < 0.0) return false;

    mat3 txi = transpose(box.rotation);

    if (t1.x > t1.y && t1.x > t1.z)
        normal = txi[0] * s.x;
    else if (t1.y > t1.z)
        normal = txi[1] * s.y;
    else
        normal = txi[2] * s.z;

    fraction = tN;

    return true;
}

vec3 IntersectTraingle(in vec3 ro, in vec3 rd, in vec3 v0, in vec3 v1, in vec3 v2) {
	vec3 v1v0 = v1 - v0;
	vec3 v2v0 = v2 - v0;
	vec3 rov0 = ro - v0;
	vec3 n = cross(v1v0, v2v0);
	vec3 q = cross(rov0, rd);
	float d = 1.0 / dot(rd, n);
	float u = d * dot(-q, v2v0);
	float v = d * dot(q, v1v0);
	float t = d * dot(-n, rov0);
	
	if (u < 0.0 || u > 1.0 || v < 0.0 || (u + v) > 1.0) {
		t = -1.0;
	}
	return vec3(t, u, v);
}

bool CastRay(vec3 rayOrigin, vec3 rayDirection, out float fraction, out vec3 normal, out Material material)
{
    float minDistance = FAR_DISTANCE;

    for (int i = 0; i < SPHERE_COUNT; i++)
    {
        float F;
        vec3 N;
        if (IntersectRaySphere(rayOrigin, rayDirection, spheres[i], F, N) && F < minDistance)
        {
            minDistance = F;
            normal = N;
            material = spheres[i].material;
        }
    }

    for (int i = 0; i < BOX_COUNT; i++)
    {
        float F;
        vec3 N;
        if (IntersectRayBox(rayOrigin, rayDirection, boxes[i], F, N) && F < minDistance)
        {
            minDistance = F;
            normal = N;
            material = boxes[i].material;
        }
    }

    fraction = minDistance;
    return minDistance != FAR_DISTANCE;
}


vec3 getSky(vec3 rd) {
	vec3 col = vec3(78.0 / 255.0, 141.0 / 255.0, 242.0 / 255.0);
	vec3 sun = vec3(0.95, 0.9, 1.0);
	sun *= max(0.0, pow(dot(rd, light), 256.0));
	col *= max(0.0, dot(light, vec3(0.0, 0.0, -1.0)));
	return clamp(sun + col * 0.1, 0, 1.0);
}

bool IsRefracted(float rand, vec3 direction, vec3 normal, float opacity, float nIn, float nOut)
{
    float fresnel = FresnelSchlick(nIn, nOut, direction, normal);
    return opacity > rand && fresnel < rand;
}

vec3 TracePath(vec3 rayOrigin, vec3 rayDirection, float seed)
{
    vec3 L = vec3(0.0);
    vec3 F = vec3(1.0);
    for (int i = 0; i < MAX_DEPTH; i++)
    {
        float fraction;
        vec3 normal;
        Material material;
        bool hit = CastRay(rayOrigin, rayDirection, fraction, normal, material);
        if (hit)
        {
            vec3 newRayOrigin = rayOrigin + fraction * rayDirection;

            vec3 hemisphereDistributedDirection = NormalOrientedHemispherePoint(normal);

            vec3 randomVec = vec3(
                random(), random(), random()
            );
            randomVec = normalize(2.0 * randomVec - 1.0);

            vec3 tangent = cross(randomVec, normal);
            vec3 bitangent = cross(normal, tangent);
            mat3 transform = mat3(tangent, bitangent, normal);

            vec3 newRayDirection = transform * hemisphereDistributedDirection;
            
            float refractRand = random();
            bool refracted = IsRefracted(refractRand, rayDirection, normal, material.opacity, N_IN, N_OUT);
            if (refracted)
            {
                vec3 idealRefraction = IdealRefract(rayDirection, normal, N_IN, N_OUT);
                newRayDirection = normalize(mix(-newRayDirection, idealRefraction, material.roughness));
                newRayOrigin += normal * (dot(newRayDirection, normal) < 0.0 ? -0.8 : 0.8);
            }
            else
            {
                vec3 idealReflection = reflect(rayDirection, normal);
                newRayDirection = normalize(mix(newRayDirection, idealReflection, material.roughness));
                newRayOrigin += normal * 0.8;
            }

            rayDirection = newRayDirection;
            rayOrigin = newRayOrigin;

            L += F * material.emmitance;
            F *= material.reflectance;
        }
        else
        {
            L += getSky(normal) * 0.3;
        }
    }

    return L;
}

void main()
{
	vec2 uv = (gl_TexCoord[0].xy - 0.5) * uViewportSize / uViewportSize.y;
	vec2 uvRes = hash22(uv + 1.0) * uViewportSize + uViewportSize;
	R_STATE.x = uint(uSeed1.x + uvRes.x);
	R_STATE.y = uint(uSeed1.y + uvRes.x);
	R_STATE.z = uint(uSeed2.x + uvRes.y);
	R_STATE.w = uint(uSeed2.y + uvRes.y);

    InitializeScene();

    vec3 direction = GetRayDirection(gl_TexCoord[0].xy, uViewportSize, uFOV, uDirection, uUp);

    vec3 col = vec3(0.0);
    for (int i = 0; i < uSamples; i++)
    {
        vec3 sampleColor = TracePath(uPosition, direction, random());
        col += sampleColor;
    }
    
    col /= float(uSamples);
    /*
    float white = 10.0;
	col *= white * 16.0;
	col = (col * (1.0 + col / white / white)) / (1.0 + col);*/
	vec3 sampleCol = texture(uSample, gl_TexCoord[0].xy).rgb;
	col = mix(sampleCol, col, uSamplePart);

	gl_FragColor = vec4(col, 1.0);
}