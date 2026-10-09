using Ironvale.Sim.Buildings;
using Ironvale.Sim.Content;
using Ironvale.Sim.Events;
using Ironvale.Sim.Population;

namespace Ironvale.Sim;

/// <summary>
/// Colonists → families (GDD v0.3 §6), generated maps only: a finished house turns the two nearest free colonists into
/// a household that leaves direct control and joins the 2A model (designation, commute, garden, decrees). New families
/// arrive only while some house has room and the stores hold food for a while (FamilyArrivalSystem).
/// </summary>
public sealed partial class World
{
    /// <summary>Called when a house is completed on a generated map.</summary>
    internal void FormFamily(Building house)
    {
        var pair = _units.Where(u => u.IsColonist && u.Controllable)
            .OrderBy(u => u.Pos.Manhattan(house.Center)).ThenBy(u => u.Id)
            .Take(Content.Balance.FamilyFromColonists).ToList();
        if (pair.Count == 0) return;
        var h = new Household
        {
            Id = NewId(),
            Name = pair.Count == 1 ? pair[0].Name : string.Join(" e ", pair.Select(u => u.Name)),
            Members = pair.Count,
            Workers = pair.Count,
            HomeId = house.Id,
            ToolCondition = Permille.One,
        };
        AddHousehold(h);
        foreach (var u in pair)
        {
            DropLoad(u);
            _units.Remove(u);
        }
        Emit(new FamilyFormed(Tick, h.Id, house.Id, h.Name, pair.Select(u => u.Id).ToArray()));
    }

    /// <summary>Households living in a house (for "is there room?").</summary>
    internal int Occupants(Building house) => _households.Count(h => h.HomeId == house.Id);

    internal Household AddArrivingFamily(Building house, string name, int members)
    {
        var h = new Household
        {
            Id = NewId(),
            Name = name,
            Members = members,
            Workers = members,
            HomeId = house.Id,
            ToolCondition = Permille.One,
        };
        AddHousehold(h);
        Emit(new FamilyArrived(Tick, h.Id, house.Id, name, members));
        return h;
    }
}
