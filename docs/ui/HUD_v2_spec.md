# HUD v2 — Especificação de implementação (Godot)

> **Fonte visual:** `docs/ui/mockups/HUD_Principal_v2.dc.html` (feito no Claude Design) + PNGs exportados em `docs/ui/mockups/png/`
> **Base:** 1920×1080 · stretch `canvas_items` · aspect `expand` (igual ao projeto)
> **Regras que continuam valendo:** `UI_UX_guide.md` (Theme único, texto ≥ 12 px, escala 80–150%, UI só lê snapshots e envia comandos)
> **Status:** 🟡 v1 — 09/10/2026

---

## 0. Como usar este documento

1. O **HTML é a referência de layout, hierarquia e comportamento**. Ele não roda sozinho (depende do runtime do Claude Design), por isso existem os PNGs.
2. **Este documento traduz o HTML para a Godot**: tokens do Theme, componentes, nós sugeridos, dados da simulação e comportamento.
3. Valores em px são da base 1920×1080 e escalam com a escala de UI.
4. Onde o mockup mostra algo que **o jogo ainda não tem** (ânimo, mercador, carvão, etc.), a seção 6 diz o que fazer.

---

## 1. Linguagem de materiais (a regra visual do HUD)

| Material | Uso | Aparência |
|---|---|---|
| **Madeira** | Estrutura fixa: barra superior e barra inferior | `#6B4630` com listras verticais sutis, borda escura `#3E2A1C`, filete de latão `#C48C39` |
| **Pergaminho** | Conteúdo: painéis, cartões, tooltips | `#F6EBD6` com borda `#5A3D28` (3 px) + filete interno de latão (2 px) |
| **Latão** | Seleção e ação principal | `#C48C39` (botão primário, item ativo, medidor) |

Sombras **sólidas** de 6 px para baixo (`rgba(30,18,8,.3)`) + sombra difusa leve. Nada de gradientes chamativos.

---

## 2. Tokens do Theme (`godot/ui/theme/main_theme.tres`)

### 2.1 Cores

| Token | Hex | Uso |
|---|---|---|
| `wood` | `#6B4630` | Barras superior/inferior, cabeçalho de painel |
| `wood_dark` | `#3E2A1C` | Bordas das barras, fundos de controles sobre madeira |
| `wood_mid` | `#5A3D28` | Borda de painéis, botões escuros |
| `wood_line` | `#8A6042` | Divisores sobre madeira, bordas tracejadas |
| `parchment` | `#F6EBD6` | Fundo de painéis e cartões |
| `parchment_chip` | `#F2E6CF` | Fundo de chips de recurso e botões claros |
| `parchment_alt` | `#E6D5B5` | Abas inativas, trilhos de barra, botões secundários |
| `parchment_light` | `#FFF9EC` | Células internas, hover claro |
| `parchment_line` | `#C9AE86` | Bordas finas internas |
| `brass` | `#C48C39` | Seleção, botão primário, filete |
| `brass_dark` | `#9B6B26` | Sombra interna do botão primário |
| `brass_light` | `#F2D08A` | Destaque sobre madeira, progresso |
| `text` | `#3B2A1E` | Texto principal |
| `text_secondary` | `#6E5A48` | Rótulos |
| `text_on_wood` | `#F6EBD6` | Texto sobre madeira |
| `text_on_wood_dim` | `#E6D5B5` | Texto secundário sobre madeira |
| `positive` | `#5E8A45` | Tendência ↑, produzindo, feliz |
| `warning` | `#D9952B` | Aviso, caixa "Por que X%?" |
| `warning_dark` | `#9A6418` | Ícone de aviso |
| `negative` | `#B04A3A` | Tendência ↓, crítico, falta |
| `info` | `#49637A` | Informação, reeve |
| `winter` | `#9DB5C8` | Inverno na linha do tempo |
| `fire` | `#D9652B` | Lenha, barra de objetivo |

### 2.2 Tipografia (Google Fonts, licença OFL)

