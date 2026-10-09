> **⚠️ ADENDO 08/10/2026 — decisões que prevalecem sobre este documento**
>
> - **Estilo visual:** 3D estilizado, cozy e cartoon. Estão **REVOGADOS**: pixel shader, render em baixa resolução (640×360/480×270), quantização de cores, dithering e snapping de câmera (§1, §9, §10 e menções a "pixel-shaded rendering").
> - **Câmera:** perspectiva com FOV baixo (ou ortográfica), alta (~55–60°), com pan, zoom suave e rotação em passos de 90°.
> - **Assets (atualizado 09/10/2026):** família **Kenney** (Nature Kit, Fantasy Town Kit, Survival Kit; CC0) para mundo e construções; personagens próprios no padrão Kenney (ver docs/characters_spec.md). Os prompts de Meshy (§16–§30) servem apenas para preencher lacunas.
> - **Continuam valendo:** escala (1 un = 1 m, ajustada à escala real dos kits), pivôs (§12), convenção de nomes (§13), manifesto P0 (§18), modularidade (§32–§33), checklists (§44–§46), TEST_VILLAGE_01 (§43) e as prioridades (§48).

# Asset Production Bible — MVP
## RTS incremental medieval em Godot
### Direção visual: 3D low-poly + pixel shader + câmera isométrica/ortográfica

> **North Star do projeto**
>
> **Um mundo medieval artesanal, orgânico e vivo, com silhuetas marcantes, formas funcionalmente exageradas, arquitetura assimétrica e materiais naturais quentes, apresentado como um diorama 3D low-poly com acabamento pixel-art.**
>
> O jogador não constrói apenas uma cidade. Ele constrói uma sociedade que aprende a funcionar sem sua intervenção direta.

---

# 1. Objetivo deste documento

Este documento define a base visual e técnica para produzir todos os assets necessários ao primeiro MVP do jogo.

O objetivo é impedir que cada asset gerado por IA pareça pertencer a um jogo diferente. Todos os modelos devem obedecer às mesmas regras de:

- proporção;
- silhueta;
- cor;
- material;
- densidade de detalhe;
- escala;
- topologia;
- câmera;
- iluminação;
- leitura à distância;
- linguagem arquitetônica.

O pipeline principal será:

```text
Referência / Concept
        ↓
Meshy
        ↓
Modelo 3D low-poly
        ↓
Blender
- cleanup
- pivô
- escala
- UV/material
- redução de polígonos
        ↓
GLB
        ↓
Godot 4.x
        ↓
Câmera ortográfica isométrica
+ iluminação estilizada
+ baixa resolução interna
+ pixel shader
        ↓
Visual 2.5D pixel-art
```

---

# 2. Fantasia central do MVP

O MVP precisa provar esta sensação:

> **No início eu resolvia os problemas da vila manualmente. Agora construí sistemas para que a própria sociedade resolva esses problemas.**

O MVP não precisa provar:

- guerra em grande escala;
- diplomacia avançada;
- múltiplas cidades;
- sucessão dinástica;
- guildas complexas;
- religião;
- política regional;
- grandes muralhas.

Ele precisa provar:

1. construção orgânica;
2. famílias e trabalhadores;
3. produção;
4. logística;
5. armazenamento;
6. consumo;
7. crescimento;
8. comércio básico;
9. automação por políticas;
10. sociedade visualmente viva.

---

# 3. Escopo de gameplay do MVP

## População

Meta inicial:

```text
10–20 famílias
20–50 habitantes simulados
```

Visualmente, o jogador deve ver uma parte relevante desses habitantes circulando.

## Recursos principais

```text
Madeira
Pedra
Comida
Ferramentas
Moedas
```

Recursos de suporte:

```text
Troncos
Grãos
Lenha
```

O MVP pode abstrair ferro bruto e outros materiais inicialmente.

## Construções principais

```text
Centro da Vila / Casa do Administrador
Casa T1
Lenhador
Pedreira
Fazenda
Campo agrícola
Celeiro
Armazém
Mercado
Ferreiro
Poço
```

## Profissões principais

```text
Aldeão
Transportador
Lenhador
Pedreiro / trabalhador da pedreira
Agricultor
Ferreiro
Mercador
Administrador
```

## Fluxo-base

```text
Floresta
   ↓
Lenhador
   ↓
Madeira
   ↓
Armazém
   ↓
Construção / Ferraria
```

```text
Campo
  ↓
Agricultor
  ↓
Grãos / alimento
  ↓
Celeiro
  ↓
Mercado
  ↓
Famílias
```

```text
Pedreira
  ↓
Pedra
  ↓
Armazém
  ↓
Construções
```

```text
Madeira + recursos
      ↓
   Ferreiro
      ↓
 Ferramentas
      ↓
Produtividade
```

---

# 4. Identidade visual oficial

## Nome interno da direção

**Stylized Living Medieval Diorama**

## Descrição

Um mundo medieval low-poly artesanal, com:

- arquitetura orgânica;
- formas levemente tortas;
- silhuetas fortes;
- proporções exageradas apenas onde ajudam a comunicar função;
- materiais simples e pintados;
- cores naturais;
- pequenos acentos saturados;
- baixa densidade de detalhe;
- visual agradável tanto de perto quanto no zoom de RTS.

## Referências conceituais

Misturar princípios de:

- cidades orgânicas e críveis;
- leitura visual forte de RTS;
- formas estilizadas e exageradas;
- construções com função reconhecível pela silhueta;
- materiais pintados à mão;
- crescimento urbano irregular;
- sociedade visualmente ativa.

A referência de Warcraft deve ser usada pelos **princípios de legibilidade e estilização**, não para copiar arquitetura, personagens, facções ou assets.

---

# 5. Dez regras obrigatórias da Art Bible

1. **Silhueta antes de detalhe.**
2. **A função de um prédio deve ser reconhecida sem abrir UI.**
3. **Exagerar apenas elementos que comuniquem função.**
4. **Evitar simetria e perfeição geométrica excessivas.**
5. **Materiais devem parecer pintados, não fotografados.**
6. **Usar base natural e acentos de cor controlados.**
7. **NPCs precisam continuar legíveis em câmera distante.**
8. **A evolução da cidade precisa aparecer visualmente.**
9. **A economia precisa existir fisicamente no mundo.**
10. **O pixel shader complementa bons modelos; não corrige modelos ruins.**

---

# 6. Linguagem de formas

## Arquitetura

Preferir:

```text
telhados inclinados
vigas grossas
paredes ligeiramente tortas
chaminés assimétricas
pedras grandes
bases largas
formas legíveis
```

Evitar:

