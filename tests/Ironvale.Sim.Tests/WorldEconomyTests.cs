namespace Ironvale.Sim.Tests;

/// <summary>The economy on a generated map (GDD v0.3 §9, plan v0.3 b2).</summary>
public class WorldEconomyTests
{
    private static World Gen(ulong seed = 42, ContentDb? content = null)
    {
        content ??= TestKit.Content;
        var scenario = ContentLoader.LoadScenario(
            File.ReadAllText(Path.Combine(DataPaths.FindDataDirectory(), DataPaths.ScenarioDir, "mvp_generated.json")), content);
        var w = World.Create(content, scenario, seed);
        w.CollectEvents = true;
        return w;
    }

    private static int Res(World w, string id) => w.Content.Resource(id).Index;

    private static Building ActiveAtBestSpot(World w, string def)
    {
        var origin = Placement.Find(w, def, Placement.FlatHallCenter) ?? throw new InvalidOperationException("no spot");
        var b = w.AddBuilding(w.Content.Building(def), origin, 0, active: true);
        return b;
    }

    private static int Stumps(World w) =>
        w.Nature!.Changes.Values.Count(n => n.Kind == NodeKind.Tree && Nature.StageOf(n, w.Tick, w.Content.Balance) == TreeStage.Stump);

    [Fact]
    public void Woodcutters_fell_real_trees_nearby()
    {
        var w = Gen();
        var cutter = ActiveAtBestSpot(w, "woodcutter");
        var (matureBefore, _, _) = w.TreesAround(cutter);
        w.Enqueue(new AssignHousehold(w.Households[0].Id, cutter.Id));
        w.StepDays(20);

        int felled = Stumps(w);
        Assert.True(felled > 0, "no tree felled");
        var (matureAfter, growing, _) = w.TreesAround(cutter);
        Assert.True(matureAfter < matureBefore);   // a young tree may have matured meanwhile
        Assert.True(growing >= felled);
        long wood = w.Ledger.ProducedOf(Res(w, "wood")).Milli;
        // Everything produced came out of felled trees (what is left is the budget of the tree being cut).
        Assert.Equal((long)felled * w.Content.Balance.TreeWood * 1000, wood + cutter.HarvestBudgetMilli);
        Assert.Contains(w.DrainEvents(), e => e is TreeFelled tf && tf.BuildingId == cutter.Id);
        TestKit.AssertInvariants(w);
    }

    [Fact]
    public void A_woodcutter_without_mature_trees_stops_and_says_why()
    {
        var w = Gen();
        var cutter = ActiveAtBestSpot(w, "woodcutter");
        int r = cutter.Def.WorkRadius;
        for (int y = cutter.Center.Y - r; y <= cutter.Center.Y + r; y++)
        for (int x = cutter.Center.X - r; x <= cutter.Center.X + r; x++)
            if (w.Map.InBounds(new Cell(x, y)) && w.Nature!.At(new Cell(x, y)).Kind == NodeKind.Tree) w.Nature.Clear(new Cell(x, y));
        w.Enqueue(new AssignHousehold(w.Households[0].Id, cutter.Id));
        w.StepDays(5);
        Assert.Equal(0, w.Ledger.ProducedOf(Res(w, "wood")).Milli);
        Assert.True(cutter.HarvestExhausted);
    }

    [Fact]
    public void Felled_trees_grow_back()
    {
        var content = TestKit.ContentWith(("treeMatureDays", "8"), ("treeStumpDays", "3"));
        var w = Gen(content: content);
        var cutter = ActiveAtBestSpot(w, "woodcutter");
        w.Enqueue(new AssignHousehold(w.Households[0].Id, cutter.Id));
        w.StepDays(2);
        var felled = w.Nature!.Changes.Where(kv => kv.Value.Kind == NodeKind.Tree).Select(kv => kv.Key).First();
        w.Enqueue(new UnassignHousehold(w.Households[0].Id));
        Assert.Equal(TreeStage.Stump, Nature.StageOf(w.Nature.At(felled), w.Tick, content.Balance));
        long sprouts = w.Nature.At(felled).Tick;   // stump until then
        w.StepTicks(sprouts - w.Tick + SimTime.TicksPerDay);
        Assert.Equal(TreeStage.Sapling, Nature.StageOf(w.Nature.At(felled), w.Tick, content.Balance));
        w.StepDays(8);
        Assert.Equal(TreeStage.Mature, Nature.StageOf(w.Nature.At(felled), w.Tick, content.Balance));
    }

