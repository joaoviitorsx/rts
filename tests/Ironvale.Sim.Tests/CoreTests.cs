namespace Ironvale.Sim.Tests;

public class CalendarTests
{
    [Fact]
    public void Constants_match_gdd_rhythm()
    {
        Assert.Equal(40, SimTime.TicksPerDay);              // 1 day = 4 s at 10 ticks/s
        Assert.Equal(1200, SimTime.TicksPerMonth);          // 2 min
        Assert.Equal(14400, SimTime.TicksPerYear);          // 24 min
    }

    [Theory]
    [InlineData(0, 1, 0, 0, Season.Spring)]
    [InlineData(SimTime.TicksPerMonth * 3, 1, 3, 0, Season.Summer)]
    [InlineData(SimTime.TicksPerMonth * 9 + SimTime.TicksPerDay * 5, 1, 9, 5, Season.Winter)]
    [InlineData(SimTime.TicksPerYear + 1, 2, 0, 0, Season.Spring)]
    public void Derives_date_from_tick(long tick, int year, int month, int day, Season season)
    {
        var cal = new Calendar(tick);
        Assert.Equal(year, cal.Year);
        Assert.Equal(month, cal.MonthOfYear);
        Assert.Equal(day, cal.DayOfMonth);
        Assert.Equal(season, cal.Season);
    }

    [Fact]
    public void Previous_season_wraps()
    {
        Assert.Equal(Season.Winter, new Calendar(0).PreviousSeason);
        Assert.Equal(Season.Spring, new Calendar(SimTime.TicksPerSeason).PreviousSeason);
    }
}

public class SchedulerTests
{
    private sealed class Counter(Frequency f, Phase p, List<string> trace) : ISimSystem
    {
        public int Runs;
        public string Name => $"{p}/{f}";
        public Phase Phase => p;
        public Frequency Frequency => f;
        public void Run(World world, in Calendar cal)
        {
            Runs++;
            trace.Add(Name);
        }
    }

    [Fact]
    public void Each_frequency_fires_the_expected_number_of_times_per_year()
    {
        var trace = new List<string>();
        var counters = Enum.GetValues<Frequency>().ToDictionary(f => f, f => new Counter(f, Phase.Production, trace));
        var scheduler = new Scheduler();
        foreach (var c in counters.Values) scheduler.Register(c);

        for (long t = 0; t < SimTime.TicksPerYear; t++) scheduler.RunStep(null!, new Calendar(t));

        Assert.Equal(14400, counters[Frequency.Tick].Runs);
        Assert.Equal(3600, counters[Frequency.Hourly].Runs);
        Assert.Equal(360, counters[Frequency.Daily].Runs);
        Assert.Equal(52, counters[Frequency.Weekly].Runs);   // days 0,7,…,357
        Assert.Equal(12, counters[Frequency.Monthly].Runs);
        Assert.Equal(4, counters[Frequency.Seasonal].Runs);
        Assert.Equal(1, counters[Frequency.Yearly].Runs);
    }

    [Fact]
    public void Runs_by_phase_order_then_registration_order()
    {
        var trace = new List<string>();
        var scheduler = new Scheduler();
        scheduler.Register(new Counter(Frequency.Tick, Phase.Decisions, trace));
        scheduler.Register(new Counter(Frequency.Tick, Phase.Production, trace));
        scheduler.Register(new Counter(Frequency.Tick, Phase.Consumption, trace));
        scheduler.Register(new Counter(Frequency.Tick, Phase.Transport, trace));
        scheduler.Register(new Counter(Frequency.Tick, Phase.Needs, trace));

        scheduler.RunStep(null!, new Calendar(1));

        Assert.Equal(new[] { "Production/Tick", "Transport/Tick", "Consumption/Tick", "Needs/Tick", "Decisions/Tick" }, trace);
    }

    [Fact]
    public void World_registers_systems_in_gdd_phase_order()
    {
        var phases = TestKit.NewWorld().Systems.Select(s => (int)s.Phase).ToList();
        Assert.Equal(phases.OrderBy(p => p), phases);
    }
}

