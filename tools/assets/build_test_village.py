#!/usr/bin/env python3
"""Generates TEST_VILLAGE_01 (Etapa 3 visual validation) from the approved ground look-dev rules.

Outputs:
  godot/scenes/test/TEST_VILLAGE_01.tscn        buildings, yard props, rocks, roads (Path3D), camera
  godot/scenes/test/TEST_VILLAGE_01_layout.json  ground shapes (roads, entrances, yards, gardens, fields,
                                                 shade) and forest/tree placements, read by TestVillage.gd
Content per the owner's brief (Bible §43 not in the repo): ~10 houses, hall, woodcutter, farm, 2 fields,
granary, warehouse, market, smithy, well, forests framing the village, ~10 rock groups, one road,
20 villagers (spawned at runtime). Deterministic (fixed seed).
"""
import json
import math
import random
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent))
from build_scenes import Scene, transform, fmt, GODOT  # noqa: E402

OUT = GODOT / "scenes" / "test" / "TEST_VILLAGE_01.tscn"
LAYOUT = GODOT / "scenes" / "test" / "TEST_VILLAGE_01_layout.json"
B = "res://assets/buildings/{}.tscn"
E = "res://assets/environment/{}.tscn"
P = "res://assets/props/{}.tscn"
rng = random.Random(1)

# (id, x, z, facing (0 = door to +Z / south, 180 = north), footprint w×d, door side in local space)
# Door sides of the modular scenes: House A/B S, House C E, woodcutter hut E, others S.
BUILDINGS = [
    ("BLD_Hall_A", 128, 114, 0, (6, 6), "S"),
    ("BLD_Market_A", 116, 121, 0, (6, 6), None),
    ("BLD_Well_A", 137, 123, 0, (2, 2), None),
    ("BLD_Granary_A", 141, 114, 0, (6, 4), "S"),
    ("BLD_Warehouse_A", 152, 113, 0, (6, 6), "S"),
    ("BLD_Smithy_A", 101, 120, 0, (4, 6), "S"),
    ("BLD_Woodcutter_A", 80, 138, 180, (4, 4), "E"),
    ("BLD_Farm_A", 176, 121, 0, (6, 4), "S"),
    ("BLD_Field_A", 172, 142, 0, (8, 8), None),
    ("BLD_Field_A", 182, 142, 0, (8, 8), None),
    ("BLD_House_T1_A", 92, 122, 0, (4, 4), "S"),
    ("BLD_House_T1_B", 160, 122, 0, (4, 4), "S"),
    ("BLD_House_T1_C", 92, 138, 180, (4, 4), "E"),
    ("BLD_House_T1_A", 100, 139, 180, (4, 4), "S"),
    ("BLD_House_T1_B", 108, 138, 180, (4, 4), "S"),
    ("BLD_House_T1_C", 116, 139, 180, (4, 4), "E"),
    ("BLD_House_T1_A", 140, 138, 180, (4, 4), "S"),
    ("BLD_House_T1_B", 148, 139, 180, (4, 4), "S"),
    ("BLD_House_T1_C", 156, 138, 180, (4, 4), "E"),
    ("BLD_House_T1_A", 164, 112, 0, (4, 4), "S"),
]
ROAD = [(56, 130), (78, 131.5), (96, 129.6), (112, 130.8), (124, 129.8), (138, 131.2), (150, 130.4), (170, 131.6), (190, 130.2), (215, 131)]
ROAD_HALL = [(128, 130.2), (127.6, 124), (128.2, 118)]
MASK_RECT = [36, 76, 204, 116]          # x, z, width, depth of the painted ground
ROOF_WEIGHTS = [("thatch", 0.5), ("tile", 0.35), ("slate", 0.15)]
CORE = (82, 104, 196, 160)             # village core (x0, z0, x1, z1) for shadow-casting trees
SIDE_DIR = {"S": (0, 1), "N": (0, -1), "E": (1, 0), "W": (-1, 0)}


def rot(x, z, deg):
    a = math.radians(deg)
    return x * math.cos(a) + z * math.sin(a), -x * math.sin(a) + z * math.cos(a)


def local_to_world(cx, cz, deg, lx, lz):
    rx, rz = rot(lx, lz, deg)
    return cx + rx, cz + rz