| Estilo | Fonte | Tamanho | Uso |
|---|---|---|---|
| `title_xl` | Alegreya SC Bold | 22 | Nome da vila, título do painel |
| `title` | Alegreya SC Bold | 18–20 | Títulos de cartão, estação, tooltip |
| `quote` | Alegreya Italic | 15–16 | Falas do reeve e dos aldeões |
| `value` | Nunito ExtraBold (800) | 18–20 | Números de recurso e estatísticas |
| `body_bold` | Nunito ExtraBold | 14–16 | Texto de ação, rótulos fortes |
| `body` | Nunito SemiBold/Bold | 14 | Corpo |
| `caption` | Nunito ExtraBold | 12 | Legendas, rótulos em caixa-alta (+6% de espaçamento) |

Números com **algarismos tabulares** (largura fixa). Mínimo absoluto: 12 px (a escala automática já garante).

### 2.3 Espaçamento, raios e bordas

- Grid de **4 px**: 4 · 6 · 8 · 10 · 12 · 16.
- Raios: botões/chips **6**, painéis **8**, pílulas **16–18**, círculos 50%.
- Bordas: painel **3 px** `wood_mid` + filete **2 px** `brass`; chips e botões **2 px** `wood_dark`.
- Botões claros têm "base" com `inset 0 -3px` (sombra interna inferior) → no `StyleBoxFlat` use `border_width_bottom` maior com cor `#E0CBA6` / `brass_dark`.

### 2.4 Como fazer os estilos na Godot

| Estilo do mockup | Implementação |
|---|---|
| Painel pergaminho com borda + filete interno | **NinePatch/StyleBoxTexture** (o `StyleBoxFlat` não faz borda dupla). Gerar 9-slice PNG em 2× a partir do mockup ou combinar com o Kenney RPG Extension |
| Barras de madeira com listras | `StyleBoxTexture` com textura de madeira tileável (listras a cada 64 px) |
| Chips, botões, abas | `StyleBoxFlat` (cantos, borda, `border_width_bottom` para a "base") |
| Sombra sólida de 6 px | `StyleBoxFlat.shadow_size` + `shadow_offset (0, 6)` ou embutida no 9-slice |
| Losango de aviso | `TextureRect` com ícone rotacionado ou SVG próprio |

Cada estilo vira uma **Theme Type Variation**: `PanelParchment`, `PanelParchmentHeader`, `BarWood`, `ChipResource`, `ChipResourceAlert`, `ButtonPrimary`, `ButtonSecondary`, `ButtonGhost`, `ButtonCategory`, `ButtonOverlay`, `Tab`, `LabelTitle`, `LabelQuote`, `LabelCaption`, `LabelValue`.

---

## 3. Layout (1920×1080)

```text
┌──────────────────────────── BARRA SUPERIOR (madeira, 72 px) ─────────────────────────────┐
│ [Brasão] Vila + progresso │ População + Ânimo │ Recursos (3 grupos) │ Estação+linha do tempo │ Velocidade │
└──────────────────────────────────────────────────────────────────────────────────────────┘
┌──┐┌──────────────┐                                                      ┌───────────────┐
│M ││ Objetivo     │                                                      │ PAINEL DO     │
│A ││ Reeve sugere │                MUNDO 3D                              │ EDIFÍCIO      │
│P ││ Alertas (≤3) │      (marcadores espaciais sobre edifícios)          │ (424 px, abas)│
│A ││ Na vila      │                                                      │               │
│S ││ (344 px)     │                                                      │               │
└──┘└──────────────┘          ┌── Bandeja de construção ──┐               └───────────────┘
 64px                          └───────────────────────────┘
┌──────────────────────────── BARRA INFERIOR (madeira, 96 px) ─────────────────────────────┐
│ Livro de contas · Opções │  6 categorias de construção (108×76)  │  Capacidade Adm. · Reeve │
└──────────────────────────────────────────────────────────────────────────────────────────┘
```

