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
