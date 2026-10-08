# GDD — Sociedade Autônoma (codinome: *Ironvale*)

> **Status:** Visão congelada · Design de sistemas em andamento
> **Versão:** 0.2 — 07/10/2026
> **Engine alvo:** Godot 4.7.x (3D low-poly, câmera ortográfica, pixel shader)
> **Plataforma alvo:** PC (Windows/Linux)

---

## 0. Como usar este documento

Este é um **documento vivo**. Cada seção tem um status:

| Status | Significado |
|---|---|
| 🔒 **Congelado** | Decidido. Só muda com justificativa forte e registro no changelog. |
| 🟡 **Em design** | Direção definida, detalhes abertos. |
| ⚪ **Aberto** | Ainda não decidido. Ver seção 13 (Decisões em aberto). |

Regra de ouro: **se uma feature não fortalece pelo menos um dos 4 pilares (seção 2), ela não entra.**

---

## 1. Visão 🔒

### 1.1 Frase-tese

> **Você não constrói uma cidade. Você constrói uma sociedade que aprende a funcionar sem você.**

### 1.2 Elevator pitch

Um *strategy city-builder* medieval incremental sobre construir uma sociedade autônoma. Você começa administrando famílias e termina governando cidades, instituições, rotas comerciais e territórios que aprenderam a funcionar sem sua intervenção direta.

**Tagline (marketing):** *Build a village. Teach it to survive. Watch it become a civilization.*

### 1.3 Fantasia do jogador

Você é um **Lorde** (não um deus). Começa perto do chão — famílias, estoques, primeiras casas — e, a cada salto de título (Lorde → Barão → Conde → Duque → Rei), **troca controle direto por alcance**. A satisfação central é olhar para algo que você fazia manualmente e perceber: *"isso agora acontece sozinho"*.

### 1.4 O problema de gênero que resolvemos

City builders tradicionais ficam **mais cansativos** conforme a cidade cresce (mais micro a cada hora). Aqui é o contrário: **quanto maior o domínio, menos decisões pequenas** e mais decisões de natureza política, estratégica e sistêmica.

> Referência de validação: o criador de *Banished* relatou exatamente essa frustração com SimCity 4 — cidades grandes viravam microgerenciamento lento e tedioso. Nosso jogo transforma essa dor em mecânica central.

### 1.5 O que o jogo **NÃO** é (anti-escopo)

- ❌ Não é um idle game (números não escalam sozinhos sem decisão).
- ❌ Não é um RTS estilo Age of Empires (guerra é custo econômico, não APM).
- ❌ Não é Dwarf Fortress (não simulamos cada órgão de cada pessoa).
- ❌ Não é "Manor Lords em pixel art" (o foco é **delegação**, não construção 3D detalhada).
- ❌ Sem multiplayer no escopo atual.
- ❌ Sem IA generativa no runtime.

---

## 2. Os 4 Pilares 🔒

| # | Pilar | Pergunta-teste para features |
|---|---|---|
| 1 | **Sociedade autônoma** | Isso faz famílias/instituições resolverem algo que antes era do jogador? |
| 2 | **Economia física** | Isso é produzido, armazenado, transportado e consumido de forma visível? |
| 3 | **Território conectado** | Isso exige integrar (estrada, mercado, administração), não só pintar o mapa? |
| 4 | **História emergente** | Isso gera consequências encadeadas que viram histórias da partida? |

---

## 3. Loops de jogo 🟡

### 3.1 Loop de crise → sistema (o loop-mestre)

```text
Problema aparece
  ↓
Jogador resolve manualmente
  ↓
Solução vira regra/política
  ↓
Regra é delegada (administrador, guilda, instituição)
  ↓
Sociedade cresce
  ↓
Crescimento gera problema de nível superior
  ↓ (volta ao topo, uma camada acima)
```

**Exemplo da cadeia de comida ao longo do jogo:**

| Escala | Problema de comida |
|---|---|
| Vila pequena | Produzir comida |
| Cidade | Distribuir comida |
| Metrópole | Preço da comida |
| Reino | Dependência regional de comida |

O recurso é o mesmo; **o tipo de problema muda**.

