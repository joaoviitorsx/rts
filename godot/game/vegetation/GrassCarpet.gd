extends Node3D
## Dense grass carpet: short geometry blades in clumps, MultiMesh chunked for culling.
## Colour/height/LOD are decided in grass_carpet.gdshader from the shared ground mask, so blades vanish on
## paths, dirt and fields and their roots match the ground colour exactly.

const SHADER := preload("res://game/vegetation/grass_carpet.gdshader")
const GroundMask := preload("res://game/terrain/GroundMask.gd")

@export var spacing := 0.12             ## metres between clumps (jittered grid)
@export var chunk_size := 8.0
@export var blades_per_clump := 7
@export var near_range := 24.0          ## dense LOD until here; beyond, a sparse LOD (every 3rd clump)
@export var blade_height := Vector2(0.2, 0.42)
@export var blade_width := 0.05
@export var fade_end := 65.0

var instances := 0

## Streaming (large maps): only chunks within `stream_radius` of `stream_focus` exist; a few are built per
## tick, far ones are freed. Memory and instance count stay bounded whatever the map size.
@export var stream_radius := 70.0
@export var chunks_per_tick := 6
var _mask
var _area: Rect2
var _seed := 1
var _focus: Node3D
var _chunks := {}          # Vector2i -> Array[Node]
var _tick := 0.0
var _mesh: ArrayMesh
var _material: ShaderMaterial


func stream(mask, area: Rect2, focus: Node3D, seed_value: int = 1) -> void:
	_mask = mask
	_area = area
	_focus = focus
	_seed = seed_value
	var rng := RandomNumberGenerator.new()
	rng.seed = seed_value
	_mesh = _clump_mesh(rng)
	_material = _make_material()
	set_process(true)


func _make_material() -> ShaderMaterial:
	var material := ShaderMaterial.new()
	material.shader = SHADER
	material.set_shader_parameter("fade_end", fade_end)
	material.set_shader_parameter("fade_start", fade_end * 0.6)
	return material


func _ready() -> void:
	set_process(false)


func _process(_delta: float) -> void:
	if _focus == null:
		return
	var cam := get_viewport().get_camera_3d()
	if cam == null:
		return
	# Grass is only visible where the camera distance < fade_end: stream just that disc around the point
	# under the camera (zero grass when zoomed far out — the baked ground colour carries the look).
	var eye := cam.global_position
	var radius := sqrt(maxf(fade_end * fade_end - eye.y * eye.y, 0.0))
	var ground := Vector2(eye.x, eye.z)
	var center := Vector2i(floori(ground.x / chunk_size), floori(ground.y / chunk_size))
	_tick -= _delta
	if _tick <= 0.0:
		_tick = 0.25
		for key: Vector2i in _chunks.keys():
			if (Vector2(key) + Vector2(0.5, 0.5)).distance_to(ground / chunk_size) * chunk_size > radius + chunk_size * 2.0:
				for n: Node in _chunks[key]:
					n.queue_free()
				_chunks.erase(key)
		_queue.clear()
		var r := int(ceil(radius / chunk_size))
		for dz in range(-r, r + 1):
			for dx in range(-r, r + 1):
				var key := center + Vector2i(dx, dz)
				if _chunks.has(key) or (Vector2(key) + Vector2(0.5, 0.5)).distance_to(ground / chunk_size) * chunk_size > radius + chunk_size:
					continue
				if Rect2(Vector2(key) * chunk_size, Vector2.ONE * chunk_size).intersects(_area):
					_queue.append(key)
		_queue.sort_custom(func(a, b): return (a - center).length_squared() < (b - center).length_squared())
	# Incremental build with a per-frame time budget (no hitches).
	var start := Time.get_ticks_usec()
	while not _queue.is_empty() and Time.get_ticks_usec() - start < budget_usec:
		if _building.is_empty():
			var key: Vector2i = _queue[0]
			if _chunks.has(key):
				_queue.pop_front()
				continue
			var rng := RandomNumberGenerator.new()
			rng.seed = hash(Vector3i(key.x, key.y, _seed))
			_building = {"key": key, "y": float(key.y) * chunk_size, "list": [], "rng": rng}
		if _step_chunk(start):
			_queue.pop_front()


