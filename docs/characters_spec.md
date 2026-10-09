# Aldeões low-poly no padrão Kenney — especificação

> Branch `feature/characters-kenney`. Status: **rascunho para aprovação** (Etapa 1). Nada modelado ainda.
> Fonte da verdade: scripts Python em `tools/blender/`; o Blender MCP serve só para inspecionar e iterar.
> Referências: decisão de 09/10/2026 (mundo na família Kenney), `asset_manifest.md` §9.2 e P39.
> O `docs/briefing_2026-10-09.md` e a `asset_production_bible_mvp.md` não estão no repo (ver P40); as seções da Bible
> citadas aqui (§13 nomes, §29 animação) seguem o que o pedido descreve.

## 1. Escala medida no projeto

| Item | Medida no pacote | Escala no jogo | Em metros |
|---|---|---|---|
| Módulo de parede Fantasy Town (`wall-door`) | 1,0 u de altura | ×2 (`TOWN_SCALE`) | 2,0 m |
| Vão da porta (`door`, igual em `wall-wood-door`) | 0,75 × 0,40 u | ×2 | **1,50 m × 0,80 m** |
| Props/árvores do Nature Kit | — | ×4 | — |
| Câmera oficial | pitch 50°, FOV 32°, distância 30 / **62** / 110 m (perto / médio / longe) | — | — |

**Escolhido — altura final: 1,30 m sem chapéu, no máximo 1,40 m com chapéu.** Sobram 10 cm na porta no pior caso e
cabe com folga na largura (ombros ≈ 0,40 m, porta 0,80 m).

**Escolhido — orientação: frente para −Y no Blender = +Z na Godot** (convenção do projeto, `WorldView.cs`:
"models face +Z"); +Y up no glTF; pivô entre os pés, no chão.

## 2. Estilo

- Low-poly no padrão Kenney: caixas e cilindros de 6–8 lados com chanfro leve (1 segmento), sem detalhe fino, sem
  normal map, sem textura de detalhe. Sombreamento flat nas faces chanfradas, suave só onde a forma é redonda (cabeça,
  chapéu).
- **Um material para todos os personagens e ferramentas** (`MAT_Villager_Atlas`): albedo de uma textura de paleta
  `T_Villager_Palette.png` 64×64, filtro nearest, sem mipmap. Roughness 1, sem metallic (como o Kenney).
- Leitura de longe (zoom médio, 62 m): silhueta e duas ou três manchas de cor por aldeão (chapéu, torso, pernas).
  Ferramenta e carga maiores que o real (~1,3×) para ler a profissão.
- Rosto mínimo: dois olhos em bloco escuro (2 caixas de 4 tris visíveis), sem boca. Bochecha rosada opcional
  (quadrado de cor, sem geometria extra).

### 2.1 Atlas de paleta (64×64, grade 8×8 de células de 8 px)

As UVs de cada face caem no **centro** de uma célula (sem sangramento com filtro). Cada **linha** do atlas é um
papel; cada **coluna** é uma variação. Trocar a cor de um papel = somar um deslocamento de coluna àquela linha.

| Linha | Papel | Colunas 0–7 |
|---|---|---|
| 0 | Pele | `#F1CFAE` `#DDA77F` `#B57A52` `#7A4E33` + 4 reservas |
| 1 | Cabelo | castanho-escuro `#3E2A1F`, castanho `#6E4429`, loiro-palha `#D2B062`, ruivo `#9C4A26`, grisalho `#A6A09A` + 3 reservas |
| 2 | Tecido A (principal) | linho `#D2C5A4`, verde musgo `#5E7045`, vermelho queimado `#A44735`, azul tecido `#49637A`, ocre `#C48C39`, vinho `#714555`, palha `#C6A25D`, madeira `#8A6042` |
| 3 | Tecido B (secundário) | mesmas 8 cores da linha 2, escolha independente |
| 4 | Couro / madeira | madeira `#8A6042`, couro `#6B4A33`, couro escuro `#4E3526`, madeira clara `#A87C55` + reservas |
| 5 | Fibras | palha `#C6A25D`, palha escura `#9E7E44`, corda `#B59A6E`, vime `#A57F4D` + reservas |
| 6 | Metal | ferro `#8E959B`, ferro escuro `#5B6166`, bronze `#A57A45` + reservas |
| 7 | Fixos | olho `#2B2420`, bochecha `#D98C7A`, faixa do reeve `#49637A`, capa do livro `#714555`, páginas `#E9DFC6`, flecha/pena `#E2DCCB` + reservas |

