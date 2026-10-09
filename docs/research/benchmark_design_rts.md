# Benchmark de design: Ironvale × metodologias, literatura e mercado (RTS, city-builder, colony sim)

> **Data:** 09/10/2026 · **Status:** referência viva (pesquisa; **não** decide nada). Sugestões que conflitam com
> `docs/decisions.md` estão marcadas **⚠ CONFLITO** e listadas no §6 para o dono levar ao `pending_decisions.md`.
> **Complementa (não repete):** `pesquisa_abertura_manor_lords.md` (abertura do ML), `terreno_penhascos_referencia.md`
> (penhascos por contorno), `construcoes_procedurais_referencia.md` (casas por gramática), `visual_mundo_addons.md`
> (addons de grama/água/céu).
> **Uso:** antes de cada marco/playtest, rodar os checklists do §5 junto com o passe de feel (`docs/feel_notes.md`).
> **Fontes:** URL em cada afirmação relevante. Na bibliografia (§8), **[V]** = página/PDF aberto e conferido;
> **[S]** = só resumo do buscador ou fonte secundária (conferir antes de citar fora daqui). Números marcados
> *(cálculo)* ou *(proposta)* são nossos, não da fonte.

---

## 0. Resumo executivo

**Diagnóstico em 5 linhas.**
1. A tese (delegação como conquista) é bem sustentada pela literatura: Koster ("diversão = aprender o padrão; tédio
   quando dominado"), Cook (skill atoms e *burnout*), Frostpunk (leis no lugar de pedidos dão agência) e o AoE IV
   ("tutorial analítico" que observa o jogador) apontam na mesma direção que a escada de delegação e o "o reeve sugere".
2. O maior risco **não** é o design da abertura, é a **legibilidade da automação**: as queixas recorrentes do gênero
   contra automação (Stellaris, Total War, Victoria 3, Manor Lords "workers waiting", Banished "falta feedback") são
   "não entendo por que deu esse resultado". Lee & See (2004) dão a receita: mostrar desempenho passado, processo e
   quando a automação falha.
3. Os dados atuais já mostram o segundo risco: nas sessões roteirizadas há **trechos de 28–49 min sem decisão** e os
   cliques/min caem a 0 **por ociosidade**, não por delegação (`docs/reports/playtest_2026-10-09_roteirizados.md`). A
   métrica da tese precisa separar "delegou" de "não tem o que fazer".
4. No visual, o ponto de atenção é a **hierarquia de saturação**: TF2/Valve, SC2 e AoE IV reservam a saturação e o
   contraste para o que é interativo; a referência Koastalia tem o **chão** como elemento mais saturado.
5. No gerador, o mercado (AoE2 RMS, Factorio) garante o início por **regras de colocação** (e não só por nota), sem
   penhasco perto do início e com o recurso essencial sempre à vista; a literatura de PCG pede **expressive range**
   com milhares de seeds e métricas emergentes.

### Top 10 recomendações (impacto × custo; detalhes na seção indicada)

| # | Recomendação | Área | Impacto | Custo | Conflito? |
|---|---|---|---|---|---|
| 1 | **Medir a tese direito:** separar "ordens manuais/min" de "tempo sem decisão" e "leitura de painéis"; personas novas `delegator` e `micromanager` na CLI com a asserção "delegar **não** é estritamente melhor" (eficiência delegator ≤ micromanager; ações/h delegator ≪ micromanager) | Delegação/Economia §3.5–3.6 | Alto | Baixo–médio | não |
| 2 | **Livro de contas "à Lee & See":** além do quê/por quê, mostrar o **histórico de acerto** do reeve (dias dentro da faixa), os 2–3 fatores que pesaram e **quando ele erra** (sobrecarga de CA); opção por decreto "pedir aprovação" × "agir e avisar" | Delegação §3.5 | Alto | Médio | não |
| 3 | **Reeve e carregadores robustos:** utilidade em *buckets* (sobrevivência > obras > buffers) com curva sobre "dias de consumo", reavaliação 1×/dia, **bônus de compromisso**, lote limitado a (máx − estoque − em trânsito) e **reserva** de pilha/vaga antes de sair | Delegação §3.5 | Alto | Baixo | não |
| 4 | **Início garantido por regra:** colocar água, pedras, afloramento e caça com "mais perto do início + raio máximo" (AoE2 `find_closest`), zona **sem penhasco** em volta do início (Factorio, AoE2), *assert* em vez de pulo silencioso; e os recursos dos primeiros 5 min **no primeiro quadro da câmera** (RITE do AoE II) | Mundo §3.2, Abertura §3.4 | Alto | Médio | parcial (altera o pipeline §8.3 aprovado) |
| 5 | **Hierarquia de saturação e valor:** chão com saturação média e pouco ruído; recursos, aldeões e telhados mais saturados e com mais contraste de valor; teste em escala de cinza e com simulador de daltonismo; destaque de interação por **contorno/ícone**, nunca por matiz (o outono já usa laranja/vermelho) | Arte/Legibilidade §3.7–3.8 | Alto | Baixo | **⚠ CONFLITO** com a paleta "meadow" saturada (08/10) e GDD v0.3 §12 |
| 6 | **Playtest v0.3 no método RITE** (corrigir entre participantes quando problema e solução são claros) + think-aloud; regras de ensino: **nada novo durante a chuva/lobos** (Valve: "players don't learn when stressed"); tutorial só onde o jogo é **incomum** (delegação, decretos, CA), não no RTS básico (Andersen et al. 2012) | Abertura/Feel §3.4, §4 | Alto | Baixo | não |
| 7 | **Degrau 0 sem tédio e sem "átomo morto":** botão/atalho de **colono ocioso** (AoE "."), balão "?" com causa, ordem a grupo; e um **uso residual** do micro depois da delegação (boi, emergências, lobos) para a habilidade não morrer (Cook) | Abertura/Delegação §3.4 | Alto | Baixo | não |
| 8 | **Pressão de médio prazo depois do 1º inverno:** o gênero perde jogadores quando a sobrevivência deixa de importar (Banished, Kingdoms and Castles, Against the Storm); garantir uma crise relevante a cada 20–30 min (GDD §11) com sucessores do inverno mais duros ou de outro tipo | Core loop/Economia §3.4, §3.6 | Alto | Médio | não |
| 9 | **Expressive range do gerador:** rodar **10 mil seeds** na CLI com métricas emergentes (não a nota do início) e heatmaps 2D; meta de 1–2 "protagonistas" por seed (lago grande, crista, árvore anciã) para diferenciação percebida (Compton) | Mundo §3.2 | Médio–alto | Baixo | não |
| 10 | **Cozy por contraste:** chuva lá fora × refúgio dentro (som muda sob o telhado, janela acesa, fumaça); noite "escura mas legível" (piso de exposição, lua azulada); névoa nas bordas do mapa; lobos anunciados (uivos) antes de chegar | Arte/Feel §3.7–3.8 | Médio | Baixo | não |

---

## 1. Como usar este documento

- **Ao projetar uma feature:** ler a área no §3 (o que fazemos × o que o mercado faz × gaps × recomendações).
- **Ao fechar um marco:** rodar os checklists do §5 que se aplicam (cada item é verificável em captura, CLI ou
  playtest) e anotar o resultado em `docs/feel_notes.md`.
- **Ao planejar playtest:** §4 (protocolo e métricas).
- **Conflitos:** nada aqui muda decisão registrada. O §6 lista o que precisa ir ao `pending_decisions.md`.

---

## 2. Metodologias (mapa rápido e como aplicar ao Ironvale)

| Método | O que diz (fonte) | Como aplicar no Ironvale |
|---|---|---|
| **MDA** (Hunicke, LeBlanc, Zubek 2004) | Designer vai de Mecânica → Dinâmica → Estética; jogador vai ao contrário. 8 estéticas (Sensation, Fantasy, Narrative, Challenge, Fellowship, Discovery, Expression, Submission) — [PDF](https://users.cs.northwestern.edu/~hunicke/MDA.pdf) | Declarar as estéticas-alvo: **Submission** (passatempo cozy) + **Discovery** + **Expression**, com **Challenge** leve. Cada mecânica nova justifica que dinâmica gera e que estética alimenta. O jogador chega pela estética (vê a chuva estragando antes de saber a regra): a abertura tem de ser legível pelo efeito no mundo |
| **Skill atoms / skill chains** (Cook 2007) | Átomo = Ação → Simulação → Feedback → Modelo mental; "burnout" quando o jogador domina uma habilidade sem uso útil; logar o estado de cada átomo por testador — [Game Developer](https://www.gamedeveloper.com/design/the-chemistry-of-game-design) | Modelar **"delegar"** como átomo: aceitar o cartão → o lenhador corta sozinho → pilha cresce sem cliques → "não preciso mais fazer isso". O micro de RTS deve sair de cena **com sucessora visível** (ajustar faixa de decreto) e **uso residual** (emergências) |
| **Loops e arcs** (Cook 2012) | Loops ganham valor ao repetir; arcs se gastam; "o que repete e o que não repete?" — [lostgarden](https://lostgarden.com/2012/04/30/loops-and-arcs/) | A abertura (chuva, 1º telhado, 1º inverno) é **arc**; a retenção depois da 1ª hora tem de vir de **loops** (coletar → construir → delegar → ajustar decretos → nova crise), não de eventos roteirizados |
| **Flow** (Csikszentmihalyi; Chen 2006/2007) | Desafio ≈ habilidade; senso de controle; DDA **ativa**: dar ao jogador escolhas embutidas que regulam a dificuldade — [tese](https://www.jenovachen.com/flowingames/Flow_in_games_final.pdf) | **Quanto delegar** e a **largura da faixa** dos decretos já são DDA embutida (quem delega menos joga mais "RTS"). Manter pausa e velocidades; não esconder isso num menu de dificuldade |
| **Curva de interesse** (Schell, lente #61) | Gancho forte, picos e vales, clímax — [resenha](https://www.gamedeveloper.com/design/feature-book-review-the-art-of-game-design-) | Curva alvo dos 60 min: gancho (nuvens/chuva 0–6 min) → vale (depósito coberto) → pico (1ª delegação 15–25) → tensão (lenha no outono 30–45) → clímax (inverno 45–60). Medir a real no playtest perguntando "o mais interessante e o mais chato" a cada 5 min |
| **Machinations / padrões de economia** (Adams & Dormans 2012; tese de Dormans) | Fontes, drenos, conversores, traders; padrões *Static/Dynamic Engine*, *Static/Dynamic Friction*, *Converter Engine*, *Stopping Mechanism* — [cap. 5](https://ptgmedia.pearsoncmg.com/imprint_downloads/peachpit/peachpit/samplechapters/0321820274/0321820274_gamemechanics_ch05section.pdf), [tese](https://eprints.illc.uva.nl/id/document/11998) | Comida/lenha = *static friction* (consumo per capita); inverno = *dynamic friction* sazonal; ferramentas = *dynamic engine* com desgaste (a crise do ano 2 é a "quina" da curva). Ver §3.6 |
| **Heurísticas de jogo** (Pinelle, Wong & Stach CHI 2008; HEP 2004; PLAY 2009) | 10 heurísticas a partir de 108 reviews; inclui **velocidade de jogo ajustável**, **comportamento previsível de unidades controladas pelo computador** e **representações que minimizem micromanagement** — [slides](https://cs.uwaterloo.ca/~lank/CS889/s20/slides/03.Readings.pdf), [HEP](https://hci.rwth-aachen.de/materials/conferences/CHI2004/2p1509.pdf) | Heurísticas 3 e 10 de Pinelle **são** a tese: colonos e reeve previsíveis e explicáveis; delegar = reduzir micro. Usar como revisão de especialista **entre** playtests (não substitui) |
| **Carga cognitiva / APM** (Thompson et al. 2013, PLOS ONE) | 3.360 jogadores de SC2: APM médio 117; ~5 ações por ciclo percepção-ação em todas as ligas; Bronze leva ~5 s por ciclo; latência de reação é o melhor preditor nas ligas baixas — [PMC](https://pmc.ncbi.nlm.nih.gov/articles/PMC3776738/) | O público cozy está **abaixo do Bronze**. Nenhuma decisão essencial deve exigir reação < 1–2 s nem sequências > 4–5 comandos *(proposta)*. Hotkeys e grupos são aceleradores opcionais |
| **UX cognitiva** (Hodent, *The Gamer's Brain*) | Pilares *usability* e *engage-ability*; sem capturar a atenção cedo "não haverá problema de retenção para resolver" — [resumo](https://www.designative.info/2019/03/28/the-ux-of-fortnite-celia-hodent/), [GDC 2016](https://gdcvault.com/play/1022951/The-Gamer-s-Brain-Part) | Já seguimos Nielsen adaptado (UI guide §1.2). Acrescentar: **affordance** dos recursos no mundo (árvore cortável parece cortável) e **atenção** (nada novo durante crise) |
| **Onboarding/FTUE** (Andersen et al. CHI 2012) | 45 mil jogadores: tutoriais só ajudam em jogo **complexo e incomum** (+29% tempo, +75% progresso no Foldit); instrução **just-in-time** +16%/+40%; **travar a interface** (*stenciling*) não ajudou — [PDF](https://grail.cs.washington.edu/projects/game-abtesting/chi2012/chi2012.pdf) | Não gastar tutorial com selecionar/clique direito (convenção RTS). Investir no que é nosso: delegação, decretos, CA, toras × lenha. Nunca travar a UI para forçar passo |
| **UI diegética × HUD** (Fagerholt & Lorentzon 2009; Iacovides et al. CHI PLAY 2015) | Tirar o HUD reduziu imersão/controle **só para experientes**; novatos dependem dele — [PDF](https://discovery.ucl.ac.uk/id/eprint/1470398/1/chip0179-iacovidesA.pdf) | Estratégia em **camadas**: HUD completo e simples no começo + a mesma informação no mundo (pilha molhada, fumaça, balões); **modo HUD mínimo** opcional para quem aprendeu |
| **Game feel** (Swink 2007/2008; "Juice it or lose it" 2012; Nielsen 1993) | 6 componentes (Input, Response, Context, Polish, Metaphor, Rules); limites 0,1 s / 1 s / 10 s — [Swink](https://www.gamedeveloper.com/design/game-feel-the-secret-ingredient), [Nielsen](https://www.nngroup.com/articles/response-times-3-important-limits/) | Num RTS de clique o feel mora em **Response + Polish + Metaphor** (boi pesado, colono humano). Tarefa delegada > 10 s precisa de **progresso visível no mundo**. Juice suave (tween, squash), **sem screenshake** (softness cozy) |
| **Cozy** (Project Horseshoe 2017) | Coziness = fantasia de **segurança, abundância e suavidade**; perigo **fora** do refúgio reforça o aconchego; quebram o cozy: notificações, pressão de tempo, escassez, recompensas extrínsecas — [relatório](https://www.projecthorseshoe.com/reports/featured/ph17r3.htm) | Chuva e lobos são o "frio lá fora": fazer do 1º telhado um momento audiovisual. Inverno **antecipável** (não surpresa). Cartão do reeve **não modal**, com limite de frequência e linguagem descritiva |
| **Fatores humanos da automação** (Sheridan & Verplank 1978; Parasuraman et al. 2000; Lee & See 2004) | Níveis de automação (sugere → executa se aprovado → executa salvo veto → executa e informa); "projete para confiança **apropriada**, não maior"; mostre desempenho passado, processo e propósito; às vezes automação mais simples rende mais porque é entendida — [Lee & See](https://csel.eng.ohio-state.edu/productions/intel/research/trust/Lee%20%26%20See%20Trust%20Review.pdf) | "O reeve sugere" = nível 4; decreto ativo = nível 6–7. O reeve **simples e previsível** é uma vantagem, não uma limitação. Ver §3.5 |
| **Playtest e telemetria** (RITE, Medlock et al. 2002; Valve/Ambinder 2009; Fulton 2002; Drachen et al.) | RITE no tutorial do AoE II: 16 novatos, 31 problemas achados, 30 corrigidos; caso "mandava cortar madeira sem árvore na tela" — [PDF](https://jpattonassociates.com/wp-content/uploads/2015/04/rite_method.pdf) | Ver §4 (protocolo) |
| **Geração procedural** (PCG Book; Smith & Whitehead 2010; Compton) | Confiabilidade (falha catastrófica × cosmética), controlabilidade, expressividade; *expressive range* com métricas **longe dos parâmetros**, ~10 mil amostras; "10.000 tigelas de mingau" — [cap. 1](https://www.pcgbook.com/chapter01.pdf), [cap. 12](https://www.pcgbook.com/chapter12.pdf), [Compton](https://galaxykate0.tumblr.com/post/139774965871/so-you-want-to-build-a-generator) | Ver §3.2 |
| **Legibilidade visual** (TF2 NPAR 2007 / GDC 2008; SC2; AoE IV; Dota 2 guide) | Hierarquia de leitura (cor → silhueta → detalhe); saturação e contraste só onde importa; pés escuros/torso claro; silhueta testada **na câmera do jogo** — [NPAR07](https://cdn.akamai.steamstatic.com/apps/valve/2007/NPAR07_IllustrativeRenderingInTeamFortress2.pdf), [GDC08](https://cdn.akamai.steamstatic.com/apps/valve/2008/GDC2008_StylizationWithAPurpose_TF2.pdf) | Ver §3.7 e checklist §5.8 |
| **Utility AI / job systems** (Mark & Dill; Game AI Pro; RimWorld; Banished; DF) | Curvas de resposta, buckets, histerese/compromisso; separar "decide" (WorkGiver) de "executa" (JobDriver) — [Graham](http://www.gameaipro.com/GameAIPro/GameAIPro_Chapter09_An_Introduction_to_Utility_Theory.pdf), [Dill](http://www.gameaipro.com/GameAIPro2/GameAIPro2_Chapter03_Dual-Utility_Reasoning.pdf) | Ver §3.5 |

---

## 3. Análise por área

Formato de cada área: **(1) Hoje** (com referência) · **(2) Mercado/literatura** · **(3) Gaps, riscos, oportunidades**
· **(4) Recomendações** (prioridade P0/P1/P2; impacto/custo; conflito).

### 3.1 Interface / HUD

**(1) Hoje.** UI guide v0.2: regra de ouro "o quê / por quê / o que fazer", 7 princípios, Nielsen, Fitts/Hick/Miller,
taxonomia diegética, hierarquia níveis 0–4 ("80% do tempo nos níveis 0 e 1"), alertas com prazo, tooltip causal com
fórmula, livro de contas, overlays com mundo dessaturado, acessibilidade mínima (escala 80–150% feita; contraste e
"cor nunca sozinha" pendentes) (`docs/UI_UX_guide.md` §1–§7). HUD v2 (spec em `docs/ui/HUD_v2_spec.md` na
`main`/`feature/hud-v2`): madeira/pergaminho/latão, painel de edifício com aba "Por que X%?", marcadores no mundo,
bandeja que abre o overlay relacionado ao posicionar, Alt fixa tooltip. Painel de seleção de colonos (GDD v0.3 §4.1).

**(2) Mercado/literatura.**
- **Logística visível como feature e tutorial.** Anno 1800 adicionou carroças e filas visíveis no armazém para
  "make logistics easier to grasp through visualization" ([Anno Union](https://www.anno-union.com/?p=9737)); o painel de
  estatísticas produção × consumo só chegou no patch 6.0 e jogadores reclamavam de proporções escondidas
  ([wiki](https://anno1800.fandom.com/wiki/Statistics)). Settlers II fez da rede de carregadores o espetáculo
  ([settlers2.net](https://settlers2.net/guides/roads/)).
- **Opacidade logística é a queixa nº 1 quando a automação falha:** Manor Lords "workers waiting"
  ([Steam](https://steamcommunity.com/app/1363080/discussions/)), cadeias pouco claras
  ([Vaporlens](https://vaporlens.app/app/1363080/manor_lords)); Banished: "dificuldade de dar feedback claro",
  "informação difícil de obter" ([Wikipedia](https://en.wikipedia.org/wiki/Banished_(video_game))).
- **Ociosos à vista.** AoE II DE: tecla "." seleciona o próximo aldeão ocioso; ponto de reunião num recurso; fila
  distribuída entre prédios; tamanho de fonte ajustável ([AoE](https://www.ageofempires.com/news/whats-new-age-empires-definitive-edition-2/)).
- **Previsão de ameaça como elemento dominante.** Frostpunk: termômetro e previsão são o maior elemento da tela e
  "ditam o ritmo" ([review](https://checkpointgaming.net/reviews/2018/04/frost-punk-review-not-winter-wonderland/));
  Manor Lords 0.8.110 passou a mostrar o calendário agrícola de 12 meses no campo
  ([Simulation Daily](https://simulationdaily.com/news/manor-lords-0-8-110-beta/)).
- **Ícones de edifício = marcador no mundo.** AoE IV usa telhados/estandartes únicos por edifício e logos iguais aos
  do HUD ([PCGamesN](https://www.pcgamesn.com/age-of-empires-4/graphics)).
- **Camadas de HUD:** novatos dependem do HUD, experientes ganham imersão sem ele (Iacovides 2015, §2).
- **Acessibilidade de referência:** Anno 117 (2025): 5 tamanhos de texto, modos de daltonismo **no mundo**, infotips
  fixáveis, duração de notificação ajustável, rebind com tecla secundária
  ([Ubisoft](https://news.ubisoft.com/en-us/article/2FfSSEUp1jtg9isC9NxowP/anno-117-pax-romana-accessibility-spotlight)).
  XAG 101: opção de fonte **não estilizada** quando o jogo usa fonte decorativa
  ([XAG](https://learn.microsoft.com/en-us/gaming/accessibility/guidelines)).

**(3) Gaps, riscos, oportunidades.**
- Não há **indicador de colono ocioso** no degrau 0 (só o balão "?", GDD v0.3 §4.2): com 8 colonos espalhados, o
  jogador perde quem parou.
- O "Por que X%?" existe para edifícios; falta o equivalente para **colono** ("por que ele parou / largou a carga") e
  para o **reeve** ("por que não mandou ninguém para a lenha").
- O HUD v2 usa **Alegreya SC** nos títulos: sem opção de fonte simples (XAG 101).
- Overlays de fluxo ficam para ~3h (UI guide §2.3), mas a queixa nº 1 do gênero é logística opaca: a **versão mínima**
  (setas de carregamento em curso, pilhas paradas destacadas) vale mais cedo.
- Risco: cartão do reeve + alertas + objetivo + feed "Na vila" na coluna esquerda = **notificações demais** (quebra o
  cozy, §2).

**(4) Recomendações.**
| P | Recomendação | Impacto/Custo | Conflito |
|---|---|---|---|
| P0 | Botão + atalho "próximo colono ocioso" e contador de ociosos no painel de seleção | Alto/Baixo | não (verificar tecla livre; P33) |
| P0 | Tooltip "Por que parado?" no colono e no carregador (mesmo formato de fatores do edifício) | Alto/Baixo | não |
| P1 | Overlay de fluxo **mínimo** antes do playtest v0.3: cargas em trânsito e pilhas paradas > N dias | Alto/Médio | não (antecipa item do UI guide §2.3; sugestão) |
| P1 | Orçamento de notificações: no máx. 1 cartão de sugestão por estação, 3 alertas visíveis, feed recolhível | Médio/Baixo | não |
| P1 | Marcador de telhado/estandarte por edifício igual ao ícone do `icon_registry` | Médio/Médio | não |
| P2 | Opções de acessibilidade: fonte simples, duração de alertas, modo HUD mínimo, velocidade 0,5× | Médio/Baixo | velocidade 0,5× é nova (sugestão) |

### 3.2 Mundo, terreno e geração procedural

**(1) Hoje.** GDD v0.3 §8: fBm inteiro em ponto fixo + domain warp leve, gradiente de costa, terraços quantizados em
3–4 níveis com autômato celular, rampas + flood fill (≥ 95% alcançável), campos de fertilidade/umidade/floresta,
jazidas, escolha do início por **nota** com re-roll determinístico; 192² células; 200 seeds testadas, < 200 ms.
Penhascos: marching squares (módulos Kenney × paredões gerados, P38). Mapa plano fica como linha de base
(`docs/balance_report.md`: no gerado, o ingênuo sobrevive em **1/3** seeds contra 3/3 no plano).

**(2) Mercado/literatura.**
- **Garantias do início por regra.** AoE2 DE RMS: `find_closest` + `max_distance_to_players` + zonas de evitar
  ([Forgotten Empires](https://www.forgottenempires.net/age-of-empires-ii-definitive-edition/rms-features)); objetos são
  colocados **em ordem** e o que não cabe é **pulado em silêncio** ("coloque o mais importante primeiro"); penhascos
  evitam a origem do jogador em 22 tiles ([doc RMS](https://docs.racket-lang.org/aoe2-rms/sections.html)). Factorio:
  área inicial sempre com os 4 minérios, **sempre um lago, nunca penhascos**, mesmo com água desligada
  ([wiki](https://wiki.factorio.com/Map_generator)).
- **Requisitos dirigem o gerador.** mapgen2 era "intencionalmente irrealista", guiado pelo jogo
  ([Red Blob](https://www.redblobgames.com/maps/mapgen2/)); mapgen4: "elevação que casa com o look desejado em vez de
  ajustar o look à elevação" ([mapgen4](https://www.redblobgames.com/maps/mapgen4/)).
- **Garantia primeiro, decoração depois.** Spelunky gera o caminho garantido antes dos templates
  ([Yu](https://gamedev.net/blogs/entry/2249558-the-full-spelunky-on-spelunky-xbla)); Unexplored gera um grafo
  abstrato e depois materializa, e as relações gravadas provam que o nível é completável
  ([Boris the Brave](https://www.boristhebrave.com/2021/04/10/dungeon-generation-in-unexplored/)).
- **Controle de forma.** Redistribuição por expoente antes de quantizar controla a fração de área por nível;
  árvores por Poisson disc/grade com jitter ([Red Blob](https://www.redblobgames.com/maps/terrain-from-noise/)).
  Minecraft separa *continentalness* de *erosion* e mapeia por splines
  ([wiki](https://minecraft.wiki/w/World_generation)).
- **Avaliação.** *Expressive range* com métricas longe dos parâmetros e ~10 mil amostras (Smith & Whitehead
  [PDF](https://www.pcgworkshop.com/archive/smith2010analyzing.pdf); [PCG cap. 12](https://www.pcgbook.com/chapter12.pdf));
  falha catastrófica × cosmética ([cap. 1](https://www.pcgbook.com/chapter01.pdf)); diferenciação percebida > unicidade
  matemática, "nem todo mundo pode ser protagonista" ([Compton](https://galaxykate0.tumblr.com/post/139774965871/so-you-want-to-build-a-generator)).
- **Informar o sítio.** RimWorld mostra bioma, chuva, temperatura e relevo antes de pousar
  ([wiki](https://rimworldwiki.com/wiki/World_generation)); Northgard tem queixas de início "random demais"
  ([Steam](https://steamcommunity.com/app/466560/discussions/2/3062995463270630845)).
- **WFC** serve para padrões locais (Townscaper, Bad North), com fallback em contradição
  ([Gumin](https://github.com/mxgmn/WaveFunctionCollapse)); variar por **shader** é mais barato que por malha
  (re-skin do Townscaper ≈ 500 tiles) ([Game Developer](https://www.gamedeveloper.com/game-platforms/how-townscaper-works-a-story-four-games-in-the-making)).
- **Detalhe reativo cozy.** Tiny Glade: "o jogo presta atenção em você"; muros que se rearranjam
  ([80.lv](https://80.lv/articles/exclusive-tiny-glade-developers-discuss-bevy-proceduralism-publishers-cozy-games/)).

**(3) Gaps, riscos, oportunidades.**
- A nota do início é **generate-and-test**: seeds "quase boas" viram re-roll; regras de colocação relativas ao início
  reduziriam re-rolls e dariam garantias explícitas (água sempre, sem penhasco no raio).
- 200 seeds validam *confiabilidade*, não *variedade*. Não medimos se as seeds parecem diferentes.
- O balanceamento piora no mapa gerado (ingênuo 1/3): falta medir **distância por caminho** (BFS) até os recursos
  por seed, não só euclidiana.
- O jogador não escolhe o local e não vê "por que este início é bom" (ML foi criticado por não escolher local; nós
  respondemos com garantia, mas não com **comunicação**).
- Outono "por hash" por árvore (GDD v0.3 §8.2) tende a salpicar; manchas coerentes (por campo de umidade/altitude)
  leem melhor e ainda são determinísticas.

**(4) Recomendações.**
| P | Recomendação | Impacto/Custo | Conflito |
|---|---|---|---|
| P0 | Zona sem penhasco em volta do início (raio > clareira) e água garantida; *assert* se algum recurso essencial não for colocado | Alto/Baixo | parcial: muda a versão do gerador (hash/saves); GDD §8.3 aprovado |
| P0 | Métricas por seed na CLI: distância por **caminho** do início até água, floresta, 10ª pedra, afloramento, caça (média, p5, p95) | Alto/Baixo | não |
| P1 | Colocação por regra ("mais perto do início dentro de raio máx.") para pedras, arbustos, afloramento, caça, antes do resto | Alto/Médio | parcial (pipeline §8.3) |
| P1 | Expressive range com 10 mil seeds: heatmaps de (fração de água × comprimento de penhasco/área), (compacidade da floresta × nº de clareiras), (nº de lagos × maior lago) | Médio/Baixo | não |
| P1 | Rampas/conectividade esculpidas **antes** de assentar os terraços (em vez de reparar com flood fill) | Médio/Médio | parcial (pipeline §8.3) |
| P2 | 1–2 "protagonistas" por seed (lago grande, crista, árvore anciã, ruína) | Médio/Médio | não |
| P2 | "Cartão do sítio" e prévia da seed no novo jogo | Médio/Baixo | não |
| P2 | Outono em manchas (campo de umidade/altitude) em vez de por árvore | Baixo/Baixo | leve: GDD §8.2 diz "por hash" (continua por hash, só agrupa) |
| P2 | Expoente de redistribuição calibrado por meta de área (ex.: ≥ 55% no nível 0 *(proposta)*) e alturas dos terraços casadas com a altura dos módulos de penhasco | Médio/Baixo | não |

### 3.3 Personagens, aldeões e animais

**(1) Hoje.** Colonos com nome no degrau 0; família no degrau 1+ (GDD v0.3 §6). NPCs cozy em parte (P27): balões
fome > frio > cansaço > feliz (máx. 4 na tela), carga visível (tora, cesto, saco, caixote), idles, lampião aceso;
proporção de cabeça em avaliação. Aldeões Quaternius "parcialmente" coesos com Kenney; proposta de testar KayKit /
Kenney Mini (P39, `docs/asset_manifest.md` §9.2). Animais Quaternius (cervo, lobo, boi=Cow), animados pelo estado do
sim; coelho ainda primitiva. Fauna como entidades do sim: rebanhos que fogem a ≤ 6 células, reprodução; lobos só
assustam (D8).

**(2) Mercado/literatura.**
- **Silhueta e escala distorcida.** SC2: elenco pequeno porque "unidades demais confundem"; Ultralisk encolhido; formas
  grandes para câmera 3/4; ciclos de andar variados para não andar em uníssono
  ([Kotaku](https://kotaku.com/the-sacrifices-of-starcraft-ii-made-in-the-name-of-spor-5777029),
  [GameSpot](https://www.gamespot.com/articles/blizzard-talks-starcraft-ii-art-design/1100-6171176/)). AoE IV
  **reduziu** detalhe das unidades para ler em 1080p ([MobileSyrup](https://mobilesyrup.com/2021/04/10/age-of-empires-iv-relic-developer-interview/)).
- **Hierarquia de leitura** TF2: cor (time) → silhueta (classe) → arma no peito; pés escuros, torso claro
  ([GDC 2008](https://cdn.akamai.steamstatic.com/apps/valve/2008/GDC2008_StylizationWithAPurpose_TF2.pdf)).
- **Tamanho na tela** *(cálculo)*: 130 m em 1920 px ≈ 14,8 px/m; a 45–50° de pitch um aldeão de 1,6 m fica com
  ~15–17 px de altura, um chibi de 1,2 m com ~11–13 px, um cesto de 0,5 m com ~5 px. A profissão só se lê pela
  **carga/ferramenta**, não pelo rosto.
- **Animação genérica apaga estado.** Banished usa o mesmo golpe para minerar, cortar e lavrar
  ([bit-tech](https://www.bit-tech.net/reviews/gaming/pc/banished-review/2/)).
- **Estado com alvo e tendência.** RimWorld: barra de humor com triângulo de alvo e limiares
  ([wiki](https://rimworldwiki.com/wiki/Mood)).
- **Coesão de pacotes:** KayKit usa atlas de gradiente único; Kenney/Quaternius cores chapadas por material; o risco
  real é **proporção** e **densidade de detalhe** ([KayKit](https://kaylousberg.itch.io/kaykit-adventurers),
  [Kenney Mini](https://kenney.nl/assets/mini-characters)).
- **Fauna.** Boids/steering (Reynolds 1987/1999) para o visual; em sim, rebanho como **agente-grupo** (centro + raio +
  fuga). Manor Lords: limite de caça ajustável e migração quando se constrói no habitat
  ([guia](https://beebom.com/fix-manor-lords-hunters-not-hunting/amp/)); Northgard gradua animais em passivo /
  defensivo / agressivo ([wiki](https://northgard.fandom.com/wiki/Deer)).
- **Multidões no Godot:** MultiMesh sem culling por instância → dividir por área; aldeões (dezenas–centenas) podem
  seguir com esqueleto + LOD; VAT só compensa com centenas na tela
  ([docs](https://docs.godotengine.org/en/stable/tutorials/performance/using_multimesh.html)). Cities: Skylines II
  lançou sem LOD de personagem e pagou em desempenho ([TechRadar](https://www.techradar.com/gaming/pc-gaming/no-cities-skylines-2s-performance-issues-arent-because-of-the-citizens-teeth-developer-confirms)).
- **Identidade.** Crítica a ML: "aldeões engrenagens sem rosto" (pesquisa da abertura §3). Sylvester: escolher a
  **representação mínima** que sustenta as histórias desejadas (apofenia)
  ([Stanford GDT](https://gdt.stanford.edu/how-do-you-create-apophenia/)).

**(3) Gaps, riscos, oportunidades.**
- Mistura Quaternius (realista) × Kenney (brinquedo) é o maior risco de coesão; decidir **uma** família humanoide.
- A ~15 px, carga e ferramenta em escala real somem; o balão é o único sinal legível hoje.
- Balão de emoção sem tendência ("piorando") não antecipa a crise.
- Coelho primitivo destoa; boi = Cow pode ser lido como vaca (ok no cozy, mas conferir com jogadores).

**(4) Recomendações.**
| P | Recomendação | Impacto/Custo | Conflito |
|---|---|---|---|
| P0 | Decidir a família humanoide (P39) com o teste de legibilidade do §5.3 **na câmera do jogo** (não no viewport) | Alto/Médio | não (decisão aberta do dono) |
| P0 | Props de trabalho e carga exagerados 1,3–1,5× *(proposta)*; animação distinta ao menos por prop (machado, picareta, cesto, tora no ombro, arrastar do boi) | Alto/Baixo | não |
| P1 | Fase de idle/andar por id (determinística) e velocidade ±5% para quebrar uníssono | Médio/Baixo | não |
| P1 | Balão com seta de tendência ou cor + ícone; nunca só cor | Médio/Baixo | não |
| P1 | Rebanho como agente-grupo no sim; boids só na view | Médio/Médio | não (alinhado à regra sim × view) |
| P2 | "Limite de caça" como decreto natural de histerese (manter cervos ≥ N) no acampamento de caça | Médio/Baixo | não |
| P2 | Escala de ameaça animal passivo / defensivo / assusta (lobo), sempre com aviso prévio (uivo) | Médio/Baixo | não (coerente com D8) |

### 3.4 Core loop e abertura

**(1) Hoje.** GDD v0.3 §1–§3: escada de delegação; 8 colonos + boi; frente de chuva garantida em 3–6 min com aviso;
limite de verbos até o min 5 (selecionar, coletar, construir, acelerar); auto-continuação curta (D4); 1ª delegação
15–25 min; 1º inverno 45–60 min; família só com casa vazia (D7); regras de ouro (objetivo nasce do mundo, sempre um
próximo passo visível). Critério de "nenhum período > 2 min sem decisão" (decisões 08/10). CLI: roteirizados
mostram trechos de 28–49 min sem decisão e 1ª sugestão aos ~12 min no 2A
(`docs/reports/playtest_2026-10-09_roteirizados.md`).

**(2) Mercado/literatura.**
- **Abertura RTS clássica = build order contínua:** AoE produz aldeões sem parar e o tutorial ensina coleta,
  produção, construção e avanço de era; desafios Art of War com medalhas para repetir
  ([quickstart AoE IV](https://www.ageofempires.com/news/quickstart-guide-age-of-empires-iv/)). O RITE do AoE II achou
  o problema "mandar cortar madeira sem árvore na tela" ([RITE](https://jpattonassociates.com/wp-content/uploads/2015/04/rite_method.pdf)).
- **Tutorial analítico.** AoE IV planejou observar o jogador e apontar mecânicas ignoradas
  ([KitGuru](https://www.kitguru.net/gaming/matthew-wilson/age-of-empires-iv-will-use-analytic-based-tutorials-and-art-of-war-missions-to-teach-players/));
  é a mesma ideia do "o reeve sugere".
- **Mentora opcional e contextual.** Against the Storm refez o tutorial (uma ordem por vez, "Read More" no HUD) e criou
  uma mentora "nunca forçada" que reage aos fracassos anteriores
  ([Eremite](https://eremitegames.com/guidance-and-lore-update-part-1/)).
- **Pressão externa e julgamento.** Against the Storm: "precisa haver alguém que venha julgar você"; "as estações e
  eventos cutucam" ([Game Developer](https://www.gamedeveloper.com/road-to-igf-2023/how-against-the-storm-managed-to-mix-city-building-and-roguelite-play)).
  RimWorld (Cassandra): começa com ameaça leve, ciclos "on/off" de dias, intervalo mínimo entre ameaças
  ([wiki](https://rimworldwiki.com/wiki/Cassandra)).
- **O fim de jogo morre quando sobreviver deixa de importar:** Banished (PC Gamer, Escapist), Kingdoms and Castles
  ("começa a parecer trivial", [Worth Playing](https://worthplaying.com/article/2018/8/16/reviews/110447-pc-review-kingdoms-and-castles/)),
  Against the Storm (endgame "desconectado", [Radiator](https://www.blog.radiator.debacle.us/2023/06/design-review-of-against-storm-by.html)).
- **Ensino fora do estresse.** Valve: "players don't learn when stressed", "players don't look up"
  ([GMTK](https://gmtk.substack.com/p/valves-secret-weapon)). Andersen 2012: ensinar o incomum, just-in-time.
- **Koster:** o tédio chega quando o padrão foi dominado — o momento certo de oferecer o próximo degrau
  ([resenha](https://www.tale-of-tales.com/DramaPrincess/wp/?p=120)). Nossa regra "o degrau seguinte só depois de
  repetir" é exatamente isso.

**(3) Gaps, riscos, oportunidades.**
- **Risco 1 — chuva ensina sob estresse.** A frente de chuva em 3–6 min é o gancho; não pode coincidir com ensino de
  verbo novo. O aviso "chuva chegando" dá a janela de ensino (antes), e o vale depois (depósito pronto) é a hora de
  ensinar o próximo.
- **Risco 2 — átomo morto.** Quando todos os colonos viram famílias, o controle direto some; Cook pede uso residual.
- **Risco 3 — vazio pós-inverno.** Dados atuais (roteiros do 2A) já mostram 28–49 min sem decisão depois do 1º ano.
- **Oportunidade:** ritmo de crises estilo Cassandra (on/off, intervalo mínimo) como regra explícita de diretor de
  eventos, respeitando o cozy (antecipável).

**(4) Recomendações.**
| P | Recomendação | Impacto/Custo | Conflito |
|---|---|---|---|
| P0 | Regra "nada novo durante emergência": durante chuva/lobos só reforçar verbos já usados; o cartão de objetivo seguinte aparece no vale | Alto/Baixo | não |
| P0 | Checar o 1º quadro da câmera em 20 seeds: árvore, arbusto, pedra, água e o local do depósito visíveis sem rolar | Alto/Baixo | não |
| P0 | Usos residuais do controle direto após a delegação: o boi continua selecionável; "convocar" colonos/famílias numa emergência (lobo, fogo) com custo | Alto/Médio | **⚠ verificar** GDD v0.3 §4.1 (aldeão de família não recebe ordem direta) — sugestão de exceção |
| P1 | Diretor de crises: alvo de 1 crise a cada 20–30 min (GDD §11) com intervalo mínimo, sempre anunciada com antecedência; sucessor do inverno 1 mais exigente (inverno 2 longo, praga, frio) | Alto/Médio | não |
| P1 | Desafios curtos opcionais estilo "Art of War" (ex.: "salve os suprimentos antes da chuva", "1º inverno sem perdas") com medalhas, reaproveitando os cenários da CLI | Médio/Médio | não |
| P2 | O reeve como mentor contextual opcional (dicas que reagem a falhas anteriores), sem modal | Médio/Médio | não |

### 3.5 Delegação e IA (reeve, carregadores, famílias)

**(1) Hoje.** Escada (GDD v0.3 §1); decretos com faixa mín/máx; CA = 4 com sobrecarga → atraso e erro
determinístico (P19); sugestão após 3 remanejamentos em 60 dias (P20) com faixa sugerida + cobertura de inverno (P21);
livro de contas com quê/por quê/decreto (UI guide §6.3); Utility AI planejada com traço do reeve (GDD §5.6, roadmap
2B.1); carregadores com urgência por dias de consumo < 20 (P15) → obra → buffer mais cheio (P2); "só carregadores
levam material" (P1). Regra "delegar nunca é estritamente melhor" (GDD §4.3).

**(2) Mercado/literatura.**
- **Leis > pedidos.** Frostpunk trocou pedidos por leis "para dar mais agência"
  ([Wikipedia](https://en.wikipedia.org/wiki/Frostpunk)). Stronghold governa por alavancas (popularidade > 50 atrai
  gente) ([manual](https://cdn.steamstatic.com/steam/apps/1295970/manuals/Stronghold_Warlords_Demo_Guide_-_Autumn_2020.pdf)).
- **DF Manager = o primeiro "reeve":** ordens com condição ("quantidade de X ≤ N"), validação no escritório do gerente
  (atraso quando ocupado = nossa sobrecarga de CA); armadilha: **lote** estoura o teto (ordem "< 100" com lote 10
  chegou a 140) ([wiki](https://dwarffortresswiki.org/index.php/Work_order)).
- **Automação que falha sem explicar** é a queixa: Victoria 3 (investimento autônomo sem poder repriorizar; comunidade
  "dividida", virou Game Rule) ([DD71](https://paradoxinteractive.com/games/victoria-3/news/dev-diary-71-autonomous-investment-in-1-2)),
  Stellaris ("drena a economia"), Three Kingdoms ("constrói tudo sem critério").
- **Delegação com risco** existe e funciona: Songs of Syx tem nobres que administram e podem conspirar
  ([Steam](https://store.steampowered.com/app/1162750/Song_of_Syx/)) — precedente para o traço do reeve.
- **Utility AI:** curvas sobre valores normalizados; oscilação quando duas ações empatam → bônus para a ação em curso,
  cooldown que decai, decidir só quando a tarefa termina ([Graham, §9.7](http://www.gameaipro.com/GameAIPro/GameAIPro_Chapter09_An_Introduction_to_Utility_Theory.pdf));
  *dual-utility* com categorias de prioridade ([Dill](http://www.gameaipro.com/GameAIPro2/GameAIPro2_Chapter03_Dual-Utility_Reasoning.pdf));
  The Sims: "um Sim faminto nem considera ver TV" (buckets). Personalidade como **multiplicador de curva** (Sims 3)
  ([Wikipedia](https://en.wikipedia.org/wiki/Utility_system)). IAUS: considerations dirigidas a dados
  ([gameai.com](https://gameai.com/iaus.php)).
- **GOAP/HTN:** GOAP (F.E.A.R.) brilha em comportamento inesperado — ruim para um city-builder previsível; HTN só se
  houver projetos de várias etapas (degrau 3+) ([Orkin](https://pages.cs.wisc.edu/~dyer/cs540/handouts/gdc2006_orkin_jeff_fear.pdf),
  [HTN](http://www.gameaipro.com/GameAIPro/GameAIPro_Chapter12_Exploring_HTN_Planners_through_Example.pdf)).
- **Smart objects (The Sims):** interações *manual / pushed / advertised / functional* ≈ a nossa escada (degrau 0 /
  ordem urgente / construções que anunciam trabalho / decretos) ([Forbus](https://users.cs.northwestern.edu/~forbus/c95-gd/lectures/The_Sims_Under_the_Hood_files/outline.htm)).
- **Job systems:** RimWorld separa *WorkGiver* (decide) e *JobDriver* (executa); prioridade estrita gera o "carrega
  tudo do mapa" ([wiki](https://rimworldwiki.com/wiki/Work)); ONI: "subir tudo não funciona", reservar 8–9 para
  emergência ([wiki](https://oxygennotincluded.wiki.gg/wiki/Priority)); Songs of Syx **reserva** a pilha antes de buscar
  ([Steam](https://steamcommunity.com/app/1162750/discussions/0/4335356354422856538)); Against the Storm pôs a Hauler
  Station como **bônus**, os trabalhadores continuam buscando os próprios insumos ([Eremite](https://eremitegames.com/?p=238209)).
- **Fatores humanos:** Lee & See: "confiança apropriada, não maior"; mostrar desempenho passado, processo (resultados
  intermediários), propósito e em que situação falha; "confiável × compreensível" ([PDF](https://csel.eng.ohio-state.edu/productions/intel/research/trust/Lee%20%26%20See%20Trust%20Review.pdf)).
  Níveis de automação de Sheridan ([NASA](https://ntrs.nasa.gov/citations/19790007441)).
- **Simulação legível:** "tudo no modelo do jogo que não copia para o modelo do jogador não vale nada" (Sylvester,
  [The Simulation Dream](https://gamedeveloper.com/design/the-simulation-dream)); "opere no nível do que o jogador vê ou
  uma camada abaixo" (Tarn Adams, [Game AI Pro 2](http://www.gameaipro.com/GameAIPro2/GameAIPro2_Chapter41_Simulation_Principles_from_Dwarf_Fortress.pdf)).

**(3) Gaps, riscos, oportunidades.**
- **Oscilação** de decretos concorrentes (dois recursos com urgência parecida) ainda não tem bônus de compromisso
  explícito (a histerese cobre um decreto, não a disputa entre decretos).
- **Estouro do teto** se a ordem do reeve não descontar o que está em trânsito (armadilha do DF).
- **Starvation** de tarefas de baixa prioridade (prioridade estrita do P2) → precisa de *aging*.
- **P1 × mercado:** obra parada sem carregador é o oposto da lição do Against the Storm (carregador = acelerador, não
  requisito). Na v0.3 os colonos carregam sozinhos no degrau 0, mas o problema volta com famílias.
- **Livro de contas** diz o quê/por quê, mas não mostra **desempenho passado** nem **concorrentes** (Lee & See).
- **Oportunidade:** o traço do reeve como multiplicador de curva (ex.: cauteloso desloca a urgência de 20 para 30
  dias) é barato e legível.

**(4) Recomendações.**
| P | Recomendação | Impacto/Custo | Conflito |
|---|---|---|---|
| P0 | Reeve em buckets (sobrevivência < 20 dias > obras > buffers > ocioso), curva sobre dias de consumo, reavaliação 1×/dia, desempate por id, **bônus de compromisso** que decai | Alto/Baixo | não |
| P0 | Ordens limitadas a (máx − estoque − em trânsito); reserva de pilha e de vaga de destino pelo carregador | Alto/Baixo | não |
| P0 | Livro de contas com: histórico de acerto (dias dentro da faixa na estação), 2–3 fatores nomeados com ícone, "sob sobrecarga eu erro 1 em N" | Alto/Médio | não |
| P1 | *Aging*: prioridade efetiva sobe com o tempo de espera (evita starvation) | Médio/Baixo | não |
| P1 | Opção por decreto: "pedir aprovação" (nível 5 de Sheridan) × "agir e avisar" (nível 7) | Médio/Médio | não |
| P1 | Rever P1: construtores buscam o material quando não há carregador, com penalidade (carregador vira acelerador) | Alto/Baixo | **⚠ CONFLITO** com P1 aprovado (09/10) |
| P1 | Um único botão de **urgência** por construção, com custo em CA (evita inflação de prioridade, lição ONI) | Médio/Baixo | não (mecânica nova) |
| P2 | Construções "anunciam" trabalho e necessidades (smart objects) só dentro de região/raio | Médio/Médio | não |
| P2 | Traço do reeve = deslocamento/escala das curvas, nunca um sistema paralelo | Médio/Baixo | não |

### 3.6 Economia e balanceamento

**(1) Hoje.** Tabelas em `balance.json`; jogadores roteirizados passive/naive/optimal/optimal_no_roads; 3 seeds × 3
anos; crises 1–3 com horário; soak de 50 anos; mapa plano × gerado (`docs/balance_report.md`). Plano v0.3 (g):
`rts_passive/naive/optimal` para a abertura. Determinismo com golden hash, save/load e fuzz.

**(2) Mercado/literatura.**
- **Padrões de Dormans** (§2): ferramenta = *dynamic engine* com desgaste; inverno = *dynamic friction*. A assinatura do
  dynamic engine é investir cedo e colher depois (curva com "quina").
- **Proporções explícitas:** Anno deriva proporções de tempos de ciclo (ex.: 1:1, 2:1, 3:2)
  ([Prima](https://primagames.com/featured/anno-1800-guide-tips-money)), e jogadores precisaram de sites externos
  enquanto o jogo não mostrava.
- **Espirais de falha:** a arte é equilibrar espirais dramáticas com oportunidades de recuperação; RimWorld usa
  expectativas que baixam após desastre e um evento de socorro único em situação desesperada
  ([Game Developer](https://gamedeveloper.com/design/the-art-of-the-spiral-failure-cascades-in-simulation-games)).
  Banished: lenhadores morrem de fome → falta tora → os demais congelam (mesma fonte).
- **Personas sintéticas:** personas que diferem só nos pesos de utilidade capturam estilos humanos tão bem quanto
  clones treinados (Holmgård et al., [AIIDE 2015](https://ojs.aaai.org/index.php/AIIDE/article/view/12849)); variar a
  habilidade dos agentes dá curva de dificuldade × competência (Zook et al., [FDG 2015](https://faculty.cc.gatech.edu/~riedl/pubs/zook-fdg15.pdf));
  King usa um agente "humano" para prever dificuldade em vez de semanas de teste manual
  ([InfoQ](https://www.infoq.com/articles/candy-crush-qa-ai-saga/)).
- **Determinismo:** Factorio compara CRC do mapa **a cada tick** no replay e acha a 1ª divergência
  ([FFF #47](https://factorio.com/blog/post/fff-47)); AoE: "sempre havia mais uma coisa que passava" e o número de
  chamadas ao random precisa ser idêntico ([Bettner & Terrano](https://web.cs.wpi.edu/%7Eclaypool/courses/4513-B03/papers/games/aoe.pdf)).
- **Pathfinding de massa:** Banished trocou A* por candidato por grafo de **regiões** (60–100× mais rápido)
  ([Shining Rock](https://shiningrocksoftware.com/?p=1971)); flow fields por destino (Supreme Commander 2,
  [Game AI Pro](http://www.gameaipro.com/GameAIPro/GameAIPro_Chapter23_Crowd_Pathfinding_and_Steering_Using_Flow_Field_Tiles.pdf)).
- **Schreiber:** sinks/faucets, economias abertas × fechadas ([Game Balance Concepts](https://gamebalanceconcepts.wordpress.com/2010/09/08/level-10-final-boss/)).

**(3) Gaps, riscos, oportunidades.**
- 3 seeds e médias escondem cauda: o gerado mostra o ingênuo morrendo em 2/3 seeds — precisa de **percentis** sobre
  50–200 seeds.
- A métrica "cliques/min cai" confunde delegação com ociosidade (§0).
- Não há persona que **delega cedo** nem uma que **nunca delega**: a regra "delegar nunca é estritamente melhor" (GDD
  §4.3) não é testada.
- Sem "dias até acabar" por recurso no HUD, a espiral fica ilegível (o UI guide prevê "dura ~17 dias" no tooltip; vale
  ter na barra para comida e lenha).

**(4) Recomendações.**
| P | Recomendação | Impacto/Custo | Conflito |
|---|---|---|---|
| P0 | Personas `delegator` e `micromanager` + asserção do custo de delegação; variar o "atraso de reação" do optimal para a curva habilidade × resultado | Alto/Médio | não |
| P0 | Relatório em **percentis** (p10/p50/p90) sobre 50–200 seeds por persona; dispersão do dia da 1ª crise | Alto/Baixo | não |
| P0 | Hash por dia + replay duas vezes + save→load→continua comparando o hash diário; RNG por subsistema (clima, fauna, reeve, admin) | Muito alto/Baixo | não (já há RNG "admin" próprio; estender) |
| P1 | Métricas novas: dias mínimos de estoque por estação, itens parados > N dias, trocas de decreto/ano (oscilação), uso de CA e erros por sobrecarga, ações do jogador/h por degrau | Alto/Baixo | não |
| P1 | Tabela de proporções gerada pela CLI a partir de `balance.json` (produção/consumo por dia por edifício) e exposta no tooltip | Médio/Baixo | não |
| P1 | Amortecedores suaves: produtividade cai aos poucos com fome/frio; "expectativas" baixam após desastre; 1 evento de socorro por partida (ex.: mascate de caridade no inverno da fome) | Médio/Baixo | não |
| P2 | Distância por regiões/campo de distância por destino para escolha de depósito e alvo do lenhador (já previsto no plano b2) | Alto/Médio | não |

### 3.7 Feel, legibilidade e acessibilidade

**(1) Hoje.** Critério de feel em todo marco (decisões 08/10): resposta < 100 ms, animação 150–250 ms, câmera suave,
nunca > 2 min sem decisão; passe de 15 min (`docs/feel_notes.md`, até agora só passes medidos pelo agente).
Acessibilidade mínima (UI guide §7): escala 80–150% feita; contraste 4,5:1, "nunca só cor", legenda para som, pausa em
menus e tooltip persistente pendentes. Sons Kenney placeholder (P26).

**(2) Mercado/literatura.**
- **GAG básico** (itens literais): ajustar velocidade do jogo; remapear controles; evitar padrões repetitivos/piscantes;
  nunca informação essencial só por cor ou só por som; volumes separados; tutoriais interativos; salvar as
  configurações ([lista](https://gameaccessibilityguidelines.com/full-list/)). Intermediário: lembrete do objetivo atual,
  autosave.
- **XAG:** contraste mínimo de elementos inativos 3:1; contraste altíssimo atrapalha alguns jogadores com deficiência
  cognitiva → **fundo de opacidade ajustável** atrás do texto ([XAG](https://learn.microsoft.com/en-us/gaming/accessibility/guidelines)).
- **Daltonismo:** ~1 em 12 homens (NEI, [página](https://www.nei.nih.gov/learn-about-eye-health/eye-conditions-and-diseases/color-blindness));
  laranja × azul funciona para os três tipos comuns; testar com simulador em 100%
  ([GAG](https://gameaccessibilityguidelines.com/ensure-no-essential-information-is-conveyed-by-a-colour-alone/)).
- **APX (AbleGamers):** *Slow It Down*, *Total Recall*, *Distinguish This From That*, *Second Channel*
  ([APX](https://accessible.games/apx)).
- **Contraexemplo:** Against the Storm criticado por não ter suporte a daltonismo
  ([TechRadar](https://www.techradar.com/gaming/consoles-pc/against-the-storm-review)).
- **Noite legível:** "faça parecer escuro, não deixe escuro"; lua azulada; não depender de albedo escuro
  ([Level Design Book](https://book.leveldesignbook.com/process/lighting/darkness)).
- **Juice cozy:** "máximo output para mínimo input", mas sem picos que quebrem a calma (Dorfromantik: animações e sons
  que não podem "perturbar a atmosfera calma", [Game Developer](https://www.gamedeveloper.com/disciplines/sparking-joy-through-tile-placement-in-idyllic-village-builder-i-dorfromantik-i-)).

**(3) Gaps.** Sem velocidade < 1×; sem fonte simples; sem teste de daltonismo registrado; nenhum passe de feel
humano ainda; chuva/neve/noite (Sky3D) podem apagar o contraste recurso × chão (neve + pedra clara).

**(4) Recomendações.**
| P | Recomendação | Impacto/Custo | Conflito |
|---|---|---|---|
| P0 | Rodar o checklist §5.7–§5.8 em 4 condições: dia limpo, chuva, noite, neve | Alto/Baixo | não |
| P0 | Teste com simulador de deuteranopia/protanopia/tritanopia (Color Oracle) em cada captura de entrega | Alto/Baixo | não |
| P1 | Piso de exposição/ambiente noturno no Sky3D; janelas, fogueira e tochas como pontos focais | Médio/Baixo | não |
| P1 | Velocidade 0,5×; fundo de texto com opacidade ajustável; fonte simples opcional; legenda visual para todo alerta sonoro | Médio/Baixo | não (opções novas) |
| P1 | "Reduzir movimento" (desliga tremidas, pops de escala, partículas fortes) | Baixo/Baixo | não |

### 3.8 Direção de arte

**(1) Hoje.** Visual 3D cozy/cartoon (Koastalia), sem pixel art (08/10); paleta V2 "meadow" mais saturada (08/10);
migração do mundo para Kenney (Nature, Fantasy Town, Survival) com recolor da paleta (09/10; `asset_manifest.md`
§9); GDD v0.3 §12: prado verde saturado e claro, 25% de outono, água turquesa com areia larga; Sky3D, grama cartoon.

**(2) Mercado/literatura.**
- **Saturação como hierarquia:** TF2 usa cores "à beira do realismo, com mais saturação e contraste de valor", "cores
  apagadas dominando e pequenas áreas de saturação"; detalhe de alta frequência "sobrepõe" a capacidade de destacar o
  gameplay por valor ([NPAR07](https://cdn.akamai.steamstatic.com/apps/valve/2007/NPAR07_IllustrativeRenderingInTeamFortress2.pdf)).
- **Legibilidade primeiro:** AoE IV — "legibilidade e jogabilidade no topo da lista" da direção visual; armas e
  capacetes aumentados; telhados/estandartes únicos ([PCGamesN](https://www.pcgamesn.com/age-of-empires-4/graphics));
  SC2 — "nada é sutil", formas grandes para câmera 3/4.
- **Cozy visual:** paleta quente, luz quente com fonte clara, sombras suaves, materiais naturais, horizonte parcialmente
  escondido por névoa (Project Horseshoe 2017); Bad North com tons "lavados" para serenidade
  ([PocketGamer.biz](https://www.pocketgamer.biz/interview/68638/indie-spotlight-plausible-concept-on-bad-north/));
  Dorfromantik: interpretação "ideal e harmônica" da paisagem real.
- **Koastalia (o que é público):** dev solo Pavel Valakh (Asaloda Games), lançamento TBA; tags *Hand-drawn*, *Stylized*,
  *Procedural Generation*; construções geradas a partir de formas desenhadas numa grade irregular; não há devlog de
  paleta/luz ([Steam](https://store.steampowered.com/app/2748140/Lands_of_Koastalia), [80.lv](https://80.lv/articles/relaxing-stylized-city-builder-with-unique-procedural-system/)).
- **Mercado cozy:** Tiny Glade vendeu 616 mil no 1º mês com "sem experiências de falha"
  ([Gigazine](https://gigazine.net/gsc_news/en/20241120-tiny-glade-600k-sold-reason)); Townscaper 380 mil contra 4 mil
  esperadas ([MCV](https://mcvuk.com/business-news/when-we-made-townscaper)); mas Dorfromantik ("raso a longo prazo")
  e Kingdoms and Castles ("trivial") mostram o teto sem profundidade.

**(3) Gaps, riscos, oportunidades.**
- **Risco principal:** a referência tem o **chão** como elemento mais saturado; seguindo à risca, recursos e aldeões
  competem com o gramado (contra TF2/AoE IV/SC2).
- **Outono 25% laranja/vermelho** compete com qualquer destaque vermelho/laranja de "marcado" e com frutos vermelhos.
- Koastalia é referência **visual**, sem guia público: precisamos de um art bible curto nosso (screenshots anotados
  com os traços adotados).

**(4) Recomendações.**
| P | Recomendação | Impacto/Custo | Conflito |
|---|---|---|---|
| P0 | Experimento A/B de captura: chão "meadow" atual × chão com saturação média e menos ruído, com recursos/aldeões/telhados mais saturados; avaliar com o checklist §5.8 e o teste de 5 s | Alto/Baixo | **⚠ CONFLITO** com a paleta V2 "meadow" (08/10) e GDD v0.3 §12 — só experimento; decisão do dono |
| P0 | Destaque de interação/marcação por contorno, ícone ou padrão; frutos com forma clara (cachos), não só cor | Alto/Baixo | não |
| P1 | Art bible de 1–2 páginas: paleta por categoria (chão, vegetação, interativos, UI), regra de saturação, silhueta, escala de props, exemplos ✓/✗ na câmera do jogo | Médio/Baixo | não |
| P1 | Névoa leve nas bordas do mapa 192² (enquadra e esconde o fim do mundo) | Médio/Baixo | não |
| P2 | Variação por shader (estação, neve, umidade) em vez de novas malhas | Médio/Baixo | não |

---

## 4. Playtest: protocolo e métricas (para a etapa 7 da v0.3)

**Protocolo (RITE + think-aloud + métricas).**
1. 5–8 participantes, ao menos 3 que **não** jogam RTS (o RITE do AoE II usou só novatos). Observador em silêncio, sem
   dicas (Valve).
2. Corrigir **entre** participantes quando problema e solução forem claros (RITE); quando a frequência for incerta,
   rodar mais pessoas antes de mexer. Lembrar: depois de cada correção aparecem erros novos, porque os jogadores vão
   mais longe ([RITE](https://jpattonassociates.com/wp-content/uploads/2015/04/rite_method.pdf)).
3. A cada 5 min, pergunta rápida: "o mais interessante e o mais chato até agora?" (curva de interesse, Schell).
4. Perguntas do UI guide §9.2 depois de 20 min + três novas: "O que o reeve faz por você agora?", "Quando você confiou
   nele, e quando não?", "O que você faria se o reeve sumisse?".
5. 5 pessoas acham problemas frequentes, **não** validam correção de problema raro: depois de 6 participantes sem
   recorrência há ≥ 88% de confiança para p = 0,30, mas só 47% para p = 0,10 (tabelas de Lewis, citadas no RITE) →
   usar a telemetria local para os raros.

**Métricas (log de sessão já existe: `SessionRecorder`, P23/P25).**
| Métrica | Alvo | Origem |
|---|---|---|
| Tempo até a 1ª ordem | < 60 s | GDD v0.3 §14 |
| Suprimentos perdidos para a chuva (%) | baixo no naive, > 0 no passive | GDD v0.3 §5 |
| Tempo até a 1ª delegação aceita | 15–25 min | GDD §11 |
| % de sugestões do reeve aceitas / adiadas / "Nunca" | > 50% aceitas | UI guide §9.3 |
| **Ordens manuais/min por degrau** | caem ao subir de degrau | tese (substitui "cliques/min" sozinho) |
| **Minutos sem decisão** (sem ordem, construção, decreto ou leitura de painel) | 0 trechos > 2 min | feel |
| **Leituras de painel/tooltip por min** | sobem com o tempo | GDD v0.2 §8 |
| Cliques em "por que parado?" por colono/edifício | queda entre playtests | proxy de opacidade |
| Uso de pausa e velocidade (tempo em cada) | — | GAG / ritmo |
| Sobrevivência e população no 1º inverno | ingênuo sobrevive com folga apertada | GDD v0.3 §14 |
| Ponto de abandono (tela, minuto) | — | funil |
| Heatmap de cliques nos primeiros 60 s | recursos dentro do 1º quadro | RITE AoE II |

**Funil público (pós-lançamento):** conquistas em degraus (1º telhado, 1ª delegação, 1º decreto, 1º inverno) servem de
funil gratuito nas estatísticas globais da Steam ([Bycer](https://www.gamedeveloper.com/design/how-to-judge-game-design-with-achievement-analytics)).
Telemetria remota de produto (opt-in) é decisão nova (privacidade) — ver §6.

---

## 5. Checklists reutilizáveis (por marco / playtest)

> Marcar ✓ / ✗ / n.a. e anotar em `docs/feel_notes.md` junto do passe de feel. Itens com ★ são bloqueantes para
> fechar o marco.

### 5.1 Interface / HUD
- [ ] ★ Toda tela responde "o quê / por quê / o que fazer" (UI guide §0) e passa no teste de 5 s.
- [ ] ★ Todo agente ou edifício parado tem "por que parado?" com causa e ação sugerida.
- [ ] Ociosos visíveis: contador + atalho para o próximo colono ocioso.
- [ ] Valor + tendência + previsão ("dura ~N dias") para comida e lenha sem abrir painel.
- [ ] No máximo 3 alertas visíveis e 1 cartão de sugestão por vez; nada modal sem necessidade.
- [ ] O ícone do edifício na UI é o mesmo do marcador no mundo.
- [ ] Previsão de ameaça (chuva chegando, inverno em N dias) sempre visível.
- [ ] Funciona em 1280×720 e 2560×1440; texto ≥ 12 px; nenhum painel cobre > 25% de um lado.

### 5.2 Mundo / terreno / geração procedural
- [ ] ★ 100% das seeds de teste com início válido; 0 falhas catastróficas após re-roll; p95 de geração < 200 ms.
- [ ] ★ Sem penhasco no raio do início; água garantida; nenhum recurso essencial "pulado" (assert).
- [ ] Distâncias por **caminho** do início a água, floresta, 10ª pedra, afloramento, caça: p5–p95 dentro das faixas
      do GDD v0.3 §8.3; variância entre seeds registrada (justiça).
- [ ] Expressive range atualizado (heatmaps 2D com ≥ 10 mil seeds) após mudança de parâmetro; nenhum viés novo.
- [ ] ≥ 1 "protagonista" por seed em X% das seeds (meta a definir).
- [ ] Teste humano de diferenciação: entre 20 miniaturas, quantas o jogador acha repetidas.
- [ ] Fração de área por nível de terraço dentro da meta; rampas visíveis e alcançáveis.

### 5.3 Personagens e animais
- [ ] ★ A ~15 px de altura (zoom de referência), a tarefa do colono é lida pela carga/ferramenta (≥ 4 tarefas).
- [ ] Pés/base mais escuros que torso/topo (aldeões e animais).
- [ ] 10 aldeões andando juntos não estão em uníssono.
- [ ] Balão de estado com ícone (não só cor) e, se possível, tendência.
- [ ] Boi, cervo, lobo, coelho distinguíveis pela silhueta em escala de cinza.
- [ ] Uma só família humanoide; escala normalizada pela porta do Fantasy Town.
- [ ] Ameaça animal sempre com aviso prévio (som/visual) e sem morte de colono (D8).

### 5.4 Core loop / abertura
- [ ] ★ 1ª ordem < 60 s; 1º objetivo nasce do mundo (chuva), não de texto.
- [ ] ★ Recursos dos primeiros 5 min e o local do depósito visíveis no 1º quadro da câmera.
- [ ] Até o min 5 só os verbos selecionar, coletar, construir, acelerar.
- [ ] Nenhum verbo novo ensinado durante emergência (chuva, lobos); ensino no "vale" seguinte.
- [ ] 1ª delegação 15–25 min, apresentada como conquista (som + crônica).
- [ ] O micro de RTS tem sucessora visível em ≤ 5 min após a delegação e uso residual em emergência.
- [ ] Nenhum trecho > 2 min sem decisão relevante (medido como em §4, não por cliques).
- [ ] Depois do 1º inverno existe uma nova pressão anunciada nos próximos 20–30 min.

### 5.5 Delegação / IA
- [ ] ★ Toda decisão do reeve aparece no livro de contas com o quê + por quê + qual decreto.
- [ ] O livro mostra o histórico de acerto do reeve e quando ele erra (sobrecarga de CA).
- [ ] Sem oscilação: trocas de decreto/ano dentro do limite; nenhum recurso alternando carregadores dia sim, dia não.
- [ ] Nenhuma ordem estoura o máximo do decreto (conta o que está em trânsito).
- [ ] Nenhuma tarefa espera > N dias sem ser atendida (aging).
- [ ] ★ Asserção da CLI: delegator não é estritamente melhor que micromanager (eficiência ≤; ações/h ≪).
- [ ] O cartão de sugestão mostra o que acontece, o custo e o que se perde (GDD v0.2 §3.2) e não é modal.
- [ ] Jogador consegue revogar/vetar qualquer decreto; efeito explicado.

### 5.6 Economia / balanceamento
- [ ] ★ Hash diário idêntico em: 2 execuções, save→load→continua, soak de 50 anos.
- [ ] Relatório com p10/p50/p90 sobre ≥ 50 seeds por persona (passive, naive, optimal, delegator, micromanager).
- [ ] Crises no horário-alvo (lenha 30–45 min; 1 crise a cada 20–30 min) com dispersão aceitável entre seeds.
- [ ] Passivo falha, ingênuo sobrevive com folga apertada, ótimo sobra (GDD v0.3 §14) — no mapa **gerado**.
- [ ] Sem deadlock; itens parados > N dias abaixo do limite; ledger fechado.
- [ ] Toda espiral tem aviso antecipado ("dias até acabar") e degradação gradual.

### 5.7 Feel / acessibilidade
- [ ] ★ Resposta visual + sonora < 100 ms a toda ordem/clique; animação de UI 150–250 ms.
- [ ] Tarefa delegada > 10 s tem progresso visível no mundo.
- [ ] Nenhuma decisão essencial exige reação < 2 s ou > 5 comandos seguidos.
- [ ] ★ Nenhuma informação essencial só por cor ou só por som.
- [ ] Contraste de texto ≥ 4,5:1; elementos inativos ≥ 3:1.
- [ ] Pausa em qualquer momento (inclusive menus); velocidade ajustável; atalhos remapeáveis; configurações salvas.
- [ ] Sem screenshake; opção de reduzir movimento.
- [ ] Passe de feel humano de 15 min registrado.

### 5.8 Direção de arte e legibilidade visual (captura 1920×1080, pitch 45–50°, ~130 m)
- [ ] ★ Em escala de cinza, aldeões, recursos e construções se separam do chão.
- [ ] ★ O elemento mais saturado da tela é interativo (não o gramado).
- [ ] Cada recurso (árvore madura, arbusto com fruta, pedra solta, afloramento, caça) identificável pela forma.
- [ ] Árvores de outono não se confundem com "marcado" nem com frutos.
- [ ] Simulador de deuteranopia, protanopia e tritanopia: seleção/marcação ainda distinguíveis.
- [ ] Penhascos leem como desnível (face, topo e base distintos); rampas visíveis.
- [ ] Caminhos contrastam com o gramado em **valor**, não só em matiz.
- [ ] Os itens ★ acima passam também à noite, na chuva e na neve.
- [ ] Miniatura a 25%: ponto focal claro (vila) e "protagonistas" do mapa visíveis.
- [ ] Coerência de pacote: proporção, densidade de detalhe e sombreamento iguais entre personagens, casas e natureza.

---

## 6. Itens que conflitam (ou tocam) decisões registradas — sugestões para `pending_decisions.md`

> Não decidido aqui. Cada linha: sugestão · decisão afetada · por quê.

| # | Sugestão | Decisão afetada | Motivo (fonte) |
|---|---|---|---|
| C1 | Experimento A/B com chão de saturação média e interativos mais saturados | **Paleta V2 "meadow" mais saturada** (08/10) e GDD v0.3 §12 ("prado verde saturado") | Hierarquia de saturação TF2/AoE IV/SC2 (§3.8) |
| C2 | Construtores buscam o material quando não há carregador (carregador vira acelerador) | **P1** (só carregadores; obra para) — aprovado 09/10 | Against the Storm, Hauler Station "é só um bônus" (§3.5) |
| C3 | Zona sem penhasco e colocação por regra antes da nota; esculpir rampas antes dos terraços | Pipeline do gerador **GDD v0.3 §8.3** (aprovado); muda versão do gerador, hash e saves | AoE2 RMS, Factorio, Spelunky (§3.2) |
| C4 | Exceção de controle direto: "convocar" famílias em emergência; boi sempre selecionável | **GDD v0.3 §4.1** (aldeão de família não recebe ordem direta) | Burnout de skill atoms (Cook) (§3.4) |
| C5 | Trilhas que surgem pelo uso (só visual primeiro; depois custo de terreno) antes de F3–F4 | **Backlog 08/10** (desire paths para F3–F4, após MVP) | Detalhe reativo cozy (Tiny Glade); Foundation (§3.2) |
| C6 | Overlay de fluxo mínimo antes do playtest v0.3 | UI guide §2.3 (overlay de fluxo aos ~3h) e roadmap (overlay "depois do Marco 2") | Logística opaca é a queixa nº 1 (§3.1) |
| C7 | Teclas: atalho de colono ocioso e eventual realocação dos grupos de controle | **P33** (1–4 velocidade, F1–F5 mapas, grupos adiados) e GDD v0.3 D6 | AoE "." (§3.1) — só achar tecla livre |
| C8 | Velocidade 0,5× para acessibilidade (o 8× continua travado até a 1ª delegação) | GDD v0.3 §2 (pausa · 1× · 2× · 4× · 8×) | GAG básico "adjust game speed" (§3.7) |
| C9 | Telemetria de produto opt-in (fora do playtest) | P23 (SessionLog só para playtest) | Funil/GUR (§4) — decisão de privacidade |
| C10 | Outono agrupado em manchas (ainda determinístico por hash/campo) | GDD v0.3 §8.2 ("por hash") | Legibilidade e Red Blob (§3.2) — impacto baixo |

Tensão (não conflito): **D12** (frente de chuva em 3–6 min) × "não ensinar sob estresse" — compatível se o aviso
"chuva chegando" vier antes e nenhum verbo novo for introduzido durante a chuva (§3.4).

---

## 7. Tabela comparativa de jogos de referência

| Jogo | Delegação / automação | Microgestão | Onboarding | HUD característico | Leitura do mundo | Ritmo da abertura (≈10 min) |
|---|---|---|---|---|---|---|
| **AoE II DE** | Ponto de reunião em recurso, replantio automático, fila; nada estratégico | Alta (aldeão ocioso é a tecla mais usada) | Campanha + desafios Art of War com medalhas; RITE no tutorial original | Barra de recursos + contador de ociosos + grade de comandos | Silhueta de unidade; recurso esgotando à vista | Build order contínua: TC produz sem parar, comida → madeira → Feudal |
| **AoE IV** | Igual; no console, predefinições de prioridade dos aldeões | Alta | "Mission Zero" automática + 5 Art of War; plano de tutorial analítico | Alto contraste e escala de texto; ícones = estandartes | Menos detalhe nas unidades; telhados únicos | Idem; marco como avanço de era |
| **StarCraft II** | Quase nenhuma | Muito alta (APM) | Campanha introduz 1 unidade por missão + Challenges | Painel inferior denso; elenco pequeno | Silhueta e escala calibradas para não esconder | Trabalhadores no mineral, supply, scout |
| **Northgard** | Colono designado a prédio vira especialista; ocioso gera comida | Média | Campanha; "fácil de pegar, difícil de dominar" | Calendário/inverno, felicidade, comida e lenha | Neve muda o mapa; regiões e criaturas | Scout, casa, lenhador, comida; expandir região |
| **Banished** | Contagem por profissão; ociosos viram carregadores | Baixa–média | Fraco (crítica de feedback) | Menus de relatório | Campos e estoques visíveis, pouca explicação | Casas, comida, lenha antes do inverno |
| **Farthest Frontier** | Prédios com raio e trabalho autônomo | Média | Não documentado | Avisos + painéis de prédio | Fertilidade e doença visíveis | Abrigo, comida, coleta, poço |
| **Kingdoms and Castles** | Autônomo | Baixa | Simples (crítica: vira "trivial") | Mínimo | Cidade e castelo legíveis | Castelo, casas, comida |
| **Foundation** | Zonas: aldeões constroem casas e trilhas sozinhos | Baixa | Sandbox | Pintura de zonas | Crescimento orgânico | Pintar zona, extração |
| **Against the Storm** | Prédios com receitas; Hauler Station como bônus | Média | Tutorial refeito + mentora opcional + "Read More" | Impaciência × Hostilidade, ordens | Ciclo de chuva; clareiras perigosas | Lenha, abrigo; relógio da Rainha já correndo |
| **RimWorld** | Aba Work (prioridade 1–4), áreas, contas | Alta (opcional) | Ajudante contextual | Alertas laterais, aba Work | Barras de humor, sujeira, sangue | Pouso, abrigo, comida; 1º evento leve (Cassandra) |
| **Settlers II** | Logística automática pela rede de estradas | Média (desenhar estradas) | Missões | Rede de bandeiras | Carregadores e burros nas estradas | Estradas, lenhador, serraria |
| **Stronghold** | Alavancas: impostos, rações, popularidade | Média | Campanha | Medidor de popularidade | Camponeses chegando/saindo | Castelo, comida, rações |
| **Anno 1800** | Cadeias automáticas; carroças visíveis | Média (proporções) | Campanha + tooltips | Necessidades por camada; estatísticas (pós-patch) | Carroças e filas no armazém | Casas de fazendeiros, cadeias simples |
| **Frostpunk** | Leis no lugar de pedidos; autômatos | Média | Cenário guiado | Gerador, Esperança/Descontentamento, termômetro e previsão | Raio de calor, neve | Carvão/madeira, 1ª lei, frio chegando |
| **Dwarf Fortress (Steam)** | Labors, work details, work orders do gerente | Alta | Tutorial "para não ir à wiki" | Menus densos, agora com mouse | Tiles, anões, estoques | Cavar abrigo, oficinas, comida |
| **Timberborn** | Distritos + prioridade de obra + automação por sensores | Média | Não documentado | Painéis de distrito | Nível de água, seca | Represa, comida, madeira antes da seca |
| **Manor Lords** | Famílias em prédios; sem decretos de estoque | Média–alta ("workers waiting") | Fraco | Barra superior por região | Boi, estoques ao relento, lotes | (ver pesquisa da abertura) |
| **Townscaper / Tiny Glade / Dorfromantik** | O algoritmo faz o trabalho visual | Nenhuma | Nenhum (brinquedo) | Mínimo | Construção reage na hora | Construir e ver aparecer |
| **Songs of Syx** | Nobres administram partes do reino (podem conspirar) | Alta | Fraco (o dev admite) | Denso | Milhares de cidadãos | Sandbox |
| **Lands of Koastalia** (ref. visual) | Construção por formas desenhadas (procedural) | — | — (TBA) | — | Grade irregular, ilha procedural | — |
| **Ironvale (plano v0.3)** | **Escada**: ordens a colonos → famílias em construções (15–25 min) → decretos mín/máx com custo de CA e reeve que explica → mestres/guildas | **Alta no início → média → baixa** (decrescente por desenho) | Objetivo nasce do mundo (chuva); verbos limitados até o min 5; sugestão do reeve no lugar de tutorial | Madeira/pergaminho; alertas com prazo; "Por que X%?"; livro de contas; overlays | Pilhas que molham, tora no chão, boi arrastando, balões, overlays com mundo dessaturado | Selecionar, coletar, boi arrasta tora, fogueira + depósito coberto antes da chuva, tendas, caça |

**Leituras da tabela.** (a) Ninguém no mercado faz a **transição explícita** de controle direto para controle por
regra dentro da mesma partida; os vizinhos mais próximos são DF (labors → gerente), Songs of Syx (nobres) e Frostpunk
(leis) — é o espaço do Ironvale. (b) Os jogos com delegação forte têm **legibilidade fraca** (DF, Songs of Syx) ou
automação criticada (Stellaris, Victoria 3); ganhar aqui é ser o que **explica**. (c) Os cozy de maior venda são
"sem falha" e respondem na hora, mas os que param aí têm teto curto (Dorfromantik, K&C).

---

## 8. Bibliografia

**Legenda:** [V] aberto e conferido pelos pesquisadores; [S] só resumo do buscador ou fonte secundária.

### Metodologia, UX, onboarding, playtest
- Hunicke, LeBlanc, Zubek (2004). MDA. https://users.cs.northwestern.edu/~hunicke/MDA.pdf [V]
- MDA e DDE (visão geral). https://en.wikipedia.org/wiki/MDA_framework [S]
- Cook, D. (2007). The Chemistry of Game Design. https://www.gamedeveloper.com/design/the-chemistry-of-game-design [V]
- Cook, D. (2012). Loops and Arcs. https://lostgarden.com/2012/04/30/loops-and-arcs/ [V]
- Chen, J. (2006). Flow in Games (MFA). https://www.jenovachen.com/flowingames/Flow_in_games_final.pdf [V]
- Chen, J. (2007). Flow in Games (and Everything Else), CACM 50(4). https://api.crossref.org/works/10.1145%2F1232743.1232769 [V, metadados]
- Schell, lente da curva de interesse (resenha). https://www.gamedeveloper.com/design/feature-book-review-the-art-of-game-design- [S]
- Andersen et al. (2012). The Impact of Tutorials on Games of Varying Complexity. https://grail.cs.washington.edu/projects/game-abtesting/chi2012/chi2012.pdf [V]
- Medlock, Wixon, Terrano, Romero, Fulton (2002). RITE. https://jpattonassociates.com/wp-content/uploads/2015/04/rite_method.pdf [V]
- Ambinder (2009). Valve's Approach to Playtesting. https://cdn.fastly.steamstatic.com/apps/valve/2009/GDC2009_ValvesApproachToPlaytesting.pdf [S]
- GMTK, Valve's Secret Weapon. https://gmtk.substack.com/p/valves-secret-weapon [V]
- Hodent, The Gamer's Brain Part 2 (GDC 2016). https://gdcvault.com/play/1022951/The-Gamer-s-Brain-Part [S]; resumo: https://www.designative.info/2019/03/28/the-ux-of-fortnite-celia-hodent/ [V]
- Pinelle, Wong, Stach (2008). Heuristic Evaluation for Games (slides). https://cs.uwaterloo.ca/~lank/CS889/s20/slides/03.Readings.pdf [S]
- Desurvire, Caplan, Toth (2004). HEP. https://hci.rwth-aachen.de/materials/conferences/CHI2004/2p1509.pdf [S]
- Desurvire, Wiberg (2009). PLAY. https://link.springer.com/doi/10.1007/978-3-642-02774-1_60 [S]
- Thompson, Blair, Chen, Henrey (2013). PLOS ONE. https://pmc.ncbi.nlm.nih.gov/articles/PMC3776738/ [V]
- Swink (2007). Game Feel: The Secret Ingredient. https://www.gamedeveloper.com/design/game-feel-the-secret-ingredient [V]
- Nielsen (1993). Response Times. https://www.nngroup.com/articles/response-times-3-important-limits/ [V]
- Juice it or lose it (cobertura). https://www.gamedeveloper.com/design/video-is-your-game-juicy-enough- [S]
- Iacovides et al. (2015). Removing the HUD. https://discovery.ucl.ac.uk/id/eprint/1470398/1/chip0179-iacovidesA.pdf [V]
- Andrews (2010). Game UI Discoveries (Fagerholt & Lorentzon). https://www.gamedeveloper.com/design/game-ui-discoveries-what-players-want [V]
- Fulton (2002). Beyond Psychological Theory. https://mud-dev.zer7.com/2002/6/23903/ [S]
- Seif El-Nasr, Drachen, Canossa (2013). Game Analytics. https://www.springer.com/br/book/9781447147688 [S]
- Drachen, Mirza-Babaei, Nacke (2018). Games User Research. https://www.porchlightbooks.com/products/games-user-research-anders-drachen-9780198794844 [S]
- Bycer (2020). Achievement Analytics. https://www.gamedeveloper.com/design/how-to-judge-game-design-with-achievement-analytics [V]
- Project Horseshoe (2017). Coziness in Games. https://www.projecthorseshoe.com/reports/featured/ph17r3.htm [V]
- Sylvester, Designing Games (resenha). https://www.gbgames.com/2014/05/07/book-review-designing-games-by-tynan-sylvester/ [V]; apofenia: https://gdt.stanford.edu/how-do-you-create-apophenia/ [S]
- Koster, A Theory of Fun (comentário). https://www.tale-of-tales.com/DramaPrincess/wp/?p=120 [S]

### Acessibilidade
- Game Accessibility Guidelines. https://gameaccessibilityguidelines.com/full-list/ [V]; cor: https://gameaccessibilityguidelines.com/ensure-no-essential-information-is-conveyed-by-a-colour-alone/ [V]
- Xbox Accessibility Guidelines. https://learn.microsoft.com/en-us/gaming/accessibility/guidelines [V]
- NEI, Color Blindness. https://www.nei.nih.gov/learn-about-eye-health/eye-conditions-and-diseases/color-blindness [V]
- AbleGamers APX. https://accessible.games/apx [S]
- Anno 117 Accessibility Spotlight. https://news.ubisoft.com/en-us/article/2FfSSEUp1jtg9isC9NxowP/anno-117-pax-romana-accessibility-spotlight [S]
- AoE II/IV accessibility. https://www.ageofempires.com/age-ii-de-accessibility/ · https://www.ageofempires.com/age-iv-accessibility/ [S]
- Against the Storm review (daltonismo). https://www.techradar.com/gaming/consoles-pc/against-the-storm-review [S]
- Level Design Book, Darkness. https://book.leveldesignbook.com/process/lighting/darkness [V]

### IA, delegação, economia, determinismo
- Mark & Dill (GDC 2010). https://gdcvault.com/play/1012410/Improving-AI-Decision-Modeling-Through [S]
- Mark & Lewis (GDC 2015). Building a Better Centaur. https://www.gdcvault.com/play/1021848/ [S]
- IAUS. https://gameai.com/iaus.php [V]; Utility system: https://en.wikipedia.org/wiki/Utility_system [V]
- Graham, Introduction to Utility Theory. http://www.gameaipro.com/GameAIPro/GameAIPro_Chapter09_An_Introduction_to_Utility_Theory.pdf [V]
- Dill, Dual-Utility Reasoning. http://www.gameaipro.com/GameAIPro2/GameAIPro2_Chapter03_Dual-Utility_Reasoning.pdf [V]
- Orkin (2006). Three States and a Plan. https://pages.cs.wisc.edu/~dyer/cs540/handouts/gdc2006_orkin_jeff_fear.pdf [V]
- Humphreys, HTN. http://www.gameaipro.com/GameAIPro/GameAIPro_Chapter12_Exploring_HTN_Planners_through_Example.pdf [V]
- Forbus, Under the hood of The Sims. https://users.cs.northwestern.edu/~forbus/c95-gd/lectures/The_Sims_Under_the_Hood_files/outline.htm [V]
- Sylvester, The Simulation Dream. https://gamedeveloper.com/design/the-simulation-dream [V]
- Adams, Simulation Principles from Dwarf Fortress. http://www.gameaipro.com/GameAIPro2/GameAIPro2_Chapter41_Simulation_Principles_from_Dwarf_Fortress.pdf [V]
- RimWorld Work / Cassandra / Mood / Raid points. https://rimworldwiki.com/wiki/Work · https://rimworldwiki.com/wiki/Cassandra · https://rimworldwiki.com/wiki/Mood [S]
- ONI Priority. https://oxygennotincluded.wiki.gg/wiki/Priority [S]
- DF Work order / Manager. https://dwarffortresswiki.org/index.php/Work_order · https://dwarffortresswiki.org/index.php/DF2014:Manager [S]
- Banished pathfinding. https://shiningrocksoftware.com/?p=1971 [V]
- Songs of Syx logística. https://steamcommunity.com/app/1162750/discussions/0/4335356354422856538 [S]
- Against the Storm Hauler Station. https://eremitegames.com/?p=238209 [S]
- Victoria 3 DD71. https://paradoxinteractive.com/games/victoria-3/news/dev-diary-71-autonomous-investment-in-1-2 [V]
- Sheridan & Verplank (1978). https://ntrs.nasa.gov/citations/19790007441 [S]
- Lee & See (2004). Trust in Automation. https://csel.eng.ohio-state.edu/productions/intel/research/trust/Lee%20%26%20See%20Trust%20Review.pdf [V]
- Zhu et al. (2018). Explainable AI for Designers. https://research.tudelft.nl/en/publications/explainable-ai-for-designers-a-human-centered-perspective-on-mixe/ [S]
- Reynolds, Steering Behaviors (1999). https://www.red3d.com/cwr/papers/1999/gdc99steer.pdf [S]
- Adams & Dormans, cap. 5. https://ptgmedia.pearsoncmg.com/imprint_downloads/peachpit/peachpit/samplechapters/0321820274/0321820274_gamemechanics_ch05section.pdf [V]
- Dormans, Engineering Emergence. https://eprints.illc.uva.nl/id/document/11998 [V]
- Schreiber, Game Balance Concepts L10. https://gamebalanceconcepts.wordpress.com/2010/09/08/level-10-final-boss/ [V]
- Muirhead, The Art of the Spiral. https://gamedeveloper.com/design/the-art-of-the-spiral-failure-cascades-in-simulation-games [V]
- Holmgård et al., personas (AIIDE 2015). https://ojs.aaai.org/index.php/AIIDE/article/view/12849 [S]
- Zook, Harrison, Riedl (FDG 2015). https://faculty.cc.gatech.edu/~riedl/pubs/zook-fdg15.pdf [S]
- Candy Crush QA com IA. https://www.infoq.com/articles/candy-crush-qa-ai-saga/ [S]
- Factorio FFF #47 / #188. https://factorio.com/blog/post/fff-47 · https://factorio.com/blog/post/fff-188 [S]
- Bettner & Terrano (2001). 1500 Archers. https://web.cs.wpi.edu/%7Eclaypool/courses/4513-B03/papers/games/aoe.pdf [S]
- Fiedler, Fix Your Timestep. https://gafferongames.com/post/fix_your_timestep/ [S]
- Emerson, Flow Field Tiles. http://www.gameaipro.com/GameAIPro/GameAIPro_Chapter23_Crowd_Pathfinding_and_Steering_Using_Flow_Field_Tiles.pdf [V]
- Botea et al. (2004). HPA*. https://webdocs.cs.ualberta.ca/%7emmueller/ps/hpastar.pdf [S]

### Geração procedural, legibilidade, arte
- Red Blob: noise / mapgen2 / mapgen4. https://www.redblobgames.com/maps/terrain-from-noise/ · https://www.redblobgames.com/maps/mapgen2/ · https://www.redblobgames.com/maps/mapgen4/ [V]
- Quilez, Domain Warping. https://iquilezles.org/articles/warp/ [S]
- Compton, So you want to build a generator. https://galaxykate0.tumblr.com/post/139774965871/so-you-want-to-build-a-generator [V]
- PCG Book cap. 1 e 12. https://www.pcgbook.com/chapter01.pdf · https://www.pcgbook.com/chapter12.pdf [V]
- Smith & Whitehead (2010). Expressive range. https://www.pcgworkshop.com/archive/smith2010analyzing.pdf [V]
- Minecraft World generation. https://minecraft.wiki/w/World_generation [V]
- Dungeon generation in Unexplored. https://www.boristhebrave.com/2021/04/10/dungeon-generation-in-unexplored/ [V]
- Spelunky (Yu). https://gamedev.net/blogs/entry/2249558-the-full-spelunky-on-spelunky-xbla [S]
- WaveFunctionCollapse. https://github.com/mxgmn/WaveFunctionCollapse [V]
- How Townscaper Works. https://www.gamedeveloper.com/game-platforms/how-townscaper-works-a-story-four-games-in-the-making [V]
- Tiny Glade (80.lv). https://80.lv/articles/exclusive-tiny-glade-developers-discuss-bevy-proceduralism-publishers-cozy-games/ [V]
- AoE2 DE RMS Features. https://www.forgottenempires.net/age-of-empires-ii-definitive-edition/rms-features [V]; RMS docs: https://docs.racket-lang.org/aoe2-rms/sections.html [V]
- Factorio Map generator. https://wiki.factorio.com/Map_generator [V]
- RimWorld World generation. https://rimworldwiki.com/wiki/World_generation [S]
- TF2 NPAR 2007. https://cdn.akamai.steamstatic.com/apps/valve/2007/NPAR07_IllustrativeRenderingInTeamFortress2.pdf [V]
- TF2 GDC 2008. https://cdn.akamai.steamstatic.com/apps/valve/2008/GDC2008_StylizationWithAPurpose_TF2.pdf [V]
- Dota 2 Character Art Guide (CG Channel). https://www.cgchannel.com/2012/06/valve-releases-useful-character-design-guide [V, só a matéria]
- KayKit Adventurers / Quaternius / Kenney Mini. https://kaylousberg.itch.io/kaykit-adventurers · https://quaternius.com/packs/ultimatemodularcharacters.html · https://kenney.nl/assets/mini-characters [V]
- Godot MultiMesh / Visibility ranges. https://docs.godotengine.org/en/stable/tutorials/performance/using_multimesh.html · https://docs.godotengine.org/en/stable/tutorials/3d/visibility_ranges.html [V]
- Bad North (paleta). https://www.pocketgamer.biz/interview/68638/indie-spotlight-plausible-concept-on-bad-north/ [S]
- Dorfromantik (Game Developer). https://www.gamedeveloper.com/disciplines/sparking-joy-through-tile-placement-in-idyllic-village-builder-i-dorfromantik-i- [V]

### Mercado (jogos)
- AoE II DE what's new. https://www.ageofempires.com/news/whats-new-age-empires-definitive-edition-2/ [V]
- AoE IV quickstart. https://www.ageofempires.com/news/quickstart-guide-age-of-empires-iv/ [V]
- AoE IV tutorial analítico (KitGuru). https://www.kitguru.net/gaming/matthew-wilson/age-of-empires-iv-will-use-analytic-based-tutorials-and-art-of-war-missions-to-teach-players/ [V]
- AoE IV entrevista Relic (MobileSyrup). https://mobilesyrup.com/2021/04/10/age-of-empires-iv-relic-developer-interview/ [V]
- AoE IV gráficos (PCGamesN). https://www.pcgamesn.com/age-of-empires-4/graphics [S]
- SC2 (Kotaku, GDC 2011). https://kotaku.com/the-sacrifices-of-starcraft-ii-made-in-the-name-of-spor-5777029 [V]; arte: https://www.gamespot.com/articles/blizzard-talks-starcraft-ii-art-design/1100-6171176/ [S]
- Northgard. https://en.wikipedia.org/wiki/Northgard [V]; inverno: https://kotaku.com/northgard-demands-that-you-take-winter-seriously-1823670669 [V]
- Banished. https://en.wikipedia.org/wiki/Banished_(video_game) [V]
- Farthest Frontier. https://forums.crateentertainment.com/t/1-year-ago-today/129468 · https://store.steampowered.com/app/1044720/Farthest_Frontier/ [V]
- Kingdoms and Castles. https://uploadvr.com/kingdoms-and-castles-sim-city-vr/ [V]; review: https://worthplaying.com/article/2018/8/16/reviews/110447-pc-review-kingdoms-and-castles/ [S]
- Foundation (Polymorph). https://intoindiegames.com/reviews/previews/foundation-our-interview-with-developer-polymorph-games/ [V]
- Against the Storm. https://www.gamedeveloper.com/road-to-igf-2023/how-against-the-storm-managed-to-mix-city-building-and-roguelite-play [V] · https://eremitegames.com/guidance-and-lore-update-part-1/ [V] · https://www.blog.radiator.debacle.us/2023/06/design-review-of-against-storm-by.html [V] · https://www.gamedeveloper.com/press-release/critically-acclaimed-city-builder-against-the-storm-hits-2-million-copies-sold [V]
- RimWorld GDC 2017. https://gdcvault.com/play/1024232 [S]
- Settlers II estradas. https://settlers2.net/guides/roads/ [S]; New Allies: https://opencritic.com/game/14336 [S]
- Stronghold Warlords (manual). https://cdn.steamstatic.com/steam/apps/1295970/manuals/Stronghold_Warlords_Demo_Guide_-_Autumn_2020.pdf [S]
- Anno 1800 "Pushing carts". https://www.anno-union.com/?p=9737 [V]; estatísticas: https://anno1800.fandom.com/wiki/Statistics [S]
- Frostpunk. https://en.wikipedia.org/wiki/Frostpunk [V]; review: https://checkpointgaming.net/reviews/2018/04/frost-punk-review-not-winter-wonderland/ [S]
- Dwarf Fortress (vendas Steam). https://gamingbolt.com/dwarf-fortress-has-sold-over-600000-copies-in-its-first-two-months [V]
- Timberborn. https://mechanistry.com/press/timberborn-mechanizes-beavers-sells-750000-copies-in-a-year · https://store.steampowered.com/app/1062090/Timberborn/ [V]
- Manor Lords vendas. https://gamedeveloper.com/business/manor-lords-sales-now-exceed-2-5-million-copies [V]; Vaporlens: https://vaporlens.app/app/1363080/manor_lords [V]; 0.8.110: https://simulationdaily.com/news/manor-lords-0-8-110-beta/ [S]
- Townscaper. https://mcvuk.com/business-news/when-we-made-townscaper [V]
- Tiny Glade vendas. https://gigazine.net/gsc_news/en/20241120-tiny-glade-600k-sold-reason [V]
- Dorfromantik. https://en.wikipedia.org/wiki/Dorfromantik [V]
- Lands of Koastalia. https://store.steampowered.com/app/2748140/Lands_of_Koastalia · https://80.lv/articles/relaxing-stylized-city-builder-with-unique-procedural-system/ [V]
- Songs of Syx. https://store.steampowered.com/app/1162750/Song_of_Syx/ [V]
- GameDiscoverCo, previsões (City Builder #2 de 95 tags em pré-lançamento). https://newsletter.gamediscover.co/p/pc-and-console-games-predictions [V]

**Lacunas conhecidas (não verificadas):** redação exata das 10 heurísticas de Pinelle; sub-pilares de Hodent; fórmula
do "compensation factor" do IAUS; regra documentada de pixels mínimos por unidade (usamos cálculo próprio); devlogs de
paleta de Koastalia/Dorfromantik; algoritmo oficial de início do Civ; mecânica exata de Hostilidade/Impaciência do
Against the Storm; vendas de Banished e Kingdoms and Castles; percentuais medidos de queixas do gênero.
