# GDD v0.2 — Core Loop & Primeiras 5 Horas

> **Depende de:** `GDD_sociedade_autonoma.md` (visão, pilares, sistemas)
> **Status:** 🟡 Em design · v0.1 — 07/10/2026
> **Objetivo:** transformar a visão em decisões concretas do que o jogador **faz**, **vê** e **sente** em cada momento das primeiras 5 horas.

---

## 1. Ritmo de tempo (resolve D5) 🟡

| Unidade | Duração em 1x | Observação |
|---|---|---|
| 1 tick | 0,1 s | 10 ticks/s |
| 1 dia | 4 s | |
| 1 mês | 30 dias = 2 min | |
| 1 estação | 3 meses = 6 min | |
| 1 ano | 4 estações = **24 min** | |

**Velocidades:** pausa · 1x · 2x · 4x · 8x (8x só libera após a primeira delegação — recompensa por automatizar).

**Consequência:** as 5 primeiras horas cobrem cerca de **18–30 anos de jogo**, dependendo da velocidade. Isso coloca a **primeira sucessão perto do fim da hora 5** — um marco natural.

> A justificativa de 24 min/ano em 1x: o inverno precisa ser sentido como ameaça nas primeiras sessões. Se o ano for curto demais, o jogador não tem tempo de se preparar; se for longo demais, a primeira crise demora a chegar.

---

## 2. Os três loops 🟡

### 2.1 Micro-loop (10–60 s) — "governar o instante"

```text
Ler sinais (alertas, cor dos estoques, agentes parados)
  → Inspecionar (clicar família/edifício/estoque)
  → Agir (construir, designar, ajustar limite)
  → Avançar o tempo
  → Ver a consequência fisicamente (carroça, pilha, fumaça da forja)
```

### 2.2 Meso-loop (5–20 min) — "resolver um problema"

```text
Crise aparece (falta de X)
  → Jogador resolve manualmente
  → Jogador repete a mesma solução 2–3 vezes
  → Jogo oferece: "Transformar isso em política?"
  → Jogador delega (custa Capacidade Administrativa)
  → A crise deixa de exigir atenção
```

### 2.3 Macro-loop (1–2 h) — "subir de camada"

```text
Delegações acumulam
  → Capacidade Administrativa vira o gargalo
  → Jogador investe em administração (escrivão, salão, mestres)
  → Desbloqueia um novo tipo de problema (distribuição, preço, instituições)
  → Título sobe (Lorde → Barão)
```

---

## 3. Duas mecânicas novas que sustentam o incremental

### 3.1 Capacidade Administrativa (CA)

A moeda **da delegação**. Sem ela, delegar seria grátis e o jogo se resolveria sozinho.

| Aspecto | Regra |
|---|---|
| O que é | Quanto da sociedade você consegue "governar à distância" |
| Fontes | Salão do Lorde (base), Escrivão (família alfabetizada), Capela (registros), Mestres de Ofício |
| Custo | Cada política ativa consome CA continuamente |
| Excesso de políticas | Se CA usada > CA disponível: políticas executam com atraso e erro (o administrador "se perde") |
| Por que funciona | Cria um **segundo eixo de crescimento**: não é só produzir mais, é **governar mais** |

**Exemplo de custo (a calibrar na planilha):**

| Política | CA |
|---|---|
| Reserva mínima de comida | 2 |
| Faixa de estoque (madeira/pedra) | 1 por recurso |
| Produção condicional (ferramentas) | 2 |
| Prioridade de empregos | 3 |
| Mestre de Ofício (delega um setor inteiro) | 5 |

### 3.2 "O jogo aprende com você" (delegação por observação)

Em vez de abrir um menu de políticas vazio, o jogo **observa ações repetidas** e propõe institucionalizá-las:

```text
Você mudou 3 famílias para lenhadoras quando a madeira caiu abaixo de ~80.
→ [Criar política] "Manter madeira acima de 80"   (custo: 1 CA)
```

