# GDD v0.3 — Abertura RTS e mundo como recurso

> **Status:** aprovado em 09/10/2026 com as recomendações D1–D12 (D10, merge na `main`, aguarda ordem explícita).
> - **Substitui** a abertura do GDD v0.2 §4.1–4.2 e do Marco 2A (6 famílias + carroça + construção por menu).
> - **Continuam valendo**, a partir do momento em que existem famílias: o GDD v0.2 (CA, sugestão, crises, ritmo macro)
>   e as mecânicas do 2A.
> - **Plano:** `docs/plan_v0.3.md`. **Decisões abertas:** §15, cada uma com recomendação.
> - **Referências:** `docs/reference/koastalia_world.png` (visual) e `docs/reference/pesquisa_manor_lords.md`
>   (abertura: urgência diegética, começo físico e humilde, boi, toras × lenha, crescimento por casa livre).

## 1. A escada de delegação

O jogo inteiro é subir esta escada. Cada degrau tira um tipo de clique repetido da mão do jogador; o jogador **sente**
o degrau porque antes fez o trabalho à mão.

| Degrau | O jogador comanda… | Exemplo | Quando aparece |
|---|---|---|---|
| **0 (novo)** | **Pessoas**, uma a uma | "Você, corte aquela árvore." | Minuto 0 |
| 1 | **Famílias** em construções | Família Roth na cabana do lenhador | Primeiras casas (15–25 min) |
| 2 | **Decretos** | "Manter lenha entre 80 e 200" | Por volta do 1º inverno (45–60 min), ou antes se o jogador abrir o painel (P) |
| 3+ | Mestres, guildas, leis | Reeve com traço, bailio, senescal | Marco 2B em diante |

**Regra de design:** o degrau seguinte só é oferecido depois que o jogador **repetiu** o anterior. A sugestão do 2A
(contar ações repetidas) passa a valer também na passagem do degrau 0 para o 1.

## 2. Ritmo de tempo

O GDD v0.2 tem 1 ano = 24 min (10 ticks/s, dia de 40 ticks); com ele o 1º inverno chegaria aos ~18 min. A pesquisa
pede **1ª delegação aos 15–25 min** e **1º inverno aos 45–60 min**.

**Proposta (D1):** a simulação continua idêntica em ticks; só muda a conversão para o tempo real, de **10 para
4 ticks/s em 1x**.
- 1 dia passa a durar **10 s** e 1 ano, **60 min**.
- O jogo começa no **1º dia da primavera** (mês 0; hoje começa no mês 2).
- O balanceamento em ticks fica intacto; mudam só os minutos citados nos relatórios.
- Efeito colateral bom: o aldeão anda ~1,3 m/s (hoje parece correr) e o ciclo dia/noite deixa de piscar.

| Estação | Minutos em 1x | Papel |
|---|---|---|
| Primavera | 0–15 | Bando, chuva e suprimentos, fogueira, depósito coberto, tendas, caça |
| Verão | 15–30 | **Primeiras casas → famílias → 1ª delegação** (lenhador/coletor); campo |
| Outono | 30–45 | Toras × lenha, horta, preparar o inverno; crise da lenha |
| Inverno | 45–60 | **A prova**: a vila aguenta com o que foi delegado? Decretos e CA começam a aliviar |

Velocidades: pausa · 1x · 2x · 4x · 8x. Como no GDD v0.2, o 8x só é liberado depois da 1ª delegação.

## 3. Abertura minuto a minuto (alvo, em 1x)

