extends RefCounted
## Applies tinted_pbr.gdshader to roof/stone materials by name (runtime override, vendor untouched).
## Roof styles give the per-house variation asked in the art feedback: tile (muted terracotta),
## thatch (straw) and slate (cool grey) — same geometry, different material override.

const SHADER := preload("res://game/materials/tinted_pbr.gdshader")

const ROOF := ["MI_RoundTiles"]
const STONE := ["MI_UnevenBrick", "MI_Brick", "MI_RockTrim", "Rocks", "PathRocks"]

const ROOF_STYLES := {
	"tile": {"saturation": 0.38, "tint": Color(1.0, 0.84, 0.74), "brightness": 0.92},
	"thatch": {"saturation": 0.15, "tint": Color(1.0, 0.8, 0.52), "brightness": 1.15},
	"slate": {"saturation": 0.0, "tint": Color(0.72, 0.78, 0.86), "brightness": 0.85},
}
const STONE_STYLE := {"saturation": 0.25, "tint": Color(1.0, 0.95, 0.87), "brightness": 1.12}

static var _cache := {}


static func apply(root: Node, roof_style: String = "tile") -> void:
	for node in root.find_children("*", "MeshInstance3D", true, false):
		var mi := node as MeshInstance3D
		if mi.mesh == null:
			continue
		for i in mi.mesh.get_surface_count():
			var m := material_for(mi.mesh.surface_get_material(i), roof_style)
			if m:
				mi.set_surface_override_material(i, m)


static func material_for(original: Material, roof_style: String) -> Material:
	if not (original is StandardMaterial3D):
		return null
	var src := original as StandardMaterial3D
	var name := src.resource_name
	var style: Dictionary
	if ROOF.has(name):
		style = ROOF_STYLES.get(roof_style, ROOF_STYLES["tile"])
	elif STONE.has(name):
		style = STONE_STYLE
	else:
		return null
	var key := "%d|%s|%s" % [src.get_instance_id(), name, roof_style if ROOF.has(name) else ""]
	if _cache.has(key):
		return _cache[key]
	var m := ShaderMaterial.new()
	m.shader = SHADER
	m.set_shader_parameter("albedo_texture", src.albedo_texture)
	m.set_shader_parameter("use_vertex_color", src.vertex_color_use_as_albedo)
	if src.normal_enabled and src.normal_texture:
		m.set_shader_parameter("normal_texture", src.normal_texture)
		m.set_shader_parameter("use_normal", true)
	for k in style:
		m.set_shader_parameter(k, style[k])
	_cache[key] = m
	return m
