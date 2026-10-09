# Gate do protótipo de aldeão (Etapa 2) — 09/10/2026

Branch `feature/characters-kenney`. Spec: `docs/characters_spec.md` (§10 checklist, §11 diversidade para aprovação).
Fonte: `tools/blender/build_villager.py` (Blender 5.1 headless; refaz tudo do zero, ~10 s com renders).
Cena de comparação: `godot/scenes/test/CHARACTERS_COMPARE.tscn` (`-- --zoom=mid|near --shot=PATH`, `--perf=N`).

## Resultado

| Item | Valor |
|---|---|
| Triângulos (montado, sem ferramenta) | **700** (corpo 316, túnica 172, chapéu 84, cabelo 80, calça 48); machado 68 |
| Altura | 1,38 m com chapéu, 1,26 m sem; porta do Fantasy Town 1,50 m |
| Ossos | 21: Root, Hips, Spine, Chest, Neck, Head, EmoteSocket (1,60 m), BackSocket, Left/Right UpperArm, LowerArm, Hand, UpperLeg, LowerLeg, Foot, ToolSocket |
| Material | 1 (`MAT_Villager_Atlas`), paleta 64×64, todas as UVs em células usadas |
| Animações | `idle` 2 s, `walk` 0,8 s, `chop` 1,2 s (30 fps, loop) |
| 50 aldeões animados | **268 FPS médio, 1% low 240 FPS**, +0,11 ms/quadro, draw calls 255 → 923 (RTX 4050 Laptop, 2311×1080, vsync off) |

## Iterações (3 de 3)

1. Primeira versão: forma e animações corretas, cabeça pequena (~4,7 cabeças), cabelo lateral em degrau, `chop`
   terminando em estocada horizontal com a lâmina de perfil.
2. Cabeça 0,33 m (~4,4 cabeças com chapéu), cabelo lateral numa peça só, golpe diagonal com a lâmina virada para o
   movimento.
3. Na Godot: tons levantados para o valor do colormap do Kenney (matizes mantidos; spec §2.1); paleta sem
   mipmap e sem compressão num material externo compartilhado (antes: *nearest com mipmap* e 7 cópias da textura).

## Comparação honesta com o Kenney Blocky

- **O Blocky ganha em expressão:** rosto pintado (bigode, sobrancelha, sombra em gradiente) e cabeça enorme. Ao
  lado do nosso, ele chama mais atenção e tem mais personalidade. O nosso rosto (olhos, nariz, bochecha) é genérico.
  A §11 (sobrancelhas, barbas, narizes) ataca isso.
- **O nosso ganha em tema e em coesão com o Fantasy Town:** roupa medieval, chanfros, cor chapada e peças
  modulares por slot. O Blocky é moderno (camiseta, jeans, robôs, terno), um bloco único por skin e sem modularidade.
- **Leitura no zoom médio:** o chapéu de palha lê bem. A túnica **verde musgo some na grama**, enquanto o vermelho do
  Blocky salta. Sugestão: evitar verde como cor dominante do torso nos presets (ou só em quem trabalha em terra
  batida) e escolher as cores de profissão pelo contraste com a grama.
- **Profissão sem UI:** o `chop` em movimento lê; parado, o machado tem ~6 px no zoom médio. Ferramentas ~1,3× já
  estão no modelo; dá para subir para ~1,5× se você quiser.

## Imagens (`docs/reports/img_characters/`)

- `godot_mid.jpg`, `godot_mid_crop_x5.png`: câmera oficial no zoom médio (o recorte ampliado 5× pixel a pixel)
- `godot_near.jpg`, `godot_near_crop_x3.png`: zoom perto
- `godot_crowd50.jpg`: 50 aldeões do teste de desempenho
- `proto_front/three_quarter/side.png`, `proto_sheet_walk_*.png`, `proto_sheet_chop_*.png`: renders do Blender
  (**só forma**: as cores do Blender deste PC não valem, P41)
- `compare_proto_vs_blocky.png`: protótipo × Blocky "k" (frente e 3/4, Blender)