```text
paredes perfeitamente retas
telhados matematicamente simétricos
microdetalhes
ornamentos delicados
geometria realista demais
```

## Exagero funcional

### Ferraria

Exagerar:

```text
chaminé
bigorna
forja
pilha de carvão
martelo
```

### Moinho

Exagerar:

```text
roda
pás
estrutura vertical
```

### Mercado

Exagerar:

```text
toldos
bancas
caixas
bandeiras
```

### Celeiro

Exagerar:

```text
volume
portas
sacos
estrutura de armazenamento
```

---

# 7. Linguagem de materiais

## Madeira

- tons quentes;
- veios simplificados;
- variação pintada;
- sem textura fotográfica;
- contraste suficiente para sobreviver à pixelização.

## Pedra

- blocos grandes;
- formas irregulares;
- pouca rugosidade visual;
- cinza quente ou levemente azulado.

## Reboco

- creme;
- marfim sujo;
- bege claro;
- leve variação de tom.

## Metal

- escuro;
- highlights fortes;
- sem PBR realista complexo;
- leitura imediata.

## Tecido

Usar como acento visual.

Cores sugeridas:

```text
vermelho queimado
azul real dessaturado
ocre
verde escuro
vinho
```

---

# 8. Paleta conceitual

Não usar esta lista como limitação absoluta, mas como guia.

## Base

```text
Madeira clara      #8A6042
Madeira escura     #513929
Terra quente       #78553B
Barro              #9C6548
Palha              #C6A25D
Pedra quente       #77736B
Pedra clara        #A39A87
Reboco             #D2C5A4
Verde musgo        #5E7045
Verde floresta     #354B35
```

## Acentos

```text
Vermelho queimado  #A44735
Azul tecido        #49637A
Ocre institucional #C48C39
Verde guilda       #45694E
Vinho              #714555
```

---

# 9. Câmera e apresentação

## Projeção

```text
Camera3D
Projection: Orthographic
```

## Ângulo inicial recomendado

```text
Yaw: 45°
Pitch: aproximadamente 35°
```

O valor deve ser testado no protótipo, mas a câmera deve permanecer relativamente fixa.

## Controles previstos

```text
pan
zoom
rotação opcional em passos de 90°
```

Evitar câmera completamente livre no MVP.

## Resolução interna de referência

Primeiro teste:

```text
640 × 360
```

Upscale com nearest-neighbor.

Segundo teste:

```text
480 × 270
```

Usar a menor resolução apenas se a leitura dos NPCs continuar boa.

---

# 10. Pixel look

Pipeline:

```text
3D normal
   ↓
render interno em baixa resolução
   ↓
quantização leve de cores
   ↓
dithering sutil
   ↓
nearest upscale
   ↓
pixel-art look
```

Evitar pixelização agressiva.

O jogador deve continuar distinguindo:

```text
lenhador
agricultor
ferreiro
mercador
```

em zoom médio.

---

# 11. Especificações técnicas dos modelos

## Escala

Padronizar:

```text
1 unidade Godot = 1 metro
```

Altura aproximada de um adulto:

```text
1.75 unidades
```

## Budget de polígonos

### Props pequenos

```text
100–600 tris
```

### Props médios

```text
400–1.200 tris
```

### NPC

```text
2.000–4.000 tris
```

### Prédio pequeno

```text
1.000–3.000 tris
```

### Prédio médio

```text
2.000–5.000 tris
```

### Prédio importante

```text
até 8.000 tris
```

Esses números são metas, não limites rígidos.

## Materiais

Meta:

```text
1 material por prop
1–2 materiais por prédio
1 atlas por conjunto de NPCs
```

## Texturas

### Props

```text
64×64
128×128
```

### NPCs

```text
128×128
256×256 atlas
```

### Prédios

```text
128×128
256×256
```

Evitar 1K/2K/4K no MVP.

---

# 12. Pivôs e origem

## Construções

Pivot:

```text
centro da base
Y = solo
```

## Props

Pivot:

```text
base do objeto
```

## Personagens

Pivot:

```text
entre os pés
```

## Árvores

Pivot:

```text
centro do tronco no solo
```

---

# 13. Convenção de nomes

Formato:

```text
[CATEGORIA]_[TIPO]_[NOME]_[VARIANTE]
```

Exemplos:

```text
BLD_House_T1_A
BLD_Blacksmith_A
BLD_Granary_A

CHR_Villager_Body_A
CHR_Farmer_A
CHR_Blacksmith_A

PROP_Barrel_A
PROP_Crate_A
PROP_WoodPile_A

ENV_Oak_A
ENV_RockCluster_A

VEH_TradeCart_A
```

---

# 14. Estrutura de pastas

```text
game/
│
├── art/
│   ├── concepts/
│   │   ├── buildings/
│   │   ├── characters/
│   │   ├── environment/
│   │   └── props/
│   │
│   ├── meshy/
│   │   ├── raw/
│   │   └── exports/
│   │
│   ├── blender/
│   │   ├── buildings/
│   │   ├── characters/
│   │   ├── environment/
│   │   ├── props/
│   │   └── vehicles/
│   │
│   ├── textures/
│   │   ├── atlases/
│   │   ├── materials/
│   │   └── masks/
│   │
│   └── references/
│
├── godot/
│   ├── assets/
│   │   ├── buildings/
│   │   ├── characters/
│   │   ├── environment/
│   │   ├── props/
│   │   ├── resources/
│   │   ├── vehicles/
│   │   └── ui/
│   │
│   ├── materials/
│   ├── shaders/
│   ├── scenes/
│   └── animations/
│
└── docs/
    ├── art_bible.md
    ├── asset_manifest.md
    └── prompts.md
```

---

# 15. Pipeline de produção por asset

Cada asset deve passar por estas etapas:

```text
1. Definir função de gameplay
2. Definir silhueta
3. Gerar concept/reference
4. Gerar modelo no Meshy
5. Revisar silhueta
6. Blender cleanup
7. Ajustar escala
8. Ajustar pivot
9. Reduzir materiais
10. Exportar GLB
11. Importar na Godot
12. Testar na câmera oficial
13. Testar pixel shader
14. Testar junto aos outros assets
15. Aprovar ou rejeitar
```

Nunca aprovar um asset isoladamente apenas porque ele parece bonito.

---

# 16. Prompt-base global para Meshy

Este bloco deve ser reaproveitado em praticamente todos os modelos.

