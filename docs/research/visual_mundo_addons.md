# Visual do mundo: addons e shaders sugeridos (pesquisa de 09/10/2026)

> Pedido do dono: usar os assets de `art/vendor_raw/` (em especial o **Stylized Nature MegaKit**), grama com o
> *Stylized Cartoon Grass*, o *Open Stylized 3D*, a *Stylized Water* e o **Sky3D**, para um mundo rico e vivo.
> Aplicação: etapa 4 do briefing (visual do mundo). Godot do projeto: **4.7 mono**.

| Recurso | Licença | Godot | O que é | Encaixe no Ironvale |
|---|---|---|---|---|
| **Stylized Nature MegaKit** (Quaternius, já em `art/vendor_raw/`) | CC0 | — | Árvores, pinheiros, arbustos, flores, tufos, pedras, cogumelos | **Base das florestas e da decoração.** Já usado no look-dev aprovado (TEST_VILLAGE: `Clusters.gd` MultiMesh + `TreeImpostors.gd` + paleta global). Falta levar para o mapa gerado (cada árvore do sim = 1 instância) |
| [Stylized Cartoon Grass](https://godotshaders.com/shader/stylized-cartoon-grass/) (dip) | MIT (código) | 4.x (relato de ajuste em 4.3) | Shader spatial unshaded para lâminas em MultiMesh: cor de topo/raiz em espaço-mundo, vento, até 4 texturas de lâmina | Alternativa/complemento ao nosso `GrassCarpet` (aprovado no look-dev). Usar a ideia de cor por espaço-mundo + variantes de lâmina; o espalhamento continua nosso (máscara do sim: sem grama em água, estrada, campo, obra) |
| [Open Stylized 3D](https://www.godotengine.org/asset-library/asset/4716) ([GitHub](https://github.com/ZvRzyan18/OpenStylized3D-Godot-Addon-)) | MIT | 4.5 (1.3.2) | Nós de espalhamento (grama/folhas com onda, billboard) + material em camadas com máscaras | Avaliar na etapa 4 para folhas/arbustos com onda; risco: alvo 4.5 (testar em 4.7) e "use poucas camadas" |
| [Stylized Water for Godot 4.x](https://godotshaders.com/shader/stylized-water-for-godot-4-x/) | MIT (código) | 4.x (fix de reverse-Z a partir de 4.3) | Cor por profundidade (Beer), espuma na borda por diferença de profundidade, ondas por textura, refração, normal animada | **Base da água** (costa e lagos): ajustar a espuma para reverse-Z, cores da paleta cozy, faixa de areia já vem do sim |
| [Sky3D](https://store.godotengine.org/asset/tokisangames/sky3d/) ([GitHub](https://github.com/TokisanGames/Sky3D)) | MIT | 4.3+ (v2.1, marcado instável) | Ciclo dia/noite: sol, lua e estrelas, atmosfera, névoa e **nuvens** que mudam com a hora; controle de luz e exposição; tempo de jogo | **Céu, luz e nuvens.** Mesmo autor do Terrain3D (já no projeto). Ligar a hora ao relógio do sim (1 dia = 10 s) e as nuvens/escurecimento ao clima do sim (chuva amanhã = nuvens chegando). GDScript (como o Terrain3D, dirigido pela view) |

## Decisões propostas para a etapa 4
1. Florestas e decoração com o Stylized Nature pelo pipeline já aprovado (MultiMesh por bloco + impostores); árvore
   cortada vira `ENV_Stump_A`; outono por instância.
2. Grama: manter o `GrassCarpet` (aprovado) e incorporar do Stylized Cartoon Grass a cor de topo/raiz em espaço-mundo
   e as variantes de lâmina; testar o Open Stylized 3D para folhagem com onda.
3. Água: shader próprio a partir do Stylized Water (MIT), com a correção de reverse-Z.
4. Céu: Sky3D ligado ao relógio e ao clima do sim; chuva com partículas.
5. Todos os addons entram por `scripts/setup_vendor.py` (originais em `art/vendor_raw/`), com crédito em `CREDITS.md`
   e origem em `docs/vendor_sources.md`.
