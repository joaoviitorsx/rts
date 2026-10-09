namespace Ironvale.Sim.Content;

public sealed class ResourceDef
{
    public required int Index { get; init; }
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required Qty CarryPerTrip { get; init; }
    /// <summary>What one colonist carries per trip (GDD v0.3 §4.3: logs are heavy). Zero = cannot carry it.</summary>
    public Qty ColonistCarry { get; init; }
    /// <summary>What the ox drags per trip (logs and stone only). Zero = the ox does not take it.</summary>
    public Qty OxCarry { get; init; }
}

[Flags]
public enum BuildingRole
{
    None = 0,
    Seat = 1,       // Salão: sede do Lorde
    Housing = 2,    // casa
    Producer = 4,   // lenhador, campo
    Storage = 8,    // celeiro, Salão (pilha da carroça); vagas = carregadores
    Shelter = 16,   // tenda (abrigo de colonos sem família) — GDD v0.3 §10
    Fire = 32,      // fogueira: calor nas estações frias, afasta lobos
}

public sealed class BuildingDef
{
    public required int Index { get; init; }
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required int FootprintW { get; init; }
    public required int FootprintH { get; init; }
    public required bool Buildable { get; init; }
    public required Qty[] Cost { get; init; }              // indexed by resource
    /// <summary>Days of work for one household at 100% productivity (more builders = faster).</summary>
    public required int BuildDays { get; init; }
    public required BuildingRole Roles { get; init; }
    public required int JobSlots { get; init; }
    public required IReadOnlyList<RecipeDef> Recipes { get; init; }
    public required Qty OutputCapacity { get; init; }
    public required Qty StorageCapacity { get; init; }
    /// <summary>Administrative capacity this building provides while active (Salão; later scribes, chapel).</summary>
    public required int AdminCapacity { get; init; }
    /// <summary>Input buffer of a producer whose recipes consume inputs (smithy), refilled by carriers.</summary>
    public required Qty InputCapacity { get; init; }
    public required int HousingCapacity { get; init; }
    /// <summary>Generated maps (GDD v0.3 §9): what the workers take from the world. None on the flat map's rules.</summary>
    public HarvestSource Harvests { get; init; }
    /// <summary>Storage open to the weather (ground pile): food and firewood spoil on rainy days (GDD v0.3 §5).</summary>
    public bool Uncovered { get; init; }
    /// <summary>Only offered on generated maps (the RTS opening's campfire, depot, tent).</summary>
    public bool GeneratedOnly { get; init; }
    /// <summary>Colonists this shelter houses (tent).</summary>
    public int ShelterCapacity { get; init; }
    /// <summary>Cells (Chebyshev) around the building the workers reach for <see cref="Harvests"/> = trees.</summary>
    public int WorkRadius { get; init; }

    public bool Has(BuildingRole role) => (Roles & role) != 0;

    /// <summary>Sum of the construction cost (capacity of the site's material stock).</summary>
    public Qty TotalCost
    {
        get
        {
            long sum = 0;
            foreach (var q in Cost) sum += q.Milli;
            return new Qty(sum);
        }
    }

    /// <summary>Stock capacity of a building of this type.</summary>
    public Qty StockCapacity =>
        Has(BuildingRole.Storage) ? StorageCapacity :
        Has(BuildingRole.Producer) ? OutputCapacity : Qty.Zero;
}

public enum RecipeKind
{
    /// <summary>Produces every hour while staffed.</summary>
    Continuous,
    /// <summary>Accumulates work in work seasons; converted at the start of the next season (harvest).</summary>
    Seasonal,
}

public sealed class RecipeDef
{
    public required int Index { get; init; }
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required RecipeKind Kind { get; init; }
    public required Qty[] OutputPerWorkerHour { get; init; }  // indexed by resource
    /// <summary>Units of each input consumed per unit of output (indexed by resource; zero = not an input).</summary>
    public required Qty[] InputPerOutput { get; init; }
    public bool HasInputs => InputPerOutput.Any(q => q.IsPositive);
    public required bool UsesTools { get; init; }
    public required bool[] WorkSeasons { get; init; }         // indexed by Season

