# Pesquisa: a abertura do Manor Lords e o "feijão com arroz bem feito"

> **Objetivo:** entender por que o começo do Manor Lords funciona, o que a crítica e os jogadores reclamam, e definir a abertura do nosso jogo (combinando com o começo RTS que decidimos).
> **Data:** 09/10/2026 · **Status:** referência para o `GDD_v0.3_abertura_rts.md` (texto do dono, salvo como recebido)

---

## 1. Como o Manor Lords começa

| Elemento | Como é |
|---|---|
| População | Um grupo de aldeões **sem casa** (acampados) |
| Animal | **Um único boi**, que puxa toras e cargas pesadas |
| Recursos | Algumas **pilhas pequenas ao ar livre** (ração e lenha), que **o mau tempo estraga** se não forem guardadas |
| Objetivo implícito | Sobreviver ao primeiro inverno |

**Ordem de construção típica (guias):**
1. **Depósito + celeiro**, cada um com uma família, para **tirar os suprimentos da chuva**.
2. **Acampamento de lenhadores** (toras para construir), longe de animais e arbustos de frutas.
3. **Cabana do lenhador** (lenha para o inverno), separada das toras de construção.
4. **Acampamento de caça** (grátis, dá carne e couro) e/ou coletor de frutas, com **trajetos curtos**.
5. **Casas em lotes grandes** (5 a 10), deixando algumas vazias, porque **novas famílias só chegam se houver casa livre**.
6. **Mercado** perto das casas (grátis): é onde as famílias buscam comida e lenha.
7. **Extensões no quintal** (galinheiro, horta) para comida no próximo inverno.

**No ano 2:** posto de comércio (única fonte de dinheiro), serraria (tábuas vendem bem), mais variedade de comida e escolha de **especialização** pela jazida rica do mapa (argila, pedra ou ferro).

---

## 2. O que funciona (o "feijão com arroz")

| O que | Por que funciona |
|---|---|
| **Urgência diegética no minuto 1** | Os suprimentos estão no chão e podem estragar. O primeiro objetivo nasce do mundo, não de um tutorial |
| **Começo humilde e físico** | "De um amontoado de tendas a uma cidade movimentada" é a satisfação central citada nas reviews |
| **Um boi só** | Logística escassa desde o início; mover toras pesadas é uma decisão |
| **Separar toras de lenha** | Ensina que o mesmo recurso (árvore) tem usos diferentes e concorrentes |
| **Construções grátis no início** (caça, mercado) | O jogador consegue agir antes de ter estoque |
| **Crescimento por casa livre** | Expansão é uma decisão: construir casa = convidar gente |
| **Quintal com extensões** | Pequenas decisões por casa que deixam a vila orgânica |
| **Variedade de comida → aprovação** | Incentivo positivo para diversificar, não só "ter comida" |
| **Distância importa** | Trajeto dos trabalhadores e do material e indústria perto da jazida mudam a produção |
| **Estradas curvas e lotes orgânicos** | Construir é prazeroso por si só; é o ponto mais elogiado |
| **Estações e inverno** | Dão o ritmo: preparar → sobreviver → crescer |
| **Jazidas ricas no mapa** | Dão o objetivo de médio prazo e a especialização |

Nas análises de reviews, o **city building central** é o tema mais citado, e o **início do jogo** é descrito como recompensador e viciante.

---

## 3. O que reclamam (onde podemos ser melhores)

| Problema | Impacto | Nossa resposta |
|---|---|---|
| **Tutorial fraco**, sistemas não explicados (iniciantes aprendem pelo YouTube) | Iniciantes travam | Objetivo sempre visível + tooltips causais + sugestão de decreto (já temos no 2A) |
| **Cadeias de produção e logística pouco claras** | Frustração | Painéis com causa ("parado: sem pedra"), % de trajeto e overlay de fluxo (UI_UX_guide) |
| **Estoques parados**, muito tempo rearranjando armazéns e mercados | Micro chato | Transporte com urgência por necessidade (P15) e, depois, delegação ao reeve |
| **Aldeões são "engrenagens sem rosto"** | Pouca ligação emocional | Famílias com nome, delegados com personalidade, balões de emoção, crônica |
| **Fim de jogo monótono, sem objetivo** | Abandono após algumas horas | Escada de delegação, instituições, sucessão: cada camada traz problemas novos |
| **Sem escala de UI** | Texto pequeno | Já temos (80–150% com piso de 12 px) |
| **Performance cai em cidades grandes** | ~10 FPS reportados | Simulação separada, LOD de simulação, MultiMesh (TDD) |
| **Não escolhe local de início** | Pode começar mal posicionado | Gerador de mundo garante clareira, água, floresta e pedra próximas |

