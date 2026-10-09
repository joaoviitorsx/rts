# Plano v0.3 — mundo gerado + abertura RTS (aprovado em 09/10/2026)

> Design em `docs/GDD_v0.3_abertura_rts.md` (decisões D1–D12 no §15); referência da abertura em
> `docs/research/pesquisa_abertura_manor_lords.md`. Branch `feature/worldgen`. Regras de sempre:
> - sim C# determinística, testes verdes (incluindo golden hash do mapa plano, determinismo, save/load e soak de
>   50 anos) e smoke na engine;
> - commit + push e capturas por etapa; dúvidas em `docs/pending_decisions.md`;
> - nada de push na `main` sem ordem.
>
> O cenário `mvp_start` (mapa plano) continua funcionando e com o mesmo hash até a etapa (g).

## Ordem do briefing de 09/10/2026 (vale sobre as letras abaixo)

| Etapa | Entrega | Letras deste plano | Estado |
|---|---|---|---|
| 0 | GDD v0.3 + plano | (a) | ✅ aprovado |
| 1 | Gerador de mundo no sim (terraços, rampas, água, recursos, animais como dados) + testes de determinismo e alcançabilidade | b1, b2 | ✅ `eee8199`, `f633afb` |
| 2 | Unidades, seleção e comandos RTS; coleta, caça, transporte físico; boi; suprimentos que a chuva estraga | (c), (d), clima de (e), fauna de b4 no sim | ✅ `0f39bc4` |
| 3 | Fogueira, tendas, depósito; colonos → famílias; toras × lenha | (e), (f) | ✅ 3a `0f73a01` · 3b `28e492b` · 3c `37719e4` · 3d (gate na CLI, `docs/reports/opening_report.md`) |
| 4 | Visual do mundo: terreno, penhascos por contorno, shader triplanar, rochas, água, florestas, animais | b3, b4 (visual) | ✅ 4a `68b61d3` · 4b Kenney `49dd9e6` · 4c céu/chuva Sky3D, animais Quaternius, grama no relevo, penhascos diagonais `e05e414`…`496c98e` |
| 5 | Balanceamento da abertura na CLI contra o checklist da pesquisa §5 | (g) | ⏳ próxima — base pronta (`--opening-report`, jogador `rts`); alvos: 1ª delegação 15–25 min (hoje ~4), inverno com folga apertada nas 3 seeds |
| 6 | HUD v2: etapas 1–3 da spec + painel de seleção de colonos | branch `feature/hud-v2` | etapas 1–2 ✅ (Theme, estrutura) |
| 7 | Playtest da nova abertura | — | — |

## (a) Documentos — esta entrega
GDD v0.3, este plano, `decisions.md`, roadmap, lacunas do Meshy no `asset_manifest.md` e referência salva em
`docs/reference/koastalia_world.png`. **Depois da aprovação:** mover `playtest_2A.md` e `plan_2A.md` para
`docs/archive/` com registro.

## (b) Gerador de mundo — 4 sub-etapas, cada uma com commit e capturas

**b1. Dados no sim**
- `Map/WorldGen.cs`: ruído inteiro em ponto fixo, terraços, água, areia, rampas, fertilidade, floresta,
  arbustos/cogumelos, pedras soltas, afloramentos, carvão/ferro e escolha do início.
- `Map/Terrain.cs`: camadas estáticas. `Map/Nature.cs`: nós mutáveis com crescimento das árvores.
- Cenário `wild_start` com `"terrain": "generated"`; o `mvp_start` continua `"flat"`.
- Save v8: seed + versão do gerador + estado mutável; o hash inclui os nós.
- CLI `--map-ascii` (mapa em texto) e `--map-png` (imagem de camadas, para revisão rápida).
- Testes:
  - mesma seed → camadas e hash iguais;
  - seeds diferentes → mapas diferentes;
  - **mapa jogável em 200 seeds** (início válido, recursos a distância e alcançáveis, ≥ 95% da terra conectada);
  - tempo de geração < 200 ms;
  - save/load idêntico.
- Captura: imagens de camadas de 3 seeds.

**b2. Economia ligada ao mundo**
- Lenhador com árvores reais num raio, floresta recuando e recrescendo devagar; coletor.
- Pedreira só em afloramento; campo × fertilidade; construir só no mesmo nível e sem água (árvores viram trabalho).
- `Pathfinder` com água e paredões intransponíveis e rampas; em 192² células, campo de distância por destino em
  cache e A* para alvos únicos.
- Cenários da CLI `--terrain flat|generated` e seção **"mapa plano × mapa gerado"** no `balance_report`, com o
  jogador 2A (famílias) nos dois mapas.
- Testes: floresta finita e recrescimento, pedreira fora da jazida rejeitada, rendimento por fertilidade, sem
  caminho através da água, rampas usadas, ledger fechado.