    public bool Produces(int resource) => OutputPerWorkerHour[resource].IsPositive;

    public bool IsWorkSeason(Season season) => WorkSeasons[(int)season];
}

public enum PolicyKind { KeepAbove }

public sealed class PolicyDef
{
    public required int Index { get; init; }
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required PolicyKind Kind { get; init; }
    public required int CaCost { get; init; }              // Capacidade Administrativa: ignored in Marco 1
    /// <summary>Default band for a new decree in the UI: Max = Min × (1 + this).</summary>
    public required int HysteresisPermille { get; init; }
    public required int MaxHouseholds { get; init; }
    /// <summary>CA per resource (indexed by resource); defaults to <see cref="CaCost"/>.</summary>
    public required int[] CaCostByResource { get; init; }

    public int CaCostFor(int resource) => CaCostByResource[resource];
}

/// <summary>Global tuning constants (data/balance.json). Placeholders until the Economy Sheet exists.</summary>
public sealed class BalanceDef
{
    public required Qty FoodPerMemberPerDay { get; init; }
    public required Qty FirewoodPerHouseholdPerWinterDay { get; init; }
    public required int HomelessHeatPermille { get; init; }
    public required int ToolWearPerWorkDayPermille { get; init; }
    public required int ToolFactorFloorPermille { get; init; }
    public required int HungryFactorPermille { get; init; }
    public required int ColdFactorPermille { get; init; }
    public required int LeaveAfterDeficitDays { get; init; }
    public required int SubsistenceFoodCoverPermille { get; init; }
    public required int SubsistenceFirewoodCoverPermille { get; init; }
    /// <summary>Ticks to walk into a road cell / an open (off-road) cell.</summary>
    public required int TicksPerCellRoad { get; init; }
    public required int TicksPerCellOffroad { get; init; }
    /// <summary>Stone paid per road cell when it is laid.</summary>
    public required Qty RoadStonePerCell { get; init; }
    /// <summary>Commute time = walking ticks × this ‰ (0 = no commute).</summary>
    public required int CommuteTicksPermille { get; init; }
    public required bool AutoRehome { get; init; }
    /// <summary>Daily free hours of a family that lives next to its work (commute eats into them).</summary>
    public required int GardenFreeHours { get; init; }
    public required Qty GardenFoodPerHour { get; init; }
    /// <summary>Minimum one-way saving, in walking ticks, for a family to move house.</summary>
    public required int RehomeMinGainTicks { get; init; }
    public required int CarrierLoadTicks { get; init; }
    public required Qty MinPickup { get; init; }
    /// <summary>Food (and firewood in autumn/winter) is urgent for carriers below this many days of need in storage.</summary>
    public required int HaulUrgentDays { get; init; }
    public required int HarvestVariancePermille { get; init; }
    public required int DeadlockWindowDays { get; init; }
    public required int PolicyLogMax { get; init; }
    public required int AdminOverloadDelayDays { get; init; }
    public required int AdminOverloadErrorPermille { get; init; }
    public required int SuggestAfterActions { get; init; }
    public required int SuggestWindowDays { get; init; }
    public required int SuggestSnoozeDays { get; init; }
    /// <summary>Suggested minimum for firewood/food covers at least this share of a winter's demand (0 = off).</summary>
    public required int SuggestWinterCoverPermille { get; init; }
    /// <summary>Families without a job help the nearest construction site that can progress.</summary>
    public required bool AutoBuilders { get; init; }
    public required int MaxBuildersPerSite { get; init; }

