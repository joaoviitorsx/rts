namespace Ironvale.Sim.Systems;

/// <summary>
/// Daily: moves homeless households into free houses, derives productivity from tools and needs,
/// and makes households leave after too many days hungry or cold (soft failure, GDD v0.2 §6).
/// </summary>
public sealed class NeedsSystem : ISimSystem
{
    public string Name => "needs";
    public Phase Phase => Phase.Needs;
    public Frequency Frequency => Frequency.Daily;

    public void Run(World w, in Calendar cal)
    {
        var bal = w.Content.Balance;
        AssignHomes(w);

        foreach (var h in w.Households.ToArray())
        {
            if (h.FoodDeficitDays >= bal.LeaveAfterDeficitDays)
            {
                w.RemoveHousehold(h, "fome");
                continue;
            }
            if (h.ColdDeficitDays >= bal.LeaveAfterDeficitDays)
            {
                w.RemoveHousehold(h, "frio");
                continue;
            }

            int tool = bal.ToolFactorFloorPermille +
                       (Permille.One - bal.ToolFactorFloorPermille) * h.ToolCondition / Permille.One;
            int needs = Permille.Mul(
                h.FoodDeficitDays > 0 ? bal.HungryFactorPermille : Permille.One,
                h.ColdDeficitDays > 0 ? bal.ColdFactorPermille : Permille.One);
            h.ProductivityPermille = Permille.Mul(tool, needs);
        }
    }

    /// <summary>
    /// Homeless families take the free house nearest to their job (first free one without a job). With
    /// balance.autoRehome, a family with a job moves to a free house that saves enough walking (2A.3).
    /// </summary>
    private static void AssignHomes(World w)
    {
        var occupants = new Dictionary<int, int>();
        foreach (var h in w.Households)
            if (h.HomeId != 0) occupants[h.HomeId] = occupants.GetValueOrDefault(h.HomeId) + 1;
        var houses = w.Buildings.Where(b => b.IsActive && b.Def.Has(BuildingRole.Housing)).ToList();
        if (houses.Count == 0) return;
        var bal = w.Content.Balance;

        foreach (var h in w.Households)
        {
            var job = w.GetBuilding(h.JobBuildingId) is { IsActive: true } j ? j : null;
            if (h.HomeId != 0 && (!bal.AutoRehome || job is null)) continue;

            Building? best = null;
            int bestTicks = int.MaxValue;
            foreach (var b in houses)
            {
                if (b.Id == h.HomeId || occupants.GetValueOrDefault(b.Id) >= b.Def.HousingCapacity) continue;
                if (job is null) { best = b; break; }
                int t = w.Paths.Ticks(b.Center, job.Center);
                if (t < bestTicks) { best = b; bestTicks = t; }
            }
            if (best is null) continue;
            if (h.HomeId != 0)
            {
                var current = w.GetBuilding(h.HomeId);
                if (current is not null && w.Paths.Ticks(current.Center, job!.Center) - bestTicks < bal.RehomeMinGainTicks) continue;
                occupants[h.HomeId] -= 1;
            }
            h.HomeId = best.Id;
            occupants[best.Id] = occupants.GetValueOrDefault(best.Id) + 1;
        }
    }
}
