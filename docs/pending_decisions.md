# Decisões pendentes (para o dono revisar)

> Registradas durante o trabalho autônomo. Cada uma: contexto, opções, escolha e motivo. A escolha é sempre a mais
> simples e reversível; quando possível fica atrás de um parâmetro em `godot/data/balance.json`.

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
- **Termos:** o painel ainda se chama "Políticas"; a troca para decreto/reeve é feita no item 6 junto com o `UI_UX_guide.md`
  (decisão registrada). As linhas novas do log já dizem "Decreto".

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
- Pedreira + ferreiro + decretos "pedra entre 20 e 40" e "ferramentas entre 8 e 12". O `--opening` de dev no jogo agora usa
  o mesmo jogador (continua jogando durante `--days`).

