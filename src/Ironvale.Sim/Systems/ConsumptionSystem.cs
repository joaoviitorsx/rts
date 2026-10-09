namespace Ironvale.Sim.Systems;

/// <summary>
/// Daily food (all year) and firewood (winter) per household. The yard garden covers part of the food (2A.3). Households without productive work
/// cover part of their needs by subsistence (anti-deadlock floor, GDD §5.3); the rest comes from
/// the nearest storage (abstract withdrawal — physical distribution is out of Marco 1 scope).
/// </summary>
public sealed class ConsumptionSystem : ISimSystem
{
    public const string FoodId = "food";
    public const string FirewoodId = "firewood";

    public string Name => "consumption";
    public Phase Phase => Phase.Consumption;
    public Frequency Frequency => Frequency.Daily;

    public void Run(World w, in Calendar cal)
    {
        var bal = w.Content.Balance;
        int food = w.Content.Resource(FoodId).Index;
        int firewood = w.Content.Resource(FirewoodId).Index;

        foreach (var h in w.Households)
        {
            var near = w.HomeCellOf(h);
            bool subsisting = h.State == HouseholdState.Subsisting;

            var foodNeed = bal.FoodPerMemberPerDay * h.Members;
            bool fed = Consume(w, h, food, foodNeed, subsisting ? bal.SubsistenceFoodCoverPermille : 0, near,
                w.GardenFood(h, cal), out var garden);
            h.GardenFoodToday = garden;
            h.FoodDeficitDays = fed ? 0 : h.FoodDeficitDays + 1;

            if (cal.IsWinter)
            {
                int heat = h.HomeId == 0 ? bal.HomelessHeatPermille : Permille.One;
                var woodNeed = bal.FirewoodPerHouseholdPerWinterDay.MulPermille(heat);
                bool warm = Consume(w, h, firewood, woodNeed, subsisting ? bal.SubsistenceFirewoodCoverPermille : 0, near,
                    Qty.Zero, out _);
                h.ColdDeficitDays = warm ? 0 : h.ColdDeficitDays + 1;
            }
            else
            {
                h.ColdDeficitDays = 0;
            }
        }
    }

    /// <summary>Need covered by subsistence, then the family's own garden, then storage.</summary>
    private static bool Consume(World w, Household h, int r, Qty need, int subsistencePermille, Cell near,
        Qty gardenCanGive, out Qty garden)
    {
        var gathered = need.MulPermille(subsistencePermille);
        garden = Qty.Min(gardenCanGive, need - gathered);
        gathered += garden;
        w.RecordProduced(r, gathered, economic: false);
        w.RecordConsumed(r, gathered, fromStorage: false);

        var rest = need - gathered;
        var taken = w.TakeFromStorages(r, rest, near);
        w.RecordConsumed(r, taken, fromStorage: true);
        return taken >= rest;
    }
}
