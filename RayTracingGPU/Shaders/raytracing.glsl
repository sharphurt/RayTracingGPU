#version 330

uniform vec2 u_resolution;
uniform vec3 u_direction;
uniform vec3 u_up;
uniform vec2 u_mouse;
uniform vec3 u_pos;
uniform sampler2D u_sample;
uniform float u_sample_part;
uniform vec2 u_seed1;
uniform vec2 u_seed2;

uniform sampler2D u_chess_texture;
uniform sampler2D u_marble_texture;

const float MAX_DIST = 99999.0;
const int MAX_REF = 8;
vec3 light = normalize(vec3(-0.5, 0.75, -1.0));

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
	p += u_seed1.x;
	vec3 p3 = fract(vec3(p.xyx) * vec3(.1031, .1030, .0973));
	p3 += dot(p3, p3.yzx+33.33);
	return fract((p3.xx+p3.yz)*p3.zy);
}

float atan2(vec2 dir)
{
    float angle = asin(dir.x) > 0 ? acos(dir.y) : -acos(dir.y);
    return angle;
}

float random()
{
	R_STATE.x = TausStep(R_STATE.x, 13, 19, 12, uint(4294967294));
	R_STATE.y = TausStep(R_STATE.y, 2, 25, 4, uint(4294967288));
	R_STATE.z = TausStep(R_STATE.z, 3, 11, 17, uint(4294967280));
	R_STATE.w = LCGStep(R_STATE.w, uint(1664525), uint(1013904223));
	return 2.3283064365387e-10 * float((R_STATE.x ^ R_STATE.y ^ R_STATE.z ^ R_STATE.w));
}

