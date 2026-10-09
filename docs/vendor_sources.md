# Vendor sources

Third-party packs are **not versioned** (≈1 GB). Originals live untouched in `art/vendor_raw/`;
`python3 scripts/setup_vendor.py` copies only glTF/GLB + referenced buffers/textures into
`godot/assets/vendor/<pack_id>/` (re-runnable, copies only what changed; `--clean` removes stale files).

To rebuild on a new machine: download each pack below, extract it into `art/vendor_raw/` keeping the
original folder name, then run the script.

| pack_id | Pack (folder in `art/vendor_raw/`) | Author | URL | Version | Pack date¹ | Downloaded² | License |
|---|---|---|---|---|---|---|---|
| quaternius_stylized_nature | `Stylized Nature MegaKit[Standard]` | Quaternius | https://quaternius.com (página do pacote: confirmar) | Standard (68/116) | — | 2026-10-08 | CC0 1.0 |
| quaternius_medieval_village | `Medieval Village MegaKit[Standard]` (from `Medieval Village MegaKit[Standard].zip`) | Quaternius | https://quaternius.com | Standard | 2025-01-21 | 2026-10-08 | CC0 1.0 |
| quaternius_fantasy_props | `Fantasy Props MegaKit[Standard]` | Quaternius | https://quaternius.com | Standard | — | 2026-10-08 | CC0 1.0 |
| quaternius_base_characters | `Universal Base Characters[Standard]` | Quaternius | https://quaternius.com | Standard | 2025-12-02 | 2026-10-08 | CC0 1.0 |
| quaternius_outfits_fantasy | `Modular Character Outfits - Fantasy[Standard]` | Quaternius | https://quaternius.com | Standard | 2026-01-29 | 2026-10-08 | CC0 1.0 |
| quaternius_ual1 | `Universal Animation Library[Standard]` | Quaternius | https://quaternius.com | Standard | 2026-06-16 | 2026-10-08 | CC0 1.0 |
| quaternius_ual2 | `Universal Animation Library 2[Standard]` | Quaternius | https://quaternius.com | Standard | 2026-06-16 | 2026-10-08 | CC0 1.0 |
| kaykit_resource_bits | *(pendente — matched by `*Resource*Bits*`)* | Kay Lousberg | https://kaylousberg.com | — | — | pendente | CC0 1.0 |
| watercolor_terrain_textures | `VoxelCoreLab_Watercolor_Terrain_Textures_1024px` | Jonas Voland / Voxel Core Lab GmbH | https://voxelcorelab.itch.io | 1024 px | — | 2026-10-08 | CC0 1.0 |
| kenney_ui_rpg | `UIpack_RPG` (UI Pack: RPG Extension) | Kenney Vleugels | https://kenney.nl · https://opengameart.org/content/ui-pack-rpg-extension | — | — | 2026-10-08 | CC0 1.0 |
| kenney_interface_sounds | `kenney_interface-sounds` (Interface Sounds 1.0) | Kenney Vleugels | https://kenney.nl/media/pages/assets/interface-sounds/fa43c1dd4d-1677589452/kenney_interface-sounds.zip | `kenney_interface-sounds.zip` | sha256 `f2193d07…81232` | 2026-10-09 | CC0 1.0 |
| kenney_impact_sounds | `kenney_impact-sounds` (Impact Sounds 1.0) | Kenney Vleugels | https://kenney.nl/media/pages/assets/impact-sounds/87b4ddecda-1677589768/kenney_impact-sounds.zip | `kenney_impact-sounds.zip` | sha256 `029d734a…77f8` | 2026-10-09 | CC0 1.0 |
| kenney_rpg_audio | `kenney_rpg-audio` (RPG Audio 1.0) | Kenney Vleugels | https://kenney.nl/media/pages/assets/rpg-audio/8e99002d76-1677590336/kenney_rpg-audio.zip | `kenney_rpg-audio.zip` | sha256 `6dbeaf85…f38b` | 2026-10-09 | CC0 1.0 |
| kenney_nature_kit | `kenney_nature_kit` (Nature Kit 2.1) | Kenney Vleugels | https://kenney.nl/assets/nature-kit | 2.1 | — | 2026-10-09 | CC0 1.0 (`Nature Kit (2.1).zip`, sha256 `fa7974a0d342bfe6…`) |
| kenney_fantasy_town_kit | `kenney_fantasy_town_kit` | Kenney Vleugels | https://kenney.nl/assets/fantasy-town-kit | 2.0 | — | 2026-10-09 | CC0 1.0 (`kenney_fantasy-town-kit_2.0.zip`, sha256 `1a7530c09f4d2fa2…`) |
| kenney_survival_kit | `kenney_survival_kit` | Kenney Vleugels | https://kenney.nl/assets/survival-kit | — | — | 2026-10-09 | CC0 1.0 (`kenney_survival-kit.zip`, sha256 `c3586341b5932c87…`) |
| quaternius_ultimate_animals | `quaternius_ultimate_animals` (Ultimate Animated Animals, July 2021) | Quaternius | https://quaternius.com | July 2021 | 2021-07 | 2026-10-09 | CC0 1.0 (sha256 `c0060caf388fd03a…`) |
| (só FBX/Blend) | `quaternius_farm_animals` (Farm Animals Animated) | Quaternius | https://quaternius.com | — | — | 2026-10-09 | CC0 1.0 (sha256 `b4bc5f209368cafc…`); sem glTF — precisa de export no Blender (P36) |
| (pendente) | `kenney_building_kit`, `kenney_castle_kit` | Kenney Vleugels | https://kenney.nl/assets | — | — | — | CC0 1.0 — **não estão nos Downloads** (P35) |