@export var budget_usec := 1500         ## max time per frame spent building grass chunks
var _queue: Array = []
var _building := {}


## Builds rows of the current chunk until the frame budget runs out. Returns true when the chunk is done.
func _step_chunk(start: int) -> bool:
	var key: Vector2i = _building["key"]
	var rng: RandomNumberGenerator = _building["rng"]
	var list: Array = _building["list"]
	var x0 := float(key.x) * chunk_size
	var y_end := float(key.y) * chunk_size + chunk_size
	while _building["y"] < y_end:
		var y: float = _building["y"]
		var x := x0
		while x < x0 + chunk_size:
			var p := Vector3(x + rng.randf_range(-0.45, 0.45) * spacing, 0, y + rng.randf_range(-0.45, 0.45) * spacing)
			if _area.has_point(Vector2(p.x, p.z)):
				var m: Color = _mask.sample(p)
				if maxf(maxf(m.r, m.g), m.b) < 0.8:
					var sc := rng.randf_range(0.8, 1.2)
					list.append(Transform3D(Basis(Vector3.UP, rng.randf() * TAU).scaled(Vector3(sc, rng.randf_range(0.8, 1.25) * sc, sc)), p))
			x += spacing
		_building["y"] = y + spacing
		if Time.get_ticks_usec() - start > budget_usec:
			return false
	var nodes: Array = []
	if not list.is_empty():
		nodes.append(_add_chunk(_mesh, _material, list, "Grass_%d_%d" % [key.x, key.y], 0.0, near_range))
		var sparse: Array = []
		for i in range(0, list.size(), 3):
			var t: Transform3D = list[i]
			sparse.append(Transform3D(t.basis.scaled(Vector3(1.35, 1.0, 1.35)), t.origin))
		nodes.append(_add_chunk(_mesh, _material, sparse, "GrassFar_%d_%d" % [key.x, key.y], near_range, fade_end + 5.0))
	_chunks[key] = nodes
	_building = {}
	return true


func _add_chunk(mesh: Mesh, material: Material, list: Array, node_name: String, begin: float, end: float) -> Node:
	var mm := MultiMesh.new()
	mm.transform_format = MultiMesh.TRANSFORM_3D
	mm.mesh = mesh
	mm.instance_count = list.size()
	for i in list.size():
		mm.set_instance_transform(i, list[i])
	var node := MultiMeshInstance3D.new()
	node.name = node_name
	node.multimesh = mm
	node.material_override = material
	node.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	node.visibility_range_begin = begin
	node.visibility_range_end = end
	add_child(node)
	return node


## One clump: several tapered, slightly curved blades (3 triangles each). UV.y = 0 root … 1 tip.
func _clump_mesh(rng: RandomNumberGenerator) -> ArrayMesh:
	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	for b in blades_per_clump:
		var ang := rng.randf() * TAU
		var off := Vector3(rng.randf_range(-0.06, 0.06), 0, rng.randf_range(-0.06, 0.06))
		var hgt := rng.randf_range(blade_height.x, blade_height.y)
		var side := Vector3(cos(ang), 0, sin(ang)) * blade_width * 0.5
		var lean := Vector3(-sin(ang), 0, cos(ang)) * rng.randf_range(0.02, 0.08)
		var b0 := off - side
		var b1 := off + side
		var tip := off + lean + Vector3.UP * hgt
		for tri in [[b0, b1, tip]]:          # 1 triangle per blade (cheap; reads fine at RTS distance)
			for v: Vector3 in tri:
				st.set_normal(Vector3.UP)
				st.set_uv(Vector2(0.5, (v.y - off.y) / hgt))
				st.add_vertex(v)
	return st.commit()