vec3 randomOnSphere() {
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

mat2 rot(float a) {
	float s = sin(a);
	float c = cos(a);
	return mat2(c, -s, s, c);
}

vec2 sphIntersect(in vec3 ro, in vec3 rd, float ra) {
	float b = dot(ro, rd);
	float c = dot(ro, ro) - ra * ra;
	float h = b * b - c;
	if(h < 0.0) return vec2(-1.0);
	h = sqrt(h);
	return vec2(-b - h, -b + h);
}

vec2 boxIntersection(in vec3 ro, in vec3 rd, in vec3 rad, out vec3 oN)  {
	vec3 m = 1.0 / rd;
	vec3 n = m * ro;
	vec3 k = abs(m) * rad;
	vec3 t1 = -n - k;
	vec3 t2 = -n + k;
	float tN = max(max(t1.x, t1.y), t1.z);
	float tF = min(min(t2.x, t2.y), t2.z);
	
	if(tN > tF || tF < 0.0)
	 return vec2(-1.0);
	 	 
	oN = -sign(rd) * step(t1.yzx, t1.xyz) * step(t1.zxy, t1.xyz);
	return vec2(tN, tF);
}

float plaIntersect(in vec3 ro, in vec3 rd, in vec4 p) {
	return -(dot(ro, p.xyz) + p.w) / dot(rd, p.xyz);
}


vec3 triIntersection(in vec3 ro, in vec3 rd, in vec3 v0, in vec3 v1, in vec3 v2) {
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

vec3 getSky(vec3 rd) {
	vec3 col = vec3(78.0 / 255.0, 141.0 / 255.0, 242.0 / 255.0);
	vec3 sun = vec3(0.95, 0.9, 1.0);
	sun *= max(0.0, pow(dot(rd, light), 256.0));
	col *= max(0.0, dot(light, vec3(0.0, 0.0, -1.0)));
	return clamp(sun + col * 0.01, 0.0, 1.0);
}

vec2 mapTexture(vec3 p, vec3 n, vec3 pos, int mode) {
	vec3 point = (p - pos);
    vec2 result = vec2(0);
    if (mode == 0) { // Plane
        result.x = mod(point.x, 1);
        result.y = mod(point.y, 1);
	}
    else if (mode == 1) { // Cylinder
        point = normalize(point);
        result.x = (1 + point.z) / 2;
        result.y = (1 + atan2(point.xy) / (3.1415926 / 2)) / 2;
	}
    else if (mode == 2) { // Sphere
		float phi = acos( -n.z );
		result.x = 1 - phi / 3.1415926;
		float theta = (acos(n.y / sin( phi ))) / ( 2 * 3.1415926);
		result.y = n.x > 0 ? theta : 1 - theta;
    }
	else if (mode == 3) { // Cube
		vec2 norm;
		if (abs(n.x) == 1.0) norm = point.yz;
		if (abs(n.y) == 1.0) norm = point.xz;
		if (abs(n.z) == 1.0) norm = point.xy;
        result.x = mod(norm.x, 1);
        result.y = mod(norm.y, 1);
	}
    return result;
}

vec3 fromRGB(int r, int g, int b) {
    return vec3(r / 255.0, g / 255.0, b / 255.0);
}

vec4 castRay(inout vec3 ro, inout vec3 rd) {
	vec4 col;
	vec2 minIt = vec2(MAX_DIST);
	vec2 it;
	vec3 n;

	mat2x4 spheres[10];
	spheres[0][0] = vec4(-2, -0.35, -1.48, 0.18);
	spheres[1][0] = vec4(-1.55, -0.3, -1.062, 0.02);
	spheres[2][0] = vec4(-1.45, -0.35, -1.062, 0.02);
	spheres[3][0] = vec4(-1.5, -0.23, -1.062, 0.02);
	spheres[4][0] = vec4(-1.44, -0.2, -1.062, 0.02);
	spheres[5][0] = vec4(-1.56, -0.5, -1.062, 0.02);
	spheres[6][0] = vec4(-1.64, -0.45, -1.062, 0.02);
	spheres[7][0] = vec4(-1.5, -0.39, -1.062, 0.02);
	spheres[8][0] = vec4(-1.67, -0.28, -1.062, 0.02);
	spheres[9][0] = vec4(-1.53, -0.35, -1.062, 0.02);

	
	spheres[0][1] = vec4(fromRGB(232, 16, 48), -0.5);
	spheres[1][1] = vec4(fromRGB(83, 165, 252), -0.8);
	spheres[2][1] = vec4(fromRGB(30, 227, 30), -0.8);
	spheres[3][1] = vec4(fromRGB(19, 29, 138), -0.8);
	spheres[4][1] = vec4(fromRGB(173, 21, 143), -0.8);
	spheres[5][1] = vec4(fromRGB(255, 206, 8), -0.8);
	spheres[6][1] = vec4(fromRGB(255, 37, 8), -0.8);
	spheres[7][1] = vec4(fromRGB(8, 255, 33), -0.8);
	spheres[8][1] = vec4(fromRGB(110, 30, 38), -0.8);
	spheres[9][1] = vec4(fromRGB(83, 165, 252), -0.8);

	
	for(int i = 0; i < spheres.length(); i++) {
		it = sphIntersect(ro - spheres[i][0].xyz, rd, spheres[i][0].w);
		if(it.x > 0.0 && it.x < minIt.x) {
			minIt = it;
			vec3 itPos = ro + rd * it.x;
			n = normalize(itPos - spheres[i][0].xyz);
			col = spheres[i][1];
		}
	}
		
	vec3 boxesPos[9];
	vec3 boxesSize[9];
	vec4 boxesColor[9];
	int useTexture[9];
	
	boxesPos[0] = vec3(-2, -0.35, -3);
	boxesSize[0] = vec3(0.25, 0.25, 0.2);
	boxesColor[0] = vec4(1, 1, 1, -2);

    boxesPos[1] = vec3(-2, 0, -1);
    boxesSize[1] = vec3(1, 1, 0.001);
    boxesColor[1] = vec4(1, 1, 1, 0);
	
	boxesPos[2] = vec3(-2, 0, -3);
	boxesSize[2] = vec3(1, 1, 0.001);
	boxesColor[2] = vec4(1, 1, 1, 0);
	
	boxesPos[3] = vec3(-2.5, 0, -2);
    boxesSize[3] = vec3(0.001, 1, 1);
    boxesColor[3] = vec4(fromRGB(17, 59, 186), 0);	
	
	boxesPos[4] = vec3(-1, 0, -2);
    boxesSize[4] = vec3(0.001, 1, 1);
    boxesColor[4] = vec4(fromRGB(232, 138, 16), 0);	
    
    boxesPos[5] = vec3(-2, -1, -2);
    boxesSize[5] = vec3(1, 0.001, 1);
    boxesColor[5] = vec4(fromRGB(255, 255, 255), 0);	
    
    boxesPos[6] = vec3(-2, 0.7, -2);
    boxesSize[6] = vec3(1, 0.001, 1);
    boxesColor[6] = vec4(fromRGB(255, 255, 255), 0);	
    
    boxesPos[7] = vec3(-1.53, -0.35, -0.96);
    boxesSize[7] = vec3(0.2, 0.2, 0.08);
    boxesColor[7] = vec4(0.8, 0.8, 0.8, 0);
    
    boxesPos[8] = vec3(-2, -0.35, -0.8);
    boxesSize[8] = vec3(0.15, 0.15, 0.5);
    boxesColor[8] = vec4(0.8, 0.8, 0.8, 1);
    
    
	for (int i = 0; i < boxesPos.length(); i++) {      		
        vec3 boxN;
        it = boxIntersection(ro - boxesPos[i], rd, boxesSize[i], boxN);
        if(it.x > 0.0 && it.x < minIt.x) {
            minIt = it;
            n = boxN;
            if (useTexture[i] == 1) {
                vec3 itPos = ro + rd * it.x;
            	vec2 uv = mapTexture(itPos, n, boxesPos[i], 3);
			    col = vec4(texture(u_chess_texture, uv).rgb, boxesColor[i].w);
			} else if (useTexture[i] == 2) {
                vec3 itPos = ro + rd * it.x;
            	vec2 uv = mapTexture(itPos, n, boxesPos[i], 3);
			    col = vec4(texture(u_marble_texture, uv).rgb, boxesColor[i].w);   
            } else {
                 col = boxesColor[i]; 
            }
        }
    }	
	/*
	mat2x4[6] planes;
	planes[0][0] = vec4(0, 0, 1, 1);
	planes[1][0] = vec4(1, 0, 0, 2);
	planes[2][0] = vec4(-1, 0, 0, 0);
    planes[3][0] = vec4(0, 0, 1, 1);
	planes[4][0] = vec4(0, 1, 0, 0.5);
	planes[5][0] = vec4(0, -1, 0, 0.5);
	
	// 119, 15, 148 фиолетовый
	// 19, 60, 173 синий
	planes[0][1] = vec4(1, 1, 1, 0);
		
	planes[1][1] = vec4(1, 1, 1, 0);
	planes[2][1] = vec4(1, 1, 1, 0);
	
//	planes[1][1] = vec4(fromRGB(230, 10, 10), 1);
//	planes[2][1] = vec4(fromRGB(10, 10, 230), 1);
	planes[3][1] = vec4(1, 1, 1, 0);
	planes[4][1] = vec4(1, 0, 0, 0);
	planes[5][1] = vec4(0, 1, 0, 0);
	
	for (int i = 0; i < planes.length(); i++){
        vec4 planeNormal = planes[i][0];
        it = vec2(plaIntersect(ro, rd, planeNormal));
        if(it.x > 0.0 && it.x < minIt.x) {
            minIt = it;
            n = planeNormal.xyz;
            col = planes[i][1];
        }
	}*/
	if(minIt.x == MAX_DIST)
	 return vec4(getSky(rd), -2.0);
	 
	if(col.a == -2.0)
	 return col;
	 
	vec3 reflected = reflect(rd, n);
	if(col.a < 0.0) {
		float fresnel = 1.0 - abs(dot(-rd, n));
		if(random() - 0.1 < fresnel * fresnel) {
			rd = reflected;
			return col;
		}
		ro += rd * (minIt.y + 0.001);
		rd = refract(rd, n, 1.0 / (1.0 - col.a));
		return col;
	}
	vec3 itPos = ro + rd * it.x;
	vec3 r = randomOnSphere();
	vec3 diffuse = normalize(r * dot(r, n));
	ro += rd * (minIt.x - 0.001);
	rd = mix(diffuse, reflected, col.a);
	return col;
}

vec3 traceRay(vec3 ro, vec3 rd) {
	vec3 col = vec3(1.0);
	for(int i = 0; i < MAX_REF; i++)
	{
		vec4 refCol = castRay(ro, rd);
		col *= refCol.rgb;
		if(refCol.a == -2.0) return col;
	}
	return vec3(0.0);
}

void main() {
	vec2 uv = (gl_TexCoord[0].xy - 0.5) * u_resolution / u_resolution.y;
	vec2 uvRes = hash22(uv + 1.0) * u_resolution + u_resolution;
	R_STATE.x = uint(u_seed1.x + uvRes.x);
	R_STATE.y = uint(u_seed1.y + uvRes.x);
	R_STATE.z = uint(u_seed2.x + uvRes.y);
	R_STATE.w = uint(u_seed2.y + uvRes.y);
	vec3 rayOrigin = u_pos;
	vec3 rayDirection = normalize(vec3(1.0, uv));
	rayDirection.zx *= rot(-u_mouse.y);
	rayDirection.xy *= rot(u_mouse.x);
	vec3 col = vec3(0.0);
	int samples = 8;
	for(int i = 0; i < samples; i++) {
		col += traceRay(rayOrigin, rayDirection);
	}
	col /= samples;
	float white = 20.0;
	col *= white * 16.0;
	col = (col * (1.0 + col / white / white)) / (1.0 + col);
	vec3 sampleCol = texture(u_sample, gl_TexCoord[0].xy).rgb;
	col = mix(sampleCol, col, u_sample_part);
	gl_FragColor = vec4(col, 1.0);
}