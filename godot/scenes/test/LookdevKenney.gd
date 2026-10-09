extends Node3D
## Look-dev of the Kenney art direction (decision 09/10/2026, waiting for approval): 3 houses assembled from the
## Fantasy Town Kit, fences, 10 trees and rocks from the Nature Kit, a campfire and tents (Nature + Survival kits),
## Survival-kit resource piles and anvil, a short modular cliff and wheat, one Quaternius villager and one Quaternius
## deer — on the approved ground (VillageTerrain + baked mask + grass carpet + cozy light), seen with the game camera.
## Dev scene: it loads the vendor copies directly (the game will only use asset scenes once the swap is approved).
## Args after "--": --zoom=near|mid|far · --no-grass · --shot=PATH

const GroundMask := preload("res://game/terrain/GroundMask.gd")
const GroundColorBaker := preload("res://game/terrain/GroundColorBaker.gd")
const CozyEnvironment := preload("res://game/visual/CozyEnvironment.gd")
const VillageTerrain := preload("res://game/terrain/VillageTerrain.gd")
const GrassCarpet := preload("res://game/vegetation/GrassCarpet.gd")
const KenneyPalette := preload("res://game/visual/KenneyPalette.gd")

const TOWN := "res://assets/vendor/kenney_fantasy_town_kit/%s.glb"
const NATURE := "res://assets/vendor/kenney_nature_kit/%s.glb"
const SURVIVAL := "res://assets/vendor/kenney_survival_kit/%s.glb"
const ANIMALS := "res://assets/vendor/quaternius_ultimate_animals/%s.gltf"
const VILLAGER := "res://assets/characters/CHR_Villager_Base.tscn"

# Pack scales to metres (measured bounding boxes): town module 1 u → 2 m (one sim cell), nature/survival props ×4,
# Quaternius animals ~4.3 u tall → a 1.4 m deer.
const TOWN_SCALE := 2.0
const NATURE_SCALE := 4.0
const ANIMAL_SCALE := 0.33
const ZOOMS := {"near": 30.0, "mid": 62.0, "far": 110.0}   # mid ≈ the Koastalia reference framing

var _zoom := "mid"


func _ready() -> void:
	var shot := ""
	for a in OS.get_cmdline_user_args():
		if a.begins_with("--zoom="): _zoom = a.substr(7)
		if a.begins_with("--shot="): shot = a.substr(7)
	CozyEnvironment.build(self)
	_ground(not OS.get_cmdline_user_args().has("--no-grass"))
	_houses()
	_fences()
	_nature()
	_camp()
	_figures()
	_camera()
	if shot != "":
		var s = load("res://game/scripts/DevShot.cs").new()
		s.set("Path", shot)
		add_child(s)


func _place(path: String, pos: Vector3, rot_deg: float = 0.0, scale: float = 1.0) -> Node3D:
	var scene := load(path) as PackedScene
	if scene == null:
		push_warning("missing " + path)
		return null
	var n := scene.instantiate() as Node3D
	n.position = pos
	n.rotation_degrees = Vector3(0, rot_deg, 0)
	n.scale = Vector3.ONE * scale
	add_child(n)
	if path.contains("kenney_nature_kit"):
		KenneyPalette.apply(n)
	return n


# ------------------------------------------------------------------------------------------------ ground

