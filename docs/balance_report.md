# Relatório de balanceamento (gerado pela CLI)

> `dotnet run --project src/Ironvale.Sim.Cli -- --balance-report docs/balance_report.md --years 3`
> Conteúdo `fnv64:35e52185f0e31b41` · cenário `mvp_start` · 3 anos · seeds 42, 7, 123.
> Minutos = tempo de jogo a 1x (1 dia = 4 s). Crises (GDD v0.2 §6): **1** lenha abaixo da demanda do inverno no
> outono · **2** sem ferramentas de reserva e condição média < 50% · **3** família com fome no inverno.
> Trajeto = % das horas de turno dos produtores gastas andando (ida e volta).

## Por cenário (média das seeds; crises = primeira ocorrência entre as seeds)

| Jogador | Sobreviveu (seeds) | Famílias no fim (mín.) | Partidas | Crise 1 lenha | Crise 2 ferramentas | Crise 3 fome no inverno | 1ª sugestão de decreto | Deadlock | Trajeto médio | Maior trecho sem acontecimento |
|---|---|---|---|---|---|---|---|---|---|---|
| passive | 0/3 | 0 (0) | 6 | ano 1 outono (~8 min) (3/3) | — | — | — | não | 0.0% | 89 dias (~5.9 min) |
| naive | 3/3 | 6 (6) | 0 | ano 1 outono (~8 min) (3/3) | ano 2 inverno (~40 min) (3/3) | — | ano 1 outono (~12 min) (3/3) | não | 39.1% | 89 dias (~5.9 min) |
| optimal | 3/3 | 6 (6) | 0 | ano 1 outono (~8 min) (3/3) | — | — | — | não | 14.0% | 89 dias (~5.9 min) |
| optimal_no_roads | 3/3 | 6 (6) | 0 | ano 1 outono (~8 min) (3/3) | ano 2 primavera (~25 min) (3/3) | — | — | não | 15.8% | 89 dias (~5.9 min) |

## Por seed

| Jogador | Seed | Famílias | Partidas (1ª) | Crise 1 | Crise 2 | Crise 3 | Comida no fim | Lenha no fim | Trajeto |
|---|---|---|---|---|---|---|---|---|---|
| passive | 42 | 0 | 6 (ano 1 outono (~9 min)) | ano 1 outono (~8 min) | — | — | 0 | 60 | 0.0% |
| passive | 7 | 0 | 6 (ano 1 outono (~9 min)) | ano 1 outono (~8 min) | — | — | 0 | 60 | 0.0% |
| passive | 123 | 0 | 6 (ano 1 outono (~9 min)) | ano 1 outono (~8 min) | — | — | 0 | 60 | 0.0% |
| naive | 42 | 6 | 0 (—) | ano 1 outono (~8 min) | ano 3 primavera (~47 min) | — | 85 | 125 | 39.1% |
| naive | 7 | 6 | 0 (—) | ano 1 outono (~8 min) | ano 2 inverno (~40 min) | — | 117 | 388 | 39.1% |
| naive | 123 | 6 | 0 (—) | ano 1 outono (~8 min) | ano 2 inverno (~40 min) | — | 133 | 441 | 39.1% |
| optimal | 42 | 6 | 0 (—) | ano 1 outono (~8 min) | — | — | 1832 | 392 | 14.0% |
| optimal | 7 | 6 | 0 (—) | ano 1 outono (~8 min) | — | — | 2503 | 303 | 14.0% |
| optimal | 123 | 6 | 0 (—) | ano 1 outono (~8 min) | — | — | 1834 | 400 | 14.1% |
| optimal_no_roads | 42 | 6 | 0 (—) | ano 1 outono (~8 min) | ano 2 primavera (~25 min) | — | 2557 | 448 | 15.8% |
| optimal_no_roads | 7 | 6 | 0 (—) | ano 1 outono (~8 min) | ano 2 primavera (~25 min) | — | 2045 | 400 | 15.8% |
| optimal_no_roads | 123 | 6 | 0 (—) | ano 1 outono (~8 min) | ano 2 primavera (~25 min) | — | 2146 | 394 | 15.8% |
