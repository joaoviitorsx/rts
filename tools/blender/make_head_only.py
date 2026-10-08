"""Blender (5.x) batch script: make a head-only copy of a Quaternius Universal Base Character.

The outfits (Modular Character Outfits - Fantasy) are meant to be worn over the HEAD only; the base body
is a single mesh, so wearing clothes over it clips. This deletes every body vertex whose dominant bone is
not part of the head/neck, keeps the skeleton/skin untouched, and exports a new .glb.
Never touches the vendor file.

Usage:
  flatpak run org.blender.Blender -b --python tools/blender/make_head_only.py -- <in.gltf> <out.glb>
"""
import sys
import bpy
import bmesh

KEEP_BONES = {"Head", "neck_01"}

argv = sys.argv[sys.argv.index("--") + 1:]
src, dst = argv[0], argv[1]

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=src)

body = None
for obj in bpy.context.scene.objects:
    if obj.type == "MESH" and obj.vertex_groups and len(obj.data.vertices) > 2000:
        body = obj
print("BODY MESH:", body.name if body else None)
if body is None:
    raise SystemExit("body mesh not found")

groups = {g.index: g.name for g in body.vertex_groups}
bm = bmesh.new()
bm.from_mesh(body.data)
deform = bm.verts.layers.deform.verify()
to_delete = []
for v in bm.verts:
    weights = v[deform]
    if not weights:
        continue
    bone_index, _ = max(weights.items(), key=lambda kv: kv[1])
    if groups.get(bone_index) not in KEEP_BONES:
        to_delete.append(v)
before = len(bm.verts)
bmesh.ops.delete(bm, geom=to_delete, context="VERTS")
bm.to_mesh(body.data)
bm.free()
print(f"VERTS {before} -> {len(body.data.vertices)}")

bpy.ops.export_scene.gltf(filepath=dst, export_format="GLB", export_animations=False, export_skins=True)
print("EXPORTED", dst)
