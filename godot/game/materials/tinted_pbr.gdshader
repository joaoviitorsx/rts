// Cozy re-grade of PBR trim-sheet materials (Medieval Village, Stylized Nature rocks): desaturate + tint
// the albedo, soften normals, no metal/specular. Lets roofs/stones follow the Bible §8 palette without
// touching the vendor textures.
shader_type spatial;
render_mode diffuse_lambert_wrap, specular_disabled;

uniform sampler2D albedo_texture : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D normal_texture : hint_normal, filter_linear_mipmap_anisotropic, repeat_enable;
uniform bool use_normal = false;
uniform bool use_vertex_color = false;
uniform float normal_scale = 0.45;
uniform float saturation = 0.6;
uniform vec3 tint : source_color = vec3(1.0);
uniform float brightness = 1.0;

void fragment() {
	vec3 c = texture(albedo_texture, UV).rgb;
	if (use_vertex_color) {
		c *= COLOR.rgb;
	}
	float l = dot(c, vec3(0.3, 0.59, 0.11));
	ALBEDO = mix(vec3(l), c, saturation) * tint * brightness;
	if (use_normal) {
		NORMAL_MAP = texture(normal_texture, UV).rgb;
		NORMAL_MAP_DEPTH = normal_scale;
	}
	ROUGHNESS = 0.92;
}
