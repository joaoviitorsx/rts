"""Prototype villager in the Kenney style (docs/characters_spec.md, stage 2): male base body + short hair (under-hat
cut) + straw hat + tunic + trousers + axe, on the 21-bone humanoid rig, with the idle, walk and chop animations.

Everything is built from scratch on every run (no state from earlier runs, no randomness):
  palette  godot/assets/characters/kenney/T_Villager_Palette.png   64×64, 8×8 cells of 8 px (spec §2.1)
  source   art/source/characters/villager_proto.blend
  glb      godot/assets/characters/kenney/CHR_Villager_Proto.glb      assembled, with animations (Godot comparison)
           godot/assets/characters/kenney/CHR_Villager_Base_M.glb     rig + body + animations
           godot/assets/characters/kenney/CHR_Part_<Slot>_<Var>.glb   rig + one skinned part
           godot/assets/characters/kenney/TOOL_Axe_A.glb              rig + axe skinned to ToolSocket
  renders  docs/reports/img_characters/proto_*.png                   shape only (P41: colour is judged in Godot)

Conventions: 1 u = 1 m, pivot between the feet, the character faces -Y (= +Z in Godot), bind pose in A-pose.
Rigid skinning: every vertex weighs 1.0 on one bone. One material, UVs on the centre of one palette cell per face.

Usage (from the repo root):
  flatpak run org.blender.Blender --background --factory-startup --python tools/blender/build_villager.py -- [--no-render]
"""
from __future__ import annotations

import math
import struct
import sys
import zlib
from pathlib import Path

import bmesh
import bpy
from mathutils import Euler, Matrix, Quaternion, Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
import render_references as rr  # noqa: E402  (shared render setup and camera)

ROOT = Path(__file__).resolve().parents[2]
OUT_GLB = ROOT / "godot" / "assets" / "characters" / "kenney"
OUT_BLEND = ROOT / "art" / "source" / "characters" / "villager_proto.blend"
OUT_IMG = ROOT / "docs" / "reports" / "img_characters"
FPS = 30

# ------------------------------------------------------------------------------------------------ palette (spec §2.1)

# Hues of the spec/Bible palette, value lifted to sit with the Kenney colormap under the game light (iteration 3:
# V' = 0.3 + 0.7·V for cloth/leather/fibre/metal, V' = 0.2 + 0.8·V for hair, saturation ×1.05). Spec §2.1 keeps both.
PALETTE: list[list[str | None]] = [
    ["F1CFAE", "DDA77F", "B57A52", "7A4E33", None, None, None, None],                      # 0 skin
    ["654330", "8B532F", "DBB660", "B04F24", "B8B1AA", None, None, None],                  # 1 hair
    ["E0D1AC", "819B5C", "BF4D37", "5E82A2", "D69637", "9C5C73", "D7AE5F", "AD764E"],      # 2 cloth A
    ["E0D1AC", "819B5C", "BF4D37", "5E82A2", "D69637", "9C5C73", "D7AE5F", "AD764E"],      # 3 cloth B
    ["AD764E", "976644", "83573D", "C28D5D", None, None, None, None],                      # 4 leather / wood
    ["D7AE5F", "BB934B", "CBAB78", "C09254", None, None, None, None],                      # 5 fibres
    ["A9B1B9", "838C94", "C08B4B", None, None, None, None, None],                          # 6 metal
    ["2B2420", "D98C7A", "5E82A2", "9C5C73", "E9DFC6", "E2DCCB", None, None],              # 7 fixed
]
RESERVED = "FF00FF"   # unused cells are loud magenta, so a stray UV shows at once
SKIN, HAIR, CLOTH_A, CLOTH_B, LEATHER, FIBRE, METAL, FIXED = range(8)


def cell_uv(row: int, col: int) -> tuple[float, float]:
    assert PALETTE[row][col] is not None, (row, col)
    return (col + 0.5) / 8.0, 1.0 - (row + 0.5) / 8.0


def write_palette_png(path: Path) -> None:
    """64×64 RGB PNG, written byte by byte (exact hex values, no colour management involved)."""
    rows = []
    for py in range(64):
        line = bytearray([0])                       # filter type 0
        for px in range(64):
            h = PALETTE[py // 8][px // 8] or RESERVED
            line += bytes.fromhex(h)
        rows.append(bytes(line))
    raw = zlib.compress(b"".join(rows), 9)

    def chunk(tag: bytes, data: bytes) -> bytes:
        return struct.pack(">I", len(data)) + tag + data + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)

    png = b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", 64, 64, 8, 2, 0, 0, 0)) \
        + chunk(b"IDAT", raw) + chunk(b"IEND", b"")
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(png)


def palette_material(png: Path) -> bpy.types.Material:
    img = bpy.data.images.load(str(png))
    img.name = "T_Villager_Palette"
    mat = bpy.data.materials.new("MAT_Villager_Atlas")
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    bsdf = next(n for n in nodes if n.type == "BSDF_PRINCIPLED")
    tex = nodes.new("ShaderNodeTexImage")
    tex.image = img
    tex.interpolation = "Closest"
    mat.node_tree.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = 1.0
    bsdf.inputs["Metallic"].default_value = 0.0
    for name in ("Specular IOR Level", "Specular"):
        if name in bsdf.inputs:
            bsdf.inputs[name].default_value = 0.0
    return mat


