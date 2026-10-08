# Asset Manifest — MVP (Etapa 1: inventário)

> **Status:** 🟡 Rascunho para aprovação · 08/10/2026
> **Regra:** nada foi importado, movido ou alterado nos pacotes nesta etapa. Só leitura dos arquivos glTF/GLB (JSON + cabeçalhos PNG).
> ⚠ **`asset_production_bible_mvp.md` continua fora do repositório.** Sem a §18 (lista P0) e a §13 (nomes), os **IDs abaixo são provisórios**: derivei a lista P0 do Marco 1 (TDD v0.1) + da cena TEST_VILLAGE_01 descrita na sua mensagem, e os nomes seguem o padrão dos seus exemplos (`BLD_House_T1_A`, `ENV_Oak_A`, `PROP_Barrel_A`). Quando a Bible entrar, reconcilio.

---

## 1. Onde os pacotes estão (≠ do esperado)

| Pacote (nome lógico) | Esperado | Encontrado | Formatos | Tamanho |
|---|---|---|---|---|
| quaternius_stylized_nature | `godot/assets/vendor/` | `assets/third_party/quaternius_stylized_nature/` (eu movi antes da sua mensagem; FBX/OBJ em `_unused/` com `.gdignore`) | glTF + PNG | 48 MB (+70 MB duplicados) |
| quaternius_medieval_village | idem | **só zip, não extraído:** `~/Downloads/Medieval Village MegaKit[Standard].zip` | glTF/FBX/OBJ | 161 MB |
| quaternius_fantasy_props | idem | `assets/Fantasy Props MegaKit[Standard]/` | glTF/FBX/OBJ | 157 MB |
| quaternius_base_characters | idem | `assets/Universal Base Characters[Standard]/` | glTF/FBX | 127 MB |
| quaternius_outfits_fantasy | idem | `assets/Modular Character Outfits - Fantasy[Standard]/` | glTF/FBX | 293 MB |
| quaternius_ual1 | idem | `assets/Universal Animation Library[Standard]/` | GLB/FBX | 61 MB |
| quaternius_ual2 | idem | `assets/Universal Animation Library 2[Standard]/` | GLB/FBX/.blend | 70 MB |
| kaykit_resource_bits | idem | **não encontrado** (há `~/Downloads/KayKit_Forest_Nature_Pack_1.0_FREE.zip`, que é outro pacote) | — | — |
| watercolor_terrain_textures | idem | **não encontrado** | — | — |

Todos os encontrados: autor **Quaternius**, licença **CC0 1.0**, versão **Standard** (gratuita, subconjunto do Pro/Source).

**Pasta `godot/` não existe.** O `project.godot` está na raiz do repo. Ver decisão A1 no §7.

---

## 2. Conteúdo por pacote

### 2.1 Stylized Nature MegaKit — 68 modelos estáticos
- Árvores: `CommonTree_1–5`, `Pine_1–5`, `DeadTree_1–5`, `TwistedTree_1–5`
- Arbustos/plantas: `Bush_Common`, `Bush_Common_Flowers`, `Fern_1`, `Plant_1`, `Plant_1_Big`, `Plant_7`, `Plant_7_Big`, `Clover_1–2`
- Flores: `Flower_3_Group/Single`, `Flower_4_Group/Single`, `Petal_1–5`
- Grama: `Grass_Common_Short/Tall`, `Grass_Wispy_Short/Tall` · Cogumelos: `Mushroom_Common`, `Mushroom_Laetiporus`
- Pedras: `Rock_Medium_1–3`, `Pebble_Round_1–5`, `Pebble_Square_1–6`
- Caminho: `RockPath_Round_Small_1–3`, `RockPath_Round_Thin/Wide`, `RockPath_Square_Small_1–3`, `RockPath_Square_Thin/Wide`
- Materiais: só albedo (normal map só nas cascas), metallic 0 / roughness 1, folhagem alpha MASK, double-sided. Texturas 512²–2048². Sem esqueletos/animações.
- ⚠ 55/88 primitivas têm `COLOR_0` em tons de cinza (máscara de AO/vento). Se o import usar vertex color como albedo, a grama/árvores escurecem.

