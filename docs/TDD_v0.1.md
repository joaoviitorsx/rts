# TDD v0.1 — Technical Design Doc (Ironvale / Sociedade Autônoma)

> **Status:** 🟡 Rascunho para aprovação · 08/10/2026
> **Depende de:** `GDD_sociedade_autonoma.md` (§7 arquitetura), `GDD_v0.2_core_loop_5h.md` (§1 tempo, §7 frequências)
> **Escopo:** Marco 1 — núcleo da simulação headless + view cinza mínima.
> **Precedência:** as "decisões posteriores" do briefing (3D cozy/cartoon, C#, PC, primitivas) valem mais que GDD e Bible.
> **Atenção:** `asset_production_bible_mvp.md` **não está no repositório**. Tudo que dependeria dela (§14 pastas, nomes, pivôs, escala) está aqui como **proposta** e marcado com 📎. Ver dúvida Q1.

---

## 0. Estado atual do ambiente (verificado)

| Item | Situação | Impacto |
|---|---|---|
| Godot | `godot` → **4.7.stable.official** (build *padrão*, sem .NET) | **Bloqueante para C# na view.** Precisa do build *Godot 4.7 .NET* (`..._mono_linux_x86_64`). |
| .NET SDK | **não instalado** (`dotnet` ausente) | **Bloqueante** para compilar sim e testes. Precisa do .NET SDK 8 (mínimo do Godot 4.7 .NET). |
| Git | pasta **não é repositório** | Precisa `git init` antes dos commits do Marco 1. |
| MCP godot-devpilot | config global aponta `GODOT_MCP_PROJECT_ROOT` para `Games/runeway`; addon não está em `rts/addons/` | Precisa apontar para `rts` e copiar/ativar o plugin. Ver §9. |
| Assets | só **Quaternius Stylized Nature MegaKit (Standard, CC0)** em `assets/` — glTF + FBX + OBJ (118 MB) | Faltam KayKit e LOWPO Villager. FBX/OBJ duplicados vão inflar import → ver §1.2. |
| Docs | `GDD_sociedade_autonoma.md` e `GDD_v0.1_sociedade_autonoma.md` são idênticos exceto o título | Ver Q16. |

---

## 1. Estrutura de pastas 📎

Godot .NET exige o `.csproj` do jogo na raiz, com o mesmo nome do projeto. A simulação vive em um **projeto .NET separado**, sem referência ao GodotSharp, e o projeto do jogo referencia ele.

```text
rts/
├── project.godot
├── Ironvale.sln                     # sim + testes + cli + jogo
├── Ironvale.csproj                  # assembly do jogo (Godot.NET.Sdk) → só código de view
├── .gitignore / .gitattributes / .editorconfig
│
├── src/                             # (.gdignore) — Godot não importa nada daqui
│   ├── Ironvale.Sim/                # net8.0, ZERO dependência de Godot
│   │   ├── Core/          World, EntityId, Qty, Rng (PCG32), StateHasher
│   │   ├── Time/          SimClock, Calendar, Frequency, Scheduler, ISimSystem
│   │   ├── Content/       ContentDb, *Def (ResourceDef, BuildingDef, RecipeDef, PolicyDef), ContentLoader (JSON)
│   │   ├── Map/           GridMap, Cell, Footprint
│   │   ├── Economy/       Stockpile, Ledger (conservação)
│   │   ├── Population/    Household, Needs
│   │   ├── Buildings/     Building, ConstructionSite
│   │   ├── Jobs/          JobSlot, JobAssignment
│   │   ├── Logistics/     HaulTask, Carrier, Shipment
│   │   ├── Policies/      Policy, KeepAbovePolicy, PolicyLog
│   │   ├── Systems/       ProductionSystem, TransportSystem, ConsumptionSystem, NeedsSystem, ToolWearSystem, PolicySystem, SubsistenceSystem
│   │   ├── Commands/      ISimCommand + comandos, CommandQueue, CommandResult
│   │   ├── Events/        SimEvent + tipos (BuildingPlaced, ShipmentStarted, …)
│   │   ├── Telemetry/     ResourceSeries, TelemetryRecorder, DeadlockDetector
│   │   └── Save/          SaveModel (DTOs), SaveSerializer, Migrations/
│   └── Ironvale.Sim.Cli/            # runner headless: `dotnet run -- --seed 42 --years 50 --csv out/`
│
├── tests/                           # (.gdignore)
│   └── Ironvale.Sim.Tests/          # xUnit
│
├── data/                            # conteúdo data-driven (JSON) — lido pela sim, empacotado no export
│   ├── resources.json
│   ├── buildings.json
│   ├── recipes.json
│   ├── policies.json
│   ├── balance.json                 # constantes globais (consumo/dia, desgaste, capacidade de carga…)
│   └── scenarios/mvp_start.json     # 6 famílias, Salão, estoque da carroça
│
├── game/                            # camada VIEW (Godot)
│   ├── scenes/        main.tscn, world/world_view.tscn, ui/hud.tscn, ui/debug_panel.tscn, ui/build_bar.tscn, ui/building_panel.tscn
│   ├── scripts/       SimHost.cs, WorldView.cs, CameraRig.cs, AgentRenderer.cs, BuildController.cs, ui/*.cs
│   ├── visuals/       visual_catalog.json (defId → primitiva ou .glb), VisualFactory.cs
│   └── materials/     placeholders cinza por função
│
├── assets/                          # packs de terceiros, intocados, um subdir por pack
│   └── third_party/quaternius_stylized_nature/ (só glTF + Textures; FBX/OBJ com .gdignore)
│
├── tools/                           # scripts auxiliares (ex.: dump de telemetria → gráfico)
└── docs/
```

### 1.1 Regras do csproj do jogo
- `Ironvale.csproj` faz `<Compile Remove="src/**;tests/**;tools/**" />` (o SDK do Godot inclui todo `**/*.cs` por padrão) e `<ProjectReference Include="src/Ironvale.Sim/Ironvale.Sim.csproj" />`.
- `Ironvale.Sim` e `Ironvale.Sim.Tests` compilam e rodam **só com o .NET SDK**, sem Godot instalado.
- `src/` e `tests/` têm `.gdignore` para o editor não varrer `bin/obj`.

### 1.2 Assets
- Proposta: mover `assets/Stylized Nature MegaKit[Standard]/` → `assets/third_party/quaternius_stylized_nature/` (sem espaço/colchete), manter **só glTF + Textures**, colocar `.gdignore` em `FBX/`, `FBX (Unity)/`, `OBJ/`, previews, e manter o `License_Standard.txt`.
- Cada pack novo (KayKit, LOWPO) segue o mesmo padrão `assets/third_party/<autor>_<pack>/`.

---

## 2. Princípios aplicados (resumo)

| Princípio | Como o código garante |
|---|---|
| Sim separada da view | `Ironvale.Sim` não referencia GodotSharp (o compilador impede). |
| Tick fixo 10/s | `SimHost` acumula `delta × speed` e chama `World.Step()` N vezes; a sim não conhece segundos, só ticks. |
| Determinismo | Quantidades em inteiro (ponto fixo), RNG próprio por sistema, iteração só em listas ordenadas por id, sem `DateTime`/`Dictionary`-enumeração/threads dentro do passo. |
| Data-driven | Defs vêm de `data/*.json`; o código só conhece *tipos* de comportamento (produtor, armazém, moradia), não "lenhador". |
| Save versionado | Envelope com `saveVersion` + cadeia de migrações desde v1. |
| View só lê | View recebe `IReadOnlyWorld` + fila de `SimEvent`; escreve só via `ISimCommand`. |
| Main thread | Sim roda na main thread no jogo normal; "avançar N anos" roda em `Task` com a view congelada e **sem** tocar a SceneTree (§5.4). |

---

## 3. Modelo de dados da simulação

### 3.1 Tipos base

```csharp
readonly record struct EntityId(int Value);          // por tipo; nunca reutilizado; 0 = nulo
readonly record struct Qty(long Milli);              // 1 unidade = 1000 milli. Sem float no estado.
readonly record struct ResourceId(ushort Index);     // índice na tabela de ContentDb (ordem estável)
readonly record struct Cell(int X, int Y);
```

- **Ponto fixo** (`Qty`, `Permille` para fatores 0..1000): elimina NaN por construção e dá determinismo cross-platform. Telemetria e UI convertem para `double` só para exibir.
- Multiplicação de taxas: `qty * permille / 1000` com arredondamento explícito (truncar) + resto acumulado por produtor (`Remainder`) para não perder produção fracionária.

### 3.2 Conteúdo (imutável, carregado do JSON)

```jsonc
// resources.json
[{ "id": "wood",     "name": "Madeira",     "carryPerTrip": 10 },
 { "id": "firewood", "name": "Lenha",       "carryPerTrip": 10 },
 { "id": "food",     "name": "Comida",      "carryPerTrip": 15 },
 { "id": "stone",    "name": "Pedra",       "carryPerTrip": 5  },
 { "id": "tools",    "name": "Ferramentas", "carryPerTrip": 5  },
 { "id": "coins",    "name": "Moedas",      "carryPerTrip": 50 }]

// buildings.json (exemplo)
{ "id": "woodcutter", "name": "Lenhador", "footprint": [2,2],
  "cost": { "wood": 20 }, "buildDays": 3,
  "roles": ["producer"], "jobSlots": 2,
  "recipes": ["chop_wood", "split_firewood"],
  "outputCapacity": 40 }

// recipes.json
{ "id": "chop_wood", "outputs": { "wood": 0.25 },     // por trabalhador por hora de sim
  "inputs": {}, "usesTools": true, "seasons": "all" }
{ "id": "grow_food", "kind": "seasonal_harvest",      // campo: acumula trabalho, colhe no outono
  "outputs": { "food": 0.8 }, "workSeasons": ["spring","summer"], "harvestSeason": "autumn" }
```

`ContentDb` congela tudo em arrays indexados; ids string → índices numa ordem estável (ordem do arquivo). O save grava o **hash do conteúdo** e os ids string (não índices), para continuar legível se a ordem mudar.

### 3.3 World (raiz do estado)

```text
World
├── Seed : ulong
├── Tick : long                      (fonte única do tempo; Calendar é derivado)
├── Content : ContentDb              (NÃO serializado; referenciado por hash)
├── Map : GridMap                    (w×h; ocupação por célula → BuildingId)
├── Households : List<Household>     (ordenada por id)
├── Buildings  : List<Building>      (ordenada por id)
├── Carriers   : List<Carrier>
├── Shipments  : List<Shipment>      (recursos em trânsito)
├── Policies   : List<Policy>
├── Rng        : RngStreams          (um PCG32 por sistema, derivados da Seed via SplitMix64)
├── NextIds    : contadores por tipo
├── Telemetry  : TelemetryRecorder   (serializado, fora do hash de estado)
└── PendingEvents : List<SimEvent>   (drenado pela view; fora do save)
```

### 3.4 Calendar (derivado de `Tick`)

| Unidade | Ticks | Fórmula |
|---|---|---|
| hora de sim | **4** (proposta — ver Q3) | 10 "horas" por dia |
| dia | 40 | `Tick / 40` |
| mês | 1 200 (30 dias) | |
| estação | 3 600 (3 meses) | Primavera, Verão, Outono, Inverno |
| ano | 14 400 (12 meses) = 24 min em 1x | |

- `Calendar` é `readonly struct` calculado: `Year, Season, Month, DayOfMonth, HourOfDay, IsWinter`.
- Partida começa no **último mês da primavera** (GDD v0.2 §4.1) → `StartTickOffset` no cenário.
- 50 anos = 720 000 ticks.

### 3.5 Household (família)

```text
Household
├── Id, Name ("Família Halberg")
├── Members : int            (ex.: 4) → define consumo
├── Workers : int            (ex.: 2) → mão de obra que ela entrega ao emprego
├── HomeId : EntityId?       (casa; sem casa = "acampada", penalidade de calor)
├── Job : JobAssignment?     (BuildingId + slot) — o "ofício" atual (ver Q8)
├── AssignedBy : Player | Policy(policyId) | None
├── ToolCondition : Permille (0..1000; 0 = sem ferramenta)
├── Needs
│   ├── FoodDeficitDays : int
│   └── ColdDeficitDays : int
├── State : Idle | Working | Hauling | Subsisting | Leaving
└── Productivity : Permille  (derivado diariamente: ferramentas × necessidades)
```

### 3.6 Building

```text
Building
├── Id, DefId, Origin : Cell, Rotation : 0..3
├── Status : UnderConstruction(progressDays) | Active
├── Output : Stockpile        (estoque físico local; capacidade do def)
├── Input  : Stockpile        (para receitas com insumo; vazio no Marco 1, exceto lenha)
├── Slots  : JobSlot[]        (cada slot → HouseholdId?)
├── ActiveRecipe : RecipeId
├── WorkAccumulator : long    (campo: trabalho acumulado até a colheita; produtor: resto fracionário)
└── Reserved : Stockpile      (quantidade já prometida a um carregador → evita dupla coleta)
```

Papéis (`roles` no JSON) definem quais sistemas olham o edifício: `seat` (Salão), `housing` (casa), `producer` (lenhador, campo), `storage` (celeiro). Celeiro e Salão têm `Stockpile` de armazenamento; o Salão funciona como celeiro inicial (pilha da carroça).

### 3.7 Stockpile

```text
Stockpile
├── Amounts : Qty[]          (indexado por ResourceId — iteração determinística)
├── Capacity : Qty           (total, soma de todos os recursos)
└── Add / TryRemove / Reserve / Release   (nunca ficam negativos; TryRemove falha em vez de clampar)
```

### 3.8 Job

No Marco 1, "Job" = **vaga de trabalho** (`JobSlot`) num edifício ativo + a família que a ocupa. Tipos de vaga vêm do papel do edifício:
- `producer` → trabalha a receita ativa;
- `storage` → **carregador** (família vira `Carrier`, ver 3.9);
- `seat` → (futuro: escrivão/administração; vazio no Marco 1).

Família sem vaga → `SubsistenceSystem`: produz um mínimo de comida e lenha direto para consumo próprio (salvaguarda anti-deadlock do GDD §5.3 — "família nunca fica 100% parada").

### 3.9 Logística (carregadores e trânsito)

```text
Carrier                       (1 por família empregada como carregador)
├── HouseholdId, HomeStorageId
├── Phase : Idle | ToPickup | Loading | ToDropoff | Unloading
├── Path : Cell[] ; PathIndex ; SubStepTicks
└── ShipmentId?

Shipment                      (recurso em trânsito — não está em nenhum estoque)
├── Id, ResourceId, Amount : Qty
├── FromBuildingId, ToBuildingId
└── CarrierId
```

Algoritmo (fase **Transporte**, todo tick):
1. A cada hora de sim, carregadores ociosos escolhem tarefa: produtor com maior `Output` não reservado (desempate por distância, depois por id). Reservam `min(carryPerTrip, disponível)`.
2. Caminham célula a célula (velocidade em ticks/célula, do `balance.json`; sem estradas no Marco 1, grid em linha reta 4-vizinhos — A* só quando houver obstáculos).
3. Na coleta: `Output.Remove` → cria `Shipment` (recurso sai do estoque e passa a existir **apenas** no shipment).
4. Na entrega: `Shipment` → `Storage.Add`; se o armazém estiver cheio, espera e emite alerta.

**Conservação:** `Ledger` soma, por recurso, `Σ estoques + Σ shipments` e checa contra `inicial + produzido − consumido` a cada dia nos testes.

Consumo das famílias (comida, lenha) sai do armazém **mais próximo da casa** de forma abstrata (sem carregador) no Marco 1 — distribuição física é a crise 5, fora do escopo.

### 3.10 Policy

```text
Policy (abstrata)
├── Id, DefId, Enabled, CreatedTick
└── Evaluate(World, PolicyContext) → List<PolicyAction>

KeepAbovePolicy : Policy
├── Resource : ResourceId
├── Threshold : Qty           (N)
├── Hysteresis : Permille     (libera famílias só acima de N × (1 + h); default 25%)
└── MaxHouseholds : int       (teto de realocação)
```

Regras de realocação (fase **Decisões**, frequência: ver Q7):
1. `stock(X)` = soma dos armazéns + shipments a caminho de armazém.
2. Se `stock < N`: escolhe um produtor de X com vaga livre; pega uma família na ordem: ociosa/subsistência → designada por **outra** política com folga → (nunca) designada pelo jogador. Se não houver produtor com vaga, loga "sem vaga para X".
3. Se `stock > N × (1+h)`: devolve uma família que **esta** política designou para subsistência (ou para a política mais faminta).
4. No máximo **1 movimento por política por avaliação** (amortece oscilação).
5. Cada ação gera `PolicyLog` legível: *"Família Halberg → Lenhador #3: lenha 42 < 80"*. (GDD §5.6 exige o administrador explicar decisões.)

Capacidade Administrativa **fora** do Marco 1 (o campo `caCost` já existe no JSON, ignorado).

### 3.11 Telemetria

Por recurso, amostra **diária**: `produced`, `consumed`, `stock` (armazéns), `local` (estoques dos produtores), `inTransit`.
- Buffer circular dos últimos 360 dias (gráfico detalhado) + agregado **mensal** desde o início (600 pontos em 50 anos).
- `DeadlockDetector` (diário): sinaliza se, por 60 dias seguidos, nenhum recurso mudou de estoque, não há shipments e todas as famílias estão `Idle` (sem trabalho nem subsistência).
- Telemetria é serializada no save mas **excluída** do hash de estado.

---

## 4. Tick scheduler

### 4.1 Frequências

```csharp
enum Frequency { Tick, Hourly, Daily, Weekly, Monthly, Seasonal, Yearly }
enum Phase     { Production, Transport, Consumption, Needs, Decisions }   // ordem fixa (GDD v0.2 §7)

interface ISimSystem {
    string Name { get; }
    Phase Phase { get; }
    Frequency Frequency { get; }
    void Run(World world, in Calendar cal);
}
```

`Fires(freq, tick)`: `Tick` sempre; `Hourly` se `tick % 4 == 0`; `Daily` se `tick % 40 == 0`; `Weekly` se `dia % 7 == 0` na virada do dia (ver Q4); `Monthly/Seasonal/Yearly` na virada correspondente. Sem offsets/escalonamento no Marco 1 (o mundo é pequeno).

### 4.2 Passo

```text
World.Step():
  1. ApplyCommands(fila do tick)           ← comandos sempre entram ANTES da sim, em ordem de chegada
  2. para cada Phase em ordem:
       para cada sistema registrado nessa Phase (ordem de registro fixa):
         se Fires(sistema.Frequency, Tick): sistema.Run(world, cal)
  3. Telemetry.OnTickEnd (diário: fecha amostra; roda DeadlockDetector)
  4. Tick++
```

### 4.3 Sistemas do Marco 1

| Sistema | Fase | Freq. | O quê |
|---|---|---|---|
| ConstructionSystem | Production | Daily | avança obras; ativa edifício |
| ProductionSystem | Production | Hourly | produtores: `workers × rate × productivity` → `Output` |
| HarvestSystem | Production | Seasonal | campo: converte trabalho acumulado em comida na colheita |
| SubsistenceSystem | Production | Daily | famílias sem vaga coletam mínimo |
| TransportSystem | Transport | Tick (planejamento Hourly) | carregadores andam, coletam, entregam |
| ConsumptionSystem | Consumption | Daily | famílias consomem comida; lenha só no inverno |
| ToolWearSystem | Consumption | Daily | desgasta `ToolCondition`; troca por ferramenta do armazém se houver |
| NeedsSystem | Needs | Daily | atualiza déficits → `Productivity`; família parte após X dias (ver Q9) |
| PolicySystem | Decisions | ver Q7 | avalia políticas, gera realocações + log |

### 4.4 RNG

- `Pcg32` próprio (não `System.Random`, cuja implementação pode mudar entre versões do .NET).
- `RngStreams`: um stream por sistema, semente = `SplitMix64(worldSeed ^ hash(nomeDoSistema))`. Adicionar um sistema novo não muda a sequência dos outros.
- Estado dos streams entra no save.
- No Marco 1, RNG é usado em pouco lugar (nomes de famílias, variação de colheita ±10%).

---

## 5. Camada de comandos (view → sim) e leitura (sim → view)

### 5.1 Comandos

```csharp
interface ISimCommand { }   // records imutáveis, serializáveis
record PlaceBuilding(string DefId, Cell Origin, int Rotation) : ISimCommand;
record CancelConstruction(EntityId BuildingId) : ISimCommand;
record AssignHousehold(EntityId HouseholdId, EntityId BuildingId) : ISimCommand;   // marca AssignedBy = Player
record UnassignHousehold(EntityId HouseholdId) : ISimCommand;
record SetRecipe(EntityId BuildingId, string RecipeId) : ISimCommand;               // lenhador: madeira ↔ lenha
record CreatePolicy(string DefId, string ResourceId, long Threshold) : ISimCommand;
record SetPolicyEnabled(EntityId PolicyId, bool Enabled) : ISimCommand;
record RemovePolicy(EntityId PolicyId) : ISimCommand;
```

- `World.Enqueue(cmd)` → aplicado no início do próximo `Step()`, com o número do tick gravado.
- Validação dentro da sim (célula livre, custo disponível, vaga livre…) → `CommandResult` como `SimEvent` (`CommandRejected{reason}`).
- **Velocidade/pausa não são comandos**: pertencem ao `SimHost` (não mudam o resultado, só o ritmo).
- `CommandLog` (tick + comando) opcional → permite *replay* determinístico (seed + log = mesma partida) e reprodução de bugs.

### 5.2 Leitura

- `IReadOnlyWorld`: interfaces só-leitura sobre listas e estoques (`IReadOnlyList<IHouseholdView>` etc.). A view nunca recebe as classes mutáveis.
- `SimEvent`s (ex.: `BuildingPlaced`, `BuildingCompleted`, `HouseholdAssigned`, `ShipmentStarted/Delivered`, `PolicyAction`, `HouseholdLeft`, `Alert`) são drenados pela view uma vez por frame para criar/destruir nós.
- Posição dos agentes: a view lê `Carrier.Path/PathIndex/SubStepTicks` e interpola com `SimHost.TickAlpha` (fração entre ticks) para movimento suave.

### 5.3 SimHost (Node C#, view)

```text
_Process(delta):
  if paused: return
  acc += delta * speed            (speed ∈ {1,2,4,8})
  n = min(floor(acc / 0.1), MaxTicksPerFrame)   ← evita "espiral da morte"
  repeat n: world.Step()
  acc -= n * 0.1
  DrainEvents(); Notify view
```

### 5.4 "Avançar N anos"
- Pausa a view, roda `world.Step()` em `Task.Run` (thread de trabalho), sem nenhuma chamada à SceneTree.
- Progresso exposto por campo `volatile`; a UI faz *polling* na main thread.
- Ao terminar: view descarta eventos intermediários e **reconstrói** a cena a partir do estado (`WorldView.RebuildFromState()`).

---

## 6. Formato do save

```jsonc
{
  "format": "ironvale-save",
  "saveVersion": 1,                 // inteiro; muda a cada alteração de schema
  "gameVersion": "0.1.0",
  "contentHash": "fnv64:9f3c…",     // hash de data/*.json; diferente → aviso, não bloqueia
  "createdUtc": "2026-10-08T…",     // metadado; fora do hash
  "state": {
    "seed": 42, "tick": 123456,
    "rng": { "production": [s, inc], … },
    "nextIds": { … },
    "map": { "w": 64, "h": 64 },
    "households": [ { "id": 1, "name": "…", "members": 4, … } ],
    "buildings":  [ { "id": 3, "def": "woodcutter", "origin": [10,12], "output": { "wood": 12500 }, … } ],
    "carriers": [ … ], "shipments": [ … ], "policies": [ … ]
  },
  "telemetry": { … }                // opcional na carga
}
```

- Serialização via **DTOs** dedicados (`SaveModel`), não via as classes de runtime → o schema do save muda só quando decidimos.
- `System.Text.Json` com source generator (sem reflexão, rápido), saída **GZip** → `user://saves/<slot>.ivsave`. A sim recebe/devolve `byte[]`/`Stream`; caminho de arquivo é problema da view.
- Recursos e defs gravados por **id string**; `Qty` em milli (inteiro).
- Migrações: `ISaveMigration { int From; JsonNode Migrate(JsonNode) }` em cadeia v1→v2→…; teste de fixture para cada versão antiga guardada em `tests/fixtures/saves/`.
- `StateHasher`: serialização canônica do `state` → FNV-1a 64. Usado nos testes de determinismo e save/load (e exibível no painel de debug).

---

## 7. Estratégia de testes

Projeto `tests/Ironvale.Sim.Tests` (xUnit), roda com `dotnet test` sem Godot.

| Grupo | Testes |
|---|---|
| Unitários | Calendar (conversões, virada de estação/ano); Scheduler (contagem de execuções por frequência em 1 ano: 14 400 / 3 600 / 360 / 12 / 4 / 1); Qty/Permille (arredondamento); Stockpile (nunca negativo, capacidade, reserva); ContentLoader (JSON inválido → erro claro com arquivo e campo) |
| Sistemas | Produção por trabalhador e por ferramenta; colheita sazonal; desgaste → queda de produtividade; consumo diário e lenha só no inverno; transporte (recurso sai do produtor, existe só no shipment, chega ao celeiro após `distância × velocidade` ticks); política (realoca abaixo de N, devolve acima de N×(1+h), não toca famílias do jogador, 1 movimento por avaliação) |
| Comandos | Validação (célula ocupada, sem recurso, vaga cheia) → `CommandRejected`; comando aplicado no tick seguinte |
| Invariantes | `Ledger` de conservação por recurso todo dia; nenhum `Qty` negativo |
| **Determinismo** | Duas `World` com mesma seed + mesmo `CommandLog` → `StateHash` igual após 5 anos; seeds diferentes → hash diferente (sanidade) |
| **Save/load** | Rodar 2 anos → salvar → carregar → rodar 3 anos ≡ rodar 5 anos direto (hash igual). Também roundtrip `save(load(save(w))) == save(w)` byte a byte |
| **Soak** (`[Trait("Category","Soak")]`) | 50 anos com cenário MVP + políticas padrão. Falha se: `DeadlockDetector` disparar; qualquer valor negativo; conservação violada; população zerada (a definir — Q9). Gera CSV da telemetria como artefato |

- `dotnet test` roda tudo, inclusive o soak (50 anos = 720 000 ticks de um mundo pequeno, deve levar segundos). Para iterar rápido: `dotnet test --filter Category!=Soak`.
- CLI `Ironvale.Sim.Cli` para rodar cenários longos e despejar CSV para análise de balanceamento.
- Lado Godot: smoke test via MCP (abrir cena, rodar, checar erros de log, screenshot). Sem framework de teste de UI no Marco 1.

---

## 8. View mínima (Marco 1)

| Peça | Implementação |
|---|---|
| Terreno | `MeshInstance3D` plano (PlaneMesh) + grid desenhado por shader simples/linhas; célula = **2 m** 📎 (Q11) |
| Edifícios | `VisualFactory` lê `game/visuals/visual_catalog.json`: `{"woodcutter": {"primitive":"box","size":[3.6,2,3.6],"color":"#8a6a4a"}}`. Trocar por GLB = trocar a entrada para `{"scene":"res://assets/...glb"}`. A sim nunca muda. |
| Agentes | Um cubo por carregador/trabalhador visível via `MultiMeshInstance3D`; posição interpolada entre ticks |
| Câmera | `CameraRig`: pivô no chão → braço → `Camera3D` perspectiva FOV ~30°, inclinação ~57°; pan (WASD/setas, borda da tela, botão do meio), zoom suave (lerp da distância, roda do mouse), rotação Q/E em passos de 90° com tween. Sem snapping, sem pixel shader. |
| Iluminação | `DirectionalLight3D` + `WorldEnvironment` simples (céu procedural, tonemap neutro) |
| HUD | barra de recursos (armazéns + tendência ↑↓ do dia), data (dia/mês/estação/ano), botões pausa/1x/2x/4x/8x + atalhos 0–4/espaço |
| Debug | tabela por recurso (prod/cons/estoque/local/trânsito, último dia e média 30 dias) + gráfico de linha (Control com `_Draw`), log de políticas, hash de estado, "avançar N anos" |
| Construir | barra de edifícios → fantasma no grid (verde/vermelho) → clique envia `PlaceBuilding`; botão direito/Esc cancela |
| Designar | clicar edifício → painel com vagas e famílias → `AssignHousehold`/`UnassignHousehold`; seletor de receita no lenhador; criar política "manter X acima de N" |

Linguagem: view em **C#** também (acesso direto às interfaces da sim, sem ponte GDScript↔C#). GDScript só se algum widget de UI ficar claramente mais simples.

---

## 9. Uso do MCP godot-devpilot

- Pré-requisitos: copiar `addons/godot_devpilot_mcp/` para `rts/addons/`, ativar o plugin, e apontar `GODOT_MCP_PROJECT_ROOT` para `/home/joaoviitosx/Documentos/Projetos/Games/rts` (hoje aponta para `runeway`; trocar exige reiniciar o Claude Code).
- Uso previsto: criar/editar cenas e nós, rodar o projeto, ler logs/erros do debugger, screenshots do jogo, input simulado para smoke test da câmera e da construção, memória de projeto/ADRs.
- Scripts C# serão escritos direto em arquivo (as ferramentas de script do MCP são orientadas a GDScript).

---

## 10. Fora do escopo do Marco 1

Capacidade Administrativa, sugestão automática de política ("o jogo aprende com você"), mercado/preços, migração, nascimentos/mortes/envelhecimento, Nomeados, instituições, distritos, estradas evoluindo, overlays de fluxo, guerra, sucessão, desbloqueios por condição, crises roteirizadas, arte final, GLBs, áudio.

---

## 11. Dúvidas e conflitos encontrados

### Bloqueios de ambiente
- **Q0. Toolchain.** Preciso de (a) **.NET SDK 8** (`sudo dnf install dotnet-sdk-8.0`) e (b) **Godot 4.7 .NET** (o binário atual é o build padrão, sem C#). Você instala, ou posso baixar o Godot .NET para `~/.local/bin/godot-4.7-mono`? Também posso rodar `git init` aqui?

### Documentos
- **Q1. Bible ausente.** `asset_production_bible_mvp.md` não está no repo. Pode adicionar? Sem ela, uso a estrutura do §1 e escala/nomes provisórios (📎).
- **Q16. GDD duplicado.** `GDD_sociedade_autonoma.md` e `GDD_v0.1_…md` são iguais (o "v0.1" diz "Versão 0.2" no cabeçalho). Qual é o canônico? Posso apagar o outro?
- **Q2. Partes do GDD anuladas pela decisão visual.** Considero obsoletos: §6.1–6.2 inteiros, o SubViewport 640×360 do §7.1, D7, risco "pixel creep", e a fase F0.5 vira "spike de câmera/MultiMesh" sem pixel art. Confirma?

### Tempo
- **Q3. "Hora de jogo" não divide o dia.** 1 dia = 40 ticks; 24 horas não cabem. Proposta: "hora de sim" = 4 ticks (10 por dia). Alternativas: dia = 48 ticks (4,8 s) com 24 h de 2 ticks, ou abandonar "hora" e usar "a cada 4 ticks".
- **Q4. Semana vs mês de 30 dias.** Semana de 7 dias atravessa meses. Proposta: semanal = a cada 7 dias absolutos (nenhum sistema do Marco 1 usa semanal).
- **Q5. 8x travado até a 1ª delegação** (GDD v0.2 §1). No Marco 1 deixo 8x sempre liberado (é ferramenta de debug). OK?

### Economia (sem Economy Sheet ainda — todos os números serão placeholders em `balance.json`)
- **Q6a. Lenha:** de onde vem? Proposta: o lenhador tem duas receitas (madeira | lenha) escolhidas pelo jogador/política. Alternativa: lenha é feita a partir de madeira (insumo) em outro edifício.
- **Q6b. Pedra:** sem pedreira no Marco 1. Fica só como estoque inicial e custo de construção?
- **Q6c. Ferramentas:** sem ferreiro, o desgaste só leva a zero. Proposta: estoque inicial na carroça + piso de produtividade sem ferramenta (ex.: 50%). Ou incluo um ferreiro mínimo (madeira+pedra → ferramentas)?
- **Q6d. Moedas:** sem mercado, qual o papel? Proposta: só existem como estoque inicial no Salão, sem fluxo no Marco 1.
- **Q6e. Construção:** custo descontado do armazém na hora (abstrato) + N dias de obra sem construtor? Ou o material precisa ser **carregado** até a obra (mais fiel ao pilar 2, mais trabalho)?
- **Q6f. Casa:** quantas famílias por casa? Sem casa → só penalidade de frio, ou também moral/partida?

### População e empregos
- **Q8. "Ofício" da família:** é o emprego atual (o que proponho) ou uma aptidão fixa que dá bônus quando ela trabalha na própria área?
- **Q8b. Família = quantos trabalhadores?** Proposta: família ocupa **1 vaga** (com `Workers` ≈ 2 contando na taxa). Alternativa: cada membro adulto ocupa uma vaga.
- **Q8c. Carregadores:** são famílias empregadas no celeiro (proposta, cada uma vira um agente visível) ou um "pool" abstrato do Salão?
- **Q9. Falha branda:** família parte após N dias de fome/frio? Se sim, o soak test com população zerada conta como falha ou como "fim de jogo" legítimo? Proposta: soak usa cenário com políticas padrão e **exige** população > 0 após 50 anos.

### Políticas
- **Q7. Frequência da política.** GDD diz mensal (2 min em 1x, 30 dias); com o inverno durando 90 dias, uma reação mensal pode ser lenta demais. Proposta: **diária** no Marco 1, com 1 movimento por avaliação; voltar a mensal quando houver CA/administrador.
- **Q7b. Política vs comando do jogador:** a política nunca mexe em família designada manualmente (proposta). Ou o jogador "entrega" famílias à política?

### Mapa e visual
- **Q11. Grid quadrado ou hexagonal?** O GDD (D3) diz tile grid, mas o pack principal de edifícios escolhido é o **KayKit Medieval *Hexagon***. Isso muda o modelo de `Cell`, footprints e pathfinding — melhor decidir agora. Também: tamanho da célula (proponho 2 m) e tamanho do mapa (proponho 64×64).
- **Q12. Assets da Quaternius já presentes:** posso usar árvores/pedras/grama do Stylized Nature como decoração do terreno já no Marco 1 (só view, sem efeito na sim), ou mantenho 100% primitivas? E posso reorganizar a pasta conforme §1.2?
- **Q13. Agentes visíveis:** só carregadores se movem (proposta) e trabalhadores aparecem parados no local de trabalho? Ou também ir e voltar de casa?

### Cenário inicial
- **Q14. Início:** Salão já posicionado pelo cenário ou o jogador escolhe o local (GDD v0.2 §4.1)? Proposta para o Marco 1: Salão pré-posicionado, estoque da carroça dentro dele, 6 famílias acampadas sem emprego.
- **Q15. Determinismo cross-platform:** o requisito é mesma máquina/mesmo build, ou Windows ≡ Linux? Com ponto fixo inteiro (proposta) os dois ficam garantidos; só confirmo para não otimizar com `float` depois.

---

## Changelog
- **v0.1 (08/10/2026)** — primeira versão para aprovação.