```text
Original stylized medieval low-poly game asset for an orthographic isometric strategy and settlement simulation game.

Art direction: handcrafted living medieval diorama, bold readable silhouette, exaggerated functional proportions, chunky geometric shapes, subtle asymmetry, slightly crooked handmade construction, warm natural materials, hand-painted texture aesthetic, strong color separation, charming but grounded medieval character.

Designed to remain clearly readable from a distant RTS camera.

Moderately exaggerated proportions without becoming cartoonish or chibi.

Clean low-poly topology, simplified geometry, large readable shapes, minimal micro-detail.

Designed for low-resolution pixel-shaded rendering in Godot.

No photorealism.
No modern elements.
No excessive fantasy ornamentation.
No tiny surface noise.
No text.
```

---

# 17. Negative prompt conceitual

Caso a ferramenta permita negative prompt, utilizar:

```text
photorealistic, realistic PBR scan, modern architecture, sci-fi, steampunk, high fantasy ornamentation, gothic overload, excessive decoration, tiny details, micro surface noise, perfect symmetry, glossy plastic, anime, chibi, toy-like proportions, text, logos, floating objects, broken topology, unnecessary interior
```

---

# 18. Manifesto de assets do MVP

## P0 — obrigatório

### Construções

```text
BLD_VillageHall_A
BLD_House_T1_A
BLD_House_T1_B
BLD_Woodcutter_A
BLD_Quarry_A
BLD_Farmstead_A
BLD_Granary_A
BLD_Warehouse_A
BLD_Market_A
BLD_Blacksmith_A
BLD_Well_A
```

### Ambiente

```text
ENV_Oak_A
ENV_Oak_B
ENV_Pine_A
ENV_Stump_A
ENV_Bush_A
ENV_RockCluster_A
ENV_RockCluster_B
ENV_GrassTuft_A
```

### Agricultura

```text
ENV_Wheat_Sprout
ENV_Wheat_Green
ENV_Wheat_Mature
ENV_Wheat_Harvested
```

### Props

```text
PROP_LogPile_A
PROP_WoodPile_A
PROP_StonePile_A
PROP_Crate_A
PROP_Barrel_A
PROP_GrainSack_A
PROP_Basket_A
PROP_Anvil_A
PROP_ToolRack_A
PROP_Fence_A
PROP_FencePost_A
PROP_Signpost_A
PROP_MarketStall_A
PROP_Handcart_A
```

### Personagens

```text
CHR_Commoner_A
CHR_Commoner_B
CHR_Administrator_A
```

Profissões devem reutilizar os corpos-base através de roupas, cores, ferramentas e acessórios.

### Ferramentas

```text
TOOL_Axe_A
TOOL_Pickaxe_A
TOOL_Hammer_A
TOOL_Hoe_A
TOOL_Sickle_A
TOOL_CarryBasket_A
```

## P1 — logo após o MVP

```text
VEH_TradeCart_A
CHR_Trader_A
CHR_Guard_A
TOOL_Spear_A
TOOL_Shield_A
BLD_TradingPost_A
BLD_Watchtower_A
PROP_Brazier_A
PROP_Lantern_A
ENV_FlowerPatch_A
ENV_DeadTree_A
```

---

# 19. Prompts — construções

---

## 19.1 Centro da Vila / Casa do Administrador

**Asset:** `BLD_VillageHall_A`

```text
Medieval village hall and administrator's lodge.

Original stylized medieval low-poly game asset for an orthographic isometric strategy and settlement simulation game.

A compact but important civic building that visually reads as the administrative center of a young village. Two-story timber-and-plaster construction, sturdy stone base, broad entrance, small covered porch, visible notice board, modest banner mount, slightly taller roofline than ordinary houses, one chimney, and a small attached records or storage room.

The building should feel important without looking like a castle.

Art direction: handcrafted living medieval diorama, bold readable silhouette, exaggerated functional proportions, chunky geometric shapes, subtle asymmetry, slightly crooked handmade construction, warm timber, pale plaster and stone materials, hand-painted texture aesthetic, strong color separation.

Designed to remain clearly readable from a distant RTS camera.

Clean low-poly topology, simplified geometry, large readable forms, medium detail, no modeled interior.

Designed for low-resolution pixel-shaded rendering in Godot.

No photorealism.
No modern elements.
No massive fortress elements.
No excessive fantasy ornamentation.
No tiny surface noise.
No text.
No characters.
```

---

## 19.2 Casa T1 A

**Asset:** `BLD_House_T1_A`

```text
Small medieval peasant house.

Original stylized medieval low-poly game asset for an orthographic isometric strategy and settlement simulation game.

A humble single-story timber-framed home with warm pale plaster walls, thick wooden beams, a raised stone foundation, steep straw roof, small crooked chimney, tiny entrance canopy, small wood pile and simple fenced vegetable patch.

The house should feel cozy, handmade and slightly asymmetrical.

The roof should dominate the silhouette and the building must remain immediately readable from a distant RTS camera.

Handcrafted living medieval diorama art direction, chunky geometric forms, moderately exaggerated proportions, warm natural materials, hand-painted texture aesthetic, strong color separation.

Clean low-poly topology, simplified geometry, large readable forms, low to medium detail.

No photorealism.
No modern elements.
No excessive fantasy ornamentation.
No perfect symmetry.
No tiny surface noise.
No interior.
No text.
No characters.
```

---

## 19.3 Casa T1 B

**Asset:** `BLD_House_T1_B`

```text
Small medieval artisan house variant.

Original stylized medieval low-poly game asset for an orthographic isometric strategy and settlement simulation game.

A compact handmade house with irregular timber frame, cream plaster, stone footing, clay-tile roof, short angled chimney, small side extension, simple exterior bench, barrel and stacked firewood.

Keep the same visual family as a humble peasant village but create a clearly different silhouette from the straw-roof house variant.

Handcrafted living medieval diorama style, bold readable silhouette, chunky proportions, subtle asymmetry, warm natural materials, hand-painted texture aesthetic.

Designed for distant RTS readability and low-resolution pixel-shaded rendering in Godot.

Clean low-poly topology.
Minimal micro-detail.
No photorealism.
No modern elements.
No excessive fantasy.
No interior.
No text.
No characters.
```

---

## 19.4 Lenhador

**Asset:** `BLD_Woodcutter_A`

```text
Medieval woodcutter hut and timber work yard.

Original stylized medieval low-poly production building for an orthographic isometric strategy game.

A rough open-sided timber shelter with a strongly readable sloped roof, oversized stacks of logs, chopping block, axe resting area, firewood piles and simple wooden supports.

The log storage and chopping area must dominate the silhouette and make the building immediately identifiable as a wood production site.

Handcrafted living medieval diorama style, chunky forms, subtle asymmetry, weathered warm wood, stone footings, hand-painted texture aesthetic.

Compact footprint, low to medium detail, clean low-poly topology, game-ready.

No photorealism.
No modern lumber equipment.
No excessive fantasy.
No text.
No characters.
No unnecessary interior.
```

