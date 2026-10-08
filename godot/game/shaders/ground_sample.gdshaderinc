// Runtime ground lookup for the terrain and the grass carpet. Not included by the bake shader
// (it renders INTO ground_color_tex, so it must not bind it).
#include "res://game/shaders/ground_common.gdshaderinc"

// Baked by GroundColorBaker.gd: rgb = ground colour, a = grass amount.
global uniform sampler2D ground_color_tex : source_color, filter_linear, repeat_disable;

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
