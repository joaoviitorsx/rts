#!/usr/bin/env python3
"""Draws the villagers' emotion balloons (godot/assets/ui/emotes/EMO_<id>.png) with ImageMagick primitives.

Own placeholder art (no font or emoji dependency on the player's machine): a white speech balloon with a soft
outline and one simple symbol — hunger (bread loaf), cold (snowflake), tired (Zz), happy (heart). Cozy palette.
Usage: python3 tools/assets/build_emotes.py   (needs `magick`)
"""
import subprocess
from pathlib import Path

OUT = Path(__file__).resolve().parents[2] / "godot" / "assets" / "ui" / "emotes"
S = 128
OUTLINE = "#5b4636"

BALLOON = [
    "-fill", "white", "-stroke", OUTLINE, "-strokewidth", "5",
    "-draw", "roundrectangle 8,8 120,100 26,26",
    "-draw", "polygon 50,98 64,122 78,98",
    "-stroke", "none", "-fill", "white", "-draw", "rectangle 52,92 76,99",
]

SYMBOLS = {
    "hunger": ["-fill", "#d8a35a", "-stroke", OUTLINE, "-strokewidth", "4", "-draw", "roundrectangle 30,38 98,78 18,18",
               "-stroke", "#a8743a", "-strokewidth", "4", "-draw", "line 48,44 42,72", "-draw", "line 66,44 60,72",
               "-draw", "line 84,44 78,72"],
    "cold": ["-stroke", "#5a9bd6", "-strokewidth", "7", "-fill", "none",
             "-draw", "line 64,22 64,86", "-draw", "line 36,38 92,70", "-draw", "line 36,70 92,38",
             "-strokewidth", "5", "-draw", "line 64,32 56,24", "-draw", "line 64,32 72,24",
             "-draw", "line 64,76 56,84", "-draw", "line 64,76 72,84"],
    "tired": ["-fill", "#7a6fb0", "-stroke", "none", "-pointsize", "52",
              "-draw", "text 28,80 'Z'", "-pointsize", "36", "-draw", "text 68,58 'z'"],
    "happy": ["-fill", "#e0607a", "-stroke", OUTLINE, "-strokewidth", "4",
              "-draw", "path 'M 64,84 C 20,58 30,24 52,28 C 60,30 64,38 64,42 C 64,38 68,30 76,28 C 98,24 108,58 64,84 Z'"],
}


def main() -> int:
    OUT.mkdir(parents=True, exist_ok=True)
    for name, symbol in SYMBOLS.items():
        out = OUT / f"EMO_{name}.png"
        subprocess.run(["magick", "-size", f"{S}x{S}", "xc:none", *BALLOON, *symbol, str(out)], check=True)
    print(f"{len(SYMBOLS)} emotes -> {OUT}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
