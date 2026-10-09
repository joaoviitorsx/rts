#!/usr/bin/env python3
"""Asset scenes for the Kenney art direction (decision 09/10/2026): one scene per model of the Nature Kit 2.1 (all of it)
and of the Survival Kit, so the game only references godot/assets/<category>/ (never vendor files).

  godot/assets/environment/kenney/K_<model>.tscn   Nature Kit — ×4 (props/trees), cliffs ×(2, 3, 2): one module =
                                                    one 2-m cell, one terrace step (3 m, Ground.LevelHeight)
  godot/assets/props/kenney/KS_<model>.tscn        Survival Kit — ×4

Each scene: a Node3D root with the vendor model instanced as a child (scale lives here) and the KenneyPaletteNode
script, which recolours the Nature Kit's turquoise/orange roles to the cozy palette at runtime (vendor untouched).
MultiMesh users (the world's NatureLook) read the first mesh + transform and recolour the mesh copy themselves.

Usage: python3 tools/assets/build_kenney_scenes.py
"""
from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
GODOT = ROOT / "godot"
NATURE = GODOT / "assets" / "vendor" / "kenney_nature_kit"
SURVIVAL = GODOT / "assets" / "vendor" / "kenney_survival_kit"
OUT_ENV = GODOT / "assets" / "environment" / "kenney"
OUT_PROPS = GODOT / "assets" / "props" / "kenney"
OUT_BLD = GODOT / "assets" / "buildings"
PALETTE_SCRIPT = "res://game/visual/KenneyPaletteNode.gd"

SKIP = ("palm", "cactus", "canoe", "bed", "statue", "platform_", "ground_", "bridge_")


def scene(path: Path, model_res: str, name: str, scale: tuple[float, float, float], palette: bool) -> None:
    lines = [f'[gd_scene load_steps={3 if palette else 2} format=3]', "",
             f'[ext_resource type="PackedScene" path="{model_res}" id="1_model"]']
    if palette:
        lines.append(f'[ext_resource type="Script" path="{PALETTE_SCRIPT}" id="2_palette"]')
    lines += ["", f'[node name="{name}" type="Node3D"]']
    if palette:
        lines.append('script = ExtResource("2_palette")')
    lines += ["", f'[node name="Model" parent="." instance=ExtResource("1_model")]',
              f"transform = Transform3D({scale[0]}, 0, 0, 0, {scale[1]}, 0, 0, 0, {scale[2]}, 0, 0, 0)", ""]
    path.write_text("\n".join(lines), encoding="utf-8")


# RTS opening buildings (briefing step 3), pivot at the footprint centre, 1 cell = 2 m. Parts reference the asset
# scenes above (K_ = Nature Kit ×4, KS_ = Survival Kit ×4): (scene, (x, y, z) metres, yaw degrees, extra scale).
OPENING = {
    "BLD_Pile_A": [   # 2×2: open heap of logs, crates and a barrel (rain spoils it)
        ("K_log_stack", (-0.7, 0, -0.2), 90, 0.75), ("KS_box-large", (0.9, 0, -0.7), 15, 1.0),
        ("KS_box", (1.2, 0, 0.3), -20, 1.0), ("KS_barrel", (0.6, 0, 1.1), 0, 1.0),
        ("KS_bedroll-packed", (-0.6, 0, 1.2), 30, 1.0), ("KS_resource-wood", (0.2, 0, 0.2), 60, 1.2)],
    "BLD_Campfire_A": [   # 1×1: stone ring, logs and a spit
        ("K_campfire_stones", (0, 0, 0), 0, 0.85), ("K_campfire_logs", (0, 0, 0), 30, 1.6),
        ("KS_campfire-stand", (0, 0, 0), 0, 1.0), ("KS_resource-wood", (0.75, 0, 0.6), 40, 1.0)],
    "BLD_Tent_A": [   # 2×2: canvas tent and bedrolls
        ("K_tent_detailedOpen", (0, 0, -0.2), 0, 1.0), ("KS_bedroll", (1.1, 0, 1.4), 80, 1.0)],
    "BLD_Gatherer_A": [   # 2×2: a lean-to with baskets of berries and mushrooms
        ("KS_structure", (0.2, 0, -0.4), 0, 1.6), ("KS_structure-canvas", (0.2, 0, -0.4), 0, 1.6),
        ("KS_bucket", (1.0, 0, 1.0), 0, 1.2), ("KS_box-open", (-0.9, 0, 1.0), 20, 1.0), ("KS_box-large-open", (1.2, 0, 0.0), -10, 1.0),
        ("K_mushroom_redGroup", (-1.4, 0, 0.2), 0, 0.8), ("K_plant_bushSmall", (-1.3, 0, -1.3), 0, 0.8)],
    "BLD_HuntingCamp_A": [   # 2×2: hide-drying frame, a small fire and the skinning bench
        ("KS_tent", (-0.4, 0, -0.5), 30, 1.2), ("KS_tent-canvas", (-0.4, 0, -0.5), 30, 1.2),
        ("KS_campfire-pit", (0.9, 0, 0.8), 0, 1.4), ("KS_workbench", (1.0, 0, -0.9), -90, 1.0)],
    "BLD_Depot_A": [   # 2×2: a roof on posts sheltering the stock
        ("KS_structure-roof", (0.68, 0, 0.7), 0, 1.25), ("K_log_stackLarge", (-0.4, 0, -0.5), 90, 0.6),
        ("KS_box-large", (0.9, 0, -0.6), 0, 1.0), ("KS_barrel", (0.9, 0, 0.6), 0, 1.0), ("KS_box", (-0.6, 0, 0.9), 10, 1.0)],
}