| Zona | Posição/tamanho |
|---|---|
| Barra superior | topo, altura 72, padding 16 |
| Trilho de mapas | x 16, y 96, largura 64 |
| Coluna esquerda | x 96, y 96, largura 344, gap 12 |
| Painel direito | direita 16, y 96, largura 424 |
| Bandeja de construção | centralizada, 112 px acima da base |
| Cartão de hover da bandeja | centralizado, 316 px acima da base, largura 520 |
| Barra inferior | base, altura 96, padding 16; laterais com 360 px cada |

Tudo com **Containers e âncoras**, nada em posição absoluta além das âncoras das zonas.

---

## 4. Componentes → cenas Godot

Cada componente é uma cena em `godot/ui/components/` e recebe dados por um método `Bind(snapshot)`.

| # | Componente (mockup) | Cena | Nós principais | Dados da simulação |
|---|---|---|---|---|
| 1 | Brasão + nome + progresso de tier | `SettlementBadge.tscn` | HBox › Panel (brasão) + VBox (Label, ProgressBar, Label) | nome da vila, tier atual, % para o próximo |
| 2 | População + ânimo | `PopulationChip.tscn` | VBox + pílula (ícone, valor, rótulo) | nº aldeões, nº famílias, **ânimo** (ver §6) |
| 3 | Chip de recurso | `ResourceChip.tscn` | Panel(ChipResource) › HBox (Icon, Label valor, Label tendência ▲▼●) | estoque, Δ/dia; variante `ChipResourceAlert` quando crítico |
| 4 | Tooltip de recurso | `ResourceTooltip.tscn` (via Nested Tooltips) | Panel(PanelParchment) › título, grade Produção/Consumo, Previsão, dica "Alt fixa" | produção e consumo por fonte, previsão em dias |
| 5 | Estação + linha do tempo | `SeasonTimeline.tscn` | VBox › (Label estação, Label dica) + barra customizada (`_draw`) + rótulos | estação, dia, ano, dias até a próxima estação, eventos futuros (colheita, mercador, inverno) |
| 6 | Velocidade | `SpeedControls.tscn` | HBox › Button pausa + botões de velocidade | estado de pausa/velocidade (**usar 1x/2x/4x/8x do jogo**, ver §6) |
| 7 | Trilho de mapas | `OverlayRail.tscn` | VBox › 5 `ButtonOverlay` (ícone + tecla) | overlay ativo |
| 8 | Próximo objetivo | `ObjectiveCard.tscn` | Panel › rótulo + prazo, texto, ProgressBar + "80/150" | objetivo atual (já existe), progresso, prazo |
| 9 | O reeve sugere | `DecreeSuggestionCard.tscn` | Panel › cabeçalho azul (retrato, título, nome) + fala + caixa do decreto + grade Custo/Você deixa de + 3 botões | sugestão (já existe no 2A), nome do reeve |
| 10 | Alerta | `AlertCard.tscn` | Panel › ícone por gravidade (losango/círculo) + VBox (problema, detalhe) + "Ir" + "✕" | alertas (já existem); "Ir" move a câmera |
| 11 | Na vila (feed) | `VillageFeed.tscn` | Panel › título + lista de `FeedEntry` (avatar, nome·papel, fala) | eventos com fala (ver §6) |
| 12 | Painel do edifício | `BuildingPanel.tscn` | Panel › cabeçalho de madeira (ícone, nome, local, fechar) + TabBar + 3 páginas | tudo do edifício selecionado |
| 12a | Aba Visão geral | `BuildingOverviewTab.tscn` | status + barra de eficiência, 3 cartões (produção, estoque, eficiência), trabalhadores (rostos, −, +), caixa "Por que X%?" com fatores + ação, seletor de prioridade de transporte | status, produção/dia, estoque local, eficiência e **fatores**, trabalhadores, prioridade |
| 12b | Aba Trabalhadores | `BuildingWorkersTab.tscn` | lista de `WorkerCard` (avatar, nome, humor, família, fala) + vagas abertas | famílias designadas, humor (ver §6) |
| 12c | Aba Histórico | `BuildingHistoryTab.tscn` | gráfico de linha 30 dias com marcador de evento + livro de contas | série de produção (telemetria já existe), entradas do livro de contas |
| 13 | Bandeja de construção | `BuildTray.tscn` | Panel › título da categoria + dica + HBox de `BuildCard` | itens da categoria, custo vs estoque |
| 13a | Cartão de construção | `BuildCard.tscn` | Button › miniatura 3D (render do modelo) + nome + custos (✕ vermelho se faltar) | custos, disponibilidade |
| 13b | Cartão de hover | `BuildHoverCard.tscn` | Panel › nome, vagas·manutenção, descrição, saída, requisito (verde/vermelho) | dados do edifício |
| 14 | Categorias | `BuildCategories.tscn` | HBox › 6 `ButtonCategory` (ícone, nome, tecla) | categoria ativa |
| 15 | Capacidade Adm. | `AdminCapacityMeter.tscn` | VBox › rótulo + "6/8" + segmentos | CA usada/total (já existe) |
| 16 | Botão do reeve | `ReeveButton.tscn` | Button › retrato + "Reeve" + "N decretos" | nº de decretos ativos; abre o painel de decretos |
| 17 | Marcador no mundo | `WorldMarker.tscn` | Control posicionado por `Camera3D.unproject_position` › pílula (ícone + texto) + haste + ponto | problemas e eventos por edifício |
| 18 | Faixa de modo | `ModeBanner.tscn` | Panel escuro com borda de latão › título + texto | "Posicionando X" / "Mapa: Y" |