---

## 19.5 Pedreira

**Asset:** `BLD_Quarry_A`

```text
Small medieval stone quarry worksite.

Original stylized medieval low-poly production asset for an orthographic isometric strategy and settlement simulation game.

A shallow exposed stone excavation with chunky carved rock faces, wooden support frame, simple lifting tripod, rope pulley, rough work shelter, stacked stone blocks, wheelbarrow area and scattered large stone chunks.

The excavated rock face and large stacked stone blocks should make the function immediately readable from a distant RTS camera.

Handcrafted living medieval diorama style, exaggerated readable forms, warm grey stone, weathered wood, hand-painted texture aesthetic.

Clean low-poly topology, medium detail, modular composition, no deep underground interior.

No photorealism.
No modern machinery.
No excessive fantasy.
No tiny surface noise.
No text.
No characters.
```

---

## 19.6 Fazenda

**Asset:** `BLD_Farmstead_A`

```text
Medieval farmstead for a small village.

Original stylized medieval low-poly game asset for an orthographic isometric strategy and settlement simulation game.

A practical timber-and-plaster farmhouse with attached open barn, straw roof, small tool lean-to, stacked hay, baskets, simple fence section and clear work area.

The barn opening, hay storage and farming props should immediately communicate agriculture.

Warm, inviting, handcrafted medieval diorama style with chunky proportions, slightly crooked construction, subtle asymmetry, natural earth colors and hand-painted texture aesthetic.

Designed to remain readable from a distant RTS camera.

Clean low-poly topology, medium detail, no interior complexity.

No photorealism.
No modern agriculture equipment.
No excessive fantasy ornamentation.
No tiny detail noise.
No text.
No characters.
```

---

## 19.7 Celeiro

**Asset:** `BLD_Granary_A`

```text
Medieval village granary.

Original stylized medieval low-poly storage building for an orthographic isometric strategy game.

A broad raised timber structure on a stone base, large double access doors, heavy roof, exposed structural beams, visible grain sacks and baskets near the entrance, small loading platform and compact protective overhang.

The broad body and oversized storage entrance must strongly communicate food storage.

Handcrafted living medieval diorama style, bold silhouette, chunky proportions, subtle asymmetry, warm wood, stone and muted grain colors, hand-painted texture aesthetic.

Designed for distant RTS readability and pixel-shaded rendering.

Clean low-poly topology, medium detail, no modeled interior.

No photorealism.
No modern elements.
No excessive fantasy.
No text.
No characters.
```

---

## 19.8 Armazém

**Asset:** `BLD_Warehouse_A`

```text
Medieval village warehouse.

Original stylized medieval low-poly logistics building for an orthographic isometric strategy and settlement simulation game.

A rectangular timber-and-stone warehouse with wide loading doors, covered loading platform, visible stacked crates, barrels and timber bundles, heavy roof, reinforced support beams and simple side storage awning.

The oversized loading doors and visible goods must clearly communicate general storage and logistics.

Handcrafted medieval diorama art direction, bold readable silhouette, chunky geometry, subtle asymmetry, warm natural materials and hand-painted texture aesthetic.

Clean low-poly topology, medium detail, no interior modeling required.

Designed for low-resolution pixel-shaded rendering in Godot.

No photorealism.
No modern elements.
No fantasy ornament overload.
No tiny surface noise.
No text.
No characters.
```

---

## 19.9 Mercado

**Asset:** `BLD_Market_A`

```text
Small medieval village market square asset.

Original stylized medieval low-poly commercial building cluster for an orthographic isometric strategy and settlement simulation game.

An open market area with two to four simple wooden stalls, broad cloth awnings, baskets, crates, sacks, barrels and a central notice or trade post.

Use warm natural materials with controlled saturated cloth accents such as muted red, ochre and desaturated blue.

The awnings and clustered stalls should create a highly recognizable commercial silhouette from a distant RTS camera.

Handcrafted living medieval diorama style, chunky geometry, slightly exaggerated proportions, subtle asymmetry and hand-painted texture aesthetic.

Clean low-poly topology, medium detail, modular feel.

No photorealism.
No modern elements.
No excessive fantasy.
No text.
No characters.
```

---

## 19.10 Ferraria

**Asset:** `BLD_Blacksmith_A`

```text
Medieval village blacksmith workshop.

Original stylized medieval low-poly game asset for an orthographic isometric strategy and settlement simulation game.

A compact but sturdy timber-and-stone workshop with an oversized crooked stone chimney that immediately identifies the building as a blacksmith.

Large visible outdoor forge, oversized anvil, stacked firewood, coal sacks, timber support beams and a heavy irregular roof.

The forge and chimney should dominate the silhouette.

Art direction: handcrafted living medieval diorama, bold readable silhouette, exaggerated functional proportions, chunky geometric shapes, subtle asymmetry, slightly crooked handmade construction, warm timber, plaster and stone materials, hand-painted texture aesthetic and strong color separation.

Charming and slightly exaggerated, but grounded and believable as a functioning medieval building.

Designed to remain recognizable from a distant RTS camera.

Clean low-poly topology.
Large forms.
Minimal micro-detail.
No interior modeling required.

Designed for low-resolution pixel-shaded rendering in Godot.

No photorealism.
No modern elements.
No excessive fantasy ornamentation.
No tiny surface noise.
No text.
No characters.
```

---

## 19.11 Poço

**Asset:** `BLD_Well_A`

```text
Small medieval village stone well.

Original stylized low-poly prop-building for an orthographic isometric strategy game.

Circular chunky stone base, thick wooden roof supports, small uneven shingle roof, visible rope, oversized bucket and simple crank mechanism.

The silhouette should remain readable at RTS distance.

Handcrafted medieval diorama style, warm stone and weathered wood, slightly exaggerated proportions, subtle asymmetry and hand-painted texture aesthetic.

Clean low-poly topology, simple geometry, game-ready.

No photorealism.
No modern elements.
No excessive ornamentation.
No text.
No characters.
```

---

# 20. Prompts — ambiente

---

## 20.1 Carvalho A

**Asset:** `ENV_Oak_A`

```text
Stylized medieval low-poly oak tree for an orthographic isometric strategy game.

Broad irregular canopy made from several chunky foliage masses, thick slightly crooked trunk, strong readable silhouette, moderate asymmetry and handcrafted diorama appearance.

Warm natural greens with slightly muted saturation, simplified bark, large shapes, no individual leaves.

Designed to remain readable under low-resolution pixel-shaded rendering.

Clean low-poly topology.

No photorealism.
No tiny leaves.
No perfect sphere canopy.
No fantasy glow.
No text.
```

