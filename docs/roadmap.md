# Roadmap de execução

> Plano operacional (o *quê* e a *ordem*). Fonte da verdade atual, junto com `docs/decisions.md`; os GDDs são
> histórico/visão (fases: GDD §9). Arquitetura: TDD v0.1.

| Etapa | Status | Entrega |
|---|---|---|
| Marco 1 — núcleo da simulação + view cinza | ✅ aprovado (08/10/2026) | sim C# headless, 50 testes, soak 50 anos, CLI, view com primitivas |
| Etapa 1 — inventário de assets | ✅ | `docs/asset_manifest.md`, `CREDITS.md` |
| Reorganização A1–A3 | ✅ | `godot/`, `art/vendor_raw/`, `scripts/setup_vendor.py`, `docs/vendor_sources.md` |
| Etapa 2 — integração de assets | ✅ concluída (08/10/2026) | assets importados, cenas herdadas, registro visual |
| Etapa 3 — TEST_VILLAGE_01 | ✅ concluída (08/10/2026) — montagem | cena de validação + screenshots em 3 zooms |
| Look-dev visual: chão, vegetação, luz e câmera | ✅ aprovado (08/10/2026) | LOOKDEV_GROUND aplicado na TEST_VILLAGE_01; ajustes finos de arte ficam para a F3 |
| **Marco 2A — loop central jogável** | 🟢 itens 1–6 feitos na branch `overnight/2026-10-09` (09/10/2026) — aguardando playtest | gate: playtest com 5 pessoas (GDD §8.3) |
| Marco 2B — diferenciais e polimento | 🟡 parcial na branch `feature/2B-polish` (log de sessão, feel, NPCs cozy, "Enquanto você estava fora") | delegados com traço, caravana e cerimônia ainda não feitos |
| **v0.3 — mundo gerado + abertura RTS** | 📝 GDD e plano aguardando aprovação (09/10/2026) — **substitui o playtest do 2A** | `docs/GDD_v0.3_abertura_rts.md`, `docs/plan_v0.3.md`; branch `feature/worldgen` |

## Etapa 2 — integração de assets (plano combinado)
1. Remover `godot/assets/vendor/.gdignore`; configurar import por pacote (escala, colisão quando fizer sentido, compressão/limite de textura).
2. Cenas herdadas em `godot/assets/<categoria>/` com nomes da Bible §13 (`BLD_*`, `ENV_*`, `PROP_*`, `CHR_*`); o código referencia só essas cenas.
3. Construções P0 montadas com o Medieval Village (pivô no centro da base, Y = solo).
4. `CHR_Villager_Base.tscn`: corpo-base (cópia sem corpo no Blender) + roupa de camponês; `AnimationLibrary` compartilhada UAL1/UAL2 com idle, walk, walk_carry, idle_carry (blend), harvest, plant_seed (+ chop; mine = chop; hammer via Mixamo no fim).
5. Registro sim id → cena (`game/visuals/visual_catalog.json`); trocar primitivas pelas cenas.
6. Terrain3D + texturas watercolor (quando estiverem em `art/vendor_raw/`).
7. Nature: normalizar escala e offset de altura nas cenas herdadas; registrar fatores no manifesto.
8. Lacunas (trigo, pilhas, tocos, poço, forja): placeholders, listadas como "falta" no manifesto.
9. TEST_VILLAGE_01 (Bible §43) + screenshots em 3 zooms + opções de material hand-painted × PBR.

## Marco 2 — dividido em 2A e 2B (decisão de 08/10/2026, ver `docs/decisions.md`)
O 2A pode começar em paralelo ao item de look-dev visual. Regra transversal: toda mecânica nova precisa ser **legível** (painel e tooltip
explicam a perda e a causa) e reforçar o loop **problema → solução → delegação**. Termos do tema medieval
(decreto, reeve, livro de contas…) no glossário do GDD §12.
**Nomes na interface × no código:** o código mantém os nomes técnicos (`Policy`, `Administrator`…); o jogador só vê
decreto/reeve/bailio/senescal, via chaves de tradução em `godot/ui/localization/ui.csv`. A troca é feita junto com as
telas do 2A, atualizando o `docs/UI_UX_guide.md` na mesma entrega.

### Marco 2A — loop central jogável para playtest (GDD v0.2)
1. **Abertura jogável sem roteiro:** suprimentos na carroça e balanceamento para que um jogador inexperiente sobreviva ao 1º inverno com folga apertada, sem morrer no ano 1. Crise da lenha no 1º outono (GDD v0.2 §4.2).
2. **Decreto (política) com faixa mínimo/máximo** (histerese explícita) em vez de limiar único — ciclos curtos, não de 3 anos.
3. **Construir consome madeira e pedra** (de verdade, com o material chegando à obra).
4. **Ferreiro** + decreto "produzir ferramentas se estoque < X".
5. **Tempo de trajeto no turno** (playtester): o trabalhador sai de casa, anda até o trabalho e volta; só produz no
   local. Tempo produtivo = turno − trajeto. Painel do edifício mostra **"% do turno em trajeto"**; tooltip explica a perda
   e sugere a solução (morar perto, estrada).
   - **Andar fora da estrada** é permitido, porém mais lento (custo de terreno no pathfinding); na estrada é mais rápido.
