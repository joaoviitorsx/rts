# Relatório de balanceamento (gerado pela CLI)

> `dotnet run --project src/Ironvale.Sim.Cli -- --balance-report docs/balance_report.md --years 3`
> Conteúdo `fnv64:8e2629e44bac4cae` · cenário `mvp_start` · 3 anos · seeds 42, 7, 123.
> Minutos = tempo de jogo a 1x (1 dia = 4 s). Crises (GDD v0.2 §6): **1** lenha abaixo da demanda do inverno no
> outono · **2** sem ferramentas de reserva e condição média < 50% · **3** família com fome no inverno.
> Trajeto = % das horas de turno dos produtores gastas andando (ida e volta).

## Por cenário (média das seeds; crises = primeira ocorrência entre as seeds)

| Jogador | Sobreviveu (seeds) | Famílias no fim (mín.) | Partidas | Crise 1 lenha | Crise 2 ferramentas | Crise 3 fome no inverno | Deadlock | Trajeto médio | Maior trecho sem acontecimento |
|---|---|---|---|---|---|---|---|---|---|
| passive | 0/3 | 0 (0) | 6 | — | — | — | não | 0.0% | 82 dias (~5.5 min) |
| naive | 0/3 | 0 (0) | 6 | — | — | — | não | 35.5% | 47 dias (~3.1 min) |
| optimal | 3/3 | 6 (6) | 0 | ano 1 outono (~8 min) (3/3) | — | — | não | 14.0% | 89 dias (~5.9 min) |
| optimal_no_roads | 3/3 | 6 (6) | 0 | ano 1 outono (~8 min) (3/3) | — | — | não | 15.9% | 89 dias (~5.9 min) |

## Por seed

| Jogador | Seed | Famílias | Partidas (1ª) | Crise 1 | Crise 2 | Crise 3 | Comida no fim | Lenha no fim | Trajeto |
|---|---|---|---|---|---|---|---|---|---|
| passive | 42 | 0 | 6 (ano 1 verão (~8 min)) | — | — | — | 0 | 60 | 0.0% |
| passive | 7 | 0 | 6 (ano 1 verão (~8 min)) | — | — | — | 0 | 60 | 0.0% |
| passive | 123 | 0 | 6 (ano 1 verão (~8 min)) | — | — | — | 0 | 60 | 0.0% |
| naive | 42 | 0 | 6 (ano 1 verão (~5 min)) | — | — | — | 0 | 60 | 35.5% |
| naive | 7 | 0 | 6 (ano 1 verão (~5 min)) | — | — | — | 0 | 60 | 35.5% |
| naive | 123 | 0 | 6 (ano 1 verão (~5 min)) | — | — | — | 0 | 60 | 35.5% |
| optimal | 42 | 6 | 0 (—) | ano 1 outono (~8 min) | — | — | 411 | 386 | 14.1% |
| optimal | 7 | 6 | 0 (—) | ano 1 outono (~8 min) | — | — | 4251 | 364 | 13.8% |
| optimal | 123 | 6 | 0 (—) | ano 1 outono (~8 min) | — | — | 3358 | 462 | 14.0% |
| optimal_no_roads | 42 | 6 | 0 (—) | ano 1 outono (~8 min) | — | — | 2258 | 416 | 15.9% |
| optimal_no_roads | 7 | 6 | 0 (—) | ano 1 outono (~8 min) | — | — | 1753 | 416 | 15.9% |
| optimal_no_roads | 123 | 6 | 0 (—) | ano 1 outono (~8 min) | — | — | 1960 | 418 | 15.9% |
