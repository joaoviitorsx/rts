extends Node3D
## Look-dev of the RTS opening buildings (briefing step 3): pile, campfire, covered depot, tent, on a 2-m grid.
## Args after "--": --shot=PATH

const CozyEnvironment := preload("res://game/visual/CozyEnvironment.gd")
const SCENES := ["BLD_Pile_A", "BLD_Campfire_A", "BLD_Depot_A", "BLD_Tent_A"]


func _ready() -> void:
	CozyEnvironment.build(self)
	var ground := MeshInstance3D.new()
	var plane := PlaneMesh.new()
	plane.size = Vector2(60, 60)
	var mat := StandardMaterial3D.new()
	mat.albedo_color = Color("7cb342")
	plane.material = mat
	ground.mesh = plane
	add_child(ground)
	for i in SCENES.size():
		var n := (load("res://assets/buildings/%s.tscn" % SCENES[i]) as PackedScene).instantiate() as Node3D
		n.position = Vector3(-9 + i * 6, 0, 0)
		add_child(n)
		var foot := MeshInstance3D.new()   # footprint outline: 1×1 cell for the campfire, 2×2 otherwise
		var q := PlaneMesh.new()
		q.size = Vector2.ONE * (2.0 if SCENES[i] == "BLD_Campfire_A" else 4.0)
		var m := StandardMaterial3D.new()
		m.albedo_color = Color(1, 1, 1, 0.25)
		m.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
		q.material = m
		foot.mesh = q
		foot.position = n.position + Vector3(0, 0.01, 0)
		add_child(foot)
	var cam := Camera3D.new()
	cam.fov = 32.0
	add_child(cam)
	cam.position = Vector3(0, 16, 17)
	cam.look_at(Vector3(0, 0.5, 0), Vector3.UP)
	cam.current = true
	for a in OS.get_cmdline_user_args():
		if a.begins_with("--shot="):
			var s = load("res://game/scripts/DevShot.cs").new()
			s.set("Path", a.substr(7))
			add_child(s)
