# Playtest — sessões roteirizadas (teste do analisador)

> Gerado por `tools/analyze_playtest.py` em 09/10/2026 · 4 sessão(ões).
> Tempos em min:seg desde o início da sessão (tempo real; nas sessões roteirizadas, tempo de jogo a 1x).
> Clique = comando enviado ao jogo. Delegação = decreto aceito de uma sugestão ou criado à mão (tecla P).

## Resumo por sessão

| Sessão | Duração | 1ª construção | 1ª sugestão | 1ª aceita | 1ª recusada | 1º decreto à mão | Sugestões (aceitas/recusadas) | Crises | Famílias perdidas | Decretos no fim | Trechos > 2 min sem decisão |
|---|---|---|---|---|---|---|---|---|---|---|---|
| naive_123 | 72:00 | 0:00 | 12:04 | — | — | — | 1 (0/0) | firewood 8:00, tools 39:48 | 0 | 0 | 5 |
| naive_42 | 72:00 | 0:00 | 12:04 | — | — | — | 1 (0/0) | firewood 8:00, tools 46:32 | 0 | 0 | 5 |
| naive_7 | 72:00 | 0:00 | 12:04 | — | — | — | 1 (0/0) | firewood 8:00, tools 39:56 | 0 | 0 | 5 |
| optimal_42 | 72:00 | 0:00 | — | — | — | 0:00 | 0 (0/0) | firewood 8:00 | 0 | 3 | 3 |

## Médias

| Métrica | Média (sessões com o evento) | Alvo |
|---|---|---|
| 1ª construção | 0:00 (4) | — |
| 1ª sugestão de decreto | 12:04 (3) | ~15–25 min (GDD v0.2 §4.2) |
| 1ª delegação aceita | — | — |
| 1ª recusada | — | — |
| Sugestões aceitas | 0/0 (0%) | > 50% (guia §9.3) |
| Decretos ativos no fim | 0.8 | ≥ 3 em ~60 min (GDD §8.3) |
| Crises por sessão | 1.8 | ≥ 2 em 60 min (GDD §8.3) |
| Famílias perdidas por sessão | 0.0 | — |
| Trechos > 2 min sem decisão por sessão | 4.5 | 0 (feel) |
| Crise 1 — lenha | 8:00 (4) | — |
| Crise 2 — ferramentas | 42:05 (3) | — |
| Crise 3 — fome no inverno | — | — |

## Cliques por minuto ao longo do tempo (janelas de 5 min)

Alvo: **cair** ao longo da partida (guia §9.3 / GDD v0.2 §8).

| Sessão | 0–5 | 5–10 | 10–15 | 15–20 | 20–25 | 25–30 | 30–35 | 35–40 | 40–45 | 45–50 | 50–55 | 55–60 | 60–65 | 65–70 | 70–75 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| naive_123 | 2.8 | 0.0 | 0.8 | 0.0 | 0.8 | 0.0 | 0.0 | 0.8 | 0.8 | 0.0 | 0.0 | 0.0 | 0.0 | 0.0 | 0.0 |
| naive_42 | 2.8 | 0.0 | 0.8 | 0.0 | 0.8 | 0.0 | 0.0 | 0.0 | 0.0 | 0.0 | 0.0 | 0.0 | 0.8 | 0.8 | 0.0 |
| naive_7 | 2.8 | 0.0 | 0.8 | 0.0 | 0.8 | 0.0 | 0.0 | 0.8 | 0.8 | 0.0 | 0.0 | 0.0 | 0.0 | 0.0 | 0.0 |
| optimal_42 | 2.8 | 0.0 | 0.4 | 0.4 | 0.4 | 0.0 | 0.0 | 0.0 | 0.0 | 0.0 | 0.0 | 0.0 | 0.0 | 0.0 | 0.0 |

## Maiores períodos sem decisão

- **naive_123:** 28.0 min a partir de 44:00; 16.0 min a partir de 20:00; 10.9 min a partir de 1:04
- **naive_42:** 40.0 min a partir de 20:00; 10.9 min a partir de 1:04; 8.0 min a partir de 60:00
- **naive_7:** 28.0 min a partir de 44:00; 16.0 min a partir de 20:00; 10.9 min a partir de 1:04
- **optimal_42:** 49.7 min a partir de 22:20; 14.0 min a partir de 0:00; 5.1 min a partir de 14:00
