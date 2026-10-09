namespace Ironvale.Sim.Systems;

/// <summary>
/// Daily needs of the colonists under direct control (GDD v0.3 §4.5), after the households': fires burn firewood in
/// the cold seasons; each colonist eats from the nearest storage; in autumn a tent or a burning fire keeps them warm,
/// in winter they need both. Hunger or cold for balance.leaveAfterDeficitDays and the colonist leaves (cozy: nobody
/// dies). Tents are filled in id order. Nothing to do on the flat map.
/// </summary>
public sealed class UnitNeedsSystem : ISimSystem
{
    public string Name => "unit_needs";
    public Phase Phase => Phase.Needs;
    public Frequency Frequency => Frequency.Daily;

    public void Run(World w, in Calendar cal)
    {
        if (w.Terrain is null) return;
        var bal = w.Content.Balance;
        bool cold = cal.Season is Season.Autumn or Season.Winter;
        int firewood = w.Content.Resource("firewood").Index, food = w.Content.Resource("food").Index;

        foreach (var fire in w.Buildings.Where(b => b.IsActive && b.Def.Has(BuildingRole.Fire)))
        {
            if (!cold) { fire.Burning = false; continue; }
            var need = Qty.Units(bal.CampfireFirewoodPerDay);
            fire.Burning = w.TryTakeExactFromStorages(firewood, need, fire.Center);
            if (fire.Burning) w.RecordConsumed(firewood, need, fromStorage: true);
        }
        AssignShelters(w);
        bool fireBurning = w.Buildings.Any(b => b.IsActive && b.Burning && b.Def.Has(BuildingRole.Fire));

        foreach (var u in w.Units.Where(u => u.IsColonist && u.Controllable).ToList())
        {
            var meal = w.TakeFromStorages(food, bal.FoodPerMemberPerDay, u.Pos);
            w.RecordConsumed(food, meal, fromStorage: true);
            u.FoodDeficitDays = meal >= bal.FoodPerMemberPerDay ? 0 : u.FoodDeficitDays + 1;

            bool sheltered = u.ShelterId != 0;
            bool warm = !cold || (cal.IsWinter ? sheltered && fireBurning : sheltered || fireBurning);
            u.ColdDeficitDays = warm ? 0 : u.ColdDeficitDays + 1;

            if (u.FoodDeficitDays > bal.LeaveAfterDeficitDays) w.RemoveUnit(u, "fome");
            else if (u.ColdDeficitDays > bal.LeaveAfterDeficitDays) w.RemoveUnit(u, "frio");
        }
    }

    private static void AssignShelters(World w)
    {
        var tents = w.Buildings.Where(b => b.IsActive && b.Def.Has(BuildingRole.Shelter)).OrderBy(b => b.Id).ToList();
        foreach (var u in w.Units.Where(u => u.ShelterId != 0 && tents.All(t => t.Id != u.ShelterId))) u.ShelterId = 0;
        foreach (var u in w.Units.Where(u => u.IsColonist && u.Controllable && u.ShelterId == 0).OrderBy(u => u.Id))
        {
            var room = tents.FirstOrDefault(t => w.Units.Count(o => o.ShelterId == t.Id) < t.Def.ShelterCapacity);
            if (room is null) break;
            u.ShelterId = room.Id;
        }
    }
}
