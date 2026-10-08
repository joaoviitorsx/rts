// Bakes ground_color() (palette + noise + mask) into a texture once, at the mask's resolution.
// rgb = ground colour, a = grass amount (0 on paths/dirt/fields, shorter on the worn strip).
shader_type canvas_item;
render_mode unshaded, blend_disabled;  // write RGBA as-is (alpha = grass amount, not opacity)

#include "res://game/shaders/ground_common.gdshaderinc"

// Global colours arrive linear; the 2D target is sRGB and the 3D side samples it with source_color,
// so encode to sRGB here (otherwise colours get linearized twice: darker and desaturated).
vec3 linear_to_srgb(vec3 c) {
	return mix(12.92 * c, 1.055 * pow(c, vec3(1.0 / 2.4)) - 0.055, step(vec3(0.0031308), c));
}

void fragment() {
	vec2 world = ground_mask_rect.xy + UV * ground_mask_rect.zw;
	vec4 m = ground_mask_at(world);
	vec2 g = ground_grass_amount(m);
	COLOR = vec4(linear_to_srgb(ground_color_from_mask(world, m)), g.x * (1.0 - g.y * 0.6));
}
