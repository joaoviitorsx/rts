# Roteiro de playtest — Marco 2A (gate: 5 pessoas)

> Baseado no GDD §8.3 (critérios de sucesso) e no `UI_UX_guide.md` §9.2–§9.3. Uma sessão = ~75 min por pessoa.
> Quem conduz **não ajuda**: só observa e anota. A pergunta do marco: a delegação parece **conquista** ("não preciso
> mais cuidar disso") ou **o jogo jogando por mim** (GDD §8.4)?

## 1. Builds

| Sistema | Arquivo (em `out/playtest/`, fora do git) | Como abrir |
|---|---|---|
| Windows 10/11 x64 | `Ironvale_playtest_2A_windows_x86_64.zip` | extrair a pasta inteira e abrir `Ironvale.exe` |
| Linux x64 | `Ironvale_playtest_2A_linux_x86_64.tar.gz` | extrair e rodar `./Ironvale.x86_64` |

- Conferir com `SHA256SUMS.txt`. O build Windows foi gerado, mas **não foi executado** nesta máquina (sem Windows);
  o Linux passou no smoke test exportado. Fazer um teste rápido no Windows antes da 1ª sessão.
- Exportar de novo: `godot-mono --headless --path godot --export-release "Windows" ../out/playtest/windows/Ironvale.exe`
  (idem `"Linux"`). Templates oficiais 4.7 mono instalados em `~/.local/share/godot/export_templates/4.7.stable.mono/`.
- O build de jogador **não tem** painel de debug. Atalhos: Espaço pausa · 1–4 velocidade · F famílias · P decretos ·
  F5/F9 salvar/carregar · Esc fecha painel · Ctrl+= / Ctrl+- escala da interface.
- **Configurações** (barra inferior): escala da interface 80–150%. Em telas pequenas o jogo sobe a escala sozinho até o
  menor texto ter ≥ 12 px (1280×720 → 129%) e mostra o motivo no painel. Se a pessoa usar um notebook pequeno, deixe-a
  ajustar antes de começar.

## 2. Registro automático (não precisa cronometrar à mão)

Cada sessão grava um CSV: tempo real (s), dia do jogo, cada comando do jogador e os eventos marcantes (sugestão
oferecida, obra concluída, família foi embora, alertas, comandos rejeitados).

- Windows: `%APPDATA%\Godot\app_userdata\Ironvale\playtest\session_*.csv`
- Linux: `~/.local/share/godot/app_userdata/Ironvale/playtest/session_*.csv`

Pedir o arquivo ao fim da sessão. Para gerar o relatório: `python3 tools/analyze_playtest.py <pasta com os CSVs> --out docs/reports/playtest_<data>.md`.
O build candidato grava o **formato v1** (sem linhas de crise): o relatório mostra crises 1–2 como "—" e estima os decretos
no fim; builds da branch `feature/2B-polish` gravam o v2 (estado diário + crises). Dele saem: cliques (comandos) por minuto, decretos criados, sugestões
oferecidas × aceitas (`AcceptSuggestion` / `DismissSuggestion`), quando cada família saiu.

## 3. Antes de começar (5 min)

1. Consentimento para gravar tela e voz (gravar se possível: OBS ou equivalente).
2. Frase única de contexto (não explicar mecânicas): *"É um jogo de construir uma vila. Você governa as regras; os
   aldeões trabalham sozinhos. Pense em voz alta o tempo todo — diga o que vê, o que acha que está acontecendo e
   o que quer fazer. Não existe resposta errada; estamos testando o jogo, não você."*
3. Jogo em 1920×1080 (ou a resolução nativa da pessoa), velocidade livre.

## 4. Sessão (60 min de jogo)

| Tempo | O que observar | Anotar |
|---|---|---|
| 0–5 min | Lê o cartão "Próximo objetivo"? Descobre como designar carregadores (Salão → Designar)? | Tempo até o 1º carregador; onde travou |
| 5–15 min | Constrói casas, campo, lenhador; entende que a obra precisa de material e construtor? | Lê "Parada: …" no painel da obra? |
| ~8–12 min | **Crise 1 — lenha no 1º outono.** Percebe o alerta "Pouca lenha"? Reage como (receita Rachar lenha, mover famílias)? | Tempo entre o alerta e a reação |
| ~12–20 min | **Cartão "O reeve sugere".** Lê? Aceita, adia ou recusa? Entende o custo em CA e o que deixa de controlar? | Ação + frase dita ao ver o cartão |
| 20 min | **Pausar e fazer as 3 perguntas-chave** (§6). Retomar. | Respostas literais |
| 20–45 min | Cria decretos por conta própria (P)? Mexe na faixa mín/máx? Abre o livro de contas do reeve? | Nº de decretos; se lê o livro |
| 30–45 min | Trajeto: nota "% do turno andando"? Constrói estrada ou casa perto? Ferreiro/pedreira (ferramentas)? | Se conecta trajeto ↔ produção |
| 45–60 min | 1º inverno: a vila aguenta **sem intervenção** depois de delegar? Alguém foi embora? | Ações manuais no inverno (CSV) |

Não responder perguntas durante o jogo; anotar a pergunta (é um problema de UI).

## 5. Teste dos 5 segundos (guia §9.2)

Pausar uma vez (~30 min) e mostrar cada painel por 5 s; perguntar "o que este painel está dizendo?":

1. Painel de um edifício em obra (material, construtores, motivo de parada).
2. Painel de um produtor com trajeto alto ("% do turno andando").
3. Painel "Decretos do reeve" (CA, estado de cada decreto).
4. O cartão de sugestão (se estiver na tela; senão, mostrar um print tirado antes da sessão).

Falhou = a pessoa não sabe dizer o objetivo do painel → informação demais ou mal hierarquizada.

## 6. Perguntas-chave (aos 20 min, sem ajuda)

1. "Por que a vila ficou (ou quase ficou) sem lenha?"
2. "O que o reeve está fazendo agora?"
3. "O que você faria em seguida?"

## 7. Depois da sessão (10 min)

1. "Conte em 1 minuto o que aconteceu na sua vila." (procurar espontaneamente: *"não preciso mais cuidar disso"*,
   *"deixei com o reeve"* — critério GDD §8.3).
2. "Teve algum momento em que você ficou sem saber o que fazer?" (feel: > 2 min sem decisão).
3. "O reeve fez algo que você não queria? Quando?" (risco: "o jogo jogando por mim").
4. "O que foi mais chato de repetir?" (candidatos a novos decretos/atalhos).
5. Nota de 1 a 5: "Eu senti que governava a vila, não que a vila se governava sozinha."

## 8. Folha de critérios por pessoa (GDD §8.3 + guia §9.3)

| Critério | Alvo | P1 | P2 | P3 | P4 | P5 |
|---|---|---|---|---|---|---|
| Passou por ≥ 2 crises em 60 min (lenha, ferramentas, fome no inverno) | sim | | | | | |
| Delegou ≥ 3 responsabilidades em ~60 min (decretos ativos/criados) | ≥ 3 | | | | | |
| Depois de delegar, sobreviveu ao inverno sem intervenção | sim | | | | | |
| Disse espontaneamente algo como "não preciso mais cuidar disso" | sim | | | | | |
| Tempo para achar "por que X caiu" (painel/tooltip) | < 15 s | | | | | |
| Cliques por minuto caem entre os 20 min iniciais e os 20 finais (CSV) | cair | | | | | |
| Alertas ignorados até virar crise | baixo | | | | | |
| Sugestões de decreto aceitas | > 50% | | | | | |
| Momentos > 2 min sem decisão | 0 | | | | | |

**Gate do 2A:** a maioria dos critérios em ≥ 4 de 5 pessoas e nenhum sinal forte de "o jogo jogando por mim" (§8.4).
Resultados em `docs/playtest_2A_resultados.md` (incluir as notas de §9); problemas de sensação também em `docs/feel_notes.md`.

## 9. Observar com atenção (pedido do dono, 09/10/2026)

| Decisão | O que olhar | Sinal de problema |
|---|---|---|
| **P1** — obra parada sem carregador | Quanto tempo a pessoa leva para designar carregadores; se lê "Parada: ninguém carregando material" e o objetivo 1 | > 3 min com casas posicionadas e nada acontecendo; frustração dita em voz alta |
| **P18** — passivo morre cedo | Se alguém demora a agir no começo e perde famílias antes do 1º outono | Famílias indo embora antes dos ~8 min sem a pessoa entender por quê |
| **P21** — faixa sugerida pelo reeve | A faixa do cartão (lenha/comida cobrem metade do inverno, com o aviso "o mínimo foi aumentado") | Pessoa recusa por achar a faixa errada, ou aceita e depois muda a faixa no painel P |

Anotar na folha (§8) com o minuto e a frase dita.

## 10. O que já se sabe (para não gastar sessão nisso)

- Arte, som e animações de UI são cinza/placeholder (F3); a pedreira é um kitbash de rochas e props só para ficar
  legível.
- Crise 2 (ferramentas) quase não aparece em 60 min nos cenários da CLI (12 ferramentas de reserva) — observar se
  alguém a encontra; senão, ajustar antes da próxima rodada.
- O livro de contas do reeve ainda mostra nomes em português mesmo com o jogo em inglês (os outros textos já traduzem).
- Aldeões "correm" no trajeto (o dia dura 4 s a 1x).