public class QtyAndRngTests
{
    [Fact]
    public void Qty_is_fixed_point_and_truncates_permille()
    {
        Assert.Equal(1500, Qty.FromDouble(1.5).Milli);
        Assert.Equal(new Qty(333), new Qty(1000).MulPermille(333));
        Assert.Equal(new Qty(-1), Qty.Units(1) - new Qty(1001));
    }

    [Fact]
    public void Pcg32_is_reproducible_and_bounded()
    {
        var a = new Pcg32(42, 54);
        var b = new Pcg32(42, 54);
        for (int i = 0; i < 1000; i++)
        {
            int x = a.Range(-5, 5);
            Assert.Equal(x, b.Range(-5, 5));
            Assert.InRange(x, -5, 5);
        }
    }

    [Fact]
    public void Rng_streams_are_independent_of_creation_order()
    {
        var s1 = new RngStreams(7);
        var s2 = new RngStreams(7);
        s1.Get("a").NextUInt();
        uint fromB1 = s1.Get("b").NextUInt();
        uint fromB2 = s2.Get("b").NextUInt();   // "a" never touched here
        Assert.Equal(fromB1, fromB2);
    }
}

public class StockpileTests
{
    [Fact]
    public void Never_goes_negative_and_respects_capacity()
    {
        var s = new Stockpile(2, Qty.Units(10));
        Assert.Equal(Qty.Units(10), s.AddUpTo(0, Qty.Units(15)));
        Assert.False(s.TryRemove(0, Qty.Units(11)));
        Assert.Equal(Qty.Units(10), s.RemoveUpTo(0, Qty.Units(99)));
        Assert.Equal(Qty.Zero, s.Get(0));
    }

    [Fact]
    public void Reserved_amount_is_not_free_and_incoming_takes_space()
    {
        var s = new Stockpile(1, Qty.Units(10));
        s.AddUpTo(0, Qty.Units(6));
        Assert.True(s.TryReserve(0, Qty.Units(4)));
        Assert.Equal(Qty.Units(2), s.Free(0));
        Assert.False(s.TryRemove(0, Qty.Units(3)));

        Assert.True(s.TryReserveIncoming(Qty.Units(4)));
        Assert.Equal(Qty.Zero, s.Space);
        Assert.False(s.TryReserveIncoming(Qty.Units(1)));
    }
}

public class ContentLoaderTests
{
    private static Dictionary<string, string> RealFiles()
    {
        string dir = DataPaths.FindDataDirectory();
        return ContentLoader.RequiredFiles.ToDictionary(f => f, f => File.ReadAllText(Path.Combine(dir, f)));
    }

    [Fact]
    public void Loads_mvp_content()
    {
        var c = TestKit.Content;
        Assert.Equal(new[] { "wood", "firewood", "food", "stone", "tools", "coins" }, c.Resources.Select(r => r.Id));
        Assert.Equal(new[] { "hall", "house", "woodcutter", "field", "smithy", "quarry", "granary" }, c.Buildings.Select(b => b.Id));
        Assert.True(c.Building("smithy").Recipes.Single().HasInputs);
        Assert.StartsWith("fnv64:", c.Hash);
    }

    [Fact]
    public void Invalid_json_names_the_file()
    {
        var files = RealFiles();
        files[ContentLoader.BuildingsFile] = "{ not json";
        var e = Assert.Throws<ContentException>(() => ContentLoader.Load(files));
        Assert.StartsWith("buildings.json", e.Message);
    }

    [Fact]
    public void Unknown_resource_names_the_field()
    {
        var files = RealFiles();
        files[ContentLoader.BuildingsFile] = files[ContentLoader.BuildingsFile].Replace("\"cost\": { \"wood\": 15 }", "\"cost\": { \"gold\": 15 }");
        var e = Assert.Throws<ContentException>(() => ContentLoader.Load(files));
        Assert.Contains("buildings[1]", e.Message);
        Assert.Contains("gold", e.Message);
    }

    [Fact]
    public void Content_hash_changes_with_content()
    {
        var files = RealFiles();
        files[ContentLoader.BalanceFile] = files[ContentLoader.BalanceFile].Replace("\"foodPerMemberPerDay\": 0.3", "\"foodPerMemberPerDay\": 0.4");
        Assert.NotEqual(TestKit.Content.Hash, ContentLoader.Load(files).Hash);
    }
}