**Ajuste na Godot (iteração 3 do protótipo):** com a luz do jogo, os tons do pedido (V 0,42–0,54 nos tecidos e
madeiras) ficaram escuros ao lado do colormap do Fantasy Town (V 0,65–0,95, S ~0,5–0,6). Os **matizes ficam**; o
valor sobe: `V' = 0,3 + 0,7·V` em tecido, couro, fibra e metal, `V' = 0,2 + 0,8·V` no cabelo, saturação ×1,05.
Pele e cores fixas não mudam. Valores em uso (`tools/blender/build_villager.py`, `PALETTE`):

| Linha | Pedido → em uso |
|---|---|
| 1 Cabelo | `3E2A1F→654330` `6E4429→8B532F` `D2B062→DBB660` `9C4A26→B04F24` `A6A09A→B8B1AA` |
| 2/3 Tecido | `D2C5A4→E0D1AC` `5E7045→819B5C` `A44735→BF4D37` `49637A→5E82A2` `C48C39→D69637` `714555→9C5C73` `C6A25D→D7AE5F` `8A6042→AD764E` |
| 4 Couro/madeira | `8A6042→AD764E` `6B4A33→976644` `4E3526→83573D` `A87C55→C28D5D` |
| 5 Fibras | `C6A25D→D7AE5F` `9E7E44→BB934B` `B59A6E→CBAB78` `A57F4D→C09254` |
| 6 Metal | `8E959B→A9B1B9` `5B6166→838C94` `A57A45→C08B4B` |

Na Godot a paleta é importada **sem compressão e sem mipmap**, com filtro *nearest* (`MAT_Villager_Atlas.tres`,
compartilhado por todos os `.glb` como material externo): mipmap misturaria células vizinhas de longe.

**Troca de cor por dados:** um shader único (`villager_palette.gdshader`) lê a linha da UV e soma um deslocamento
de coluna vindo de *instance uniforms*: `skin`, `hair`, `cloth_a`, `cloth_b`, `leather`, `fiber` (6 inteiros por
peça; as linhas 6 e 7 não mudam). Um material só, sem duplicar recurso, sem modelo novo por cor.

## 3. Proporção e orçamento

- ~4 cabeças de altura: cabeça 0,32 m (com cabelo), tronco 0,42 m, pernas 0,45 m (até o quadril), pés 0,08 m.
  Mãos em bloco de ~0,10 m (grandes), pés de 0,16 m de comprimento.
- Pivô entre os pés, no chão (0,0,0). 1 unidade = 1 m. Olha para **−Y no Blender**, que vira **+Z na Godot**
  (convenção do projeto: `WorldView.cs`, "models face +Z").
- **Orçamento: 300–900 triângulos** por aldeão montado (corpo + cabelo + chapéu + torso + pernas), sem ferramenta e sem
  carga. Ferramenta: até 120 tris. Carga: até 150 tris. Estimativa do corpo base: ~250–350 tris.

## 4. Modularidade

Peças separadas, todas pesadas no **mesmo esqueleto** (mesmos nomes de ossos). Na Godot, cada peça é reparentada no
`Skeleton3D` do corpo base, como o `ModularCharacter.cs` já faz com os Quaternius (o skin liga por nome de osso).

