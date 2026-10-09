namespace Ironvale.Sim.Tests;

/// <summary>Marco 2A.3: commute inside the shift, roads, moving house closer to work.</summary>
public class CommuteTests
{
    /// <summary>Wood made in one day by one household working at a woodcutter placed at <paramref name="cutterAt"/>.</summary>
    private static (double wood, int commutePermille, World w) OneDay(Cell cutterAt, Action<World>? setup = null)
    {
        var w = TestKit.NewWorld();
        var home = TestKit.AddActive(w, "house", new Cell(30, 26));
        var cutter = TestKit.AddActive(w, "woodcutter", cutterAt);
        setup?.Invoke(w);
        var h = w.Households[0];
        h.HomeId = home.Id;
        w.Assign(h, cutter, AssignmentSource.Player, 0);
        w.StepTicks(SimTime.TicksPerDay - w.Tick % SimTime.TicksPerDay);   // start of a day
        h.HomeId = home.Id;                                                   // keep the test's home (no auto move)
        int commute = w.CommutePermille(h);
        var before = cutter.Stock.Get(TestKit.Res("wood"));
        w.StepDays(1);
        TestKit.AssertInvariants(w);
        return ((cutter.Stock.Get(TestKit.Res("wood")) - before).AsDouble, commute, w);
    }

    [Fact]
    public void Living_far_from_work_costs_part_of_the_shift()
    {
        var (near, nearPct, _) = OneDay(new Cell(30, 22));   // next door
        var (far, farPct, _) = OneDay(new Cell(30, 4));      // ~20 cells away
        Assert.True(farPct > nearPct + 100, $"far commute {farPct}‰ vs near {nearPct}‰");
        Assert.True(far < near * 0.85, $"far produced {far}, near {near}");
        // Lost output ≈ the share of the shift spent walking.
        Assert.InRange(far / near, 1 - farPct / 1000.0 - 0.05, 1 - farPct / 1000.0 + 0.08);
    }

    [Fact]
    public void A_road_to_work_shortens_the_commute()
    {
        var (_, without, _) = OneDay(new Cell(30, 4));
        var (_, with, _) = OneDay(new Cell(30, 4), w =>
        {
            w.Enqueue(new PlaceRoad(Enumerable.Range(6, 20).Select(y => new Cell(32, y)).ToArray()));   // 20 stone of 30
            w.Step();
        });
        Assert.True(with < without, $"road {with}‰ should beat {without}‰");
    }

    [Fact]
    public void A_family_moves_to_a_free_house_closer_to_its_job()
    {
        var w = TestKit.NewWorld();
        var farHouse = TestKit.AddActive(w, "house", new Cell(30, 50));
        var cutter = TestKit.AddActive(w, "woodcutter", new Cell(30, 4));
        var h = w.Households[0];
        w.StepDays(1);                                  // everyone gets a house: only the far one exists
        Assert.Equal(farHouse.Id, h.HomeId);
        w.Assign(h, cutter, AssignmentSource.Player, 0);
        var nearHouse = TestKit.AddActive(w, "house", new Cell(30, 8));
        w.StepDays(1);
        Assert.Equal(nearHouse.Id, h.HomeId);
    }

    [Fact]
    public void Without_auto_rehome_families_stay_put()
    {
        var w = TestKit.NewWorld(TestKit.ContentWith(("autoRehome", "0")));
        var farHouse = TestKit.AddActive(w, "house", new Cell(30, 50));
        var cutter = TestKit.AddActive(w, "woodcutter", new Cell(30, 4));
        var h = w.Households[0];
        w.StepDays(1);
        w.Assign(h, cutter, AssignmentSource.Player, 0);
        TestKit.AddActive(w, "house", new Cell(30, 8));
        w.StepDays(1);
        Assert.Equal(farHouse.Id, h.HomeId);
    }

    [Fact]
    public void Walking_shows_in_telemetry_as_lost_shift_hours()
    {
        var (_, pct, w) = OneDay(new Cell(30, 4));
        var t = w.Telemetry;
        Assert.True(t.ShiftHoursPermille > 0);
        double lost = (double)t.CommuteHoursPermille / t.ShiftHoursPermille;
        Assert.InRange(lost, pct / 1000.0 - 0.06, pct / 1000.0 + 0.06);
    }
}