def nearest_on_road(px, pz):
    best, bd = None, 1e9
    for road in (ROAD, ROAD_HALL):
        for (ax, az), (bx, bz) in zip(road, road[1:]):
            dx, dz = bx - ax, bz - az
            t = max(0.0, min(1.0, ((px - ax) * dx + (pz - az) * dz) / (dx * dx + dz * dz)))
            qx, qz = ax + t * dx, az + t * dz
            d = math.hypot(px - qx, pz - qz)
            if d < bd:
                best, bd = (qx, qz), d
    return best


def pick_roof():
    r = rng.random()
    for name, w in ROOF_WEIGHTS:
        if r < w:
            return name
        r -= w
    return "tile"


def main():
    s = Scene("TEST_VILLAGE_01")
    s.root_props.append(f'script = ExtResource("{s.ext_id("res://scenes/test/TestVillage.gd", "Script")}")')
    terrain_script = s.ext_id("res://game/terrain/VillageTerrain.gd", "Script")
    s.node("VillageTerrain", "Node3D", [f'script = ExtResource("{terrain_script}")',
                                        f"cover = Rect2({MASK_RECT[0] - 20}, {MASK_RECT[1] - 20}, {MASK_RECT[2] + 40}, {MASK_RECT[3] + 40})"])
    grass_script = s.ext_id("res://game/vegetation/GrassCarpet.gd", "Script")
    s.node("GrassCarpet", "Node3D", [f'script = ExtResource("{grass_script}")'])

    def curve(points):
        data = []
        for x, z in points:
            data += [0, 0, 0, 0, 0, 0, x, 0.05, z]
        return s.subresource("Curve3D", [
            "_data = {\n\"points\": PackedVector3Array(" + ", ".join(fmt(v) for v in data) + "),\n"
            "\"tilts\": PackedFloat32Array(" + ", ".join("0" for _ in points) + ")\n}", f"point_count = {len(points)}"])

    s.node("Road", "Path3D", [f'curve = SubResource("{curve(ROAD)}")'])
    s.node("RoadHall", "Path3D", [f'curve = SubResource("{curve(ROAD_HALL)}")'])

    layout = {"mask_rect": MASK_RECT, "roads": [{"points": ROAD, "width": 2.8}, {"points": ROAD_HALL, "width": 2.2}],
              "dirt": [], "paths": [], "plowed": [], "walls": [], "shade": [], "trees": [], "rocks": [],
              "fences": [], "bush_edges": []}
    occupied = []   # (x, z, radius) for tree/rock placement

    s.node("Buildings", "Node3D", [])
    s.node("Props", "Node3D", [])
    for bid, x, z, facing, (w, d), door in BUILDINGS:
        is_house = bid.startswith("BLD_House")
        jitter = rng.choice([-1, 1]) * rng.uniform(5, 15) if is_house else 0.0
        deg = facing + jitter
        props = [f"metadata/footprint = Vector2({w}, {d})"]
        if is_house or bid in ("BLD_Hall_A", "BLD_Granary_A", "BLD_Warehouse_A", "BLD_Farm_A", "BLD_Smithy_A", "BLD_Woodcutter_A"):
            roof = "slate" if bid == "BLD_Hall_A" else ("tile" if bid in ("BLD_Granary_A", "BLD_Warehouse_A") else pick_roof())
            props.append(f'metadata/roof = "{roof}"')
        s.instance(B.format(bid), bid, transform((x, 0, z), deg), parent="Buildings", props=props)
        occupied.append((x, z, max(w, d) * 0.75 + 2.0))

        if bid == "BLD_Field_A":
            layout["plowed"].append({"center": [x, z], "size": [w - 0.4, d - 0.4], "rot": deg})
            continue
        if bid == "BLD_Market_A":
            layout["dirt"].append({"center": [x, z], "size": [w + 1.5, d + 1.5], "value": 0.8})
            continue
        layout["walls"].append({"center": [x, z], "size": [w, d], "rot": deg})
        if door:
            dx, dz = SIDE_DIR[door]
            reach = (d if dz else w) / 2 + 1.1
            ex, ez = local_to_world(x, z, deg, dx * reach, dz * reach)
            layout["dirt"].append({"center": [ex, ez], "size": [1.8 + (1.5 if not is_house else 0), 1.4], "value": 0.9})
            rx, rz = nearest_on_road(ex, ez)
            if math.hypot(rx - ex, rz - ez) < 14:
                layout["paths"].append({"points": [[ex, ez], [rx, rz]], "width": 1.1, "value": 0.85})
        if bid == "BLD_Hall_A":
            hx, hz = local_to_world(x, z, deg, 0, d / 2 + 3.0)
            layout["dirt"].append({"center": [hx, hz], "size": [7.0, 4.0], "value": 0.75})
        if is_house:
            # Back yard: irregular dirt + 1–3 optional props (fence, garden, firewood, barrel, bucket, bench)
            yx, yz = local_to_world(x, z, deg, 0, -(d / 2 + 2.0))
            layout["dirt"].append({"center": [yx, yz], "size": [w * 0.8, 2.4], "value": 0.7})
            choices = rng.sample(["fence", "garden", "firewood", "barrel", "bucket", "bench"], rng.randint(1, 3))
            if "fence" in choices:
                for lx in (-1.0, 1.0):
                    fx, fz = local_to_world(x, z, deg, lx * 1.05, -(d / 2 + 3.4))
                    s.instance(P.format("PROP_Fence_A"), "PROP_Fence_A", transform((fx, 0, fz), deg), parent="Props")
                    layout["fences"].append([fx, fz])
                for lx in (-1, 1):
                    fx, fz = local_to_world(x, z, deg, lx * 2.1, -(d / 2 + 2.3))
                    s.instance(P.format("PROP_Fence_A"), "PROP_Fence_A", transform((fx, 0, fz), deg + 90), parent="Props")
                    layout["fences"].append([fx, fz])
            if "garden" in choices:
                gx, gz = local_to_world(x, z, deg, rng.choice([-1, 1]) * 2.2, -(d / 2 + 2.4))
                layout["plowed"].append({"center": [gx, gz], "size": [1.8, 1.4], "rot": deg})
                layout.setdefault("gardens", []).append([gx, gz, deg])
            if "firewood" in choices:
                fx, fz = local_to_world(x, z, deg, w / 2 + 0.9, -0.6)
                s.instance(P.format("PROP_ChoppingBlock_A"), "PROP_ChoppingBlock_A", transform((fx, 0, fz), rng.uniform(0, 360)), parent="Props")
                fx2, fz2 = local_to_world(x, z, deg, w / 2 + 0.8, 0.6)
                s.instance(P.format("PROP_Crate_A"), "PROP_Crate_A", transform((fx2, 0, fz2), rng.uniform(0, 360)), parent="Props")
            for item, prop in (("barrel", "PROP_Barrel_A"), ("bucket", "PROP_Bucket_A"), ("bench", "PROP_Bench_A")):
                if item in choices:
                    side = rng.choice([-1, 1])
                    px, pz = local_to_world(x, z, deg, side * (w / 2 + 0.6), rng.uniform(-1.2, 1.2))
                    s.instance(P.format(prop), prop, transform((px, 0, pz), deg + (90 if item == "bench" else rng.uniform(0, 360))), parent="Props")

    for pid, x, z, r in [("PROP_Cart_A", 123, 126.2, 80), ("PROP_Barrel_A", 133, 126, 0), ("PROP_Crate_A", 145, 127, 20),
                         ("PROP_Sack_A", 138, 118, 0), ("PROP_Fence_A", 186.5, 121, 90)]:
        s.instance(P.format(pid), pid, transform((x, 0, z), r), parent="Props")

    def near_road(px, pz, margin):
        q = nearest_on_road(px, pz)
        return math.hypot(px - q[0], pz - q[1]) < margin

    def free(px, pz, r):
        return not near_road(px, pz, 3.5 + r * 0.3) and all(math.hypot(px - ox, pz - oz) > orad + r for ox, oz, orad in occupied)

    # Forest belts framing the village (dense, clumped by noise) + a few groves/lone trees inside.
    oaks = [f"ENV_Oak_{c}" for c in "ABCDE"] + ["ENV_Oak_Big_A"]
    pines = [f"ENV_Pine_{c}" for c in "ABCDE"] + ["ENV_Pine_Big_A"]

    def belt(area, count, kinds, gap, clump=0.0):
        placed, tries = 0, 0
        while placed < count and tries < count * 60:
            tries += 1
            px, pz = rng.uniform(area[0], area[1]), rng.uniform(area[2], area[3])
            if clump and (math.sin(px * 0.09) * math.cos(pz * 0.07) + 0.3) < clump * rng.random():
                continue
            if not free(px, pz, gap):
                continue
            occupied.append((px, pz, gap))
            # Only trees near the village core cast real shadows; deep-forest trees rely on the ground
            # mask's canopy shade (big saving: no alpha-tested leaves in 4 shadow cascades).
            dx = max(CORE[0] - px, 0, px - CORE[2]); dz = max(CORE[1] - pz, 0, pz - CORE[3])
            layout["trees"].append({"scene": E.format(rng.choice(kinds)), "pos": [round(px, 2), round(pz, 2)],
                                    "rot": round(rng.uniform(0, 360), 1), "scale": round(rng.uniform(0.85, 1.25), 2),
                                    "shadow": math.hypot(dx, dz) < 10.0})
            placed += 1

    belt((36, 74, 76, 192), 100, oaks + pines, 1.8, clump=0.55)       # west wood
    belt((74, 218, 76, 101), 95, pines + oaks, 1.8, clump=0.55)      # north belt
    belt((198, 240, 96, 192), 65, oaks + ["ENV_TwistedTree_A"], 1.9, clump=0.55)  # east grove
    belt((74, 198, 160, 192), 65, oaks + pines, 2.0, clump=0.7)     # south edge, gappier
    belt((84, 196, 104, 156), 9, oaks + ["ENV_TwistedTree_B"], 5.5)  # lone trees in town
    for t in layout["trees"]:
        layout["shade"].append({"center": t["pos"], "radius": 3.4 * t["scale"], "strength": 0.75})

    s.node("Rocks", "Node3D", [])
    placed = 0
    while placed < 10:
        px, pz = rng.uniform(70, 205), rng.uniform(100, 162)
        if not free(px, pz, 2.5):
            continue
        occupied.append((px, pz, 2.5))
        rid = rng.choice(["ENV_RockGroup_A", "ENV_RockGroup_B"])
        s.instance(E.format(rid), rid, transform((round(px, 2), -0.35, round(pz, 2)), rng.uniform(0, 360), round(rng.uniform(1.0, 1.4), 2)), parent="Rocks")
        layout["rocks"].append([round(px, 2), round(pz, 2)])
        layout["shade"].append({"center": [px, pz], "radius": 2.4, "strength": 0.6})
        placed += 1

    # Deep forest beyond the painted ground (after the rocks so the approved village layout keeps its RNG draws).
    # Always far from the camera bounds, so at play distances it renders as impostors (TreeImpostors.gd).
    belt((4, 36, 40, 236), 170, oaks + pines, 1.7, clump=0.35)       # deep west
    belt((36, 252, 26, 76), 230, pines + oaks, 1.7, clump=0.35)      # deep north
    belt((36, 252, 192, 238), 170, oaks + pines, 1.8, clump=0.45)    # deep south
    belt((240, 254, 76, 192), 45, oaks + pines, 1.8, clump=0.35)     # deep east
    # Fill the framing belts on the painted ground (appended, so earlier placements stay where they were).
    n_before = len(layout["trees"])
    belt((36, 74, 76, 192), 55, oaks + pines, 1.7, clump=0.4)        # west wood
    belt((74, 218, 76, 98), 60, pines + oaks, 1.7, clump=0.4)        # north belt
    belt((198, 240, 96, 192), 35, oaks + pines, 1.8, clump=0.45)     # east grove
    belt((74, 198, 166, 192), 30, oaks + pines, 1.9, clump=0.6)      # south edge
    for t in layout["trees"][n_before:]:
        layout["shade"].append({"center": t["pos"], "radius": 3.4 * t["scale"], "strength": 0.75})

    s.node("Villagers", "Node3D", [])
    rig = s.ext_id("res://game/scripts/CameraRig.cs", "Script")
    s.node("CameraRig", "Node3D", [f'script = ExtResource("{rig}")', "transform = Transform3D(1, 0, 0, 0, 1, 0, 0, 0, 1, 130, 0, 128)"])

    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(s.render(), encoding="utf-8")
    LAYOUT.write_text(json.dumps(layout, indent=0), encoding="utf-8")
    print(f"wrote {OUT.name} ({len(BUILDINGS)} buildings, {len(s.nodes)} nodes) and {LAYOUT.name} "
          f"({len(layout['trees'])} trees, {len(layout['dirt'])} dirt shapes, {len(layout['fences'])} fences)")


if __name__ == "__main__":
    main()
