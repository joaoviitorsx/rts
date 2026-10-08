@tool
extends Node3D
## Village ground: Terrain3D for relief + painted layers, shaded by our own cozy shader
## (game/terrain/cozy_ground.gdshader): palette colours + low-frequency noise, no repeating texture.
## Layers (control base id): grass (0), dirt (1), path (2), plowed field (3).
## Small Stylized Nature tufts/flowers/pebbles are scattered on top with MultiMesh, thinner near paths/buildings.
## GDScript on purpose: Terrain3D is a GDExtension whose API is far simpler to drive from GDScript.

enum Tex { GRASS, DIRT, PATH, PLOWED }

const SHADER := preload("res://game/terrain/cozy_ground.gdshader")
const PlantPalette := preload("res://game/vegetation/PlantPalette.gd")

## Ground colour variations, applied as GLOBAL shader uniforms (ground + grass + plants share them).
## Bible §8: moss #5E7045, forest #354B35, warm earth #78553B.
const PALETTES := {
	"moss": {   # V1 — Bible palette as-is
		"ground_base": Color("5e7045"), "ground_light": Color("86a04f"), "ground_dark": Color("354b35"),
		"ground_dirt": Color("78553b"), "ground_path": Color("a58b66"), "ground_plowed": Color("5e4230"),
		"veg_tip": Color("a9c060"), "veg_shadow": Color("354b35"),
	},
	"meadow": { # V2 — brighter, warmer meadow (closer to the Koastalia reference)
		"ground_base": Color("7a9a3e"), "ground_light": Color("a4bf55"), "ground_dark": Color("4a6a34"),
		"ground_dirt": Color("8a6444"), "ground_path": Color("c2a47a"), "ground_plowed": Color("6e4b33"),
		"veg_tip": Color("c8d66a"), "veg_shadow": Color("3f5a2e"),
	},
}

@export var origin := Vector3.ZERO           ## world position of the first region corner
@export var regions := Vector2i(1, 1)        ## number of 256 m regions on X/Z
@export var texture_paths: Dictionary = {"grass": "", "dirt": "", "path": ""}
@export var uv_scale := 0.08
@export var palette := "moss"
@export var detail_texture_path := ""   ## optional watercolor detail for dirt/path (CC0), subtle

var terrain: Node3D  # Terrain3D (typed loosely so the script parses even without the extension)


func _ready() -> void:
	if not Engine.is_editor_hint():
		build()


func is_available() -> bool:
	return ClassDB.class_exists("Terrain3D")


func build() -> bool:
	if not is_available():
		push_warning("Terrain3D not loaded: VillageTerrain falls back to a flat plane")
		_build_fallback()
		return false
	if terrain:
		terrain.queue_free()
	terrain = ClassDB.instantiate("Terrain3D")
	terrain.name = "Terrain3D"
	add_child(terrain)
	terrain.set_collision_mode(0)
	terrain.material.world_background = 0   # NONE
	terrain.material.auto_shader = false
	terrain.material.enable_shader_override(true)
	terrain.material.set_shader_override(SHADER)
	apply_palette(palette)
	if detail_texture_path != "" and ResourceLoader.exists(detail_texture_path):
		terrain.material.set_shader_param("detail_texture", load(detail_texture_path))
		terrain.material.set_shader_param("detail_strength", 0.25)
	terrain.assets = ClassDB.instantiate("Terrain3DAssets")
	var colors := {
		"grass": [Color(0.42, 0.62, 0.30), Color(0.55, 0.72, 0.38)],
		"dirt": [Color(0.52, 0.40, 0.27), Color(0.62, 0.49, 0.33)],
		"path": [Color(0.66, 0.58, 0.44), Color(0.76, 0.69, 0.55)],
	}
	var id := 0
	for key in ["grass", "dirt", "path"]:
		terrain.assets.set_texture(id, _texture_asset(key, colors[key]))
		id += 1
	for x in regions.x:
		for z in regions.y:
			terrain.data.add_region_blankp(origin + Vector3(x * 256 + 1, 0, z * 256 + 1))
	return true


