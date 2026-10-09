extends "res://scenes/test/LookdevKenney.gd"
## Prototype villager gate (docs/characters_spec.md §10): the own Kenney-style villager next to a Fantasy Town house
## (one standing in the doorway for the scale check), a Nature Kit tree, and the Kenney Blocky reference at the same
## height — on the approved Kenney look-dev ground, seen with the official camera (CameraRig: pitch 50°, FOV 32°).
## Args after "--": --zoom=near|mid|far · --no-grass · --shot=PATH · --perf=N (N animated villagers, prints FPS, quits)

const PROTO := "res://assets/characters/kenney/CHR_Villager_Proto.glb"
const BLOCKY := "res://assets/vendor/kenney_blocky_characters/character-k.glb"
const BLOCKY_SCALE := 1.30 / 2.70          # Blocky is 2.7 u tall; scaled to the villager height (spec §1)
const DOOR := Vector3(-12.49, 0, -4.56)     # house 1 of the look-dev: 3×2 at (-16, 0, -8), 8°, door cell x = 1
const HOUSE_YAW := 8.0
const CAM_PITCH := 50.0
const CAM_FOV := 32.0

var _perf := -1
var _frames: PackedFloat64Array = []
var _draws: PackedInt64Array = []
var _clock := 0.0


func _ready() -> void:
	for a in OS.get_cmdline_user_args():
		if a.begins_with("--perf="): _perf = int(a.substr(7))
	if _perf >= 0:
		DisplayServer.window_set_vsync_mode(DisplayServer.VSYNC_DISABLED)
	super._ready()


func _figures() -> void:
	var door := _villager(DOOR + Vector3(0, 0, 0.05).rotated(Vector3.UP, deg_to_rad(HOUSE_YAW)), HOUSE_YAW, "idle")
	door.name = "InDoorway"
	_villager(Vector3(-14.9, 0, -2.6), 90.0, "walk")
	_place(NATURE % "tree_oak", Vector3(-7.7, 0, -3.0), 30.0, NATURE_SCALE)
	_villager(Vector3(-8.75, 0, -3.0), 90.0, "chop")
	var b := _place(BLOCKY, Vector3(-10.6, 0, -3.1), HOUSE_YAW, BLOCKY_SCALE)
	_play(b, "idle")
	if _perf > 0:
		_crowd(_perf)
	_report_material()


func _villager(pos: Vector3, yaw: float, anim: String) -> Node3D:
	var v := _place(PROTO, pos, yaw)
	_play(v, anim)
	return v


func _play(root: Node, anim: String, offset: float = 0.0) -> void:
	if root == null:
		return
	var ap := root.find_child("AnimationPlayer", true, false) as AnimationPlayer
	if ap == null or not ap.has_animation(anim):
		push_warning("no animation %s in %s" % [anim, root.name])
		return
	for n in ap.get_animation_list():
		ap.get_animation(n).loop_mode = Animation.LOOP_LINEAR
	ap.play(anim)
	ap.seek(offset, true)


## N villagers on the meadow in front of the houses, mixed clips and phases (deterministic).
func _crowd(n: int) -> void:
	var clips := ["idle", "walk", "chop"]
	var cols := 10
	for i in n:
		var pos := Vector3(-22.0 + (i % cols) * 2.2, 0, 1.5 + (i / cols) * 2.4)
		var v := _place(PROTO, pos, float((i * 47) % 360))
		_play(v, clips[i % 3], (i * 0.137) as float)


func _report_material() -> void:
	var v := get_node_or_null("InDoorway")
	if v == null:
		return
	for m in v.find_children("*", "MeshInstance3D", true, false):
		var mi := m as MeshInstance3D
		var mat := mi.mesh.surface_get_material(0) as BaseMaterial3D
		if mat:
			print("MATERIAL %s: %s filter=%d tex=%s" % [mi.name, mat.resource_name, mat.texture_filter,
				mat.albedo_texture.resource_path if mat.albedo_texture else "-"])
		break


func _camera() -> void:
	var cam := Camera3D.new()
	cam.fov = CAM_FOV
	add_child(cam)
	var dist: float = ZOOMS.get(_zoom, 62.0)
	var pitch := deg_to_rad(CAM_PITCH)
	var target := Vector3(-11.5, 0, -2.0) if _perf <= 0 else Vector3(-12.0, 0, 2.0)
	cam.position = target + Vector3(0, sin(pitch) * dist, cos(pitch) * dist)
	cam.look_at(target, Vector3.UP)
	cam.current = true


func _process(delta: float) -> void:
	if _perf < 0:
		return
	_clock += delta
	if _clock < 3.0:            # warm-up: shader compilation, grass streaming
		return
	_frames.append(delta)
	_draws.append(Performance.get_monitor(Performance.RENDER_TOTAL_DRAW_CALLS_IN_FRAME))
	if _clock < 11.0:
		return
	var sorted := _frames.duplicate()
	sorted.sort()
	var total := 0.0
	for d in _frames: total += d
	var avg_ms := total / _frames.size() * 1000.0
	var p99_ms: float = sorted[int(sorted.size() * 0.99)] * 1000.0
	var draws := 0
	for d in _draws: draws += d
	print("PERF villagers=%d frames=%d avg_fps=%.1f avg_ms=%.2f p99_ms=%.2f (1%%-low fps %.1f) max_ms=%.2f draw_calls=%d vsync=off size=%s gpu=%s" % [
		_perf, _frames.size(), 1000.0 / avg_ms, avg_ms, p99_ms, 1000.0 / p99_ms, sorted[-1] * 1000.0,
		draws / _draws.size(), get_viewport().get_visible_rect().size, RenderingServer.get_video_adapter_name()])
	get_tree().quit()
