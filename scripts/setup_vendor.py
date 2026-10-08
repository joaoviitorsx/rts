#!/usr/bin/env python3
"""Copy the glTF/GLB files (and the buffers/textures they reference) from the untouched
third-party packs in art/vendor_raw/ into the Godot project at godot/assets/vendor/<pack_id>/.

- Never modifies art/vendor_raw/ (read-only use).
- Never copies FBX/OBJ/Blend: Godot must only import glTF/GLB.
- Idempotent: files are copied only when missing or different (size + content hash).
- Safe to re-run after adding a pack to art/vendor_raw/.

Usage:
    python3 scripts/setup_vendor.py            # copy/update
    python3 scripts/setup_vendor.py --dry-run  # show what would change
    python3 scripts/setup_vendor.py --clean    # also delete files in vendor/ that no source produces

Sources and versions of each pack: docs/vendor_sources.md
"""
from __future__ import annotations

import argparse
import fnmatch
import hashlib
import json
import shutil
import struct
import sys
from pathlib import Path
from urllib.parse import unquote

ROOT = Path(__file__).resolve().parent.parent
RAW = ROOT / "art" / "vendor_raw"
DST = ROOT / "godot" / "assets" / "vendor"

MODEL_EXT = {".gltf", ".glb"}
IMAGE_EXT = {".png", ".jpg", ".jpeg", ".webp", ".tga", ".exr"}
SKIP_DIR_WORDS = ("fbx", "obj", "unity", "blend")

# id -> (glob for the raw folder, [(source subdir, destination subdir, kind)])
# kind: "models" = .gltf/.glb in that dir (non-recursive) + their dependencies
#       "models_recursive" = same, walking subdirs that are not FBX/OBJ/Unity
#       "images_recursive" = every image file (texture-only packs)
PACKS: dict[str, tuple[str, list[tuple[str, str, str]]]] = {
    "quaternius_stylized_nature": ("Stylized Nature MegaKit*", [("glTF", "", "models")]),
    "quaternius_medieval_village": ("Medieval Village MegaKit*", [("glTF", "", "models")]),
    "quaternius_fantasy_props": ("Fantasy Props MegaKit*", [("Exports/glTF", "", "models")]),
    "quaternius_base_characters": ("Universal Base Characters*", [
        ("Base Characters/Godot - UE", "bodies", "models"),
        ("Hairstyles/Rigged to Head Bone/glTF (Godot -Unreal)", "hair_rigged", "models"),
        ("Hairstyles/Origin at 0/glTF (Godot)", "hair_static", "models"),
    ]),
    "quaternius_outfits_fantasy": ("Modular Character Outfits - Fantasy*", [
        ("Exports/glTF (Godot-Unreal)/Outfits", "outfits", "models"),
        ("Exports/glTF (Godot-Unreal)/Modular Parts", "parts", "models"),
    ]),
    "quaternius_ual1": ("Universal Animation Library[[]*", [("Unreal-Godot", "", "models")]),
    "quaternius_ual2": ("Universal Animation Library 2*", [
        ("Unreal-Godot", "", "models"),
        ("Female Mannequin/Unreal-Godot", "female_mannequin", "models"),
    ]),
    # Layout unknown until downloaded: discovered recursively.
    "kaykit_resource_bits": ("*Resource*Bits*", [("", "", "models_recursive")]),
    "watercolor_terrain_textures": ("*atercolor*", [("", "", "images_recursive")]),
}


def gltf_dependencies(model: Path) -> list[Path]:
    """External files (buffers, images) referenced by a .gltf or .glb."""
    if model.suffix.lower() == ".glb":
        data = model.read_bytes()
        if data[:4] != b"glTF":
            raise ValueError(f"{model}: not a GLB")
        json_len = struct.unpack("<I", data[12:16])[0]
        doc = json.loads(data[20:20 + json_len])
    else:
        doc = json.loads(model.read_text(encoding="utf-8"))
    deps = []
    for entry in doc.get("buffers", []) + doc.get("images", []):
        uri = entry.get("uri")
        if uri and not uri.startswith("data:"):
            deps.append((model.parent / unquote(uri)).resolve())
    return deps


def skip_dir(path: Path) -> bool:
    return any(word in part.lower() for part in path.parts for word in SKIP_DIR_WORDS)


