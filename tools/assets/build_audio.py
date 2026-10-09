#!/usr/bin/env python3
"""Generates the game's sound resources (godot/assets/audio/SFX_<id>.tres) from the vendor audio copies.

Each logical sound id (what the code plays: Sfx.Play("ui_click")) is an AudioStreamRandomizer over a few vendor
variations with a little pitch/volume variation, so repeated sounds don't feel mechanical. The game references only
these resources, never vendor files (same rule as the model scenes). Placeholder sounds: Kenney audio packs (CC0).

Usage: python3 tools/assets/build_audio.py
"""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "godot" / "assets" / "audio"
UI = "res://assets/vendor/kenney_interface_sounds/{}.ogg"
IMPACT = "res://assets/vendor/kenney_impact_sounds/{}.ogg"
RPG = "res://assets/vendor/kenney_rpg_audio/{}.ogg"

# id: (files, random_pitch, volume_db) — UI_UX_guide §5.3 table.
SOUNDS = {
    "ui_hover": ([UI.format("tick_002"), UI.format("tick_004")], 1.05, -14.0),               # soft click
    "ui_click": ([IMPACT.format(f"impactWood_light_00{i}") for i in range(5)], 1.08, -6.0),   # wooden "toc"
    "panel_open": ([UI.format("open_002")], 1.04, -10.0),
    "panel_close": ([UI.format("close_002")], 1.04, -10.0),
    "build_snap": ([UI.format("tick_001")], 1.06, -16.0),                                   # ghost snaps to a cell
    "build_place": ([IMPACT.format(f"impactWood_medium_00{i}") for i in range(5)], 1.06, -4.0),  # hammer
    "road_place": ([IMPACT.format(f"impactMining_00{i}") for i in range(5)], 1.06, -8.0),
    "build_error": ([IMPACT.format(f"impactSoft_medium_00{i}") for i in range(3)], 1.02, -6.0),  # muffled
    "site_complete": ([IMPACT.format(f"impactWood_heavy_00{i}") for i in range(3)], 1.03, -3.0),  # pop
    "decree_stamp": ([IMPACT.format(f"impactPlate_light_00{i}") for i in range(3)], 1.03, -6.0),  # stamp
    "suggestion": ([RPG.format("bookOpen")], 1.0, -6.0),                                     # the reeve's book
    "alert": ([UI.format("question_002")], 1.0, -10.0),
}


def tres(files: list[str], pitch: float, volume_db: float) -> str:
    lines = [f'[gd_resource type="AudioStreamRandomizer" load_steps={len(files) + 1} format=3]', ""]
    for i, f in enumerate(files, 1):
        lines.append(f'[ext_resource type="AudioStream" path="{f}" id="{i}"]')
    lines += ["", "[resource]", f"random_pitch = {pitch}", "random_volume_offset_db = 1.0",
              f"streams_count = {len(files)}"]
    for i in range(len(files)):
        lines += [f'stream_{i}/stream = ExtResource("{i + 1}")', f"stream_{i}/weight = 1.0"]
    return "\n".join(lines) + "\n"


def main() -> int:
    OUT.mkdir(parents=True, exist_ok=True)
    for sid, (files, pitch, vol) in SOUNDS.items():
        for f in files:
            if not (ROOT / "godot" / f.removeprefix("res://")).is_file():
                raise SystemExit(f"{sid}: missing {f} (run scripts/setup_vendor.py)")
        (OUT / f"SFX_{sid}.tres").write_text(tres(files, pitch, vol))
    # volume per sound lives next to the streams (read by game/audio/Sfx.cs)
    (OUT / "sfx_volumes.json").write_text("{\n" + ",\n".join(f'  "{k}": {v[2]}' for k, v in SOUNDS.items()) + "\n}\n")
    print(f"{len(SOUNDS)} sounds -> {OUT.relative_to(ROOT)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