# ------------------------------------------------------------------------------------------------ rig (spec §6)

S45 = math.sin(math.radians(45))
ARM_L = Vector((S45, 0, -S45))           # A-pose: arms 45° below the horizontal
ARM_R = Vector((-S45, 0, -S45))
SHOULDER_L, SHOULDER_R = Vector((0.19, 0, 0.855)), Vector((-0.19, 0, 0.855))
UPPER_ARM, LOWER_ARM, HAND = 0.16, 0.14, 0.10
HIP_X, HIP_Z, KNEE_Z, ANKLE_Z = 0.085, 0.47, 0.27, 0.075
HEAD_BOT, HEAD_TOP, HEAD_W, HEAD_D = 0.95, 1.26, 0.33, 0.29   # big cozy head (~4.4 heads tall with the hat)
FACE_Y = -HEAD_D / 2


def arm_points(side: str) -> tuple[Vector, Vector, Vector, Vector]:
    s, a = (SHOULDER_L, ARM_L) if side == "Left" else (SHOULDER_R, ARM_R)
    elbow = s + a * UPPER_ARM
    wrist = elbow + a * LOWER_ARM
    return s, elbow, wrist, wrist + a * HAND


# name: (head, tail, parent, deform)
def bone_table() -> dict[str, tuple[Vector, Vector, str | None, bool]]:
    t: dict[str, tuple[Vector, Vector, str | None, bool]] = {
        "Root": (Vector((0, 0, 0)), Vector((0, 0, 0.1)), None, False),
        "Hips": (Vector((0, 0, HIP_Z)), Vector((0, 0, 0.60)), "Root", True),
        "Spine": (Vector((0, 0, 0.60)), Vector((0, 0, 0.72)), "Hips", True),
        "Chest": (Vector((0, 0, 0.72)), Vector((0, 0, 0.89)), "Spine", True),
        "Neck": (Vector((0, 0, 0.89)), Vector((0, 0, 0.95)), "Chest", True),
        "Head": (Vector((0, 0, HEAD_BOT)), Vector((0, 0, HEAD_TOP)), "Neck", True),
        "EmoteSocket": (Vector((0, 0, 1.60)), Vector((0, 0, 1.67)), "Head", False),
        "BackSocket": (Vector((0, 0.12, 0.80)), Vector((0, 0.20, 0.80)), "Chest", True),
    }
    for side in ("Left", "Right"):
        s, elbow, wrist, tip = arm_points(side)
        t[f"{side}UpperArm"] = (s, elbow, "Chest", True)
        t[f"{side}LowerArm"] = (elbow, wrist, f"{side}UpperArm", True)
        t[f"{side}Hand"] = (wrist, tip, f"{side}LowerArm", True)
        x = HIP_X if side == "Left" else -HIP_X
        t[f"{side}UpperLeg"] = (Vector((x, 0, HIP_Z)), Vector((x, 0, KNEE_Z)), "Hips", True)
        t[f"{side}LowerLeg"] = (Vector((x, 0, KNEE_Z)), Vector((x, 0, ANKLE_Z)), f"{side}UpperLeg", True)
        t[f"{side}Foot"] = (Vector((x, 0, ANKLE_Z)), Vector((x, -0.11, 0.03)), f"{side}LowerLeg", True)
    grip = (arm_points("Right")[2] + arm_points("Right")[3]) / 2
    t["ToolSocket"] = (grip, grip + Vector((0, -0.08, 0)), "RightHand", True)
    return t


BONES = bone_table()
BONE_NAMES = list(BONES)
GRIP = BONES["ToolSocket"][0]


def build_armature() -> bpy.types.Object:
    data = bpy.data.armatures.new("VillagerRig")
    arm = bpy.data.objects.new("VillagerRig", data)
    bpy.context.scene.collection.objects.link(arm)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="EDIT")
    for name, (head, tail, parent, deform) in BONES.items():
        eb = data.edit_bones.new(name)
        eb.head, eb.tail = head, tail
        eb.use_deform = deform
        if parent:
            eb.parent = data.edit_bones[parent]
    # The tool socket's Z is the blade direction (down while the arm hangs): animation aims it with an 'up' hint.
    data.edit_bones["ToolSocket"].align_roll(Vector((0, 0, -1)))
    bpy.ops.object.mode_set(mode="OBJECT")
    data.display_type = "STICK"
    return arm


# ------------------------------------------------------------------------------------------------ geometry

