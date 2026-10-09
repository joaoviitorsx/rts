namespace Ironvale.Sim.Tests;

/// <summary>Marco 2B "while you were away": the chronicle observes a fast-forward without changing it.</summary>
public class ChronicleTests
{
    private static (World w, Chronicle c) FastForward(int years, bool chronicle, ulong seed = 42)
    {
        var w = TestKit.NewWorld(seed, opening: true);
        var c = new Chronicle();
        w.CollectEvents = chronicle;
        for (int d = 0; d < years * SimTime.DaysPerYear; d++)
        {
            w.StepDays(1);
            if (chronicle) c.Observe(w, w.DrainEvents());
        }
        if (chronicle) c.Finish(w);
        return (w, c);
    }

    [Fact]
    public void Watching_does_not_change_the_simulation()
    {
        var (watched, _) = FastForward(5, chronicle: true);
        var (plain, _) = FastForward(5, chronicle: false);
        Assert.Equal(SaveSerializer.StateHashHex(plain), SaveSerializer.StateHashHex(watched));
    }

    [Fact]
    public void Five_years_produce_dated_highlights()
    {
        var (w, c) = FastForward(5, chronicle: true);
        Assert.Equal(6, c.StartPopulation);
        Assert.Equal(5, c.Years);
        Assert.Equal(w.Households.Count, c.EndPopulation);
        Assert.NotEmpty(c.Moments);
        Assert.Contains(c.Moments, m => m.Key is "built" or "good_winter" or "reeve_busy");
        var highlights = c.Highlights(5);
        Assert.InRange(highlights.Count, 1, 5);
        Assert.Equal(highlights.OrderBy(m => m.Year).ThenBy(m => (int)m.Season), highlights);
    }

    [Fact]
    public void Families_leaving_are_remembered_with_names_and_reasons()
    {
        var w = TestKit.NewWorld();   // nobody plays: hunger by the end of year 1
        var c = new Chronicle();
        for (int d = 0; d < SimTime.DaysPerYear; d++)
        {
            w.StepDays(1);
            c.Observe(w, w.DrainEvents());
        }
        c.Finish(w);
        var left = Assert.Single(c.Moments.Where(m => m.Key == "left").Take(1));
        Assert.Contains("fome", left.Args[2]);
        Assert.Equal(5, left.Weight);
    }
}