---

## 20.2 Carvalho B

**Asset:** `ENV_Oak_B`

```text
Stylized low-poly oak tree variant for an orthographic medieval strategy game.

Tall uneven trunk with a wider asymmetrical crown, several large clustered foliage masses, visible branching near the silhouette, warm natural green palette and handcrafted diorama style.

Must belong to the same visual family as other village trees while having a clearly different silhouette.

Large simple forms, clean low-poly topology, no individual leaves.

No photorealism.
No tiny detail noise.
No fantasy elements.
```

---

## 20.3 Pinheiro

**Asset:** `ENV_Pine_A`

```text
Stylized medieval low-poly pine tree for an orthographic isometric strategy game.

Tall tapered silhouette, layered chunky branch masses, slightly crooked trunk, irregular handmade appearance, dark desaturated green foliage and simple warm brown bark.

Strong readable shape from a distant RTS camera, handcrafted diorama aesthetic.

Clean low-poly topology, large forms, no individual needles.

No photorealism.
No perfect symmetry.
No fantasy glow.
```

---

## 20.4 Toco

**Asset:** `ENV_Stump_A`

```text
Stylized low-poly chopped tree stump for a medieval isometric strategy game.

Wide chunky stump with simplified cut rings, a few visible roots, small wood chips and slightly irregular shape.

Warm hand-painted wood colors, handcrafted diorama style, very readable at small scale.

Clean simple topology.

No photorealism.
No tiny detail noise.
```

---

## 20.5 Arbusto

**Asset:** `ENV_Bush_A`

```text
Stylized low-poly medieval countryside bush for an orthographic strategy game.

Compact irregular cluster of chunky foliage shapes, warm natural green palette, small visible branches, handcrafted diorama style.

Readable from distance, simple silhouette, clean low-poly topology.

No individual leaves.
No photorealism.
No fantasy glow.
```

---

## 20.6 Rochas A

**Asset:** `ENV_RockCluster_A`

```text
Stylized low-poly medieval environment rock cluster for an orthographic isometric strategy game.

Three to five chunky irregular stones with broad planar faces, warm grey and muted brown tones, varied size and asymmetrical composition.

Handcrafted diorama style, clear silhouette, clean low-poly geometry.

No photorealism.
No tiny cracks.
No glossy material.
```

---

## 20.7 Rochas B

**Asset:** `ENV_RockCluster_B`

```text
Stylized low-poly stone outcrop for an orthographic medieval strategy game.

A larger irregular central boulder with two smaller surrounding stones, angular chunky planes, subtle warm-grey color variation and handcrafted diorama appearance.

Readable from RTS distance, low detail, clean topology.

No photorealism.
No tiny surface noise.
No fantasy crystals.
```

---

# 21. Agricultura — estágios de plantação

Os estágios devem compartilhar a mesma footprint.

---

## 21.1 Broto

**Asset:** `ENV_Wheat_Sprout`

```text
Stylized low-poly young wheat crop cluster for an orthographic medieval strategy game.

Short sparse green sprouts arranged in simple readable rows, chunky simplified blades, handcrafted diorama style, designed to be instanced repeatedly across farm fields.

Very low polygon count and clear color separation.

No photorealism.
No individual complex leaves.
```

---

## 21.2 Crescimento

**Asset:** `ENV_Wheat_Green`

```text
Stylized low-poly growing wheat crop cluster for an orthographic medieval strategy game.

Medium-height dense green stalks with simplified chunky shapes, organized in readable rows, handcrafted diorama style and designed for repeated instancing.

Very low polygon count.

No photorealism.
No tiny plant detail.
```

---

## 21.3 Maduro

**Asset:** `ENV_Wheat_Mature`

```text
Stylized low-poly mature wheat crop cluster for an orthographic medieval strategy game.

Dense golden stalks with oversized simplified wheat heads, strong warm color, readable rows, handcrafted diorama aesthetic and designed for repeated field instancing.

Very low polygon count and clear silhouette.

No photorealism.
No micro-detail.
```

---

## 21.4 Colhido

**Asset:** `ENV_Wheat_Harvested`

```text
Stylized low-poly harvested wheat field cluster for an orthographic medieval strategy game.

Short golden-brown stubble with a few small tied sheaves, simple readable rows, handcrafted diorama aesthetic, designed for repeated instancing.

Very low polygon count.

No photorealism.
No tiny surface detail.
```

---

# 22. Prompts — props e recursos

---

## 22.1 Pilha de troncos

**Asset:** `PROP_LogPile_A`

```text
Stylized low-poly medieval log pile for an orthographic isometric strategy game.

Stack of six to ten thick cut logs with exaggerated round ends, warm hand-painted wood colors, slightly irregular arrangement and strong readable silhouette.

Chunky simplified geometry, handcrafted diorama style, game-ready.

No photorealism.
No tiny bark details.
No text.
```

---

## 22.2 Pilha de lenha

**Asset:** `PROP_WoodPile_A`

```text
Stylized low-poly medieval firewood pile for an orthographic isometric strategy game.

Compact stack of split firewood pieces arranged under a very small rough wooden cover, chunky shapes, warm brown palette and handcrafted diorama style.

Readable from distance, clean low-poly topology.

No photorealism.
No tiny wood splinters.
```

---

## 22.3 Pilha de pedra

**Asset:** `PROP_StonePile_A`

```text
Stylized low-poly medieval construction stone pile for an orthographic isometric strategy game.

Irregular stack of chunky cut stones and rough rocks in warm grey tones, asymmetrical arrangement, broad planar faces and handcrafted diorama aesthetic.

Clear readable silhouette and clean low-poly geometry.

No photorealism.
No tiny cracks.
```

---

## 22.4 Caixa

**Asset:** `PROP_Crate_A`

```text
Stylized low-poly medieval wooden storage crate for an orthographic isometric strategy game.

Chunky wooden planks, thick corner braces, simple square construction, warm hand-painted wood colors and slightly uneven handmade appearance.

Very clean low-poly topology, clear silhouette.

No photorealism.
No metal modern hardware.
No text or logos.
```

---

## 22.5 Barril

**Asset:** `PROP_Barrel_A`

```text
Stylized low-poly medieval wooden barrel for an orthographic isometric strategy game.

Chunky rounded wooden body with thick dark iron bands, slightly exaggerated proportions and hand-painted material aesthetic.

Strong readable silhouette, simple clean topology.

No photorealism.
No tiny surface detail.
No text.
```

---

## 22.6 Saco de grãos

**Asset:** `PROP_GrainSack_A`

