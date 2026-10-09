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

    public static IScriptedPlayer Create(string id, bool roads = true) => id switch
    {
        "passive" => new PassivePlayer(),
        "naive" => new NaivePlayer(),
        "optimal" => new OptimalPlayer(roads),
        _ => throw new ArgumentException($"unknown player '{id}' (passive | naive | optimal)"),
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
        foreach (var (def, origin) in Layout) w.Enqueue(new PlaceBuilding(def, origin, 0));
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
        // Late reaction to the firewood alert: one woodcutter switches to firewood ~30 days before winter.
        int daysToWinter = ((int)Season.Winter * SimTime.MonthsPerSeason - cal.MonthOfYear) * SimTime.DaysPerMonth - cal.DayOfMonth;
        var firewood = w.Content.Resource("firewood").Index;
        if (daysToWinter is > 0 and <= 30 && w.StorageStock(firewood) < Qty.Units(w.Households.Count * 60))
        {
            var cutter = w.Buildings.FirstOrDefault(b => b.IsActive && b.Def.Id == "woodcutter" && b.Recipe?.Id == "chop_wood");
            if (cutter is not null) w.Enqueue(new SetRecipe(cutter.Id, "split_firewood"));
        }
        // Back to wood in spring.
        if (cal.Season == Season.Spring && cal.DayOfMonth == 0 && cal.MonthOfYear == 0)
            foreach (var b in w.Buildings.Where(b => b.IsActive && b.Def.Id == "woodcutter" && b.Recipe?.Id == "split_firewood"))
                w.Enqueue(new SetRecipe(b.Id, "chop_wood"));
    }
}

/// <summary>The scripted MVP opening (food first, decrees, carriers) plus roads linking the work areas.</summary>
public sealed class OptimalPlayer(bool roads) : IScriptedPlayer
{
    public string Id => roads ? "optimal" : "optimal_no_roads";

    public void Start(World w) => MvpOpening.Apply(w, roads: roads);

    public void Daily(World w) { }
}
