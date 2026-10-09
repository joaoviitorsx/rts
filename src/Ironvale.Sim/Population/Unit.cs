namespace Ironvale.Sim.Population;

public enum UnitKind { Colonist, Ox }

/// <summary>What the player told a unit to do (GDD v0.3 §4.2). Targets: a cell, a ground item, an animal or a building.</summary>
public enum OrderKind
{
    Move,
    /// <summary>Tree, loose stone, bush or mushrooms at <see cref="UnitOrder.Cell"/>.</summary>
    Gather,
    /// <summary>A ground item (felled log, carcass, dropped load).</summary>
    Pickup,
    Hunt,
    Build,
    Deposit,
    Scare,
    /// <summary>Split logs into firewood at a storage.</summary>
    Split,
}

public readonly record struct UnitOrder(OrderKind Kind, Cell Cell, int TargetId);

/// <summary>Where the unit is in its current order.</summary>
public enum UnitStep
{
    Idle,
    /// <summary>Walking to the order's place (or chasing).</summary>
    Going,
    /// <summary>Doing the work (chopping, gathering, shooting, building, splitting).</summary>
    Working,
    /// <summary>Taking the load to the nearest storage before resuming the order.</summary>
    Delivering,
    /// <summary>Fetching construction material from a storage.</summary>
    Fetching,
    /// <summary>Scared by a wolf: runs to the nearest storage, then idles.</summary>
    Fleeing,
}

/// <summary>
/// A colonist or the ox under the player's direct control (GDD v0.3 §4, "degrau 0"). Moves cell by cell, carries one
/// resource at a time (the load lives only here, never in two places) and does what its order says. A colonist who
/// joins a family (step 3) leaves direct control (<see cref="HouseholdId"/> ≠ 0).
/// </summary>
public sealed class Unit
{
    public int Id { get; internal set; }
    public UnitKind Kind { get; internal set; }
    public string Name { get; internal set; } = "";
    public Cell Pos { get; internal set; }
    /// <summary>Cell being walked into (== <see cref="Pos"/> when standing).</summary>
    public Cell Next { get; internal set; }
    /// <summary>Ticks left to reach <see cref="Next"/>.</summary>
    public int StepTicks { get; internal set; }
    public UnitOrder? Order { get; internal set; }
    internal List<UnitOrder> Queue { get; } = new();
    public IReadOnlyList<UnitOrder> Queued => Queue;
    public UnitStep Step { get; internal set; }
    /// <summary>Progress of the current work, in ticks.</summary>
    public int WorkTicks { get; internal set; }
    /// <summary>Where the load goes / material comes from (building id), or 0.</summary>
    public int HelperId { get; internal set; }
    public int CarryResource { get; internal set; } = -1;
    public Qty CarryAmount { get; internal set; }
    /// <summary>Nothing of the same kind left nearby after the last node: waits with a "?" (GDD v0.3 D4).</summary>
    public bool Confused { get; internal set; }
    public int FoodDeficitDays { get; internal set; }
    public int ColdDeficitDays { get; internal set; }
    /// <summary>Tent sheltering this colonist (0 = none).</summary>
    public int ShelterId { get; internal set; }
    /// <summary>Family this colonist joined (0 = under direct control).</summary>
    public int HouseholdId { get; internal set; }

    /// <summary>Last node worked (auto-continue looks around it) and its kind (None = no auto-continue pending).</summary>
    public Cell LastNode { get; internal set; }
    public NodeKind ContinueKind { get; internal set; }

    public bool IsColonist => Kind == UnitKind.Colonist;
    public bool IsCarrying => CarryResource >= 0 && CarryAmount.IsPositive;
    public bool IsMoving => Next != Pos;
    public bool Controllable => HouseholdId == 0;
}