### 3.2 Loop de minuto a minuto (core loop — a definir no GDD v0.2)

```text
Observar (overlays, alertas, estoques)
  → Diagnosticar (por que o pão subiu?)
  → Agir (construir, designar, definir política)
  → Esperar/acelerar o tempo
  → Ver a consequência física no mapa
```

### 3.3 Loop de sessão (meta)

```text
Estação → Ano → Década → Sucessão (fim de geração)
```

---

## 4. Progressão de autoridade 🟡

### 4.1 Escada de delegação

O mesmo problema (ex.: madeira) atravessa o jogo inteiro; o que muda é a **interface** entre jogador e sociedade.

| Nível | Comando do jogador | Quem executa |
|---|---|---|
| 1. Manual | "3 famílias cortam madeira" | Jogador |
| 2. Meta | "Manter ≥ 300 de madeira" | Mestre Lenhador |
| 3. Instituição | "Guilda gerencia a indústria florestal" | Guilda da Madeira |
| 4. Regional | "Reserva regional = 1.000" | Administração da vila |
| 5. Política | "Reserva estratégica de 4 meses; exportar excedente" | Coroa / Ministério |

### 4.2 Fases × títulos

| Fase | Título | Jogador administra | Jogo passa a administrar |
|---|---|---|---|
| Acampamento | Lorde | famílias e trabalhos | tarefas individuais |
| Vila | Lorde | produção e estoque | famílias |
| Cidade | Barão | distritos e logística | empregos |
| Senhorio | Conde | comércio e políticas | produção local |
| Reino | Duque/Rei | território, diplomacia, guerra | cidades |
| Domínio | Rei | estratégia econômica | regiões |

### 4.3 Regra de design crítica

**Delegar nunca pode ser estritamente melhor.** Toda delegação troca algo:

| Ganha | Perde |
|---|---|
| eficiência, tempo do jogador | controle fino, previsibilidade |
| escala | poder político (instituições ganham voz) |
| arrecadação | controle de preços/impostos |

Sem esse trade-off, a delegação vira só "desbloqueio" e o jogo perde tensão.

---

## 5. Sistemas 🟡

### 5.1 População: modelo híbrido de famílias

**Unidade atômica = Família (Household)**, não o indivíduo.

```text
Família Halberg
├── Ocupação principal: Ferreiros
├── Gerações no assentamento: 4
├── Propriedades: casa, ferraria, 2 lotes
├── Prestígio: 63 · Lealdade: 81%
└── Necessidades: comida ✓ segurança ✓ religião ✓ tributação ✗
```

**Níveis de detalhe de simulação (Simulation LOD):**

| Nível | Quem | Como é simulado |
|---|---|---|
| Agregado | maioria das famílias | estatístico (contadores por família) |
| Nomeado | líderes, mestres, mercadores, generais, nobres | indivíduo completo (nome, traços, idade, história) |
| Visual | ~100–200 agentes na tela | sprites que *representam* a simulação |

> Referência: em *Victoria 3*, os **Pops** são os "átomos" da simulação — grupos demográficos que trabalham, pagam impostos e formam grupos de interesse políticos. Nossa família é uma versão menor e mais pessoal do mesmo princípio.

**Promoção de indivíduos:** um membro agregado vira "Nomeado" quando um evento o torna relevante (mestre de ofício, herói de guerra, líder de facção). Isso mantém o custo baixo e as histórias pessoais.

### 5.2 Instituições emergentes

Instituições **nascem de famílias**, não de menus.

```text
Johann Falk → Ferreiro → Mestre → Oficina Falk → Guilda dos Ferreiros → Guilda Metalúrgica de Ironvale
```

Quando Johann morre, **a instituição continua**.

Cada instituição tem: `Poder`, `Riqueza`, `Influência`, `Demandas`. Demandas não atendidas reduzem lealdade/cooperação; atendidas, aumentam a eficiência delegada.

> Política emerge da economia — não existe um "sistema político" separado.

### 5.3 Economia física

Tudo existe fisicamente: **produzido → armazenado → transportado → consumido**.

