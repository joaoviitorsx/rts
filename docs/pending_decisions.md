# Decisões pendentes (para o dono revisar)

> Registradas durante o trabalho autônomo. Cada uma: contexto, opções, escolha e motivo. A escolha é sempre a mais
> simples e reversível; quando possível fica atrás de um parâmetro em `godot/data/balance.json`.
>
> **Status (09/10/2026): todas aprovadas pelo dono** (P1–P24). Parâmetros mantidos em `balance.json`; o playtest calibra.
> Observar com atenção no playtest: **P1** (obra parada sem carregador), **P18** (passivo morre cedo), **P21** (faixa sugerida).

## 2A.1 — Construção consumindo material

### P1. Quem leva o material até a obra? (prioridade média)
- **Contexto:** o material sai do armazém e vai até a obra. Hoje só carregadores (famílias designadas ao Salão/Celeiro) fazem isso.
- **Opções:** (a) só carregadores; (b) os próprios construtores buscam o material quando não há carregador; (c) a obra puxa o material de forma abstrata.
- **Escolha:** (a). Sem carregador a obra para, com motivo no painel ("ninguém carregando material — designe carregadores")
  e alerta na tela. Ensina o carregador cedo e mantém a economia física.
- **Reverter:** (b) é um sistema novo (construtor vira carregador temporário); (c) quebraria a tese do estoque físico.
- **Risco:** jogador novo posiciona casas e nada acontece. O item 5 (abertura) deve começar com 1 carregador ou dar esse objetivo primeiro.

### P2. Prioridade dos carregadores (prioridade baixa)
- **Escolha:** (1) produção de recurso com decreto em falta → (2) material de obra (obra mais antiga primeiro) → (3) buffer de
  produtor mais cheio. Autorregula: sem material no armazém, os carregadores voltam a esvaziar produtores.

### P3. Trabalho da obra (prioridade baixa)
- **Escolha:** `buildDays` = dias de trabalho de 1 família a 100%; até `maxBuildersPerSite` (3) famílias somam. Famílias sem emprego
  ajudam a obra mais próxima que pode avançar (`autoBuilders` em `balance.json`, 1 = ligado). A obra não avança além da fração de
  material entregue; o material só é consumido na conclusão (cancelar devolve tudo ao armazém). Construtores gastam ferramenta.
- **Construtor designado pelo jogador** é liberado ao fim da obra (não vira trabalhador do edifício).

## 2A.2 — Decreto com faixa mín/máx

### P4. Faixa padrão e passo da UI (prioridade baixa)
- **Escolha:** ao criar, o máximo sugerido = mínimo × 1,25 (`hysteresisPermille` em `policies.json`, agora "faixa padrão");
  o jogador edita os dois. SpinBox com passo 1 e setas de 10 (passo 10 arredondava 1875 → 1880 e divergia do sim).
- **Validação:** mínimo ≥ 0 e máximo > mínimo; senão o comando é rejeitado com motivo.

### P5. Textos do livro de contas (prioridade média)
- **Contexto:** o log agora é estruturado (chave + argumentos), traduzido pelo `ui.csv` (`log.*`). Os argumentos ainda são
  nomes do conteúdo em português (recurso, edifício) e o trecho "(saiu de X)" vem pronto do sim.
- **Escolha:** manter assim até os nomes de conteúdo virarem chaves `tr()` (pendência de UI já registrada no roadmap).
- **Termos:** feito no item 6 — o painel é "Decretos do reeve" e o livro de contas usa "decreto".
- **Atualização (09/10):** nomes de recursos, edifícios e receitas agora vêm do `ui.csv` (`res.*`, `bld.*`, `rcp.*`) em toda a
  UI e nos rótulos 3D, com o nome do sim como reserva (conferido com `--locale=en`). **Falta:** os argumentos do livro de
  contas (nomes e o trecho "(saiu de X)") ainda saem do sim em português — exige mudar o log para ids; fica para depois do playtest.