**Miniaturas 3D da bandeja:** renderizar cada edifício em `SubViewport` (fundo transparente) e salvar como textura no build (mesmo pipeline dos ícones de recursos).

**Ícones:** os SVGs do mockup são formas simples originais e podem ser extraídos como `.svg` para `godot/ui/icons/svg/`. Onde ficarem fracos, substituir por game-icons.net (com crédito em `icon_credits.csv`). Tudo registrado no `icon_registry` por ID.

---

## 5. Comportamento

### 5.1 Teclado (via G.U.I.D.E, remapeável)

| Tecla | Ação |
|---|---|
| Espaço | Pausar/continuar |
| 1 · 2 · 3 · 4 | Velocidades 1x · 2x · 4x · 8x (o mockup mostra 3; o jogo tem 4) |
| F1–F5 | Mapas: Fluxo, Necessidades, Trabalho, Alcance, Fertilidade (toggle) |
| H · C · R · O · A · E | Categorias de construção |
| Esc | Cadeia: cancela posicionamento → fecha mapa → fecha bandeja → fecha painel → menu |
| Botão direito | Cancela posicionamento |
| Alt (segurar) | Fixa o tooltip aberto |

### 5.2 Interações-chave

| Situação | Comportamento |
|---|---|
| Mapa (overlay) ativo | Mundo **dessaturado** (~15% de saturação, 90% de brilho) em 220 ms; botão do mapa fica escuro com ícone dourado; faixa "Mapa: X" com legenda no topo |
| Escolher edifício na bandeja | Entra em modo "Posicionando X" e **abre sozinho o mapa relacionado** (ex.: Lenhador → Alcance; Campo → Fertilidade) |
| Item sem recurso | Borda vermelha, custo com ✕, clique não posiciona |
| Hover em recurso | Tooltip com produção/consumo por fonte e previsão; Alt fixa |
| Hover em item da bandeja | Cartão acima da bandeja com descrição, saída e requisito |
| Inverno | Vinheta azul nas bordas da tela (300 ms); linha do tempo toda azul |
| Fora do inverno | Vinheta quente sutil |
| Categoria ativa | Botão dourado, levantado 4 px (140 ms) |
| Painel do edifício | Abre ao selecionar; fecha com ✕ ou Esc |
| Alerta "Ir" | Câmera vai até a causa; "✕" dispensa |
| Sugestão do reeve | "Criar decreto" cria e o cartão some com animação até o botão do Reeve; "Agora não" adia 90 dias; "Nunca" desativa aquele tipo |