**b3. Visual**
- Terrain3D a partir da altura do sim (rampas suaves); camadas grama / terra / areia / caminho / campo.
- Água: plano com shader estilizado (cor por profundidade e espuma na borda) e faixa de areia.
- Florestas MultiMesh por bloco, com cor por instância (outono 15–30%) e impostores à distância; a árvore some e
  vira toco quando é cortada. Arbustos e pedras na borda; afloramentos e jazidas visíveis.
- Paredões placeholder com rochas do Stylized Nature. Grama e flores (GrassCarpet + Stylized Nature).
- Orçamento: ≤ 8 ms/quadro no zoom da referência (RTX 4050, 1080p).
- Entrega: **3 seeds no zoom da referência** + uma imagem lado a lado com a referência.

**b4. Fauna no mundo**
- Rebanhos de cervos, coelhos e matilhas como entidades do sim: andar, fugir de colonos, reproduzir.
- Quaternius Animal Pack: o dono coloca em `art/vendor_raw/`; eu derivo glTF e cenas `CHR_Deer`/`CHR_Rabbit`/`CHR_Wolf`.
- Se o pack ainda não tiver chegado, cápsulas coloridas como placeholder.
- Caça entra na etapa (d).

## (c) Unidades, seleção e comandos RTS
- Ritmo D1 (4 ticks/s, começo no mês 0) entra aqui, junto com o cenário `wild_start`.
- Entidades `Colonist` e `Ox` no sim: posição em célula + progresso, tarefa, carga e necessidades.
- Comandos novos `OrderUnits(ids, alvo, fila)`, validados no sim.
- View:
  - seleção por clique, retângulo, Shift, duplo clique e grupos;
  - painel de seleção;
  - cursor contextual e marcador de ordem com som.
- Testes: ordens rejeitadas com motivo, determinismo com ordens aleatórias (fuzz), save no meio de uma ordem.

## (d) Coleta, caça e transporte para a pilha
- Cortar (a árvore cai como tora no chão), carregar pedaços ou **arrastar com o boi**, rachar lenha, colher, coletar
  pedra, caçar e espantar lobos; carga física até a pilha/depósito.
- Auto-continuação curta e balão "?".
- Recurso `hides`; necessidades do colono (comida, frio, sono) e ir embora por déficit.
- Animações: corte, colheita e mineração (UAL); caça = arco/lança (lacuna: ver o manifesto).

## (e) Fogueira, tendas, depósito coberto + clima
- Construções da abertura com obra e material carregado pelos próprios colonos (ordem "construir").
- Raio de calor da fogueira, que gasta lenha e afasta lobos.
- **Clima diário determinístico**, com frente de chuva garantida em 3–6 min. A pilha ao relento estraga comida e
  lenha (com tooltip e balão); o depósito coberto protege. View: chuva e luz fria.
- Cartão de objetivos reativo ao mundo ("salve os suprimentos da chuva").

## (f) Transição colonos → famílias
- Casa pronta → o par vira household (§5 do GDD), com "Quem mora aqui?" opcional.
- Famílias novas só com casa vazia (D7); cabanas de lenhador e de coletor e acampamento de caça (degrau 1).
- Sugestão do degrau 0→1 ("Mostrar onde"); cartão de objetivos novo para a abertura RTS; Salão vira construção (D9).

## (g) Balanceamento da abertura na CLI
- Jogadores roteirizados `rts_passive` / `rts_naive` / `rts_optimal`, que emitem ordens de unidade.
- Metas (checklist do GDD §14):
  - 1ª família e **1ª delegação aos 15–25 min**;
  - crise da lenha no outono (30–45 min);
  - **1º inverno aos 45–60 min**, com o ingênuo sobrevivendo e o passivo não;
  - nenhum trecho > 2 min sem decisão.
- Seções novas no `balance_report`.
- Ao fechar: o menu deixa só o `wild_start`; o `mvp_start`, o `MvpOpening` e os objetivos 2A vão para o arquivo
  morto com registro; passe de feel medido.

## Riscos
- **Desempenho do sim:** 37 mil células, milhares de árvores e dezenas de unidades. Árvores em arrays por célula,
  crescimento só no tick diário, unidades com A* em cache. Medir no soak de 50 anos (hoje ~1 s).
- **Terrain3D com paredões:** o heightmap estica nas bordas verticais. Mitigação: degrau curto no heightmap e o
  paredão como malha por cima.
- **Degrau 0 cansativo:** micro demais antes da 1ª família. Mitigação: auto-continuação curta, ordens a grupo e
  números na CLI; o playtest julga.
- **Escopo:** (b)–(g) é maior que o 2A inteiro. Se apertar, fechar **b1–b3 + c–d** jogáveis antes de (e)–(g).

## Dependências do dono
- Aprovar D1–D12 (GDD §15).
- Baixar o Quaternius Ultimate Animated Animal Pack para `art/vendor_raw/`.
- Peças do Meshy (`asset_manifest.md` §8) em `art/vendor_raw/meshy/`.
- Blender aberto com o addon MCP (`BLENDER_MCP_SAFE_MODE=1`) quando for ajustar essas peças. Sem MCP, a etapa fica
  com placeholders e segue.
