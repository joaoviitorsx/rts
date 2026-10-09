// Impostor bake (scenes/tools/ImpostorBaker): renders a tree unshaded, opaque (alpha = coverage), one pass each:
//  pass 0 → albedo: bark texture colour; leaves today's palette (only fills filtering neighbours at runtime).
//  pass 1 → world normal * 0.5 + 0.5, with the same spherical crown normals as plants.gdshader.
//  pass 2 → surface param in R: bark 0.25, leaves 0.35 + 0.65 * palette t; the baker packs it into the normal
//           atlas alpha so the runtime impostor recolours leaves with the live global palette.
// The viewport writes sRGB, so data passes are pre-linearised to survive the encode.
shader_type spatial;
render_mode unshaded, cull_disabled;

global uniform vec4 veg_tip;
global uniform vec4 veg_shadow;

uniform sampler2D albedo_texture : source_color, filter_linear_mipmap, repeat_enable;
uniform vec4 albedo_color : source_color = vec4(1.0);
uniform bool is_leaf = false;
uniform int bake_pass = 0;
uniform float alpha_scissor = 0.5;
uniform float lightness = 1.0;
uniform float crown_height = 0.0;
uniform float crown_normal_blend = 0.75;

varying vec3 v_world;
varying vec3 v_crown;

vec3 to_linear(vec3 c) {
	return mix(c / 12.92, pow((c + 0.055) / 1.055, vec3(2.4)), step(vec3(0.04045), c));
}

void vertex() {
	v_world = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	v_crown = MODEL_MATRIX[3].xyz + vec3(0.0, crown_height * length(MODEL_MATRIX[1].xyz), 0.0);
}

void fragment() {
	vec4 t = texture(albedo_texture, UV) * albedo_color;
	float l = dot(t.rgb, vec3(0.3, 0.59, 0.11));
	float pal = smoothstep(0.08, 0.75, l);
	vec3 n = NORMAL;
	if (is_leaf && crown_height > 0.0) {
		vec3 sphere = normalize((VIEW_MATRIX * vec4(normalize(v_world - v_crown), 0.0)).xyz);
		n = normalize(mix(n, sphere, crown_normal_blend));
	}
	if (bake_pass == 1) {
		vec3 wn = normalize((INV_VIEW_MATRIX * vec4(n, 0.0)).xyz);
		ALBEDO = to_linear(wn * 0.5 + 0.5);
	} else if (bake_pass == 2) {
		ALBEDO = to_linear(vec3(is_leaf ? 0.35 + 0.65 * pal : 0.25, 0.0, 0.0));
	} else {
		ALBEDO = is_leaf ? mix(veg_shadow.rgb, veg_tip.rgb, pal) * lightness : t.rgb;
	}
	if (t.a < alpha_scissor) {
		discard;
	}
}