class Part:
    """One skinned mesh: primitives are added with a palette cell and a bone; faces get the cell's UV."""

    def __init__(self, name: str):
        self.name = name
        self.bm = bmesh.new()
        self.uv = self.bm.loops.layers.uv.verify()
        self.deform = self.bm.verts.layers.deform.verify()

    def _finish(self, before: set, cell: tuple[int, int], bone: str, bevel: float) -> None:
        new_faces = [f for f in self.bm.faces if f not in before]
        if bevel > 0:
            verts = list({v for f in new_faces for v in f.verts})
            edges = list({e for f in new_faces for e in f.edges})
            bmesh.ops.bevel(self.bm, geom=verts + edges, offset=bevel, offset_type="OFFSET", segments=1,
                            profile=0.5, affect="EDGES", clamp_overlap=True)
            new_faces = [f for f in self.bm.faces if f not in before]
        u, v = cell_uv(*cell)
        b = BONE_NAMES.index(bone)
        for f in new_faces:
            f.smooth = False
            for loop in f.loops:
                loop[self.uv].uv = (u, v)
                loop.vert[self.deform][b] = 1.0

    def box(self, p0, p1, w0: float, d0: float, cell, bone: str, w1: float | None = None, d1: float | None = None,
            hint=(1, 0, 0), bevel: float = 0.0) -> None:
        """Box from p0 (centre of one end) to p1 (centre of the other); cross-section w×d, tapering w0×d0 → w1×d1."""
        p0, p1 = Vector(p0), Vector(p1)
        a = (p1 - p0)
        length = a.length
        a.normalize()
        h = Vector(hint)
        u = h - a * a.dot(h)
        if u.length < 1e-6:
            u = Vector((0, 1, 0)) - a * a.y
        u.normalize()
        v = a.cross(u)
        w1 = w0 if w1 is None else w1
        d1 = d0 if d1 is None else d1
        before = set(self.bm.faces)
        res = bmesh.ops.create_cube(self.bm, size=1.0)
        for vert in res["verts"]:
            x, y, z = vert.co
            t = z + 0.5
            w, d = w0 + (w1 - w0) * t, d0 + (d1 - d0) * t
            vert.co = p0 + a * (t * length) + u * (x * w) + v * (y * d)
        self._finish(before, cell, bone, bevel)

    def cylinder(self, base, height: float, r0: float, r1: float, cell, bone: str, sides: int = 8) -> None:
        """Vertical prism from z=base.z up; a flat side faces the front (-Y)."""
        before = set(self.bm.faces)
        m = Matrix.Translation(Vector(base) + Vector((0, 0, height / 2))) @ Matrix.Rotation(math.pi / sides, 4, "Z")
        bmesh.ops.create_cone(self.bm, cap_ends=True, cap_tris=False, segments=sides, radius1=r0, radius2=r1,
                              depth=height, matrix=m)
        self._finish(before, cell, bone, 0.0)

    def to_object(self, arm: bpy.types.Object, mat: bpy.types.Material) -> bpy.types.Object:
        me = bpy.data.meshes.new(self.name)
        self.bm.normal_update()
        self.bm.to_mesh(me)
        self.bm.free()
        me.materials.append(mat)
        ob = bpy.data.objects.new(self.name, me)
        bpy.context.scene.collection.objects.link(ob)
        for name in BONE_NAMES:
            ob.vertex_groups.new(name=name)
        ob.parent = arm
        mod = ob.modifiers.new("Armature", "ARMATURE")
        mod.object = arm
        return ob


def build_body() -> Part:
    p = Part("CHR_Body_M")
    f = FACE_Y
    p.box((0, 0, HEAD_BOT), (0, 0, HEAD_TOP), HEAD_W, HEAD_D, (SKIN, 1), "Head", bevel=0.038)
    p.box((0, f + 0.006, 1.065), (0, f - 0.033, 1.065), 0.048, 0.042, (SKIN, 1), "Head")              # nose
    for sx in (1, -1):
        p.box((sx * 0.07, f + 0.006, 1.11), (sx * 0.07, f - 0.012, 1.11), 0.036, 0.062, (FIXED, 0), "Head")   # eye
        p.box((sx * 0.10, f + 0.004, 1.05), (sx * 0.10, f - 0.004, 1.05), 0.048, 0.024, (FIXED, 1), "Head")  # cheek
        p.box((sx * (HEAD_W / 2 - 0.01), 0.0, 1.08), (sx * (HEAD_W / 2 + 0.024), 0.0, 1.08), 0.05, 0.07, (SKIN, 1),
              "Head", hint=(0, 1, 0))                                                                    # ear
    p.box((0, 0, 0.86), (0, 0, 0.97), 0.11, 0.10, (SKIN, 1), "Neck")
    for side in ("Left", "Right"):
        _, _, wrist, tip = arm_points(side)
        p.box(wrist, tip, 0.10, 0.085, (SKIN, 1), f"{side}Hand", hint=(0, 1, 0), bevel=0.012)
        x = HIP_X if side == "Left" else -HIP_X
        p.box((x, 0.05, 0.0425), (x, -0.13, 0.0425), 0.12, 0.085, (LEATHER, 2), f"{side}Foot", bevel=0.012)
    return p