```text
Stylized low-poly medieval grain sack for an orthographic strategy game.

Bulky tied cloth sack with slightly exaggerated rounded form, warm beige fabric, subtle hand-painted shading and a small visible grain opening detail.

Readable from distance, low polygon count.

No photorealism.
No text.
No logos.
```

---

## 22.7 Cesto

**Asset:** `PROP_Basket_A`

```text
Stylized low-poly medieval woven basket for an orthographic strategy game.

Large practical carrying basket with thick simplified weave pattern, warm brown material, chunky handle and strong readable silhouette.

Handcrafted diorama style, low polygon count.

No photorealism.
No tiny weave detail.
```

---

## 22.8 Bigorna

**Asset:** `PROP_Anvil_A`

```text
Stylized low-poly medieval blacksmith anvil for an orthographic strategy game.

Heavy oversized iron anvil with chunky horn, broad top surface and thick base, dark metal with strong painted highlights.

Functional proportions slightly exaggerated for RTS readability.

Clean low-poly topology.

No photorealism.
No modern industrial design.
```

---

## 22.9 Rack de ferramentas

**Asset:** `PROP_ToolRack_A`

```text
Stylized low-poly medieval workshop tool rack for an orthographic strategy game.

Simple wooden frame holding several oversized readable tools such as hammer, tongs and small axe, chunky construction and warm hand-painted materials.

Designed to read clearly from a distant camera.

Clean low-poly topology.

No photorealism.
No modern tools.
No text.
```

---

## 22.10 Cerca modular

**Asset:** `PROP_Fence_A`

```text
Stylized low-poly medieval village wooden fence segment for an orthographic strategy game.

Rough uneven vertical posts connected by two horizontal rails, slightly crooked handmade construction, warm weathered wood and chunky proportions.

Modular straight segment, clean topology and simple silhouette.

No photorealism.
No perfect symmetry.
No modern hardware.
```

---

## 22.11 Poste de cerca

**Asset:** `PROP_FencePost_A`

```text
Stylized low-poly medieval wooden fence post for an orthographic strategy game.

Single thick rough-cut timber post with slightly uneven top and simple handmade shape.

Warm weathered wood, very low polygon count, designed to match modular fence segments.

No photorealism.
```

---

## 22.12 Placa de estrada

**Asset:** `PROP_Signpost_A`

```text
Stylized low-poly medieval wooden signpost for an orthographic strategy game.

Tall crooked wooden post with two blank directional boards, thick readable shapes, small stone footing and warm hand-painted wood.

No readable text on the boards.

Handcrafted diorama style, clean low-poly topology.

No photorealism.
No modern signage.
No letters.
No logos.
```

---

## 22.13 Banca de mercado

**Asset:** `PROP_MarketStall_A`

```text
Stylized low-poly medieval market stall for an orthographic strategy game.

Simple timber frame, broad cloth canopy, countertop, crates and baskets, chunky exaggerated proportions and strong readable commercial silhouette.

Use warm wood and one controlled muted cloth accent color.

Handcrafted diorama style, clean low-poly topology.

No photorealism.
No text.
No characters.
```

---

## 22.14 Carrinho de mão

**Asset:** `PROP_Handcart_A`

```text
Stylized low-poly medieval two-wheel handcart for an orthographic strategy game.

Compact wooden cargo cart with oversized readable wheels, thick handles, simple plank body and slightly irregular handmade construction.

Designed for villagers to push or pull during logistics tasks.

Warm hand-painted wood, chunky geometry, clean low-poly topology.

No photorealism.
No modern hardware.
No cargo included.
```

---

# 23. Personagens — estratégia modular

Não gerar dez personagens completamente separados.

Criar:

```text
2 corpos-base
+
cabeças/cabelos
+
roupas
+
ferramentas
+
acessórios
```

Assim:

```text
Commoner Base
      ↓
  acessórios
      ↓
Farmer
Woodcutter
Quarry Worker
Blacksmith
Hauler
Merchant
Administrator
```

---

# 24. Prompt — corpo-base A

**Asset:** `CHR_Commoner_A`

```text
Full-body stylized medieval low-poly villager character for an orthographic isometric strategy game.

Neutral adult commoner body, approximately five and a half heads tall, slightly oversized head, hands and boots for strong RTS readability.

Broad simple torso, chunky limbs, simple medieval tunic, belt, trousers and leather boots.

Warm grounded proportions, charming handcrafted diorama style, clear silhouette and strong color separation.

Neutral A-pose for rigging.
Symmetrical body pose.
No equipment.
No held objects.
No cape.
No armor.
No complex accessories.

Clean low-poly topology suitable for animation and shared rigging.

Designed for low-resolution pixel-shaded rendering in Godot.

No photorealism.
No anime.
No chibi.
No excessive fantasy.
No text.
```

---

# 25. Prompt — corpo-base B

**Asset:** `CHR_Commoner_B`

```text
Full-body stylized medieval low-poly villager character variant for an orthographic isometric strategy game.

Neutral adult commoner with a different body silhouette from the primary villager: slightly narrower shoulders, different face shape and clothing cut while preserving the same rig-friendly proportions.

Approximately five and a half heads tall with slightly oversized head, hands and boots for RTS readability.

Simple medieval tunic or dress-inspired work clothing, belt, practical trousers or lower garment and sturdy boots.

Neutral A-pose for rigging.
No equipment.
No held objects.
No armor.
No cape.

Handcrafted living medieval diorama style, warm grounded materials, strong simple shapes, clean low-poly topology suitable for shared animation rig.

No photorealism.
No anime.
No chibi.
No excessive fantasy.
No text.
```

---

# 26. Prompt — administrador

**Asset:** `CHR_Administrator_A`

```text
Full-body stylized medieval village administrator character for an orthographic isometric strategy game.

Adult civic official with slightly more refined clothing than common villagers: long practical tunic, modest short mantle, leather belt, document pouch and small ledger book.

The silhouette should communicate authority and organization without looking noble, royal or heavily wealthy.

Approximately five and a half heads tall with slightly exaggerated hands and head for RTS readability.

Neutral A-pose for rigging.

Handcrafted medieval diorama style, grounded colors with one controlled deep blue or burgundy accent, simple clean geometry and strong readable silhouette.

Clean low-poly topology compatible with animation.

No photorealism.
No heavy armor.
No crown.
No excessive fantasy.
No magical elements.
No text.
```

---

# 27. Profissões — kit visual

As profissões devem preferencialmente usar os corpos-base.

## Lenhador

```text
Base body
+
capuz simples
+
avental curto
+
machado
+
tons verde/marrom
```

## Agricultor

```text
Base body
+
chapéu largo
+
sickle/hoe
+
tons palha/verde
```

## Pedreira