## 2A.3 — Trajeto, estrada, horta, cenários

### P6. Pedra não tem fonte no jogo (prioridade ALTA)
- **Contexto:** a carroça traz 30 pedras e nada produz pedra. Estrada (1 pedra/célula), lenhador (2), celeiro (5) e o
  ferreiro do item 4 (madeira + pedra → ferramentas) disputam um estoque finito.
- **Opções:** (a) adicionar uma Pedreira (produtora de pedra) no item 4; (b) ferramentas só de madeira; (c) estrada grátis.
- **Escolha:** (a) no item 4 — mínimo necessário para o ferreiro funcionar e dar uso à estrada. Fica fora do escopo
  de "features novas"? É pré-requisito do ferreiro (receita aprovada); registrado aqui para o dono confirmar.

### P7. Estrada paga na hora, sem obra (prioridade média)
- **Escolha:** a estrada desconta pedra do armazém ao ser traçada (abstrato) e aparece na hora. Diferente das obras
  (material carregado), mas simples e legível. `roadStonePerCell` em `balance.json`.
- **Reverter:** transformar estrada em obra com material carregado (sistema da 2A.1 já existe).

### P8. Escala do trajeto (prioridade média)
- **Contexto:** o andar dos carregadores é comprimido com o dia (1 dia = 40 ticks; 1 célula fora da estrada = 3 ticks).
  Usado direto no trajeto, 13 células comeriam o turno inteiro.
- **Escolha:** trajeto = ticks de caminhada da rota × `commuteTicksPermille` (70‰): 20 células fora da estrada ≈ 20% do
  turno, ≈ 14% na estrada. Carregadores não têm trajeto separado (andar é o trabalho deles).
- **Efeito duplo de propósito:** quem mora longe perde turno **e** tempo livre (horta), como pedido pelo playtester.

### P9. Mudança automática de casa e ajuda na obra (prioridade baixa)
- **Escolha:** `autoRehome` (1): família com emprego muda para casa livre que economize ≥ `rehomeMinGainTicks` (12) de
  caminhada. Famílias ociosas **no momento** (sem emprego ou com emprego fora de estação) ajudam obras — sem isso, os
  campos prendiam as famílias no outono e nada era construído.

### P10. Jogadores roteirizados da CLI (prioridade média)
- **passive:** não faz nada. **naive:** casas longe do trabalho, sem estrada, sem decretos, preenche vagas e só reage à
  lenha ~30 dias antes do inverno. **optimal:** abertura roteirizada (comida primeiro, decretos, carregadores) + estradas.
  `optimal_no_roads` mede o efeito da estrada. São proxies; o dono pode querer outra definição de "ingênuo".
- **Ritmo:** `CrisisWatch.LongestQuietDays` (dias sem obra concluída, partida, crise ou troca de estação) é uma
  aproximação de "> 2 min sem decisão".

### P11. Comandos aplicados na hora (prioridade baixa)
- **Escolha:** a view aplica o comando no mesmo tick (`World.ApplyPendingCommands`), inclusive pausado. Resultado idêntico
  ao de esperar o próximo passo (teste). Corrige "posicionar pausado não faz nada".

## 2A.4 — Ferreiro + decreto de ferramentas

### P12. Pedreira adicionada (prioridade ALTA — confirmar) 
- **Escolha:** Pedreira (3×3, 15 madeira, 2 vagas, 0,25 pedra/trabalhador-hora), sem recurso no mapa (pode ser posta em
  qualquer lugar). Resolve P6. Modelo 3D ainda não existe: bloco cinza no `visual_catalog.json` (lacuna para o manifesto).
- **Alternativa:** ferramentas só de madeira e sem pedreira (pedra continuaria finita).