def build_tunic() -> Part:
    p = Part("CHR_Part_Torso_Tunic")
    p.box((0, 0, 0.57), (0, 0, 0.73), 0.31, 0.205, (CLOTH_A, 1), "Spine", w1=0.33, d1=0.21, bevel=0.02)
    p.box((0, 0, 0.72), (0, 0, 0.905), 0.33, 0.21, (CLOTH_A, 1), "Chest", w1=0.37, d1=0.22, bevel=0.03)
    p.box((0, 0, 0.40), (0, 0, 0.60), 0.37, 0.25, (CLOTH_A, 1), "Hips", w1=0.32, d1=0.21)          # hem, flared
    p.box((0, 0, 0.585), (0, 0, 0.64), 0.335, 0.225, (LEATHER, 1), "Hips")                       # belt
    p.box((0, -0.118, 0.585), (0, -0.124, 0.64), 0.06, 0.06, (METAL, 2), "Hips")                  # buckle
    for side in ("Left", "Right"):
        s, elbow, wrist, _ = arm_points(side)
        a = (elbow - s).normalized()
        p.box(s - a * 0.03, elbow + a * 0.015, 0.115, 0.115, (CLOTH_A, 1), f"{side}UpperArm", hint=(0, 1, 0))
        p.box(elbow - a * 0.01, wrist + a * 0.005, 0.105, 0.105, (CLOTH_A, 1), f"{side}LowerArm",
              w1=0.115, d1=0.115, hint=(0, 1, 0))
    return p


def build_trousers() -> Part:
    p = Part("CHR_Part_Legs_Trousers")
    for side in ("Left", "Right"):
        x = HIP_X if side == "Left" else -HIP_X
        p.box((x, 0, 0.52), (x, 0, 0.255), 0.135, 0.14, (CLOTH_B, 7), f"{side}UpperLeg")
        p.box((x, 0, 0.275), (x, 0, 0.06), 0.125, 0.13, (CLOTH_B, 7), f"{side}LowerLeg", w1=0.12, d1=0.125)
    return p


def build_hair_short_under_hat() -> Part:
    """Short cut for heads with a hat: no crown on top (the hat covers it), hair shows below the brim."""
    p = Part("CHR_Part_Hair_ShortUnderHat")
    w, top = HEAD_W + 0.016, HEAD_TOP - 0.004
    p.box((0, 0.0, top - 0.042), (0, 0.0, top), w, HEAD_D + 0.03, (HAIR, 1), "Head")                # band + fringe
    p.box((0, HEAD_D / 2 - 0.02, 1.00), (0, HEAD_D / 2 - 0.02, top), w, 0.07, (HAIR, 1), "Head", bevel=0.015)  # back
    for sx in (1, -1):   # one block from the temple to the back, above the ear
        p.box((sx * (w / 2 - 0.015), 0.045, 1.115), (sx * (w / 2 - 0.015), 0.045, top), 0.03, 0.21, (HAIR, 1), "Head")
    return p


def build_hat_straw() -> Part:
    p = Part("CHR_Part_Hat_Straw")
    z = HEAD_TOP - 0.015
    p.cylinder((0, 0, z), 0.025, 0.285, 0.285, (FIBRE, 0), "Head")                               # brim
    p.cylinder((0, 0, z + 0.025), 0.11, 0.19, 0.155, (FIBRE, 0), "Head")                         # crown
    p.cylinder((0, 0, z + 0.025), 0.032, 0.196, 0.192, (CLOTH_A, 1), "Head")                     # band
    return p


def build_axe() -> Part:
    p = Part("TOOL_Axe_A")
    f = Vector((0, -1, 0))   # handle direction (ToolSocket Y at rest)
    b = Vector((0, 0, -1))   # blade direction (ToolSocket Z at rest)
    g = GRIP
    p.box(g - f * 0.09, g + f * 0.46, 0.04, 0.04, (LEATHER, 0), "ToolSocket", bevel=0.006)
    head = g + f * 0.40
    p.box(head - b * 0.04, head + b * 0.085, 0.075, 0.036, (METAL, 1), "ToolSocket", hint=tuple(f))
    p.box(head + b * 0.08, head + b * 0.15, 0.085, 0.03, (METAL, 0), "ToolSocket", w1=0.13, d1=0.012, hint=tuple(f))
    return p


# ------------------------------------------------------------------------------------------------ animation

def mirror(pose: dict) -> dict:
    out = {}
    for name, spec in pose.items():
        n = name.replace("Left", "§").replace("Right", "Left").replace("§", "Right")
        s = {}
        for k, val in spec.items():
            if k in ("aim", "up", "move"):
                s[k] = (-val[0], val[1], val[2])
            elif k == "rot":
                s[k] = (val[0], -val[1], -val[2])
        out[n] = s
    return out


def blend(a: dict, b: dict, t: float) -> dict:
    """Per-channel mix of two poses (for in-between keys)."""
    out = {}
    for name in set(a) | set(b):
        sa, sb = a.get(name, {}), b.get(name, {})
        s = {}
        for k in set(sa) | set(sb):
            va = Vector(sa.get(k, (0, 0, 0) if k in ("rot", "move") else sb[k]))
            vb = Vector(sb.get(k, (0, 0, 0) if k in ("rot", "move") else sa[k]))
            if k in ("aim", "up"):
                va, vb = va.normalized(), vb.normalized()
            s[k] = tuple(va.lerp(vb, t))
        out[name] = s
    return out


