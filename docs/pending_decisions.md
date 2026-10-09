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

## 2A.2 — Decreto com faixa mín/máx

### P4. Faixa padrão e passo da UI (prioridade baixa)
- **Escolha:** ao criar, o máximo sugerido = mínimo × 1,25 (`hysteresisPermille` em `policies.json`, agora "faixa padrão");
  o jogador edita os dois. SpinBox com passo 1 e setas de 10 (passo 10 arredondava 1875 → 1880 e divergia do sim).
- **Validação:** mínimo ≥ 0 e máximo > mínimo; senão o comando é rejeitado com motivo.

### P5. Textos do livro de contas (prioridade média)
- **Contexto:** o log agora é estruturado (chave + argumentos), traduzido pelo `ui.csv` (`log.*`). Os argumentos ainda são
  nomes do conteúdo em português (recurso, edifício) e o trecho "(saiu de X)" vem pronto do sim.
- **Escolha:** manter assim até os nomes de conteúdo virarem chaves `tr()` (pendência de UI já registrada no roadmap).
- **Termos:** o painel ainda se chama "Políticas"; a troca para decreto/reeve é feita no item 6 junto com o `UI_UX_guide.md`
  (decisão registrada). As linhas novas do log já dizem "Decreto".

