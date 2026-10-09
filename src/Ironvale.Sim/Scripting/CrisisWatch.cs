namespace Ironvale.Sim.Scripting;

/// <summary>
/// Observer for balance runs: records when the first crises of GDD v0.2 §6 appear (it never changes the world).
/// <list type="bullet">
/// <item>1 — firewood: in autumn, firewood in storage below the coming winter's demand.</item>
/// <item>2 — tools: no spare tools in storage and the families' average tool condition below 50%.</item>
/// <item>3 — winter hunger: a family goes hungry in winter.</item>
/// </list>
/// </summary>
public sealed class CrisisWatch
{
    public long? FirewoodDay { get; private set; }
    public long? ToolsDay { get; private set; }
    public long? WinterHungerDay { get; private set; }
    public int Departures { get; private set; }
    public long? FirstDepartureDay { get; private set; }
    public int MinPopulation { get; private set; } = int.MaxValue;
    /// <summary>
    /// Game-feel proxy: longest stretch (days) without a notable moment — a building finished, a family left, a crisis
    /// began or a season changed. GDD feel rule: never more than 2 min (30 days at 1x) without a relevant decision.
    /// </summary>
    public long LongestQuietDays { get; private set; }

    private int Crises => (FirewoodDay is null ? 0 : 1) + (ToolsDay is null ? 0 : 1) + (WinterHungerDay is null ? 0 : 1);

    private int _lastPopulation = -1;
    private int _lastActive = -1;
    private long _lastNotable;
    private Season? _lastSeason;

    /// <summary>Call once per day.</summary>
    public void Observe(World w)
    {
        var cal = w.Calendar;
        long day = w.ElapsedTicks / SimTime.TicksPerDay;
        int pop = w.Households.Count;
        if (_lastPopulation >= 0 && pop < _lastPopulation)
        {
            Departures += _lastPopulation - pop;
            FirstDepartureDay ??= day;
        }
        int active = w.Buildings.Count(b => b.IsActive);
        bool notable = (_lastPopulation >= 0 && pop != _lastPopulation) || (_lastActive >= 0 && active > _lastActive)
                       || (_lastSeason is { } s && s != cal.Season);
        _lastPopulation = pop;
        _lastActive = active;
        _lastSeason = cal.Season;
        MinPopulation = Math.Min(MinPopulation, pop);
        if (pop == 0) return;
        int crisesBefore = Crises;

        var bal = w.Content.Balance;
        if (FirewoodDay is null && cal.Season == Season.Autumn)
        {
            var demand = bal.FirewoodPerHouseholdPerWinterDay * (pop * SimTime.DaysPerMonth * SimTime.MonthsPerSeason);
            if (w.StorageStock(w.Content.Resource("firewood").Index) < demand) FirewoodDay = day;
        }
        if (ToolsDay is null && !w.StorageStock(w.Content.Resource("tools").Index).IsPositive
            && w.Households.Average(h => h.ToolCondition) < Permille.One / 2)
            ToolsDay = day;
        if (WinterHungerDay is null && cal.IsWinter && w.Households.Any(h => h.FoodDeficitDays > 0))
            WinterHungerDay = day;

        if (notable || Crises != crisesBefore) _lastNotable = day;
        LongestQuietDays = Math.Max(LongestQuietDays, day - _lastNotable);
    }

    /// <summary>Minutes of play at 1x for a sim day count (1 day = 4 s).</summary>
    public static double Minutes(long day) => day * SimTime.TicksPerDay / (double)SimTime.TicksPerSecondAt1x / 60.0;
}