func _ground(grass: bool) -> void:
	var terrain := VillageTerrain.new()
	terrain.name = "Ground"
	terrain.cover = Rect2(-80, -80, 160, 160)
	add_child(terrain)
	terrain.apply_palette("meadow")
	var mask := GroundMask.new(Rect2(-60, -60, 120, 120), 3)
	mask.paint_polyline(PackedVector2Array([Vector2(-60, 6), Vector2(-10, 4), Vector2(12, 8), Vector2(60, 2)]), 3.0, GroundMask.PATH)
	mask.paint_polyline(PackedVector2Array([Vector2(0, 5), Vector2(-2, -8)]), 2.0, GroundMask.PATH)
	for c in [Vector2(-12, -3), Vector2(6, -6), Vector2(-3, -14)]:
		mask.paint_blob(c, Vector2(6, 4), GroundMask.DIRT, 0.8)
	mask.paint_rect(Vector2(22, -10), Vector2(12, 10), 0.0, GroundMask.PLOWED, 1.0, 0.3)
	mask.blur(GroundMask.PATH, 0.9)
	mask.blur(GroundMask.DIRT, 0.4)
	mask.shade_path_margins(0.45)
	mask.publish()
	var baker := GroundColorBaker.new()
	add_child(baker)
	baker.bake(Vector2i(mask.w, mask.h))
	if grass:
		var carpet := GrassCarpet.new()
		carpet.name = "Grass"
		add_child(carpet)
		carpet.stream(mask, Rect2(-60, -60, 120, 120), self, 7)


# ------------------------------------------------------------------------------------------------ houses

## A w×d house from Fantasy Town modules (1 module = 1 sim cell = 2 m): walls on the perimeter (door and shutters on
## the front), gable roof along x.
func _house(origin: Vector3, w: int, d: int, wood: bool, rot_deg: float) -> void:
	var house := Node3D.new()
	house.position = origin
	house.rotation_degrees = Vector3(0, rot_deg, 0)
	add_child(house)
	var plain := "wall-wood" if wood else "wall"
	var window := "wall-wood-window-shutters" if wood else "wall-window-shutters"
	var door := "wall-wood-door" if wood else "wall-door"
	for x in w:
		for z in d:
			var cell := Vector3(x + 0.5, 0, z + 0.5)
			# The kit's wall sits on the +X side of its tile: rotate it to each outside face.
			if z == d - 1:   # front (+Z)
				var front := door if x == w / 2 else window
				_module(house, front, cell, -90.0)
			if z == 0:
				_module(house, window if x % 2 == 0 else plain, cell, 90.0)
			if x == w - 1:
				_module(house, plain, cell, 0.0)
			if x == 0:
				_module(house, plain, cell, 180.0)
			# Roof: one slope per row facing out, ridge along x.
			if d == 2:
				_module(house, "roof", cell + Vector3(0, 1, 0), 90.0 if z == 1 else -90.0)
	if d == 2:
		_module(house, "roof-gable-end", Vector3(-0.0, 1, 1.0), 180.0)
		_module(house, "roof-gable-end", Vector3(w, 1, 1.0), 0.0)
	_module(house, "chimney", Vector3(w - 0.5, 1, 0.5), 0.0)


func _module(parent: Node3D, name: String, local_cell: Vector3, rot_deg: float) -> void:
	var scene := load(TOWN % name) as PackedScene
	if scene == null:
		push_warning("missing module " + name)
		return
	var n := scene.instantiate() as Node3D
	n.position = local_cell * TOWN_SCALE
	n.rotation_degrees = Vector3(0, rot_deg, 0)
	n.scale = Vector3.ONE * TOWN_SCALE
	parent.add_child(n)


func _houses() -> void:
	_house(Vector3(-16, 0, -8), 3, 2, false, 8.0)
	_house(Vector3(2, 0, -12), 2, 2, true, -6.0)
	_house(Vector3(-8, 0, -22), 3, 2, true, 3.0)


func _fences() -> void:
	# Yard of the first house + a short run along the path.
	for i in 6:
		_place(TOWN % "fence", Vector3(-17 + i * 2.0, 0, -1.2), 90.0, TOWN_SCALE)
	for i in 3:
		_place(TOWN % "fence", Vector3(-18.0, 0, -6.5 + i * 2.0), 0.0, TOWN_SCALE)
	_place(TOWN % "fence-gate", Vector3(-5.2, 0, -1.2), 90.0, TOWN_SCALE)


# ------------------------------------------------------------------------------------------------ nature, camp

