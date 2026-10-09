# Referência: construções procedurais por módulos (BACKLOG)

> Texto de pesquisa enviado pelo dono em 09/10/2026, resumido e salvo. **Backlog:** casas procedurais ficam para
> depois do primeiro playtest (briefing D6). Kit de módulos = **Quaternius Medieval Village MegaKit** (não gerar
> paredes no Meshy).

## 1. Ideia

Dois sistemas complementares: **geração procedural de construções** (paredes, telhados, portas, janelas combinados
por regras, casas diferentes mas coerentes) e **geração procedural do terreno** (ver
`terreno_penhascos_referencia.md`). Lands of Koastalia usa grade irregular e deixa desenhar uma área para gerar a
construção; os detalhes internos não são públicos — a proposta reproduz os princípios com gramáticas, sockets e
malhas por contorno.

## 2. Kit de módulos

```
Buildings/Medieval_House/
├── Foundation/  FOUNDATION_Straight, FOUNDATION_Corner
├── Walls/       WALL_Solid_A/B, WALL_Window_A, WALL_Door_A, WALL_Corner
├── Roof/        ROOF_Slope_A, ROOF_Ridge, ROOF_Gable_End, ROOF_Hip_Corner
└── Details/     DETAIL_Chimney, DETAIL_WoodBeam, DETAIL_WindowShutter
```

**Contrato geométrico** (valores ilustrativos): parede 2 m de largura × 2,5 m de altura, módulo de 2 m de
profundidade, telhado a 40°, parede de 0,2 m, pivô padronizado, conectores em posições e orientações exatas. Peças
geradas por IA não garantem medidas nem topologia: ajustar no Blender.

## 3. Gramática + sockets

- A **gramática** descreve a estrutura válida; os **sockets** dizem quais conexões físicas são permitidas
  (`SOCKET_WALL_RIGHT` só liga a um socket compatível, na orientação certa; extremidade de telhado nunca no meio).
- Regra: footprint válido → fundação → paredes externas (exatamente 1 entrada principal, janelas onde permitido,
  cantos fechados) → telhado compatível → acessórios opcionais → validação.
- Exemplo aberto: [ProceduralGeneration (Unity)](https://github.com/evesfect/ProceduralGeneration) — sockets,
  compatibilidade e sorteio ponderado.

## 4. Hierarquia de regras (não sortear tudo)

- **Obrigatório:** paredes fechadas, cobertura válida, uma entrada acessível, sem colisões.
- **Condicional:** chaminé só em telhado compatível, janela só em parede externa, anexo só com espaço.
- **Aleatório:** cores, janelas secundárias, comprimento permitido, ornamentos, pequenas assimetrias.

## 5. Integração com o terreno

O terreno vem antes; o resolvedor de footprint recebe inclinação, espaço, elevação, direção da estrada e colisões.
Inclinação: nivelar só a fundação ou fundação adaptativa (suportes), nunca deformar paredes e telhados.

## 6. Ordem sugerida

P0 gramática de casas retangulares · P1 fundações adaptativas, footprints irregulares (L, anexos), estradas
procedurais · P2 telhados complexos. Não começar com Wave Function Collapse (útil depois para padrões locais).

## 7. Prompt de exemplo (parede modular, só se um dia faltar módulo)

> Stylized cozy medieval RTS modular wall segment. Single rectangular timber-frame wall with warm ivory plaster and
> thick exposed wooden beams. Designed as one reusable module in a procedural building kit. Straight vertical sides,
> flat top and bottom, consistent wall thickness, no roof, no foundation, no doors, no windows, no surrounding
> environment. Chunky handcrafted cartoon proportions, matte clay-like materials, simplified low-poly inspired
> geometry. Isolated full 3D object.