### P13. Receita e insumos do ferreiro (prioridade média)
- **Escolha:** 1 ferramenta = 2 madeira + 1 pedra; 0,05 ferramenta/trabalhador-hora (~1/dia com 1 família). Buffer de insumo
  de 20 (dividido entre os insumos) reabastecido pelos carregadores **quando cai abaixo da metade** (senão eles passavam
  todas as viagens trazendo migalhas e nunca levavam a produção embora).
- **Prioridade dos carregadores:** decreto urgente → obra/insumo (mais antigo primeiro) → buffer de produtor mais cheio.

### P14. Jogador "optimal" usa a indústria no 1º inverno (prioridade baixa)
- Substituído por **P22** (o ótimo cuida da pedreira e do ferreiro à mão para caber na CA). O `--opening` de dev no jogo
  usa o mesmo jogador (continua jogando durante `--days`).

## 2A.5 — Abertura sem roteiro

### P15. Urgência de transporte por necessidade (prioridade ALTA — confirmar)
- **Contexto:** sem decreto, os carregadores levavam sempre o buffer mais cheio (madeira) e a colheita apodrecia no
  campo enquanto as famílias iam embora de fome.
- **Escolha:** comida (sempre) e lenha (outono/inverno) abaixo de `haulUrgentDays` (20) dias de consumo viram urgentes,
  como um decreto em falta. Parâmetro em `balance.json`.

### P16. Carroça e capacidade (prioridade média)
- **Escolha:** comida 300 → **400**; lenha 60 (mantida). Varredura (3 seeds × 3 anos) em `docs/balance_report.md`:
  ingênuo não perde ninguém no ano 1 em nenhuma seed; passivo perde todos no 1º verão; ótimo estável.
- **Observação:** o resultado do ingênuo no ano 3 não é monótono com a carroça (mais comida às vezes piora) — o roteiro
  ingênuo nunca expande. Não otimizei para o ano 3.

### P17. Lista de objetivos (prioridade média)
- **Escolha:** 9 objetivos derivados do estado (carregadores → casas → campo → lenhador → lenha p/ inverno → decreto →
  celeiro → pedreira + ferreiro → 1º inverno), calculados na camada de UI como os alertas (sem estado salvo).
- **Alternativa:** objetivos como sistema da simulação (com recompensa/registro) — mais pesado; só se o playtest pedir.

### P18. Passivo morre no 1º verão (prioridade baixa)
- Fazer nada perde todas as famílias por volta de 8 min (fome). O GDD pede falha branda; o passivo literalmente não
  constrói casas nem campos. Se parecer duro demais no playtest: subir `subsistenceFoodCoverPermille`.

## 2A.6 — Capacidade Administrativa + sugestão de decreto

### P19. Custos e capacidade (prioridade ALTA — confirmar)
- **Escolha:** Salão = **4** de CA; decreto de faixa = 1 por recurso; ferramentas = 2 ("produção condicional",
  GDD v0.2 §3.1). Com 3 de CA e comida custando 2, o critério §8.3 ("delegou ≥ 3 em 60 min") ficava impossível.
- **Sobrecarga:** cada ponto acima do limite faz o reeve avaliar a cada 1 + n×`adminOverloadDelayDays` dias e falhar
  ações com n×`adminOverloadErrorPermille` (150 ‰) de chance (fluxo de RNG próprio "admin" → determinístico).
- **Sem fontes novas de CA no 2A** (escrivão, capela ficam para depois): o teto é 4 durante o playtest.

### P20. O que conta como repetição (prioridade média)
- **Escolha:** só remanejamentos — família tirada de um emprego e posta noutro produtor, ou troca de receita num
  edifício com gente. Primeiras contratações não contam (senão a sugestão de comida saía no 1º minuto só por
  preencher os campos). 3 ações em 60 dias (`suggestAfterActions`, `suggestWindowDays`); "Agora não" = 90 dias.

### P21. Faixa sugerida (prioridade média)
- **Escolha:** mínimo = média do estoque nos momentos das 3 ações, arredondada a 10; máximo = mínimo × 1,25.
  Simples e explicável ("quando o estoque estava em ~60"), mas para lenha pode ficar baixo para o inverno.