### 2.2 Medieval Village MegaKit — 176 peças modulares (zip)
- Paredes (grid 2 m, altura 3,12 m): `Wall_Plaster_*` (Straight, Door_Flat/Round, Window_*, WoodGrid), `Wall_UnevenBrick_*`, `Wall_Arch`, cantos `Corner_*`
- Pisos: `Floor_Brick`, `Floor_RedBrick`, `Floor_UnevenBrick`, `Floor_WoodDark/Light*`
- Telhados: `Roof_RoundTiles_4x4 … 8x14`, `Roof_Wooden_2x1*`, `Roof_Tower_RoundTiles`, `Roof_Dormer_RoundTile`, `Roof_Front_Brick*`, `Roof_Log`, suportes
- Portas/janelas: `Door_1/2/4/8_Flat/Round`, `DoorFrame_*`, `Window_*`, `WindowShutters_*`, `Balcony_*`, `Overhang_*`, `Stairs_*`
- Props: `Prop_Wagon`, `Prop_Crate`, `Prop_WoodenFence_Single/Extension1/2`, `Prop_MetalFence_*`, `Prop_Chimney/2`, `Prop_Support`, `Prop_Brick1–4`, `Prop_Vine*`, `Prop_ExteriorBorder_*`
- Materiais: trim sheets com normal map (mesma linguagem do Fantasy Props).

### 2.3 Fantasy Props MegaKit — 94 modelos
- Economia/aldeia: `Barrel`, `Barrel_Apples`, `Barrel_Holder`, `Crate_Wooden`, `Crate_Metal`, `FarmCrate_Empty/Apple/Carrot`, `Bag` (saco ~0,8 m), `Pouch_Large`, `Bucket_Wooden_1`, `Bucket_Metal`, `Stall_Empty`, `Stall_Cart_Empty`, `Anvil`, `Anvil_Log`, `Whetstone`, `Workbench`, `Workbench_Drawers`, `Cauldron`, `Axe_Bronze`, `Pickaxe_Bronze`, `Carrot`, `Coin`, `Coin_Pile`, `Coin_Pile_2`, `Rope_1–3`, `Chain_Coil`
- Mobiliário/decoração/iluminação: bancos, mesas, camas, estantes, livros, velas, tochas, banners, poções, pratos, armas, `Chest_Wood` (único com esqueleto: 3 ossos + 4 animações de abrir/fechar)
- Materiais: trim sheets PBR 2048² (BaseColor + Normal + ORM), vertex color tingindo madeira (intencional). Sem emissivo (tochas/velas apagadas).

### 2.4 Universal Base Characters
- Corpos: `Superhero_Male_FullBody`, `Superhero_Female_FullBody` (só o tipo "Superhero" no Standard). **Cabeça e corpo são uma malha só** (+ olhos + sobrancelhas).
- Cabelos: `Hair_Beard`, `Hair_Buns`, `Hair_Buzzed`, `Hair_BuzzedFemale`, `Hair_Long`, `Hair_SimpleParted`, `Eyebrows_*` — em duas versões: "Rigged to Head Bone" e "Origin at 0".
- Esqueleto: **65 ossos estilo UE Mannequin** (`root → pelvis → spine_01…`), não Mixamo. Texturas 2048².

### 2.5 Modular Character Outfits – Fantasy
- Outfits completos: `Male_Peasant`, `Female_Peasant`, `Male_Ranger`, `Female_Ranger` + partes modulares (Arms/Body/Feet/Legs, capuz, ombreiras).
- **Camponês = `Male_Peasant` / `Female_Peasant`.** Não incluem cabeça (o README diz: usar só a cabeça do Base Character; corpo inteiro por baixo gera *clipping*).
- Mesmo esqueleto de 65 ossos do Base Characters. Texturas **4096²** (pesadas para RTS); albedos alternativos `T_Peasant_2_BaseColor`.

