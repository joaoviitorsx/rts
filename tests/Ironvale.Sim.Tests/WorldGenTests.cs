using System.Diagnostics;

namespace Ironvale.Sim.Tests;

/// <summary>Generated maps (GDD v0.3 §8, plan v0.3 b1): determinism, variety, playability, saves.</summary>
public class WorldGenTests
{
    private static BalanceDef Bal => TestKit.Content.Balance;

    private static GeneratedWorld Gen(ulong seed) => WorldGen.Generate(seed, 192, 192, 0, Bal);

    [Theory]
    [InlineData(1UL)]
    [InlineData(42UL)]
    [InlineData(987654321UL)]
    public void Same_seed_gives_the_same_map(ulong seed) =>
        Assert.Equal(WorldGen.Fingerprint(Gen(seed)), WorldGen.Fingerprint(Gen(seed)));

    [Fact]
    public void Different_seeds_give_different_maps()
    {
        var prints = Enumerable.Range(1, 20).Select(s => WorldGen.Fingerprint(Gen((ulong)s))).ToList();
        Assert.Equal(prints.Count, prints.Distinct().Count());
    }

    [Fact]
    public void Every_seed_is_playable()
    {
        var failures = new List<string>();
        for (ulong seed = 1; seed <= 200; seed++)
        {
            var g = Gen(seed);
            foreach (var p in WorldCheck.Problems(g, 0, Bal)) failures.Add($"seed {seed}: {p}");
        }
        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    [Fact]
    public void Maps_vary_in_water_relief_and_riches()
    {
        var infos = Enumerable.Range(1, 60).Select(s => Gen((ulong)s).Terrain.Info).ToList();
        Assert.Contains(infos, i => i.CoastSides != 0);
        Assert.Contains(infos, i => i.CoastSides == 0 && i.Lakes > 0);
        Assert.Contains(infos, i => System.Numerics.BitOperations.PopCount((uint)i.CoastSides) == 2);
        Assert.Contains(infos, i => i.Levels == 3);
        Assert.Contains(infos, i => i.Levels == 4);
        foreach (var kind in new[] { DepositKind.Outcrop, DepositKind.Coal, DepositKind.Iron })
            Assert.Contains(infos, i => i.RichDeposit == kind);
        Assert.True(infos.Count(i => i.StartPond) < infos.Count / 2, "most starts should have natural water nearby");
    }

    [Fact]
    public void Generation_is_fast_enough()
    {
        Gen(1);   // warm-up (JIT)
        var sw = Stopwatch.StartNew();
        for (ulong seed = 100; seed < 120; seed++) Gen(seed);
        // GDD v0.3 §14: < 200 ms per map (Release). Tests run in Debug, which is ~2× slower.
        Assert.True(sw.ElapsedMilliseconds / 20 < 400, $"{sw.ElapsedMilliseconds / 20} ms per map");
    }

    [Fact]
    public void Walkers_never_cross_water_or_cliffs()
    {
        var w = World.Create(TestKit.Content, GeneratedScenario, 42);
        var t = w.Terrain!;
        var dist = WorldCheck.WalkingDistance(t, t.Start);
        // Farthest reachable cell on another level: the route must climb through ramps, step by legal step.
        int far = Enumerable.Range(0, dist.Length).Where(i => dist[i] != int.MaxValue)
            .OrderByDescending(i => dist[i]).ThenBy(i => i).First();
        var target = new Cell(far % t.Width, far / t.Width);
        var route = w.Paths.Route(t.Start, target);
        Assert.Equal(target, route[^1]);
        var prev = t.Start;
        foreach (var c in route)
        {
            Assert.True(t.CanStep(prev, c), $"illegal step {prev} → {c}");
            prev = c;
        }
    }

    // ------------------------------------------------------------ world + save

    private static readonly Lazy<ScenarioDef> Generated =
        new(() => DataPaths.LoadWithScenario(DataPaths.FindDataDirectory(), "mvp_generated").Scenario);

    private static ScenarioDef GeneratedScenario => Generated.Value;

    [Fact]
    public void The_hall_sits_on_the_start_clearing()
    {
        var w = World.Create(TestKit.Content, GeneratedScenario, 7);
        var hall = w.SeatBuilding!;
        Assert.Equal(w.Terrain!.Start, hall.Center);
        Assert.Equal(192, w.Map.Width);
    }

    [Fact]
    public void Generated_world_survives_save_and_load()
    {
        var w = World.Create(TestKit.Content, GeneratedScenario, 11);
        MvpOpeningAtStart(w);
        w.StepDays(30);
        // A felled tree must come back from the save as a change, not as the generated tree.
        var nature = w.Nature!;
        int tree = Enumerable.Range(0, w.Map.Width * w.Map.Height).First(i => nature.At(i).Kind == NodeKind.Tree);
        nature.Set(tree, new NatureNode(NodeKind.Tree, 0, w.Tick + 999, 0));
        nature.Deposits[0].Units -= 5;

        var loaded = SaveSerializer.Load(SaveSerializer.Save(w), TestKit.Content).World;
        Assert.Equal(SaveSerializer.StateHashHex(w), SaveSerializer.StateHashHex(loaded));
        Assert.Equal(w.Tick + 999, loaded.Nature!.At(tree).Tick);
        Assert.Equal(nature.Deposits[0].Units, loaded.Nature.Deposits[0].Units);

        w.StepDays(30);
        loaded.StepDays(30);
        Assert.Equal(SaveSerializer.StateHashHex(w), SaveSerializer.StateHashHex(loaded));
    }

    [Fact]
    public void Flat_saves_carry_no_generated_map()
    {
        var json = System.Text.Encoding.UTF8.GetString(Gunzip(SaveSerializer.Save(TestKit.NewWorld())));
        Assert.DoesNotContain("\"generated\"", json);
    }

    private static byte[] Gunzip(byte[] data)
    {
        using var input = new System.IO.Compression.GZipStream(new MemoryStream(data), System.IO.Compression.CompressionMode.Decompress);
        using var output = new MemoryStream();
        input.CopyTo(output);
        return output.ToArray();
    }

    /// <summary>Two carriers on the hall (the scripted layouts are for the flat map).</summary>
    private static void MvpOpeningAtStart(World w)
    {
        foreach (var h in w.Households.Take(2)) w.Enqueue(new AssignHousehold(h.Id, w.SeatBuilding!.Id));
    }
}
