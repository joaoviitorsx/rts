extends RefCounted
## Merges every static (non-skinned) MeshInstance3D under `root` into ONE MeshInstance3D with one surface per
## material. A modular building drops from ~30 pieces × 2–3 surfaces to ~3–5 surfaces (far fewer draw calls,
## also in shadow passes). Call after material overrides are applied (it keeps the active material).


static func merge(root: Node3D, keep_names: Array = []) -> int:
	var groups := {}            # Material -> SurfaceTool
	var sources: Array = []
	var inv := root.global_transform.affine_inverse() if root.is_inside_tree() else Transform3D()
	for node in root.find_children("*", "MeshInstance3D", true, false):
		var mi := node as MeshInstance3D
		if mi.mesh == null or mi.skin != null or keep_names.has(String(mi.name)):
			continue
		var xf := _relative(root, mi)
		for i in mi.mesh.get_surface_count():
			# PrimitiveMesh (placeholders) is always triangles; ArrayMesh may hold lines/points.
			if mi.mesh is ArrayMesh and (mi.mesh as ArrayMesh).surface_get_primitive_type(i) != Mesh.PRIMITIVE_TRIANGLES:
				continue
			var mat := mi.get_active_material(i)
			if not groups.has(mat):
				var st := SurfaceTool.new()
				st.begin(Mesh.PRIMITIVE_TRIANGLES)
				groups[mat] = st
			(groups[mat] as SurfaceTool).append_from(mi.mesh, i, xf)
		sources.append(mi)
	if sources.size() < 2:
		return 0
	var merged := ArrayMesh.new()
	var idx := 0
	for mat in groups:
		var st: SurfaceTool = groups[mat]
		st.commit(merged)
		merged.surface_set_material(idx, mat)
		idx += 1
	var out := MeshInstance3D.new()
	out.name = "Merged"
	out.mesh = merged
	root.add_child(out)
	for mi in sources:
		mi.queue_free()
	return sources.size()


static func _relative(root: Node3D, node: Node3D) -> Transform3D:
	var xf := Transform3D()
	var n: Node = node
	while n != root and n != null:
		xf = (n as Node3D).transform * xf
		n = n.get_parent()
	return xf