### 2.6 UAL1 + UAL2 — animações
- Cada pacote: `*_Standard.glb` (sem root motion) e `*_Standard_RM.glb` (root motion em +Z), manequim de cores chapadas.
- **Mesmos 65 ossos, mesma ordem e hierarquia** do Base Characters/Outfits → sem renomear ossos.
- UAL1 (43): `Idle_Loop`, `Idle_Talking_Loop`, `Idle_Torch_Loop`, `Walk_Loop`, `Walk_Formal_Loop`, `Jog_Fwd_Loop`, `Sprint_Loop`, `Crouch_*`, `Roll`, `Jump_*`, `Swim_*`, `Interact`, `PickUp_Table`, `Push_Loop`, `Fixing_Kneeling`, `Driving_Loop`, `Dance_Loop`, `Sitting_*`, `Hit_*`, `Death01`, `Punch_*`, `Sword_*`, `Pistol_*`, `Spell_Simple_*`, `A_TPose`
- UAL2 (43): **`Farm_Harvest`**, **`Farm_PlantSeed`**, `Farm_Watering`, **`TreeChopping_Loop`**, **`Walk_Carry_Loop`**, `Chest_Open`, `Consume`, `LayToIdle`, `ClimbUp_1m`, `Yes`, idles (`FoldArms`, `Lantern`, `No`, `Rail_*`, `TalkingPhone`), escudo/espada/combate, `NinjaJump_*`, `Slide_*`, `Zombie_*`
- ⚠ **Não existe `IDLE_CARRY` no UAL2** (a sua mensagem cita; o arquivo não tem). Ver lacuna L1.

---

## 3. Manifesto P0 (IDs provisórios)

Legenda de status: **pronto** = usar o arquivo como está (cena herdada + ajuste de escala/material) · **montar** = compor a partir de peças · **falta** = não existe nos pacotes.
Caminhos relativos a `assets/` (Fantasy Props = `FP/Exports/glTF/`, Medieval Village = `MVK/` após extrair o zip, Nature = `NAT/glTF/`, Base Characters = `UBC/`, Outfits = `OUT/Exports/glTF (Godot-Unreal)/`).

### 3.1 Construções (sim id → cena)

| ID (provisório) | sim id | Pacote | Arquivo(s) | Status | Observações |
|---|---|---|---|---|---|
| BLD_Hall_A | `hall` | MVK + FP | `Wall_UnevenBrick_*`, `Wall_Plaster_Door_Round`, `Roof_RoundTiles_8x10`, `Door_8_Round`, `Prop_Chimney`; `FP/Banner_1` | montar | Footprint 3×3 células (6×6 m) |
| BLD_House_T1_A/B/C | `house` | MVK | `Wall_Plaster_Straight/Window_*/Door_Flat`, `Roof_RoundTiles_4x4` ou `Roof_Wooden_2x1*`, `WindowShutters_*` | montar | 2×2 células (4×4 m); 3 variações por telhado/janela |
| BLD_Woodcutter_A | `woodcutter` | MVK + FP | `Wall_Plaster_WoodGrid`, `Roof_Wooden_2x1*`, `Prop_Support`; `FP/Anvil_Log` (toco), `FP/Axe_Bronze` | montar | Pilha de toras **falta** (L5) |
| BLD_Farm_A | (casa da fazenda) | MVK + FP | paredes Plaster + `Roof_Wooden_*`; `FP/FarmCrate_*`, `FP/Bucket_Wooden_1`, `FP/Bag` | montar | Não existe no sim do Marco 1; só cena de teste |
| BLD_Field_A | `field` | — | — | **falta** | Solo arado + estágios do trigo (L3, L4) |
| BLD_Granary_A | `granary` | MVK + FP | `Wall_UnevenBrick_*`, `Door_4_Round`, `Roof_Wooden_2x1*`; `FP/Bag`, `FP/Barrel`, `FP/Crate_Wooden` | montar | Sacos de grão = `Bag` (só 1 variação) |
| BLD_Warehouse_A | (armazém) | MVK + FP | paredes + `Roof_RoundTiles_6x8`; `FP/Crate_*`, `MVK/Prop_Crate`, `FP/Barrel_Holder` | montar | Fora do sim do Marco 1 |
| BLD_Market_A | (mercado) | FP | `Stall_Empty`, `Stall_Cart_Empty`, `FarmCrate_Apple/Carrot`, `Barrel_Apples` | pronto/montar | Bancas sem toldo colorido |
| BLD_Smithy_A | (ferraria) | MVK + FP | paredes Brick + `Prop_Chimney2`; `FP/Anvil`, `FP/Workbench`, `FP/Whetstone`, `FP/Cauldron` | montar | Forja/brasas **falta** (L7) |
| BLD_Well_A | (poço) | MVK + FP | `Prop_Brick1–4`/`Floor_UnevenBrick` (aro), `Roof_Wooden_2x1`, `Prop_Support`, `FP/Bucket_Wooden_1`, `FP/Rope_1` | montar | Montagem improvisada; validar visual (L8) |
| BLD_Site_A | (obra) | MVK + FP | `Prop_Support`, `Floor_WoodLight`, `FP/Crate_Wooden` | montar | Estado "em construção" do sim |

