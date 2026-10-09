# Relatório de balanceamento (gerado pela CLI)

> `dotnet run --project src/Ironvale.Sim.Cli -- --balance-report docs/balance_report.md --years 3`
> Conteúdo `fnv64:4b0f9add38856dab` · cenário `mvp_start` · 3 anos · seeds 42, 7, 123.
> Minutos = tempo de jogo a 1x (1 dia = 4 s). Crises (GDD v0.2 §6): **1** lenha abaixo da demanda do inverno no
> outono · **2** sem ferramentas de reserva e condição média < 50% · **3** família com fome no inverno.
> Trajeto = % das horas de turno dos produtores gastas andando (ida e volta).

## Por cenário (média das seeds; crises = primeira ocorrência entre as seeds)

| Jogador | Sobreviveu (seeds) | Famílias no fim (mín.) | Partidas | Crise 1 lenha | Crise 2 ferramentas | Crise 3 fome no inverno | Deadlock | Trajeto médio | Maior trecho sem acontecimento |
|---|---|---|---|---|---|---|---|---|---|
| passive | 0/3 | 0 (0) | 6 | ano 1 outono (~8 min) (3/3) | — | — | não | 0.0% | 89 dias (~5.9 min) |
| naive | 3/3 | 6 (6) | 0 | ano 1 outono (~8 min) (3/3) | — | — | não | 38.6% | 89 dias (~5.9 min) |
| optimal | 3/3 | 6 (6) | 0 | ano 1 outono (~8 min) (3/3) | — | — | não | 14.2% | 89 dias (~5.9 min) |
| optimal_no_roads | 3/3 | 6 (6) | 0 | ano 1 outono (~8 min) (3/3) | — | — | não | 15.6% | 89 dias (~5.9 min) |

## Por seed

| Jogador | Seed | Famílias | Partidas (1ª) | Crise 1 | Crise 2 | Crise 3 | Comida no fim | Lenha no fim | Trajeto |
|---|---|---|---|---|---|---|---|---|---|
| passive | 42 | 0 | 6 (ano 1 outono (~9 min)) | ano 1 outono (~8 min) | — | — | 0 | 60 | 0.0% |
| passive | 7 | 0 | 6 (ano 1 outono (~9 min)) | ano 1 outono (~8 min) | — | — | 0 | 60 | 0.0% |
| passive | 123 | 0 | 6 (ano 1 outono (~9 min)) | ano 1 outono (~8 min) | — | — | 0 | 60 | 0.0% |
| naive | 42 | 6 | 0 (—) | ano 1 outono (~8 min) | — | — | 137 | 404 | 38.6% |
| naive | 7 | 6 | 0 (—) | ano 1 outono (~8 min) | — | — | 125 | 310 | 38.6% |
| naive | 123 | 6 | 0 (—) | ano 1 outono (~8 min) | — | — | 137 | 403 | 38.6% |
| optimal | 42 | 6 | 0 (—) | ano 1 outono (~8 min) | — | — | 1544 | 372 | 14.2% |
| optimal | 7 | 6 | 0 (—) | ano 1 outono (~8 min) | — | — | 1902 | 368 | 14.1% |
| optimal | 123 | 6 | 0 (—) | ano 1 outono (~8 min) | — | — | 2421 | 466 | 14.2% |
| optimal_no_roads | 42 | 6 | 0 (—) | ano 1 outono (~8 min) | — | — | 2582 | 406 | 15.6% |
| optimal_no_roads | 7 | 6 | 0 (—) | ano 1 outono (~8 min) | — | — | 2071 | 410 | 15.6% |
| optimal_no_roads | 123 | 6 | 0 (—) | ano 1 outono (~8 min) | — | — | 2095 | 425 | 15.6% |