```text
Armazém → Carregadores → Carroça → Estrada → Fronteira → Destino
```

Consequências que precisam existir:
- Ponte destruída quebra cadeia.
- Bandidos aumentam custo de rota.
- Guerra distante gera inflação local (cadeia: ferro ↓ → ferramentas ↓ → produtividade agrícola ↓ → pão ↑ → insatisfação ↑).

**Salvaguarda obrigatória (anti-deadlock):** economias simuladas podem "travar" (ex.: ninguém tem dinheiro, ninguém compra, tudo para). Toda cadeia precisa de:
- fontes e sumidouros de moeda explícitos (tributo, salário da Coroa, comércio externo);
- produção de subsistência mínima garantida (família nunca fica 100% parada);
- telemetria de debug que detecte estoques/fluxos congelados.

> Esse risco é discutido abertamente pela comunidade de *Songs of Syx*: economias complexas demais com milhares de habitantes tendem a travar e quebrar a partida. Simplicidade deliberada é feature, não preguiça.

### 5.4 Logística e estradas

- Estradas evoluem pelo **uso**: trilha → caminho → estrada → estrada pavimentada → rota comercial.
- **Overlay de fluxo** mostra volume por recurso (largura/cor da linha).
- Distritos/vilas têm estoques próprios e dependem de conexões para trocar (inspiração: distritos de *Timberborn*).

### 5.5 Território: ocupar ≠ integrar

```text
VALE DE ROTH
Controle militar         82%
Controle administrativo  18%
Lealdade local           24%
Integração econômica      7%
```

Integrar exige: estrada, mercado, administração, segurança, comércio, migração. Território não integrado rende pouco e se revolta fácil.

**Especialização geográfica** por atributos de região (solo, floresta, pedra, ferro, rio, porto, altitude). Conquistar Blackridge pelo ferro deve ser uma decisão econômica, não "+1 território".

### 5.6 Delegação: o Administrador (IA de políticas)

O jogador **não dá ordens de construção** ao administrador; dá **diretrizes**:

```text
Especialização: Agrícola
Estoque mínimo: 6 meses
Exportação: Trigo > 500
Tributação: Moderada
Recrutamento: Somente defensivo
```

**Arquitetura sugerida: Utility AI.** Cada ação possível (construir celeiro, realocar família, vender excedente) recebe uma pontuação a partir de *curvas de resposta* sobre o estado do mundo; as políticas do jogador **são** os pesos e limiares dessas curvas. Vantagens:
- data-driven (ajustável sem recompilar);
- comportamento legível ("o administrador priorizou comida porque a reserva caiu para 40 dias");
- escala bem de NPC → vila → região.

> Referência: Dave Mark (Infinite Axis Utility System, GDC AI Summit 2013/2015) e o livro *Behavioral Mathematics for Game AI*.

**Requisito de UX:** o administrador precisa **explicar suas decisões** (log legível). Delegação sem transparência vira frustração.

### 5.7 Guerra como custo econômico

- Soldados **saem da população** (agricultores, artesãos...).
- Equipamento, comida e salário vêm da economia.
- Mortes nomeadas geram consequências sociais (família perde provedor, filho abandona ofício).
- Uma cidade rica não tem exército grande; tem **capacidade de sustentar** um.

### 5.8 Sucessão (o "prestige" narrativo)

Ao morrer o governante, começa uma nova geração. **Nada é resetado.** O herdeiro herda instituições, territórios, reputação, dívidas, rivalidades e famílias poderosas, e escolhe **até 3 legados** do antecessor:

```text
[ ] Reforma Agrária        +15% produtividade agrícola
[ ] Carta dos Mercadores   +1 rota comercial por cidade
[ ] Exército Profissional  milícias ganham XP mais rápido
```

### 5.9 Mundo vivo

Outras sociedades evoluem em paralelo (população, especialização, anexações), em simulação **mais abstrata** que a do jogador.

---

## 6. Arte e apresentação 🔒 (direção) / 🟡 (detalhes)

### 6.1 Direção: 3D low-poly + câmera ortográfica + pixel shader