func _nature() -> void:
	var trees := [
		["tree_default", Vector3(-26, 0, -14)], ["tree_oak", Vector3(-22, 0, -22)], ["tree_oak_fall", Vector3(-18, 0, -26)],
		["tree_fat", Vector3(12, 0, -22)], ["tree_detailed_fall", Vector3(20, 0, -18)], ["tree_pineTallA", Vector3(24, 0, -22)],
		["tree_simple", Vector3(-34, 0, 2)], ["tree_default_fall", Vector3(30, 0, 12)], ["tree_tall", Vector3(-28, 0, 14)],
		["tree_blocks_fall", Vector3(10, 0, 18)],
	]
	for t in trees:
		_place(NATURE % t[0], t[1], randf_range(0, 360), NATURE_SCALE * randf_range(0.9, 1.15))
	for r in [["rock_largeA", Vector3(-22, 0, 10), 3.5], ["rock_largeC", Vector3(18, 0, 4), 4.0], ["stone_tallB", Vector3(-6, 0, 16), 3.0],
			["rock_smallB", Vector3(4, 0, 12), 4.0], ["rock_smallFlatA", Vector3(-14, 0, 12), 4.0]]:
		_place(NATURE % r[0], r[1], randf_range(0, 360), r[2])
	# A short terrace edge from the modular cliff pieces (1 u = 2 m) and a wheat patch.
	for i in 6:
		_place(NATURE % ("cliff_block_rock" if i % 3 else "cliff_large_rock"), Vector3(-34 + i * 2.0, -2.0, 4), 0.0, 2.0)
	for x in 5:
		for z in 4:
			_place(NATURE % ("crops_wheatStageB" if (x + z) % 3 else "crops_wheatStageA"), Vector3(17 + x * 2.2, 0, -14 + z * 2.2), 0.0, 3.2)


func _camp() -> void:
	_place(NATURE % "campfire_stones", Vector3(-2, 0, 12), 0.0, NATURE_SCALE)
	_place(NATURE % "tent_detailedOpen", Vector3(-9, 0, 11), 30.0, NATURE_SCALE)
	_place(NATURE % "tent_smallClosed", Vector3(5, 0, 15), -40.0, NATURE_SCALE)
	_place(SURVIVAL % "tent", Vector3(-6, 0, 18), 10.0, 4.5)
	_place(SURVIVAL % "resource-wood", Vector3(3, 0, 9), 20.0, 5.0)
	_place(SURVIVAL % "resource-stone-large", Vector3(7, 0, 10), 0.0, 4.0)
	_place(NATURE % "log_stack", Vector3(0, 0, 17), 70.0, NATURE_SCALE)
	_place(SURVIVAL % "workbench-anvil", Vector3(-12, 0, 16), 0.0, 4.0)


func _figures() -> void:
	var v := _place(VILLAGER, Vector3(-1, 0, 14), 200.0)
	if v and v.has_method("Play"):
		v.call("Play", "idle", 1.0)
	var deer := _place(ANIMALS % "Deer", Vector3(16, 0, 16), -60.0, ANIMAL_SCALE)
	if deer:
		var ap := deer.find_child("AnimationPlayer", true, false) as AnimationPlayer
		if ap and ap.has_animation("Eating"):
			ap.play("Eating")


func _camera() -> void:
	# Same framing as the game camera (CameraRig: pitch 50°, FOV 32), looking at the village.
	var cam := Camera3D.new()
	cam.fov = 32.0
	add_child(cam)
	var dist: float = ZOOMS.get(_zoom, 45.0)
	var pitch := deg_to_rad(47.0)
	var target := Vector3(-2, 0, 12) if _zoom == "near" else Vector3(0, 0, 2)   # near: the camp
	cam.position = target + Vector3(0, sin(pitch) * dist, cos(pitch) * dist)
	cam.look_at(target, Vector3.UP)
	cam.current = true