### 3.2 Ambiente

| ID | Pacote | Arquivo | Status | Observações |
|---|---|---|---|---|
| ENV_Oak_A–E | NAT | `CommonTree_1–5` | pronto | Escala ~0,6 (L10) |
| ENV_Pine_A–E | NAT | `Pine_1–5` | pronto | Escala ~0,6 |
| ENV_DeadTree_A–C | NAT | `DeadTree_1–3` | pronto | Escala ~0,4; pivô XZ deslocado |
| ENV_Bush_A/B | NAT | `Bush_Common`, `Bush_Common_Flowers` | pronto | Escala ~0,5 |
| ENV_Grass_A–D | NAT | `Grass_Common_Short/Tall`, `Grass_Wispy_Short/Tall` | pronto | Escala ~0,4; MultiMesh |
| ENV_Flower_A/B | NAT | `Flower_3_Group`, `Flower_4_Group` | pronto | Escala ~0,3 |
| ENV_Rock_A–C | NAT | `Rock_Medium_1–3` | pronto | Pedra/pedreira |
| ENV_Pebble_A–E | NAT | `Pebble_Round_*`, `Pebble_Square_*` | pronto | |
| ENV_Stump_A | — | — | **falta** | Toco após corte (L6) |
| ENV_Road_A | NAT | `RockPath_*` (pedras soltas) | montar | Estrada de terra depende da textura (L2) |
| ENV_Fence_A | MVK | `Prop_WoodenFence_Single/Extension1/2` | pronto | Cerca do campo |
| TER_Grass / TER_Dirt / TER_Path / TER_Water | — | — | **falta** | Pacote watercolor não encontrado (L2) |

### 3.3 Props

| ID | Pacote | Arquivo | Status | Observações |
|---|---|---|---|---|
| PROP_Barrel_A/B | FP | `Barrel`, `Barrel_Apples` | pronto | |
| PROP_Crate_A/B/C | FP / MVK | `Crate_Wooden`, `Crate_Metal`, `MVK/Prop_Crate` | pronto | `Crate_Wooden` tem escala 0,78 na raiz (ok) |
| PROP_FarmCrate_A/B/C | FP | `FarmCrate_Empty/Apple/Carrot` | pronto | |
| PROP_Sack_A | FP | `Bag` | pronto | Uma variação só |
| PROP_Bucket_A | FP | `Bucket_Wooden_1` | pronto | |
| PROP_Cart_A | MVK | `Prop_Wagon` | pronto | Carroça inicial / logística |
| PROP_StallCart_A | FP | `Stall_Cart_Empty` | pronto | Pivô deslocado −0,6 m em X |
| PROP_Anvil_A | FP | `Anvil` | pronto | |
| PROP_Axe_A | FP | `Axe_Bronze` | pronto | Pivô no cabo (bom para mão); raiz rotacionada −90° Z |
| PROP_Pickaxe_A | FP | `Pickaxe_Bronze` | pronto | Pivô no cabo |
| PROP_CoinPile_A | FP | `Coin_Pile`, `Coin_Pile_2` | pronto | Moedas no Salão |
| PROP_Workbench_A | FP | `Workbench` | pronto | |
| PROP_WoodPile_A / PROP_FirewoodPile_A | — | — | **falta** | KayKit Resource Bits (L5) |
| PROP_StonePile_A | — | — | **falta** | idem |
| PROP_Hammer_A, PROP_Saw_A, PROP_Hoe_A, PROP_Pitchfork_A | — | — | **falta** | L9 |