| Slot | Variações iniciais | Observação |
|---|---|---|
| Corpo base | M, F | Mesma altura e esqueleto; F com ombros 10% mais estreitos e quadril um pouco mais largo. Inclui cabeça, olhos, mãos e pés (pele / sapato de couro). Braços em pele; a manga vem do torso |
| Cabelo | curto, longo, careca, trança; barba (opcional, slot próprio) | Careca = sem malha |
| Chapéu | nenhum, capuz, palha, gorro, lenço | Com chapéu, o cabelo usa a variante "sob chapéu" (sem topo) para não atravessar |
| Torso | túnica, vestido, colete sobre camisa, avental de couro | Vestido e túnica longa cobrem o quadril; o avental é casca sobre a túnica (slot `Overlay`, opcional) |
| Pernas | calça, saia longa | Saia presa ao osso `Hips`, em sino largo para o joelho não atravessar no `walk` |
| Ferramenta | machado, picareta, martelo, enxada, foice, arco | Objeto rígido no `ToolSocket`; pivô na pegada da mão |
| Carga | cesto, saco, tora, feixe de lenha | Objeto rígido no `BackSocket` (cesto, saco) ou entre as mãos (tora, feixe: socket `ToolSocket` com pose `carry_*`) |

Skinning rígido: cada vértice pesa 1,0 em um único osso. Os vãos entre peças rígidas são escondidos por sobreposição
(manga cobre o cotovelo, calça entra na bota), no estilo blocado do Kenney.

## 5. Profissões (presets em `godot/data/villager_presets.json`)

| Profissão | Corpo | Cabelo | Chapéu | Torso (A/B) | Pernas | Ferramenta | Carga |
|---|---|---|---|---|---|---|---|
| Camponês | M | curto | palha | túnica linho / couro | calça madeira | — | saco |
| Camponesa | F | trança | lenço vermelho | vestido verde musgo | saia longa | — | cesto |
| Lenhador | M | curto + barba | gorro vermelho | túnica verde musgo | calça madeira | machado | tora |
| Agricultor | M ou F | longo | palha | colete ocre sobre camisa linho | calça | enxada / foice | feixe |
| Ferreiro | M | careca + barba | — | avental de couro sobre túnica escura | calça | martelo | — |
| Caçador | M ou F | curto | capuz verde | túnica verde musgo / couro | calça | arco | — |
| Carregador | M | curto | gorro ocre | túnica linho | calça | — | cesto nas costas / saco |
| Pedreiro | M | curto | lenço linho | túnica azul tecido | calça | picareta / martelo | — |
| Reeve | M ou F | curto | — | túnica vinho limpa + faixa azul | calça | livro (mão esquerda) | — |

Todas as combinações saem só de dados (slots + deslocamentos de cor), sem modelo novo.

## 6. Esqueleto (21 ossos)

Nomes do `SkeletonProfileHumanoid` da Godot, para retarget futuro:

```
Root
└─ Hips
   ├─ Spine ─ Chest ─┬─ Neck ─ Head ─ EmoteSocket
   │                 ├─ LeftUpperArm ─ LeftLowerArm ─ LeftHand
   │                 ├─ RightUpperArm ─ RightLowerArm ─ RightHand ─ ToolSocket
   │                 └─ BackSocket
   ├─ LeftUpperLeg ─ LeftLowerLeg ─ LeftFoot
   └─ RightUpperLeg ─ RightLowerLeg ─ RightFoot
```

18 ossos humanoides + 3 sockets. `EmoteSocket` fica 0,25 m acima do topo da cabeça (≈ 1,60 m do chão, acima do
chapéu), para os balões de emoção; segue a cabeça, sem keyframes próprios. Sem `Shoulder`, `UpperChest`, dedos, olhos e mandíbula: no estilo blocado eles não
têm o que mover; o perfil da Godot aceita ossos ausentes. Bind pose em **A-pose** (braços 45° abaixo da
horizontal), como pede a Bible §45 ("A-pose limpa"); o retarget da Godot corrige a pose de repouso
("Fix Silhouette"). As animações começam em pose relaxada.

## 7. Animações (keyframes gerados por script)

Ações separadas no `.glb` do corpo base. Interpolação Bézier com poses-chave fortes e leve exagero: antecipação,
golpe rápido em 2–3 quadros, *hold* no impacto, recuperação suave. Sem root motion: o deslocamento vem do jogo.
O `walk` tem um quique de 3–4 cm por passo (cozy, saltitante). 30 fps.

