namespace Ironvale.Sim.Tests;

/// <summary>Marco 2A.6: administrative capacity and "the game learns from you" decree suggestions.</summary>
public class ReeveTests
{
    [Fact]
    public void Decrees_use_capacity_and_disabled_ones_are_free()
    {
        var w = TestKit.NewWorld();
        Assert.Equal(4, w.AdminCapacity);                          // the Salão
        w.Enqueue(new CreatePolicy("keep_above", "food", 100, 200));    // 1
        w.Enqueue(new CreatePolicy("keep_above", "tools", 5, 10));      // 2 (conditional production)
        w.Step();
        Assert.Equal(3, w.AdminUsed);
        w.Enqueue(new SetPolicyEnabled(w.Policies[1].Id, false));
        w.Step();
        Assert.Equal(1, w.AdminUsed);
        Assert.Equal(0, w.AdminOverload);
    }

    [Fact]
    public void An_overloaded_reeve_acts_late_and_says_so()
    {
        int RecruitedAfter(int extraDecrees, out World w)
        {
            w = TestKit.NewWorld();
            var cutter = TestKit.AddActive(w, "woodcutter", new Cell(26, 30));
            w.Enqueue(new CreatePolicy("keep_above", "wood", 1000, 1200));
            foreach (var r in new[] { "food", "firewood", "stone", "coins" }.Take(extraDecrees))
                w.Enqueue(new CreatePolicy("keep_above", r, 0, 10_000));   // cost CA, never act
            w.StepDays(3);
            return cutter.AssignedCount;
        }
        int fine = RecruitedAfter(3, out var wFine);                // 4 CA of 4
        int overloaded = RecruitedAfter(4, out var wOver);          // 5 CA of 4
        Assert.Equal(0, wFine.AdminOverload);
        Assert.Equal(1, wOver.AdminOverload);
        Assert.True(overloaded < fine, $"overloaded recruited {overloaded}, fine {fine}");
        Assert.Contains(wOver.PolicyLog, e => e.Key == "overloaded" || e.Key == "recruited");
    }

    [Fact]
    public void Overload_errors_are_deterministic()
    {
        string Run()
        {
            var w = TestKit.NewWorld(seed: 99);
            TestKit.AddActive(w, "woodcutter", new Cell(26, 30));
            foreach (var r in new[] { "wood", "food", "firewood", "stone", "coins" })
                w.Enqueue(new CreatePolicy("keep_above", r, 1000, 2000));
            w.StepDays(20);
            return SaveSerializer.StateHashHex(w);
        }
        Assert.Equal(Run(), Run());
    }

    private static (World w, Building cutter) ManualWoodcutting()
    {
        var w = TestKit.NewWorld();
        var cutter = TestKit.AddActive(w, "woodcutter", new Cell(26, 30));
        TestKit.AddActive(w, "woodcutter", new Cell(29, 26));
        return (w, cutter);
    }

    [Fact]
    public void Three_manual_moves_toward_a_resource_make_the_reeve_offer_a_decree()
    {
        var (w, cutter) = ManualWoodcutting();
        var second = w.Buildings.Last();
        w.Enqueue(new AssignHousehold(w.Households[0].Id, cutter.Id));
        w.Enqueue(new AssignHousehold(w.Households[1].Id, cutter.Id));
        w.StepDays(1);
        Assert.Null(w.Suggestion);
        w.Enqueue(new AssignHousehold(w.Households[2].Id, second.Id));
        w.Step();
        var s = Assert.IsType<DecreeSuggestion>(w.Suggestion);
        Assert.Equal(TestKit.Res("wood"), s.Resource);
        Assert.Equal(3, s.Actions);
        Assert.True(s.Min.IsPositive && s.Max > s.Min);
        Assert.Contains(w.DrainEvents(), e => e is SuggestionOffered);

        w.Enqueue(new AcceptSuggestion(s.Id));
        w.Step();
        Assert.Null(w.Suggestion);
        var decree = Assert.Single(w.Policies);
        Assert.Equal((s.Min, s.Max), (decree.Min, decree.Max));
    }

    [Fact]
    public void Not_now_snoozes_and_never_stops_suggestions_for_that_resource()
    {
        var (w, cutter) = ManualWoodcutting();
        var second = w.Buildings.Last();
        void ThreeMoves()
        {
            foreach (var h in w.Households.Where(h => h.HasJob).ToList()) w.Enqueue(new UnassignHousehold(h.Id));
            w.Enqueue(new AssignHousehold(w.Households[0].Id, cutter.Id));
            w.Enqueue(new AssignHousehold(w.Households[1].Id, cutter.Id));
            w.Enqueue(new AssignHousehold(w.Households[2].Id, second.Id));
            w.Step();
        }
        ThreeMoves();
        w.Enqueue(new DismissSuggestion(w.Suggestion!.Id, Forever: false));
        w.Step();
        ThreeMoves();
        Assert.Null(w.Suggestion);                                   // snoozed
        w.Tick += (long)(w.Content.Balance.SuggestSnoozeDays + 1) * SimTime.TicksPerDay;   // jump the clock
        ThreeMoves();
        Assert.NotNull(w.Suggestion);                                // back after the snooze
        w.Enqueue(new DismissSuggestion(w.Suggestion!.Id, Forever: true));
        w.Step();
        w.Tick += 400L * SimTime.TicksPerDay;
        ThreeMoves();
        Assert.Null(w.Suggestion);                                   // never
    }

    [Fact]
    public void No_suggestion_when_a_decree_already_covers_the_resource()
    {
        var (w, cutter) = ManualWoodcutting();
        w.Enqueue(new CreatePolicy("keep_above", "wood", 10, 20));
        foreach (var h in w.Households.Take(2)) w.Enqueue(new AssignHousehold(h.Id, cutter.Id));
        w.Enqueue(new AssignHousehold(w.Households[2].Id, w.Buildings.Last().Id));
        w.StepDays(1);
        Assert.Null(w.Suggestion);
    }

    [Fact]
    public void Suggestions_survive_save_and_load()
    {
        var (w, cutter) = ManualWoodcutting();
        foreach (var h in w.Households.Take(2)) w.Enqueue(new AssignHousehold(h.Id, cutter.Id));
        w.Enqueue(new AssignHousehold(w.Households[2].Id, w.Buildings.Last().Id));
        w.Step();
        var copy = SaveSerializer.Load(SaveSerializer.Save(w), w.Content).World;
        Assert.Equal(w.Suggestion!.Id, copy.Suggestion!.Id);
        Assert.Equal(SaveSerializer.StateHashHex(w), SaveSerializer.StateHashHex(copy));
    }
}
