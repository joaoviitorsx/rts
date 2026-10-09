extends Node
## Bakes the distant-forest tree impostors: 8 yaw views per tree at the official camera pitch (50°), orthographic,
## into a 4×2 atlas pair per tree (albedo + world normal/palette param), plus impostors.json with the quad sizes.
## Needs a real GPU render (not --headless):
##   godot-mono --path godot res://scenes/tools/ImpostorBaker.tscn && godot-mono --headless --path godot --import
## Output is derived from vendor meshes, so it is not versioned (only the *.import presets are); rebake after
## changing tree scenes, tree materials or the bake/impostor shaders. scripts/setup_vendor.py --derive runs it.

const Clusters := preload("res://game/vegetation/Clusters.gd")
const PlantPalette := preload("res://game/vegetation/PlantPalette.gd")
const BAKE_SHADER := preload("res://game/vegetation/impostor_bake.gdshader")
const OUT := "res://assets/environment/impostors"
const COLS := 4
const ROWS := 2
const CELL_PX := 256
const PITCH_DEG := 50.0   # CameraRig.PitchDegrees
const PAD := 1.04
const TREES := ["ENV_Oak_A", "ENV_Oak_B", "ENV_Oak_C", "ENV_Oak_D", "ENV_Oak_E", "ENV_Oak_Big_A",
	"ENV_Pine_A", "ENV_Pine_B", "ENV_Pine_C", "ENV_Pine_D", "ENV_Pine_E", "ENV_Pine_Big_A",
	"ENV_TwistedTree_A", "ENV_TwistedTree_B"]
## Texture import: VRAM compressed with mipmaps; never treated as a normal map (alpha carries data).
const IMPORT_PRESET := '[remap]\n\nimporter="texture"\ntype="CompressedTexture2D"\n\n[params]\n\ncompress/mode=2\n' + \
	'compress/normal_map=2\nmipmaps/generate=true\nprocess/fix_alpha_border=true\nprocess/size_limit=0\ndetect_3d/compress_to=0\n'

var _vp: SubViewport
var _cam: Camera3D


func _ready() -> void:
	_vp = SubViewport.new()
	_vp.size = Vector2i(COLS * CELL_PX, ROWS * CELL_PX)
	_vp.transparent_bg = true
	_vp.own_world_3d = true
	_vp.render_target_update_mode = SubViewport.UPDATE_DISABLED
	add_child(_vp)
	_cam = Camera3D.new()
	_cam.projection = Camera3D.PROJECTION_ORTHOGONAL
	_cam.near = 0.1
	_cam.far = 400.0
	var env := Environment.new()   # neutral: no tonemap curve, fog, glow or adjustments
	env.background_mode = Environment.BG_CLEAR_COLOR
	env.tonemap_mode = Environment.TONE_MAPPER_LINEAR
	_cam.environment = env
	_cam.basis = Basis(Vector3.RIGHT, -deg_to_rad(PITCH_DEG))
	_vp.add_child(_cam)
	DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(OUT))

	var meta := {"cols": COLS, "rows": ROWS, "pitch_deg": PITCH_DEG, "trees": {}}
	for id in TREES:
		var path := "res://assets/environment/%s.tscn" % id
		if not ResourceLoader.exists(path):
			print("IMPOSTOR skip %s (missing)" % id)
			continue
		var t0 := Time.get_ticks_msec()
		meta["trees"][path] = await _bake(id, path)
		print("IMPOSTOR %s cell=%.2f ms=%d" % [id, meta["trees"][path]["cell"], Time.get_ticks_msec() - t0])
	var f := FileAccess.open(OUT + "/impostors.json", FileAccess.WRITE)
	f.store_string(JSON.stringify(meta, "\t"))
	f.close()
	print("IMPOSTOR done: %d trees" % meta["trees"].size())
	get_tree().quit()


