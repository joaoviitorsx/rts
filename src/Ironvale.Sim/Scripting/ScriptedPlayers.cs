namespace Ironvale.Sim.Scripting;

/// <summary>A scripted "player" for balance runs (CLI scenarios, soak tests). Acts only through commands.</summary>
public interface IScriptedPlayer
{
    string Id { get; }
    void Start(World w);
    /// <summary>Called once per day start (after the day's systems ran).</summary>
    void Daily(World w);
}

public static class ScriptedPlayers
{
    public static readonly string[] Ids = { "passive", "naive", "optimal" };
    /// <summary>Players of the RTS opening (generated maps, wild_start).</summary>
    public static readonly string[] RtsIds = { "rts" };

    public static IScriptedPlayer Create(string id, bool roads = true) => id switch
    {
        "passive" => new PassivePlayer(),
        "naive" => new NaivePlayer(),
        "optimal" => new OptimalPlayer(roads),
        "rts" => new RtsPlayer(),
        _ => throw new ArgumentException($"unknown player '{id}' (passive | naive | optimal | rts)"),
    };
}

/// <summary>Does nothing: the families only subsist. Should struggle (GDD v0.2: failure is soft but real).</summary>
public sealed class PassivePlayer : IScriptedPlayer
{
    public string Id => "passive";
    public void Start(World w) { }
    public void Daily(World w) { }
}

/// <summary>
/// A newcomer: builds the basics without thinking about distance (houses far from work), no roads, no decrees.
/// Fills free jobs with idle families, keeps two carriers, and only reacts to firewood when winter is ~30 days away.
/// </summary>
public sealed class NaivePlayer : IScriptedPlayer
{
    public static readonly (string Def, Cell Origin)[] Layout =
    {
        ("house", new Cell(40, 22)),
        ("house", new Cell(43, 22)),
        ("house", new Cell(46, 22)),
        ("woodcutter", new Cell(18, 40)),
        ("woodcutter", new Cell(21, 40)),
        ("field", new Cell(35, 35)),
        ("field", new Cell(39, 35)),
        ("granary", new Cell(30, 35)),
    };

    public string Id => "naive";

    public void Start(World w)
    {
        foreach (var (def, origin) in Layout) Placement.Place(w, def, origin);
        var hall = w.SeatBuilding!;
        foreach (var h in w.Households.Take(2)) w.Enqueue(new AssignHousehold(h.Id, hall.Id));
    }

