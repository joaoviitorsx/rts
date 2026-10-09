# Relatório da madrugada — 09/10/2026

Branch: `overnight/2026-10-09` (push feito a cada item; nada na `main`). Último commit de feature: `9f1ba5d`;
depois disso só trabalho seguro (testes, relatório).

## 1. Resumo (5 linhas)

1. **Os 6 itens do Marco 2A foram implementados na ordem do plano**, cada um com testes, smoke na engine, commit e push.
2. Testes: 50 → **103** (determinismo, save/load, soak de 50 anos, fuzz de comandos e as mecânicas novas), build sem avisos.
3. Balanceamento pela CLI (`docs/balance_report.md`): o jogador ingênuo não perde ninguém no ano 1 em 3 seeds; a crise
   da lenha surge no 1º outono (~8 min); a 1ª sugestão de decreto aos ~12 min; a crise das ferramentas aos ~40 min.
4. Dois bugs graves achados e corrigidos: carregadores deixavam a colheita apodrecer no campo (fome com comida no
   mapa) e o build de jogador quebrava no HUD (só sem o painel de debug).
5. Build de playtest Windows + Linux pronto em `out/playtest/` e roteiro em `docs/playtest_2A.md`; o Windows **não foi
   executado** (não há Windows aqui) e os passes de feel humanos de 15 min continuam com você.

## 2. Itens concluídos

| Item | Commits | Como testar no jogo (2 linhas) |
|---|---|---|
| 1. Construção consome madeira e pedra | `684ee3d` | Posicione uma casa sem carregadores: o painel da obra diz "Parada: ninguém carregando material". Designe 2 famílias no Salão e veja o material chegar ("8/15, 3 a caminho") e famílias sem emprego construindo. |
| 2. Decreto com faixa mín/máx | `c27bf22` | Tecla P: crie "Manter Madeira entre 40 e 60". A linha de estado diz "recrutando / dentro da faixa / liberando"; o livro de contas explica cada movimento. |
| 3. Trajeto + estrada + horta + cenários | `ff6371d` `96583e2` `5ff8257` `c54d168` | Selecione um campo longe das casas: "Trajeto: X% do turno andando"; botão Estrada (arrastar em L, Ctrl remove) e veja o % cair. Famílias (F): trajeto, tempo livre e horta por família. |
| 4. Ferreiro + decreto de ferramentas | `8dcbd35` | Construa Pedreira e Ferreiro: o painel do ferreiro mostra insumos (madeira/pedra) reabastecidos pelos carregadores e "parado: sem pedra" quando falta. Decreto "Ferramentas entre 8 e 12" põe famílias lá. |
| 5. Abertura sem roteiro | `7fe1f85` `9f1ba5d` | Jogo novo começa só com o Salão e a carroça; o cartão "Próximo objetivo (1/9)" guia até o 1º inverno. Por volta de 8 min (1x) aparece "Pouca lenha — inverno em N dias". |
| 6. CA + sugestão de decreto + telas | `16dc84b` `1a97fe9` `0fdea82` | Barra de cima: "CA 0/4". Mova 3 vezes famílias de outro trabalho para lenhadores: surge "💡 O reeve sugere" com Criar decreto / Agora não / Nunca; painel P = "Decretos do reeve" + "Livro de contas do reeve". |

Também: `f11f354` (plano em `docs/plan_2A.md`). Dev: `-- --player=naive|optimal --days=N --panel=... --shot=arquivo.png`.

## 3. Parciais ou bloqueados

Nenhum item ficou bloqueado. O que ficou parcial e por quê:

- **Build Windows:** exportado (`Ironvale_playtest_2A_windows_x86_64.zip`), mas não executado — não há Windows nem
  permissão para instalar Wine. O build Linux exportado passou no smoke test.
- **Passes de feel (itens 3, 5, 6):** fiz só a parte medível (resposta, FPS, ritmo pela CLI, capturas) em
  `docs/feel_notes.md`. Os 15 min jogados são seus.
- **Teste de resolução (1280×720 / 2560×1440):** o gerenciador de janelas ignora `--resolution`; só verifiquei a janela
  real 1920×897.
- **Termos:** a interface já diz decreto/reeve, mas nomes de conteúdo (recursos, edifícios) e o trecho "(saiu de X)" do
  livro de contas ainda vêm do sim em português (pendência antiga de UI).
- **Placeholders:** a pedreira é um bloco cinza (sem modelo); som e animações de UI ficam para F3.

## 4. Balanceamento (3 anos, seeds 42 / 7 / 123 — `docs/balance_report.md`)

| Cenário | Sobreviveu? | Crise 1 (lenha) | Crise 2 (ferramentas) | Crise 3 (fome no inverno) | Deadlock | Trajeto médio |
|---|---|---|---|---|---|---|
| passive | 0/3 (todos saem no 1º outono, ~9 min) | ano 1 outono (~8 min) | — | — | não | 0% |
| naive | 3/3 (6 famílias) | ano 1 outono (~8 min) | ano 2 inverno (~40 min) | — | não | 39% (casas longe do trabalho) |
| optimal | 3/3 (6 famílias) | ano 1 outono (~8 min) | — (ferreiro evita) | — | não | 13,9% |
| optimal sem estrada | 3/3 | ano 1 outono (~8 min) | ano 2 primavera (~25 min) | — | não | 15,9% |

