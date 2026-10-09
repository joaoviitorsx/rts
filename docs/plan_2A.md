# Plano do Marco 2A (aprovado em 08/10/2026)

> Ordem fixa: cada item depende do anterior. Incrementos jogáveis: ao fim de cada item, build sem avisos, todos os
> testes verdes (determinismo, save/load, soak de 50 anos), smoke test, commit e push da branch.
> Passe de feel (medido) após 3, 5 e 6; os 15 min reais são jogados pelo dono. Se o tempo apertar, terminar 1→5
> bem feitos em vez de começar o 6 pela metade.

## Estado de partida
Custo da obra é descontado ao posicionar; o decreto já tem histerese fixa (25%); não há estrada nem pathfinding
(carregadores andam em "L"); casa = primeira livre; `MvpOpening` é um jogador roteirizado (soak e CLI).

## 1. Construção consome madeira e pedra
Posicionar cria uma obra com demanda de material (nada é descontado na hora). Carregadores levam o material do
armazém até a obra, com prioridade logo depois das faltas urgentes de decreto. A obra só avança com material entregue
**e** trabalho: famílias desocupadas ajudam a obra mais próxima automaticamente (parâmetro em `balance.json`), e o
jogador pode designar famílias de propósito. Cancelar devolve ao armazém o material entregue (ledger fecha). Painel:
"Madeira 8/15 (3 a caminho) · 2 construtores · parada: falta pedra". Testes: sem material não avança, cancelamento
devolve, ledger conserva, save.

## 2. Decreto com faixa mín/máx
Mínimo e máximo explícitos: abaixo do mín recruta, acima do máx libera, entre os dois não faz nada. UI mostra o
estado ("abaixo do mínimo: recrutando"). Textos do livro de contas viram entradas estruturadas (chave + argumentos)
para a UI traduzir. Testes: sem oscilação, validação mín < máx, save.

## 3. Trajeto + fora da estrada + horta + cenários da CLI
Estrada = peça 1×1 barata; pathfinding A* no grid com custo menor na estrada e maior no gramado; cache por par
casa→trabalho invalidado quando o mapa muda. Tempo produtivo = turno (10 h) − ida e volta. Carregadores usam o mesmo
A*. Ao trocar de emprego, a família muda para a casa livre mais perto. Horta: comida para a própria família no tempo
livre (quem mora longe tem menos). Painel do edifício: "% do turno em trajeto" + tooltip com causa e solução; painel
da família: tempo livre e horta. View: aldeões indo e voltando pelo caminho. CLI: `--player passive|naive|optimal`,
CSV de % de trajeto, produção perdida e tempo livre, com/sem estrada; `docs/balance_report.md`. Passe de feel.

## 4. Ferreiro + decreto de ferramentas
Receita madeira + pedra → ferramentas; entrega de insumo armazém → produtor (reaproveita a entrega de obra). Decreto
"manter ferramentas entre mín e máx". Painel: "parado: sem pedra".

## 5. Abertura sem roteiro
O jogo deixa de aplicar o `MvpOpening` (fica como jogador "optimal" da CLI). Ajustar carroça, mês inicial e taxas pelos
cenários: ingênuo não perde família no ano 1; crise da lenha no 1º outono; passivo sofre. Cartão de "próximo
objetivo" sempre visível. Passe de feel.

## 6. Capacidade Administrativa + sugestão de decreto
CA base do Salão; cada decreto ativo consome CA; acima do limite o reeve avalia com atraso e às vezes erra
(determinístico). O jogo observa designações manuais para produtores de recurso baixo; após 3 parecidas propõe
"Manter X entre A e B" com o que acontece, custo em CA e o que se perde de controle (GDD v0.2 §3.2). Telas: cartão de
sugestão (§3.3), medidor de CA, painel do reeve + livro de contas, com decreto/reeve no `ui.csv`; `UI_UX_guide.md`
atualizado junto. Passe de feel, build Windows (+ Linux) e roteiro de playtest (GDD §8.3 + UI_UX_guide §9.2).

## Riscos (aceitos)
Saves: subir versão e rejeitar antigos com mensagem clara, sem migração. Balanceamento mais apertado pode quebrar o
soak: reajustar a cada item. A* determinístico e barato (64×64 + cache). Sugestão heurística: calibrar no playtest.
Passe de feel: medir o que dá; os 15 min reais são do dono.