| Minutos | Situação | O jogador faz | Aprende |
|---|---|---|---|
| 0 | **8 colonos** (indivíduos com nome) e **1 boi** numa clareira plana, perto de água, com floresta, pedras soltas e caça à vista. **Pilhas de comida e lenha no chão, ao relento.** Nuvens no horizonte | — | — |
| 0–1 | — | **1ª ordem de coleta** (árvore, arbusto, pedra) | Selecionar, clique direito = ação |
| 0–5 | Tudo vai para a pilha | Coleta; o **boi arrasta toras** inteiras (colono leva pouco por vez) | Carga física; o boi é escasso |
| 3–10 | **Chuva:** a pilha ao relento perde comida e lenha (balão e texto: "estragando na chuva") | **Fogueira** + **depósito coberto**: o 1º objetivo **nasce do mundo** | Construir com material carregado; abrigar estoque |
| 5–15 | Noites frias | **Tendas** (madeira + couro): **caça** cervos e coelhos; colhe frutas | Caça; couro; distância importa; lobos à noite longe da fogueira |
| 15–25 | Colonos passam o tempo cortando árvore por árvore | **Primeiras casas** → pares de colonos viram **famílias**; **cabana do lenhador / do coletor** com uma família | **1ª delegação**, apresentada como conquista (§4.6) |
| 25–45 | Árvore tem dois usos | **Toras** (construir) × **lenha** (inverno); horta no quintal; celeiro; novas famílias chegam **se houver casa livre** | Usos concorrentes; casa = convite |
| 30–45 | Outono, lenha curta | Mais uma família na lenha, colheita guardada | Crise 1 do GDD v0.2 |
| 45–60 | **1º inverno** | Pouco a fazer se delegou bem; Salão, decretos e CA aliviam | A prova do degrau 1; entrada no degrau 2 |
| Ano 2+ | Uma **jazida rica** (pedra, carvão ou ferro) no mapa | Especialização; ferreiro; o boi vira recurso disputado | Médio prazo |

**Regras de ouro (pesquisa §4):**
1. O 1º objetivo nasce do mundo (comida na chuva), não de um texto.
2. É possível agir no 1º minuto sem estoque: coletar e caçar são grátis.
3. Tudo é físico.
4. O micro do começo é proposital e temporário.
5. Sempre existe um próximo passo visível.
6. O mapa garante um bom começo.

Limite de onboarding: até o minuto 5, só os verbos **selecionar, coletar, construir e acelerar**.

## 4. Controle RTS: seleção e comandos

### 4.1 Seleção (só na view; a seleção não é estado do sim)
- **Clique** num colono ou no boi seleciona; **arrastar** cria um retângulo de seleção; **Shift** adiciona ou remove.
- **Duplo clique** seleciona os colonos visíveis que estão na mesma tarefa.
- **Ctrl+1–5** cria grupo e **1–5** recupera (velocidade muda de tecla, ver D6). **Esc** limpa a seleção.
- **Aldeão de família** (degrau 1+): clicar abre o painel da família. Ele não recebe ordem direta (o degrau 1 manda
  nele), e o retângulo de seleção o ignora.
- **Painel de seleção** (embaixo, UI guide §8.1):
  - um cartão por colono: nome, ícone da tarefa, carga e balão de necessidade;
  - com vários selecionados: contagem por tarefa;
  - ações: **Parar**, **Voltar à fogueira**, **Depositar carga**.

### 4.2 Comandos (clique direito contextual; o cursor muda ao passar sobre o alvo)
| Alvo | Colono | Boi | Cursor (game-icons) |
|---|---|---|---|
| Árvore madura | Cortar; a árvore cai como **tora no chão** | — | machado |
| Tora no chão | Carregar um pedaço (3 madeira por viagem) | **Arrastar a tora inteira** (12) até o depósito | corrente / mão |
| Pedra solta | Coletar pedra | Arrastar (16) | picareta |
| Arbusto / cogumelos | Colher (comida) | — | cesto |
| Cervo / coelho | Caçar (carne + couro) | — | arco |
| Lobo | Espantar (≥ 3 colonos; um só foge) | — | tocha |
| Obra | Construir e levar material | Levar material | martelo |
| Depósito / pilha | Depositar; **rachar lenha** (tora → lenha) | Descarregar | caixa / machadinha |
| Chão | Mover | Mover | pegada |

- **Shift + clique direito** põe a ordem na fila. **Ordem a um grupo:** cada colono pega um alvo diferente do mesmo
  tipo, perto do clicado.
- **Auto-continuação curta (D4):** quando o alvo acaba, o colono procura outro igual a até `autoContinueCells` (4)
  células. Se não acha, para com um balão **"?"**. É o atrito que o degrau 1 remove (o lenhador alcança um raio de 12).
- **Resposta:** som e marcador no chão em < 100 ms (feel do 2A), e uma fala curta do colono (placeholder).

### 4.3 Carga física
- Colono: **um tipo de recurso** por vez, até `carryPerTrip` (tabelas do 2A). Toras são pesadas, só 3 por viagem.
- **Boi:** carga grande (toras e pedra), mais lento, e não coleta. Pasta sozinho, sem custo de comida.
- Tudo vai fisicamente para a pilha ou o depósito mais perto. O material de obra sai do depósito e é levado à obra.