- **Alternativa:** considerar a demanda do próximo inverno para lenha/comida.
- **Atualização (09/10):** feito atrás de parâmetro — `suggestWinterCoverPermille` (500): para lenha e comida o mínimo
  sugerido cobre pelo menos metade do inverno (6 famílias → lenha 270); o cartão avisa "o mínimo foi aumentado".
  `0` volta ao comportamento anterior (só o estoque observado).

### P22. Jogadores roteirizados mudaram (prioridade baixa)
- **optimal:** mantém 3 decretos (3/4 de CA) e cuida da pedreira/ferreiro à mão (trocar o decreto de madeira pelo de
  ferramentas fazia a madeira zerar → sem ferramentas → colapso no ano 15).
- **naive:** no aviso de lenha remaneja as famílias dos campos para os lenhadores e as devolve na primavera (como um
  humano faria). Nunca aceita sugestões. Sobrevive aos anos 1–2; cai no inverno do ano 3.

### P23. Build e instrumentação do playtest (prioridade baixa)
- `godot/export_presets.cfg` (Windows/Linux, release, exclui cenas de teste/ferramentas e impostores);
  `project.godot` aponta `dotnet/project/solution_directory="../"` (a solução fica na raiz).
- `SessionLog`: CSV por sessão em `user://playtest/` (comandos e eventos) só para medir o playtest; não altera o jogo.

### P24. Crise 2 (ferramentas) dentro de 60 min (prioridade média)
- **Contexto:** com 12 ferramentas de reserva a crise 2 nunca aparecia em 3 anos — o GDD §8.3 pede ≥ 2 crises em 60 min.
- **Escolha:** carroça com **6** ferramentas (desgaste igual). Ingênuo: crise 2 no inverno do ano 2 (~40 min, janela
  30–60 min do GDD v0.2 §4.3) e passa a sobreviver aos 3 anos nas 3 seeds; ótimo evita com o ferreiro; ótimo sem estrada
  encontra na primavera do ano 2 (~25 min). Alternativas testadas: 4 ferr. + desgaste 12 (crise aos ~14–18 min, perto
  demais da crise 1) e 6 + desgaste 16 (~14–19 min).


## Branch feature/2B-polish (a partir de `playtest-2A-candidate`)

### P25. Formato do log de playtest (prioridade média)
- **Contexto:** o build candidato (congelado) grava só comandos e eventos (v1): crises 1 e 2 e decretos ativos não aparecem.
- **Escolha:** a branch grava o v2 (linha `meta`, estado diário, linhas de crise) com o mesmo código no jogo e na CLI
  (`SessionRecorder`); o analisador lê v1 e v2 e diz o que o v1 não tem. **Opção para o dono:** se quiser crises com horário
  já no 1º playtest, usar um build da branch (mas ele também traz as mudanças de view/UI dos itens seguintes).

### P26. Sons placeholder e retorno de UI (prioridade baixa)
- **Escolha:** pacotes Kenney (Interface, Impact, RPG Audio; CC0) em `art/vendor_raw/`, copiados pelo `setup_vendor.py`; o jogo
  toca só `godot/assets/audio/SFX_<id>.tres` (IDs lógicos: hover, clique "toc", martelada, carimbo, livro do reeve…), gerados
  por `tools/assets/build_audio.py`. Barramento "SFX"; volume geral e de efeitos nas Configurações (`user://settings.cfg`).
- **Botões:** hover = clarear + escala 1,03; clique = "afundar" (escala 0,97) — por escala, sem mexer no layout (o guia fala
  em 1–2 px; deslocar dentro de containers brigaria com o layout).
- **Decreto criado:** carimbo + pulso no medidor de CA (o "cartão voando até o medidor" fica para a arte final).
- **Poeira:** esferas translúcidas (a textura de gradiente não renderizava como partícula); discreta de propósito.