## Paints texture `tex` on every terrain vertex inside the axis-aligned rectangle.
func paint_rect(center: Vector3, size: Vector2, tex: int) -> void:
	if not terrain:
		return
	var half := size * 0.5
	for x in range(int(floor(center.x - half.x)), int(ceil(center.x + half.x)) + 1):
		for z in range(int(floor(center.z - half.y)), int(ceil(center.z + half.y)) + 1):
			terrain.data.set_control_base_id(Vector3(x, 0, z), tex)


## Paints a road along a polyline with the given width.
func paint_path(points: PackedVector3Array, width: float, tex: int = Tex.PATH) -> void:
	if not terrain or points.size() < 2:
		return
	for i in points.size() - 1:
		var a := points[i]
		var b := points[i + 1]
		var length := a.distance_to(b)
		var steps := int(ceil(length))
		for s in steps + 1:
			var p := a.lerp(b, float(s) / max(steps, 1))
			for dx in range(-int(ceil(width)), int(ceil(width)) + 1):
				for dz in range(-int(ceil(width)), int(ceil(width)) + 1):
					if Vector2(dx, dz).length() <= width * 0.5:
						terrain.data.set_control_base_id(Vector3(round(p.x) + dx, 0, round(p.z) + dz), tex)


func apply_palette(name: String) -> void:
	palette = name
	if not PALETTES.has(name):
		return
	for key in PALETTES[name]:
		RenderingServer.global_shader_parameter_set(key, PALETTES[name][key])


func layer_at(world: Vector3) -> int:
	return terrain.data.get_control_base_id(world) if terrain else Tex.GRASS


## Scatters small decoration (MultiMesh, chunked in CHUNK m tiles for culling and distance fade).
## Spec keys:
##   scene   res:// path of an asset scene (its first mesh + our normalization scale are used)
##   count   instances to place
##   mode    "uniform" | "cluster" (patches) | "edge" (near paths/dirt, not on them) | "around" (near points)
##   clusters, radius   for "cluster"; points (Array[Vector3]), radius for "around"
##   scale   [min, max] random scale; fade (m) distance where it disappears (default 120)
func scatter(specs: Array, area: Rect2, seed_value: int = 1) -> int:
	var rng := RandomNumberGenerator.new()
	rng.seed = seed_value
	var placed := 0
	for spec: Dictionary in specs:
		var source := _first_mesh(load(spec["scene"]))
		if source.is_empty():
			continue
		source["mesh"] = PlantPalette.convert_mesh(source["mesh"], spec.get("flower_slot", 0))
		var mode: String = spec.get("mode", "uniform")
		var count: int = spec["count"]
		var radius: float = spec.get("radius", 4.0)
		var scale_range: Array = spec.get("scale", [0.8, 1.25])
		var centers: Array[Vector3] = []
		if mode == "cluster":
			for c in spec.get("clusters", 20):
				for attempt in 30:
					var cp := Vector3(rng.randf_range(area.position.x, area.end.x), 0, rng.randf_range(area.position.y, area.end.y))
					if _density(cp) > 0.6:
						centers.append(cp)
						break
		elif mode == "around":
			for v in spec.get("points", []):
				centers.append(v)
		var chunks := {}
		var made := 0
		var tries := 0
		while made < count and tries < count * 12:
			tries += 1
			var pos: Vector3
			if centers.is_empty():
				pos = Vector3(rng.randf_range(area.position.x, area.end.x), 0, rng.randf_range(area.position.y, area.end.y))
			else:
				var c: Vector3 = centers[rng.randi() % centers.size()]
				var r := absf(rng.randfn(0.0, radius * 0.5))
				var ang := rng.randf() * TAU
				pos = c + Vector3(cos(ang) * r, 0, sin(ang) * r)
			if not area.has_point(Vector2(pos.x, pos.z)):
				continue
			var d := _density(pos)
			var accept := d
			if mode == "edge":
				accept = 1.0 if layer_at(pos) == Tex.GRASS and d < 0.95 else 0.0
			if rng.randf() > accept:
				continue
			var s := rng.randf_range(scale_range[0], scale_range[1])
			var basis := Basis(Vector3.UP, rng.randf() * TAU).scaled(Vector3.ONE * s)
			var key := Vector2i(floori(pos.x / CHUNK), floori(pos.z / CHUNK))
			if not chunks.has(key):
				chunks[key] = []
			chunks[key].append(Transform3D(basis, pos) * source["xform"])
			made += 1
		for key in chunks:
			_add_multimesh(source["mesh"], chunks[key], "%s_%d_%d" % [String(spec["scene"]).get_file().get_basename(), key.x, key.y],
				spec.get("fade", 120.0))
		placed += made
	return placed