class Animator:
    def __init__(self, arm: bpy.types.Object):
        self.arm = arm
        self.order = [n for n in BONE_NAMES]   # bone_table lists parents before children
        self.prev: dict[str, Quaternion] = {}

    def begin(self, name: str, frames: int) -> bpy.types.Action:
        act = bpy.data.actions.new(name)
        act.use_fake_user = True
        self.arm.animation_data_create().action = act
        self.prev = {}
        act.frame_range = (0, frames)
        return act

    def key(self, frame: int, pose: dict) -> None:
        pbs = self.arm.pose.bones
        for pb in pbs:
            pb.rotation_mode = "QUATERNION"
            pb.rotation_quaternion = (1, 0, 0, 0)
            pb.location = (0, 0, 0)
        bpy.context.view_layer.update()
        for name in self.order:
            spec = pose.get(name)
            if not spec:
                continue
            pb = pbs[name]
            m0 = pb.matrix.copy()
            r0 = m0.to_3x3().normalized()
            if "aim" in spec:
                d = Vector(spec["aim"]).normalized()
                if "up" in spec:
                    z = Vector(spec["up"])
                    z = (z - d * d.dot(z)).normalized()
                    x = d.cross(z)
                    rot = Matrix((x, d, z)).transposed()
                else:
                    rot = r0.col[1].rotation_difference(d).to_matrix() @ r0
            else:
                rot = r0
            if "rot" in spec:
                rot = Euler([math.radians(c) for c in spec["rot"]], "XYZ").to_matrix() @ rot
            m = Matrix.Translation(m0.translation + Vector(spec.get("move", (0, 0, 0)))) @ rot.to_4x4()
            pb.matrix = m
            bpy.context.view_layer.update()
        for pb in pbs:
            q = pb.rotation_quaternion.copy()
            if pb.name in self.prev and q.dot(self.prev[pb.name]) < 0:
                q.negate()
                pb.rotation_quaternion = q
            self.prev[pb.name] = q
            pb.keyframe_insert("rotation_quaternion", frame=frame, group=pb.name)
            if pb.name == "Hips":
                pb.keyframe_insert("location", frame=frame, group=pb.name)


ARMS_RELAXED = {
    "LeftUpperArm": {"aim": (0.28, 0.0, -1)}, "LeftLowerArm": {"aim": (0.2, -0.15, -1)},
    "RightUpperArm": {"aim": (-0.28, 0.0, -1)}, "RightLowerArm": {"aim": (-0.2, -0.15, -1)},
}


def anim_idle(an: Animator) -> None:
    """2 s loop: breathing and a light sway."""
    an.begin("idle", 60)
    base = dict(ARMS_RELAXED)
    sway_l = {**base, "Spine": {"rot": (0, 1.5, 0)}, "Chest": {"rot": (-1, 0, 0)}, "Head": {"rot": (0, -1.5, 2)}}
    breath = {**base, "Hips": {"move": (0, 0, -0.006)}, "Chest": {"rot": (-2.5, 0, 0)}, "Head": {"rot": (2, 0, 0)},
              "LeftUpperArm": {"aim": (0.33, 0.0, -1)}, "RightUpperArm": {"aim": (-0.33, 0.0, -1)},
              "LeftLowerArm": {"aim": (0.24, -0.15, -1)}, "RightLowerArm": {"aim": (-0.24, -0.15, -1)}}
    sway_r = {**base, "Spine": {"rot": (0, -1.5, 0)}, "Chest": {"rot": (-1, 0, 0)}, "Head": {"rot": (0, 1.5, -2)}}
    for f, p in ((0, base), (15, sway_l), (30, breath), (45, sway_r), (60, base)):
        an.key(f, p)


WALK_CONTACT = {   # right heel strikes in front, left toe pushes behind
    "Hips": {"move": (0, 0, -0.01), "rot": (0, 0, 5)},
    "Spine": {"rot": (5, 0, 0)}, "Chest": {"rot": (0, 0, -9)}, "Head": {"rot": (-4, 0, 4)},
    "RightUpperLeg": {"aim": (0, -0.45, -1)}, "RightLowerLeg": {"aim": (0, -0.32, -1)}, "RightFoot": {"aim": (0, -1, 0.3)},
    "LeftUpperLeg": {"aim": (0, 0.38, -1)}, "LeftLowerLeg": {"aim": (0, 0.8, -1)}, "LeftFoot": {"aim": (0, -1, -0.7)},
    "LeftUpperArm": {"aim": (0.2, -0.55, -1)}, "LeftLowerArm": {"aim": (0.12, -1.0, -0.7)},
    "RightUpperArm": {"aim": (-0.2, 0.5, -1)}, "RightLowerArm": {"aim": (-0.12, 0.4, -1)},
}
WALK_PASS = {      # right leg under the body, left knee up: the hop of the cozy walk
    "Hips": {"move": (0, 0, 0.035)},
    "Spine": {"rot": (3, 0, 0)}, "Head": {"rot": (-2, 0, 0)},
    "RightUpperLeg": {"aim": (0, 0.06, -1)}, "RightLowerLeg": {"aim": (0, 0.1, -1)}, "RightFoot": {"aim": (0, -1, -0.45)},
    "LeftUpperLeg": {"aim": (0, -0.55, -1)}, "LeftLowerLeg": {"aim": (0, 0.55, -1)}, "LeftFoot": {"aim": (0, -1, -0.15)},
    "LeftUpperArm": {"aim": (0.22, -0.08, -1)}, "LeftLowerArm": {"aim": (0.16, -0.3, -1)},
    "RightUpperArm": {"aim": (-0.22, 0.08, -1)}, "RightLowerArm": {"aim": (-0.16, -0.1, -1)},
}