```text
Base body
+
capuz ou faixa
+
pickaxe
+
tons cinza/marrom
```

## Ferreiro

```text
Base body
+
avental escuro
+
luvas grandes
+
hammer
+
tons carvão/vermelho queimado
```

## Transportador

```text
Base body
+
cinto
+
luvas simples
+
basket/crate/handcart
+
tons neutros
```

## Mercador

```text
Base body
+
roupa mais limpa
+
bolsa
+
tecido azul ou vinho
```

---

# 28. Ferramentas — prompts

---

## Machado

**Asset:** `TOOL_Axe_A`

```text
Stylized low-poly medieval woodcutter axe for an orthographic strategy game.

Oversized readable iron axe head with thick wooden handle, chunky simplified geometry, functional medieval design, strong silhouette and hand-painted material style.

Scaled slightly larger than realistic for RTS readability.

Clean low-poly topology.

No photorealism.
No fantasy runes.
No decorative weapon styling.
```

---

## Picareta

**Asset:** `TOOL_Pickaxe_A`

```text
Stylized low-poly medieval worker pickaxe for an orthographic strategy game.

Large simple iron pick head with thick wooden handle, slightly exaggerated proportions for distant readability, handcrafted practical appearance and clean low-poly geometry.

No photorealism.
No fantasy ornamentation.
No modern design.
```

---

## Martelo

**Asset:** `TOOL_Hammer_A`

```text
Stylized low-poly medieval blacksmith hammer for an orthographic strategy game.

Heavy oversized iron hammer head with thick wooden handle, chunky proportions and strong readable silhouette.

Handcrafted practical medieval design, clean low-poly topology, hand-painted material style.

No photorealism.
No fantasy weapon styling.
```

---

## Enxada

**Asset:** `TOOL_Hoe_A`

```text
Stylized low-poly medieval farming hoe for an orthographic strategy game.

Long thick wooden handle with broad simple iron blade, slightly exaggerated dimensions for RTS readability, handcrafted practical design and clean low-poly topology.

No photorealism.
No modern elements.
```

---

## Foice

**Asset:** `TOOL_Sickle_A`

```text
Stylized low-poly medieval farming sickle for an orthographic strategy game.

Large curved iron blade with thick short wooden handle, exaggerated readable silhouette, practical medieval tool design and clean low-poly topology.

No photorealism.
No fantasy weapon design.
```

---

## Cesto carregável

**Asset:** `TOOL_CarryBasket_A`

```text
Stylized low-poly medieval carrying basket for a villager character.

Large practical woven basket sized to be clearly visible from a distant RTS camera, thick simplified weave, chunky handles and warm brown material.

Clean low-poly topology and handcrafted diorama style.

No photorealism.
No tiny weave detail.
```

---

# 29. Animações necessárias para o MVP

Criar um rig compartilhado.

## Core

```text
idle
walk
turn
carry_idle
carry_walk
```

## Trabalho

```text
chop
mine
farm_hoe
harvest_sickle
hammer_anvil
pickup_ground
drop_ground
push_handcart
```

## Social

```text
talk
point
inspect
```

## Regras de animação

A animação deve ser:

```text
ampla
clara
ligeiramente exagerada
com poses fortes
```

Exemplo:

```text
anticipation
   ↓
ação principal
   ↓
impacto
   ↓
recovery
```

Não priorizar realismo de captura de movimento.

Priorizar leitura.

---

# 30. Prompt para carroça comercial — P1

**Asset:** `VEH_TradeCart_A`

```text
Stylized low-poly medieval trade cart for an orthographic isometric strategy game.

Medium-sized wooden merchant cart with oversized readable wheels, sturdy plank cargo bed, small cloth cover frame, visible ropes and space for crates, sacks and barrels.

Slightly exaggerated practical proportions, handmade asymmetry, warm weathered wood and muted cloth accent.

Designed to remain clearly readable from a distant RTS camera.

Clean low-poly topology, game-ready, hand-painted material aesthetic.

No photorealism.
No modern hardware.
No excessive fantasy.
No text.
No animals included.
```

---

# 31. Resource presentation

Recursos não devem existir apenas como ícones.

Quando possível, o estoque físico deve aparecer no mundo.

## Madeira

Representação:

```text
PROP_LogPile_A
PROP_WoodPile_A
```

## Pedra

```text
PROP_StonePile_A
```

## Comida

```text
PROP_GrainSack_A
PROP_Basket_A
```

## Ferramentas

```text
PROP_ToolRack_A
```

## Moedas

Moedas podem permanecer principalmente na UI no MVP.

Não criar pilhas de ouro espalhadas pela vila.

---

# 32. Variantes de cor e materiais

Não gerar modelos separados quando apenas uma mudança de material resolve.

Exemplo:

```text
House_A
```

pode possuir:

```text
Roof_Straw
Roof_Clay
Roof_DarkWood
```

Mercado:

```text
Canopy_Red
Canopy_Blue
Canopy_Ochre
```

NPC:

```text
Tunic_Brown
Tunic_Green
Tunic_Beige
Tunic_Blue
```

Variar cor sem destruir a identidade da vila.

---

# 33. Estratégia de modularidade

## Casas

Criar módulos opcionais:

```text
chimney_A
chimney_B
woodpile
small_garden
bench
barrel
side_shed
```

Assim:

```text
House_A
+ chimney_B
+ garden
+ woodpile
```

gera uma casa diferente sem novo prédio completo.

## Mercado

```text
stall_A
stall_B
crate_group
barrel_group
basket_group
```

## Armazém

```text
crate_group_A
crate_group_B
wood_bundle
stone_bundle
```

---

# 34. Terreno

O terreno não deve usar meshes de IA para tudo.

Criar proceduralmente no Godot ou Blender.

## Materiais necessários

```text
MAT_Grass
MAT_Dirt
MAT_ForestGround
MAT_FieldSoil
MAT_RoadDirt
MAT_RoadPacked
MAT_Stone
```

## Regras

- baixo ruído;
- macroformas legíveis;
- cores controladas;
- sem texturas fotográficas;
- transições suaves;
- pixel shader fará parte do acabamento.

---

# 35. Estradas

Para o MVP:

```text
trilha
estrada de terra
```

Pós-MVP:

```text
estrada consolidada
estrada de pedra
rota comercial principal
```

Estradas devem ser splines/meshes geradas pelo jogo, não assets únicos pré-montados.

---

# 36. Efeitos visuais

Não usar Meshy.

Criar na Godot.

## P0

```text
chimney_smoke
forge_smoke
forge_sparks
dust_walk
construction_dust
selection_outline
building_placement_ghost
resource_pickup_feedback
```

## P1

