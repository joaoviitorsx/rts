#!/usr/bin/env python3
"""Generates the game's asset scenes (godot/assets/<category>/*.tscn) from tools/assets/asset_catalog.json.

Every scene is a plain Node3D root named after its ID with the vendor model(s) instanced as children, so
scale/offset/pivot fixes live in our scene (never in the vendor file) and the game code only references
these scenes. Re-runnable: output is fully regenerated from the catalog.

Kinds:
  model       one vendor model, with scale and automatic ground offset (min Y of the model -> 0)
  modular     a building assembled from Medieval Village MegaKit pieces (walls on a 2 m grid, corners, roof, door)
  composite   free list of parts (vendor models, other generated scenes, placeholders)
  placeholder primitive stand-in for an asset we don't have yet (listed as "falta" in the manifest)
  character   modular character (head-only body + outfit + hair + shared AnimationLibrary)

Usage: python3 tools/assets/build_scenes.py [--check]
"""
from __future__ import annotations

import json
import math
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
GODOT = ROOT / "godot"
CATALOG = Path(__file__).with_name("asset_catalog.json")

PREFIX = {
    "MVK": "res://assets/vendor/quaternius_medieval_village/{}.gltf",
    "FP": "res://assets/vendor/quaternius_fantasy_props/{}.gltf",
    "NAT": "res://assets/vendor/quaternius_stylized_nature/{}.gltf",
}

WALL_H = 3.12
WALL_STYLES = {
    "Plaster": {"wall": "Wall_Plaster_Straight", "door": "Wall_Plaster_Door_Round", "door_panel": "Door_1_Round",
                "window": "Wall_Plaster_Window_Wide_Round", "corner": "Corner_Exterior_Wood"},
    "PlasterGrid": {"wall": "Wall_Plaster_WoodGrid", "door": "Wall_Plaster_Door_Flat", "door_panel": "Door_1_Flat",
                    "window": "Wall_Plaster_Window_Thin_Round", "corner": "Corner_Exterior_Wood"},
    "Brick": {"wall": "Wall_UnevenBrick_Straight", "door": "Wall_UnevenBrick_Door_Round", "door_panel": "Door_1_Round",
              "window": "Wall_UnevenBrick_Window_Wide_Round", "corner": "Corner_Exterior_Brick"},
}


# ---------------------------------------------------------------------------------------------- math

def transform(pos=(0, 0, 0), rot_y_deg=0.0, scale=1.0) -> str:
    """Godot Transform3D literal: basis columns (x, y, z) then origin."""
    if isinstance(scale, (int, float)):
        scale = (scale, scale, scale)
    a = math.radians(rot_y_deg)
    c, s = math.cos(a), math.sin(a)
    x = (c * scale[0], 0.0, -s * scale[0])
    y = (0.0, 1.0 * scale[1], 0.0)
    z = (s * scale[2], 0.0, c * scale[2])
    vals = [*x, *y, *z, *pos]
    return "Transform3D(" + ", ".join(fmt(v) for v in vals) + ")"


def fmt(v: float) -> str:
    v = round(v, 5)
    return "0" if v == 0 else (str(int(v)) if v == int(v) else f"{v:g}")


def rotate_xz(x, z, deg):
    a = math.radians(deg)
    return x * math.cos(a) + z * math.sin(a), -x * math.sin(a) + z * math.cos(a)


# ---------------------------------------------------------------------------------------------- bounds