def anim_walk(an: Animator) -> None:
    """0.8 s loop, two steps."""
    an.begin("walk", 24)
    for f, p in ((0, WALK_CONTACT), (6, WALK_PASS), (12, mirror(WALK_CONTACT)), (18, mirror(WALK_PASS)),
                 (24, WALK_CONTACT)):
        an.key(f, p)


CHOP_STANCE = {
    "LeftUpperLeg": {"aim": (0.16, -0.05, -1)}, "LeftLowerLeg": {"aim": (0.08, 0.06, -1)}, "LeftFoot": {"aim": (0.3, -1, -0.4)},
    "RightUpperLeg": {"aim": (-0.16, 0.06, -1)}, "RightLowerLeg": {"aim": (-0.08, 0.1, -1)}, "RightFoot": {"aim": (-0.3, -1, -0.4)},
}
CHOP_READY = {**CHOP_STANCE,
    "Hips": {"move": (0, 0, -0.02)}, "Spine": {"rot": (8, 0, 0)}, "Chest": {"rot": (0, 0, -10)}, "Head": {"rot": (6, 0, 6)},
    "RightUpperArm": {"aim": (-0.25, -0.6, -0.75)}, "RightLowerArm": {"aim": (0.2, -1, 0.05)},
    "LeftUpperArm": {"aim": (0.22, -0.65, -0.7)}, "LeftLowerArm": {"aim": (-0.35, -1, 0.05)},
    "ToolSocket": {"aim": (0.25, -1, 0.6), "up": (1, 0.2, 0.2)},
}
CHOP_WINDUP = {**CHOP_STANCE,   # anticipation: twist right, axe over the right shoulder
    "Hips": {"move": (0, 0, -0.005), "rot": (0, 0, -8)}, "Spine": {"rot": (-5, 0, 0)}, "Chest": {"rot": (0, 0, -38)},
    "Head": {"rot": (0, 0, 28)},
    "RightUpperArm": {"aim": (-0.6, 0.3, 0.4)}, "RightLowerArm": {"aim": (-0.15, 0.3, 1)},
    "LeftUpperArm": {"aim": (-0.35, -0.6, 0.05)}, "LeftLowerArm": {"aim": (-0.55, 0.1, 0.8)},
    "ToolSocket": {"aim": (-0.25, 0.75, 0.6), "up": (0.3, -1, -0.4)},
}
CHOP_IMPACT = {**CHOP_STANCE,   # the blade bites at waist height, in front and a little to the left
    "Hips": {"move": (0, 0, -0.035), "rot": (0, 0, 6)}, "Spine": {"rot": (15, 0, 0)}, "Chest": {"rot": (0, 0, 24)},
    "Head": {"rot": (8, 0, -16)},
    "RightUpperArm": {"aim": (0.15, -0.75, -0.5)}, "RightLowerArm": {"aim": (0.35, -1, -0.3)},
    "LeftUpperArm": {"aim": (0.3, -0.7, -0.6)}, "LeftLowerArm": {"aim": (0.05, -1, -0.3)},
    "ToolSocket": {"aim": (0.3, -1, -0.45), "up": (0.75, 0.15, -0.65)},
}
CHOP_HOLD = {**CHOP_IMPACT,     # recoil on impact
    "Chest": {"rot": (0, 0, 20)}, "ToolSocket": {"aim": (0.3, -1, -0.3), "up": (0.75, 0.15, -0.65)}}


def anim_chop(an: Animator) -> None:
    """1.2 s loop: anticipation (slow) → strike (3 frames) → impact hold → recovery."""
    an.begin("chop", 36)
    keys = [(0, CHOP_READY), (11, CHOP_WINDUP), (13, blend(CHOP_WINDUP, CHOP_IMPACT, 0.45)), (15, CHOP_IMPACT),
            (21, CHOP_HOLD), (28, blend(CHOP_HOLD, CHOP_READY, 0.6)), (36, CHOP_READY)]
    for f, p in keys:
        an.key(f, p)


# ------------------------------------------------------------------------------------------------ checks, export

def tris(ob: bpy.types.Object) -> int:
    return sum(len(p.vertices) - 2 for p in ob.data.polygons)


def check_uvs(ob: bpy.types.Object) -> list[str]:
    bad = []
    uv = ob.data.uv_layers.active.data
    for d in uv:
        cx, cy = d.uv.x * 8 - 0.5, (1 - d.uv.y) * 8 - 0.5
        col, row = round(cx), round(cy)
        if abs(cx - col) > 1e-4 or abs(cy - row) > 1e-4 or not (0 <= row < 8 and 0 <= col < 8) or PALETTE[row][col] is None:
            bad.append(f"{ob.name}: uv {tuple(d.uv)}")
    return bad