- 1ª sugestão de decreto: ingênuo no 1º outono (~12 min) nas 3 seeds (GDD v0.2 pede ~15–25 min).
- Estrada reduz o trajeto (15,9% → 13,9%); morar longe custa ~39% do turno.
- Crise 3 (fome no inverno) não aparece nos roteiros — pode aparecer com humanos; observar no playtest.
- "Maior trecho sem acontecimento" chega a 89 dias (~6 min) nos roteiros depois do 1º ano (eles param de construir);
  o cartão de objetivo existe para que um humano sempre tenha o próximo passo.

## 5. `docs/pending_decisions.md` por prioridade

**Alta (confirmar antes do playtest)**
1. **P6/P12 — Pedreira adicionada.** Pedra não tinha fonte; sem ela, ferreiro e estrada esgotavam a carroça.
2. **P15 — Urgência de transporte por necessidade.** Comida (e lenha no outono/inverno) abaixo de 20 dias de consumo é prioridade dos carregadores; corrigiu a fome com comida no campo.
3. **P19 — CA:** Salão = 4; faixa = 1 por recurso; ferramentas = 2. Com 3 de CA o critério "delegou ≥ 3" era impossível.

**Média**
4. P1 — Só carregadores levam material à obra (sem carregador a obra para, com motivo e alerta).
5. P21 — Faixa sugerida = estoque médio observado (lenha 60–80 pode ser baixa para o inverno).
6. P20 — Só remanejamentos contam como repetição para a sugestão.
7. P24 — Carroça com 6 ferramentas (crise 2 aos ~40 min).
8. P16 — Carroça com 400 de comida (era 300).
9. P8 — Escala do trajeto (`commuteTicksPermille` 70‰); quem mora longe perde turno **e** tempo livre.
10. P10/P22 — Definição dos jogadores roteirizados (passive/naive/optimal).
11. P13 — Receita do ferreiro e reabastecimento de insumo abaixo de metade.
12. P17 — Lista de 9 objetivos na camada de UI.
13. P5 — Argumentos do livro de contas ainda em português.
14. P7 — Estrada paga na hora (sem obra).

**Baixa**
15. P2, P3, P4, P9, P11, P14, P18, P23 — prioridades dos carregadores, regras da obra, passo da UI, mudança automática de casa, comandos instantâneos, roteiro ótimo com indústria, passivo morrer cedo, presets/instrumentação.

## 6. Trabalho seguro feito depois dos itens

- **Fuzz de comandos** (`450ba04`): 2 anos de comandos aleatórios em 3 seeds (mais 30 seeds avulsas), invariantes todo
  dia e uma cópia carregada de save no dia 200 idêntica por hash — tudo verde.
- **Profiling (Release, sem mudar comportamento):** ~725 mil ticks/s com o jogador ótimo (50 anos em ~1 s); ~270 mil com
  o ingênuo. A 8x o jogo precisa de 80 ticks/s. No jogo, 1% low de ~40 FPS é igual pausado (vsync/compositor, não sim).
- **Legibilidade em 1280×720 (simulada):** o layout é o mesmo (base 1920, `canvas_items`), mas o texto secundário fica com
  ~9 px. Sugestão para depois do playtest: opção de escala da UI.

## 7. Riscos e próximos passos

**Riscos**
- **A heurística da sugestão** (3 remanejamentos em 60 dias) e a faixa sugerida são suposições; só o playtest mostra se
  a sugestão parece conquista ou "o jogo jogando por mim" (GDD §8.4).
- **Balanceamento caótico:** pequenas mudanças na carroça mudam o desfecho do ingênuo no ano 3 de forma não monótona; a
  trava de regressão cobre só o ano 1 (3 seeds).
- **Windows não testado:** risco de algo específico de plataforma (.NET/GDExtension Terrain3D) na 1ª sessão.
- **CA sem fontes novas:** o teto de 4 dura o playtest inteiro; pode parecer arbitrário.
- **Saves:** cada item subiu a versão (agora v7); saves anteriores são rejeitados com mensagem (decisão aprovada).

**Próximos passos**
1. Você: passe de feel de 15 min (builds em `out/playtest/` ou `godot-mono --path godot`) e revisar P6/P12, P15 e P19.
2. Rodar o build Windows numa máquina Windows antes da 1ª sessão.
3. Playtest com 5 pessoas seguindo `docs/playtest_2A.md`; coletar os CSV de sessão.
4. Ajustar, a partir do playtest, a heurística/faixa da sugestão e o ritmo das crises; só então merge na `main`.
5. Depois do gate do 2A: Marco 2B (fora do escopo desta madrugada).
