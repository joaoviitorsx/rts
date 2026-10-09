namespace Ironvale.Sim.Tests;

/// <summary>
/// Random (but seeded) command storms over two years: every invariant must hold every day — ledger conservation,
/// no negatives, consistent references — and a save/load in the middle must not change the outcome.
/// </summary>
[Trait("Category", "Soak")]
public class FuzzTests
{
    private static readonly string[] Defs = { "house", "woodcutter", "field", "smithy", "quarry", "granary" };
    private static readonly string[] Res = { "wood", "firewood", "food", "stone", "tools" };

    private static SimCommand? RandomCommand(World w, Random rng)
    {
        Cell RandomCell() => new(rng.Next(0, w.Map.Width), rng.Next(0, w.Map.Height));
        T Pick<T>(IReadOnlyList<T> list) => list[rng.Next(list.Count)];
        switch (rng.Next(12))
        {
            case 0: case 1: return new PlaceBuilding(Pick(Defs), RandomCell(), rng.Next(4));
            case 2:
                var sites = w.Buildings.Where(b => !b.IsActive).ToList();
                return sites.Count == 0 ? null : new CancelConstruction(Pick(sites).Id);
            case 3: case 4:
                return w.Households.Count == 0 ? null : new AssignHousehold(Pick(w.Households).Id, Pick(w.Buildings).Id);
            case 5: return w.Households.Count == 0 ? null : new UnassignHousehold(Pick(w.Households).Id);
            case 6:
                var producers = w.Buildings.Where(b => b.Def.Recipes.Count > 1).ToList();
                return producers.Count == 0 ? null : new SetRecipe(Pick(producers).Id, Pick(Pick(producers).Def.Recipes).Id);
            case 7:
                long min = rng.Next(0, 300);
                return new CreatePolicy("keep_above", Pick(Res), min, min + rng.Next(1, 300));
            case 8:
                if (w.Policies.Count == 0) return null;
                var p = Pick(w.Policies);
                return rng.Next(3) switch
                {
                    0 => new SetPolicyEnabled(p.Id, rng.Next(2) == 0),
                    1 => new RemovePolicy(p.Id),
                    _ => new SetPolicyBand(p.Id, rng.Next(0, 200), rng.Next(200, 500)),
                };
            case 9:
                var a = RandomCell();
                var cells = Enumerable.Range(0, rng.Next(1, 8)).Select(i => new Cell(a.X + i, a.Y)).ToArray();
                return rng.Next(4) == 0 ? new RemoveRoad(cells) : new PlaceRoad(cells);
            case 10:
                return w.Suggestion is { } s ? (rng.Next(2) == 0 ? new AcceptSuggestion(s.Id) : new DismissSuggestion(s.Id, rng.Next(2) == 0)) : null;
            default: return null;
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Random_commands_never_break_the_invariants(int seed)
    {
        var w = TestKit.NewWorld((ulong)seed, opening: true);
        w.CollectEvents = false;
        var rng = new Random(seed);
        World? copy = null;
        for (int day = 0; day < 2 * SimTime.DaysPerYear; day++)
        {
            for (int i = rng.Next(0, 4); i > 0; i--)
                if (RandomCommand(w, rng) is { } cmd)
                {
                    w.Enqueue(cmd);
                    copy?.Enqueue(cmd);
                }
            int ticks = rng.Next(1, SimTime.TicksPerDay * 2);
            w.StepTicks(ticks);
            copy?.StepTicks(ticks);   // lockstep: same commands at the same ticks
            TestKit.AssertInvariants(w);
            if (day == 200)
            {
                copy = SaveSerializer.Load(SaveSerializer.Save(w), w.Content).World;
                copy.CollectEvents = false;
            }
            if (copy is not null && day > 200 && day % 50 == 0)
                Assert.Equal(SaveSerializer.StateHashHex(w), SaveSerializer.StateHashHex(copy));
        }
    }
}
