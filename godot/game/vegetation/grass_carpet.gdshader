// Grass carpet blades (MultiMesh). Adapted from "Stylized Cartoon Grass" by dip000
// (https://godotshaders.com/shader/stylized-cartoon-grass/, MIT): root coloured by the terrain colour
// texture, root→tip gradient, wind sway. Ironvale changes: the terrain colour texture is the same baked
// ground colour the terrain shader uses (invisible transition); 1-triangle geometry blades (no textures);
// lit with the ground's up normal; height follows the baked grass amount (zero on paths/dirt/fields);
// blades shrink to nothing with camera distance (LOD).
shader_type spatial;
render_mode cull_disabled, diffuse_lambert_wrap, specular_disabled;

#include "res://game/shaders/ground_sample.gdshaderinc"

global uniform vec4 veg_tip;
global uniform float wind_strength;
global uniform float wind_speed;

uniform float tip_lighten = 0.45;
uniform float root_darken = 0.18;      // blade base slightly darker than the ground (depth, cartoon read)
uniform float clump_variation = 0.12;
uniform float tip_curve = 1.6;
uniform float fade_start = 30.0;
uniform float fade_end = 50.0;

varying vec3 v_color;

void vertex() {
	vec3 root = MODEL_MATRIX[3].xyz;   // instance origin = clump root
	vec4 g = ground_sample(root.xz);   // rgb ground colour, a grass amount
	float dist = length(root - INV_VIEW_MATRIX[3].xyz);
	float h = UV.y;                    // 0 root, 1 tip
	VERTEX.y *= smoothstep(0.3, 0.8, g.a) * (1.0 - smoothstep(fade_start, fade_end, dist));
	float t = TIME * wind_speed;
	VERTEX.xz += vec2(sin(root.x * 0.35 + root.z * 0.2 + t), cos(root.z * 0.3 + t * 0.8)) * wind_strength * h * h;
	float vary = 1.0 + (gc_hash(floor(root.xz * 3.0)) - 0.5) * 2.0 * clump_variation;
	vec3 root_c = g.rgb * (1.0 - root_darken);
	vec3 tip = mix(g.rgb, veg_tip.rgb, tip_lighten) * vary;
	v_color = mix(root_c, tip, pow(h, tip_curve));
}

void fragment() {
	ALBEDO = v_color;
	ROUGHNESS = 1.0;
	NORMAL = normalize((VIEW_MATRIX * vec4(0.0, 1.0, 0.0, 0.0)).xyz);
}
