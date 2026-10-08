// Shared ground colour for Ironvale (cozy look). Used by the terrain shader AND the grass carpet, so a
// grass blade's root is exactly the ground colour under it (invisible transition).
// Palette = global shader uniforms (Project Settings > Shader Globals): one place drives all ground/plants.
//
// ground_mask (RGBA8, world-aligned over ground_mask_rect = x, z, width, depth):
//   R plowed field · G path · B dirt (entrances/yards) · A shade (under trees, along walls, path margins)

global uniform vec4 ground_base;
global uniform vec4 ground_light;
global uniform vec4 ground_dark;
global uniform vec4 ground_dirt;
global uniform vec4 ground_path;
global uniform vec4 ground_plowed;
global uniform float ground_patch_scale;
global uniform float ground_patch_strength;
global uniform float ground_shade_strength;
global uniform vec4 ground_mask_rect;
global uniform sampler2D ground_mask : filter_linear, repeat_disable;

float gc_hash(vec2 p) {
	vec3 p3 = fract(vec3(p.xyx) * 0.1031);
	p3 += dot(p3, p3.yzx + 33.33);
	return fract((p3.x + p3.y) * p3.z);
}

float gc_noise(vec2 p) {
	vec2 i = floor(p);
	vec2 f = fract(p);
	vec2 u = f * f * (3.0 - 2.0 * f);
	return mix(mix(gc_hash(i), gc_hash(i + vec2(1.0, 0.0)), u.x),
			   mix(gc_hash(i + vec2(0.0, 1.0)), gc_hash(i + vec2(1.0, 1.0)), u.x), u.y);
}

float gc_fbm(vec2 p) {
	return 0.5 * gc_noise(p) + 0.3 * gc_noise(p * 2.03 + vec2(17.1, 9.2)) + 0.2 * gc_noise(p * 4.1 + vec2(3.7, 1.9));
}

// Organic, hand-drawn layer borders: displace the mask lookup by ~0.6 m of noise.
vec2 gc_warp(vec2 world) {
	return (vec2(gc_noise(world * 0.45), gc_noise(world * 0.45 + vec2(5.2, 1.3))) - 0.5) * 1.2;
}

vec4 ground_mask_at(vec2 world) {
	vec2 uv = (world + gc_warp(world) - ground_mask_rect.xy) / ground_mask_rect.zw;
	if (uv.x < 0.0 || uv.y < 0.0 || uv.x > 1.0 || uv.y > 1.0) {
		return vec4(0.0);
	}
	return texture(ground_mask, uv);
}

// 0 = no grass (path, dirt, field), 1 = full grass; plus "worn" (0..1) for the trodden strip beside paths.
vec2 ground_grass_amount(vec4 m) {
	float bare = max(max(m.r, m.g), m.b);
	float worn = smoothstep(0.06, 0.4, m.g) * (1.0 - smoothstep(0.4, 0.55, m.g));
	return vec2(1.0 - smoothstep(0.3, 0.5, bare), worn);
}

// Grass colour only (no paths/dirt): large low-frequency light patches + very large darker areas.
vec3 ground_grass_color(vec2 world) {
	float patches = smoothstep(0.3, 0.75, gc_fbm(world * ground_patch_scale));
	vec3 c = mix(ground_base.rgb, ground_light.rgb, patches * ground_patch_strength);
	float big = smoothstep(0.45, 0.85, gc_fbm(world * ground_patch_scale * 0.35 + vec2(41.0, 7.0)));
	return mix(c, ground_dark.rgb, big * 0.3);
}

vec3 ground_color_from_mask(vec2 world, vec4 m) {
	vec3 c = ground_grass_color(world);

	// Worn grass strip beside paths (yellowed, trodden)
	vec2 g = ground_grass_amount(m);
	c = mix(c, mix(c, ground_path.rgb, 0.4), g.y);

	// Dirt (entrances/yards) and plowed field
	c = mix(c, ground_dirt.rgb, smoothstep(0.42, 0.58, m.b));
	c = mix(c, ground_plowed.rgb, smoothstep(0.42, 0.58, m.r));

	// Path: darker earth at the margin, lighter trodden centre
	float path = smoothstep(0.42, 0.58, m.g);
	vec3 path_c = mix(ground_dirt.rgb, ground_path.rgb, smoothstep(0.55, 0.9, m.g));
	c = mix(c, path_c, path);

	// Shade under trees / along walls / at path margins (contact darkening)
	c *= 1.0 - m.a * ground_shade_strength;
	// Painterly tone variation (~1 m), no repeating texture
	c *= 1.0 + (gc_noise(world * 0.8) - 0.5) * 0.08;
	return c;
}

vec3 ground_color(vec2 world) {
	return ground_color_from_mask(world, ground_mask_at(world));
}
