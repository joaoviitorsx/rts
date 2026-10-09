// Cliff walls of the terraces (GDD v0.3 §12, docs/research/terreno_penhascos_referencia.md): pale limestone,
// triplanar (no UVs, no stretching on vertical faces), faceted shading from flat normals, darker in the cracks, a grassy
// lip blended in at the top so wall and ground meet without a visible seam.
shader_type spatial;
render_mode cull_disabled;

global uniform vec4 veg_shadow;
global uniform vec4 ground_base;
uniform vec4 rock_light : source_color = vec4(0.87, 0.85, 0.79, 1.0);
uniform vec4 rock_dark : source_color = vec4(0.62, 0.60, 0.55, 1.0);
uniform float lip = 0.35;   // metres of grass blended over the top edge

varying vec3 world_pos;
varying float top_y;

float hash(vec3 p) { return fract(sin(dot(p, vec3(127.1, 311.7, 74.7))) * 43758.5453); }
float noise3(vec3 p) {
	vec3 i = floor(p), f = fract(p);
	vec3 u = f * f * (3.0 - 2.0 * f);
	float n000 = hash(i), n100 = hash(i + vec3(1, 0, 0)), n010 = hash(i + vec3(0, 1, 0)), n110 = hash(i + vec3(1, 1, 0));
	float n001 = hash(i + vec3(0, 0, 1)), n101 = hash(i + vec3(1, 0, 1)), n011 = hash(i + vec3(0, 1, 1)), n111 = hash(i + vec3(1, 1, 1));
	return mix(mix(mix(n000, n100, u.x), mix(n010, n110, u.x), u.y), mix(mix(n001, n101, u.x), mix(n011, n111, u.x), u.y), u.z);
}

void vertex() {
	world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	top_y = COLOR.r * 20.0;   // mesh stores the top height of its wall (÷20) in the vertex colour
}

void fragment() {
	vec3 n = normalize(cross(dFdy(world_pos), dFdx(world_pos)));   // flat facets
	float strata = noise3(vec3(world_pos.x * 0.6, world_pos.y * 2.2, world_pos.z * 0.6));
	float speck = noise3(world_pos * 3.1);
	vec3 rock = mix(rock_dark.rgb, rock_light.rgb, smoothstep(0.25, 0.75, strata * 0.7 + speck * 0.3));
	rock *= 0.85 + 0.15 * clamp(n.y + 0.5, 0.0, 1.0);
	float grass = smoothstep(top_y - lip, top_y - 0.05, world_pos.y) * step(0.2, n.y + 0.6);
	ALBEDO = mix(rock, ground_base.rgb, grass);
	ROUGHNESS = 0.95;
	NORMAL = (VIEW_MATRIX * vec4(n, 0.0)).xyz;
}
