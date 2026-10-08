namespace Ironvale.Sim.Systems;

/// <summary>Decides, each hour, whether a household works, hauls or only subsists.</summary>
public sealed class HouseholdStateSystem : ISimSystem
{
    public string Name => "household_state";
    public Phase Phase => Phase.Production;
    public Frequency Frequency => Frequency.Hourly;

    public void Run(World w, in Calendar cal)
    {
        foreach (var h in w.Households) Refresh(w, h, cal);
    }

    internal static void Refresh(World w, Household h, in Calendar cal)
    {
        var b = w.GetBuilding(h.JobBuildingId);
        h.State = b switch
        {
            null => HouseholdState.Subsisting,
            { IsStorage: true, IsActive: true } => HouseholdState.Hauling,
            _ when b.IsProductiveIn(cal.Season) => HouseholdState.Working,
            _ => HouseholdState.Subsisting,
        };
    }
}
