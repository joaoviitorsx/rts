# Credits

All third-party assets below are released under **CC0 1.0 Universal (Public Domain)**.
Credit is not required; we record it anyway.

| Pack | Author | Version | License | Source | Local copy |
|---|---|---|---|---|---|
| Stylized Nature MegaKit | Quaternius | Standard (68/116 models) | CC0 1.0 | https://quaternius.com | `assets/third_party/quaternius_stylized_nature/` |
| Medieval Village MegaKit | Quaternius | Standard | CC0 1.0 | https://quaternius.com | `~/Downloads/Medieval Village MegaKit[Standard].zip` (not extracted yet) |
| Fantasy Props MegaKit | Quaternius | Standard | CC0 1.0 | https://quaternius.com | `assets/Fantasy Props MegaKit[Standard]/` |
| Universal Base Characters | Quaternius | Standard | CC0 1.0 | https://quaternius.com | `assets/Universal Base Characters[Standard]/` |
| Modular Character Outfits – Fantasy | Quaternius | Standard | CC0 1.0 | https://quaternius.com | `assets/Modular Character Outfits - Fantasy[Standard]/` |
| Universal Animation Library | Quaternius | Standard | CC0 1.0 | https://quaternius.com | `assets/Universal Animation Library[Standard]/` |
| Universal Animation Library 2 | Quaternius | Standard | CC0 1.0 | https://quaternius.com | `assets/Universal Animation Library 2[Standard]/` |

| Hand-Painted Watercolor Terrain Textures | Jonas Voland (Voxel Core Lab GmbH) | 1024 px | CC0 1.0 | https://voxelcorelab.itch.io | `art/vendor_raw/VoxelCoreLab_Watercolor_Terrain_Textures_1024px/` |
| UI Pack: RPG Extension | Kenney (www.kenney.nl) | — | CC0 1.0 | https://kenney.nl | `art/vendor_raw/UIpack_RPG/` |
| Interface Sounds | Kenney (www.kenney.nl) | 1.0 | CC0 1.0 | https://kenney.nl/assets/interface-sounds | `art/vendor_raw/kenney_interface-sounds/` |
| Impact Sounds | Kenney (www.kenney.nl) | 1.0 | CC0 1.0 | https://kenney.nl/assets/impact-sounds | `art/vendor_raw/kenney_impact-sounds/` |
| RPG Audio | Kenney (www.kenney.nl) | 1.0 | CC0 1.0 | https://kenney.nl/assets/rpg-audio | `art/vendor_raw/kenney_rpg-audio/` |

| Nature Kit | Kenney (www.kenney.nl) | 2.1 | CC0 1.0 | https://kenney.nl/assets/nature-kit | `art/vendor_raw/kenney_nature_kit/` |
| Fantasy Town Kit | Kenney (www.kenney.nl) | 2.0 | CC0 1.0 | https://kenney.nl/assets/fantasy-town-kit | `art/vendor_raw/kenney_fantasy_town_kit/` |
| Survival Kit | Kenney (www.kenney.nl) | — | CC0 1.0 | https://kenney.nl/assets/survival-kit | `art/vendor_raw/kenney_survival_kit/` |
| Ultimate Animated Animals | Quaternius | July 2021 | CC0 1.0 | https://quaternius.com | `art/vendor_raw/quaternius_ultimate_animals/` |
| Farm Animals Animated | Quaternius | — | CC0 1.0 | https://quaternius.com | `art/vendor_raw/quaternius_farm_animals/` |

Planned, not yet in the project: KayKit Resource Bits (Kay Lousberg, CC0); game-icons.net icons (CC BY 3.0, per-icon credit in `docs/icon_credits.csv`).

Quaternius: https://www.patreon.com/quaternius · Kay Lousberg: https://kaylousberg.com

## Code / tools (MIT)

| Tool | Author | License | Use |
|---|---|---|---|
| [Terrain3D](https://github.com/TokisanGames/Terrain3D) 1.0.2 | Cory Petkovsek, Roope Palmroos & contributors | MIT | Terrain relief, layer painting; `game/terrain/cozy_ground.gdshader` is based on its `extras/shaders/minimum.gdshader` |
| [Sky3D](https://github.com/TokisanGames/Sky3D) 2.1.0 | Cory Petkovsek & contributors, J. Cuéllar | MIT | Sky, sun/moon, clouds and time of day of generated maps (`game/world/WorldSky.gd`), driven by the sim clock and weather |
| [Stylized Cartoon Grass](https://godotshaders.com/shader/stylized-cartoon-grass/) | dip000 | MIT | Grass carpet shader approach (terrain-coloured root, root→tip gradient, wind) adapted in `game/vegetation/grass_carpet.gdshader` |
