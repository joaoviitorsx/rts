extends Node3D

const GroundMask := preload("res://game/terrain/GroundMask.gd")
const PlantPalette := preload("res://game/vegetation/PlantPalette.gd")
const GroundColorBaker := preload("res://game/terrain/GroundColorBaker.gd")
const MaterialTint := preload("res://game/materials/MaterialTint.gd")
## LOOKDEV_GROUND (20×20 m): ground + grass carpet + vegetation integration look-dev, isolated from the village.
## Args after "--": --zoom=near|mid  --palette=moss|meadow  --no-grass  --perf=N (seconds; prints FPS then quits)
## Keys: Z zoom · G palette · H grass on/off.

const AREA := Rect2(0, 0, 20, 20)
const MASK_RECT := Rect2(-8, -8, 36, 36)
const ZOOMS := {"near": 20.0, "mid": 38.0}
const FOCUS := Vector3(10, 0, 10)

var _zoom := "near"
var _palette := "meadow"
var _roof := "tile"
var _walker: Node3D
var _walk_t := 0.0
var _road := PackedVector2Array([Vector2(-8, 14.2), Vector2(4, 14.7), Vector2(10, 13.8), Vector2(16, 14.5), Vector2(28, 14.0)])
var _perf_seconds := 0.0
var _perf_frames: Array[float] = []
var _elapsed := 0.0
var _label: Label
var _baker: Node
var _mask_size := Vector2i.ZERO


func _ready() -> void:
	var args := OS.get_cmdline_user_args()
	for a in args:
		if a.begins_with("--zoom="): _zoom = a.substr(7)
		if a.begins_with("--palette="): _palette = a.substr(10)
		if a.begins_with("--roof="): _roof = a.substr(7)
		if a.begins_with("--perf="): _perf_seconds = float(a.substr(7))
		if a == "--vsync=off": DisplayServer.window_set_vsync_mode(DisplayServer.VSYNC_DISABLED)
	_setup_environment()
	$VillageTerrain.apply_palette(_palette)

	var mask := _build_mask()
	_baker = GroundColorBaker.new()
	add_child(_baker)
	_baker.bake(Vector2i(mask.w, mask.h))
	if not args.has("--no-grass"):
		$GrassCarpet.stream(mask, MASK_RECT, $CameraRig, 7)   # same streaming path as the village
	PlantPalette.apply($Nature)
	MaterialTint.apply($Nature, _roof)
	_scatter_clusters()
	_spawn_villager()

	$CameraRig.EdgePan = false
	$CameraRig.FocusAt(FOCUS, ZOOMS.get(_zoom, 15.0))
	# Captures (--write-movie) must not react to stray keyboard/mouse input on the popup window.
	if OS.get_cmdline_args().has("--write-movie"):
		$CameraRig.set_process_unhandled_input(false)
		$CameraRig.set_process(false)
		set_process_unhandled_key_input(false)
	_label = Label.new()
	var layer := CanvasLayer.new()
	add_child(layer)
	layer.add_child(_label)
	_update_label()


func _build_mask() -> GroundMask:
	var m := GroundMask.new(MASK_RECT, 4)
	m.paint_polyline(_road, 2.2, GroundMask.PATH)
	# house (10,7) 4×4, door on the south side at x≈9: dirt only at the entrance + back yard, irregular edges
	m.paint_blob(Vector2(9.1, 10.2), Vector2(1.6, 1.2), GroundMask.DIRT, 0.9)
	m.paint_polyline(PackedVector2Array([Vector2(9.1, 10.6), Vector2(9.4, 13.4)]), 1.1, GroundMask.PATH, 0.85)
	m.paint_blob(Vector2(13.4, 4.4), Vector2(2.4, 1.8), GroundMask.DIRT, 0.9)
	m.blur(GroundMask.PATH, 0.9)
	m.blur(GroundMask.DIRT, 0.35)
	for t in $Nature.get_children():
		var n := String(t.name)
		if n.begins_with("ENV_Oak") or n.begins_with("ENV_Pine"):
			m.add_shade_disc(Vector2(t.position.x, t.position.z), 3.4, 0.8)
		elif n.begins_with("ENV_Rock"):
			m.add_shade_disc(Vector2(t.position.x, t.position.z), 2.0, 0.6)
		elif n.begins_with("ENV_Bush"):
			m.add_shade_disc(Vector2(t.position.x, t.position.z), 1.2, 0.5)
		elif n.begins_with("BLD_"):
			m.add_shade_walls(Vector2(t.position.x, t.position.z), Vector2(4, 4), t.rotation_degrees.y, 1.4, 0.55)
	m.blur(GroundMask.SHADE, 0.4, 1)
	m.shade_path_margins(0.45)
	m.publish()
	return m