| Nome | Duração | Loop | Notas |
|---|---|---|---|
| `idle` | 2,0 s | sim | respiração (Chest ±2°, escala nula), balanço lateral leve |
| `idle_look` | 3,0 s | não | cabeça olha para os lados e volta ao `idle` |
| `walk` | 0,8 s | sim | 2 passos, braços opostos, quique |
| `run` | 0,5 s | sim | tronco inclinado 10°, braços dobrados |
| `carry_idle` / `carry_walk` | 2,0 / 0,8 s | sim | braços à frente segurando a carga; mesmo quique do walk |
| `chop` | 1,2 s | sim | antecipação → golpe horizontal → impacto (hold) → recuperação |
| `mine` | 1,2 s | sim | picareta de cima para baixo |
| `hammer` | 0,8 s | sim | martelo na bigorna, golpe curto |
| `hoe` | 1,2 s | sim | enxada no chão à frente |
| `harvest` | 1,5 s | sim | agacha, colhe, levanta |
| `pickup` / `drop` | 0,8 s | não | pegar e soltar do chão |
| `shoot_bow` | 1,5 s | não | puxa, mira, solta |
| `talk` / `wave` | 2,0 s / 1,0 s | sim / não | gesto de mão; aceno |
| `sit` | 2,0 s | sim | sentado no banco (assento a 0,30 m) |

## 8. Exportação

- glTF binário (`.glb`), +Y up, 1 u = 1 m, pivô entre os pés, sem câmeras e luzes. Animações só no corpo base.
- Nomes (Bible §13): `CHR_Villager_Base_M.glb`, `CHR_Villager_Base_F.glb`, peças `CHR_Part_<Slot>_<Var>.glb`
  (ex.: `CHR_Part_Hat_Straw.glb`, `CHR_Part_Torso_Tunic.glb`), ferramentas `TOOL_<Nome>_A.glb`
  (`TOOL_Axe_A.glb`), cargas `PROP_Carry_<Nome>_A.glb`.
- Saídas: `.blend` em `art/source/characters/`, `.glb` e `T_Villager_Palette.png` em `godot/assets/characters/kenney/`
  (separado dos Quaternius em `godot/assets/characters/`, que não serão tocados).

## 9. Pipeline e reprodutibilidade

- Referência visual: imagens em `docs/reference/characters/` (prints de personagens Kenney + ficha de conceito, a
  fornecer). O protótipo é comparado lado a lado com elas.

- `tools/blender/build_villager.py` (Etapa 2) → depois `build_characters.py` (todas as peças) e
  `validate_characters.py` (tris, nomes de ossos, animações, escala, pivô, material único).
- Execução: `flatpak run --filesystem=<repo> org.blender.Blender --background --factory-startup --python
  tools/blender/<script>.py`. O script cria a cena do zero (sem estado anterior), gera a paleta, modela, arma, anima,
  salva o `.blend` e exporta. Rodar de novo gera o mesmo conteúdo.
- Renders de conferência (frente, lado, 3/4, folha de quadros-chave) pelo próprio Blender; a cena de comparação
  oficial é na Godot, com a câmera do jogo.

## 10. Gate do protótipo (Etapa 2)

No máximo **3 iterações** antes de mostrar o resultado. Checklist de aprovação:

- [x] Silhueta legível no zoom médio da câmera oficial (62 m, pitch 50°, FOV 32°): o chapéu e a postura leem; o
      verde da túnica perde contraste em cima da grama (ver relatório).
- [ ] Profissão identificável sem abrir UI: **parcial** — o `chop` em movimento lê; parado, o machado tem ~6 px no
      zoom médio. Decisão do dono no gate.
- [x] Nenhuma cor fora da paleta: toda UV no centro de uma célula usada; textura sem mipmap e sem compressão.
- [x] Passa pela porta com folga: 1,38 m com chapéu × 1,50 m; ombros 0,40 m (braços relaxados ~0,66 m) × 0,80 m.
- [x] 50 aldeões animados a 60 FPS: **268 FPS médios, 1% low 240 FPS** (RTX 4050 Laptop, 2311×1080, vsync off);
      +0,11 ms/quadro sobre a cena sem eles; draw calls 255 → 923.
