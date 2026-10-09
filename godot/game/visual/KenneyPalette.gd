extends RefCounted
## Kenney Nature Kit 2.1 ships a turquoise/orange palette by design (materials are plain colours named by role).
## This maps each role to the approved cozy palette (meadow greens, warm earth, pale limestone like the Koastalia walls),
## so the kit sits next to the ground, the grass and the Fantasy Town houses. Surface overrides only: the vendor
## files are never touched. Fantasy Town / Survival use a colormap texture and need no remap.

const ROLES := {
	"grass": Color("8fbf45"), "dirt": Color("9c7650"), "dirtDark": Color("7a5a3c"),
	"stone": Color("d9d6ca"), "stoneDark": Color("aaa79b"),
	"leafsGreen": Color("5e9b34"), "leafsDark": Color("3f7a2b"), "leafsFall": Color("e0952c"),
	"woodBark": Color("7a5434"), "woodBarkDark": Color("5e3f28"), "wood": Color("a8743f"), "woodDark": Color("7a5230"),
	"woodInner": Color("e3c79a"), "woodBirch": Color("ece6d6"),
	"water": Color("7dd8d4"), "corn": Color("e6c35a"),
	"colorRed": Color("c4453c"), "colorRedDark": Color("9a3430"), "colorYellow": Color("f0c24a"),
	"colorPurple": Color("9d86d8"), "colorTan": Color("d9a46a"),
}

static var _cache := {}


static func material(role: String) -> Material:
	if not ROLES.has(role):
		return null
	if not _cache.has(role):
		var m := StandardMaterial3D.new()
		m.albedo_color = ROLES[role]
		m.roughness = 0.95
		_cache[role] = m
	return _cache[role]


## Recolours every Nature Kit surface under `root` by its material name.
static func apply(root: Node) -> void:
	var nodes: Array = root.find_children("*", "MeshInstance3D", true, false)
	if root is MeshInstance3D:
		nodes.append(root)
	for n: MeshInstance3D in nodes:
		if n.mesh == null:
			continue
		for i in n.mesh.get_surface_count():
			var src := n.mesh.surface_get_material(i)
			var m := material(src.resource_name if src else "")
			if m:
				n.set_surface_override_material(i, m)
