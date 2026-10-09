namespace Ironvale.Sim.Save;

// Save DTOs. Decoupled from runtime classes so the save schema only changes on purpose.
// Any change here must bump SaveSerializer.CurrentVersion. Marco 2A decision: older saves are rejected
// with a clear message (no migrations while there are no players).
// Resources and defs are stored by string id (never by index) so content reordering doesn't break saves.

public sealed class SaveFile
{
    public string Format { get; set; } = SaveSerializer.FormatId;
    public int SaveVersion { get; set; }
    public string GameVersion { get; set; } = "";
    public string ContentHash { get; set; } = "";
    public string CreatedUtc { get; set; } = "";
    public StateDto State { get; set; } = new();
    public TelemetryDto? Telemetry { get; set; }
}

public sealed class StateDto
{
    public ulong Seed { get; set; }
    public long Tick { get; set; }
    public long StartTick { get; set; }
    public string ScenarioId { get; set; } = "";
    public int MapWidth { get; set; }
    public int MapHeight { get; set; }
    public int NextId { get; set; }
    public List<RngDto> Rng { get; set; } = new();
    public List<HouseholdDto> Households { get; set; } = new();
    public List<BuildingDto> Buildings { get; set; } = new();
    public List<CarrierDto> Carriers { get; set; } = new();
    public List<ShipmentDto> Shipments { get; set; } = new();
    public List<PolicyDto> Policies { get; set; } = new();
    public LedgerDto Ledger { get; set; } = new();
    public List<PolicyLogDto> PolicyLog { get; set; } = new();
    /// <summary>Road cells as indices (y × width + x), ascending.</summary>
    public List<int> Roads { get; set; } = new();
    public List<PlayerActionDto> PlayerActions { get; set; } = new();
    public SuggestionDto? Suggestion { get; set; }
    /// <summary>Resource id → tick until which suggestions are muted (long.MaxValue = never).</summary>
    public SortedDictionary<string, long> SuggestionMuted { get; set; } = new(StringComparer.Ordinal);
}

public sealed class PlayerActionDto
{
    public long Tick { get; set; }
    public string Resource { get; set; } = "";
    public long StockUnits { get; set; }
}

public sealed class SuggestionDto
{
    public int Id { get; set; }
    public string Resource { get; set; } = "";
    public long Min { get; set; }
    public long Max { get; set; }
    public int Actions { get; set; }
    public long AverageStockUnits { get; set; }
    public long OfferedTick { get; set; }
}

public sealed class RngDto
{
    public string Name { get; set; } = "";
    public ulong State { get; set; }
    public ulong Inc { get; set; }
}

public sealed class HouseholdDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Members { get; set; }
    public int Workers { get; set; }
    public int HomeId { get; set; }
    public int JobBuildingId { get; set; }
    public string AssignedBy { get; set; } = "";
    public int AssignedByPolicyId { get; set; }
    public int ToolCondition { get; set; }
    public int FoodDeficitDays { get; set; }
    public int ColdDeficitDays { get; set; }
    public int Productivity { get; set; }
    public string State { get; set; } = "";
    public int ToolHoursToday { get; set; }
    public int BuildSiteId { get; set; }
    public long GardenFoodToday { get; set; }
}

public sealed class BuildingDto
{
    public int Id { get; set; }
    public string Def { get; set; } = "";
    public int X { get; set; }
    public int Y { get; set; }
    public int Rotation { get; set; }
    public bool Active { get; set; }
    public long BuildWorkMilli { get; set; }
    public SortedDictionary<string, long> Stock { get; set; } = new(StringComparer.Ordinal);
    public SortedDictionary<string, long> Reserved { get; set; } = new(StringComparer.Ordinal);
    public long Incoming { get; set; }
    public SortedDictionary<string, long> InStock { get; set; } = new(StringComparer.Ordinal);
    public SortedDictionary<string, long> InReserved { get; set; } = new(StringComparer.Ordinal);
    public long InIncoming { get; set; }
    public int[] Slots { get; set; } = Array.Empty<int>();
    public string? Recipe { get; set; }
    public long SeasonalWorkMilli { get; set; }
    public SortedDictionary<string, long> RemainderMicro { get; set; } = new(StringComparer.Ordinal);
}

public sealed class CarrierDto
{
    public int Id { get; set; }
    public int HouseholdId { get; set; }
    public int BaseId { get; set; }
    public string Phase { get; set; } = "";
    public int X { get; set; }
    public int Y { get; set; }
    public int TargetX { get; set; }
    public int TargetY { get; set; }
    public int NextX { get; set; }
    public int NextY { get; set; }
    public int StepTicks { get; set; }
    public int WaitTicks { get; set; }
    public int PickupId { get; set; }
    public int DropoffId { get; set; }
    public string? Resource { get; set; }
    public long Amount { get; set; }
    public int ShipmentId { get; set; }
}

public sealed class ShipmentDto
{
    public int Id { get; set; }
    public string Resource { get; set; } = "";
    public long Amount { get; set; }
    public int FromId { get; set; }
    public int ToId { get; set; }
    public int CarrierId { get; set; }
}

public sealed class PolicyDto
{
    public int Id { get; set; }
    public string Def { get; set; } = "";
    public bool Enabled { get; set; }
    public string Resource { get; set; } = "";
    public long Min { get; set; }
    public long Max { get; set; }
    public long CreatedTick { get; set; }
    public string LastBlockedReason { get; set; } = "";
}

public sealed class LedgerDto
{
    public SortedDictionary<string, long> Initial { get; set; } = new(StringComparer.Ordinal);
    public SortedDictionary<string, long> Produced { get; set; } = new(StringComparer.Ordinal);
    public SortedDictionary<string, long> Consumed { get; set; } = new(StringComparer.Ordinal);
}

public sealed class PolicyLogDto
{
    public long Tick { get; set; }
    public int PolicyId { get; set; }
    public string Key { get; set; } = "";
    public string[] Args { get; set; } = Array.Empty<string>();
}

/// <summary>Telemetry is saved so graphs continue after load, but excluded from the state hash.</summary>
public sealed class TelemetryDto
{
    public List<string> Resources { get; set; } = new();
    public List<DailyDto> Daily { get; set; } = new();
    public List<MonthlyDto> Monthly { get; set; } = new();
    public long[] TodayProduced { get; set; } = Array.Empty<long>();
    public long[] TodayConsumed { get; set; } = Array.Empty<long>();
    public long[] MonthProduced { get; set; } = Array.Empty<long>();
    public long[] MonthConsumed { get; set; } = Array.Empty<long>();
    public bool ActivityToday { get; set; }
    public int FrozenDays { get; set; }
    public bool Deadlocked { get; set; }
    public long DeadlockDay { get; set; }
}

public sealed class DailyDto
{
    public long Day { get; set; }
    public long[] Produced { get; set; } = Array.Empty<long>();
    public long[] Consumed { get; set; } = Array.Empty<long>();
    public long[] Stored { get; set; } = Array.Empty<long>();
    public long[] Local { get; set; } = Array.Empty<long>();
    public long[] Transit { get; set; } = Array.Empty<long>();
    public int Population { get; set; }
}

public sealed class MonthlyDto
{
    public long MonthIndex { get; set; }
    public long[] Produced { get; set; } = Array.Empty<long>();
    public long[] Consumed { get; set; } = Array.Empty<long>();
    public long[] StoredAtEnd { get; set; } = Array.Empty<long>();
    public int PopulationAtEnd { get; set; }
}