### 3.4 Personagem e animações

| ID | Pacote | Arquivo | Status | Observações |
|---|---|---|---|---|
| CHR_Villager_Base (M) | UBC + OUT | `UBC/Base Characters/Godot - UE/Superhero_Male_FullBody.gltf` + `OUT/Outfits/Male_Peasant.gltf` | montar | *Clipping* corpo×roupa (L11) |
| CHR_Villager_Base (F) | UBC + OUT | `Superhero_Female_FullBody.gltf` + `Female_Peasant.gltf` | montar | idem |
| CHR_Hair_* | UBC | `Hairstyles/Rigged to Head Bone/Hair_*.gltf` | pronto | Variação de aldeões |
| ANIM idle | UAL1 | `Idle_Loop` | pronto | |
| ANIM walk | UAL1 | `Walk_Loop` | pronto | ~0,98 m/s (root motion) |
| ANIM walk_carry | UAL2 | `Walk_Carry_Loop` | pronto | |
| ANIM idle_carry | — | — | **falta** | L1 |
| ANIM harvest | UAL2 | `Farm_Harvest` | pronto | |
| ANIM plant_seed | UAL2 | `Farm_PlantSeed` | pronto | |
| ANIM chop | UAL2 | `TreeChopping_Loop` | pronto | Lenhador |
| ANIM mine | — | — | **falta** | L12 |
| ANIM hammer (ferreiro) | — | — | **falta** | L13 |
| ANIM gather / pick_up (chão) | UAL1 | `PickUp_Table` (altura de mesa) | montar | Só pega em mesa; ver L14 |
| ANIM sit / talk / interact | UAL1 | `Sitting_*`, `Idle_Talking_Loop`, `Interact` | pronto | Ambiente/vida |

---

## 4. Lacunas e soluções sugeridas

| # | Lacuna | Solução recomendada | Alternativas |
|---|---|---|---|
| L1 | **idle_carry** | Montar no Godot, sem asset novo: `AnimationTree` com *Blend2* filtrado por osso — braços/coluna do 1º quadro de `Walk_Carry_Loop` sobre pernas de `Idle_Loop` | Blender: congelar pose e exportar clipe; Mixamo "Carrying Idle" (retarget) |
| L2 | **Texturas watercolor** (grama/terra/caminho/água) — pacote não encontrado | Você baixar o pacote que tinha em mente e me dizer o caminho | Temporário: cores chapadas + ruído no shader do Terrain3D |
| L3 | **Campo arado** (solo com sulcos) | Malha simples no Blender (plano com sulcos) + textura de terra | Textura de terra do Terrain3D pintada sob o campo |
| L4 | **Estágios do trigo** (broto → verde → maduro → colhido) | Blender: 1 tufo low-poly, 4 variações de altura/cor, instanciado em MultiMesh | Reaproveitar `Grass_Wispy_*` recolorido via material override (rápido, menos legível) |
| L5 | **Pilhas de madeira/lenha/pedra** | Baixar **KayKit Resource Bits** (CC0, era o plano original) | Blender: cilindros empilhados (toras) + reaproveitar `Pebble_*` para pedra |
| L6 | **Toco de árvore** | Blender: cortar a base de `CommonTree_1` numa cópia | KayKit Forest Nature (já está nos seus Downloads): verificar se tem tocos |
| L7 | **Forja/brasas** | Blender: bloco de tijolo + `Prop_Chimney2`; brasa com material emissivo | `Cauldron` + partículas de fogo |
| L8 | **Poço** | Montagem com peças MVK + `Bucket` + `Rope` (validar visualmente na Etapa 3) | Blender: modelo próprio simples |
| L9 | **Martelo, serra, enxada, forcado** | Blender: modelos triviais (≤ 200 tris), só aparecem na mão durante animação | Omitir no MVP (não são P0 da sim) |
| L10 | (escala) Nature 2–3× maior que o resto | Escala fixa na cena herdada (ver §5) | — |
| L11 | **Cabeça + corpo numa malha só** (roupa sobre corpo = *clipping*) | Blender: em uma **cópia** do `Superhero_*_FullBody`, apagar o corpo abaixo do pescoço → exportar `UBC_*_HeadOnly.glb` em `assets/characters/` | Shader que descarta pixels do corpo por máscara de vértice; ou aceitar *clipping* (não recomendo) |
| L12 | **Minerar** | Reaproveitar `TreeChopping_Loop` com `PROP_Pickaxe_A` na mão | Mixamo "Mining" + retarget |
| L13 | **Martelar na bigorna** | Mixamo "Hammering" + retarget por `SkeletonProfileHumanoid` | `Sword_Regular_A` em loop curto (improviso) |
| L14 | **Pegar do chão / coletar** | `Farm_Harvest` serve para coletar no campo; para carregar caixas, Mixamo "Picking Up" | `Crouch_Idle_Loop` + `Interact` |