```text
rain
snow
fog
fire
torch
brazier
```

---

# 37. Iluminação

## Dia

- luz quente;
- sombras legíveis;
- sem contraste fotográfico extremo.

## Noite

- azul dessaturado no ambiente;
- luz quente em:
  - janelas;
  - forja;
  - mercado;
  - lanternas.

## Regra

A luz deve ajudar a ler função.

Exemplo:

```text
Ferreiro
→ forge glow
```

```text
Casa ocupada
→ janela quente
```

---

# 38. Estações

O MVP pode começar com:

```text
Verão
Inverno simplificado
```

Depois:

```text
Primavera
Verão
Outono
Inverno
```

Preferir alteração via:

```text
material parameter
shader mask
vegetation state
```

em vez de criar quatro modelos completamente diferentes.

---

# 39. LOD e performance

Mesmo no MVP, preparar o pipeline.

## LOD0

Perto:

```text
modelo completo
props
animações
partículas
```

## LOD1

Médio:

```text
menos props
menos geometria
menos animações
```

## LOD2

Longe:

```text
silhueta simplificada
animação reduzida
sem props pequenos
```

---

# 40. NPCs e performance

Nunca assumir:

```text
1 cidadão simulado = 1 agente visual complexo permanente
```

Separar:

```text
SimulationCitizen
```

de:

```text
VisualAgent
```

Exemplo:

```text
50 habitantes simulados
30 visíveis
20 agregados
```

No futuro:

```text
2.000 habitantes simulados
100–200 visíveis
```

---

# 41. Assets que NÃO devemos produzir ainda

Para evitar escopo excessivo:

```text
castelo
catedral
cavalaria
armaduras completas
cerco
navios
portos grandes
palácio
muralhas complexas
máquinas de guerra
magia
monstros
grandes monumentos
```

Esses assets não ajudam a provar o loop central do MVP.

---

# 42. Ordem de produção recomendada

## Sprint Art 01 — linguagem visual

Produzir apenas:

```text
House_T1_A
Blacksmith_A
Oak_A
Commoner_A
Crate_A
```

Importar todos na Godot.

Testar juntos.

Se parecerem pertencer ao mesmo jogo, seguir.

---

## Sprint Art 02 — vila funcional

```text
VillageHall
House_T1_B
Woodcutter
Farmstead
Granary
Warehouse
Well
Market
```

---

## Sprint Art 03 — cadeia econômica

```text
Quarry
StonePile
LogPile
GrainSack
Basket
Anvil
ToolRack
Handcart
MarketStall
Fence
```

---

## Sprint Art 04 — população

```text
Commoner_B
Administrator
Axe
Pickaxe
Hammer
Hoe
Sickle
CarryBasket
```

Criar rig e animações.

---

## Sprint Art 05 — natureza

```text
Oak_B
Pine_A
Stump
Bush
RockCluster_A
RockCluster_B
Wheat stages
```

---

# 43. Primeiro teste visual obrigatório

Montar uma única cena:

```text
TEST_VILLAGE_01

10 casas
1 Centro da Vila
1 lenhador
1 fazenda
2 campos
1 celeiro
1 armazém
1 mercado
1 ferraria
1 poço

20 árvores
10 clusters de pedras
20 NPCs
3 carrinhos
1 estrada principal
```

Se esta cena não estiver visualmente coerente, não gerar mais assets.

---

# 44. Checklist de aprovação por asset

Antes de aprovar:

```text
[ ] Reconheço o asset pela silhueta?
[ ] A função está visualmente clara?
[ ] Ele combina com os outros assets?
[ ] A proporção está correta?
[ ] O pivot está correto?
[ ] A escala está correta?
[ ] Possui poucos materiais?
[ ] Continua legível com pixel shader?
[ ] Continua legível em zoom médio?
[ ] Não possui microdetalhes inúteis?
[ ] Não parece fotorealista?
[ ] Não parece um asset genérico de fantasy pack?
[ ] Não copia diretamente Warcraft ou outra IP?
[ ] Está pronto para GLB/Godot?
```

---

# 45. Checklist específico de NPC

```text
[ ] Silhueta legível?
[ ] Ferramenta visível?
[ ] Cabeça/mãos suficientemente grandes?
[ ] Profissão reconhecível?
[ ] Funciona com o rig compartilhado?
[ ] A-pose limpa?
[ ] Sem acessórios atravessando corpo?
[ ] Poucos materiais?
[ ] Performance adequada?
```

---

# 46. Checklist específico de construção

```text
[ ] Reconhecível sem UI?
[ ] Elemento funcional dominante?
[ ] Telhado e base bem separados?
[ ] Silhueta assimétrica suficiente?
[ ] Entrada legível?
[ ] Não depende de microdetalhe?
[ ] Funciona em 4 rotações?
[ ] Props podem ser separados?
[ ] Espaço de footprint claro?
```

---

# 47. Definition of Done do pacote MVP

O pacote de arte do MVP está pronto quando:

1. todas as construções P0 estiverem no Godot;
2. todos os props P0 estiverem no Godot;
3. todos os NPCs compartilharem um rig compatível;
4. animações básicas funcionarem;
5. pixel shader estiver aplicado;
6. a cena TEST_VILLAGE_01 estiver visualmente coerente;
7. nenhuma construção depender de UI para comunicar sua função;
8. o jogo permanecer legível em câmera distante;
9. o frame rate permanecer estável no cenário de teste;
10. a linguagem visual estiver consistente.

---

# 48. Prioridade absoluta

Se houver conflito entre:

```text
mais detalhe
```

e:

```text
melhor leitura
```

escolher:

```text
melhor leitura
```

Se houver conflito entre:

```text
mais realismo
```

e:

```text
mais personalidade
```

escolher:

```text
mais personalidade
```

Se houver conflito entre:

```text
asset individual muito bonito
```

e:

```text
coerência do conjunto
```

escolher:

```text
coerência do conjunto
```

---

# 49. Resumo do pipeline definitivo

```text
VISUAL IDENTITY
Stylized Living Medieval Diorama
        ↓
CONCEPT
silhueta + função
        ↓
MESHY
low-poly base
        ↓
BLENDER
cleanup + scale + pivot + material
        ↓
GLB
        ↓
GODOT
orthographic camera
        ↓
PIXEL PIPELINE
low-res + quantization + dithering
        ↓
GAME
living medieval society
```

---

# 50. Regra final

Cada asset criado para o projeto deve responder positivamente a esta pergunta:

> **Quando o jogador olhar para a vila sem abrir nenhum menu, ele consegue entender melhor como aquela sociedade funciona?**

Se a resposta for não, o asset precisa ser simplificado, redesenhado ou removido.