---

## 4. A nossa abertura: Manor Lords + começo RTS

| Tempo | O que acontece | Inspiração |
|---|---|---|
| **0 min** | Bando de ~8 colonos numa clareira com **1 boi** e **pilhas de comida e lenha no chão, expostas à chuva** | ML: urgência diegética |
| **0–5 min** | **Controle direto**: selecionar colonos e mandar coletar árvores, pedras soltas e frutas; tudo vai para uma pilha. O boi arrasta toras | Pedido: começo RTS |
| **5–10 min** | **Fogueira + depósito coberto** para salvar os suprimentos da chuva (1º objetivo nascido do mundo). Tendas para dormir | ML: depósito primeiro |
| **10–15 min** | **Caça** (cervos e coelhos dão carne e couro) e coleta de frutas, com distância importando | ML: caça grátis |
| **15–25 min** | **Primeiras casas**: colonos viram famílias. Surge o lenhador e a **1ª delegação**: você para de mandar cortar árvore por árvore | Nossa tese: escada de delegação |
| **25–45 min** | Separação **toras (construção) × lenha (inverno)**; quintal com horta; preparação do inverno | ML: dois usos da árvore |
| **45–60 min** | **1º inverno** como prova; decretos e CA começam a aliviar | Nosso 2A |
| **Ano 2+** | Jazida rica (pedra, carvão, ferro) define a especialização; o boi vira recurso disputado | ML: especialização |

### Regras de ouro da abertura
1. **O primeiro objetivo nasce do mundo**, não de um texto: a comida no chão vai estragar.
2. **Agir no primeiro minuto** sem precisar de estoque: coletar à mão e caçar são grátis.
3. **Tudo físico:** nada aparece no estoque sem alguém (ou o boi) carregar.
4. **O micro do começo é proposital e temporário**: a primeira delegação, aos 15–25 min, tem que parecer alívio e conquista.
5. **Sempre um próximo passo visível** (cartão de objetivo), resolvendo a maior crítica ao Manor Lords.
6. **O mapa garante um bom começo**: clareira, água, floresta, pedra e caça a distâncias razoáveis.

### O que NÃO copiar
- O tutorial fraco e os sistemas sem explicação.
- Aldeões sem identidade.
- Ficar rearranjando armazéns: isso deve ser delegável.
- Falta de objetivo no fim de jogo.

---

## 5. Checklist para o agente (critérios de aceitação da abertura)

- [ ] Começa com ~8 colonos, 1 boi e pilhas de comida/lenha ao ar livre que estragam com chuva.
- [ ] Nos primeiros 60 s o jogador já deu uma ordem de coleta.
- [ ] O 1º objetivo ("salve os suprimentos da chuva") aparece por causa do mundo, não por roteiro.
- [ ] Coleta, caça e transporte são físicos (boi e colonos carregando).
- [ ] Toras e lenha são recursos separados.
- [ ] A 1ª delegação acontece entre 15 e 25 min e é apresentada como conquista.
- [ ] O 1º inverno chega por volta de 45–60 min e é sobrevivível por um jogador ingênuo.
- [ ] Nenhum período de mais de 2 min sem decisão relevante.
- [ ] Todo "parado" tem causa e solução explicadas.
- [ ] O gerador de mundo nunca cria um início sem clareira, água, floresta, pedra e caça por perto.

---

## Fontes
- [Manor Lords guide: How to survive your first two years — Epic Games Store](https://store.epicgames.com/news/manor-lords-guide-how-to-survive-your-first-two-years)
- [Manor Lords: Starting Strategies and Build Orders — GamerGuides](https://gamerguides.com/manor-lords/guide/basics/overview/starting-strategies-and-build-orders)
- [Manor Lords Review Summary — Vaporlens](https://vaporlens.app/app/1363080/manor_lords)
- [Manor Lords Early Access review — VideoGamer](https://videogamer.com/reviews/manor-lords-review)
- [Early Access Review: Manor Lords — Seasoned Gaming](https://seasonedgaming.com/2024/05/04/early-access-review-manor-lords-to-reign-or-refrain/)
