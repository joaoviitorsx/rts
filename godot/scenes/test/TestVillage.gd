extends Node3D
## TEST_VILLAGE_01 runtime, built with the approved ground look-dev pipeline:
## ground mask → baked ground colour → streamed grass carpet, single plant palette, roof/stone re-grade,
## MultiMesh forests framing the village, tuft/flower clusters at path edges, fences, trunks and rocks,
## 20 animated villagers, official camera, cozy environment.
## Args after "--": --zoom=near|mid|far  --no-grass  --perf=N  --vsync=off     Keys: Z zoom · H grass

const GroundMask := preload("res://game/terrain/GroundMask.gd")
const GroundColorBaker := preload("res://game/terrain/GroundColorBaker.gd")
const PlantPalette := preload("res://game/vegetation/PlantPalette.gd")
const Clusters := preload("res://game/vegetation/Clusters.gd")
const MaterialTint := preload("res://game/materials/MaterialTint.gd")
const CozyEnvironment := preload("res://game/visual/CozyEnvironment.gd")
const StaticMerge := preload("res://game/visual/StaticMerge.gd")
const FOREST_CHUNK := 64.0

const LAYOUT := "res://scenes/test/TEST_VILLAGE_01_layout.json"
const ZOOMS := {"near": 22.0, "mid": 45.0, "far": 95.0}
const FOCUS := Vector3(130, 0, 129)
const MALE := "res://assets/characters/CHR_Villager_Base.tscn"
const FEMALE := "res://assets/characters/CHR_Villager_Base_F.tscn"

var _layout: Dictionary
var _zoom := "mid"
var _walkers: Array = []   # {node, curve, offset, dir, speed, lane}
var _perf_seconds := 0.0
var _perf_frames: Array[float] = []
var _elapsed := 0.0
var _rng := RandomNumberGenerator.new()


func _ready() -> void:
	_rng.seed = 7
	var args := OS.get_cmdline_user_args()
	for a in args:
		if a.begins_with("--zoom="): _zoom = a.substr(7)
		if a.begins_with("--perf="): _perf_seconds = float(a.substr(7))
		if a == "--vsync=off": DisplayServer.window_set_vsync_mode(DisplayServer.VSYNC_DISABLED)
	CozyEnvironment.build(self)
	$VillageTerrain.apply_palette("meadow")
	_layout = JSON.parse_string(FileAccess.get_file_as_string(LAYOUT))

	var t0 := Time.get_ticks_msec()
	var mask := _build_mask()
	var baker := GroundColorBaker.new()
	add_child(baker)
	baker.bake(Vector2i(mask.w, mask.h))
	var t1 := Time.get_ticks_msec()
	if not args.has("--no-grass"):
		var r: Array = _layout["mask_rect"]
		$GrassCarpet.stream(mask, Rect2(r[0], r[1], r[2], r[3]), $CameraRig, 7)
	_build_forest()
	_build_clusters()
	var merged := 0
	for b in $Buildings.get_children():
		MaterialTint.apply(b, b.get_meta("roof", "tile"))
		merged += StaticMerge.merge(b)
	MaterialTint.apply($Rocks)
	MaterialTint.apply($Props)
	_spawn_villagers()
	print("VILLAGE mask+bake_ms=%d total_ready_ms=%d trees=%d merged_pieces=%d" % [t1 - t0, Time.get_ticks_msec() - t0, _layout["trees"].size(), merged])

	$CameraRig.EdgePan = false
	$CameraRig.SetBounds(Rect2(60, 95, 150, 75))
	$CameraRig.FocusAt(FOCUS, ZOOMS.get(_zoom, 45.0))
	if OS.get_cmdline_args().has("--write-movie"):   # captures ignore stray input on the popup window
		$CameraRig.set_process_unhandled_input(false)
		$CameraRig.set_process(false)
		set_process_unhandled_key_input(false)


func _v2(a: Array) -> Vector2:
	return Vector2(a[0], a[1])


func _build_mask() -> GroundMask:
	var r: Array = _layout["mask_rect"]
	var m := GroundMask.new(Rect2(r[0], r[1], r[2], r[3]), 3)
	for road in _layout["roads"]:
		var pts := PackedVector2Array()
		for p in road["points"]: pts.append(_v2(p))
		m.paint_polyline(pts, road["width"], GroundMask.PATH)
	for p in _layout["paths"]:
		var pts := PackedVector2Array()
		for q in p["points"]: pts.append(_v2(q))
		m.paint_polyline(pts, p["width"], GroundMask.PATH, p["value"])
	for d in _layout["dirt"]:
		m.paint_blob(_v2(d["center"]), _v2(d["size"]), GroundMask.DIRT, d["value"])
	for f in _layout["plowed"]:
		m.paint_rect(_v2(f["center"]), _v2(f["size"]), f["rot"], GroundMask.PLOWED, 1.0, 0.3)
	m.blur(GroundMask.PATH, 0.9)
	m.blur(GroundMask.DIRT, 0.4)
	m.blur(GroundMask.PLOWED, 0.3, 1)
	for s in _layout["shade"]:
		m.add_shade_disc(_v2(s["center"]), s["radius"], s["strength"])
	for wl in _layout["walls"]:
		m.add_shade_walls(_v2(wl["center"]), _v2(wl["size"]), wl["rot"], 1.4, 0.55)
	m.blur(GroundMask.SHADE, 0.4, 1)
	m.shade_path_margins(0.45)
	m.publish()
	return m


