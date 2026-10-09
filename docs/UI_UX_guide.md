# UI/UX Guide — Interface do Jogo (MVP)

> **Status:** 🟡 v0.1 — 08/10/2026
> **Depende de:** `GDD_sociedade_autonoma.md`, `GDD_v0.2_core_loop_5h.md` (§9 UI necessária), `asset_production_bible_mvp.md`
> **Assets de UI escolhidos:** [Kenney UI Pack: RPG Extension](https://opengameart.org/content/ui-pack-rpg-extension) (CC0) + ícones de [game-icons.net](https://game-icons.net) (CC BY 3.0)

---

## 0. Por que este documento existe

Num jogo de simulação, **a interface é metade do jogo**. A tese do projeto, *"a sociedade aprende a funcionar sem você"*, só funciona se o jogador **entender** o que a sociedade está fazendo. Uma simulação ótima com UI confusa parece aleatória; uma simulação simples com UI clara parece profunda.

**Regra de ouro da UI deste projeto:**

> Todo número na tela precisa responder a uma de três perguntas: **O que está acontecendo? Por quê? O que eu posso fazer?**

---

## 1. Princípios (o que seguir sempre)

### 1.1 Os 7 princípios do projeto

| # | Princípio | Na prática |
|---|---|---|
| 1 | **Clareza antes de beleza** | Se o ornamento atrapalha a leitura, sai o ornamento |
| 2 | **Causalidade visível** | Todo alerta mostra a causa ("Pão subiu porque faltam ferramentas") |
| 3 | **Revelação progressiva** | Mostrar só o que o jogador já precisa; painéis surgem quando a mecânica é desbloqueada (GDD v0.2 §9) |
| 4 | **O mundo antes do painel** | Se dá para mostrar no mapa (pilha vazia, forja apagada), mostre no mapa; o painel confirma |
| 5 | **Consistência absoluta** | A mesma cor, ícone e posição significam sempre a mesma coisa |
| 6 | **Feedback para toda ação** | Clique, construção, decreto aceito: tudo tem resposta visual e sonora em < 100 ms |
| 7 | **A interface encolhe com o jogo** | A delegação deve **reduzir** cliques e **aumentar** leitura (GDD v0.2 §8) |

### 1.2 Heurísticas de Nielsen aplicadas ao jogo

As 10 heurísticas de usabilidade de Jakob Nielsen são a base da avaliação de qualquer interface. Tradução para este jogo:

| Heurística | Aplicação |
|---|---|
| 1. Visibilidade do estado do sistema | Data, estação, velocidade, recursos e tendência (↑↓) sempre visíveis |
| 2. Correspondência com o mundo real | Termos medievais claros ("celeiro", "lenha"), nunca jargão técnico ("buffer", "tick") |
| 3. Controle e liberdade | Desfazer construção recém-posicionada; revogar decreto; pausar a qualquer momento |
| 4. Consistência e padrões | Botão fechar sempre no canto superior direito; ESC sempre fecha o painel do topo |
| 5. Prevenção de erros | Fantasma de construção vermelho onde não pode construir; confirmação só para ações irreversíveis |
| 6. Reconhecer em vez de lembrar | Ícones com rótulo no tooltip; custos mostrados antes de construir |
| 7. Flexibilidade e eficiência | Atalhos de teclado para quem já domina; mouse basta para quem está começando |
| 8. Estética minimalista | Cada painel com **um** objetivo; dados secundários em abas ou tooltips |
| 9. Ajudar a reconhecer e recuperar erros | "A vila está sem lenha → 2 lenhadores parados por falta de machados → [Ver ferreiro]" |
| 10. Ajuda e documentação | Tooltips ricos e uma enciclopédia simples (pós-MVP) |

### 1.3 Leis de UX úteis

| Lei | O que diz | Uso no jogo |
|---|---|---|
| **Fitts** | Alvos grandes e próximos são mais rápidos de clicar | Botões de velocidade e pausa grandes; ações frequentes perto do cursor (menu de contexto) |
| **Hick** | Mais opções = decisão mais lenta | Menu de construção em categorias (≤ 7 itens por grupo) |
| **Miller** | Memória de trabalho limitada (~7 itens) | Barra superior com no máximo ~7 recursos; o resto em painel expandido |
| **Gestalt (proximidade, similaridade)** | Elementos próximos/parecidos são lidos como grupo | Agrupar recursos por cadeia (comida junto, construção junto) |
| **Lei de Jakob** | Usuários esperam que seu jogo funcione como os outros que já jogaram | Seguir convenções de city builders (barra no topo, construção embaixo, detalhes à direita) |

### 1.4 Tipos de UI em jogos

Segundo a classificação de Fagerholt & Lorentzon (*Beyond the HUD*, 2009):

| Tipo | Definição | Exemplos no jogo |
|---|---|---|
| **Diegética** | Existe no mundo e os personagens "veem" | Pilhas de estoque, fumaça da forja, janela acesa, placa de estrada |
| **Espacial** | Está no espaço 3D, mas não no mundo da história | Contorno de seleção, fantasma de construção, raio de alcance, linhas do overlay de fluxo |
| **Meta** | Representa algo da ficção, mas fora do espaço 3D | Vinheta azul no inverno, tela escurecendo quando há fome |
| **Não diegética** | Interface pura, fora do mundo | Barra de recursos, painéis, menus |

**Diretriz:** priorizar **diegética e espacial** para a informação de "o que está acontecendo"; usar a **não diegética** para "por quê" e "o que fazer". É isso que torna o pilar *"economia física"* visível.

---

## 2. Arquitetura da informação

### 2.1 Hierarquia (do mais visível ao mais escondido)

```text
Nível 0 — Mundo 3D          → o que está acontecendo (sempre)
Nível 1 — HUD fixo          → estado geral: recursos, data, velocidade, alertas
Nível 2 — Painéis contextuais → detalhes do que foi selecionado
Nível 3 — Tooltips          → causa, fórmula, histórico
Nível 4 — Telas cheias      → reeve, sucessão, enciclopédia
```

O jogador deve conseguir jogar **80% do tempo nos níveis 0 e 1**.

### 2.2 Layout de tela (1920×1080 de referência)

```text
┌─────────────────────────────────────────────────────────────────────────┐
│ [Título/Lorde] │ 🪵 120↑ 🔥 80↓ 🌾 450↑ 🪨 30 🔨 12↓ 🪙 210 │ Prim. Ano 2 │⏸▶▶▶│
├──────────┬──────────────────────────────────────────────────┬───────────┤
│ ALERTAS  │                                                  │  PAINEL   │
│ (pilha,  │                                                  │ CONTEXTUAL│
│  máx. 4) │                  MUNDO 3D                        │ (seleção) │
│          │                                                  │           │
│ ⚠ Lenha  │                                                  │           │
│ 💡 Suges.│                                                  │           │
├──────────┴──────────────────────────────────────────────────┴───────────┤
│ [Overlays] │   [Construção: categorias + itens]   │ [CA ▓▓▓░ 6/8] [Adm] │
└─────────────────────────────────────────────────────────────────────────┘
```

| Zona | Conteúdo | Regra |
|---|---|---|
| Topo | Recursos (máx. ~7) com tendência, data/estação, velocidade | Sempre visível; nunca cobre o mundo |
| Esquerda | Próximo objetivo, sugestão do reeve e alertas | Máximo 4 empilhados; os mais antigos se agrupam |
| Direita | Painel do que está selecionado (família, edifício, estoque) | Abre ao selecionar; fecha com ESC |
| Base | Menu de construção, botões de overlay, medidor de Capacidade Administrativa | Categorias recolhidas por padrão |
| Centro | Mundo 3D | **Nunca** coberto por painel fixo; no máximo 25% de cada lado |

### 2.3 Inventário de telas do MVP (de GDD v0.2 §9)

| Elemento | Aparece em | Objetivo único |
|---|---|---|
| Barra de recursos com tendência | 0 min | "Tenho o suficiente?" |
| Relógio de estação + previsão do inverno | 0 min | "Quanto tempo até o perigo?" |
| Painel de família | 0 min | "Esta família está bem? O que faz?" |
| Painel de edifício | 0 min | "Está produzindo? Por que não?" |
| Menu de construção | 0 min | "O que posso construir e quanto custa?" |
| Cartão de sugestão de decreto | ~15 min | "Quer automatizar o que você vem repetindo?" |
| Medidor de Capacidade Administrativa | ~1h | "Quanto ainda consigo governar?" |
| Painel do reeve + livro de contas | ~1h | "O que está automatizado e o que ele decidiu?" |
| Overlay de fluxo | ~3h | "Para onde vão os recursos?" |
| Painel de debug (só dev) | — | Telemetria; nunca aparece no build de jogador |

---

## 3. Wireframes dos componentes principais

### 3.1 Recurso na barra superior

```text
┌──────────────┐
│ 🌾 450  ↑+12 │   ícone · valor · tendência por dia (verde ↑ / vermelho ↓)
└──────────────┘
Tooltip:
  Comida — 450
  Produção: +38/dia (Campo ×2, Coleta ×1)
  Consumo:  −26/dia (6 famílias)
  Dura até: ~17 dias sem colheita
  [Clique: abrir detalhes]
```

### 3.2 Painel de edifício

```text
┌─ Lenhador ──────────────────── ✕ ┐
│ ● Produzindo          ▓▓▓▓▓░ 80% │  ← status em uma linha, com cor
│                                  │
│ Trabalhadores  2/3  [+][−]       │
│ Produção      14 lenha/dia       │
│ Estoque local  22/50 🪵          │
│                                  │
│ ⚠ Eficiência reduzida:           │  ← causa sempre explícita
│   machados desgastados (−20%)    │
│   [Ver ferreiro →]               │  ← ação sugerida
└──────────────────────────────────┘
```

### 3.3 Cartão de sugestão de decreto (o momento-chave do jogo)

```text
┌─ 💡 O reeve sugere ──────────────────────┐
│ Você colocou famílias para produzir lenha │
│ 3 vezes quando o estoque estava em ~80.  │
│                                          │
│ Decreto: manter lenha entre 80 e 100     │
│                                          │
│ Custo: 1 de Capacidade Administrativa    │
│ Você deixa de: escolher quem produz lenha│
│ (o reeve não toca nas famílias que você  │
│ designou)                                │
│                                          │
│ [Criar decreto]   [Agora não]   [Nunca]  │
└──────────────────────────────────────────┘
```

Implementado no Marco 2A (cinza): só **remanejamentos** contam como repetição (tirar uma família de um emprego e pôr
noutro, ou trocar a receita de um edifício com gente) — preencher vagas pela primeira vez não conta. 3 ações em 60 dias
geram a oferta; "Agora não" adia 90 dias; "Nunca" silencia aquele recurso.

Requisitos (GDD v0.2 §3.2): sempre mostrar **o que acontece**, **o custo** e **o que você perde de controle**.

### 3.4 Alerta

```text
⚠ Lenha acaba em ~6 dias (inverno em 9)     [Ir] [✕]
```

Formato fixo: **ícone de gravidade · problema · prazo · ação**.

| Gravidade | Cor | Som | Pausa automática? |
|---|---|---|---|
| Informação | Azul | Nenhum | Não |
| Aviso | Âmbar | Leve | Não |
| Crítico | Vermelho | Marcante | Opcional (configurável) |

---

## 4. Sistema visual

### 4.1 Assets escolhidos e como usar

| Asset | Uso | Observações |
|---|---|---|
| **Kenney UI Pack: RPG Extension** (CC0) | Molduras de painel, botões, barras, sliders, checkboxes | Usar como `StyleBoxTexture` com 9-slice. O pacote inclui arquivo vetorial: exporte em 2× para telas grandes |
| **game-icons.net** (CC BY 3.0) | Ícones de recursos, profissões, edifícios, ações | Baixar em SVG, recolorir no Studio do site. **Obrigatório dar crédito ao autor de cada ícone** em `CREDITS.md` e na tela de créditos |

**Regras para os ícones:**
- Uma única família visual: mesma espessura e mesmo nível de detalhe.
- Ícones de **recurso**: preenchimento colorido (cor do recurso) sobre fundo neutro.
- Ícones de **ação**: monocromáticos (cor do texto).
- Tamanhos padronizados: **16, 24, 32 e 48 px** (lógicos). Nada fora disso.
- Todo ícone tem **tooltip com o nome**.
- Manter uma planilha `docs/icon_credits.csv` com: ID do ícone · arquivo · autor · URL.

### 4.2 Paleta de UI

Derivada da paleta do mundo (Bible §8), puxada para o cozy:

| Token | Uso | Cor sugerida |
|---|---|---|
| `bg_panel` | Fundo dos painéis | `#F2E6CF` (creme) |
| `bg_panel_alt` | Linhas alternadas, seções | `#E6D5B5` |
| `border` | Bordas e divisórias | `#8A6042` |
| `text_primary` | Texto principal | `#3B2A1E` |
| `text_secondary` | Rótulos, dados secundários | `#6E5A48` |
| `accent` | Botão principal, seleção | `#C48C39` (ocre) |
| `positive` | Tendência ↑, sucesso | `#5E8A45` |
| `warning` | Aviso | `#D9952B` |
| `negative` | Tendência ↓, crítico | `#B04A3A` |
| `info` | Informação, sugestão | `#49637A` |

**Regra:** cor **nunca** carrega significado sozinha. Sempre junto de ícone, seta ou texto (acessibilidade para daltonismo).

### 4.3 Tipografia

| Uso | Estilo | Sugestão (Google Fonts, licença OFL) |
|---|---|---|
| Títulos de painel | Serifada com personalidade medieval leve | *Alegreya SC* ou *Cinzel* (só títulos curtos) |
| Texto e números | Sem serifa, alta legibilidade | *Nunito* ou *Source Sans 3* |
| Números em tabelas | Algarismos de largura fixa (tabular) | A mesma da UI, com `tnum` se disponível |

Tamanhos (em 1080p): títulos 22 px · corpo 16 px · secundário 14 px · **mínimo absoluto 12 px**.

### 4.4 Espaçamento

Grid de **4 px**. Espaçamentos permitidos: 4 · 8 · 12 · 16 · 24 · 32. Padding interno de painel: 12–16 px. Nada "a olho".

---

## 5. Interação

### 5.1 Mouse

| Ação | Resultado |
|---|---|
| Clique esquerdo | Selecionar / confirmar |
| Clique direito | Cancelar modo atual (construção) / fechar menu de contexto |
| Arrastar com botão do meio | Pan da câmera |
| Roda | Zoom |
| Passar o mouse | Tooltip após **400 ms** (instantâneo se outro tooltip já estiver aberto) |

### 5.2 Teclado (atalhos padrão do gênero)

| Tecla | Ação |
|---|---|
| Espaço | Pausar / continuar |
| 1 · 2 · 3 · 4 | Velocidades |
| WASD / setas | Mover câmera |
| Q · E | Girar câmera 90° |
| B | Menu de construção |
| ESC | Fechar painel do topo → cancelar modo → menu do jogo |
| F1–F4 | Overlays |
| Del | Demolir seleção (com confirmação) |

Todos os atalhos **remapeáveis** (pós-MVP, mas estruturar o `InputMap` desde já).

### 5.3 Feedback e "juice"

| Evento | Visual | Som |
|---|---|---|
| Hover em botão | Leve clareamento + escala 1,03 | Clique suave |
| Clique | Afundar 1–2 px | "Toc" de madeira |
| Construção posicionada | Poeira + pop de escala | Martelada |
| Recurso entra no estoque | Número pisca na barra | Discreto, com limite de repetição |
| Decreto criado | Cartão "voa" até o medidor de CA | Som de "carimbo" |
| Erro (sem recurso) | Tremida curta + custo em vermelho | Som abafado |

**Tempo de resposta:** < 100 ms para feedback de clique; animações de UI entre 120 e 250 ms. Nada que faça o jogador **esperar**.

### 5.4 Prevenção e recuperação de erros

- Fantasma de construção: verde = pode, vermelho = não pode + motivo no tooltip.
- Custos aparecem **antes** de clicar e ficam vermelhos se faltar recurso.
- Desfazer a última construção enquanto ela ainda não começou (reembolso total).
- Confirmação **somente** para ações destrutivas (demolir, desligar decreto com efeito grande).

---

## 6. Padrões específicos de simulação/city builder

### 6.1 Causalidade explicada

Todo valor derivado tem tooltip com **a fórmula em linguagem humana**:

```text
Produtividade da fazenda: 72%
  Base                      100%
  Ferramentas desgastadas   −20%
  Inverno                   −15%
  Mestre agricultor          +7%
```

### 6.2 Tendências, não só valores

Valor isolado não ajuda a decidir. Sempre que possível: **valor + tendência + previsão** ("dura ~17 dias").

### 6.3 Livro de contas do reeve (transparência da delegação)

```text
Primavera, Ano 3
• Família Roth → Campo #8: Comida 80 abaixo do mínimo 90 (decreto "Comida")
• Sobrecarregado (5/4 de CA): deixei Lenha 120 para depois
```

Frases curtas, sempre com **o quê + por quê + qual decreto**. Sem isso, a delegação parece o jogo jogando sozinho (risco nº 2 do GDD).

### 6.4 Overlays

| Overlay | Mostra |
|---|---|
| Fluxo | Linhas de transporte (largura = volume, cor = recurso) |
| Necessidades | Casas coloridas por satisfação |
| Trabalho | Edifícios por eficiência |
| Alcance | Áreas de serviço (celeiro, mercado) |

Ao ativar um overlay, o mundo fica **dessaturado** e só o dado do overlay fica colorido.

---

## 7. Acessibilidade (mínimo do MVP)

Baseado nas *Game Accessibility Guidelines* (nível básico):

- [ ] Escala de UI ajustável (80%–150%).
- [ ] Contraste de texto ≥ **4,5:1** (critério WCAG AA) sobre o fundo do painel.
- [ ] Nenhuma informação transmitida só por cor.
- [ ] Legendas/indicação visual para todo alerta sonoro.
- [ ] Pausa disponível a qualquer momento, inclusive em menus.
- [ ] Tooltips persistentes opcionais (não somem enquanto o mouse se move para dentro deles).
- [ ] Fontes nunca abaixo de 12 px na escala 100%.

---

## 8. Implementação na Godot

### 8.1 Regras técnicas (obrigatórias)

1. **Um único `Theme`** em `godot/ui/theme/main_theme.tres` aplicado na raiz da UI. **Proibido** definir cor, fonte ou StyleBox diretamente num nó.
2. Variações via **Theme Type Variations** (ex.: `PanelPrimary`, `ButtonAccent`, `LabelSecondary`).
3. Layout só com **Containers** (`VBoxContainer`, `HBoxContainer`, `MarginContainer`, `GridContainer`) e âncoras. Nada posicionado em pixels absolutos.
4. Configuração de projeto: resolução base **1920×1080**, `stretch mode = canvas_items`, `aspect = expand`. Testar em 1280×720, 1920×1080, 2560×1440 e ultrawide.
5. A UI **só lê** snapshots da simulação e **envia comandos**. Nenhuma regra de jogo dentro da UI.
6. Atualização da UI **por evento ou a cada N frames**, nunca recalculando tudo em `_process` a cada frame.
7. Componentes reutilizáveis como cenas (`ResourceChip.tscn`, `AlertCard.tscn`, `BuildingPanel.tscn`, `PolicyCard.tscn`, `RichTooltip.tscn`).
8. Todos os textos via **chaves de tradução** (`tr("ui.resource.food")`), mesmo que o jogo comece só em português.
9. Ícones referenciados por **ID lógico** num registro (`icon_registry.tres`), nunca pelo caminho do arquivo.
10. **Termos na interface × no código** (decisão de 08/10/2026): o código usa nomes técnicos (`Policy`, `Administrator`);
    o jogador só vê o tema medieval, via chaves do `ui.csv`: política → **decreto**, administrador → **reeve** (regional:
    **bailio**; topo: **senescal**), log → **livro de contas do reeve**, CA → escrivães, pergaminhos, salão do senhor.

### 8.2 Estrutura de pastas

```text
godot/ui/
├── theme/
│   ├── main_theme.tres
│   ├── stylebox/          # StyleBoxTexture do Kenney (9-slice)
│   └── fonts/
├── icons/
│   ├── svg/               # originais do game-icons.net
│   └── icon_registry.tres
├── components/            # cenas reutilizáveis
├── screens/               # HUD, painéis, telas cheias
└── localization/
```

### 8.3 Pipeline dos assets de UI

```text
Kenney (vetor) → exportar PNG 2× → importar com filtro Linear + mipmaps desligados
  → StyleBoxTexture com margens de 9-slice → registrar no Theme

game-icons.net → recolorir no Studio → SVG → importar na Godot (escala conforme tamanho alvo)
  → registrar no icon_registry → adicionar crédito em icon_credits.csv
```

---

## 9. Metodologia de trabalho

### 9.1 Processo por tela

```text
1. Objetivo único da tela (uma frase)
2. Lista de informações (o que, por quê, o que fazer)
3. Wireframe em baixa fidelidade (papel, ASCII ou Figma)
4. Implementação com Theme padrão (cinza)
5. Teste com 1 pessoa (5 min, pensar em voz alta)
6. Ajustes
7. Aplicar arte final (Kenney + ícones)
8. Checklist da seção 10
```

**Nunca** pular do passo 1 para o 7.

### 9.2 Teste de usabilidade (barato e eficaz)

- **Pensar em voz alta:** a pessoa joga e narra o que pensa; você não ajuda, só anota.
- **Teste dos 5 segundos:** mostre um painel por 5 s e pergunte o que ele diz. Se a pessoa não souber, o painel tem informação demais.
- **Perguntas-chave após 20 min:**
  1. "Por que a vila ficou sem lenha?"
  2. "O que o reeve está fazendo agora?"
  3. "O que você faria em seguida?"
- **5 pessoas** já revelam a maioria dos problemas graves (recomendação clássica de Nielsen).

### 9.3 Métricas de UI

| Métrica | Alvo |
|---|---|
| Tempo para encontrar "por que X caiu" | < 15 s |
| Cliques por minuto | Deve **cair** ao longo da partida (GDD v0.2 §8) |
| Alertas ignorados até virar crise | Medir: alto demais = alerta mal desenhado |
| Sugestões de decreto aceitas | > 50% (senão, estão mal explicadas ou mal temporizadas) |

---

## 10. Checklist de aprovação por tela

```text
[ ] Tem um objetivo único descrito em uma frase?
[ ] Responde "o quê", "por quê" e "o que fazer"?
[ ] Usa só o Theme (sem estilo local)?
[ ] Funciona em 1280×720 e 2560×1440?
[ ] Todo ícone tem tooltip?
[ ] Nenhuma informação depende só de cor?
[ ] Contraste de texto ≥ 4,5:1?
[ ] Fecha com ESC?
[ ] Tem feedback para todas as ações?
[ ] Não cobre mais de 25% do mundo de cada lado?
[ ] Passou no teste dos 5 segundos?
[ ] Créditos dos ícones registrados?
```

---

## 11. Anti-padrões (não fazer)

- ❌ Planilhas gigantes como interface principal.
- ❌ Alertas que não dizem o que fazer.
- ❌ Números sem tendência nem contexto.
- ❌ Painéis modais que pausam o jogo sem necessidade.
- ❌ Mais de um painel grande aberto ao mesmo tempo.
- ❌ Ícones de estilos diferentes misturados.
- ❌ Texto em imagem (impossível traduzir e escalar).
- ❌ Lógica de jogo dentro de scripts de UI.
- ❌ Tutorial em texto longo; preferir ensinar pela sugestão de decreto e pelo contexto.

---

## 12. Referências

### Metodologia e princípios
- Jakob Nielsen — *10 Usability Heuristics for User Interface Design* (Nielsen Norman Group)
- Jon Yablonski — *Laws of UX* (lawsofux.com): Fitts, Hick, Miller, Jakob, Gestalt
- Celia Hodent — *The Gamer's Brain: How Neuroscience and UX Can Impact Video Game Design*
- Erik Fagerholt & Magnus Lorentzon — *Beyond the HUD: User Interfaces for Increased Player Immersion in FPS Games* (2009): classificação diegética/espacial/meta/não diegética
- *Game Accessibility Guidelines* (gameaccessibilityguidelines.com)
- WCAG 2.x — critérios de contraste

### Jogos para estudar a UI
- **Lands of Koastalia** — estética cozy, painéis creme, ícones coloridos
- **Manor Lords** — organização dos painéis de edifício e de região
- **Frostpunk** — alertas com prazo e o Livro de Leis (decisões com trade-off claro)
- **Against the Storm** — tooltips ricos e explicação de causa
- **Timberborn** — painel de distrito e prioridades
- **Factorio** — painel de estatísticas como recompensa

### Godot
- Docs: *GUI skinning and themes*, *Size and anchors*, *Using Containers*, *Multiple resolutions*, *Internationalizing games*

### Assets
- Kenney UI Pack: RPG Extension — https://opengameart.org/content/ui-pack-rpg-extension (CC0)
- game-icons.net — https://game-icons.net (CC BY 3.0, crédito obrigatório)
- Google Fonts — fontes sob licença OFL

---

## Changelog
- **v0.2 (09/10/2026)** — Marco 2A: termos decreto/reeve na interface (§8.1 regra 10), cartão de sugestão de decreto
  (§3.3) e livro de contas do reeve (§6.3) como implementados em cinza; coluna esquerda com próximo objetivo.
- **v0.1 (08/10/2026)** — Primeira versão: princípios, arquitetura da informação, wireframes, sistema visual com Kenney + game-icons, interação, acessibilidade, regras de implementação na Godot, metodologia de teste e checklists.