¹ Modification date of the pack's root folder as shipped (≈ release/build date).
² Confirmed by the project owner.

## Godot addons and adapted code

| id | Source | Version | License | How it gets into the project |
|---|---|---|---|---|
| terrain3d | https://github.com/TokisanGames/Terrain3D/releases (`Terrain3D_v1.0.2-stable.zip`) | 1.0.2 (officially 4.4–4.6; verified loading on 4.7) | MIT | extracted to `art/vendor_raw/Terrain3D_v1.0.2-stable/`; `setup_vendor.py` installs Win/Linux x86_64 binaries into `godot/addons/terrain_3d/` |
| stylized_cartoon_grass | https://godotshaders.com/shader/stylized-cartoon-grass/ (dip000) | page as of 2026-10-08 | MIT | ideas/code adapted (not copied verbatim) in `godot/game/vegetation/grass_carpet.gdshader`, credited in the file header |

Evaluated and not used: Open Stylized 3D (MIT; own node types + billboard waves, no terrain-colour matching);
godot-landscaper (licence not stated — scattering is done by our own scripts).

## Import presets
- `kenney_ui_rpg`: `setup_vendor.py` writes `*.png.import` before Godot's first import — lossless, no mipmaps, no size
  limit (UI_UX_guide §8.3). Final UI art is F3; for now the pack is only available.
- `watercolor_terrain_textures`: default 3D preset (VRAM, mipmaps, 1024 px). Only the luminance of `Dirt_03.png` is used,
  as subtle detail on bare ground (`ground_detail_*` shader globals).
- The `__MACOSX/` folder and `._*` files in the watercolor zip are skipped.
- Kenney audio packs (placeholder sounds, 2B polish): `.ogg` only, default import. The game plays them only through
  `godot/assets/audio/SFX_<id>.tres` (AudioStreamRandomizer per logical id), generated by `tools/assets/build_audio.py`.

## Derived in Godot
- Tree impostor atlases (`godot/assets/environment/impostors/`, not versioned except `*.import`): rendered from the
  Stylized Nature tree scenes by `scenes/tools/ImpostorBaker.tscn` (`setup_vendor.py --derive` runs it; needs a GPU
  window). Import: VRAM + mipmaps, never a normal map (alpha carries the palette param). Rebake after changing tree
  scenes, tree materials or `impostor_bake.gdshader` / `tree_impostor.gdshader`.

## Known issues in the originals (handled by the script, originals untouched)
- `Universal Base Characters`: `Superhero_*_FullBody.gltf` reference `T_Eye_Normal_png.png` /
  `T_Hair_1_Normal_png.png`, which don't exist; the script copies `T_Eye_Normal.png` / `T_Hair_1_Normal.png`
  under the expected names.
- Folders in some zips are read-only (Windows attributes); `chmod -R u+w` was applied to `art/vendor_raw/`
  (permissions only, contents unchanged).
- `desktop.ini` files were deleted from the Stylized Nature folder during the first reorganization.

## Temporary
- (removed) the temporary `.gdignore` used during Marco 1.