## Forest/tree MultiMeshes grouped by scene (draw calls stay low with hundreds of trees).
func _build_forest() -> void:
	# Grouped by tree type AND 64 m tile: per-tile frustum culling + mesh LOD by distance.
	var groups := {}
	for t in _layout["trees"]:
		var xf := Transform3D(Basis(Vector3.UP, deg_to_rad(t["rot"])).scaled(Vector3.ONE * t["scale"]),
			Vector3(t["pos"][0], 0, t["pos"][1]))
		var key := "%s|%d|%d" % [t["scene"], floori(t["pos"][0] / FOREST_CHUNK), floori(t["pos"][1] / FOREST_CHUNK)]
		if not groups.has(key):
			groups[key] = []
		groups[key].append(xf)
	var forest := Node3D.new()
	forest.name = "Forest"
	add_child(forest)
	for key in groups:
		Clusters.add(forest, key.get_slice("|", 0), groups[key], 0, true, true)


## Integration clusters: tall tufts at rocks/fences/trunks, bushes at the forest/field transition,
## flowers in two colours per area (cream near rocks/trunks, yellow along the road), ferns in the woods.
func _build_clusters() -> void:
	var deco := Node3D.new()
	deco.name = "Clusters"
	add_child(deco)
	var rocks: Array = []
	for p in _layout["rocks"]: rocks.append(Vector3(p[0], 0, p[1]))
	var fences: Array = []
	for p in _layout["fences"]: fences.append(Vector3(p[0], 0, p[1]))
	var trunks: Array = []
	for t in _layout["trees"]: trunks.append(Vector3(t["pos"][0], 0, t["pos"][1]))
	var edge_trees: Array = trunks.filter(func(v): return _rng.randf() < 0.35)
	var road_edge: Array = []
	for road in _layout["roads"]:
		var pts: Array = road["points"]
		for i in pts.size() - 1:
			var a := _v2(pts[i])
			var b := _v2(pts[i + 1])
			var n := (b - a).normalized().orthogonal()
			var steps := int(a.distance_to(b) / 5.0)
			for k in steps:
				var off: float = (float(road["width"]) * 0.5 + 0.8) * (1.0 if _rng.randf() < 0.5 else -1.0)
				var p: Vector2 = a.lerp(b, (k + _rng.randf()) / maxf(steps, 1)) + n * off
				road_edge.append(Vector3(p.x, 0, p.y))
	var E := "res://assets/environment/%s.tscn"
	Clusters.add(deco, E % "ENV_Grass_B", Clusters.ring(rocks, 1.4, 2.8, 30, _rng, Vector2(0.9, 1.3)))
	Clusters.add(deco, E % "ENV_Grass_D", Clusters.ring(fences, 0.1, 0.8, 6, _rng, Vector2(0.8, 1.2)))
	Clusters.add(deco, E % "ENV_Grass_D", Clusters.ring(edge_trees, 0.4, 1.5, 8, _rng, Vector2(0.8, 1.2)), 0, false, false, 90.0)
	Clusters.add(deco, E % "ENV_Bush_A", Clusters.ring(edge_trees, 3.0, 6.0, 2, _rng, Vector2(0.8, 1.4)), 0, false, true, 140.0)
	Clusters.add(deco, E % "ENV_Bush_B", Clusters.ring(edge_trees, 3.0, 6.0, 1, _rng, Vector2(0.8, 1.3)), 0, false, true, 140.0)
	Clusters.add(deco, E % "ENV_Fern_A", Clusters.ring(edge_trees, 1.0, 3.0, 2, _rng, Vector2(0.6, 0.9)), 0, false, false, 90.0)
	Clusters.add(deco, E % "ENV_FlowerSingle_A", Clusters.ring(rocks, 1.8, 3.2, 8, _rng, Vector2(0.12, 0.16)), 0)
	Clusters.add(deco, E % "ENV_FlowerSingle_A", Clusters.ring(edge_trees.slice(0, 25), 1.2, 2.6, 5, _rng, Vector2(0.12, 0.16)), 0, false, false, 90.0)
	Clusters.add(deco, E % "ENV_FlowerSingle_B", Clusters.ring(road_edge, 0.0, 0.8, 3, _rng, Vector2(0.12, 0.16)), 1, false, false, 90.0)
	Clusters.add(deco, E % "ENV_Grass_A", Clusters.ring(road_edge, 0.0, 1.0, 4, _rng, Vector2(0.6, 0.9)), 0, false, false, 90.0)
	var gardens: Array = []
	for g in _layout.get("gardens", []): gardens.append(Vector3(g[0], 0, g[1]))
	Clusters.add(deco, E % "ENV_GroundLeaf_A", Clusters.ring(gardens, 0.0, 0.7, 6, _rng, Vector2(0.5, 0.7)))


