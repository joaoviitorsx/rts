namespace Ironvale.Sim.Systems;

/// <summary>
/// Decides, each hour, whether a household works, hauls, builds or only subsists. Families without a job help
/// the nearest construction site that can progress (balance.autoBuilders), up to maxBuildersPerSite per site.
/// </summary>
public sealed class HouseholdStateSystem : ISimSystem
{
    public string Name => "household_state";
    public Phase Phase => Phase.Production;
    public Frequency Frequency => Frequency.Hourly;

    public void Run(World w, in Calendar cal)
    {
        foreach (var h in w.Households) Refresh(w, h, cal);
        if (w.Content.Balance.AutoBuilders) AssignHelpers(w);
    }

    internal static void Refresh(World w, Household h, in Calendar cal)
    {
        h.BuildSiteId = 0;
        var b = w.GetBuilding(h.JobBuildingId);
        if (b is { IsActive: false })
        {
            h.State = HouseholdState.Building;   // assigned to a site by the player
            h.BuildSiteId = b.Id;
            return;
        }
        h.State = b switch
        {
            null => HouseholdState.Subsisting,
            { IsStorage: true, IsActive: true } => HouseholdState.Hauling,
            _ when b.IsProductiveIn(cal.Season) => HouseholdState.Working,
            _ => HouseholdState.Subsisting,
        };
    }

    /// <summary>Jobless households → nearest site that can progress (ties by id), respecting the per-site cap.</summary>
    private static void AssignHelpers(World w)
    {
        List<Building>? sites = null;
        foreach (var b in w.Buildings)
            if (b.CanProgress) (sites ??= new()).Add(b);
        if (sites is null) return;

        int max = w.Content.Balance.MaxBuildersPerSite;
        var count = new Dictionary<int, int>();
        foreach (var h in w.Households)
            if (h.BuildSiteId != 0) count[h.BuildSiteId] = count.GetValueOrDefault(h.BuildSiteId) + 1;

        foreach (var h in w.Households)
        {
            if (h.HasJob || h.State != HouseholdState.Subsisting) continue;
            var home = w.HomeCellOf(h);
            Building? best = null;
            foreach (var s in sites)
            {
                if (count.GetValueOrDefault(s.Id) >= max) continue;
                if (best is null || s.Center.Manhattan(home) < best.Center.Manhattan(home)) best = s;
            }
            if (best is null) return;   // every site is full
            h.BuildSiteId = best.Id;
            h.State = HouseholdState.Building;
            count[best.Id] = count.GetValueOrDefault(best.Id) + 1;
        }
    }
}