6. **Horta no quintal** (playtester): cada família produz verdura no tempo livre; quem mora longe do trabalho tem menos
   tempo livre (liga trajeto ↔ comida). Painel da família mostra o tempo livre e a produção da horta.
7. **Capacidade Administrativa** (escrivães, pergaminhos, salão do senhor) + **sugestão automática de decreto** (GDD v0.2 §3).

**Gate do 2A:** playtest com **5 pessoas** pelos critérios do GDD §8.3, mais o passe de feel (abaixo).

### Marco 2B — diferenciais e polimento (depois do gate do 2A)
1. **Delegados são pessoas:** o reeve é um aldeão com nome e 1 traço, implementado como modificador na Utility AI
   (delegar = escolher em quem confiar):
   - *Cauteloso*: guarda mais que o pedido e desperdiça trabalho.
   - *Ambicioso*: rende mais, ganha prestígio e faz exigências.
   - *Desleixado*: custa menos CA, mas erra.
   - *Ganancioso*: desvia uma fração para a própria família.
2. **"Enquanto você estava fora":** acelerar 1/5/10 anos e receber uma crônica com os momentos marcantes
   ("o reeve Tomas salvou a vila no inverno do ano 12").
3. **Caravana mercante** sazonal e simples: compra excedente, vende ferramentas e comida por moedas (destino para pedra e moedas).
4. **NPCs cozy:**
   - andar levemente saltitante e idles variados (espreguiçar, olhar em volta, bocejar);
   - aldeões que se cruzam acenam ou param para conversar;
   - item carregado visível (tora no ombro, cesto), e o andar muda com o peso;
   - balões de emoção com as necessidades (fome, frio, cansaço, feliz) — forma diegética do estado das famílias;
   - rotina visível: saem de manhã, voltam ao anoitecer, janela acende.
   - **Proporção dos personagens:** o cozy pede cabeça maior. Propor opções (escala de ossos × outro modelo) com
     screenshots antes de decidir.
5. **Cerimônia do decreto:** o pregoeiro lê o decreto na praça e ele é pregado no quadro de avisos.

### Game feel — critério de aprovação de todo marco
- Toda ação com resposta visual + sonora em < 100 ms e animação de 150–250 ms.
- Câmera com suavização, zoom em direção ao cursor e rotação de 90° animada.
- Posicionar construção: fantasma suave, encaixe com som, rotação por tecla. Obra com andaime, poeira e estalo final.
- Copiar construção, desfazer e atalhos para tudo que se repete.
- Nenhum período de mais de 2 min sem decisão relevante; sempre um próximo objetivo visível.
- Antes de fechar cada marco: **passe de feel** de 15 min jogando só para avaliar sensação, com notas em `docs/feel_notes.md`.

### Telas do MVP (UI_UX_guide §2.3) — processo §9.1 por tela
Cada tela passa por: 1 objetivo → 2 informações → 3 wireframe → 4 cinza (Theme padrão) → 5 teste com 1 pessoa →
6 ajustes → 7 arte final (Kenney + game-icons, **F3**) → 8 checklist §10. Nunca pular de 1 para 7.

| Tela | Objetivo único | Etapa §9.1 atual | Item |
|---|---|---|---|
| Barra de recursos com tendência | "Tenho o suficiente?" | 4 (cinza) — falta teste com pessoa | — |
| Relógio de estação + previsão do inverno | "Quanto tempo até o perigo?" | 4 (cinza, só texto) | 2A.1 |
| Painel de família | "Esta família está bem? O que faz?" | 4 (cinza) | 2A.6 (tempo livre, horta) |
| Painel de edifício | "Está produzindo? Por que não?" | 4 (cinza) | 2A.3 (material chegando), 2A.5 (% em trajeto) |
| Menu de construção | "O que posso construir e quanto custa?" | 4 parcial (barra simples; falta atalho B e categorias) | 2A.3, 2A.4 |
| Cartão de sugestão de decreto | "Quer automatizar o que você vem repetindo?" | 1–3 (wireframe §3.3 no guia) | 2A.7 |
| Medidor de Capacidade Administrativa | "Quanto ainda consigo governar?" | 1 | 2A.7 |
| Painel do reeve + livro de contas | "O que está automatizado e o que ele decidiu?" | 4 parcial (PoliciesPanel lista + log) | 2A.2, 2A.7, 2B.1 (traço) |
| Crônica "Enquanto você estava fora" | "O que aconteceu enquanto eu não olhava?" | — | 2B.2 |
| Balões de emoção (diegético) | "Quem está mal, e por quê?" | — | 2B.4 |
| Overlay de fluxo | "Para onde vão os recursos?" | — (depois do Marco 2; F1–F4) | — |
| Painel de debug (só dev) | Telemetria | pronto (F12, só build debug) | — |