## Tall tufts around rocks/trunks/fences, two flower colours in clusters, ferns under trees.
func _scatter_clusters() -> void:
	var rock := $Nature/ENV_Rock_A.position as Vector3
	var trunks: Array[Vector3] = []
	for t in $Nature.get_children():
		if String(t.name).begins_with("ENV_Oak") or String(t.name).begins_with("ENV_Pine"):
			trunks.append(t.position)
	var rng := RandomNumberGenerator.new()
	rng.seed = 11
	_ring("ENV_Grass_B", [rock], 1.3, 2.4, 40, rng, [0.9, 1.3])
	_ring("ENV_Grass_D", trunks, 0.5, 1.6, 22, rng, [0.8, 1.2])
	_ring("ENV_Fern_A", trunks, 1.0, 3.0, 6, rng, [0.6, 0.9])
	_ring("ENV_GroundLeaf_B", trunks, 0.6, 2.5, 10, rng, [0.7, 1.0])
	_ring("ENV_FlowerSingle_A", [rock], 1.6, 2.8, 10, rng, [0.12, 0.16], 0)          # cream flowers (0.25–0.35 m)
	_ring("ENV_FlowerSingle_A", trunks.slice(0, 1), 1.2, 2.6, 8, rng, [0.12, 0.16], 0)
	var edge: Array[Vector3] = []
	for i in 12:
		var t := float(i) / 11.0
		var p := _road_point(t)
		edge.append(Vector3(p.x, 0, p.y + (1.7 if i % 2 == 0 else -1.7)))
	_ring("ENV_FlowerSingle_B", edge, 0.0, 0.7, 4, rng, [0.12, 0.16], 1)            # yellow along the road
	_ring("ENV_Grass_A", edge, 0.0, 0.9, 6, rng, [0.6, 0.9])


func _ring(scene_id: String, centers: Array, r0: float, r1: float, per_center: int,
		rng: RandomNumberGenerator, scale_range: Array, flower_slot: int = 0) -> void:
	var src := _first_mesh(load("res://assets/environment/%s.tscn" % scene_id))
	if src.is_empty():
		return
	var xforms: Array[Transform3D] = []
	for c: Vector3 in centers:
		for i in per_center:
			var a := rng.randf() * TAU
			var r := rng.randf_range(r0, r1)
			var p := c + Vector3(cos(a) * r, 0, sin(a) * r)
			var s := rng.randf_range(scale_range[0], scale_range[1])
			xforms.append(Transform3D(Basis(Vector3.UP, rng.randf() * TAU).scaled(Vector3.ONE * s), p) * src["xform"])
	var mm := MultiMesh.new()
	mm.transform_format = MultiMesh.TRANSFORM_3D
	mm.mesh = PlantPalette.convert_mesh(src["mesh"], flower_slot)
	mm.instance_count = xforms.size()
	for i in xforms.size():
		mm.set_instance_transform(i, xforms[i])
	var node := MultiMeshInstance3D.new()
	node.multimesh = mm
	node.name = "Clusters_" + scene_id
	add_child(node)


func _first_mesh(scene: PackedScene) -> Dictionary:
	var root := scene.instantiate()
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
	return out


func _road_point(t: float) -> Vector2:
	var total := 0.0
	for i in _road.size() - 1:
		total += _road[i].distance_to(_road[i + 1])
	var d := t * total
	for i in _road.size() - 1:
		var seg := _road[i].distance_to(_road[i + 1])
		if d <= seg:
			return _road[i].lerp(_road[i + 1], d / seg)
		d -= seg
	return _road[_road.size() - 1]


func _spawn_villager() -> void:
	_walker = load("res://assets/characters/CHR_Villager_Base.tscn").instantiate()
	add_child(_walker)
	_walker.Play("walk", 1.0)


