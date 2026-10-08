#!/usr/bin/env python3
"""Generates godot/scenes/test/TEST_VILLAGE_01.tscn — the visual validation village (Etapa 3).

Content (from the owner's brief; Bible §43 not in the repo yet): ~10 houses, hall, woodcutter, farm,
2 fields, granary, warehouse, market, smithy, well, ~20 trees, ~10 rock groups, one road, and 20 villagers
spawned at runtime by TestVillage.cs. Placement is deterministic (fixed RNG seed).
"""
import json
import math
import random
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent))
from build_scenes import Scene, transform, fmt, GODOT  # noqa: E402

OUT = GODOT / "scenes" / "test" / "TEST_VILLAGE_01.tscn"
B = "res://assets/buildings/{}.tscn"
E = "res://assets/environment/{}.tscn"
P = "res://assets/props/{}.tscn"

# (id, center x, center z, rotation, footprint w×d in meters, door side in local space)
BUILDINGS = [
    ("BLD_Hall_A", 128, 114, 0, (6, 6)),
    ("BLD_Market_A", 116, 121, 0, (6, 6)),
    ("BLD_Well_A", 137, 123, 0, (2, 2)),
    ("BLD_Granary_A", 141, 114, 0, (6, 4)),
    ("BLD_Warehouse_A", 152, 113, 0, (6, 6)),
    ("BLD_Smithy_A", 101, 120, 0, (4, 6)),
    ("BLD_Woodcutter_A", 80, 138, 180, (4, 4)),
    ("BLD_Farm_A", 176, 121, 0, (6, 4)),
    ("BLD_Field_A", 172, 142, 0, (8, 8)),
    ("BLD_Field_A", 182, 142, 0, (8, 8)),
    # houses: north of the road face south (rot 0), south of the road face north (rot 180)
    ("BLD_House_T1_A", 92, 122, 0, (4, 4)),
    ("BLD_House_T1_B", 160, 122, 0, (4, 4)),
    ("BLD_House_T1_C", 92, 138, 180, (4, 4)),
    ("BLD_House_T1_A", 100, 138, 180, (4, 4)),
    ("BLD_House_T1_B", 108, 138, 180, (4, 4)),
    ("BLD_House_T1_C", 116, 138, 180, (4, 4)),
    ("BLD_House_T1_A", 140, 138, 180, (4, 4)),
    ("BLD_House_T1_B", 148, 138, 180, (4, 4)),
    ("BLD_House_T1_C", 156, 138, 180, (4, 4)),
    ("BLD_House_T1_A", 164, 112, 0, (4, 4)),
]
ROAD = [(68, 130), (96, 130), (124, 130), (150, 131), (170, 131), (196, 131)]
ROAD_HALL = [(128, 130), (128, 118)]


def curve(scene: Scene, points):
    data = []
    for x, z in points:
        data += [0, 0, 0, 0, 0, 0, x, 0.05, z]
    return scene.subresource("Curve3D", [
        "_data = {\n\"points\": PackedVector3Array(" + ", ".join(fmt(v) for v in data) + "),\n"
        "\"tilts\": PackedFloat32Array(" + ", ".join("0" for _ in points) + ")\n}",
        f"point_count = {len(points)}",
    ])


