#version 330

uniform vec2 uViewportSize;
uniform vec3 uDirection;
uniform vec3 uUp;
uniform vec3 uPosition;
uniform float uFOV;

uniform sampler2D uMainTexture;

#define MAX_LENGTH 1000
#define MAX_ITERATION 100

float sphere(vec4 s, vec3 p) {
    return length(p - s.xyz) - s.w;
}

float getDist(vec3 p)
{
    float dist1 = sphere(vec4(0, 1, 0, 1), p);
    return min(0.5, dist1);
//    return min(min(dist1, dist2, 0.5), dist3, 0.5);
}

vec3 getNormal(vec3 p) {
    float d = getDist(p);
    vec2 e = vec2(0.001, 0);
    vec3 n = d - vec3(getDist(p - e.xyy), getDist(p - e.yxy), getDist(p - e.yyx));
    return normalize(n);
}

float raymarchLight(vec3 ro, vec3 rd) {
    float dO = 0;
    float md = 1;
    for (int i = 0; i < 20; i++)
    {
        vec3 p = ro + rd * dO;
        float dS = getDist(p);
        md = min(md, dS);
        dO += dS;
        if (dO > 50 || dS < 0.1)
         break;
    }
    return md;
}


vec4 getLight(vec3 p, vec3 ro, int i, vec3 lightPos) {
    vec3 l = normalize(lightPos - p);
    vec3 n = getNormal(p);
    float dif = clamp(dot(n, l) * 0.5 + 0.5, 0, 1);
    float d = raymarchLight(p + n * 0.1 * 10, l);
    d += 1;
    d = clamp(d, 0, 1);
    dif *= d;
    vec4 col = vec4(dif, dif, dif, 1);
    float occ = (float(i) / MAX_ITERATION * 2);
    occ = 1 - occ;
    occ *= occ;
    col.rgb *= occ;
    float fog = distance(p, ro);
    fog /= MAX_LENGTH;
    fog = clamp(fog, 0, 1);
    fog *= fog;
    col.rgb = col.rgb * (1 - fog) + 0.28 * fog;
    return col;
}

vec4 raymarch(vec3 ro, vec3 rd) {
    vec3 p = ro;
    for (int i = 0; i < MAX_ITERATION; i++) {
        float dist = getDist(p);
        if (dist > MAX_LENGTH)
            return vec4(0);
         
         p += rd * dist; 
         
         if (dist < 0.001)
            return getLight(p, ro, i, vec3(0, 50, 0));
    }
    
    return vec4(0);
}

vec3 getRayDirection(vec2 texcoord, vec2 viewportSize, float fov, vec3 direction, vec3 up)
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

void main() {
	vec2 uv = (gl_TexCoord[0].xy - 0.5) * uViewportSize.x / uViewportSize.y;
    vec3 ro = vec3(uPosition);
//	vec3 rd = normalize(vec3(1.0, uv));
	  //  vec3 ro = uPosition;
    vec3 rd = getRayDirection(uv, uViewportSize, uFOV, uDirection, uUp);
 
    vec4 c = raymarch(ro, rd);
    c = c * c.a + texture(uMainTexture, uv) * (1 - c.a);
    gl_FragColor = c;
}