const CHUNK := 32.0


func _add_multimesh(mesh: Mesh, xforms: Array, node_name: String, fade: float) -> void:
	var mm := MultiMesh.new()
	mm.transform_format = MultiMesh.TRANSFORM_3D
	mm.mesh = mesh
	mm.instance_count = xforms.size()
	for i in xforms.size():
		mm.set_instance_transform(i, xforms[i])
	var node := MultiMeshInstance3D.new()
	node.name = "Scatter_" + node_name
	node.multimesh = mm
	node.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	node.visibility_range_end = fade
	node.visibility_range_end_margin = 15.0
	node.visibility_range_fade_mode = GeometryInstance3D.VISIBILITY_RANGE_FADE_SELF
	add_child(node)


func _density(p: Vector3) -> float:
	if not terrain:
		return 1.0
	if layer_at(p) != Tex.GRASS:
		return 0.0
	var near := 0
	for r: float in [1.5, 3.5]:
		for a in 8:
			var o: Vector3 = Vector3(cos(a * TAU / 8.0), 0, sin(a * TAU / 8.0)) * r
			if layer_at(p + o) != Tex.GRASS:
				near += 2 if r < 2.0 else 1
	return clampf(1.0 - near * 0.12, 0.0, 1.0)


## First mesh of a scene and its transform relative to the scene root (keeps our normalization scale).
func _first_mesh(scene: PackedScene) -> Dictionary:
	var root := scene.instantiate()
	var found := {}
	for n in root.find_children("*", "MeshInstance3D", true, false):
		var xform := Transform3D()
		var node: Node = n
		while node != root:
			xform = (node as Node3D).transform * xform
			node = node.get_parent()
		found = {"mesh": (n as MeshInstance3D).mesh, "xform": xform}
		break
	root.free()
	return found


func commit() -> void:
	if terrain:
		terrain.data.update_maps(3, true, false)   # TYPE_MAX: all maps


func _texture_asset(key: String, gradient_colors: Array):
	var asset = ClassDB.instantiate("Terrain3DTextureAsset")
	asset.name = key
	asset.uv_scale = uv_scale
	var path: String = texture_paths.get(key, "")
	if path != "" and ResourceLoader.exists(path):
		asset.albedo_texture = load(path)
	else:
		asset.albedo_texture = _placeholder_albedo(gradient_colors[0], gradient_colors[1], key.hash())
	return asset


## Placeholder: seamless noise in two tones (alpha = height for Terrain3D blending).
func _placeholder_albedo(a: Color, b: Color, seed_value: int) -> Texture2D:
	var noise := FastNoiseLite.new()
	noise.seed = seed_value
	noise.frequency = 0.02
	var size := 256
	var img := Image.create_empty(size, size, true, Image.FORMAT_RGBA8)
	for x in size:
		for y in size:
			# 4D-free seamless trick: sample a torus-wrapped 2D domain via blended corners
			var u := float(x) / size
			var v := float(y) / size
			var n := (noise.get_noise_2d(x, y) * (1.0 - u) * (1.0 - v)
				+ noise.get_noise_2d(x - size, y) * u * (1.0 - v)
				+ noise.get_noise_2d(x, y - size) * (1.0 - u) * v
				+ noise.get_noise_2d(x - size, y - size) * u * v)
			var t := clampf(n * 0.5 + 0.5, 0.0, 1.0)
			var c := a.lerp(b, t)
			c.a = t
			img.set_pixel(x, y, c)
	img.generate_mipmaps()
	return ImageTexture.create_from_image(img)


func _build_fallback() -> void:
	var plane := MeshInstance3D.new()
	var mesh := PlaneMesh.new()
	mesh.size = Vector2(regions.x * 256, regions.y * 256)
	plane.mesh = mesh
	plane.position = origin + Vector3(mesh.size.x / 2, 0, mesh.size.y / 2)
	add_child(plane)
