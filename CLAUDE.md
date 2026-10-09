# Ironvale — instruções para agentes

## Documentos de referência (ler antes de trabalhar na área)
- `docs/GDD_sociedade_autonoma.md`, `docs/GDD_v0.2_core_loop_5h.md` — visão, sistemas, core loop.
- `docs/TDD_v0.1.md` — arquitetura técnica (sim C# pura × view Godot).
- **`docs/decisions.md` + `docs/roadmap.md` — fonte da verdade atual** (decisões datadas; o que está feito e o que vem
  a seguir, respeitando os gates). Os GDDs são histórico/visão; glossário do tema medieval no GDD §12.
- `docs/feel_notes.md` — passe de feel de 15 min antes de fechar cada marco.
- `docs/asset_manifest.md`, `docs/vendor_sources.md` — assets e origem dos pacotes.
- **`docs/UI_UX_guide.md` — referência OBRIGATÓRIA para qualquer trabalho de UI.** Em especial §8.1:
  um único Theme (`godot/ui/theme/main_theme.tres`), nada de cor/fonte/StyleBox direto nos nós,
  layout só com Containers e âncoras (base 1920×1080, `canvas_items`, `expand`), UI só lê snapshots e
  envia comandos, ícones por ID lógico (icon_registry), textos via `tr()`. Processo por tela §9.1.
- `docs/asset_production_bible_mvp.md` — Bible de assets (IDs §13/§18, TEST_VILLAGE §43). Ainda não está no repo.

## Decisões que valem mais que os documentos antigos
- Visual 3D cozy/cartoon (Lands of Koastalia): sem pixel shader, baixa resolução, dithering ou snapping.
- UI: Kenney UI Pack RPG Extension (CC0) + game-icons.net (CC BY 3.0, crédito por ícone em `docs/icon_credits.csv` e `CREDITS.md`); organização inspirada em Manor Lords.
- Simulação em C# puro (`src/Ironvale.Sim`, sem Godot), determinística, tick fixo; view nunca altera o estado.

## Regras de trabalho
- Commits pequenos por etapa; perguntar quando docs conflitarem (não decidir sozinho).
- Nunca modificar `art/vendor_raw/`; só glTF/GLB (nada de FBX/OBJ); o jogo referencia só cenas em `godot/assets/<categoria>/`.

## Comandos
- Testes: `dotnet test tests/Ironvale.Sim.Tests` · CLI: `dotnet run --project src/Ironvale.Sim.Cli -- --years 50`
- Jogo: `godot-mono --path godot` · smoke: `godot-mono --path godot -- --smoke`
- Assets: `python3 scripts/setup_vendor.py --derive` · cenas: `python3 tools/assets/build_scenes.py`