    public void Daily(World w)
    {
        var cal = w.Calendar;
        // Fill empty jobs: fields first, then woodcutters.
        foreach (var b in w.Buildings.Where(b => b.IsActive && b.IsProducer).OrderBy(b => b.Def.Id == "field" ? 0 : 1).ThenBy(b => b.Id))
        {
            if (b.FreeSlotIndex() < 0) continue;
            var idle = w.Households.FirstOrDefault(h => !h.HasJob);
            if (idle is null) break;
            w.Enqueue(new AssignHousehold(idle.Id, b.Id));
        }
        // Late reaction to the firewood alert (~30 days before winter): woodcutters switch to firewood and the field
        // hands, idle in autumn, are moved there — the kind of repeated move the reeve learns from (GDD v0.2 §3.2).
        int daysToWinter = ((int)Season.Winter * SimTime.MonthsPerSeason - cal.MonthOfYear) * SimTime.DaysPerMonth - cal.DayOfMonth;
        var firewood = w.Content.Resource("firewood").Index;
        if (daysToWinter is > 0 and <= 30 && w.StorageStock(firewood) < Qty.Units(w.Households.Count * 60))
        {
            foreach (var cutter in w.Buildings.Where(b => b.IsActive && b.Def.Id == "woodcutter" && b.Recipe?.Id == "chop_wood"))
                w.Enqueue(new SetRecipe(cutter.Id, "split_firewood"));
            var freeSlots = w.Buildings.Where(b => b.IsActive && b.Def.Id == "woodcutter")
                .SelectMany(b => Enumerable.Repeat(b, b.Def.JobSlots - b.AssignedCount)).ToList();
            var fieldHands = w.Households.Where(h => w.GetBuilding(h.JobBuildingId) is { Def.Id: "field" } f
                                                     && !f.IsProductiveIn(cal.Season)).ToList();
            for (int i = 0; i < Math.Min(freeSlots.Count, fieldHands.Count); i++)
                w.Enqueue(new AssignHousehold(fieldHands[i].Id, freeSlots[i].Id));
        }
        // Back to the fields and to wood in spring.
        if (cal.Season == Season.Spring && cal.DayOfMonth == 0 && cal.MonthOfYear == 0)
        {
            foreach (var b in w.Buildings.Where(b => b.IsActive && b.Def.Id == "woodcutter" && b.Recipe?.Id == "split_firewood"))
                w.Enqueue(new SetRecipe(b.Id, "chop_wood"));
            var fieldSlots = w.Buildings.Where(b => b.IsActive && b.Def.Id == "field")
                .SelectMany(b => Enumerable.Repeat(b, b.Def.JobSlots - b.AssignedCount)).ToList();
            var cutters = w.Households.Where(h => w.GetBuilding(h.JobBuildingId) is { Def.Id: "woodcutter" }).ToList();
            // Keep one family per woodcutter; the rest go back to the fields.
            var movable = cutters.GroupBy(h => h.JobBuildingId).SelectMany(g => g.Skip(1)).ToList();
            for (int i = 0; i < Math.Min(fieldSlots.Count, movable.Count); i++)
                w.Enqueue(new AssignHousehold(movable[i].Id, fieldSlots[i].Id));
        }
    }
}

/// <summary>
/// The scripted MVP opening (food first, three decrees, carriers) plus roads linking the work areas. In the first
/// winter it adds a quarry and a smithy and runs them by hand (one family each, only while stone / tools are short),
/// staying within the reeve's capacity (2A.6: 3 of 4 CA).
/// </summary>
public sealed class OptimalPlayer(bool roads) : IScriptedPlayer
{
    public static readonly Cell QuarryAt = new(20, 30);
    public static readonly Cell SmithyAt = new(26, 36);
    private bool _industry;

    public string Id => roads ? "optimal" : "optimal_no_roads";

    public void Start(World w) => MvpOpening.Apply(w, roads: roads);

    public void Daily(World w)
    {
        if (!_industry && w.Calendar.Season == Season.Winter)
        {
            _industry = true;
            Placement.Place(w, "quarry", QuarryAt);   // generated maps: the nearest outcrop
            Placement.Place(w, "smithy", SmithyAt);
        }
        Manage(w, "quarry", "stone", low: 20, high: 60);
        Manage(w, "smithy", "tools", low: 8, high: 14);
    }

    /// <summary>One family on the building while the resource is below <paramref name="low"/>; freed above <paramref name="high"/>.</summary>
    private static void Manage(World w, string def, string res, int low, int high)
    {
        var b = w.Buildings.FirstOrDefault(x => x.IsActive && x.Def.Id == def);
        if (b is null) return;
        var stock = w.StorageStockIncludingTransit(w.Content.Resource(res).Index);
        if (stock < Qty.Units(low) && b.AssignedCount == 0)
        {
            var family = w.Households.FirstOrDefault(h => !h.HasJob)
                         ?? w.Households.LastOrDefault(h => h.HasJob && h.AssignedBy != AssignmentSource.Player
                                                            && w.GetBuilding(h.JobBuildingId) is { IsStorage: false });
            if (family is not null) w.Enqueue(new AssignHousehold(family.Id, b.Id));
        }
        else if (stock > Qty.Units(high))
        {
            foreach (var id in b.SlotHouseholds.Where(id => id != 0)) w.Enqueue(new UnassignHousehold(id));
        }
    }
}
