# Referências dos aldeões próprios

Geradas por `tools/blender/render_references.py` (Blender headless) a partir do **Kenney Blocky Characters 2.0**
(CC0, `art/vendor_raw/kenney_blocky_characters/`) e do **Fantasy Town Kit** na escala do jogo (módulo ×2 = 2 m).

| Arquivo | O que mostra |
|---|---|
| `blocky_lineup.png` | Os 18 personagens do pacote, `a` a `r` da esquerda para a direita |
| `blocky_<k,a,m,f>_{front,three_quarter,side}.png` | Os 4 mais próximos de um aldeão: `k` camponês/lenhador, `a` agricultor, `m` caçador, `f` camponesa |
| `scale_house_villager*.png` | Casa 3×1 do Fantasy Town com o `k` escalado para **1,30 m** (altura da spec) ao lado da porta de **1,50 m** |

**Só forma e silhueta.** O Blender deste PC (Flatpak 5.1) roda a gestão de cor em modo de fallback (P41): as cores
destes renders não valem; cor se julga no screenshot da Godot. PNGs reduzidos para 128 cores.
Regerar: `flatpak run org.blender.Blender --background --factory-startup --python tools/blender/render_references.py -- --chars k,a,m,f`
(o script grava em resolução cheia; a redução de cores é um passo manual).