    // ---- world as a resource (GDD v0.3 §7): generated maps only
    /// <summary>Days from sapling to a mature (cuttable) tree; young from a quarter of it.</summary>
    public required int TreeMatureDays { get; init; }
    /// <summary>Days a stump stays before a sapling sprouts.</summary>
    public required int TreeStumpDays { get; init; }
    /// <summary>Wood (units) a mature tree gives when felled.</summary>
    public required int TreeWood { get; init; }
    public required int LooseStoneUnits { get; init; }
    public required int BushFood { get; init; }
    public required int MushroomFood { get; init; }
    public required int OutcropUnits { get; init; }
    public required int OreUnits { get; init; }
    /// <summary>Units multiplier of the map's rich deposit.</summary>
    public required int RichDepositFactor { get; init; }
    /// <summary>Site clearing: household-hours to fell a tree / remove a bush or stone under a new building.</summary>
    public required int ClearTreeHours { get; init; }
    public required int ClearNodeHours { get; init; }
    /// <summary>Harvest walk: output lost per cell between the building and the tree being felled (‰), and its cap.</summary>
    public required int HarvestWalkPermillePerCell { get; init; }
    public required int HarvestWalkMaxPermille { get; init; }

    // ---- RTS opening (GDD v0.3 §4–§10): units, gathering, fauna, weather. Generated maps only.
    /// <summary>Walking speed vs. the carriers' step costs (‰): 1667 = a colonist needs 5 ticks per off-road cell.</summary>
    public required int ColonistStepPermille { get; init; }
    public required int OxStepPermille { get; init; }
    public required int ChopTicks { get; init; }
    public required int GatherTicks { get; init; }
    public required int HandleTicks { get; init; }
    public required int AutoContinueCells { get; init; }
    /// <summary>Construction work of one colonist per hour, in ‰ of a household's (1000).</summary>
    public required int ColonistBuildPermille { get; init; }
    public required int SplitTicksPerUnit { get; init; }
    public required int HuntRangeCells { get; init; }
    public required int HuntShotTicks { get; init; }
    public required int HuntHitPermille { get; init; }
    public required int DeerFood { get; init; }
    public required int DeerHides { get; init; }
    public required int RabbitFood { get; init; }
    public required int DeerFleeCells { get; init; }
    public required int RabbitFleeCells { get; init; }
    public required int WolfThreatCells { get; init; }
    public required int WolfScareGroup { get; init; }
    /// <summary>Chance of rain per day by season (spring, summer, autumn, winter — snow in winter), ‰.</summary>
    public required int[] RainPermille { get; init; }
    /// <summary>Day (from the start) by which the first rain is guaranteed if none fell yet.</summary>
    public required int FirstRainDay { get; init; }
    public required int OpenPileSpoilPermille { get; init; }
    public required int CampfireFirewoodPerDay { get; init; }
}

public sealed class ScenarioBuilding
{
    public required BuildingDef Def { get; init; }
    /// <summary>Absolute cell, or an offset from the generated start when <see cref="AtStart"/>.</summary>
    public required Cell Origin { get; init; }
    public required int Rotation { get; init; }
    public bool AtStart { get; init; }
}

public enum TerrainKind { Flat, Generated }

/// <summary>Where a producer's output comes from on a generated map.</summary>
public enum HarvestSource
{
    None,
    /// <summary>Mature trees within the work radius (woodcutter): felled one by one, they grow back slowly.</summary>
    Trees,
    /// <summary>The outcrop the building stands on (quarry): finite.</summary>
    Outcrop,
}

public sealed class ScenarioUnit
{
    public required string Kind { get; init; }   // "colonist" | "ox"
    public required string Name { get; init; }
}

public sealed class ScenarioHousehold
{
    public required string Name { get; init; }
    public required int Members { get; init; }
    public required int Workers { get; init; }
}

public sealed class ScenarioDef
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required int MapWidth { get; init; }
    public required int MapHeight { get; init; }
    /// <summary>Flat (the 2A map) or generated from the seed (GDD v0.3 §8).</summary>
    public TerrainKind Terrain { get; init; }
    public required int StartMonth { get; init; }
    public required IReadOnlyList<ScenarioBuilding> Buildings { get; init; }
    public required Qty[] Stock { get; init; }                 // placed in the first storage building
    public required IReadOnlyList<ScenarioHousehold> Households { get; init; }
    /// <summary>Colonists and the ox of the RTS opening (GDD v0.3 §3), placed around the start.</summary>
    public IReadOnlyList<ScenarioUnit> Units { get; init; } = Array.Empty<ScenarioUnit>();
    public required int HouseholdToolCondition { get; init; }
}
