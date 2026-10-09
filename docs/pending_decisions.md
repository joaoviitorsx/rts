# Decisões pendentes (para o dono revisar)

> Registradas durante o trabalho autônomo. Cada uma: contexto, opções, escolha e motivo. A escolha é sempre a mais
> simples e reversível; quando possível fica atrás de um parâmetro em `godot/data/balance.json`.

## 2A.1 — Construção consumindo material

### P1. Quem leva o material até a obra? (prioridade média)
- **Contexto:** o material sai do armazém e vai até a obra. Hoje só carregadores (famílias designadas ao Salão/Celeiro) fazem isso.
- **Opções:** (a) só carregadores; (b) os próprios construtores buscam o material quando não há carregador; (c) a obra puxa o material de forma abstrata.
- **Escolha:** (a). Sem carregador a obra para, com motivo no painel ("ninguém carregando material — designe carregadores")
  e alerta na tela. Ensina o carregador cedo e mantém a economia física.
- **Reverter:** (b) é um sistema novo (construtor vira carregador temporário); (c) quebraria a tese do estoque físico.
- **Risco:** jogador novo posiciona casas e nada acontece. O item 5 (abertura) deve começar com 1 carregador ou dar esse objetivo primeiro.

### P2. Prioridade dos carregadores (prioridade baixa)
- **Escolha:** (1) produção de recurso com decreto em falta → (2) material de obra (obra mais antiga primeiro) → (3) buffer de
  produtor mais cheio. Autorregula: sem material no armazém, os carregadores voltam a esvaziar produtores.

### P3. Trabalho da obra (prioridade baixa)
- **Escolha:** `buildDays` = dias de trabalho de 1 família a 100%; até `maxBuildersPerSite` (3) famílias somam. Famílias sem emprego
  ajudam a obra mais próxima que pode avançar (`autoBuilders` em `balance.json`, 1 = ligado). A obra não avança além da fração de
  material entregue; o material só é consumido na conclusão (cancelar devolve tudo ao armazém). Construtores gastam ferramenta.
- **Construtor designado pelo jogador** é liberado ao fim da obra (não vira trabalhador do edifício).

