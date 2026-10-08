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

    private static void AssignHomes(World w)
    {
        var occupants = new Dictionary<int, int>();
        foreach (var h in w.Households)
            if (h.HomeId != 0) occupants[h.HomeId] = occupants.GetValueOrDefault(h.HomeId) + 1;

        foreach (var h in w.Households)
        {
            if (h.HomeId != 0) continue;
            foreach (var b in w.Buildings)
            {
                if (!b.IsActive || !b.Def.Has(BuildingRole.Housing)) continue;
                int used = occupants.GetValueOrDefault(b.Id);
                if (used >= b.Def.HousingCapacity) continue;
                h.HomeId = b.Id;
                occupants[b.Id] = used + 1;
                break;
            }
        }
    }
}
