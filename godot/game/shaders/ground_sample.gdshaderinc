// Runtime ground lookup for the terrain and the grass carpet. Not included by the bake shader
// (it renders INTO ground_color_tex, so it must not bind it).
#include "res://game/shaders/ground_common.gdshaderinc"

// Baked by GroundColorBaker.gd: rgb = ground colour, a = grass amount.
global uniform sampler2D ground_color_tex : source_color, filter_linear, repeat_disable;

// Watercolor detail (Jonas Voland, CC0): only its LUMINANCE, normalised by the texture mean, modulates the
// palette colour on bare ground (dirt/path). The pack's own colours never reach the screen.
global uniform sampler2D ground_detail_tex : source_color, filter_linear_mipmap, repeat_enable;
global uniform float ground_detail_mean;
global uniform float ground_detail_strength;
global uniform float ground_detail_scale;

float ground_detail(vec2 world, float bare) {
	float l = dot(texture(ground_detail_tex, world * ground_detail_scale).rgb, vec3(0.3, 0.59, 0.11));
	return 1.0 + (l / max(ground_detail_mean, 0.001) - 1.0) * ground_detail_strength * 0.1 * bare;
}

vec2 ground_uv(vec2 world) {
	return (world - ground_mask_rect.xy) / ground_mask_rect.zw;
}

bool ground_inside(vec2 uv) {
	return uv.x >= 0.0 && uv.y >= 0.0 && uv.x <= 1.0 && uv.y <= 1.0;
}

// Fast path used at runtime: baked colour + grass amount; procedural fallback outside the baked area.
vec4 ground_sample(vec2 world) {
	vec2 uv = ground_uv(world);
	if (ground_inside(uv)) {
		return texture(ground_color_tex, uv);
	}
	return vec4(ground_grass_color(world), 1.0);
}
