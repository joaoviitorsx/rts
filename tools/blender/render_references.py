"""Reference renders for the own villagers (docs/characters_spec.md §10): Kenney Blocky Characters from the
front, 3/4 and side, a lineup of the whole pack, and a Fantasy Town house with a character beside it for scale.

Shape and silhouette only: this Blender (Flatpak 5.1) runs colour management in fallback mode, so colours are judged
in Godot, never here.

Usage (from the repo root):
  flatpak run org.blender.Blender --background --factory-startup --python tools/blender/render_references.py -- \
      [--chars a,b,c] [--out docs/reference/characters]
"""
from __future__ import annotations

import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
VENDOR = ROOT / "godot" / "assets" / "vendor"
BLOCKY = VENDOR / "kenney_blocky_characters"
TOWN = VENDOR / "kenney_fantasy_town_kit"
TOWN_SCALE = 2.0          # LookdevKenney.gd: 1 module = 1 sim cell = 2 m
VILLAGER_HEIGHT = 1.30    # characters_spec.md §1
VIEWS = {"front": (0.0, -1.0, 0.12), "three_quarter": (0.75, -1.0, 0.35), "side": (1.0, 0.0, 0.12)}


def args() -> dict[str, str]:
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    out = {"chars": "", "out": str(ROOT / "docs" / "reference" / "characters"), "only": ""}
    for i in range(0, len(argv) - 1, 2):
        out[argv[i].lstrip("-")] = argv[i + 1]
    return out


def reset() -> None:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    setup_render()


def setup_render() -> None:
    """Engine, neutral grey world and one sun: the same look for every reference and prototype render."""
    scene = bpy.context.scene
    engines = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items]
    scene.render.engine = next(e for e in ("BLENDER_EEVEE", "BLENDER_EEVEE_NEXT", "BLENDER_WORKBENCH") if e in engines)
    scene.render.film_transparent = False
    scene.render.image_settings.file_format = "PNG"
    world = bpy.data.worlds.new("World")
    scene.world = world
    world.use_nodes = True
    bg = next(n for n in world.node_tree.nodes if n.type == "BACKGROUND")
    bg.inputs[0].default_value = (0.42, 0.44, 0.47, 1.0)
    bg.inputs[1].default_value = 0.9
    sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", "SUN"))
    sun.data.energy = 3.0
    sun.rotation_euler = (math.radians(50), 0.0, math.radians(35))
    scene.collection.objects.link(sun)


def import_glb(path: Path) -> list[bpy.types.Object]:
    before = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=str(path))
    return [o for o in bpy.data.objects if o not in before]


def roots(objs: list[bpy.types.Object]) -> list[bpy.types.Object]:
    return [o for o in objs if o.parent is None or o.parent not in objs]


def bbox(objs: list[bpy.types.Object]) -> tuple[Vector, Vector]:
    bpy.context.view_layer.update()
    pts = [o.matrix_world @ Vector(c) for o in objs if o.type == "MESH" for c in o.bound_box]
    return (Vector(min(p[i] for p in pts) for i in range(3)), Vector(max(p[i] for p in pts) for i in range(3)))


def pose_static(objs: list[bpy.types.Object]) -> None:
    """Blocky ships one action per clip; hold the 'static' pose so every render shows the rest silhouette."""
    action = bpy.data.actions.get("static")
    for o in objs:
        if o.type == "ARMATURE" and action:
            o.animation_data_create().action = action
    bpy.context.scene.frame_set(0)


def place(objs: list[bpy.types.Object], loc: Vector, scale: float = 1.0, rot_z: float = 0.0) -> None:
    for r in roots(objs):
        r.location = loc
        r.scale = (scale, scale, scale)
        r.rotation_mode = "XYZ"   # the glTF importer leaves objects in quaternion mode
        r.rotation_euler = (0.0, 0.0, math.radians(rot_z))


def blocky(letter: str) -> list[bpy.types.Object]:
    objs = import_glb(BLOCKY / f"character-{letter}.glb")
    pose_static(objs)
    return objs


def camera_shot(objs: list[bpy.types.Object], direction: tuple[float, float, float], path: Path,
                res: tuple[int, int] = (640, 800), margin: float = 1.15,
                bounds: tuple[Vector, Vector] | None = None) -> None:
    scene = bpy.context.scene
    lo, hi = bounds or bbox(objs)
    centre = (lo + hi) / 2
    size = hi - lo
    cam = bpy.data.objects.get("RefCam") or bpy.data.objects.new("RefCam", bpy.data.cameras.new("RefCam"))
    if cam.name not in scene.collection.objects:
        scene.collection.objects.link(cam)
    cam.data.type = "ORTHO"
    d = Vector(direction).normalized()
    cam.location = centre + d * (size.length * 3 + 5)
    cam.rotation_euler = (-d).to_track_quat("-Z", "Y").to_euler()
    scene.render.resolution_x, scene.render.resolution_y = res
    aspect = res[0] / res[1]
    # ortho_scale covers the larger of the projected width and height (rough projection on the view plane)
    right = d.cross(Vector((0, 0, 1)))
    right = right.normalized() if right.length > 1e-6 else Vector((1, 0, 0))
    corners = [Vector((x, y, z)) for x in (lo.x, hi.x) for y in (lo.y, hi.y) for z in (lo.z, hi.z)]
    w = max((c - centre).dot(right) for c in corners) * 2
    h = (hi.z - lo.z)
    cam.data.ortho_scale = max(w, h * aspect) * margin if aspect >= 1 else max(w / aspect, h) * margin
    scene.camera = cam
    scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)
    print(f"  wrote {path}")