### P27. NPCs cozy (prioridade média)
- **Balões:** fome > frio > cansaço (trajeto ≥ 25% do turno ou ferramenta < 30%) > feliz (tem casa e a horta rendeu);
  no máximo **4** na tela, "feliz" só 1 por vez em rodízio de 5 s; ícones desenhados por `tools/assets/build_emotes.py`
  (arte própria, sem depender de emoji/fonte no PC do jogador).
- **Carga visível:** madeira/lenha = tora no ombro, comida = cesto, pedra/moedas = saco, ferramentas = caixote — modelos
  existentes (MVK/FP) em `PROP_Carry_*`, centralizados pela caixa envolvente.
- **Idles:** em casa alterna parado/conversa/sentado a cada ~7 s por aldeão.
- **Rotina:** aldeões já vão e voltam pela rota; casas com moradores acendem um lampião quente junto à porta no fim do dia
  e apagam de manhã (fade). Como o dia dura 4 s a 1x, o ciclo é rápido; a 8x quase não se vê.
- **Proporção:** escala do osso da cabeça (modificador de esqueleto), **não** ligada por padrão; comparação 1,0 · 1,15 · 1,3
  em `docs/reports/img_2B/proporcao_cabeca.jpg` (dev: `--head-scale=1.15`, cena `scenes/test/NPC_SHOWCASE.tscn`).
  Decisão do dono.

### P28. Item 4 (lacunas de assets com o Blender MCP) — BLOQUEADO (prioridade alta para você destravar)
- **Contexto:** nesta sessão não há ferramentas do Blender MCP (nenhum servidor MCP do Blender conectado; nada escutando
  na porta do addon). O Blender existe como flatpak (`org.blender.Blender`).
- **Não fiz:** estágios do trigo, pilhas de madeira/lenha/pedra, toco, poço, forja, ícones renderizados para o
  `icon_registry`. Não troquei de método por conta própria.
- **Opções:** (a) você abre o Blender com o addon MCP (localhost, `BLENDER_MCP_SAFE_MODE=1`) e eu sigo pelo MCP;
  (b) autoriza usar o Blender **headless** por linha de comando (`flatpak run org.blender.Blender -b --python …`, como o
  `setup_vendor.py --derive` já faz), gerando .glb em pasta nova, sem cena aberta para estragar.

### P29. "Enquanto você estava fora" (prioridade média)
- **Escolha:** botão "Ausência" na barra inferior → 1/5/10 anos com o reeve governando sozinho (sem jogador); a crônica
  (`Ironvale.Sim.Scripting.Chronicle`, só observa) guarda: famílias que foram embora (nomes e motivo), invernos com fome/frio
  ou sem perdas, anos em que o reeve mais remanejou, sobrecarga de CA, colheita farta (≥ 2000), obras concluídas, sugestão
  deixada esperando. Mostra até 12 momentos (por peso), em ordem cronológica. O avanço roda dia a dia com eventos ligados;
  o resultado do mundo é idêntico ao avanço antigo (teste `Watching_does_not_change_the_simulation`).
- **Limites:** sem nomes de reeve (delegados com personalidade são 2B, não feitos); argumentos da crônica (nomes de edifícios)
  ainda em português, como o livro de contas.


## v0.3 — mundo gerado + abertura RTS (09/10/2026)

As decisões abertas da v0.3 estão no `docs/GDD_v0.3_abertura_rts.md` §15 (D1–D12), cada uma com recomendação:
- D1: ritmo de 4 ticks/s (ano de 60 min, inverno aos 45–60 min);
- D2: base da branch;
- D3: mapa de 192²;
- D4: auto-continuação curta;
- D5: árvore sob obra;
- D6: teclas de grupo;
- D7: família nova só com casa vazia;
- D8: lobos só assustam;
- D9: Salão vira construção;
- D10: merge na `main`;
- D11: boi;
- D12: chuva estraga a pilha ao relento.
