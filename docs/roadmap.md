# Roadmap de execução

> Plano operacional (o *quê* e a *ordem*). Visão e fases: GDD §9. Arquitetura: TDD v0.1.

| Etapa | Status | Entrega |
|---|---|---|
| Marco 1 — núcleo da simulação + view cinza | ✅ aprovado (08/10/2026) | sim C# headless, 50 testes, soak 50 anos, CLI, view com primitivas |
| Etapa 1 — inventário de assets | ✅ | `docs/asset_manifest.md`, `CREDITS.md` |
| Reorganização A1–A3 | ✅ | `godot/`, `art/vendor_raw/`, `scripts/setup_vendor.py`, `docs/vendor_sources.md` |
| **Etapa 2 — integração de assets** | ⏳ próxima | ver abaixo |
| Etapa 3 — TEST_VILLAGE_01 | ⏳ | cena de validação + screenshots em 3 zooms + opções de material |
| **Marco 2 — loop central (GDD v0.2)** | 📋 registrado, não iniciado | ver abaixo |

## Etapa 2 — integração de assets (plano combinado)
1. Remover `godot/assets/vendor/.gdignore`; configurar import por pacote (escala, colisão quando fizer sentido, compressão/limite de textura).
2. Cenas herdadas em `godot/assets/<categoria>/` com nomes da Bible §13 (`BLD_*`, `ENV_*`, `PROP_*`, `CHR_*`); o código referencia só essas cenas.
3. Construções P0 montadas com o Medieval Village (pivô no centro da base, Y = solo).
4. `CHR_Villager_Base.tscn`: corpo-base (cópia sem corpo no Blender) + roupa de camponês; `AnimationLibrary` compartilhada UAL1/UAL2 com idle, walk, walk_carry, idle_carry (blend), harvest, plant_seed (+ chop; mine = chop; hammer via Mixamo no fim).
5. Registro sim id → cena (`game/visuals/visual_catalog.json`); trocar primitivas pelas cenas.
6. Terrain3D + texturas watercolor (quando estiverem em `art/vendor_raw/`).
7. Nature: normalizar escala e offset de altura nas cenas herdadas; registrar fatores no manifesto.
8. Lacunas (trigo, pilhas, tocos, poço, forja): placeholders, listadas como "falta" no manifesto.
9. TEST_VILLAGE_01 (Bible §43) + screenshots em 3 zooms + opções de material hand-painted × PBR.

## Marco 2 — loop central do GDD v0.2 (não iniciar antes da Etapa 2/3)
1. **Abertura jogável sem roteiro:** suprimentos na carroça e balanceamento para que um jogador inexperiente sobreviva ao 1º inverno com folga apertada, sem morrer no ano 1. Crise da lenha no 1º outono (GDD v0.2 §4.2).
2. **Política com faixa mínimo/máximo** (histerese explícita) em vez de limiar único — ciclos curtos, não de 3 anos.
3. **Construir consome madeira e pedra** (de verdade, com o material chegando à obra).
4. **Ferreiro** + política "produzir ferramentas se estoque < X".
5. **Capacidade Administrativa** + **sugestão automática de política** (GDD v0.2 §3).
6. **Caravana mercante** sazonal e simples: compra excedente, vende ferramentas e comida por moedas (destino para pedra e moedas).

### Economy Sheet via CLI
- Cenários de balanceamento na CLI: **jogador passivo**, **jogador ingênuo**, **abertura ótima**.
- `docs/balance_report.md` com CSV e gráficos por cenário, verificando se as crises 1–3 aparecem nos minutos previstos no GDD v0.2 §4.