### 4.4 Colonos sem ordem
Ficam junto à fogueira e não trabalham sozinhos (é o degrau 0). Comer na pilha e dormir na tenda acontecem sem ordem.

### 4.5 Necessidades
- **Comida:** come uma vez por dia da pilha ou do depósito, no mesmo consumo do membro de família do 2A.
- **Frio e sono:** à noite, na primavera e no outono, quem está longe da fogueira e sem tenda sente frio, e frio deixa
  o colono mais lento. No inverno, sem casa nem tenda, conta déficit de frio.
- **Partida:** com déficit por `leaveAfterDeficitDays`, o colono vai embora, como as famílias no 2A. Jogo cozy:
  ninguém morre.

### 4.6 Sugestão do degrau 0 para o 1
Reaproveita o sistema de sugestão do 2A. Depois de `suggestAfterActions` ordens de coleta do mesmo tipo dentro da
janela, aparece o cartão: *"Seus colonos vivem cortando árvores. Uma Cabana de lenhador com uma família corta
sozinha as árvores em volta."*
- Botões: **Mostrar onde** (destaca o melhor lugar, com as árvores no raio) · Agora não · Nunca.
- Só aparece quando existe uma família.
- Ao aceitar e designar, um momento de conquista: som, e a crônica registra *"o dia em que paramos de cortar árvore
  por árvore"*.

## 5. Tempo e intempérie (novo)

- **Clima** sorteado por dia, de forma determinística: limpo, nublado ou chuva. A chance de chuva depende da estação;
  neve no inverno.
- O gerador garante uma **frente de chuva nos primeiros 3–6 min**, avisada antes com nuvens no céu e o texto
  "chuva chegando". É uma regra do mundo, não um roteiro.
- **Pilha ao relento:** comida e lenha perdem `openPileSpoilPermille` (20 ‰) por dia de chuva.
  - O tooltip e o balão explicam: "Ao relento: perde 2% por dia de chuva. Construa um depósito coberto".
  - O depósito coberto e o celeiro protegem.
- **Visual:** partículas de chuva, luz mais fria e chão escurecido. Neve fica para F3.

## 6. Colonos → famílias (o elo com o 2A)

- Quando uma **casa fica pronta**, 2 colonos sem família (os mais próximos, desempate por id) viram um
  **household** com `Members = 2` e `Workers = 2`.
  - Saem do controle direto e entram em todo o modelo do 2A: designação, trajeto, horta, decretos, ferramentas.
  - Opcional: "Quem mora aqui?" deixa trocar o par.
- **Crescimento por casa livre (D7, Manor Lords):** novas famílias de 2–4 pessoas chegam a cada ~10 dias **só se
  houver casa vazia** e comida para 30 dias. Construir casa = convidar gente.
- Tendas abrigam até 2 colonos sem família e não formam família.

## 7. Mundo como recurso

Tudo é um nó do mapa (posição, quantidade, estado), salvo e incluído no hash. Os números são placeholders e ficam em
`balance.json`.

| Nó | Dá | Finito? | Recresce | Quem explora |
|---|---|---|---|---|
| **Árvore** (carvalho, bétula, pinheiro; jovem / madura) | tora (madura: 12 madeira) | sim | **toco → muda (1 ano) → madura (~4 anos)**, se o toco não estiver sob construção ou estrada | colono e boi (0), lenhador (1) |
| **Pedra solta** | pedra (6) | sim; não recresce | — | colono, boi |
| **Afloramento** (jazida) | pedra (400) | sim, grande | — | só a **pedreira** construída em cima |
| **Jazida rica** (1 por mapa: pedra, carvão ou ferro) | define a especialização do ano 2 | grande | — | pedreira / mina (F2+) |
| **Arbusto de frutas** | comida (8) | por estação | frutifica no verão e no outono | colono, coletor (1) |
| **Cogumelos** | comida (4) | sim | brotam no outono, na floresta densa | colono, coletor |
| **Carvão / ferro** | — por ora | jazida | — | **visíveis e bloqueados** ("precisa de mina — mais tarde") |
| **Cervo** | comida 40 + couro 2 | rebanho | reproduz na primavera se o rebanho tiver ≥ 2 | caça |
| **Coelho** | comida 8 | sim | rápido | caça |
| **Lobo** | — | matilha | — | ameaça leve (§10) |