Isso ensina o sistema de políticas **sem tutorial** e faz a delegação parecer uma **conquista do jogador**, não um desbloqueio arbitrário.

**Requisito:** toda sugestão mostra (1) o que vai acontecer, (2) o custo em CA, (3) o que você perde de controle.

---

## 4. Linha do tempo das primeiras 5 horas

Cada bloco tem: o que o jogador faz, o que é novo, a crise, a delegação conquistada e a **emoção-alvo**.

### 4.1 Minutos 0–5 — Chegada

| | |
|---|---|
| Situação | 6 famílias, uma carroça com suprimentos, uma clareira junto a um rio, fim da primavera |
| Jogador faz | Escolhe local do Salão; posiciona 3 abrigos; designa famílias a coletar madeira e frutos |
| Novo | Câmera, construção, designação de família, estoque físico (pilha da carroça) |
| Crise | Nenhuma — só um **relógio visível do inverno** |
| Emoção | *"Somos poucos e frágeis."* |

**Regra de onboarding:** até o minuto 5, o jogador usa no máximo 4 verbos: construir, designar, inspecionar, acelerar.

### 4.2 Minutos 5–30 — Primeiro verão e outono

| | |
|---|---|
| Jogador faz | Lenhador, campo, celeiro; primeira estrada (trilha surge pelo uso) |
| Novo | Necessidades da família (comida, abrigo, calor); carregadores; estações |
| Crise 1 | Primeiro **outono com estoque baixo de lenha** (falta calor para o inverno) |
| Delegação | Primeira sugestão: *"Manter lenha acima de X"* → **1ª política (~15–25 min)** |
| Emoção | *"Eu resolvi isso. E agora não preciso mais olhar."* |

### 4.3 Minutos 30–60 — Primeiro inverno

| | |
|---|---|
| Jogador faz | Gerencia o inverno; constrói ferreiro; descobre que ferramentas se desgastam |
| Novo | Ferramentas (consumo e produtividade), moeda simples, mercado |
| Crise 2 | **Ferramentas acabando** → produtividade cai em cadeia (lenha ↓, colheita ↓) |
| Delegação | *"Produzir ferramentas se estoque < X"* e *"Reserva mínima de comida = N dias"* |
| Marco | **Sobreviver ao 1º inverno** → libera velocidade 8x |
| Emoção | *"A vila aguentou. Algumas coisas já se cuidam sozinhas."* |

> **Este é o momento de verdade do MVP** (seção 8.3 do GDD). Se aqui o jogador não sentir a delegação como conquista, o resto não importa.

### 4.4 Horas 1–2 — Vila (10–20 famílias)

| | |
|---|---|
| Jogador faz | Expande com imigrantes; pedreira; casas de madeira → pedra; primeiro escrivão |
| Novo | **Capacidade Administrativa** visível; migração (famílias chegam se a vila prospera) |
| Crise 3 | **CA estoura** — muitas políticas, administrador erra (comida estragando, lenha em excesso) |
| Delegação | **Administrador da Vila** — um painel que agrupa políticas com prioridades |
| Emoção | *"Governar também custa. Preciso de gente para governar."* |

### 4.5 Horas 2–3 — Famílias com nome e peso

| | |
|---|---|
| Jogador faz | Lida com famílias que se destacam; nomeia o primeiro **Mestre de Ofício** |
| Novo | Família Nomeada (prestígio, lealdade); Mestre de Ofício (delega um setor inteiro) |
| Crise 4 | **Problema de distribuição**: comida existe, mas não chega às casas distantes do celeiro |
| Delegação | Mestre Lenhador / Mestre Ferreiro assumem seus setores |
| Trade-off | Mestres pedem algo: menos imposto, prioridade de matéria-prima, prestígio |
| Emoção | *"Essas pessoas importam — e começam a querer coisas."* |

