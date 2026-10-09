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
    python3 scripts/setup_vendor.py --derive   # also (re)build derived files: Blender (head-only bodies) and
                                               # Godot (tree impostor atlases; needs a GPU window, not headless)

Sources and versions of each pack: docs/vendor_sources.md
"""
from __future__ import annotations

import argparse
import fnmatch
import hashlib
import json
import shutil
import shutil as _sh
import struct
import subprocess
import sys
from pathlib import Path
from urllib.parse import unquote

ROOT = Path(__file__).resolve().parent.parent
RAW = ROOT / "art" / "vendor_raw"
DST = ROOT / "godot" / "assets" / "vendor"

MODEL_EXT = {".gltf", ".glb"}
IMAGE_EXT = {".png", ".jpg", ".jpeg", ".webp", ".tga", ".exr"}
AUDIO_EXT = {".ogg", ".wav"}
SKIP_DIR_WORDS = ("fbx", "obj", "unity", "blend", "__macosx")

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
    # UI art (F3): Kenney UI Pack RPG Extension — PNG only (vector/swf/spritesheet stay in vendor_raw).
    "kenney_ui_rpg": ("UIpack_RPG", [("PNG", "", "images_recursive")]),
    # Placeholder sounds (2B polish): Kenney audio packs, CC0 — .ogg only.
    "kenney_interface_sounds": ("kenney_interface-sounds", [("Audio", "", "audio_recursive")]),
    "kenney_impact_sounds": ("kenney_impact-sounds", [("Audio", "", "audio_recursive")]),
    "kenney_rpg_audio": ("kenney_rpg-audio", [("Audio", "", "audio_recursive")]),
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
        if kind == "audio_recursive":
            for f in sorted(src.rglob("*")):
                if f.is_file() and f.suffix.lower() in AUDIO_EXT and not f.name.startswith("._"):
                    plan[dst / f.relative_to(src)] = f
            continue
        if kind == "images_recursive":
            for f in sorted(src.rglob("*")):
                if f.name.startswith("._"):      # macOS AppleDouble metadata, not images
                    continue
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


# Godot addons installed from art/vendor_raw (PC only: Windows/Linux x86_64 binaries).
ADDONS = [
    # (glob for the raw folder, addon folder inside it, destination, binary name filters to keep)
    ("Terrain3D_v*", "addons/terrain_3d", "godot/addons/terrain_3d", ("windows.", "linux.")),
]
ADDON_ARCH_SKIP = ("arm64", "rv64", "arm32")


def install_addons(dry_run: bool) -> int:
    for pattern, sub, dst, keep in ADDONS:
        matches = sorted(d for d in RAW.iterdir() if d.is_dir() and fnmatch.fnmatch(d.name, pattern))
        if not matches:
            print(f"- addon {dst}: not in art/vendor_raw/ (skipped)")
            continue
        src = matches[-1] / sub
        copied = 0
        for f in sorted(src.rglob("*")):
            if not f.is_file():
                continue
            rel = f.relative_to(src)
            if rel.parts[0] == "bin" and (not any(k in f.name for k in keep) or any(a in f.name for a in ADDON_ARCH_SKIP)):
                continue
            out = ROOT / dst / rel
            if same_file(f, out):
                continue
            copied += 1
            if not dry_run:
                out.parent.mkdir(parents=True, exist_ok=True)
                shutil.copy2(f, out)
        print(f"- addon {dst} from '{matches[-1].name}': {copied} files {'to copy' if dry_run else 'copied'}")
    return 0


# Derived assets built with Blender from the vendor copies (never from/into art/vendor_raw).
DERIVED = [
    ("tools/blender/make_head_only.py",
     "godot/assets/vendor/quaternius_base_characters/bodies/Superhero_Male_FullBody.gltf",
     "godot/assets/characters/source/CHR_Base_Male_HeadOnly.glb"),
    ("tools/blender/make_head_only.py",
     "godot/assets/vendor/quaternius_base_characters/bodies/Superhero_Female_FullBody.gltf",
     "godot/assets/characters/source/CHR_Base_Female_HeadOnly.glb"),
]


def blender_command() -> list[str] | None:
    if _sh.which("blender"):
        return ["blender"]
    if _sh.which("flatpak") and subprocess.run(["flatpak", "info", "org.blender.Blender"],
                                               capture_output=True).returncode == 0:
        return ["flatpak", "run", "org.blender.Blender"]
    return None


def derive(force: bool) -> int:
    blender = blender_command()
    for script, src, dst in DERIVED:
        out = ROOT / dst
        if out.exists() and not force:
            print(f"- derived {dst}: up to date")
            continue
        if blender is None:
            print(f"error: Blender not found (needed for {dst})", file=sys.stderr)
            return 1
        out.parent.mkdir(parents=True, exist_ok=True)
        cmd = blender + ["-b", "--python", str(ROOT / script), "--", str(ROOT / src), str(out)]
        result = subprocess.run(cmd, capture_output=True, text=True)
        ok = result.returncode == 0 and out.exists()
        print(f"- derived {dst}: {'built' if ok else 'FAILED'}")
        if not ok:
            print(result.stdout[-2000:], result.stderr[-2000:], file=sys.stderr)
            return 1
    return bake_impostors(force)


# Tree impostor atlases rendered by Godot from the tree scenes (needs a real GPU render, so not --headless).
IMPOSTORS = "godot/assets/environment/impostors/impostors.json"


def bake_impostors(force: bool) -> int:
    if (ROOT / IMPOSTORS).exists() and not force:
        print(f"- derived {IMPOSTORS}: up to date")
        return 0
    godot = next((g for g in ("godot-mono", "godot") if _sh.which(g)), None)
    if godot is None:
        print("error: godot-mono not found (needed for tree impostors)", file=sys.stderr)
        return 1
    project = str(ROOT / "godot")
    steps = [[godot, "--headless", "--path", project, "--import"],                       # meshes the baker loads
             [godot, "--path", project, "res://scenes/tools/ImpostorBaker.tscn"],
             [godot, "--headless", "--path", project, "--import"]]                       # the new atlases
    for cmd in steps:
        result = subprocess.run(cmd, capture_output=True, text=True)
        if result.returncode != 0:
            print(result.stdout[-2000:], result.stderr[-2000:], file=sys.stderr)
            break
    ok = (ROOT / IMPOSTORS).exists()
    print(f"- derived {IMPOSTORS}: {'built' if ok else 'FAILED'}")
    return 0 if ok else 1


# Godot import presets per pack, written as <file>.import only if Godot hasn't imported the file yet.
# UI art must stay crisp: lossless, no mipmaps, no size limit (UI_UX_guide §8.3).
IMPORT_PRESETS = {
    "kenney_ui_rpg": ('[remap]\n\nimporter="texture"\ntype="CompressedTexture2D"\n\n[params]\n\n'
                      'compress/mode=0\nmipmaps/generate=false\nprocess/size_limit=0\ndetect_3d/compress_to=0\n'),
}


def write_import_presets(dry_run: bool) -> int:
    written = 0
    for pack_id, preset in IMPORT_PRESETS.items():
        root = DST / pack_id
        if not root.is_dir():
            continue
        for f in root.rglob("*"):
            if f.is_file() and f.suffix.lower() in IMAGE_EXT and not f.with_name(f.name + ".import").exists():
                written += 1
                if not dry_run:
                    f.with_name(f.name + ".import").write_text(preset)
    return written


def same_file(a: Path, b: Path) -> bool:
    if not b.exists() or a.stat().st_size != b.stat().st_size:
        return False
    return hashlib.sha256(a.read_bytes()).digest() == hashlib.sha256(b.read_bytes()).digest()


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--dry-run", action="store_true")
    parser.add_argument("--clean", action="store_true", help="delete files in vendor/ no source produces")
    parser.add_argument("--derive", action="store_true", help="build derived files with Blender if missing")
    parser.add_argument("--force-derive", action="store_true", help="rebuild derived files even if present")
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

    install_addons(args.dry_run)
    presets = write_import_presets(args.dry_run)
    if presets:
        print(f"- import presets written: {presets}")

    if (args.derive or args.force_derive) and not args.dry_run:
        if derive(args.force_derive) != 0:
            return 1

    verb = "would copy" if args.dry_run else "copied"
    print(f"{verb} {copied}, unchanged {unchanged}" + (f", removed {removed}" if args.clean else ""))
    return 0


if __name__ == "__main__":
    sys.exit(main())
