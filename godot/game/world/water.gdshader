// Stylized water (GDD v0.3 §12): colour by depth (Beer-like falloff from shallow cyan to deep turquoise), foam where the
// surface meets the shore, gentle waves and an animated sparkle. Technique after "Stylized Water for Godot 4.x"
// (godotshaders.com, MIT) — written for Godot 4.7's reverse-Z depth buffer and our cozy palette.
shader_type spatial;
render_mode blend_mix, depth_draw_always, cull_back, specular_schlick_ggx;

uniform vec4 shallow_color : source_color = vec4(0.49, 0.85, 0.83, 0.78);
uniform vec4 deep_color : source_color = vec4(0.10, 0.50, 0.66, 0.95);
uniform vec4 foam_color : source_color = vec4(0.97, 0.98, 0.95, 1.0);
uniform float depth_falloff = 0.35;        // per metre of water seen through
uniform float foam_width = 0.55;           // metres of depth difference that read as foam
uniform float wave_height = 0.06;
uniform float wave_speed = 0.6;
uniform sampler2D DEPTH_TEXTURE : hint_depth_texture, filter_linear_mipmap;
uniform sampler2D SCREEN_TEXTURE : hint_screen_texture, filter_linear_mipmap;

varying vec3 world_pos;

float hash(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }
float noise(vec2 p) {
	vec2 i = floor(p), f = fract(p);
	vec2 u = f * f * (3.0 - 2.0 * f);
	return mix(mix(hash(i), hash(i + vec2(1, 0)), u.x), mix(hash(i + vec2(0, 1)), hash(i + vec2(1, 1)), u.x), u.y);
}

void vertex() {
	world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	float t = TIME * wave_speed;
	VERTEX.y += (sin(world_pos.x * 0.35 + t) + cos(world_pos.z * 0.29 + t * 1.3)) * wave_height;
}

void fragment() {
	// Linear depth of the ground behind the surface (reverse-Z aware via the inverse projection).
	float raw = texture(DEPTH_TEXTURE, SCREEN_UV).r;
	vec4 ndc = vec4(SCREEN_UV * 2.0 - 1.0, raw, 1.0);
	vec4 view = INV_PROJECTION_MATRIX * ndc;
	view.xyz /= view.w;
	float behind = -view.z;
	float surface = -VERTEX.z;
	float water_depth = max(behind - surface, 0.0);

	float k = 1.0 - exp(-water_depth * depth_falloff);
	vec4 col = mix(shallow_color, deep_color, k);

	// Refraction: the bottom shows through, a little wobbly, where the water is shallow.
	vec2 wobble = (vec2(noise(world_pos.xz * 0.8 + TIME * 0.3), noise(world_pos.zx * 0.8 - TIME * 0.25)) - 0.5) * 0.012;
	vec3 bottom = texture(SCREEN_TEXTURE, SCREEN_UV + wobble).rgb;
	col.rgb = mix(bottom * shallow_color.rgb * 1.2, col.rgb, clamp(col.a + k * 0.5, 0.0, 1.0));

	// Shore foam: a soft broken band where the ground comes close to the surface.
	float edge = 1.0 - smoothstep(0.0, foam_width, water_depth);
	float breakup = noise(world_pos.xz * 1.6 + vec2(TIME * 0.4, -TIME * 0.3));
	float foam = smoothstep(0.35, 0.65, edge * (0.6 + breakup * 0.8));
	// Sparkles on open water
	float sparkle = smoothstep(0.82, 0.95, noise(world_pos.xz * 2.4 + TIME * 0.6)) * k * 0.35;

	ALBEDO = mix(col.rgb, foam_color.rgb, foam) + sparkle;
	ALPHA = 1.0;
	ROUGHNESS = 0.18;
	METALLIC = 0.0;
	SPECULAR = 0.45;
}
