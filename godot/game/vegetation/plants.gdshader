// Single vegetation palette for every plant (tufts, bushes, ferns, flowers, tree leaves). The pack's own
// colours are discarded: texture luminance drives a gradient between the global veg_shadow → veg_tip,
// flowers use one of two global flower colours, and low plants fade into the ground colour at their base
// so they sit on the ground.
shader_type spatial;
render_mode cull_disabled, diffuse_lambert_wrap, specular_disabled;

#include "res://game/shaders/ground_common.gdshaderinc"

global uniform vec4 veg_tip;
global uniform vec4 veg_shadow;
global uniform vec4 flower_a;
global uniform vec4 flower_b;
global uniform float wind_strength;
global uniform float wind_speed;

uniform sampler2D albedo_texture : source_color, filter_linear_mipmap, repeat_enable;
uniform float alpha_scissor = 0.5;
uniform bool is_flower = false;
uniform int flower_slot = 0;
uniform float ground_blend_height = 0.35;  // metres; 0 = no blend (tree crowns)
uniform float variation = 0.18;            // per-plant brightness variation
uniform float sway = 0.0;                  // wind on leaves (0 for rigid)
uniform float lightness = 1.0;

varying vec3 v_world;
varying vec3 v_origin;
varying float v_height;

void vertex() {
	vec3 origin = MODEL_MATRIX[3].xyz;
	vec3 world = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	float h = max(world.y - origin.y, 0.0);
	if (sway > 0.0) {
		float t = TIME * wind_speed;
		VERTEX.xz += vec2(sin(origin.x * 0.3 + t), cos(origin.z * 0.3 + t * 0.7)) * wind_strength * sway * h * 0.15;
	}
	v_world = world;
	v_origin = origin;
	v_height = h;
}

void fragment() {
	vec4 t = texture(albedo_texture, UV);
	float l = dot(t.rgb, vec3(0.3, 0.59, 0.11));
	vec3 c;
	if (is_flower) {
		vec3 fc = flower_slot == 0 ? flower_a.rgb : flower_b.rgb;
		c = fc * (0.75 + 0.45 * l);
	} else {
		c = mix(veg_shadow.rgb, veg_tip.rgb, smoothstep(0.08, 0.75, l)) * lightness;
	}
	vec2 o = v_origin.xz + v_world.xz * 0.02;
	c *= 1.0 + (gc_noise(o * 0.37) - 0.5) * 2.0 * variation;
	if (ground_blend_height > 0.0) {
		c = mix(ground_color(v_world.xz), c, smoothstep(0.0, ground_blend_height, v_height));
	}
	ALBEDO = c;
	ALPHA = t.a;
	ALPHA_SCISSOR_THRESHOLD = alpha_scissor;
	ROUGHNESS = 1.0;
}