**Recursos:** madeira (**toras**) e **lenha** continuam separadas, como no 2A. A lenha sai de rachar toras (colono na
pilha; depois, a receita do lenhador). Recurso novo: **couro** (`hides`). Carne conta como `food`.

## 8. Gerador de mundo

### 8.1 Objetivo
Geografia viva no espírito da referência Koastalia:
- prado verde saturado;
- manchas irregulares de floresta densa, com clareiras;
- costa ou lago turquesa com faixa de areia;
- terraços baixos com paredões de pedra clara;
- caminhos de terra.

A geografia **importa**: água e penhascos bloqueiam, a fertilidade varia, floresta, pedra e caça têm lugar, e a
distância custa trajeto.

### 8.2 Diversidade por seed
| Parâmetro | Faixa |
|---|---|
| Água | costa em 1–2 lados, 0–2 lagos, ou ambos; sempre água a ≤ 15 células do início |
| Terraços | 3–4 níveis, com desníveis de 2–3 m |
| Floresta | cobre 35–55% da terra, em manchas grandes e bosques |
| Espécies | carvalho dominante; bétula e pinheiro conforme altitude e umidade; **copas de outono** em 15–30% (visual, por hash) |
| Fertilidade | ruído, com bônus perto da água e nas planícies baixas; 3 faixas legíveis (pobre / boa / rica) |
| Jazidas | 2–4 afloramentos (um a ≤ 30 do início); **1 jazida rica** sorteada; carvão e ferro a ≥ 40 do início |
| Fauna | 2–4 rebanhos de cervo; coelhos nas bordas; 1–2 matilhas longe do início |

### 8.3 Pipeline (C# puro, só inteiros: resultado idêntico em qualquer máquina)
1. **Altura:** ruído com hash inteiro em ponto fixo (fBm de 4 oitavas, domain warp leve) e um gradiente de costa
   nos lados sorteados.
2. **Água:** abaixo do mar vira oceano; bacias fechadas viram lagos. Uma faixa de 1–2 células em volta vira **areia**.
3. **Terraços:** a altura é quantizada em 3–4 níveis. Um autômato celular remove ilhas e dentes pequenos, para os
   paredões ficarem longos e suaves.
4. **Rampas:** uma a cada ~24 células de fronteira (mínimo 1). Depois, um flood fill a partir do início acrescenta
   rampas até ≥ 95% da terra ser alcançável.
5. **Fertilidade, umidade e floresta:** campos de ruído independentes. Árvores por célula com probabilidade ∝
   densidade, mais densas no miolo. Arbustos e pedras soltas na borda; cogumelos no miolo.
6. **Jazidas:** afloramentos na base dos paredões e nos terraços altos; a jazida rica e carvão/ferro longe do início.
7. **Início:** nota por candidato:
   - clareira plana de raio ≥ 8 no mesmo nível;
   - água a 6–15 células e floresta a 5–12;
   - ≥ 10 pedras soltas a ≤ 15 e afloramento a ≤ 30;
   - ≥ 6 arbustos a ≤ 20 e caça a ≤ 30.

   Escolhe o melhor (desempate por índice). Se nenhum passa, gera de novo com `seed + tentativa`, de forma
   determinística.
8. **Fauna:** rebanhos nas manchas grandes, longe do início.

**Tamanho (D3):** 192 × 192 células (384 m), com o início perto do centro. Hoje o mapa tem 64 × 64.

### 8.4 Save e hash
- **Camadas estáticas** (altura, água, rampa, fertilidade, jazida) saem de `seed + versão do gerador`. O save guarda
  os dois e regenera; uma versão diferente é rejeitada.
- **Estado mutável** (árvores, toras no chão, pedras, arbustos, jazidas consumidas, animais, clima) fica no save e
  entra no hash.
- **Árvores:** 1 slot por célula (espécie + estágio + idade). O jitter dentro da célula é só visual, por hash.

### 8.5 "Mapa plano"
O cenário `mvp_start` (mapa plano 64 × 64, 6 famílias e carroça) **continua existindo** como linha de base da CLI
(comparação "plano × gerado" no `balance_report`) e para os testes antigos (golden hash do candidato). Sai do menu
quando a nova abertura estiver balanceada (§13).

## 9. Economia ligada ao mundo