def gltf_min_y(res_path: str) -> float:
    """Min Y of a .gltf's geometry in scene space (node transforms applied)."""
    path = GODOT / res_path.removeprefix("res://")
    doc = json.loads(path.read_text(encoding="utf-8"))

    def mat_of(n):
        if "matrix" in n:
            m = n["matrix"]
            return [[m[0], m[4], m[8], m[12]], [m[1], m[5], m[9], m[13]], [m[2], m[6], m[10], m[14]], [0, 0, 0, 1]]
        t = n.get("translation", [0, 0, 0]); q = n.get("rotation", [0, 0, 0, 1]); sc = n.get("scale", [1, 1, 1])
        qx, qy, qz, qw = q
        r = [[1 - 2 * (qy * qy + qz * qz), 2 * (qx * qy - qz * qw), 2 * (qx * qz + qy * qw)],
             [2 * (qx * qy + qz * qw), 1 - 2 * (qx * qx + qz * qz), 2 * (qy * qz - qx * qw)],
             [2 * (qx * qz - qy * qw), 2 * (qy * qz + qx * qw), 1 - 2 * (qx * qx + qy * qy)]]
        return [[r[i][0] * sc[0], r[i][1] * sc[1], r[i][2] * sc[2], t[i]] for i in range(3)] + [[0, 0, 0, 1]]

    def mul(a, b):
        return [[sum(a[i][k] * b[k][j] for k in range(4)) for j in range(4)] for i in range(4)]

    lo = [math.inf]

    def walk(i, m):
        n = doc["nodes"][i]
        m = mul(m, mat_of(n))
        if "mesh" in n:
            for p in doc["meshes"][n["mesh"]]["primitives"]:
                a = doc["accessors"][p["attributes"]["POSITION"]]
                for cx in (a["min"][0], a["max"][0]):
                    for cy in (a["min"][1], a["max"][1]):
                        for cz in (a["min"][2], a["max"][2]):
                            y = m[1][0] * cx + m[1][1] * cy + m[1][2] * cz + m[1][3]
                            lo[0] = min(lo[0], y)
        for ch in n.get("children", []):
            walk(ch, m)

    ident = [[1, 0, 0, 0], [0, 1, 0, 0], [0, 0, 1, 0], [0, 0, 0, 1]]
    for r in doc["scenes"][doc.get("scene", 0)]["nodes"]:
        walk(r, ident)
    return lo[0]


# ---------------------------------------------------------------------------------------------- scene writer

class Scene:
    def __init__(self, root_name: str, root_type: str = "Node3D"):
        self.root_name = root_name
        self.root_type = root_type
        self.root_props: list[str] = []
        self.ext: dict[str, tuple[str, str]] = {}   # path -> (type, id)
        self.sub: list[str] = []
        self.nodes: list[str] = []
        self.names: dict[str, int] = {}
        self._sub_count = 0

    def ext_id(self, path: str, kind: str = "PackedScene") -> str:
        if path not in self.ext:
            self.ext[path] = (kind, f"{len(self.ext) + 1}_{Path(path).stem[:24]}")
        return self.ext[path][1]

    def unique(self, name: str) -> str:
        n = self.names.get(name, 0)
        self.names[name] = n + 1
        return name if n == 0 else f"{name}_{n + 1}"

    def instance(self, path: str, name: str, xform: str, parent: str = "."):
        eid = self.ext_id(path)
        self.nodes.append(f'[node name="{self.unique(name)}" parent="{parent}" instance=ExtResource("{eid}")]\n'
                          f"transform = {xform}\n")

    def node(self, name: str, type_: str, props: list[str], parent: str = "."):
        body = "".join(p + "\n" for p in props)
        self.nodes.append(f'[node name="{self.unique(name)}" type="{type_}" parent="{parent}"]\n{body}')

    def subresource(self, type_: str, props: list[str]) -> str:
        self._sub_count += 1
        sid = f"{type_}_{self._sub_count}"
        self.sub.append(f'[sub_resource type="{type_}" id="{sid}"]\n' + "".join(p + "\n" for p in props))
        return sid

    def primitive(self, name: str, spec: dict, xform: str):
        r, g, b = hex_color(spec.get("color", "#cccccc"))
        mat = self.subresource("StandardMaterial3D", [f"albedo_color = Color({r:.3f}, {g:.3f}, {b:.3f}, 1)", "roughness = 0.9"])
        size = spec["size"]
        kind = spec.get("primitive", "box")
        if kind == "cylinder":
            mesh = self.subresource("CylinderMesh", [f'material = SubResource("{mat}")', f"top_radius = {fmt(size[0] / 2)}",
                                                     f"bottom_radius = {fmt(size[0] / 2)}", f"height = {fmt(size[1])}"])
        elif kind == "cone":
            mesh = self.subresource("CylinderMesh", [f'material = SubResource("{mat}")', "top_radius = 0.02",
                                                     f"bottom_radius = {fmt(size[0] / 2)}", f"height = {fmt(size[1])}"])
        else:
            mesh = self.subresource("BoxMesh", [f'material = SubResource("{mat}")',
                                                f"size = Vector3({fmt(size[0])}, {fmt(size[1])}, {fmt(size[2])})"])
        # primitives are centred: lift by half height so the base sits on the given position
        self.nodes.append(f'[node name="{self.unique(name)}" type="MeshInstance3D" parent="."]\n'
                          f"transform = {xform}\n"
                          f'mesh = SubResource("{mesh}")\n')

    def render(self) -> str:
        steps = len(self.ext) + len(self.sub) + 1
        out = [f"[gd_scene load_steps={steps} format=3]\n"]
        for path, (kind, eid) in self.ext.items():
            out.append(f'[ext_resource type="{kind}" path="{path}" id="{eid}"]')
        if self.ext:
            out.append("")
        for s in self.sub:
            out.append(s)
        root = f'[node name="{self.root_name}" type="{self.root_type}"]\n' + "".join(p + "\n" for p in self.root_props)
        out.append(root)
        out.extend(self.nodes)
        return "\n".join(out)


