# Relatório de balanceamento (gerado pela CLI)

> `dotnet run --project src/Ironvale.Sim.Cli -- --balance-report docs/balance_report.md --years 3`
> Conteúdo `fnv64:5a02dc06eaba173e` · cenário `mvp_start` · 3 anos · seeds 42, 7, 123.
> Minutos = tempo de jogo a 1x (1 dia = 10 s, GDD v0.3 D1). Crises (GDD v0.2 §6): **1** lenha abaixo da demanda do inverno no
> outono · **2** sem ferramentas de reserva e condição média < 50% · **3** família com fome no inverno.
> Trajeto = % das horas de turno dos produtores gastas andando (ida e volta).

## Por cenário (média das seeds; crises = primeira ocorrência entre as seeds)

| Jogador | Sobreviveu (seeds) | Famílias no fim (mín.) | Partidas | Crise 1 lenha | Crise 2 ferramentas | Crise 3 fome no inverno | 1ª sugestão de decreto | Deadlock | Trajeto médio | Maior trecho sem acontecimento |
|---|---|---|---|---|---|---|---|---|---|---|
| passive | 0/3 | 0 (0) | 6 | ano 1 outono (~20 min) (3/3) | — | — | — | não | 0.0% | 89 dias (~14.8 min) |
| naive | 3/3 | 6 (6) | 0 | ano 1 outono (~20 min) (3/3) | ano 2 inverno (~100 min) (3/3) | — | ano 1 outono (~30 min) (3/3) | não | 39.1% | 89 dias (~14.8 min) |
| optimal | 3/3 | 6 (6) | 0 | ano 1 outono (~20 min) (3/3) | — | — | — | não | 14.0% | 89 dias (~14.8 min) |
| optimal_no_roads | 3/3 | 6 (6) | 0 | ano 1 outono (~20 min) (3/3) | ano 2 primavera (~62 min) (3/3) | — | — | não | 15.8% | 89 dias (~14.8 min) |

## Por seed

| Jogador | Seed | Famílias | Partidas (1ª) | Crise 1 | Crise 2 | Crise 3 | Comida no fim | Lenha no fim | Trajeto |
|---|---|---|---|---|---|---|---|---|---|
| passive | 42 | 0 | 6 (ano 1 outono (~24 min)) | ano 1 outono (~20 min) | — | — | 0 | 60 | 0.0% |
| passive | 7 | 0 | 6 (ano 1 outono (~24 min)) | ano 1 outono (~20 min) | — | — | 0 | 60 | 0.0% |
| passive | 123 | 0 | 6 (ano 1 outono (~24 min)) | ano 1 outono (~20 min) | — | — | 0 | 60 | 0.0% |
| naive | 42 | 6 | 0 (—) | ano 1 outono (~20 min) | ano 3 primavera (~116 min) | — | 85 | 125 | 39.1% |
| naive | 7 | 6 | 0 (—) | ano 1 outono (~20 min) | ano 2 inverno (~100 min) | — | 117 | 388 | 39.1% |
| naive | 123 | 6 | 0 (—) | ano 1 outono (~20 min) | ano 2 inverno (~100 min) | — | 133 | 441 | 39.1% |
| optimal | 42 | 6 | 0 (—) | ano 1 outono (~20 min) | — | — | 1832 | 392 | 14.0% |
| optimal | 7 | 6 | 0 (—) | ano 1 outono (~20 min) | — | — | 2503 | 303 | 14.0% |
| optimal | 123 | 6 | 0 (—) | ano 1 outono (~20 min) | — | — | 1834 | 400 | 14.1% |
| optimal_no_roads | 42 | 6 | 0 (—) | ano 1 outono (~20 min) | ano 2 primavera (~62 min) | — | 2557 | 448 | 15.8% |
| optimal_no_roads | 7 | 6 | 0 (—) | ano 1 outono (~20 min) | ano 2 primavera (~62 min) | — | 2045 | 400 | 15.8% |
| optimal_no_roads | 123 | 6 | 0 (—) | ano 1 outono (~20 min) | ano 2 primavera (~62 min) | — | 2146 | 394 | 15.8% |

## Mapa plano × mapa gerado (GDD v0.3 §8–§9)

