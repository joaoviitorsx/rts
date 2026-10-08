extends RefCounted
## MultiMesh helpers for decoration that must follow the single plant palette:
## rings of tufts/flowers around points (rocks, trunks, fences, path edges) and forests of trees.

const PlantPalette := preload("res://game/vegetation/PlantPalette.gd")

static var _mesh_cache := {}


## First mesh of a scene and its transform relative to the scene root (keeps our normalization scale).
static func first_mesh(scene_path: String) -> Dictionary:
	if _mesh_cache.has(scene_path):
		return _mesh_cache[scene_path]
	var root := (load(scene_path) as PackedScene).instantiate()
	var out := {}
	for n in root.find_children("*", "MeshInstance3D", true, false):
		var xf := Transform3D()
		var node: Node = n
		while node != root:
			xf = (node as Node3D).transform * xf
			node = node.get_parent()
		out = {"mesh": (n as MeshInstance3D).mesh, "xform": xf}
		break
	root.free()
	_mesh_cache[scene_path] = out
	return out


## One MultiMeshInstance3D for `transforms` (world placements of the scene root).
static func add(parent: Node, scene_path: String, transforms: Array, flower_slot: int = 0, is_tree: bool = false,
		shadows: bool = false, fade: float = 0.0) -> MultiMeshInstance3D:
	var src := first_mesh(scene_path)
	if src.is_empty() or transforms.is_empty():
		return null
	var mm := MultiMesh.new()
	mm.transform_format = MultiMesh.TRANSFORM_3D
	mm.mesh = PlantPalette.convert_mesh(src["mesh"], flower_slot, is_tree)
	mm.instance_count = transforms.size()
	for i in transforms.size():
		mm.set_instance_transform(i, (transforms[i] as Transform3D) * src["xform"])
	var node := MultiMeshInstance3D.new()
	node.name = "MM_" + scene_path.get_file().get_basename()
	node.multimesh = mm
	node.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_ON if shadows else GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	if fade > 0.0:
		node.visibility_range_end = fade
		node.visibility_range_end_margin = 10.0
		node.visibility_range_fade_mode = GeometryInstance3D.VISIBILITY_RANGE_FADE_SELF
	parent.add_child(node)
	return node


## Random placements in a ring [r0, r1] around each centre.
static func ring(centers: Array, r0: float, r1: float, per_center: int, rng: RandomNumberGenerator,
		scale_range: Vector2) -> Array:
	var out: Array = []
	for c: Vector3 in centers:
		for i in per_center:
			var a := rng.randf() * TAU
			var r := rng.randf_range(r0, r1)
			var s := rng.randf_range(scale_range.x, scale_range.y)
			out.append(Transform3D(Basis(Vector3.UP, rng.randf() * TAU).scaled(Vector3.ONE * s),
				c + Vector3(cos(a) * r, 0, sin(a) * r)))
	return out
