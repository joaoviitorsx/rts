# Notas de feel

> Passe de feel antes de fechar cada marco: 15 min jogando **só** para avaliar a sensação (critérios em
> `docs/roadmap.md` → "Game feel"). Anotar o que pareceu lento, mudo, travado ou sem decisão; não corrigir durante o passe.

## Modelo

### Marco X — DD/MM/AAAA (build/commit)
- **Resposta (< 100 ms, animação 150–250 ms):**
- **Câmera (suavização, zoom no cursor, rotação 90°):**
- **Construção (fantasma, encaixe, som, obra):**
- **Repetição (copiar, desfazer, atalhos):**
- **Ritmo (períodos > 2 min sem decisão; próximo objetivo visível?):**
- **Top 3 para corrigir antes de fechar:**

## Marco 2A — item 3 — 09/10/2026 (passe medido pelo agente, sem jogador humano)

> Os 15 min reais são do dono. Aqui só o que dá para medir/observar automaticamente.

- **Resposta (< 100 ms, animação 150–250 ms):** antes, comandos esperavam o próximo tick (até 100 ms a 1x) e **não
  respondiam com o jogo pausado**. Corrigido: o comando é aplicado na hora, no mesmo tick (determinístico; teste
  `Applying_commands_immediately_gives_the_same_world_as_at_the_next_step`). Animações de UI ainda não existem (F3).
- **Desempenho:** jogo com a abertura, 60 dias: ~59,7 FPS com vsync, 1% low ~40 FPS em pausa, 1x e 8x (o mesmo
  pausado → não é a simulação; provável jitter de vsync/compositor). Sem hitches > 33 ms.
- **Câmera:** sem mudanças neste item.
- **Construção:** fantasma não fica mais vermelho por falta de material (o material vem depois). Obra mostra material
  a caminho, construtores e motivo de parada. Sem som/poeira/andaime ainda (2B/F3).
- **Estrada:** arrastar em L, Ctrl+arrastar remove; fantasma por célula (livre/ocupada/já é estrada). Falta som e
  "encaixe".
- **Trajeto visível:** aldeões correm pelo caminho de manhã e ao fim do dia; como o dia dura 4 s a 1x, eles passam
  muito rápido (rotina visível de verdade é do 2B).
- **Ritmo (períodos > 2 min sem decisão):** nos cenários da CLI, depois do 1º ano nada acontece além das estações
  (trechos de até 89 dias ≈ 6 min). O jogador roteirizado para de construir; um humano teria o que fazer, mas não há
  **próximo objetivo visível** — previsto no item 5 (cartão de objetivo).
- **Top 3 para corrigir:** (1) abertura: decretos ocupam todas as famílias e casas ficam sem construtor no 1º verão
  (item 5); (2) cartão de próximo objetivo (item 5); (3) pedra sem fonte (estrada e ferreiro dependem dela — item 4).

## Marco 2A — item 5 — 09/10/2026 (passe medido pelo agente, sem jogador humano)

- **Próximo objetivo visível:** cartão fixo no canto superior esquerdo desde o segundo 0 ("Próximo objetivo (1/9):
  Designe 2 carregadores…"). 9 passos até sobreviver ao 1º inverno, depois "Cresça…". Cada texto diz **o que fazer e
  onde** (Salão → Designar; receita Rachar lenha; tecla P). Falta: destaque visual/seta no alvo (F3).
- **Ritmo:** com o cartão, sempre há uma próxima ação. Nos cenários da CLI o jogador ingênuo encontra a crise da lenha aos
  ~8 min (1º outono) — dentro do GDD v0.2 §4.2 (5–30 min). Crise 2 (ferramentas) não aparece em 3 anos nos cenários:
  12 ferramentas de reserva + desgaste lento (pendência de balanceamento para o 1º playtest).
- **Logística (achado grave, corrigido):** a colheita ficava no campo e as famílias passavam fome com comida no mapa,
  porque os carregadores preferiam o buffer de madeira mais cheio. Agora comida (e lenha no outono/inverno) abaixo de
  `haulUrgentDays` (20) dias de consumo é prioridade. Isso sozinho fez o ingênuo sobreviver ao ano 1.
- **Abertura:** o jogo começa sem roteiro (só o Salão e a carroça). Carroça: 400 comida, 60 lenha (antes 300/60).
- **Top 3 para o dono testar:** (1) os textos do cartão são claros sem tutorial? (2) a crise da lenha (~8 min) é
  percebida antes de virar fome/frio? (3) o jogo começa rodando a 1x — deveria começar pausado?

## Marco 2A — item 6 — 09/10/2026 (passe medido pelo agente, sem jogador humano)

- **Momento-chave (sugestão do reeve):** nos cenários da CLI, o jogador ingênuo recebe a 1ª sugestão no 1º outono
  (~12 min), logo depois da crise da lenha (~8 min) — perto do alvo do GDD v0.2 §4.2 (15–25 min). O cartão mostra por
  quê / o quê / custo em CA / o que se perde, com Criar decreto · Agora não · Nunca. Só remanejamentos contam como
  repetição (preencher vagas no começo não dispara sugestão).
- **CA sempre visível** na barra de cima ("CA 0/4"), em alerta quando estoura; o painel de decretos mostra o custo de
  cada decreto. Sobrecarga: o reeve atrasa e falha (determinístico), e o livro de contas diz isso.
- **Build de jogador:** exportar revelou um crash que só existe sem o painel de debug (null == null no HUD) —
  corrigido; o build Linux passa no smoke test exportado. Windows exportado, não executado (sem Windows aqui).
- **Resposta:** comandos continuam instantâneos (mesmo tick). Sem som/animação de "carimbo" ao aceitar (F3).
- **Ritmo:** cartão de objetivo nunca volta atrás (antes regredia quando o jogador esvaziava um campo).
- **Top 3 para o dono testar:** (1) a faixa sugerida pelo reeve (estoque observado, ex.: lenha 60–80) faz sentido ou
  deveria considerar o inverno? (2) o custo em CA está claro antes de aceitar? (3) 4 de CA no começo é apertado o
  bastante para ser uma escolha?