def plan_pack(pack_id: str, raw_dir: Path, entries) -> dict[Path, Path]:
    """Returns {destination: source}."""
    plan: dict[Path, Path] = {}
    for src_sub, dst_sub, kind in entries:
        src = raw_dir / src_sub if src_sub else raw_dir
        dst = DST / pack_id / dst_sub if dst_sub else DST / pack_id
        if not src.is_dir():
            print(f"  ! {pack_id}: missing folder '{src_sub}'", file=sys.stderr)
            continue
        if kind == "images_recursive":
            for f in sorted(src.rglob("*")):
                if f.is_file() and f.suffix.lower() in IMAGE_EXT and not skip_dir(f.relative_to(src).parent):
                    plan[dst / f.relative_to(src)] = f
            continue
        files = src.rglob("*") if kind == "models_recursive" else src.glob("*")
        for model in sorted(files):
            if not model.is_file() or model.suffix.lower() not in MODEL_EXT:
                continue
            rel_dir = model.parent.relative_to(src)
            if skip_dir(rel_dir):
                continue
            plan[dst / rel_dir / model.name] = model
            for dep in gltf_dependencies(model):
                source = dep
                if not source.is_file():
                    # Known pack bug: the .gltf asks for 'X_png.png' but ships 'X.png'.
                    # Copy the real file under the expected name (vendor_raw stays untouched).
                    alt = dep.with_name(dep.name.replace("_png.png", ".png"))
                    if alt != dep and alt.is_file():
                        print(f"  ~ {model.name}: '{dep.name}' missing, using '{alt.name}'")
                        source = alt
                    else:
                        print(f"  ! {model.name}: missing dependency {dep.name}", file=sys.stderr)
                        continue
                try:
                    rel = dep.relative_to(model.parent)
                except ValueError:
                    # Dependency outside the model folder (e.g. ../Textures): keep the uri layout working
                    # by mirroring it next to the copied model would escape dst; flatten instead.
                    print(f"  ! {model.name}: dependency outside folder ({dep}); flattened", file=sys.stderr)
                    rel = Path(dep.name)
                plan[dst / rel_dir / rel] = source
    return plan


def same_file(a: Path, b: Path) -> bool:
    if not b.exists() or a.stat().st_size != b.stat().st_size:
        return False
    return hashlib.sha256(a.read_bytes()).digest() == hashlib.sha256(b.read_bytes()).digest()


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--dry-run", action="store_true")
    parser.add_argument("--clean", action="store_true", help="delete files in vendor/ no source produces")
    args = parser.parse_args()

    if not RAW.is_dir():
        print(f"error: {RAW} not found", file=sys.stderr)
        return 1

    full_plan: dict[Path, Path] = {}
    for pack_id, (pattern, entries) in PACKS.items():
        matches = [d for d in sorted(RAW.iterdir()) if d.is_dir() and fnmatch.fnmatch(d.name, pattern)]
        if not matches:
            print(f"- {pack_id}: not in art/vendor_raw/ (skipped)")
            continue
        if len(matches) > 1:
            print(f"error: {pack_id}: pattern '{pattern}' matches {[m.name for m in matches]}", file=sys.stderr)
            return 1
        plan = plan_pack(pack_id, matches[0], entries)
        full_plan.update(plan)
        print(f"- {pack_id}: {len(plan)} files from '{matches[0].name}'")

    copied = unchanged = 0
    for dst, src in sorted(full_plan.items()):
        if same_file(src, dst):
            unchanged += 1
            continue
        copied += 1
        if args.dry_run:
            print(f"  would copy {dst.relative_to(ROOT)}")
            continue
        dst.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(src, dst)

    removed = 0
    if args.clean and DST.is_dir():
        keep = set(full_plan)
        for f in sorted(DST.rglob("*")):
            # Godot writes *.import sidecars next to assets; keep those of files we manage.
            managed = f.with_suffix("") if f.suffix == ".import" else f
            if f.is_file() and managed not in keep:
                removed += 1
                if args.dry_run:
                    print(f"  would delete {f.relative_to(ROOT)}")
                else:
                    f.unlink()

    verb = "would copy" if args.dry_run else "copied"
    print(f"{verb} {copied}, unchanged {unchanged}" + (f", removed {removed}" if args.clean else ""))
    return 0


if __name__ == "__main__":
    sys.exit(main())