def hex_color(h: str):
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) / 255 for i in (0, 2, 4))


def resolve(src: str, catalog_index: dict) -> str:
    if src.startswith("res://"):
        return src
    prefix, _, name = src.partition("/")
    if prefix in PREFIX:
        return PREFIX[prefix].format(name)
    if prefix == "SCN":
        entry = catalog_index[name]
        return f"res://assets/{entry['category']}/{name}.tscn"
    raise ValueError(f"unknown source prefix in '{src}'")


# ---------------------------------------------------------------------------------------------- kinds

def build_model(scene: Scene, e: dict, idx: dict):
    path = resolve(e["src"], idx)
    scale = e.get("scale", 1.0)
    lift = 0.0
    if e.get("ground", True) and path.endswith(".gltf"):
        lift = -gltf_min_y(path) * scale + e.get("sink", 0.0)
        e["_lift"] = round(lift, 3)
    pos = e.get("offset", [0, 0, 0])
    scene.instance(path, "Model", transform((pos[0], pos[1] + lift, pos[2]), e.get("rot", 0), scale))


def add_part(scene: Scene, part: dict, idx: dict, base=(0.0, 0.0, 0.0), base_rot=0.0):
    px, py, pz = part.get("pos", [0, 0, 0])
    rx, rz = rotate_xz(px, pz, base_rot)
    pos = (base[0] + rx, base[1] + py, base[2] + rz)
    rot = base_rot + part.get("rot", 0)
    if "primitive" in part:
        size = part["size"]
        scene.primitive(part.get("name", "Placeholder"), part, transform((pos[0], pos[1] + size[1] / 2, pos[2]), rot))
        return
    path = resolve(part["src"], idx)
    scene.instance(path, part.get("name", Path(path).stem), transform(pos, rot, part.get("scale", 1.0)))


def build_modular(scene: Scene, e: dict, idx: dict):
    style = WALL_STYLES[e.get("wall", "Plaster")]
    ox, oz = e.get("offset", [0, 0])
    for block in e.get("blocks", [e]):
        _build_block(scene, block, style if "wall" not in block else WALL_STYLES[block["wall"]], idx,
                     (ox + block.get("at", [0, 0])[0], oz + block.get("at", [0, 0])[1]))
    for part in e.get("parts", []):
        add_part(scene, part, idx)