- [x] Comparação lado a lado com `docs/reference/characters/` (Kenney Blocky).

Relatório do gate: `docs/reports/2026-10-09_villager_proto.md`.

## 11. Diversidade dos aldeões (Etapa 3) — rascunho para aprovação

> Complemento do dono de 09/10/2026. O protótipo da Etapa 2 não muda; tudo abaixo entra na Etapa 3.

### 11.1 Corpo: um esqueleto, variação por escala de ossos e poucas malhas

| Eixo | Faixas | Como |
|---|---|---|
| Altura | baixa 0,94 · média 1,00 · alta 1,06 | escala uniforme da raiz do personagem (proporções iguais) |
| Compleição | magra · média · robusta | **3 variantes de malha** geradas pelo mesmo script (largura de tronco, saia, pernas e mãos ×0,90 / ×1,00 / ×1,12); os ossos não mudam |
| Idade | criança · adulto · idoso | criança: raiz ×0,65 e osso `Head` ×1,25 (cabeça proporcionalmente maior), sem barba; idoso: pose-base curvada (Spine/Chest/Neck +8°/+6°/−6°, aplicada por um `SkeletonModifier3D` depois da animação), `walk` a 0,8×, cabelo grisalho/branco |
| Sexo | M · F, mesmas faixas | corpo base M/F (spec §4) |

- **Escala não-uniforme em osso fica proibida** (com rotação ela vira cisalhamento nos filhos); por isso a
  compleição é malha e a altura é escala uniforme.
- **Porta:** "alta" ficou em +6% e não +8%: 1,38 m × 1,06 = 1,46 m com chapéu, 4 cm abaixo da porta de 1,50 m.
  Com +8% sobrariam 1 cm. Regra do validador: altura máxima com chapéu ≤ 1,46 m em qualquer combinação.
- **Encaixe:** o `validate_characters.py` monta todas as combinações corpo × compleição × idade × roupa × ferramenta
  em `idle`, `walk` e na pose extrema de cada trabalho, e mede a interpenetração (vértice de corpo/pele fora da roupa
  que o cobre). Roupas são feitas para a compleição "robusta" mais 1 cm e encolhidas nas outras.

### 11.2 Cabeça e rosto (blocos simples, slot próprio para cada um)

| Slot | Variações |
|---|---|
| Cabelo | adulto: curto, raspado, careca, médio, longo solto, rabo, trança, coque · criança: tigela, dois rabinhos. Cada um com corte "sob chapéu" (só o que aparece abaixo da aba) |
| Barba | nenhuma, bigode, cavanhaque, curta, cheia |
| Sobrancelha | reta, arqueada, grossa (2 blocos, 4–8 tris) |
| Nariz | bloco, botão (pequeno), comprido |
| Olhos | normais/próximos, grandes/afastados |
| Pele | 5 tons (linha 0 do atlas) |
| Cor de cabelo | 7: castanho-escuro, castanho, preto, loiro-palha, ruivo, grisalho, branco (linha 1) |

Regras (em dados): barba só em adulto/idoso masculino; capuz não combina com coque nem rabo; chapéu troca o cabelo
pela versão "sob chapéu".

### 11.3 Roupa: qualidade, inverno e acessórios

- **3 níveis de qualidade** em todas as peças de roupa:
  - remendada: cores desbotadas e 1–2 remendos (malha com quadrados de outra cor por cima);
  - simples: linho e lã naturais;
  - tingida: cores da paleta (azul, vinho, verde…).
- **O atlas cresce para 128×128** (16×16 células de 8 px; a Bible §11 aceita atlas de NPC até 256²). Nas linhas de
  tecido: colunas 0–7 tingidas, 8–11 naturais (linho cru, lã crua, lã cinza, lã marrom), 12–15 desbotadas. A linha 8
  passa a ser de mantos (lãs de inverno). A qualidade só muda a coluna da cor, mais a malha de remendo, e nenhuma cor
  fica fora da paleta.
