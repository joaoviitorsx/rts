extends SceneTree
## Headless check that the Terrain3D GDExtension loads in this Godot version.
## godot-mono --headless --path godot --script res://tools/editor/check_terrain3d.gd
func _init() -> void:
	var ok := ClassDB.class_exists("Terrain3D")
	print("TERRAIN3D_CLASS ", ok)
	if ok:
		var t = ClassDB.instantiate("Terrain3D")
		print("TERRAIN3D_VERSION ", t.get_version() if t.has_method("get_version") else "?")
		t.free()
	quit(0 if ok else 1)