def evaluated_bounds(objs: list[bpy.types.Object]) -> tuple[Vector, Vector]:
    dg = bpy.context.evaluated_depsgraph_get()
    pts = []
    for o in objs:
        oe = o.evaluated_get(dg)
        me = oe.to_mesh()
        pts += [oe.matrix_world @ v.co for v in me.vertices]
        oe.to_mesh_clear()
    return (Vector(min(p[i] for p in pts) for i in range(3)), Vector(max(p[i] for p in pts) for i in range(3)))


def export(path: Path, arm: bpy.types.Object, meshes: list[bpy.types.Object], animations: bool) -> None:
    bpy.ops.object.select_all(action="DESELECT")
    for o in [arm, *meshes]:
        o.select_set(True)
    bpy.context.view_layer.objects.active = arm
    arm.data.pose_position = "POSE" if animations else "REST"
    wanted = dict(filepath=str(path), export_format="GLB", use_selection=True, export_yup=True, export_apply=False,
                  export_animations=animations, export_animation_mode="ACTIONS", export_skins=True,
                  export_def_bones=False, export_cameras=False, export_lights=False, export_extras=False,
                  export_materials="EXPORT", export_image_format="AUTO", export_reset_pose_bones=True,
                  export_force_sampling=True, export_frame_range=False, export_morph=False)
    valid = set(bpy.ops.export_scene.gltf.get_rna_type().properties.keys())
    bpy.ops.export_scene.gltf(**{k: v for k, v in wanted.items() if k in valid})
    arm.data.pose_position = "POSE"
    print(f"  glb {path.relative_to(ROOT)}  ({path.stat().st_size // 1024} KiB)")


# ------------------------------------------------------------------------------------------------ godot import

RES = "res://assets/characters/kenney/"
MATERIAL_TRES = f"""[gd_resource type="StandardMaterial3D" load_steps=2 format=3]

[ext_resource type="Texture2D" path="{RES}T_Villager_Palette.png" id="1"]

[resource]
resource_name = "MAT_Villager_Atlas"
albedo_texture = ExtResource("1")
roughness = 1.0
texture_filter = 0
"""


def godot_imports(glbs: list[Path]) -> None:
    """One shared material for every villager glb: nearest filter and no mipmaps on the palette (mipmaps blend
    neighbouring cells at distance = colours off the palette), lossless; the glbs use it as external material and
    discard their embedded copy of the palette. Import files exist after the first Godot import; patch if present."""
    (OUT_GLB / "MAT_Villager_Atlas.tres").write_text(MATERIAL_TRES, encoding="utf-8")
    settings = {
        OUT_GLB / "T_Villager_Palette.png.import": {"compress/mode": "0", "mipmaps/generate": "false",
                                                     "detect_3d/compress_to": "0"},
    }
    sub = ('{"materials": {"MAT_Villager_Atlas": {"use_external/enabled": true, '
           f'"use_external/path": "{RES}MAT_Villager_Atlas.tres"}}}}}}')
    for g in glbs:
        settings[g.with_name(g.name + ".import")] = {"_subresources": sub, "gltf/embedded_image_handling": "0"}
    for path, keys in settings.items():
        if not path.exists():
            print(f"  ! {path.name} missing: run `godot-mono --headless --path godot --import`, then this script again")
            continue
        lines = path.read_text(encoding="utf-8").splitlines()
        for i, line in enumerate(lines):
            k = line.split("=", 1)[0]
            if k in keys:
                lines[i] = f"{k}={keys[k]}"
        path.write_text("\n".join(lines) + "\n", encoding="utf-8")
    for stale in OUT_GLB.glob("*_T_Villager_Palette.png*"):   # copies extracted before the external material
        stale.unlink()


# ------------------------------------------------------------------------------------------------ renders

def render_set(arm: bpy.types.Object, meshes: list[bpy.types.Object]) -> None:
    OUT_IMG.mkdir(parents=True, exist_ok=True)
    rr.setup_render()
    scene = bpy.context.scene
    ground = bpy.data.objects.new("Ground", bpy.data.meshes.new("Ground"))
    ground.data.from_pydata([(-3, -3, 0), (3, -3, 0), (3, 3, 0), (-3, 3, 0)], [], [(0, 1, 2, 3)])
    scene.collection.objects.link(ground)

    # Turnaround on the first idle frame.
    arm.animation_data.action = bpy.data.actions["idle"]
    scene.frame_set(0)
    lo, hi = evaluated_bounds(meshes)
    for view, direction in rr.VIEWS.items():
        rr.camera_shot(meshes, direction, OUT_IMG / f"proto_{view}.png", res=(600, 800), bounds=(lo, hi))

    # Key-frame sheets: one fixed framing per action (union of every sampled frame), two angles.
    for action, frames in (("walk", [0, 3, 6, 9, 12, 15, 18, 21]), ("chop", [0, 6, 11, 13, 15, 18, 21, 28])):
        arm.animation_data.action = bpy.data.actions[action]
        boxes = []
        for f in frames:
            scene.frame_set(f)
            boxes.append(evaluated_bounds(meshes))
        lo = Vector(min(b[0][i] for b in boxes) for i in range(3))
        hi = Vector(max(b[1][i] for b in boxes) for i in range(3))
        for view, direction in (("side", (-1.0, 0.0, 0.12)), ("three_quarter", (-0.75, -1.0, 0.35))):
            tiles = []
            for f in frames:
                scene.frame_set(f)
                tile = OUT_IMG / f"_tmp_{action}_{view}_{f:02d}.png"
                rr.camera_shot(meshes, direction, tile, res=(300, 400), margin=1.1, bounds=(lo, hi))
                tiles.append(tile)
            sheet(tiles, OUT_IMG / f"proto_sheet_{action}_{view}.png")