def composite(name: str, parts) -> None:
    import math
    lines = [f"[gd_scene load_steps={len(parts) + 1} format=3]", ""]
    ids = {}
    for scene_name, *_ in parts:
        if scene_name in ids:
            continue
        ids[scene_name] = f"{len(ids) + 1}_{scene_name}"
        folder = "environment/kenney" if scene_name.startswith("K_") else "props/kenney"
        lines.append(f'[ext_resource type="PackedScene" path="res://assets/{folder}/{scene_name}.tscn" id="{ids[scene_name]}"]')
    lines += ["", f'[node name="{name}" type="Node3D"]', ""]
    for i, (scene_name, (x, y, z), yaw, sc) in enumerate(parts):
        c, s_ = math.cos(math.radians(yaw)) * sc, math.sin(math.radians(yaw)) * sc
        lines += [f'[node name="{scene_name}_{i}" parent="." instance=ExtResource("{ids[scene_name]}")]',
                  f"transform = Transform3D({c:.4f}, 0, {-s_:.4f}, 0, {sc}, 0, {s_:.4f}, 0, {c:.4f}, {x}, {y}, {z})", ""]
    (OUT_BLD / f"{name}.tscn").write_text("\n".join(lines), encoding="utf-8")


def cozy_atlas() -> None:
    """Survival Kit colormap → cozy: saturated oranges become warm wood, reds a muted brick, blues a soft slate."""
    import colorsys
    from PIL import Image
    src = Image.open(SURVIVAL / "Textures" / "colormap.png").convert("RGBA")
    out = src.copy()
    px = out.load()
    for y in range(out.height):
        for x in range(out.width):
            r, g, b, a = px[x, y]
            h, s, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
            deg = h * 360
            if s > 0.35 and 12 <= deg <= 48:        # orange → wood
                h, s, v = 28 / 360, s * 0.62, v * 0.78
            elif s > 0.35 and (deg < 12 or deg > 340):   # red → brick
                s, v = s * 0.75, v * 0.82
            elif s > 0.3 and 190 <= deg <= 260:        # blue → slate
                s, v = s * 0.55, v * 0.9
            elif s > 0.15 and 20 <= deg <= 45 and v > 0.9:   # peach → canvas
                s = s * 0.8
            r2, g2, b2 = colorsys.hsv_to_rgb(h, s, v)
            px[x, y] = (round(r2 * 255), round(g2 * 255), round(b2 * 255), a)
    out.save(OUT_PROPS / "survival_colormap_cozy.png")


def main() -> None:
    cozy_atlas()
    OUT_ENV.mkdir(parents=True, exist_ok=True)
    OUT_PROPS.mkdir(parents=True, exist_ok=True)
    n = 0
    for glb in sorted(NATURE.glob("*.glb")):
        stem = glb.stem
        if any(stem.startswith(s) or s in stem for s in SKIP):
            continue
        scale = (2.0, 3.0, 2.0) if stem.startswith("cliff_") else (4.0, 4.0, 4.0)
        scene(OUT_ENV / f"K_{stem}.tscn", f"res://assets/vendor/kenney_nature_kit/{glb.name}", f"K_{stem}", scale, True)
        n += 1
    for glb in sorted(SURVIVAL.glob("*.glb")):
        if glb.stem.startswith(("metal-", "structure-metal", "floor")):
            continue
        scene(OUT_PROPS / f"KS_{glb.stem}.tscn", f"res://assets/vendor/kenney_survival_kit/{glb.name}", f"KS_{glb.stem}",
              (4.0, 4.0, 4.0), True)
        n += 1
    for name, parts in OPENING.items():
        composite(name, parts)
    print(f"{len(OPENING)} opening buildings -> {OUT_BLD.relative_to(ROOT)}")
    print(f"{n} Kenney scenes -> {OUT_ENV.relative_to(ROOT)}, {OUT_PROPS.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
