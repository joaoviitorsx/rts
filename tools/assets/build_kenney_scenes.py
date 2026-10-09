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


def main() -> None:
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
              (4.0, 4.0, 4.0), False)
        n += 1
    print(f"{n} Kenney scenes -> {OUT_ENV.relative_to(ROOT)}, {OUT_PROPS.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
