// Grass carpet blades (MultiMesh). Adapted from "Stylized Cartoon Grass" by dip000
// (https://godotshaders.com/shader/stylized-cartoon-grass/, MIT): root coloured by the terrain, root→tip
// gradient, wind sway. Ironvale changes: the root colour is computed with the SAME function as the ground
// shader (ground_common.gdshaderinc) so the transition is invisible; geometry blades (no textures);
// blades are lit with the ground's up normal; height follows the ground mask (zero on paths/dirt/fields,
// shorter on the worn strip); blades shrink to nothing with camera distance (cheap LOD).
shader_type spatial;
render_mode cull_disabled, diffuse_lambert_wrap, specular_disabled;

#include "res://game/shaders/ground_common.gdshaderinc"

global uniform vec4 veg_tip;
global uniform float wind_strength;
global uniform float wind_speed;

uniform float tip_lighten = 0.32;
uniform float tip_curve = 1.6;
uniform float fade_start = 32.0;
uniform float fade_end = 55.0;

varying vec3 v_color;

void vertex() {
	vec3 root = MODEL_MATRIX[3].xyz;   // MultiMesh instance origin = blade clump root
	vec4 m = ground_mask_at(root.xz);
	vec2 grass = ground_grass_amount(m);
	float dist = length(root - INV_VIEW_MATRIX[3].xyz);
	float fade = 1.0 - smoothstep(fade_start, fade_end, dist);
	float h = UV.y;                    // 0 at the root, 1 at the tip (set by GrassCarpet.gd)

	VERTEX.y *= grass.x * (1.0 - grass.y * 0.6) * fade;
	float t = TIME * wind_speed;
	vec2 sway = vec2(sin(root.x * 0.35 + root.z * 0.2 + t), cos(root.z * 0.3 + t * 0.8)) * wind_strength;
	VERTEX.xz += sway * h * h;

	vec3 base = ground_color_from_mask(root.xz, m);
	// The tip is always grass-coloured (never path/dirt), shaded like the ground under it.
	vec3 grass_c = ground_grass_color(root.xz) * (1.0 - m.a * ground_shade_strength);
	vec3 tip = mix(grass_c, veg_tip.rgb, tip_lighten) * (1.0 + (gc_noise(root.xz * 0.6) - 0.5) * 0.08);
	v_color = mix(base, tip, pow(h, tip_curve));
}

void fragment() {
	ALBEDO = v_color;
	ROUGHNESS = 1.0;
	NORMAL = normalize((VIEW_MATRIX * vec4(0.0, 1.0, 0.0, 0.0)).xyz);   // lit like the ground below
}
