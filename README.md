# Ironvale — Sociedade Autônoma

City builder medieval sobre delegação (Godot 4.7 .NET + simulação C# pura). Docs em `docs/` (GDD, TDD, manifesto de assets).

## Requisitos
- .NET SDK 8 (`dotnet --list-sdks`)
- Godot 4.7 **.NET** (nesta máquina: `godot-mono`, wrapper em `~/.local/bin` que exporta `DOTNET_ROOT`)
- Python 3 (só para `scripts/setup_vendor.py`)

## Rodar o jogo
```bash
godot-mono --path godot                 # jogo
godot-mono --path godot --editor        # editor
# atalhos de desenvolvimento (depois de "--"):
godot-mono --path godot -- --opening --days=60 --speed=8 --debug
```
`--opening` aplica a abertura roteirizada (casas, lenhadores, campos, celeiro, 2 carregadores, 3 políticas); `--days=N` pré-simula; `--debug` abre o painel de debug.

**Controles:** WASD/setas/borda da tela/botão do meio = mover · roda = zoom · Q/E = girar 90° · Espaço = pausa · 1–4 = 1x/2x/4x/8x ·
barra inferior = construir (R gira, Shift mantém, botão direito cancela) · clique num edifício = inspecionar/designar família ·
P = painel Famílias/Políticas · F3 = debug (fluxos, gráfico, avançar N anos, hash) · F5/F9 = salvar/carregar rápido.

## Testes e simulação headless
```bash
dotnet test tests/Ironvale.Sim.Tests                         # 50 testes, inclui soak de 50 anos (~5 s)
dotnet test tests/Ironvale.Sim.Tests --filter Category!=Soak # rápido
dotnet run --project src/Ironvale.Sim.Cli -- --years 50 --seed 42 --csv out/   # resumo anual + CSV
godot-mono --path godot -- --smoke                           # smoke test de input na engine (SMOKE OK/FAIL)
```

## Estrutura
`src/Ironvale.Sim` simulação (sem Godot) · `src/Ironvale.Sim.Cli` runner headless · `tests/` xUnit ·
`godot/` projeto Godot (`data/` conteúdo JSON, `game/` view) · `art/vendor_raw/` pacotes originais (fora do git) ·
`scripts/setup_vendor.py` copia glTF para `godot/assets/vendor/` (ver `docs/vendor_sources.md`).