- **Inverno:** manto com capuz abaixado, sobre o torso (slot `Cloak`), ligado pela estação da simulação.
- **Acessórios opcionais:** cinto com bolsa, lenço no pescoço, avental (já previsto) e cajado para idosos, na mão
  esquerda via `BoneAttachment3D` no `LeftHand`.

### 11.4 Geração determinística ligada à simulação

- **Seed = SplitMix64(id do aldeão).** Nunca `GetHashCode()`, que no .NET muda a cada execução. Mesmo id, mesma
  aparência, inclusive depois de save/load. O sorteio usa um gerador próprio (PCG32) a partir da seed, separado do RNG
  da simulação.
- **Genes × aparência:** a seed sorteia os **genes** (tom de pele, cor e tipo de cabelo, nariz, olhos, sobrancelha,
  tendência a barba, altura, compleição). A aparência do momento sai dos genes mais idade, profissão, prosperidade e
  estação.
- **Herança:** cada gene do filho vem do pai ou da mãe (moeda da seed do filho), com 10% de chance de sortear um
  valor novo. A altura é a média dos pais ± 1 faixa.
- **Envelhecimento:** a etapa (criança < 14 anos ≤ adulto < 55 ≤ idoso) vem da idade da simulação. O cabelo fica
  grisalho a partir de ~45 anos e branco a partir de ~65 (± jitter da seed). A barba aparece aos ~17 para quem tem
  o gene.
- **Prosperidade:** define o nível de qualidade da roupa quando existir na simulação; até lá, "simples".
- **Profissão:** define ferramenta, chapéu de trabalho e peça de trabalho (avental do ferreiro, faixa do reeve). O
  resto vem dos genes.
- **Anti-clone:** dentro da mesma família e entre famílias de casas vizinhas, ninguém repete cabelo + roupa + cor
  principal. Na colisão, re-sorteia só a cor da roupa (nunca um gene herdado), com sal crescente, na ordem dos ids.
  O resultado é determinístico.
- **O que falta na simulação** (P46): hoje `Unit` tem só `Id`/`Name` e `Household` tem só a contagem `Members`;
  não há sexo, idade nem pais. Até isso existir, sexo e etapa de vida saem da seed e as famílias visuais saem do id
  da `Household` (pai = fundador 1, mãe = fundador 2, filhos = id da casa + índice). A API já recebe idade e
  ids dos pais, para ligar sem retrabalho.

### 11.5 Dados

`godot/data/villager_appearance.json`: listas de peças por slot, com tris, compatibilidades e pesos de sorteio por
etapa/sexo; cores por linha do atlas; faixas de altura/compleição; limiares de idade; regras (exclusões e
dependências); tabela profissão → ferramenta/roupa de trabalho. Nada disso entra no hash de determinismo da
simulação: a view só lê o id (e, no futuro, idade, sexo, pais, prosperidade e estação).

### 11.6 Orçamento e desempenho

- **300–900 tris na pior combinação** sem ferramenta, verificado pelo validador em todas as combinações. Teto por
  slot: corpo 220, rosto (olhos/nariz/sobrancelha) 40, cabelo 100, barba 40, chapéu 90, torso 180, pernas 60,
  acessório 60, manto 100. O protótipo tem 700 tris; o corpo cai de 316 para ~220 tirando o chanfro de mãos e pés.
- Um material e um atlas; cor por *instance uniforms* (shader da spec §2.1).
- Meta: 50 aldeões **diferentes** animados a 60 FPS. O protótipo fez 268 FPS com 50; mais slots aumentam as draw
  calls (~13 por aldeão hoje, contando sombra). Se passar do limite, mesclar na hora de montar as peças estáticas de
  cada aldeão num `ArrayMesh` só, com o mesmo skin.

### 11.7 Entregas da diversidade (Godot)

1. Folha de contato com 30 aldeões de seeds diferentes, lado a lado.
2. Árvore de família: pai, mãe e 3 filhos, mostrando a herança.
3. O mesmo aldeão criança, adulto e idoso.
4. O mesmo aldeão nos 3 níveis de roupa + inverno.

