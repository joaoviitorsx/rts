extends RefCounted
## Distant-forest impostors: one camera-facing quad per tree in a MultiMesh, using the atlases baked by
## scenes/tools/ImpostorBaker (8 yaw views, world normals, palette param). If the bake is missing the forest
## simply stays as meshes.

const Clusters := preload("res://game/vegetation/Clusters.gd")
const SHADER := preload("res://game/vegetation/tree_impostor.gdshader")
const META := "res://assets/environment/impostors/impostors.json"

static var _meta = null
static var _meshes := {}


static func available(scene_path: String) -> bool:
	if _meta == null:
		_meta = {}
		if FileAccess.file_exists(META):
			_meta = JSON.parse_string(FileAccess.get_file_as_string(META)).get("trees", {})
		else:
			push_warning("Tree impostors not baked (%s); distant forest stays as meshes" % META)
	return _meta.has(scene_path) and ResourceLoader.exists(_meta[scene_path]["albedo"])


## Quad mesh + material per tree type. The quad's size/offset only feed the AABB; the shader builds the billboard.
static func _mesh(scene_path: String) -> QuadMesh:
	if _meshes.has(scene_path):
		return _meshes[scene_path]
	var t: Dictionary = _meta[scene_path]
	var m := ShaderMaterial.new()
	m.shader = SHADER
	m.set_shader_parameter("albedo_atlas", load(t["albedo"]))
	m.set_shader_parameter("normal_atlas", load(t["normal"]))
	m.set_shader_parameter("cell", t["cell"])
	m.set_shader_parameter("center_y", t["center_y"])
	m.set_shader_parameter("lightness", t["lightness"])
	var q := QuadMesh.new()
	q.size = Vector2(t["cell"], t["cell"])
	q.center_offset = Vector3(0, t["center_y"], 0)
	q.material = m
	_meshes[scene_path] = q
	return q


## Impostors for `transforms` (world placements of the scene root), visible from `begin` metres on
## (`hysteresis` metres either side before switching, so the swap does not flicker at the threshold).
static func add(parent: Node, scene_path: String, transforms: Array, begin: float, hysteresis: float) -> MultiMeshInstance3D:
	if transforms.is_empty() or not available(scene_path):
		return null
	var src := Clusters.first_mesh(scene_path)
	var mm := MultiMesh.new()
	mm.transform_format = MultiMesh.TRANSFORM_3D
	mm.mesh = _mesh(scene_path)
	mm.instance_count = transforms.size()
	for i in transforms.size():
		mm.set_instance_transform(i, (transforms[i] as Transform3D) * src["xform"])
	var node := MultiMeshInstance3D.new()
	node.name = "IMP_" + scene_path.get_file().get_basename()
	node.multimesh = mm
	node.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	node.visibility_range_begin = begin
	node.visibility_range_begin_margin = hysteresis
	parent.add_child(node)
	return node