def sheet(tiles: list[Path], out: Path) -> None:
    """Glue equal-size PNG tiles in one row (numpy inside Blender), then delete the tiles."""
    import numpy as np
    arrays = []
    for t in tiles:
        img = bpy.data.images.load(str(t))
        w, h = img.size
        a = np.empty(w * h * 4, dtype=np.float32)
        img.pixels.foreach_get(a)
        arrays.append(a.reshape(h, w, 4))
        bpy.data.images.remove(img)
        t.unlink()
    row = np.concatenate(arrays, axis=1)
    h, w = row.shape[:2]
    img = bpy.data.images.new(out.stem, w, h, alpha=True)
    img.pixels.foreach_set(row.ravel())
    img.filepath_raw = str(out)
    img.file_format = "PNG"
    img.save()
    print(f"  wrote {out.relative_to(ROOT)}")


# ------------------------------------------------------------------------------------------------ main

def main() -> None:
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.render.fps = FPS
    scene.unit_settings.system = "METRIC"

    png = OUT_GLB / "T_Villager_Palette.png"
    write_palette_png(png)
    mat = palette_material(png)
    arm = build_armature()
    parts = {
        "body": build_body(), "hair": build_hair_short_under_hat(), "hat": build_hat_straw(),
        "torso": build_tunic(), "legs": build_trousers(), "tool": build_axe(),
    }
    objs = {k: p.to_object(arm, mat) for k, p in parts.items()}

    an = Animator(arm)
    anim_idle(an)
    anim_walk(an)
    anim_chop(an)
    arm.animation_data.action = bpy.data.actions["idle"]
    scene.frame_set(0)

    # ---- report
    print("\n== bones (%d)" % len(BONE_NAMES))
    for name, (head, tail, parent, deform) in BONES.items():
        print(f"  {name:14s} parent={parent or '-':14s} head=({head.x:+.3f},{head.y:+.3f},{head.z:+.3f})"
              f"{'' if deform else '  (no deform)'}")
    print("== triangles")
    total = 0
    for k, o in objs.items():
        n = tris(o)
        if k != "tool":
            total += n
        print(f"  {o.name:28s} {n:4d}")
    print(f"  villager without tool          {total:4d}  (budget 300–900)")
    arm.data.pose_position = "REST"
    lo, hi = evaluated_bounds([o for k, o in objs.items() if k != "tool"])
    no_hat = evaluated_bounds([o for k, o in objs.items() if k not in ("tool", "hat")])[1]
    arm.data.pose_position = "POSE"
    print(f"== size (rest): height {hi.z:.3f} m with hat, {no_hat.z:.3f} m without; "
          f"width {hi.x - lo.x:.3f} m (A-pose arms), depth {hi.y - lo.y:.3f} m; floor {lo.z:+.3f}")
    bad = [b for o in objs.values() for b in check_uvs(o)]
    print(f"== palette: {'all UVs on used cells' if not bad else bad[:5]}")
    print(f"== materials: {sorted({m.name for o in objs.values() for m in o.data.materials})}")
    print(f"== actions: {[(a.name, tuple(int(x) for x in a.frame_range)) for a in bpy.data.actions]}")

    # ---- save + export
    OUT_BLEND.parent.mkdir(parents=True, exist_ok=True)
    bpy.context.preferences.filepaths.save_version = 0   # no .blend1 backups next to the versioned source
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT_BLEND))
    print(f"  blend {OUT_BLEND.relative_to(ROOT)}")
    export(OUT_GLB / "CHR_Villager_Proto.glb", arm, list(objs.values()), True)
    export(OUT_GLB / "CHR_Villager_Base_M.glb", arm, [objs["body"]], True)
    for k in ("hair", "hat", "torso", "legs"):
        export(OUT_GLB / f"{objs[k].name.replace('CHR_Part_', 'CHR_Part_')}.glb", arm, [objs[k]], False)
    export(OUT_GLB / "TOOL_Axe_A.glb", arm, [objs["tool"]], False)
    godot_imports(sorted(OUT_GLB.glob("*.glb")))

    if "--no-render" not in argv:
        render_set(arm, list(objs.values()))


if __name__ == "__main__":
    main()