Estilo conhecido como "3D pixel art" (referência principal: **t3ssel8r**). A cena é 3D low-poly, renderizada em **resolução interna baixa** e ampliada com *nearest neighbor*, com contornos e iluminação quantizada para parecer pixel art desenhada à mão.

**Por que essa escolha (vs. 2D pixel art puro):**

| Ganho | Custo |
|---|---|
| Iluminação, sombras, dia/noite e estações "de graça" | Risco técnico na estabilidade da câmera (pixel creep) |
| Um modelo serve para todas as direções (sem 4–8 sprites por objeto) | Pipeline 3D (Blender) + shaders |
| Terreno com relevo real (vales, colinas, rios) | Rotação livre da câmera fica limitada |
| Construções podem crescer/evoluir por partes | Zoom precisa ser discreto |

### 6.2 Regras de renderização

| Regra | Decisão |
|---|---|
| Projeção | **Ortográfica** obrigatória (snapping só funciona bem em ortho; em perspectiva pixels em profundidades diferentes "escorregam" em taxas diferentes) |
| Ângulo | Isométrico "dimétrico" (~30° de elevação, 45° de azimute) |
| Rotação | **4 ângulos fixos de 90°**, com transição rápida (rotação livre re-rasteriza a cena e gera cintilação) |
| Resolução interna | Base **640×360** (escala inteira para 1280×720, 1920×1080 ×3, 2560×1440 ×4) |
| Snapping | Câmera move em incrementos de 1 texel no espaço do mundo + compensação de subpixel no upscale (rolagem suave sem *pixel creep*) |
| Zoom | **Níveis discretos** (altera o tamanho ortográfico; a resolução interna fica fixa) |
| Contorno | Pós-processamento por profundidade + normal: linha externa (silhueta) escura, linha interna (vincos) clara |
| Iluminação | Toon/cel com 2–4 bandas; sombras duras |
| Texturas | Baixa resolução, filtro *nearest*, **densidade de texels consistente** (1 texel ≈ 1 pixel de tela no zoom padrão) |
| Paleta | Paleta limitada (32–48 cores) com quantização opcional no pós-processamento |
| UI e números | **Renderizados em resolução nativa**, fora do viewport pixelado (legibilidade de dados é crítica num jogo de simulação) |
| Overlays de fluxo | Desenhados no viewport pixelado (linhas "pixel"), rótulos em resolução nativa |

### 6.3 Direção estética

- Low-poly com formas legíveis à distância: telhados com cor por função (palha = moradia, telha = ofício, ardósia = administração).
- Construções evoluem por **partes** (cabana → casa → casa de pedra) — reforça visualmente o progresso da sociedade.
- Estações por parâmetros de shader (cor de grama/folhas, neve acumulando).
- Habitantes: modelos de ~100–300 tris, animação por **Vertex Animation Texture (VAT)** para multidões.
- **Prioridade visual nº 1 continua:** *o jogador precisa ver a economia funcionando* (carroças, carregadores, pilhas de estoque visíveis, estradas mudando).
- **Overlays obrigatórios:** logística/fluxo, necessidades, lealdade, integração territorial.

### 6.4 Pipeline de assets

```text
Blender (low-poly, UV em atlas de paleta)
  → glTF 2.0
  → Godot (material compartilhado de paleta + shader toon)
  → MultiMesh / chunks estáticos mesclados
```

Um **atlas de paleta** compartilhado (UVs apontando para quadrados de cor) permite que quase todo o jogo use **um único material**, reduzindo draw calls.

---

## 7. Arquitetura técnica 🟡

### 7.1 Princípio: Simulação separada da Visualização

```text
┌──────────── SIMULATION (dados puros, sem Nodes) ────────────┐
│ Households · Economy · Jobs · Logistics · Institutions ·    │
│ Regions · Policies/AI · Calendar                            │
│ Tick fixo (ex.: 10 ticks/s em velocidade 1x)                │
└───────────────┬─────────────────────────────────────────────┘
                │ eventos / snapshots (somente leitura)
┌───────────────▼─────────────────────────────────────────────┐
│ VIEW (Godot SceneTree)                                       │
│ SubViewport 3D 640×360 (terreno em chunks, edifícios,        │
│ MultiMeshInstance3D p/ agentes) → pós-processo (contorno,    │
│ paleta) → upscale nearest + subpixel                         │
│ UI/rótulos em resolução nativa por cima                      │
└──────────────────────────────────────────────────────────────┘
```

