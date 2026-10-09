namespace Ironvale.Sim.Population;

public enum AssignmentSource
{
    /// <summary>No owner: policies may take this household.</summary>
    None,
    /// <summary>Set by the player: policies never touch it.</summary>
    Player,
    /// <summary>Set by a policy (see <see cref="Household.AssignedByPolicyId"/>).</summary>
    Policy,
}

public enum HouseholdState
{
    /// <summary>Working at a producer during its productive season.</summary>
    Working,
    /// <summary>Employed as a carrier at a storage building.</summary>
    Hauling,
    /// <summary>No productive job: gathers a minimum for its own needs (anti-deadlock floor).</summary>
    Subsisting,
    /// <summary>Working on a construction site (assigned by the player, or helping because it has no job).</summary>
    Building,
}

/// <summary>Family: the atomic unit of population (GDD §5.1).</summary>
public sealed class Household
{
    public int Id { get; internal set; }
    public string Name { get; internal set; } = "";
    public int Members { get; internal set; }
    public int Workers { get; internal set; }
    public int HomeId { get; internal set; }
    /// <summary>Building where the household works (its current "ofício"). 0 = none.</summary>
    public int JobBuildingId { get; internal set; }
    public AssignmentSource AssignedBy { get; internal set; }
    public int AssignedByPolicyId { get; internal set; }
    public int ToolCondition { get; internal set; }
    public int FoodDeficitDays { get; internal set; }
    public int ColdDeficitDays { get; internal set; }
    public int ProductivityPermille { get; internal set; } = Permille.One;
    public HouseholdState State { get; internal set; } = HouseholdState.Subsisting;
    /// <summary>Hours worked today with tools (drives daily tool wear).</summary>
    public int ToolHoursToday { get; internal set; }
    /// <summary>Construction site this household works on this hour (0 = none). Recomputed hourly.</summary>
    public int BuildSiteId { get; internal set; }

    public bool HasJob => JobBuildingId != 0;
}