- **Lenhador** (degrau 1): corta as **árvores maduras reais** num raio de 12 células, da mais perto para a mais longe.
  - A ida e volta entra no trajeto do 2A. A floresta recua e cresce de volta devagar.
  - Painel: "Árvores maduras no raio: 34 · crescendo: 12 · ida média: 6 células". Sem árvores: "parada: raio sem
    árvores maduras — mude a cabana de lugar".
- **Coletor** (degrau 1): arbustos e cogumelos num raio de 10. **Acampamento de caça** (degrau 1, sem custo de
  material, como no Manor Lords): caça os rebanhos do raio sem esgotá-los (respeita a reprodução).
- **Pedreira:** só sobre afloramento; o fantasma fica vermelho fora dele. A jazida esgota, e isso aparece no painel.
- **Campo:** a colheita é multiplicada pela fertilidade da área (60–130%). O fantasma mostra "Fertilidade: boa".
- **Construir:** só no mesmo nível e sem água. Árvores no lugar **viram trabalho da obra** (D5): a madeira vai para o
  depósito.
- **Pathfinding:** água e paredões são intransponíveis e as rampas ligam os níveis. O custo fora da estrada do 2A
  continua.

## 10. Fauna e lobos

- **Cervos:** andam em rebanho pela floresta e fogem de colonos a ≤ 6 células. Caçar = perseguir, acertar (com chance
  ligada à ferramenta) e carregar a carcaça.
- **Coelhos:** ficam nas bordas e são caça fácil, de pouco rendimento.
- **Lobos (ameaça leve, D8):** aparecem à noite no outono e no inverno, nas bordas da floresta.
  - O colono **sozinho** fora do raio da fogueira foge e **larga a carga** (ela fica no chão e pode ser recuperada).
  - A fogueira acesa afasta os lobos. Um grupo de ≥ 3 os espanta. Ninguém morre.
- **View:** Quaternius Ultimate Animated Animal Pack (CC0), baixado pelo dono para `art/vendor_raw/`.
- **Backlog (F4):** domesticação e pastoreio.

## 11. Construções da abertura

| Construção | Custo (placeholder) | Função |
|---|---|---|
| **Fogueira** | 5 madeira | Calor no raio; ponto de encontro; afasta lobos; gasta lenha nas noites frias |
| **Depósito coberto** | 12 madeira | Protege o estoque da chuva; capacidade pequena; destino das cargas |
| **Tenda** | 6 madeira + 2 couro | Abriga 2 colonos sem família |
| Casa (2A) | 15 madeira + 4 pedra | Forma uma família (§6); casa vazia atrai uma família nova |
| Cabana do lenhador / do coletor | 10 madeira | Degrau 1 (§9) |
| Acampamento de caça | grátis (só trabalho) | Degrau 1 (§9) |
| Salão | do 2A | Construção do ano 1–2 (D9): dá CA e libera os decretos (degrau 2) |

## 12. Visual (análise de `koastalia_world.png`)

- **Câmera:** pitch ~45–50°, zoom médio (~130 m de largura). As capturas de entrega usam esse enquadramento.
- **Prado:** verde saturado e claro, com manchas de baixa frequência; a paleta "meadow" aprovada está próxima.
- **Água:** turquesa forte no fundo, clareando para ciano na praia, com **faixa de areia larga e clara** de borda
  suave e espuma fina.
- **Florestas:**
  - copas redondas de folhosas, densas, em manchas irregulares com clareiras;
  - ~70% verdes em 2–3 tons, ~25% amarelas/laranja e algumas vermelhas;
  - mais soltas na borda, com arbustos.
  - Implementação: MultiMesh por bloco com os carvalhos do Stylized Nature, cor por instância sobre a paleta global e
    impostores à distância (já existentes).
- **Terraços:** paredões baixos (2–3 m) de pedra clara seguindo a curva. Placeholder de rocha até chegarem os módulos
  do Meshy.
- **Campos** dourados de borda macia; **caminhos** de terra clara sinuosos.
- **Grama e flores:** `GrassCarpet` + tufos, trevos e flores do Stylized Nature em MultiMesh, mais ralos perto de
  caminhos e construções.
- **Relevo:** Terrain3D gerado da altura do sim, com camadas grama / terra / areia / caminho / campo.

## 13. Reaproveitado × obsoleto

