# Referência: terreno, penhascos e rochas integrados (estilo Lands of Koastalia)

> Texto de pesquisa enviado pelo dono em 09/10/2026 (briefing "nova direção"), salvo como recebido, com títulos
> ajustados. Uso: etapa 4 do briefing (visual do mundo). Resumo das regras que valem para o projeto no fim (§9).

## 1. Ideia central

O segredo é não tratar penhascos como simples objetos colocados sobre o terreno. Recomendação: um **Hybrid
Procedural Terrain System**, que combina:

- **Terrain Mesh:** gera a superfície do mundo.
- **Cliff Mesh Generator:** cria paredes rochosas acompanhando as elevações.
- **Rock Scatter:** distribui rochas decorativas.
- **Material Blending:** mistura os materiais de solo e pedra nas regiões de contato.

## 2. Geometria

Uma região elevada (platô) tem o contorno identificado, e uma parede rochosa liga a borda ao nível inferior. O solo
superior e a parede **compartilham a mesma borda geométrica**; o material esconde a transição.

Algoritmo:
1. Gerar um mapa de alturas.
2. Calcular a inclinação.
3. Identificar regiões que devem ter penhascos.
4. Extrair os contornos das regiões elevadas.
5. Gerar segmentos de geometria ligando a parte superior à inferior.
6. Adicionar irregularidades com ruído e triangulação.
7. Aplicar o material de pedra, misturando-o com o solo perto da borda.

O penhasco acompanha qualquer contorno, inclusive curvas, bifurcações e formatos irregulares.

### Contour-Based Cliff Generation

A borda superior é uma linha de pontos 3D (P0…Pn). O algoritmo cria uma segunda linha na base (B0…Bn) e triangula
as duas, formando uma parede contínua. Para evitar paredes lisas: deslocar os vértices **internos** na horizontal,
variar subdivisões, triângulos de tamanhos diferentes, faixas com profundidades irregulares, normais controladas
para faces facetadas. **Os vértices da borda superior seguem exatamente o terreno** (sem deslocamento próprio), para
não abrir rachaduras.

## 3. Evitar emendas

| Problema | Solução |
|---|---|
| Buracos entre solo e parede | Compartilhar as posições dos vértices da borda (com chunks: uma fonte única da borda) |
| Contato artificial entre grama e rocha | Shader com máscara de transição |
| Paredes retas demais | Variação controlada nos vértices |
| Repetição das rochas | Detalhes modulares e variação de formas |
| Rochas flutuando | Alinhamento à superfície e interseção controlada |

Misturar cores não resolve uma junção geométrica ruim: primeiro as malhas conectadas ou sobrepostas, depois o
acabamento visual.

## 4. Grama × pedra: triplanar com mistura por inclinação

- Solo plano → grama; inclinação média → solo e rocha; inclinação alta → rocha.
- S = 1 − dot(N, U) (N normal, U vertical); M = smoothstep(0,25; 0,75; S); C = (1 − M)·C_grama + M·C_rocha.
- Máscara de ruído de baixa intensidade para a transição não ficar uniforme.
- Projeção **triplanar** nos penhascos (eixos do mundo, sem depender de UV) para não distorcer.
- Referência aberta: [Unity URP Vertex Blend Triplanar Shader](https://github.com/cathyhlshih/UnityURPVertexBlendTriplanarShader).

## 5. Rochas individuais (Meshy) — três tipos

| Tipo | Uso |
|---|---|
| **Surface Rock** | Rochas isoladas sobre grama, areia ou encosta, com a base parcialmente enterrada |
| **Embedded Rock** | Rochas maiores que parecem emergir do solo; parte abaixo da superfície, o shader mistura no contato |
| **Cliff Detail** | Nas bases, faces e extremidades dos penhascos, para quebrar a regularidade |

Mais avançado: máscara de contato por distância (o solo perto das rochas vira pedregoso aos poucos). O código escolhe
pontos, orienta pela normal e enterra parcialmente.

## 6. Pipeline do mundo

Seed + parâmetros → regras espaciais (áreas construíveis, inclinações, recursos) → resolução de footprint das
construções → gramática + montagem dos módulos → mundo final. O terreno é gerado **antes** das construções; o
gerador de casas recebe inclinação, espaço, elevação, orientação da estrada e colisões.

Terreno inclinado: nivelar só a fundação ou usar fundação adaptativa; nunca deformar paredes/telhados.

| Verificação | Comportamento |
|---|---|
| Inclinação do solo | Aceitar, nivelar dentro de limites ou rejeitar |
| Proximidade do penhasco | Recuo mínimo de segurança |
| Espaço disponível | Define as dimensões possíveis |
| Direção da estrada | Orienta a porta principal |
| Elevação | Escolhe a fundação |
| Colisões | Impede sobreposição com árvores, rochas e estruturas |

## 7. Prompt de rocha para o Meshy

> Stylized pale limestone rock outcrop for a cozy medieval RTS game. Large asymmetrical rock formation with broad
> angular faceted surfaces, irregular upper silhouette and extended lower geometry intended to be partially buried in
> procedural terrain. Warm ivory and light desaturated gray stone, chunky sculpted shapes, no grass, no soil, no
> terrain tile, no pedestal. Isolated 3D asset, handcrafted diorama visual style, low-poly inspired topology, matte
> materials.

Depois de gerar: padronizar dimensões, pivô, materiais e encaixes no Blender.

## 8. MVP sugerido (ordem)

| Prioridade | Sistema | Resultado |
|---|---|---|
| P0 | Heightmap + terreno contínuo | Colinas e terreno natural |
| P0 | Cliff Mesh Generator | Penhascos conectados ao relevo |
| P0 | Material Blending | Transições suaves entre grama e pedra |
| P0 | Vegetation Scatter | Florestas e recursos |
| P1 | Adaptive Foundations | Construções em terreno inclinado |
| P1 | Procedural Roads | Caminhos ligando estruturas |

Penhascos: malhas geradas por contorno em vez de dezenas de peças encaixadas à mão.

## 9. O que vale para o Ironvale (decidido no briefing de 09/10/2026)

- Sim (C#) define terraços, células de penhasco e rampas; a **view** gera a malha de penhasco por contorno
  (marching squares sobre a máscara de nível), compartilhando os vértices da borda com o chão. Ruído/irregularidade
  **só na view** (não muda a simulação).
- Terrain3D para o chão; se as emendas Terrain3D + penhasco não ficarem limpas, propor terreno inteiro como malha própria.
- Shader triplanar com mistura por inclinação + ruído, paleta cozy.
- Rochas nos 3 tipos acima para esconder emendas; peças do Meshy listadas no `asset_manifest.md` §8.
- Construção só em células planas de terraço, com recuo do penhasco e motivo no fantasma vermelho; porta orientada
  para a estrada.