def town_module(name: str, cell: Vector, rot_deg: float, origin: Vector) -> list[bpy.types.Object]:
    """Godot (x, y, z) -> Blender (x, -z, y); a Godot yaw is the same angle about Blender Z."""
    objs = import_glb(TOWN / f"{name}.glb")
    g = cell * TOWN_SCALE
    place(objs, origin + Vector((g.x, -g.z, g.y)), TOWN_SCALE, rot_deg)
    return objs


def house(origin: Vector, roof_rot: float = 0.0) -> list[bpy.types.Object]:
    """3×1 house of Fantasy Town modules at the game scale: door in the middle of the front (+Z in Godot = -Y here),
    shuttered windows beside it, an A-frame roof (`roof-gable`, one per cell) with the ridge along x."""
    objs: list[bpy.types.Object] = []
    w = 3
    for x in range(w):
        cell = Vector((x + 0.5, 0, 0.5))
        # The kit's wall sits on the +X side of its tile: rotate it to each outside face.
        objs += town_module("wall-door" if x == w // 2 else "wall-window-shutters", cell, -90.0, origin)
        objs += town_module("wall-window-small" if x == w // 2 else "wall", cell, 90.0, origin)
        if x == w - 1:
            objs += town_module("wall", cell, 0.0, origin)
        if x == 0:
            objs += town_module("wall", cell, 180.0, origin)
        objs += town_module("roof-gable", cell + Vector((0, 1, 0)), roof_rot, origin)
    objs += town_module("chimney", Vector((w - 0.6, 1, 0.3)), 0.0, origin)
    return objs


def main() -> None:
    a = args()
    out = Path(a["out"])
    out.mkdir(parents=True, exist_ok=True)
    letters = [chr(c) for c in range(ord("a"), ord("r") + 1)]

    if a["only"] == "scale":
        letters = []
    elif a["only"] == "lineup":
        a["chars"] = "-"
    # 1. Lineup of the whole pack (to choose the closest-to-medieval ones).
    lineup: list[bpy.types.Object] = []
    if letters:
        reset()
    for i, letter in enumerate(letters):
        objs = blocky(letter)
        place(objs, Vector((i * 1.15, 0, 0)))
        lineup += objs
    if lineup:
        camera_shot(lineup, (0.0, -1.0, 0.15), out / "blocky_lineup.png", res=(2400, 420), margin=1.02)

    # 2. Front, 3/4, side of the chosen ones.
    chosen = [c for c in a["chars"].split(",") if c and c != "-"] or ([] if a["chars"] == "-" else ["a", "f", "m", "q"])
    for letter in chosen if a["only"] != "scale" else []:
        reset()
        objs = blocky(letter)
        lo, hi = bbox(objs)
        print(f"character-{letter}: height {hi.z - lo.z:.3f} u, width {hi.x - lo.x:.3f} u")
        for view, direction in VIEWS.items():
            camera_shot(objs, direction, out / f"blocky_{letter}_{view}.png")

    if not chosen:
        return
    # 3. Scale: Fantasy Town house (×2) with a Blocky character scaled to the villager height beside the door.
    reset()
    h = house(Vector((0, 0, 0)), float(a.get("roof_rot", "0")))
    ch = blocky(chosen[0])
    lo, hi = bbox(ch)
    s = VILLAGER_HEIGHT / (hi.z - lo.z)
    place(ch, Vector((4.4, -2.6, 0)), s, 0.0)          # beside the door (door cell x=1 → 2..4 m, front face y=-2)
    ground = bpy.data.objects.new("Ground", bpy.data.meshes.new("Ground"))
    ground.data.from_pydata([(-3, -7, 0), (9, -7, 0), (9, 3, 0), (-3, 3, 0)], [], [(0, 1, 2, 3)])
    bpy.context.scene.collection.objects.link(ground)
    lo, hi = bbox(h + ch)
    print(f"house: {hi.x - lo.x:.2f} × {hi.y - lo.y:.2f} × {hi.z - lo.z:.2f} m; character {VILLAGER_HEIGHT} m (scale {s:.3f})")
    camera_shot(h + ch, (0.55, -1.0, 0.55), out / "scale_house_villager.png", res=(1200, 900), margin=1.1)
    camera_shot(h + ch, (0.0, -1.0, 0.05), out / "scale_house_villager_front.png", res=(1200, 900), margin=1.1)


if __name__ == "__main__":
    main()
