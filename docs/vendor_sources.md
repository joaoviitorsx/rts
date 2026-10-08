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
| watercolor_terrain_textures | *(pendente — matched by `*atercolor*`)* | — | — | — | — | pendente | CC0 (informado) |

¹ Modification date of the pack's root folder as shipped (≈ release/build date).
² Confirmed by the project owner.

## Known issues in the originals (handled by the script, originals untouched)
- `Universal Base Characters`: `Superhero_*_FullBody.gltf` reference `T_Eye_Normal_png.png` /
  `T_Hair_1_Normal_png.png`, which don't exist; the script copies `T_Eye_Normal.png` / `T_Hair_1_Normal.png`
  under the expected names.
- Folders in some zips are read-only (Windows attributes); `chmod -R u+w` was applied to `art/vendor_raw/`
  (permissions only, contents unchanged).
- `desktop.ini` files were deleted from the Stylized Nature folder during the first reorganization.

## Temporary
- (removed) the temporary `.gdignore` used during Marco 1.

