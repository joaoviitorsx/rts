namespace Ironvale.Sim.Content;

public sealed class ResourceDef
{
    public required int Index { get; init; }
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required Qty CarryPerTrip { get; init; }
}

[Flags]
public enum BuildingRole
{
    None = 0,
    Seat = 1,       // Salão: sede do Lorde
    Housing = 2,    // casa
    Producer = 4,   // lenhador, campo
    Storage = 8,    // celeiro, Salão (pilha da carroça); vagas = carregadores
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
    public required int HousingCapacity { get; init; }

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
    public required int CarrierTicksPerCell { get; init; }
    public required int CarrierLoadTicks { get; init; }
    public required Qty MinPickup { get; init; }
    public required int HarvestVariancePermille { get; init; }
    public required int DeadlockWindowDays { get; init; }
    public required int PolicyLogMax { get; init; }
    /// <summary>Families without a job help the nearest construction site that can progress.</summary>
    public required bool AutoBuilders { get; init; }
    public required int MaxBuildersPerSite { get; init; }
}

public sealed class ScenarioBuilding
{
    public required BuildingDef Def { get; init; }
    public required Cell Origin { get; init; }
    public required int Rotation { get; init; }
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
    public required int StartMonth { get; init; }
    public required IReadOnlyList<ScenarioBuilding> Buildings { get; init; }
    public required Qty[] Stock { get; init; }                 // placed in the first storage building
    public required IReadOnlyList<ScenarioHousehold> Households { get; init; }
    public required int HouseholdToolCondition { get; init; }
}
