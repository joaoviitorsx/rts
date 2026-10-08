extends RefCounted
## Puts every Stylized Nature plant surface through game/vegetation/plants.gdshader (single palette).
## Works on MeshInstance3D (surface overrides) and on meshes for MultiMesh (returns a converted copy).
## Bark, rocks and mushrooms keep their own materials.

const SHADER := preload("res://game/vegetation/plants.gdshader")
const FOLIAGE := ["Grass", "Leaves", "Leaves_NormalTree", "Leaves_Pine", "Leaves_TwistedTree"]
const TREE_LEAVES := ["Leaves_NormalTree", "Leaves_Pine", "Leaves_TwistedTree"]

## Approximate crown centre height (model units) per tree leaf material, for spherical crown normals.
const CROWN_HEIGHT := {"Leaves_NormalTree": 5.2, "Leaves_Pine": 4.6, "Leaves_TwistedTree": 9.0}

static var _cache := {}


## Material for one original surface material, or null if it is not a plant material.
static func material_for(original: Material, flower_slot: int, is_tree: bool) -> Material:
	if not (original is BaseMaterial3D):
		return null
	var name := original.resource_name
	var is_flower := name == "Flowers"
	if not is_flower and not FOLIAGE.has(name):
		return null
	var key := "%s|%d|%d|%d" % [name, original.get_instance_id(), flower_slot, int(is_tree)]
	if _cache.has(key):
		return _cache[key]
	var m := ShaderMaterial.new()
	m.shader = SHADER
	m.set_shader_parameter("albedo_texture", (original as BaseMaterial3D).albedo_texture)
	m.set_shader_parameter("is_flower", is_flower)
	m.set_shader_parameter("flower_slot", flower_slot)
	var leaves := TREE_LEAVES.has(name)
	m.set_shader_parameter("ground_blend_height", 0.0 if (is_tree and leaves) else 0.3)
	m.set_shader_parameter("sway", 1.0 if leaves else 0.4)
	m.set_shader_parameter("variation", 0.14 if leaves else 0.1)
	m.set_shader_parameter("lightness", 0.95 if name == "Leaves_Pine" else 1.0)
	if is_tree and leaves:
		m.set_shader_parameter("crown_height", CROWN_HEIGHT.get(name, 5.0))
	_cache[key] = m
	return m


static func apply(root: Node, flower_slot: int = 0) -> void:
	for node in root.find_children("*", "MeshInstance3D", true, false):
		var mi := node as MeshInstance3D
		if mi.mesh == null:
			continue
		var is_tree := _is_tree(mi)
		for i in mi.mesh.get_surface_count():
			var m := material_for(mi.mesh.surface_get_material(i), flower_slot, is_tree)
			if m:
				mi.set_surface_override_material(i, m)


## Copy of `mesh` with plant surfaces converted (for MultiMesh, which has no per-surface override).
static func convert_mesh(mesh: Mesh, flower_slot: int = 0, is_tree: bool = false) -> Mesh:
	var copy := mesh.duplicate() as Mesh
	for i in copy.get_surface_count():
		var m := material_for(mesh.surface_get_material(i), flower_slot, is_tree)
		if m:
			copy.surface_set_material(i, m)
	return copy


static func _is_tree(mi: MeshInstance3D) -> bool:
	var n: Node = mi
	while n:
		var s := String(n.name)
		if s.contains("Tree") or s.begins_with("ENV_Oak") or s.begins_with("ENV_Pine"):
			return true
		n = n.get_parent()
	return false