    [Fact]
    public void Quarries_only_go_on_an_outcrop_and_empty_it()
    {
        var content = TestKit.ContentWith(("outcropUnits", "3"));
        var w = Gen(content: content);
        var quarry = content.Building("quarry");
        var outcrop = w.Nature!.Deposits.Where(d => d.Kind == DepositKind.Outcrop)
            .OrderBy(d => d.Center.Manhattan(w.Terrain!.Start)).First();   // the one guaranteed near the start
        Assert.NotNull(w.PlacementError(quarry, new Cell(outcrop.Origin.X + 1, outcrop.Origin.Y), 0));
        Assert.NotNull(w.PlacementError(quarry, w.Terrain!.Start, 0));
        Assert.Null(w.PlacementError(quarry, outcrop.Origin, 0));
        Assert.NotNull(w.PlacementError(content.Building("house"), outcrop.Origin, 0));   // deposits stay free

        var b = w.AddBuilding(quarry, outcrop.Origin, 0, active: true);
        w.Enqueue(new AssignHousehold(w.Households[0].Id, b.Id));
        w.StepDays(10);
        Assert.Equal(0, outcrop.Units);
        Assert.Equal(outcrop.InitialUnits * 1000, w.Ledger.ProducedOf(Res(w, "stone")).Milli);
        Assert.True(b.HarvestExhausted);
    }

    [Fact]
    public void Harvest_follows_the_fertility_of_the_soil()
    {
        var content = TestKit.ContentWith(("harvestVariancePermille", "0"));
        var w = Gen(content: content);
        var field = ActiveAtBestSpot(w, "field");
        int fertility = w.FertilityPermille(field);
        Assert.InRange(fertility, 600, 1300);
        field.SeasonalWorkMilli = 100_000;
        // Harvest runs at the first season start after a work season (scenario starts in late spring).
        while (!w.Calendar.IsSeasonStart || w.Calendar.Season != Season.Autumn) w.Step();
        w.Step();
        Assert.Equal(100_000L * fertility / 1000, field.Stock.Get(Res(w, "food")).Milli);
    }

    [Fact]
    public void Builders_clear_trees_off_a_site_and_keep_the_wood()
    {
        var w = Gen();
        var house = w.Content.Building("house");
        // A spot full of mature trees, next to the clearing.
        Cell? spot = null;
        for (int r = 8; r < 40 && spot is null; r++)
        for (int y = w.Terrain!.Start.Y - r; y <= w.Terrain.Start.Y + r && spot is null; y++)
        for (int x = w.Terrain.Start.X - r; x <= w.Terrain.Start.X + r; x++)
        {
            var c = new Cell(x, y);
            if (!w.CanPlace(house, c, 0)) continue;
            int trees = 0;
            for (int dy = 0; dy < 2; dy++)
            for (int dx = 0; dx < 2; dx++)
                if (w.Nature!.At(new Cell(x + dx, y + dy)) is { Kind: NodeKind.Tree } n
                    && Nature.StageOf(n, w.Tick, w.Content.Balance) == TreeStage.Mature) trees++;
            if (trees >= 2) { spot = c; break; }
        }
        Assert.NotNull(spot);
        w.Enqueue(new PlaceBuilding("house", spot!.Value, 0));
        w.ApplyPendingCommands();
        var site = w.Buildings.Single(b => b.Origin == spot && b.Def.Id == "house");
        Assert.True(site.ClearWorkMilli >= 2 * w.Content.Balance.ClearTreeHours * 1000L);
        Assert.True(site.CanProgress);   // clearing needs no material

        w.Enqueue(new AssignHousehold(w.Households[0].Id, site.Id));
        w.StepDays(4);
        Assert.Equal(0, site.ClearWorkMilli);
        for (int dy = 0; dy < 2; dy++)
        for (int dx = 0; dx < 2; dx++)
            Assert.Equal(NodeKind.None, w.Nature!.At(new Cell(spot.Value.X + dx, spot.Value.Y + dy)).Kind);
        Assert.True(w.Ledger.ProducedOf(Res(w, "wood")).Milli >= 2 * w.Content.Balance.TreeWood * 1000L);
        TestKit.AssertInvariants(w);
    }

    [Fact]
    public void Roads_skip_water_and_trees()
    {
        var w = Gen();
        var t = w.Terrain!;
        var water = Enumerable.Range(0, t.Width * t.Height).Select(w.Map.CellAt).First(t.IsWater);
        var tree = Enumerable.Range(0, t.Width * t.Height).Select(w.Map.CellAt).First(c => w.Nature!.At(c).Kind == NodeKind.Tree);
        var free = new Cell(t.Start.X + 3, t.Start.Y + 3);
        w.Enqueue(new PlaceRoad(new[] { water, tree, free }));
        w.ApplyPendingCommands();
        Assert.False(w.Map.IsRoad(water));
        Assert.False(w.Map.IsRoad(tree));
        Assert.True(w.Map.IsRoad(free));
    }

    [Fact]
    public void A_generated_opening_runs_deterministically_through_save_and_load()
    {
        var a = Gen(9);
        ScriptedPlayers.Create("optimal").Start(a);
        a.StepDays(200);
        var b = SaveSerializer.Load(SaveSerializer.Save(a), TestKit.Content).World;
        a.StepDays(100);
        b.StepDays(100);
        Assert.Equal(SaveSerializer.StateHashHex(a), SaveSerializer.StateHashHex(b));
        Assert.True(Stumps(a) > 0);
        TestKit.AssertInvariants(a);
    }
}