Observações sobre fontes externas:
- **Mixamo**: grátis e com uso comercial permitido em jogos, mas **não é CC0** (não pode redistribuir os arquivos crus). Usa esqueleto `mixamorig:*` → retarget no import com `SkeletonProfileHumanoid`.
- **Meshy** (IA): só se você aceitar; a licença depende do plano, o estilo tende a destoar e a topologia costuma precisar de limpeza. Prefiro Blender para peças pequenas.

---

## 5. Escala, orientação e pivô (amostras medidas)

Medido aplicando as transformações dos nós às caixas delimitadoras das malhas.

| Pacote | Amostra | Tamanho (L×A×P, m) | Pivô | Frente | Diferença |
|---|---|---|---|---|---|
| Nature | `CommonTree_1` | ~4 × 7–9,4 × 4 | base, ~0,2–0,34 m **abaixo** de Y=0 (raiz enterrada) | n/a | **2–3× grande demais** |
| Nature | `Grass_Common_Tall` | — × 1,1–1,9 | base | n/a | grama de 1,9 m (deveria ~0,5) |
| Nature | `Fern_1` | 9 × 2,7 × 8,5 | base | n/a | samambaia de 9 m |
| Nature | `TwistedTree_*` | 10–13,5 × 15,7–19 | base, **XZ deslocado até 4,6 m** | n/a | enorme + pivô fora do centro |
| Medieval Village | `Wall_Plaster_Straight` | 2 × 3,12 | base | +Z | 1 m ✔ (grid 2 m) |
| Medieval Village | `Prop_Wagon` | 1,95 × 1,53 × 4,02 | base | +Z | ✔ |
| Fantasy Props | `Barrel` | 0,70 × 0,90 × 0,70 | base, centrado | +Z | ✔ |
| Fantasy Props | `Stall_Empty` | 1,85 × 2,63 × 0,93 | base | +Z | ✔ |
| Fantasy Props | `Axe_Bronze` | — | **no cabo** (minY −0,38); raiz −90° Z, escala 0,847 | — | intencional (mão) |
| Fantasy Props | `Banner_*_Cloth`, `Chandelier`, `Torch_Metal`, `Lantern_Wall` | — | **ponto de fixação** (topo/parede) | +Z | intencional |
| Base Characters | `Superhero_Male_FullBody` | 1,82 de altura | pés, centrado | +Z | ✔ |
| Outfits | `Male_Peasant` | até ~1,55 (sem cabeça) | pés | +Z | ✔ |
| UAL1/2 | Mannequin | 1,83 de altura | pés | +Z (root motion anda em +Z) | ✔ |