- **Fica:** households, designação, trajeto, horta, decretos com faixa, CA, sugestão, ferreiro, ferramentas,
  transporte, construção com material, crônica, log de sessão, golden hash (mapa plano), juice e sons, NPCs cozy.
- **Muda:**
  - a abertura (cenário novo `wild_start`);
  - `Pathfinder` (água, paredões e rampas);
  - lenhador (árvores reais), pedreira (só em jazida), campo (fertilidade);
  - cartão de objetivos (agora reativo ao mundo);
  - ritmo (D1) e Salão (D9).
- **Para `docs/archive/`** (com registro em `decisions.md`):
  - `playtest_2A.md` e `plan_2A.md` (o playtest não acontece; o plano foi concluído);
  - `MvpOpening` e os objetivos do 2A, quando o `wild_start` estiver balanceado.
  - A tag `playtest-2A-candidate` fica como histórico.
- **Backlog** (vindo da pesquisa): mercado, extensões de quintal (galinheiro), variedade de comida → aprovação,
  posto de comércio, serraria e tábuas.

## 14. Critérios de aceitação

**Abertura (pesquisa §5, medidos na CLI com os jogadores roteirizados `rts_passive` / `rts_naive` / `rts_optimal`,
em 3+ seeds, e depois no playtest):**
- [ ] Começa com ~8 colonos, 1 boi e pilhas de comida e lenha ao relento que estragam com a chuva.
- [ ] Nos primeiros 60 s já existe uma ordem de coleta (playtest).
- [ ] O 1º objetivo ("salve os suprimentos da chuva") aparece por causa do mundo.
- [ ] Coleta, caça e transporte são físicos (boi e colonos carregando).
- [ ] Toras e lenha são recursos separados.
- [ ] A 1ª delegação acontece entre 15 e 25 min e é apresentada como conquista.
- [ ] O 1º inverno chega aos 45–60 min; o ingênuo sobrevive com folga apertada e o passivo não.
- [ ] Nenhum período de mais de 2 min sem decisão relevante.
- [ ] Todo "parado" tem causa e solução.
- [ ] Nenhuma seed gera início sem clareira, água, floresta, pedra e caça por perto.

**Técnicos:**
- 200 seeds passam no teste de mapa jogável e cada uma gera em < 200 ms.
- Mesma seed → mesmo hash, inclusive via save/load e soak de 50 anos.
- ≤ 8 ms por quadro no zoom da referência.

## 15. Decisões para aprovar (recomendação primeiro)

| # | Questão | Recomendação | Alternativa |
|---|---|---|---|
| D1 | Ritmo para 1º inverno aos 45–60 min | **4 ticks/s em 1x** (1 dia = 10 s, 1 ano = 60 min), começo no mês 0; balanceamento em ticks intacto | Dia mais longo em ticks (mexe em todo o balanceamento) |
| D2 | Base da branch `feature/worldgen` | `feature/2B-polish` (traz log de sessão, crônica, juice e NPCs) | Tag do candidato |
| D3 | Tamanho do mapa | 192 × 192 (384 m) | 128 × 128 (mais rápido, menos variedade) |
| D4 | Auto-continuação no degrau 0 | Curta (4 células) e depois "?" | Estilo AoE, sempre continua (anula o degrau 1) |
| D5 | Árvore no lugar da obra | Vira trabalho da obra, e a madeira volta | Proibir construir em cima |
| D6 | Teclas 1–4 (hoje: velocidade) | Grupos em Ctrl+1–5 / 1–5; velocidade em F1–F4 e Espaço | Grupos em F1–F5 |
| D7 | Crescimento da população | Famílias novas só com **casa vazia** + comida (Manor Lords) | Viajantes em datas fixas |
| D8 | Lobos | Só assustam (larga a carga e foge); grupo de 3 espanta | Lobo fere e o colono fica dias parado |
| D9 | Salão | Construção do ano 1–2 que dá CA e libera os decretos | Salão inicial, como hoje |
| D10 | Merge na `main` | Mesclar `overnight/2026-10-09` + `feature/2B-polish` agora, sem tag de playtest | `main` parada até a v0.3 |
| D11 | Boi | 1 boi selecionável que arrasta toras/pedra; sem custo de comida no MVP | Boi come feno (mais logística) |
| D12 | Chuva | Clima diário determinístico + frente de chuva garantida em 3–6 min; 2% por dia de chuva na pilha ao relento | Chuva só visual (perde a urgência diegética) |