### Economy Sheet via CLI
- Cenários de balanceamento na CLI: **jogador passivo**, **jogador ingênuo**, **abertura ótima**.
- Implementados **junto com o tempo de trajeto (2A.5)**, não antes.
- Cada cenário mede o **impacto do trajeto**: % do turno em trajeto por edifício, produção perdida/ano e tempo livre
  (horta) por família, com e sem estrada, morando perto × longe do trabalho.
- `docs/balance_report.md` com CSV e gráficos por cenário, verificando se as crises 1–3 aparecem nos minutos previstos no GDD v0.2 §4.

## Marco 2A — estado (09/10/2026, madrugada)
- Plano: `docs/plan_2A.md`. Relatório: `docs/reports/2026-10-09_madrugada.md`. Decisões a revisar: `docs/pending_decisions.md`.
- Itens 1–6 implementados, cada um com testes (100 no total, incluindo determinismo, save/load e soak de 50 anos),
  smoke na engine e commit. Balanceamento: `docs/balance_report.md` (CLI `--balance-report`).
- Build de playtest (Windows + Linux, release) em `out/playtest/` (fora do git) e roteiro em `docs/playtest_2A.md`.
- **Gate:** playtest com 5 pessoas (GDD §8.3) — não iniciado. Passe de feel humano (15 min) após os itens 3, 5 e 6:
  pendente do dono (notas medidas em `docs/feel_notes.md`).

## Onde paramos (08/10/2026)
- Paleta V2 "meadow" (mais saturada) escolhida e já padrão global. A aprovação do look-dev visual completo é o item
  "Look-dev visual" da tabela acima. Imagens em `docs/lookdev/`.
- TEST_VILLAGE_01 reconstruída com o pipeline do look-dev (`tools/assets/build_test_village.py` → `.tscn` + `_layout.json`,
  controlador `scenes/test/TestVillage.gd`): casas giradas ±5–15° para a estrada, telhados tile/thatch/slate, quintais com props,
  terra só em entrada/quintal, 249 árvores em florestas (MultiMesh), aglomerados de tufos/flores, 20 aldeões.
- Performance (sem vsync, RTX 4050, 1080p): perto 5,6 ms (~179 FPS, 1% low 164); distante 6,9 ms.
- **Resolvido (08/10/2026, noite):** pico de 112 ms não se repetiu (transitório); grama só no frustum, construções
  mescladas por material, floresta em blocos de 64 m, sombra só nas árvores perto da vila; 334 árvores.
  Capturas finais: `docs/lookdev/test_village_{near,mid,far,3zooms}.jpg`.
  Perf (sem vsync): 6–7 ms (~140–160 FPS) com a GPU já aquecida por muitas medições.
- **Próximas pendências:**
  1. ~~Florestas pouco densas na borda~~ — feito: impostores de árvore (8 vistas a 50°, normal em espaço-mundo, folha
     recolorida pela paleta global; `scenes/tools/ImpostorBaker.tscn`, `game/vegetation/TreeImpostors.gd`). Tiles de
     floresta a mais de 120 m da câmera trocam malha por impostor (troca seca com histerese de 5 m: o fade com dither do
     Godot deixava as duas metades vazadas). Floresta profunda fora do chão pintado + preenchimento das bordas:
     334 → 1067 árvores. Perf (sem vsync, na tomada): near 5,3 ms · mid 5,8 ms · far 4,7 ms (antes, 334 árvores:
     mid 5,4 · far 4,4). Tufos/arbustos de borda só nas árvores do chão pintado.
  2. ~~Integrar `UIpack_RPG` (Kenney) e watercolor~~ — feito (watercolor = detalhe sutil na terra/caminho).
  3. ~~HUD/debug do Marco 1 nas regras §8.1~~ — feito em cinza (`godot/ui/`); telas §2.3 no plano do Marco 2 acima.
     Pendências de UI: nomes de conteúdo, motivos de rejeição e textos do log ainda vêm do sim em português (não são
     chaves `tr()`); `icon_registry.tres` ainda não existe (chips mostram texto); atalho B do menu de construção.
- Sistema travou 2× durante a sessão (sem erro de SSD nos logs; ver chat). Evitar baterias longas de testes de GPU seguidas.
- Arquivos ainda ausentes: `docs/asset_production_bible_mvp.md`, `docs/reference/koastalia_ref.png`, KayKit Resource Bits.