- **Convenção Godot:** +Y para cima, frente do modelo **+Z** (`Vector3.MODEL_FRONT`). Ao usar `look_at`, passar `use_model_front = true`.
- **Proposta de escala nas cenas herdadas:** árvores ×0,6 · árvores mortas/retorcidas ×0,4 · arbustos ×0,5 · grama/flores/trevo ×0,35 · samambaia ×0,25 · pedras ×0,8. Valores finais ajustados na Etapa 3, ao lado de um aldeão de 1,8 m.
- **Pose de repouso dos esqueletos difere um pouco** (pelve 0,917 m no manequim × 0,949 m no corpo-base) e as animações UAL têm trilhas de posição em todos os ossos → aplicar o `BoneMap`/`SkeletonProfileHumanoid` no import e remover trilhas de posição (exceto root/quadril), como o próprio Quaternius recomenda no `Godot_Setup.png`.

---

## 6. Diferenças visuais entre pacotes (para decidir, não decidi)

| # | Diferença | Proposta |
|---|---|---|
| V1 | **Nature é pintado à mão** (só albedo, roughness 1). **Props/Village/Outfits são PBR com trim sheet** (normal + ORM, metallic até 1). Lado a lado, os props vão parecer mais "realistas" e brilhantes. | Override de material nos PBR: roughness ≥ 0,8, metallic ≤ 0,2 (exceto metal real) e normal map com força reduzida. Testar na Etapa 3 com 2–3 ajustes e você escolhe. |
| V2 | Vertex color: na Nature é máscara cinza (não é cor); nos Props tinge a madeira (é cor) | Desligar "vertex color como albedo" na Nature e manter nos Props |
| V3 | Texturas 4096² nas roupas e 2048² no resto; câmera RTS alta | Limitar o import a 1024² (personagens) e 1024–2048² (prédios); economiza VRAM |
| V4 | Saturação: Nature mais saturada/verde; Village em tons terrosos | Ajuste fino no `WorldEnvironment` (cor/saturação global) em vez de mexer em cada material |
| V5 | Sem emissivo em tochas/velas | Material override emissivo + luz pontual só onde fizer sentido (ferraria, Salão) |

---

## 7. Decisões que preciso antes da Etapa 2

- **A1 — Estrutura do projeto.** Sua mensagem fala em `godot/assets/vendor/`, mas hoje o `project.godot` fica na raiz do repo (o TDD v0.1 seguiu isso). Proposta: **mover o projeto Godot para `rts/godot/`** e manter `src/`, `tests/`, `data/` e `docs/` na raiz. A Godot deixa de varrer `src/` e `tests/`, e `godot/assets/vendor/` fica literal. Alternativa: manter a raiz e usar `assets/vendor/`.
- **A2 — FBX/OBJ dentro dos pacotes.** A Godot importa tudo que estiver dentro do projeto (Props, Outfits, Base Characters e UAL têm FBX/OBJ). Sem mexer nos originais, proposta: guardar os pacotes **originais fora do projeto Godot** (`rts/vendor_src/`, intocados) e copiar só os glTF/GLB + texturas, sem alteração, para `godot/assets/vendor/<pacote>/`. Alternativa: colocar `.gdignore` nas subpastas FBX/OBJ dentro do vendor (são arquivos novos, não alteram os originais, mas tocam a pasta).
- **A3 — Git.** Os pacotes somam ~920 MB (cerca de 330 MB só em glTF + texturas). Não temos git-lfs. Proposta: **não versionar o vendor** (gitignore) e registrar a origem/versão de cada pacote no `CREDITS.md`; versionar só as cenas herdadas. Já commitei os glTF da Nature (48 MB) antes da sua mensagem; posso remover do índice.
- **A4 — Pacotes faltando.** Medieval Village está só em zip (posso extrair para o vendor?), e KayKit Resource Bits e watercolor não estão na máquina. Baixa você, ou sigo com as soluções L2/L5?
- **A5 — Terrain3D.** É um GDExtension (C++), instalado por zip do GitHub/Asset Library. Posso baixar a versão compatível com Godot 4.7 na Etapa 2?
- **A6 — Bible.** Sem a §13/§18/§43, sigo com os IDs provisórios acima?