### 4.6 Horas 3–4 — Segundo distrito

| | |
|---|---|
| Jogador faz | Funda um segundo núcleo (vila de mineração/pedreira); conecta por estrada |
| Novo | Estoques por distrito; carroças entre distritos; overlay de fluxo |
| Crise 5 | **Estrada única congestiona** ou ponte cai em cheia de primavera |
| Delegação | Política de transferência entre distritos ("enviar excedente acima de X") |
| Marco | Título sobe para **Barão** |
| Emoção | *"Agora vejo fluxos, não casas."* |

### 4.7 Horas 4–5 — Primeira instituição e fim da geração

| | |
|---|---|
| Jogador faz | Uma oficina de família vira **instituição** (ex.: Oficina Falk); decide atender ou recusar suas demandas |
| Novo | Instituições com poder/riqueza/demandas; envelhecimento do Lorde visível |
| Crise 6 | Instituição exige algo que conflita com a reserva da vila (ex.: exportar ferramentas) |
| Marco | **Morte do Lorde** → primeira **Sucessão** → escolha de até 3 legados |
| Emoção | *"A vila é maior do que eu. Ela vai continuar."* |

---

## 5. Árvore de progressão (desbloqueios por condição, não por pontos) 🟡

Nada é comprado com "pontos de pesquisa". Desbloqueios acontecem quando a **sociedade alcança uma condição** — isso reforça que é ela quem evolui.

```text
[Salão do Lorde] ─────────────────────────────────────────────┐
   │                                                          │
   ├─ Lenhador / Coletor / Campo / Celeiro   (início)         │
   │                                                          │
   ├─ Ferreiro ← condição: 1º outono concluído                │
   │     └─ Política "produção condicional"                   │
   │                                                          │
   ├─ Mercado + Moeda ← condição: 8 famílias                  │
   │                                                          │
   ├─ Escrivão ← condição: 1º inverno sobrevivido             │
   │     └─ Capacidade Administrativa visível                 │
   │           └─ Administrador da Vila ← CA ≥ 6              │
   │                                                          │
   ├─ Pedreira / Casa de pedra ← 12 famílias                  │
   │                                                          │
   ├─ Mestre de Ofício ← família com prestígio ≥ 50 num setor │
   │                                                          │
   ├─ Segundo distrito ← 20 famílias + estrada até recurso    │
   │     └─ Overlay de fluxo + transferências                 │
   │           └─ Título: Barão                               │
   │                                                          │
   └─ Instituição ← Mestre por ≥ 10 anos + oficina própria    │
         └─ Sucessão (morte do Lorde) ────────────────────────┘
```

---

## 6. Catálogo de crises (primeiras 5h)

| # | Crise | Gatilho | Sistema que ensina | Solução manual | Solução delegada |
|---|---|---|---|---|---|
| 1 | Falta de lenha | Outono + estoque < demanda de inverno | Estações, estoque | Mais lenhadores | Faixa de estoque |
| 2 | Ferramentas acabando | Desgaste acumulado | Cadeia produtiva | Forjar manualmente | Produção condicional |
| 3 | Fome de inverno | Colheita fraca | Reserva de comida | Racionar / caçar | Reserva mínima em dias |
| 4 | CA estourada | Políticas > capacidade | Custo de governar | Desligar políticas | Escrivão / Administrador |
| 5 | Distribuição falha | Casas longe do celeiro | Logística física | Celeiro novo | Mestre / transferência |
| 6 | Estrada/ponte | Cheia de primavera | Infraestrutura | Reconstruir | Rota alternativa |
| 7 | Demanda de instituição | Poder da oficina | Política emergente | Negociar | Carta/privilégio |