### 5.3 Movimento e som (UI_UX_guide §5.3)

- Entradas e saídas de painel: 150–250 ms, mesmo easing em todo o HUD (Tween nativo ou Anima).
- Hover em cartões da bandeja: sobe 3 px em 140 ms.
- Som em hover, clique, abrir e fechar (sons Kenney CC0), com leve variação de tom.

---

## 6. Diferenças entre o mockup e o jogo atual

| No mockup | Situação no jogo | O que fazer |
|---|---|---|
| Velocidades 1–3 | O jogo tem 1x/2x/4x/8x | Manter as 4 do jogo, mesmo visual |
| CA "6/8" | CA total = 4 no 2A | Segmentos dinâmicos pelo total real |
| **Ânimo da vila / humor dos aldeões** | Não existe | Mostrar só quando existir; esconder o componente até lá |
| **Feed "Na vila" com falas** | Não existe | Gerar falas simples a partir de eventos já existentes (falta de lenha, ferramenta gasta, família chegou) com frases em `ui.csv`. Se não couber agora, esconder |
| **Linha do tempo com eventos** (colheita, mercador, nevasca) | Só estações | Mostrar estação + inverno; colheita se houver data prevista; mercador quando a caravana existir |
| **Progresso de tier ("62% para Vila")** | Não existe | Esconder até existir |
| Mina, carvoaria, tecelão, casa de pedra, mercado | Não existem | Bandeja mostra só os edifícios do jogo; a estrutura de categorias aceita novos |
| Fertilidade (F5) | Depende do gerador de mundo | Botão desabilitado com tooltip "em breve" |
| Prioridade de transporte por edifício | Existe prioridade global, não por edifício | Se o sim não suportar, esconder; registrar em pending_decisions |
| Painel de colonos (abertura RTS) | **Não está no mockup** | Fica para o mockup da abertura RTS |

Regra: **nenhum dado falso na tela**. Se o jogo não tem o dado, o componente fica oculto, nunca com números de exemplo.

---

## 7. Plano de implementação (ordem)

1. **Theme**: tokens, fontes, Type Variations e 9-slices (pergaminho, madeira). Tela de teste com todos os estilos lado a lado.
2. **Estrutura**: barras superior e inferior, trilho de mapas, coluna esquerda e painel direito vazios, com âncoras; testar em 1280×720, 1920×1080 e 2560×1440.
3. **Componentes com dados que já existem**: recursos + tooltip, estação, velocidade, objetivo, alertas, sugestão do reeve, CA, botão do reeve, painel do edifício (visão geral e trabalhadores), histórico com telemetria.
4. **Bandeja de construção** com miniaturas 3D renderizadas e cartão de hover.
5. **Mapas (overlays)** + dessaturação + faixa de modo + abertura automática ao posicionar.
6. **Marcadores no mundo** (problemas e eventos sobre edifícios).
7. **Movimento e som.**
8. Componentes que dependem de sistemas futuros (ânimo, feed, tier, eventos): deixar as cenas prontas e ocultas.

**Critério de aceite:** screenshot do jogo ao lado do PNG do mockup, no mesmo estado, em 1920×1080, com diferenças só onde a §6 permite.

---

## 8. Addons envolvidos

| Addon | Uso no HUD |
|---|---|
| Nested Tooltips (MIT, C#) | Tooltips de recurso, fatores de eficiência ("Machados desgastados" abre outro tooltip) |
| G.U.I.D.E (MIT) | Teclas da §5.1 com rebind |
| Maaack's Game Template (MIT) | Menu de opções (botão de engrenagem da barra inferior) |
| TauPlot (BSD-3) — opcional | Gráfico da aba Histórico (ou `_draw` simples) |
| Tween nativo ou Anima (MIT) | Animações de painel e hover |