> Mesmos jogadores e seeds; `mvp_start` (plano 64×64) × `mvp_generated` (gerado 192×192, a seed também gera o mapa).
> No gerado: o layout do 2A é deslocado para a clareira inicial e cada construção vai para o lugar válido mais perto
> (lenhador perto de árvores maduras, campo em solo fértil, pedreira no afloramento mais perto, sem atravessar penhascos);
> lenhadores derrubam árvores reais, a pedreira esvazia o afloramento, a colheita segue a fertilidade.

| Jogador | Sobreviveu plano | Sobreviveu gerado | Partidas plano | Partidas gerado | Crise 1 plano | Crise 1 gerado | Crise 2 plano | Crise 2 gerado | Trajeto plano | Trajeto gerado | Árvores derrubadas | Fertilidade dos campos | Pedra extraída da jazida |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| passive | 0/3 | 0/3 | 6 | 6 | ano 1 outono (~20 min) (3/3) | ano 1 outono (~20 min) (3/3) | — | — | 0.0% | 0.0% | 0 | — | 0 |
| naive | 3/3 | 1/3 | 0 | 4 | ano 1 outono (~20 min) (3/3) | ano 1 outono (~20 min) (2/3) | ano 2 inverno (~100 min) (3/3) | ano 2 verão (~71 min) (1/3) | 39.1% | 40.0% | 144 | 98% | 0 |
| optimal | 3/3 | 3/3 | 0 | 0 | ano 1 outono (~20 min) (3/3) | ano 1 outono (~20 min) (3/3) | — | ano 2 verão (~67 min) (3/3) | 14.0% | 21.6% | 168 | 98% | 117 |
| optimal_no_roads | 3/3 | 3/3 | 0 | 0 | ano 1 outono (~20 min) (3/3) | ano 1 outono (~20 min) (3/3) | ano 2 primavera (~62 min) (3/3) | ano 2 verão (~67 min) (3/3) | 15.8% | 23.6% | 168 | 98% | 127 |

### Mapa gerado, por seed

| Jogador | Seed | Famílias | Partidas (1ª) | Crise 1 | Crise 2 | Crise 3 | Comida no fim | Lenha no fim | Trajeto | Árvores derrubadas |
|---|---|---|---|---|---|---|---|---|---|---|
| passive | 42 | 0 | 6 (ano 1 outono (~24 min)) | ano 1 outono (~20 min) | — | — | 0 | 60 | 0.0% | 0 |
| passive | 7 | 0 | 6 (ano 1 outono (~24 min)) | ano 1 outono (~20 min) | — | — | 0 | 60 | 0.0% | 0 |
| passive | 123 | 0 | 6 (ano 1 outono (~24 min)) | ano 1 outono (~20 min) | — | — | 0 | 60 | 0.0% | 0 |
| naive | 42 | 6 | 0 (—) | ano 1 outono (~20 min) | ano 2 verão (~71 min) | ano 1 inverno (~41 min) | 357 | 0 | 34.2% | 355 |
| naive | 7 | 0 | 6 (ano 1 outono (~21 min)) | ano 1 outono (~20 min) | — | — | 0 | 60 | 50.5% | 33 |
| naive | 123 | 0 | 6 (ano 1 verão (~16 min)) | — | — | — | 0 | 60 | 35.4% | 45 |
| optimal | 42 | 6 | 0 (—) | ano 1 outono (~20 min) | ano 2 verão (~67 min) | — | 2849 | 390 | 19.2% | 157 |
| optimal | 7 | 6 | 0 (—) | ano 1 outono (~20 min) | ano 2 verão (~67 min) | — | 4102 | 458 | 19.0% | 165 |
| optimal | 123 | 6 | 0 (—) | ano 1 outono (~20 min) | ano 2 verão (~69 min) | — | 3478 | 483 | 26.7% | 183 |
| optimal_no_roads | 42 | 6 | 0 (—) | ano 1 outono (~20 min) | ano 2 verão (~67 min) | — | 3170 | 506 | 21.3% | 171 |
| optimal_no_roads | 7 | 6 | 0 (—) | ano 1 outono (~20 min) | ano 2 verão (~67 min) | — | 4116 | 395 | 20.5% | 161 |
| optimal_no_roads | 123 | 6 | 0 (—) | ano 1 outono (~20 min) | ano 2 verão (~76 min) | — | 3092 | 467 | 28.9% | 172 |