func _spawn_villagers() -> void:
	var roads := [$Road.curve, $RoadHall.curve]
	for i in 14:
		var curve: Curve3D = roads[1] if i % 5 == 4 else roads[0]
		var v := _spawn(i)
		v.Play("walk_carry" if i % 3 == 0 else "walk", 1.0)
		_walkers.append({"node": v, "curve": curve, "offset": _rng.randf() * curve.get_baked_length(),
			"dir": 1.0 if i % 2 == 0 else -1.0, "speed": _rng.randf_range(1.0, 1.4), "lane": 0.7 if i % 2 == 0 else -0.7})
	var stations := [["BLD_Woodcutter_A", Vector3(-1.2, 0, -2.6), "chop"], ["BLD_Woodcutter_A", Vector3(1.5, 0, -3.0), "idle_carry"],
		["BLD_Field_A", Vector3(-1.5, 0, 0.5), "plant_seed"], ["BLD_Field_A_2", Vector3(1.0, 0, -1.0), "watering"],
		["BLD_Market_A", Vector3(0.5, 0, 0.6), "talk"], ["BLD_Smithy_A", Vector3(0.6, 0, 3.4), "mine"]]
	for i in stations.size():
		var b := $Buildings.get_node_or_null(NodePath(stations[i][0])) as Node3D
		if b == null:
			continue
		var v := _spawn(20 + i)
		v.position = b.position + (stations[i][1] as Vector3).rotated(Vector3.UP, b.rotation.y)
		v.look_at(Vector3(b.position.x, 0, b.position.z), Vector3.UP, true)
		v.Play(stations[i][2], 1.0)


func _spawn(index: int) -> Node3D:
	var v: Node3D = load(FEMALE if index % 3 == 1 else MALE).instantiate()
	$Villagers.add_child(v)
	return v


func _process(delta: float) -> void:
	for w in _walkers:
		var curve: Curve3D = w["curve"]
		var length := curve.get_baked_length()
		w["offset"] += w["dir"] * w["speed"] * delta
		if w["offset"] > length or w["offset"] < 0.0:
			w["dir"] = -w["dir"]
			w["lane"] = -w["lane"]
			w["offset"] = clampf(w["offset"], 0.0, length)
		var here := curve.sample_baked(w["offset"])
		var ahead := curve.sample_baked(clampf(w["offset"] + w["dir"] * 0.5, 0.0, length))
		var fwd := Vector3(ahead.x - here.x, 0, ahead.z - here.z)
		if fwd.length_squared() < 1e-6:
			continue
		fwd = fwd.normalized()
		var side: Vector3 = fwd.cross(Vector3.UP) * float(w["lane"])
		(w["node"] as Node3D).position = Vector3(here.x + side.x, 0, here.z + side.z)
		(w["node"] as Node3D).rotation.y = atan2(fwd.x, fwd.z)
	if _perf_seconds > 0.0:
		_elapsed += delta
		if _elapsed > 3.0:
			_perf_frames.append(delta * 1000.0)
		if _elapsed > 3.0 + _perf_seconds:
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
			var hitches := []
			for i in _perf_frames.size():
				if _perf_frames[i] > 20.0:
					hitches.append("#%d:%.0fms" % [i, _perf_frames[i]])
			print("VILLAGE hitches>20ms=%d %s" % [hitches.size(), " ".join(hitches.slice(0, 20))])
			print("VILLAGE PERF fps=%.1f avg_ms=%.2f 1%%low_fps=%.1f worst_ms=%.1f prims=%d draws=%d grass_chunks=%d" % [
				1000.0 / avg, avg, 1000.0 / low, sorted[0], Performance.get_monitor(Performance.RENDER_TOTAL_PRIMITIVES_IN_FRAME),
				Performance.get_monitor(Performance.RENDER_TOTAL_DRAW_CALLS_IN_FRAME), $GrassCarpet._chunks.size()])
			get_tree().quit()


func _unhandled_key_input(e: InputEvent) -> void:
	if not (e is InputEventKey and e.pressed and not e.echo):
		return
	if e.keycode == KEY_Z:
		var keys := ZOOMS.keys()
		_zoom = keys[(keys.find(_zoom) + 1) % keys.size()]
		$CameraRig.FocusAt(FOCUS, ZOOMS[_zoom])
	elif e.keycode == KEY_H:
		$GrassCarpet.visible = not $GrassCarpet.visible
