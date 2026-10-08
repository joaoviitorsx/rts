extends Node3D
## Dense grass carpet: short geometry blades in clumps, MultiMesh chunked for culling.
## Colour/height/LOD are decided in grass_carpet.gdshader from the shared ground mask, so blades vanish on
## paths, dirt and fields and their roots match the ground colour exactly.

const SHADER := preload("res://game/vegetation/grass_carpet.gdshader")
const GroundMask := preload("res://game/terrain/GroundMask.gd")

@export var spacing := 0.12             ## metres between clumps (jittered grid)
@export var chunk_size := 8.0
@export var blades_per_clump := 6
@export var blade_height := Vector2(0.16, 0.32)
@export var blade_width := 0.045
@export var fade_end := 55.0

var instances := 0


func build(mask: GroundMask, area: Rect2, seed_value: int = 1) -> int:
	for c in get_children():
		c.queue_free()
	var rng := RandomNumberGenerator.new()
	rng.seed = seed_value
	var mesh := _clump_mesh(rng)
	var material := ShaderMaterial.new()
	material.shader = SHADER
	material.set_shader_parameter("fade_end", fade_end)
	material.set_shader_parameter("fade_start", fade_end * 0.6)
	var chunks := {}
	instances = 0
	var y := area.position.y
	while y < area.end.y:
		var x := area.position.x
		while x < area.end.x:
			var p := Vector3(x + rng.randf_range(-0.45, 0.45) * spacing, 0, y + rng.randf_range(-0.45, 0.45) * spacing)
			var m := mask.sample(p)
			if maxf(maxf(m.r, m.g), m.b) < 0.8:   # the shader shrinks the rest smoothly
				var s := rng.randf_range(0.8, 1.2)
				var t := Transform3D(Basis(Vector3.UP, rng.randf() * TAU).scaled(Vector3(s, rng.randf_range(0.8, 1.25) * s, s)), p)
				var key := Vector2i(floori(p.x / chunk_size), floori(p.z / chunk_size))
				if not chunks.has(key):
					chunks[key] = []
				chunks[key].append(t)
				instances += 1
			x += spacing
		y += spacing
	for key in chunks:
		var list: Array = chunks[key]
		var mm := MultiMesh.new()
		mm.transform_format = MultiMesh.TRANSFORM_3D
		mm.mesh = mesh
		mm.instance_count = list.size()
		for i in list.size():
			mm.set_instance_transform(i, list[i])
		var node := MultiMeshInstance3D.new()
		node.name = "Grass_%d_%d" % [key.x, key.y]
		node.multimesh = mm
		node.material_override = material
		node.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
		node.visibility_range_end = fade_end + 5.0
		add_child(node)
	return instances


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
		var m0 := off - side * 0.6 + lean * 0.35 + Vector3.UP * hgt * 0.55
		var m1 := off + side * 0.6 + lean * 0.35 + Vector3.UP * hgt * 0.55
		var tip := off + lean + Vector3.UP * hgt
		for tri in [[b0, b1, m1], [b0, m1, m0], [m0, m1, tip]]:
			for v: Vector3 in tri:
				st.set_normal(Vector3.UP)
				st.set_uv(Vector2(0.5, (v.y - off.y) / hgt))
				st.add_vertex(v)
	return st.commit()
