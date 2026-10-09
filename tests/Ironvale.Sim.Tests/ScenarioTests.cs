namespace Ironvale.Sim.Tests;

/// <summary>Scripted players and the crisis watch used by the CLI balance scenarios (Marco 2A.3).</summary>
public class ScenarioTests
{
    private static (World w, CrisisWatch watch) Run(IScriptedPlayer player, int days)
    {
        var w = TestKit.NewWorld();
        w.CollectEvents = false;
        player.Start(w);
        var watch = new CrisisWatch();
        for (int d = 0; d < days && !w.IsCollapsed; d++)
        {
            w.StepDays(1);
            player.Daily(w);
            watch.Observe(w);
            TestKit.AssertInvariants(w);
        }
        return (w, watch);
    }

    [Fact]
    public void Optimal_player_survives_the_first_year_and_meets_the_firewood_crisis_in_autumn()
    {
        var (w, watch) = Run(ScriptedPlayers.Create("optimal"), SimTime.DaysPerYear);
        Assert.Equal(6, w.Households.Count);
        Assert.NotNull(watch.FirewoodDay);
        Assert.Equal(Season.Autumn, new Calendar(w.StartTick + watch.FirewoodDay!.Value * SimTime.TicksPerDay).Season);
        Assert.NotEmpty(w.Map.Roads);
    }

    [Fact]
    public void Roads_lower_the_share_of_the_shift_spent_walking()
    {
        double Commute(bool roads)
        {
            var (w, _) = Run(ScriptedPlayers.Create("optimal", roads), 120);
            return (double)w.Telemetry.CommuteHoursPermille / w.Telemetry.ShiftHoursPermille;
        }
        Assert.True(Commute(true) < Commute(false));
    }

    [Fact]
    public void A_naive_layout_far_from_work_walks_much_more()
    {
        var (naive, _) = Run(ScriptedPlayers.Create("naive"), 60);
        var (optimal, _) = Run(ScriptedPlayers.Create("optimal"), 60);
        double N(World w) => (double)w.Telemetry.CommuteHoursPermille / Math.Max(1, w.Telemetry.ShiftHoursPermille);
        Assert.True(N(naive) > N(optimal) * 1.5, $"naive {N(naive):P0} vs optimal {N(optimal):P0}");
    }

    /// <summary>Marco 2A.5 balance gate: a newcomer loses nobody in year 1, meets the firewood crisis in the first
    /// autumn, and doing nothing is punished (GDD v0.2 §4.2).</summary>
    [Theory]
    [InlineData(42UL)]
    [InlineData(7UL)]
    [InlineData(123UL)]
    public void Naive_player_loses_nobody_in_year_one_and_meets_the_firewood_crisis(ulong seed)
    {
        var w = TestKit.NewWorld(seed);
        w.CollectEvents = false;
        var player = ScriptedPlayers.Create("naive");
        player.Start(w);
        var watch = new CrisisWatch();
        for (int d = 0; d < SimTime.DaysPerYear; d++)
        {
            w.StepDays(1);
            player.Daily(w);
            watch.Observe(w);
        }
        Assert.Equal(0, watch.Departures);
        Assert.NotNull(watch.FirewoodDay);
        Assert.Equal(Season.Autumn, new Calendar(w.StartTick + watch.FirewoodDay!.Value * SimTime.TicksPerDay).Season);
        Assert.Equal(1, new Calendar(w.StartTick + watch.FirewoodDay!.Value * SimTime.TicksPerDay).Year);
    }

    [Fact]
    public void Doing_nothing_costs_families_in_year_one()
    {
        var (_, watch) = Run(ScriptedPlayers.Create("passive"), SimTime.DaysPerYear);
        Assert.True(watch.Departures > 0);
    }

    [Fact]
    public void Unknown_player_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => ScriptedPlayers.Create("pro"));
    }
}

public class CommandTimingTests
{
    [Fact]
    public void Applying_commands_immediately_gives_the_same_world_as_at_the_next_step()
    {
        var a = TestKit.NewWorld();
        var b = TestKit.NewWorld();
        a.StepTicks(17);
        b.StepTicks(17);
        var cmd = new PlaceBuilding("house", new Cell(26, 30), 0);
        a.Enqueue(cmd);
        b.Enqueue(cmd);
        b.ApplyPendingCommands();             // the view does this so a paused game still answers
        Assert.Equal(2, b.Buildings.Count);
        a.StepDays(3);
        b.StepDays(3);
        Assert.Equal(SaveSerializer.StateHashHex(a), SaveSerializer.StateHashHex(b));
    }
}