func _setup_environment() -> void:
	var env := Environment.new()
	var sky_mat := ProceduralSkyMaterial.new()
	sky_mat.sky_top_color = Color("6f9fd0")
	sky_mat.sky_horizon_color = Color("d9e4e8")
	sky_mat.ground_horizon_color = Color("c9d3b8")
	sky_mat.ground_bottom_color = Color("6b7a52")
	var sky := Sky.new()
	sky.sky_material = sky_mat
	env.background_mode = Environment.BG_SKY
	env.sky = sky
	# Warm, slightly green ambient (a blue sky ambient makes every shadow cyan)
	env.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	env.ambient_light_color = Color("b9c3a0")
	env.ambient_light_energy = 0.6
	env.reflected_light_source = Environment.REFLECTION_SOURCE_DISABLED
	env.tonemap_mode = Environment.TONE_MAPPER_ACES
	env.tonemap_exposure = 0.9
	env.ssao_enabled = true
	env.ssao_radius = 1.2
	env.ssao_intensity = 1.6
	env.ssao_power = 1.4
	env.fog_enabled = true
	env.fog_light_color = Color("cfdbe0")
	env.fog_density = 0.0008
	env.fog_sky_affect = 0.2
	env.fog_aerial_perspective = 0.08
	env.adjustment_enabled = true
	env.adjustment_saturation = 1.15
	env.adjustment_contrast = 1.08
	var grad := Gradient.new()
	grad.set_color(0, Color(0.02, 0.03, 0.05))
	grad.set_color(1, Color(1.0, 0.98, 0.94))
	var lut := GradientTexture1D.new()
	lut.gradient = grad
	env.adjustment_color_correction = lut
	env.glow_enabled = true
	env.glow_intensity = 0.1
	var we := WorldEnvironment.new()
	we.environment = env
	add_child(we)
	var sun := DirectionalLight3D.new()
	sun.rotation_degrees = Vector3(-36, -140, 0)       # lower, from the south-west
	sun.light_color = Color(1.0, 0.92, 0.8)
	sun.light_energy = 1.2
	sun.shadow_enabled = true
	sun.shadow_blur = 0.6
	sun.directional_shadow_max_distance = 90.0
	add_child(sun)


func _process(delta: float) -> void:
	if _walker:
		_walk_t = fmod(_walk_t + delta * 0.035, 1.0)
		var t := pingpong(_walk_t * 2.0, 1.0)
		var p := _road_point(0.25 + t * 0.5)
		var q := _road_point(0.25 + clampf(t + (0.01 if _walk_t < 0.5 else -0.01), 0, 1) * 0.5)
		_walker.position = Vector3(p.x, 0, p.y)
		if p.distance_to(q) > 0.0001:
			_walker.rotation.y = atan2(q.x - p.x, q.y - p.y)
	if _perf_seconds > 0:
		_elapsed += delta
		if _elapsed > 2.0:
			_perf_frames.append(delta * 1000.0)
		if _elapsed > 2.0 + _perf_seconds:
			var sorted := _perf_frames.duplicate()
			sorted.sort()
			sorted.reverse()
			var avg := 0.0
			for f in _perf_frames: avg += f
			avg /= _perf_frames.size()
			var low := 0.0
			var k := maxi(1, sorted.size() / 100)
			for i in k: low += sorted[i]
			low /= k
			print("LOOKDEV PERF fps=%.1f avg_ms=%.2f 1%%low_fps=%.1f worst_ms=%.1f prims=%d draws=%d" % [1000.0 / avg, avg, 1000.0 / low, sorted[0],
				Performance.get_monitor(Performance.RENDER_TOTAL_PRIMITIVES_IN_FRAME), Performance.get_monitor(Performance.RENDER_TOTAL_DRAW_CALLS_IN_FRAME)])
			get_tree().quit()


func _unhandled_key_input(e: InputEvent) -> void:
	if not (e is InputEventKey and e.pressed and not e.echo):
		return
	match e.keycode:
		KEY_Z:
			_zoom = "mid" if _zoom == "near" else "near"
			$CameraRig.FocusAt(FOCUS, ZOOMS[_zoom])
		KEY_G:
			_palette = "moss" if _palette == "meadow" else "meadow"
			$VillageTerrain.apply_palette(_palette)
			_baker.bake(_baker._viewport.size)
		KEY_H:
			$GrassCarpet.visible = not $GrassCarpet.visible
		KEY_R:
			_roof = {"tile": "thatch", "thatch": "slate", "slate": "tile"}[_roof]
			MaterialTint.apply($Nature, _roof)
	_update_label()


func _update_label() -> void:
	_label.text = "LOOKDEV · paleta %s · telhado %s · zoom %s   [Z zoom · G paleta · H grama · R telhado]" % [_palette, _roof, _zoom]