### 7.2 Decisões técnicas recomendadas

| Tema | Recomendação | Por quê |
|---|---|---|
| Estado da simulação | Classes/arrays puros, **não** Nodes | Testável, serializável, roda fora da main thread |
| Tick | Fixo e desacoplado do framerate; velocidades 1x/2x/4x = mais ticks | Reprodutibilidade |
| Aleatoriedade | RNG com *seed* por sistema | Bugs reproduzíveis, replays |
| Determinismo | Save → load deve produzir o mesmo futuro | Debug e testes (prática do *Factorio*) |
| Agendamento | Sistemas com frequências diferentes (necessidades: 1×/dia; preços: 1×/semana; política: 1×/mês) | Custo distribuído |
| Paralelismo | `WorkerThreadPool` só para cálculo de dados puros; **nunca** tocar a SceneTree fora da main thread | Nodes não são thread-safe |
| Renderização de multidões | `MultiMeshInstance3D` + animação VAT em vez de milhares de `CharacterBody3D`/esqueletos | Menos draw calls, nós e custo de animação |
| Cenário estático | Terreno e edifícios mesclados por *chunk* (ex.: 16×16 tiles), um material de paleta | Draw calls baixos mesmo com cidade grande |
| Custo de GPU | Resolução interna baixa reduz *fill rate*; gargalo esperado = draw calls e CPU da simulação | Direciona onde otimizar |
| Pathfinding | Grafo de estradas + flow fields/cache por destino; consultas distribuídas entre frames | Evitar picos |
| Conteúdo | Data-driven (Resources `.tres` ou JSON): recursos, edifícios, receitas, políticas | Balanceamento sem código |
| Save | Serialização versionada do estado da simulação desde o dia 1 | Retrofit de save é caro |
| Testes | Testes unitários da simulação headless + "soak test" (simular 50 anos sem render) | Detectar deadlocks/inflação |

### 7.3 Ferramentas internas (desde o protótipo)

- Painel de debug com fluxos de cada recurso (produção/consumo/estoque por tick).
- Botão "avançar N anos" headless.
- Log de decisões do Administrador.
- Gráficos de séries temporais (preço, população, estoque).

---

## 8. MVP / Protótipo vertical 🟡

### 8.1 A única pergunta que o MVP responde

> **É divertido construir uma pequena sociedade e vê-la, progressivamente, assumir responsabilidades que eram minhas?**

### 8.2 Escopo do MVP

| Categoria | Conteúdo |
|---|---|
| Escala | 10–20 famílias, 1 região |
| Recursos | madeira, pedra, comida, ferramentas, moedas |
| Edifícios | casa, lenhador, pedreira, campo, celeiro, mercado, ferreiro |
| Sistemas | empregos, transporte físico, estoque mín/máx, necessidades familiares, inverno |
| Delegação | 1 "Administrador da Vila" com 3–4 políticas |
| Fora | guerra, diplomacia, múltiplas regiões, sucessão, instituições emergentes |

### 8.3 Critérios de sucesso (mensuráveis)

- [ ] Jogador passa por **≥ 2 crises** (ex.: inverno, falta de ferramentas) em 60 min.
- [ ] Em ~60 min, o jogador **delegou ≥ 3 responsabilidades** que fazia manualmente.
- [ ] Após delegar, a vila **sobrevive ao inverno sem intervenção**.
- [ ] Playtesters (mín. 5) descrevem espontaneamente a sensação de *"não preciso mais cuidar disso"*.
- [ ] Soak test de 50 anos simulados **sem deadlock econômico**.

### 8.4 Critério de falha (pivot)

Se a delegação parecer **"o jogo jogando por mim"** (tédio) em vez de **"conquista"**, revisar o trade-off da seção 4.3 antes de adicionar qualquer conteúdo.

---