func _bake(id: String, path: String) -> Dictionary:
	var src := Clusters.first_mesh(path)
	var mesh: Mesh = src["mesh"]
	var xform: Transform3D = src["xform"]
	if not xform.basis.orthonormalized().is_equal_approx(Basis()):
		push_warning("IMPOSTOR %s: mesh node is rotated; frames will be offset" % id)

	# Bounds in mesh-local units: radius around the trunk axis and height range (exact, from vertices).
	var r := 0.0
	var y0 := INF
	var y1 := -INF
	for s in mesh.get_surface_count():
		for v: Vector3 in mesh.surface_get_arrays(s)[Mesh.ARRAY_VERTEX]:
			r = maxf(r, Vector2(v.x, v.z).length())
			y0 = minf(y0, v.y)
			y1 = maxf(y1, v.y)
	var p := deg_to_rad(PITCH_DEG)
	var cy := (y0 + y1) * 0.5
	var cell := 2.0 * maxf(r, (y1 - y0) * 0.5 * cos(p) + r * sin(p)) * PAD

	# Bake materials (one per surface), shared by the 8 copies so a pass switch is one parameter per surface.
	var mats: Array[ShaderMaterial] = []
	var lightness := 1.0
	for i in mesh.get_surface_count():
		var orig := mesh.surface_get_material(i)
		var leaf := PlantPalette.material_for(orig, 0, true) as ShaderMaterial
		var m := ShaderMaterial.new()
		m.shader = BAKE_SHADER
		if leaf:
			m.set_shader_parameter("is_leaf", true)
			for k in ["albedo_texture", "lightness", "crown_height"]:
				m.set_shader_parameter(k, leaf.get_shader_parameter(k))
			lightness = leaf.get_shader_parameter("lightness")
		elif orig is BaseMaterial3D:
			var b := orig as BaseMaterial3D
			m.set_shader_parameter("albedo_texture", b.albedo_texture)
			m.set_shader_parameter("albedo_color", b.albedo_color)
			m.set_shader_parameter("alpha_scissor",
				b.alpha_scissor_threshold if b.transparency == BaseMaterial3D.TRANSPARENCY_ALPHA_SCISSOR else 0.0)
		mats.append(m)

	var copies: Array[MeshInstance3D] = []
	for k in COLS * ROWS:
		var mi := MeshInstance3D.new()
		mi.mesh = mesh
		for i in mats.size():
			mi.set_surface_override_material(i, mats[i])
		_vp.add_child(mi)
		copies.append(mi)
	# Leaf cards are mostly transparent, so vertex bounds overshoot: render once, then reframe on the real
	# coverage (largest half-extent from the cell centre over all frames, +3 px for filtering).
	_place(copies, cell, cy)
	var half := _coverage_half_extent(await _render(mats, 0))
	cell *= (half + 3.0) / (CELL_PX * 0.5)
	_place(copies, cell, cy)
	var images: Array[Image] = []
	for pass_id in 3:
		images.append(await _render(mats, pass_id))
	for c in copies:
		c.queue_free()

	# Normal atlas alpha = palette param where covered, 0 elsewhere.
	var albedo := images[0]
	var normal := images[1]
	var param := images[2].get_data()
	var data := normal.get_data()
	for i in range(3, data.size(), 4):
		data[i] = param[i - 3] if data[i] > 127 else 0
	normal.set_data(normal.get_width(), normal.get_height(), false, Image.FORMAT_RGBA8, data)

	var out := {"albedo": "%s/%s_albedo.png" % [OUT, id], "normal": "%s/%s_normal.png" % [OUT, id],
		"cell": cell, "center_y": cy, "lightness": lightness}
	for key in ["albedo", "normal"]:
		(albedo if key == "albedo" else normal).save_png(out[key])
		if not FileAccess.file_exists(out[key] + ".import"):   # versioned; Godot adds the uid on first import
			var imp := FileAccess.open(out[key] + ".import", FileAccess.WRITE)
			imp.store_string(IMPORT_PRESET)
			imp.close()
	return out


## Frame k = tree yawed by k·45°, laid out row-major; ortho projection, so copies never see each other.
func _place(copies: Array[MeshInstance3D], cell: float, cy: float) -> void:
	for k in copies.size():
		var screen := _cam.basis.x * ((k % COLS) - (COLS - 1) * 0.5) * cell \
			+ _cam.basis.y * ((ROWS - 1) * 0.5 - (k / COLS)) * cell
		copies[k].transform = Transform3D(Basis(Vector3.UP, k * TAU / copies.size()), screen - Vector3(0, cy, 0))
	_cam.size = ROWS * cell
	_cam.position = _cam.basis.z * 150.0


func _render(mats: Array[ShaderMaterial], pass_id: int) -> Image:
	for m in mats:
		m.set_shader_parameter("bake_pass", pass_id)
	_vp.render_target_update_mode = SubViewport.UPDATE_ONCE
	await RenderingServer.frame_post_draw
	await RenderingServer.frame_post_draw
	var img := _vp.get_texture().get_image()
	img.convert(Image.FORMAT_RGBA8)
	return img


## Largest |dx|/|dy| (px) of any covered pixel from its cell centre.
func _coverage_half_extent(img: Image) -> float:
	var data := img.get_data()
	var w := img.get_width()
	var half := 1.0
	for i in range(3, data.size(), 4):
		if data[i] > 127:
			var x := (i >> 2) % w
			var y := (i >> 2) / w
			half = maxf(half, maxf(absf(x % CELL_PX + 0.5 - CELL_PX * 0.5), absf(y % CELL_PX + 0.5 - CELL_PX * 0.5)))
	return half
