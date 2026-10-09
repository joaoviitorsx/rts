using Ironvale.Sim.Content;
using Ironvale.Sim.Time;

namespace Ironvale.Sim.Systems;

/// <summary>
/// Growth by free house (GDD v0.3 §6, D7 — Manor Lords): every <c>familyArrivalDays</c> days, if some house has room
/// and the stores hold food for <c>familyArrivalFoodDays</c> days of everyone, a family of 2–4 arrives and moves in.
/// Generated maps only; stateless (the day number decides), so saves need nothing new.
/// </summary>
public sealed class FamilyArrivalSystem : ISimSystem
{
    private static readonly string[] Names =
    {
        "Família Moreira", "Família Ferreira", "Família do Vale", "Família Rocha", "Família Carvalho", "Família Ribeiro",
        "Família Pinheiro", "Família da Ponte", "Família Lobo", "Família Serra", "Família Barros", "Família Fontes",
    };

    public string Name => "families";
    public Phase Phase => Phase.Needs;
    public Frequency Frequency => Frequency.Daily;

    public void Run(World w, in Calendar cal)
    {
        if (w.Terrain is null) return;
        var bal = w.Content.Balance;
        long day = (cal.Tick - w.StartTick) / SimTime.TicksPerDay;
        if (day == 0 || day % bal.FamilyArrivalDays != 0) return;

        var house = w.Buildings.Where(b => b.IsActive && b.Def.Has(BuildingRole.Housing) && w.Occupants(b) < b.Def.HousingCapacity)
            .OrderBy(b => b.Id).FirstOrDefault();
        if (house is null) return;

        int mouths = w.Households.Sum(h => h.Members) + w.Units.Count(u => u.IsColonist && u.Controllable);
        int members = 2 + (int)((day / bal.FamilyArrivalDays) % 3);
        var need = bal.FoodPerMemberPerDay * ((mouths + members) * bal.FamilyArrivalFoodDays);
        if (w.StorageStock(w.Content.Resource("food").Index) < need) return;

        w.AddArrivingFamily(house, Names[(int)(day / bal.FamilyArrivalDays % Names.Length)], members);
    }
}