## 9. Roadmap por fases (com *gates*) 🟡

| Fase | Entrega | Gate para avançar |
|---|---|---|
| **F0 — Papel** | GDD v0.2: core loop + primeiras 5h + economia em planilha | Planilha da economia fecha sem deadlock |
| **F0.5 — Spike visual** | Cena de teste: terreno + 10 casas + 200 agentes MultiMesh, câmera ortho com snapping, 4 rotações, zoom discreto, contorno | Rolagem sem *pixel creep*; 60 fps num PC modesto; "parece pixel art" num GIF |
| **F1 — Simulação headless** | Sim sem gráficos, testes, soak test | 50 anos estáveis; console mostra fluxos |
| **F2 — Protótipo cinza** | Visual com quadrados, UI crua, 1 administrador | 5 playtests; critérios 8.3 |
| **F3 — Vertical slice** | Arte final de 1 bioma, 1 estação completa, overlays | Trailer/GIF que comunica a tese em 30s |
| **F4 — Camada 2** | Múltiplos distritos, logística entre eles, instituições emergentes | Delegação nível 3 funcionando |
| **F5 — Camada 3** | Regiões, integração territorial, sucessão | Partida de 10h coerente |
| **F6 — Mundo** | Outras sociedades, comércio externo, guerra | — |

> Contexto de escopo: *Banished* (1 dev) levou cerca de 3 anos; *Songs of Syx* (1 dev) está em desenvolvimento desde 2014. Gates existem para não construirmos o andar 5 antes de saber se o térreo é divertido.

---

## 10. Riscos e mitigação

| Risco | Impacto | Mitigação |
|---|---|---|
| Escopo infinito (feature creep) | Projeto nunca termina | Pilares como filtro + gates por fase |
| Delegação = tédio | Mata a tese | Trade-offs (4.3) + crises de nível superior |
| Deadlock/colapso econômico | Partidas quebram | Fontes/sumidouros explícitos + soak tests |
| Performance com população grande | Jogo trava | Sim separada, LOD de simulação, MultiMesh, tick escalonado |
| Ilegibilidade ("por que isso aconteceu?") | Frustração | Overlays, log do administrador, tooltips causais |
| Arte consumir tempo demais | Atraso | Protótipo cinza até F3; paleta e atlas definidos cedo |
| Câmera 3D pixel instável (cintilação, *pixel creep*) | Visual "quebrado" | Spike F0.5 antes de qualquer arte; ortho + snapping + 4 ângulos fixos |
| Contornos ruidosos em cenas densas | Poluição visual | Limiar de profundidade ajustável; contorno interno opcional por material |
| Save quebra a cada versão | Retrabalho | Serialização versionada desde F1 |

---

## 11. Métricas de design (a calibrar)

- Tempo até a primeira delegação: alvo **15–25 min**.
- Decisões manuais/minuto: **deve cair** ao longo da partida; decisões estratégicas/hora devem **subir**.
- Frequência de crises: 1 crise relevante a cada ~20–30 min de jogo.

---

## 12. Glossário

| Termo | Definição |
|---|---|
| Família (Household) | Unidade atômica da população |
| Nomeado | Indivíduo promovido a simulação completa |
| Política | Regra/diretriz delegável (limiar, prioridade, proibição) |
| Instituição | Entidade persistente nascida de famílias (oficina, guilda, ordem) |
| Integração | Grau em que um território está conectado à sociedade |
| Legado | Bônus herdado na sucessão |

---

## 13. Decisões em aberto ⚪

| # | Decisão | Opções | Recomendação inicial |
|---|---|---|---|
| D1 | Linguagem do núcleo da simulação | GDScript · C# · GDExtension (C++/Rust) | **C#** para a simulação (performance, tipagem, testes com xUnit) e GDScript ou C# na view. Revisar se exportação web for requisito. |
| D2 | Tempo | Tempo real com pausa · turnos sazonais | Tempo real com pausa e velocidades |
| D3 | Grid | Tile grid · livre | Tile grid 3D com altura (simplifica logística, pathfinding e construção sobre relevo) |
| D7 | Resolução interna | 480×270 · 640×360 | 640×360 (mais detalhe para cidades densas); validar no spike |
| D8 | Animação de multidão | Esqueleto · VAT · billboards | VAT; validar no spike |
| D4 | Moeda no MVP | Economia de troca · moeda desde o início | Moeda simples desde o início (base para impostos e comércio) |
| D5 | Unidade de tempo | 1 dia = X segundos | Definir no GDD v0.2 junto com o ritmo da 1ª hora |
| D6 | Nome final | — | Manter codinome até F3 |

