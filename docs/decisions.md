# Log de decisões

> Fonte da verdade atual, junto com `docs/roadmap.md`. Os GDDs (`GDD_sociedade_autonoma.md` — o oficial — e
> `GDD_v0.2_core_loop_5h.md`; cópia antiga em `docs/archive/`) ficam como histórico/visão: quando divergirem, vale o que está aqui.
> Uma linha por decisão, mais recente embaixo. Datas no formato DD/MM/AAAA.

| Data | Decisão | Substitui / observação |
|---|---|---|
| 07/10/2026 | **Visão e pilares** do jogo (city builder sobre delegação) e **GDD original** (`GDD_sociedade_autonoma.md`, o oficial). | `docs/archive/GDD_v0.1_sociedade_autonoma.md` é cópia antiga, só histórico |
| 07/10/2026 | **Recomendação de C#** para o núcleo da simulação (GDD D1). | Confirmada em 08/10/2026 |
| 08/10/2026 | **Visual 3D cozy/cartoon** (referência: Lands of Koastalia). Sem pixel shader, baixa resolução, dithering ou snapping. | GDD D7 (resolução interna), D8 e as referências de pixel art 3D |
| 08/10/2026 | **Confirmação do C#: simulação em C# puro** (`src/Ironvale.Sim`, sem Godot), determinística, tick fixo; a view só lê estado e envia comandos. GDScript só quando for claramente mais simples (ex.: integração com Terrain3D). | Fecha GDD D1 |
| 08/10/2026 | **Só PC.** | — |
| 08/10/2026 | **Assets 3D CC0**: Quaternius (Stylized Nature, Medieval Village, personagens/animações) e KayKit; só glTF/GLB; originais intocados em `art/vendor_raw/`. | — |
| 08/10/2026 | **UI: Kenney UI Pack RPG Extension (CC0) + game-icons.net (CC BY 3.0, crédito por ícone)**, organização inspirada em Manor Lords. `docs/UI_UX_guide.md` é obrigatório para UI; arte final da UI em F3. | — |
| 08/10/2026 | **Paleta V2 "meadow"** (mais saturada) escolhida como padrão global. | Aprovação do look-dev visual completo pendente (roadmap) |
| 08/10/2026 | **Marco 2 dividido**: **2A** = loop central jogável para playtest (abertura sem roteiro, decreto com faixa mín/máx, construção consumindo recursos, ferreiro, tempo de trajeto, horta no quintal, CA, sugestão automática de decreto). **2B** = diferenciais e polimento (delegados com personalidade, "enquanto você estava fora", caravana mercante, NPCs cozy, cerimônia do decreto). | Gate do 2A: playtest com 5 pessoas (GDD §8.3) |
| 08/10/2026 | **Feedback de playtester no 2A**: tempo de trajeto no turno (só produz no local; painel mostra % em trajeto), andar fora da estrada mais lento (custo de terreno), horta no quintal no tempo livre. Desire paths e animais de tração vão para o backlog. | — |
| 08/10/2026 | **Legibilidade obrigatória**: toda mecânica nova explica perda e causa em painel/tooltip e reforça o loop problema → solução → delegação. | — |
| 08/10/2026 | **Diferencial: delegados são pessoas** — o reeve tem nome e 1 traço (cauteloso, ambicioso, desleixado, ganancioso) que modifica a Utility AI. Delegar = escolher em quem confiar. | Marco 2B |
| 08/10/2026 | **Diferencial: "Enquanto você estava fora"** — acelerar 1/5/10 anos e ler a crônica dos momentos marcantes. | Marco 2B; absorve parte da "Crônica" do backlog |
| 08/10/2026 | **Tema medieval da delegação** (UI e textos): política = *decreto* (pregoeiro + quadro de avisos); administrador = *reeve*, regional = *bailio*, topo = *senescal*; CA = escrivães, pergaminhos, salão do senhor; log = *livro de contas do reeve*. | Glossário do GDD §12 atualizado |
| 08/10/2026 | **Game feel é critério de aprovação de todo marco**: resposta < 100 ms, animação de 150–250 ms, câmera suave, construção com fantasma/encaixe/som, desfazer e atalhos, nunca > 2 min sem decisão relevante; passe de feel de 15 min com notas em `docs/feel_notes.md`. | — |
| 08/10/2026 | **Pós-playtest (backlog)**: decretos antigos viram "costume"; "demonstrar em vez de configurar". | — |
| 08/10/2026 | **Nomes na interface × código**: o código mantém `Policy`/`Administrator`; o jogador só vê decreto/reeve via chaves do `ui.csv`. Troca feita com as telas do 2A, junto com o `UI_UX_guide.md`. | — |
| 08/10/2026 | **Cenários de balanceamento da CLI** implementados junto com o tempo de trajeto (2A). | — |
| 08/10/2026 | **GDD oficial** = `GDD_sociedade_autonoma.md`; a cópia v0.1 foi para `docs/archive/`. | Resolve TDD Q16 |
| 08/10/2026 | **Etapas 2 e 3 concluídas** (integração e montagem da TEST_VILLAGE_01). O look-dev visual (chão, vegetação, luz, câmera) vira item próprio, em andamento; fecha com a aprovação do LOOKDEV_GROUND aplicado na TEST_VILLAGE_01 e não bloqueia o 2A. | — |
| 08/10/2026 | **Look-dev visual aprovado** e item fechado; ajustes finos de arte ficam para a F3. | — |
| 08/10/2026 | **Ordem do Marco 2A**: 1 construção consome madeira/pedra → 2 decreto com faixa mín/máx → 3 trajeto + fora da estrada + horta + cenários CLI → 4 ferreiro + decreto de ferramentas → 5 abertura sem roteiro balanceada → 6 CA + sugestão de decreto (telas com decreto/reeve). Incrementos jogáveis, commit + push por item; passe de feel após 3, 5 e 6; build + roteiro de playtest após 6. | — |