def main():
    rng = random.Random(1)
    s = Scene("TEST_VILLAGE_01")
    s.root_props.append(f'script = ExtResource("{s.ext_id("res://scenes/test/TestVillage.cs", "Script")}")')

    sky_mat = s.subresource("ProceduralSkyMaterial", ["sky_top_color = Color(0.42, 0.62, 0.88, 1)",
                                                      "sky_horizon_color = Color(0.82, 0.88, 0.94, 1)",
                                                      "ground_horizon_color = Color(0.75, 0.8, 0.7, 1)"])
    sky = s.subresource("Sky", [f'sky_material = SubResource("{sky_mat}")'])
    env = s.subresource("Environment", ["background_mode = 2", f'sky = SubResource("{sky}")', "ambient_light_source = 3",
                                        "ambient_light_energy = 0.9", "tonemap_mode = 2", "ssao_enabled = true",
                                        "glow_enabled = true", "glow_intensity = 0.3"])
    s.node("WorldEnvironment", "WorldEnvironment", [f'environment = SubResource("{env}")'])
    s.node("Sun", "DirectionalLight3D", ["transform = Transform3D(0.819, -0.4698, 0.3289, 0, 0.5736, 0.8192, -0.5736, -0.6709, 0.4698, 0, 20, 0)",
                                          "light_color = Color(1, 0.96, 0.88, 1)", "light_energy = 1.15", "shadow_enabled = true",
                                          "directional_shadow_max_distance = 250.0"])
    terrain_script = s.ext_id("res://game/terrain/VillageTerrain.gd", "Script")
    s.node("VillageTerrain", "Node3D", [f'script = ExtResource("{terrain_script}")', "cover = Rect2(40, 80, 180, 110)"])

    s.node("Road", "Path3D", [f'curve = SubResource("{curve(s, ROAD)}")'])
    s.node("RoadHall", "Path3D", [f'curve = SubResource("{curve(s, ROAD_HALL)}")'])

    s.node("Buildings", "Node3D", [])
    for bid, x, z, rot, (w, d) in BUILDINGS:
        s.instance(B.format(bid), bid, transform((x, 0, z), rot), parent="Buildings",
                   props=[f"metadata/footprint = Vector2({w}, {d})"])

    s.node("Nature", "Node3D", [])
    occupied = [(x, z, max(w, d) / 2 + 2.5) for _, x, z, _, (w, d) in BUILDINGS]

    def free(x, z, r=2.0):
        if any(math.hypot(x - ox, z - oz) < orr + r for ox, oz, orr in occupied):
            return False
        if any(abs(z - rz) < 4.5 and 60 < x < 200 for _, rz in ROAD[:1]) and abs(z - 130.5) < 4.5:
            return False
        return True

    def scatter(ids, count, area, min_gap, scale_range=(0.85, 1.15)):
        placed = 0
        tries = 0
        while placed < count and tries < count * 200:
            tries += 1
            x = rng.uniform(area[0], area[1]); z = rng.uniform(area[2], area[3])
            if not free(x, z, min_gap):
                continue
            occupied.append((x, z, min_gap))
            sid = rng.choice(ids)
            s.instance(E.format(sid), sid, transform((round(x, 2), 0, round(z, 2)), rng.uniform(0, 360),
                                                    round(rng.uniform(*scale_range), 2)), parent="Nature")
            placed += 1

    oaks = [f"ENV_Oak_{c}" for c in "ABCDE"]
    pines = [f"ENV_Pine_{c}" for c in "ABCDE"]
    scatter(oaks + pines + ["ENV_Pine_Big_A", "ENV_Oak_Big_A"], 26, (56, 80, 96, 168), 2.2, (0.8, 1.3))   # western wood
    scatter(pines + ["ENV_Pine_Big_A"], 14, (80, 200, 94, 104), 2.4, (0.8, 1.3))                       # northern belt
    scatter(oaks + ["ENV_Oak_Big_A", "ENV_TwistedTree_A"], 10, (186, 208, 100, 168), 2.5, (0.8, 1.25)) # eastern grove
    scatter(oaks + pines, 9, (95, 165, 146, 168), 2.6, (0.85, 1.2))                                    # southern edge
    scatter(oaks + ["ENV_TwistedTree_B", "ENV_DeadTree_A"], 6, (80, 200, 104, 160), 6.0, (0.85, 1.15)) # lone trees in town
    scatter(["ENV_RockGroup_A", "ENV_RockGroup_B"], 10, (60, 205, 95, 165), 3.0)
    # grass tufts, flowers and pebbles are scattered at runtime with MultiMesh (VillageTerrain.scatter)
    for i in range(14):   # path stones along the road edges
        x = 70 + i * 9 + rng.uniform(-2, 2)
        s.instance(E.format("ENV_PathStone_C"), "ENV_PathStone_C",
                   transform((round(x, 2), 0, round(130.5 + rng.choice([-2.2, 2.2]), 2)), rng.uniform(0, 360)), parent="Nature")

    s.node("Props", "Node3D", [])
    for pid, x, z, rot in [("PROP_Cart_A", 124, 126, 80), ("PROP_Barrel_A", 133, 126, 0), ("PROP_Crate_A", 145, 127, 20),
                           ("PROP_Sack_A", 138, 118, 0), ("PROP_WoodPile_A", 75, 134, 0), ("PROP_Fence_A", 186, 121, 90)]:
        s.instance(P.format(pid), pid, transform((x, 0, z), rot), parent="Props")

    s.node("Villagers", "Node3D", [])
    rig_script = s.ext_id("res://game/scripts/CameraRig.cs", "Script")
    s.node("CameraRig", "Node3D", [f'script = ExtResource("{rig_script}")', "transform = Transform3D(1, 0, 0, 0, 1, 0, 0, 0, 1, 128, 0, 128)"])

    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(s.render(), encoding="utf-8")
    print(f"wrote {OUT.relative_to(GODOT.parent)}: {len(BUILDINGS)} buildings, {len(s.nodes)} nodes")


if __name__ == "__main__":
    main()