**Regras do sistema de crises:**
- Nunca 2 crises **novas** ao mesmo tempo nas primeiras 2h.
- Toda crise é **anunciada antes** (relógio, previsão, alerta) — sem surpresas injustas.
- Toda crise tem **causa visível no mapa** (pilha vazia, forja apagada, ponte quebrada).
- Falha é **branda**: famílias vão embora, produtividade cai; *game over* só se a população chegar a zero.

---

## 7. Loop de simulação (frequências) 🟡

| Frequência | Sistemas |
|---|---|
| A cada tick (0,1 s) | Movimento de agentes, carregamento/descarga |
| A cada hora de jogo | Atribuição de tarefas, consumo em edifícios |
| Diário | Necessidades das famílias, consumo de comida/lenha, desgaste de ferramentas |
| Semanal | Preços do mercado, migração (avaliação) |
| Mensal | Avaliação das políticas (Utility AI do administrador), sugestões de política, CA |
| Sazonal | Colheita, clima, eventos de crise |
| Anual | Envelhecimento, nascimentos/mortes, prestígio, promoção de Nomeados |

Ordem dentro de um passo: **produção → transporte → consumo → necessidades → decisões**. Ordem fixa = determinismo.

---

## 8. Verbos do jogador por fase

| Fase | Verbos principais | Verbos que somem/diminuem |
|---|---|---|
| Acampamento | construir, designar, inspecionar | — |
| Vila | definir limite, aceitar política | designar família individual |
| Vila madura | priorizar, nomear mestre | ajustar limites individuais |
| Dois distritos | conectar, transferir, negociar | construir edifícios de produção básicos |
| Instituição | conceder, recusar, suceder | gerenciar setores |

**Métrica-chave:** número de cliques por minuto **cai** ao longo das 5h, enquanto o tempo gasto lendo painéis e overlays **sobe**.

---

## 9. UI necessária para as 5 horas (mínimo)

| Elemento | Quando aparece |
|---|---|
| Barra de recursos com tendência (↑↓) | 0 min |
| Relógio de estação e previsão do inverno | 0 min |
| Painel de família (necessidades, ofício) | 0 min |
| Cartão de sugestão de política | ~15 min |
| Medidor de Capacidade Administrativa | ~1h |
| Painel do Administrador + log de decisões | ~1h |
| Overlay de fluxo | ~3h |
| Painel de instituição (poder, demandas) | ~4h |
| Tela de sucessão / legados | ~5h |

---

## 10. Critérios de validação deste design

- [ ] Primeira política aceita entre **15 e 25 min** em ≥ 4 de 5 playtests.
- [ ] Jogador explica com as próprias palavras por que a crise 2 aconteceu.
- [ ] Após 1h, ≥ 3 políticas ativas e vila sobrevive ao inverno sem intervenção.
- [ ] Ninguém relata "não sei o que fazer agora" por mais de 2 min seguidos.
- [ ] Ninguém relata "o jogo está jogando sozinho" como algo negativo.

---

## 11. Decisões em aberto

| # | Decisão | Recomendação |
|---|---|---|
| C1 | Sugestão de política: automática ou só quando o jogador abre o painel? | Automática, discreta, descartável |
| C2 | CA é número único ou por área (comida, produção, logística)? | Número único no MVP |
| C3 | Lorde morre por idade fixa ou probabilidade? | Probabilidade crescente a partir de ~55 anos, com aviso |
| C4 | Sucessão encerra o capítulo (salvar e "Capítulo 2") ou é contínua? | Contínua, com tela de legado |
| C5 | Quantas famílias iniciais? | 6 (testar 4–8 na planilha) |

---

## 12. Próximo passo

**Economy Sheet v0.1** — planilha com:
- taxas de produção/consumo por edifício e por família;
- desgaste de ferramentas; consumo de lenha por estação;
- custo de CA das políticas;
- simulação de 10 anos com 6 famílias iniciais, verificando se as crises 1–3 aparecem **quando** este documento diz que devem aparecer.

Depois dela: **Spike visual (F0.5)** em paralelo com o **TDD v0.1**.