def _build_block(scene: Scene, b: dict, style: dict, idx: dict, origin):
    w, d = b["size"]
    ox, oz = origin
    openings = {side: {int(k): v for k, v in m.items()} for side, m in b.get("openings", {}).items()}
    sides = {
        # side: (modules, rot, position(a) -> (x, z))
        "S": (w // 2, 0, lambda a: (a, d / 2)),
        "N": (w // 2, 180, lambda a: (-a, -d / 2)),
        "E": (d // 2, 90, lambda a: (w / 2, -a)),
        "W": (d // 2, -90, lambda a: (-w / 2, a)),
    }
    for side, (n, rot, at) in sides.items():
        if side in b.get("open_sides", []):
            continue
        length = n * 2
        for i in range(n):
            a = -length / 2 + 1 + 2 * i
            x, z = at(a)
            kind = openings.get(side, {}).get(i, "wall")
            scene.instance(PREFIX["MVK"].format(style[kind]), f"Wall_{side}{i}", transform((ox + x, 0, oz + z), rot))
            if kind == "door":
                dx, dz = rotate_xz(-0.56, b.get("door_z", -0.11), rot)
                scene.instance(PREFIX["MVK"].format(style["door_panel"]), f"Door_{side}{i}",
                               transform((ox + x + dx, 0.03, oz + z + dz), rot))
    for cx, cz in ((w / 2, d / 2), (-w / 2, d / 2), (w / 2, -d / 2), (-w / 2, -d / 2)):
        scene.instance(PREFIX["MVK"].format(style["corner"]), "Corner", transform((ox + cx, 0, oz + cz)))
    if "roof" in b:
        roof_rot = b.get("roof_rot", 0)
        roof_y = b.get("roof_y", WALL_H)
        scene.instance(PREFIX["MVK"].format(b["roof"]), "Roof", transform((ox, roof_y, oz), roof_rot))
        # Gable ends: the roof pieces are open at both ends of the ridge (roof-local ±Z).
        span, depth = roof_span(b["roof"])
        if b.get("gables", True) and span:
            for local_z, rot in ((depth / 2, 180), (-depth / 2, 0)):
                gx, gz = rotate_xz(0, local_z, roof_rot)
                scene.instance(PREFIX["MVK"].format(f"Roof_Front_Brick{span}"), "Gable",
                               transform((ox + gx, roof_y + b.get("gable_y", 0), oz + gz), roof_rot + rot))


def roof_span(roof: str):
    """(gable width, ridge length) in meters, from the MVK roof name."""
    m = re.match(r"Roof_RoundTiles_(\d+)x(\d+)$", roof) or re.match(r"Roof_(\d+)x(\d+)_RoundTile$", roof)
    return (int(m.group(1)), int(m.group(2))) if m else (0, 0)


def build_composite(scene: Scene, e: dict, idx: dict):
    for part in e["parts"]:
        add_part(scene, part, idx)


def build_placeholder(scene: Scene, e: dict, idx: dict):
    for part in e.get("parts", [e]):
        p = dict(part)
        p.setdefault("pos", [0, 0, 0])
        add_part(scene, p, idx)
    scene.root_props.append('metadata/placeholder = true')


def build_character(scene: Scene, e: dict, idx: dict):
    script = "res://game/scripts/visual/ModularCharacter.cs"
    scene.root_props.append(f'script = ExtResource("{scene.ext_id(script, "Script")}")')
    scene.instance(resolve(e["body"], idx), "Body", transform())
    scene.instance(resolve(e["outfit"], idx), "Outfit", transform())
    if e.get("hair"):
        scene.instance(resolve(e["hair"], idx), "Hair", transform())
    lib = scene.ext_id(e["library"], "AnimationLibrary")
    scene.node("AnimationPlayer", "AnimationPlayer", [
        'root_node = NodePath("../Body")',
        f'libraries = {{\n&"": ExtResource("{lib}")\n}}',
        f'autoplay = &"{e.get("autoplay", "idle")}"',
    ])
    for part in e.get("attachments", []):
        add_part(scene, part, idx)


BUILDERS = {"model": build_model, "modular": build_modular, "composite": build_composite,
            "placeholder": build_placeholder, "character": build_character}


def main() -> int:
    check = "--check" in sys.argv
    catalog = json.loads(CATALOG.read_text(encoding="utf-8"))
    entries = catalog["assets"]
    idx = {e["id"]: e for e in entries}
    written = 0
    for e in entries:
        scene = Scene(e["id"])
        BUILDERS[e["kind"]](scene, e, idx)
        if e.get("note"):
            scene.root_props.append(f'metadata/note = "{e["note"]}"')
        out = GODOT / "assets" / e["category"] / f"{e['id']}.tscn"
        text = scene.render()
        if check:
            continue
        out.parent.mkdir(parents=True, exist_ok=True)
        if not out.exists() or out.read_text(encoding="utf-8") != text:
            out.write_text(text, encoding="utf-8")
            written += 1
    lifts = {e["id"]: e["_lift"] for e in entries if "_lift" in e and abs(e["_lift"]) > 0.005}
    print(f"{len(entries)} scenes, {written} written")
    if lifts:
        print("ground offsets applied:", json.dumps(lifts))
    return 0


if __name__ == "__main__":
    sys.exit(main())