---

## 14. Referências

### Jogos (e o que estudar em cada um)
- **Banished** — população como recurso real; escopo de 1 dev. Ler o devlog da Shining Rock.
- **Songs of Syx** — escala, automação de tarefas rotineiras, simulação econômica simples por design.
- **Victoria 3** — Pops e grupos de interesse emergindo da economia (dev diaries #1 e #57).
- **Factorio** — sensação de automação; determinismo e save/load (Friday Facts #76, #270).
- **Manor Lords** — regiões especializadas e comércio.
- **Timberborn** — distritos com estoques próprios e conexões.
- **Foundation** — crescimento orgânico por zonas.
- **Against the Storm** — pressão crescente com a expansão.
- **Farthest Frontier** — recursos físicos e biomas.
- **RimWorld / Dwarf Fortress** — histórias emergentes.

### Artigos e técnicas
- Victoria 3 Dev Diary #57 — https://www.paradoxinteractive.com/games/victoria-3/news/dev-diary-57-the-journey-so-far
- Factorio FFF #76 (lockstep/MP) — https://factorio.com/blog/post/fff-76
- Factorio FFF #270 (save/load determinístico) — https://cf-www.factorio.com/blog/post/fff-270
- Entrevista Banished (micro em cidades grandes) — https://primagames.com/?p=311490
- Utility AI (visão geral) — https://en.wikipedia.org/wiki/Utility_system
- IAUS (Dave Mark) — https://gameai.com/iaus.php
- Godot: MultiMeshInstance2D — https://docs.godotengine.org/en/stable/classes/class_multimeshinstance2d.html
- Texel Splatting (por que ortho + snapping funciona) — https://arxiv.org/abs/2603.14587
- Shader de contorno 3D pixel art (Godot 4, MIT) — https://godotshaders.com/shader/3d-pixel-art-outline-highlight-post-processing-shader/
- Demo 3D pixel art em Godot — https://github.com/leopeltola/Godot-3d-pixelart-demo
- Câmera 3D pixel art em Godot (snapping) — https://github.com/BradFitz66/Godot-3D-pixel-art-camera
- Discussão de pixel creep em Godot — https://godotforums.org/d/36180-subpixel-snapping-in-a-3d-pixel-art-game
- Referência visual: t3ssel8r (YouTube) — "3D pixel art rendering"
- Godot: Thread / thread safety — https://docs.godotengine.org/en/latest/classes/class_thread.html
- Godot: NavigationServer — https://docs.godotengine.org/en/stable/tutorials/navigation/navigation_using_navigationservers.html

---

## 15. Próximos documentos

1. **GDD v0.2 — Core Loop & Primeiras 5 Horas**: o que o jogador faz em 5 min, 30 min, 1h, 3h, 5h; árvore de progressão; primeira delegação.
2. **Economy Sheet v0.1**: planilha com taxas de produção/consumo do MVP, simulando 10 anos para validar equilíbrio antes do código.
3. **TDD v0.1 (Technical Design Doc)**: modelo de dados da simulação, tick scheduler, formato de save, estrutura de pastas do projeto Godot.

---

## Changelog
- **v0.2 (07/10/2026)** — Direção de arte trocada para 3D low-poly + câmera ortográfica + pixel shader; regras de renderização, pipeline de assets, fase F0.5 (spike visual), novos riscos e decisões D7–D8. Core loop detalhado em `GDD_v0.2_core_loop_5h.md`.
- **v0.1 (07/10/2026)** — Consolidação da visão, pilares, progressão de autoridade, sistemas, arquitetura, MVP, roadmap com gates, riscos e decisões em aberto.
