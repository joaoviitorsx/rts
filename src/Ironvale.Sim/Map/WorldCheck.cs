namespace Ironvale.Sim.Map;

/// <summary>
/// "Playable map" rules of GDD v0.3 §8.3/§14, checked on a generated map: flat clearing at the start, water, forest,
/// loose stone, bushes, an outcrop and a deer herd at a reasonable walking distance, coal and iron somewhere, one rich
/// deposit, ≥ 95 % of the land reachable. Used by the tests over many seeds and by the CLI report.
/// </summary>
public static class WorldCheck
{
    public static List<string> Problems(GeneratedWorld g, long now, BalanceDef bal)
    {
        var t = g.Terrain;
        var nat = g.Nature;
        var problems = new List<string>();
        var dist = WalkingDistance(t, t.Start);
        int n = t.Width * t.Height;

        int level = t.LevelAt(t.Start);
        for (int y = t.Start.Y - 7; y <= t.Start.Y + 7; y++)
        for (int x = t.Start.X - 7; x <= t.Start.X + 7; x++)
        {
            var c = new Cell(x, y);
            if (!t.InBounds(c) || !t.IsBuildable(c) || t.GroundOf(c) != Ground.Grass || t.LevelAt(c) != level || nat.DepositAt(c) is not null)
            {
                problems.Add($"clearing not flat at {c}");
                goto clearingDone;
            }
        }
        clearingDone:

        int land = 0, reached = 0;
        for (int i = 0; i < n; i++)
        {
            if (t.GroundAt[i] == Ground.Water) continue;
            land++;
            if (dist[i] != int.MaxValue) reached++;
        }
        if (reached * 1000L < land * 950L) problems.Add($"only {reached * 1000L / land}‰ of the land reachable");

        int Count(Func<int, bool> pred, int min, int max)
        {
            int k = 0;
            for (int i = 0; i < n; i++)
                if (dist[i] >= min && dist[i] <= max && pred(i)) k++;
            return k;
        }

        if (Count(i => NextToWater(t, i), 0, 15) == 0) problems.Add("no water within 15 cells");
        int trees = Count(i => nat.At(i) is { Kind: NodeKind.Tree } node && Nature.StageOf(node, now, bal) == TreeStage.Mature, 4, 14);
        if (trees < 25) problems.Add($"{trees} mature trees within 4–14 cells");
        int stones = Count(i => nat.At(i).Kind == NodeKind.Stone, 0, 15);
        if (stones < 10) problems.Add($"{stones} loose stones within 15 cells");
        int bushes = Count(i => nat.At(i).Kind == NodeKind.Bush, 0, 20);
        if (bushes < 6) problems.Add($"{bushes} bushes within 20 cells");
        if (!nat.Deposits.Any(d => d.Kind == DepositKind.Outcrop && dist[t.Index(d.Center)] <= 30)) problems.Add("no outcrop within 30 cells");
        foreach (var kind in new[] { DepositKind.Coal, DepositKind.Iron })
            if (!nat.Deposits.Any(d => d.Kind == kind)) problems.Add($"no {kind} deposit");
        if (nat.Deposits.Count(d => d.Rich) != 1) problems.Add("not exactly one rich deposit");
        if (!nat.Fauna.Any(f => f.Kind == FaunaKind.Deer && dist[t.Index(f.Cell)] <= 30)) problems.Add("no deer within 30 cells");
        return problems;
    }

    private static bool NextToWater(Terrain t, int i)
    {
        if (t.GroundAt[i] == Ground.Water) return false;
        var c = new Cell(i % t.Width, i / t.Width);
        foreach (var (dx, dy) in Terrain.Dirs)
        {
            var nb = new Cell(c.X + dx, c.Y + dy);
            if (t.InBounds(nb) && t.IsWater(nb)) return true;
        }
        return false;
    }

    /// <summary>Steps (4-neighbour, respecting water, cliffs and ramps) from <paramref name="from"/>; unreachable = int.MaxValue.</summary>
    public static int[] WalkingDistance(Terrain t, Cell from)
    {
        var dist = new int[t.Width * t.Height];
        Array.Fill(dist, int.MaxValue);
        var queue = new Queue<Cell>();
        dist[t.Index(from)] = 0;
        queue.Enqueue(from);
        while (queue.TryDequeue(out var c))
        {
            foreach (var (dx, dy) in Terrain.Dirs)
            {
                var nb = new Cell(c.X + dx, c.Y + dy);
                if (!t.InBounds(nb) || dist[t.Index(nb)] != int.MaxValue || !t.CanStep(c, nb)) continue;
                dist[t.Index(nb)] = dist[t.Index(c)] + 1;
                queue.Enqueue(nb);
            }
        }
        return dist;
    }
}
