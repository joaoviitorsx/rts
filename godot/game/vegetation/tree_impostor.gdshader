// Distant-tree impostor (atlases baked by scenes/tools/ImpostorBaker): camera-facing quad that cross-fades the
// two nearest of 8 baked yaw views. Lighting uses the baked world normals rotated into the live camera yaw, and
// leaves are recoloured from the live global palette (same gradient as plants.gdshader), so impostors and real
// trees match across the swap distance and follow palette changes without a rebake.
shader_type spatial;
render_mode cull_disabled, diffuse_lambert_wrap, specular_disabled;

#include "res://game/shaders/ground_common.gdshaderinc"

global uniform vec4 veg_tip;
global uniform vec4 veg_shadow;

uniform sampler2D albedo_atlas : source_color, filter_linear_mipmap, repeat_disable;
uniform sampler2D normal_atlas : filter_linear_mipmap, repeat_disable;
uniform float cell = 8.0;       // quad size, mesh-local units
uniform float center_y = 4.0;   // quad centre height, mesh-local units
uniform float lightness = 1.0;
uniform float variation = 0.14;
uniform float alpha_scissor = 0.5;

const float COLS = 4.0;
const float ROWS = 2.0;
const float FRAMES = 8.0;

varying vec2 v_frames;   // the two yaw frames to blend
varying float v_blend;
varying float v_phi;     // camera yaw: rotates baked normals into the world
varying vec3 v_origin;
varying vec4 v_tint;   // per-tree autumn tint (MultiMesh custom data), same as plants.gdshader

void vertex() {
	v_tint = INSTANCE_CUSTOM;
	vec3 center = (MODEL_MATRIX * vec4(0.0, center_y, 0.0, 1.0)).xyz;
	float s = length(MODEL_MATRIX[1].xyz);
	vec2 corner = vec2(UV.x - 0.5, 0.5 - UV.y);
	vec3 world = center + (INV_VIEW_MATRIX[0].xyz * corner.x + INV_VIEW_MATRIX[1].xyz * corner.y) * cell * s;
	VERTEX = (inverse(MODEL_MATRIX) * vec4(world, 1.0)).xyz;
	NORMAL = (inverse(MODEL_MATRIX) * vec4(INV_VIEW_MATRIX[2].xyz, 0.0)).xyz;

	vec3 f = -INV_VIEW_MATRIX[2].xyz;
	float phi = atan(-f.x, -f.z);
	vec3 x = MODEL_MATRIX[0].xyz;
	float psi = atan(-x.z, x.x);
	float k = mod((psi - phi) / (TAU / FRAMES), FRAMES);
	float k0 = floor(k);
	v_frames = vec2(k0, mod(k0 + 1.0, FRAMES));
	v_blend = k - k0;
	v_phi = phi;
	v_origin = MODEL_MATRIX[3].xyz;
}

vec2 frame_uv(float k, vec2 uv) {
	return (vec2(mod(k, COLS), floor(k / COLS)) + uv) / vec2(COLS, ROWS);
}

void fragment() {
	vec2 uv0 = frame_uv(v_frames.x, UV);
	vec2 uv1 = frame_uv(v_frames.y, UV);
	vec4 a = mix(texture(albedo_atlas, uv0), texture(albedo_atlas, uv1), v_blend);
	vec4 n = mix(texture(normal_atlas, uv0), texture(normal_atlas, uv1), v_blend);
	// Param is stored un-premultiplied in a 0-alpha background, so divide by coverage after filtering.
	float param = n.a / max(a.a, 0.01);
	float leaf = smoothstep(0.27, 0.33, param);
	float tone = clamp((param - 0.35) / 0.65, 0.0, 1.0);
	vec3 leaf_c = mix(veg_shadow.rgb, veg_tip.rgb, tone) * lightness;
	leaf_c = mix(leaf_c, v_tint.rgb * (0.55 + 0.6 * tone), v_tint.a);
	vec3 c = mix(a.rgb, leaf_c, leaf);
	c *= 1.0 + (gc_noise(v_origin.xz * 0.37) - 0.5) * 2.0 * variation * leaf;

	vec3 wn = normalize(n.rgb * 2.0 - 1.0);
	float cs = cos(v_phi);
	float sn = sin(v_phi);
	wn = vec3(wn.x * cs + wn.z * sn, wn.y, -wn.x * sn + wn.z * cs);
	NORMAL = normalize((VIEW_MATRIX * vec4(wn, 0.0)).xyz);
	ALBEDO = c;
	ALPHA = a.a;
	ALPHA_SCISSOR_THRESHOLD = alpha_scissor;
	ROUGHNESS = 1.0;
}
