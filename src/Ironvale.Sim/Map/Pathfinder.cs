namespace Ironvale.Sim.Map;

/// <summary>
/// Walking costs and routes (Marco 2A.3). For each target cell a Dijkstra distance field (ticks to reach the target)
/// is computed once and cached until the map changes; a walker always steps to the neighbour that minimises
/// cost-to-enter + distance, ties broken N, E, S, W. Memoryless and deterministic: routes never need saving.
/// <para>Entering a road cell costs <c>ticksPerCellRoad</c>, open ground <c>ticksPerCellOffroad</c>; cells of other
/// buildings cost <see cref="BuildingCostFactor"/>× off-road (walkers go around them, but are never stuck).</para>
/// <para>Generated maps (GDD v0.3): water and cliffs cannot be crossed, ramps join the terraces. Unreachable cells keep
/// <see cref="int.MaxValue"/> ticks.</para>
/// </summary>
public sealed class Pathfinder
{
    public const int BuildingCostFactor = 6;
    private static readonly (int dx, int dy)[] Steps = { (0, -1), (1, 0), (0, 1), (-1, 0) };

    private readonly GridMap _map;
    private readonly int _road;
    private readonly int _offroad;
    /// <summary>Distance fields kept (oldest dropped first): units chasing trees ask for many targets. Pure cache.</summary>
    public const int MaxFields = 96;
    private readonly Dictionary<int, int[]> _fields = new();
    private readonly Queue<int> _fieldOrder = new();
    private int _version = -1;

    public Pathfinder(GridMap map, BalanceDef balance)
    {
        _map = map;
        _road = balance.TicksPerCellRoad;
        _offroad = balance.TicksPerCellOffroad;
    }

    /// <summary>Ticks to walk into <paramref name="c"/> when heading to a target inside building <paramref name="targetBuilding"/>.</summary>
    public int EnterCost(Cell c, int targetBuilding)
    {
        if (_map.IsRoad(c)) return _road;
        int b = _map.BuildingAt(c);
        return b != 0 && b != targetBuilding ? _offroad * BuildingCostFactor : _offroad;
    }

    /// <summary>Ticks from <paramref name="from"/> to <paramref name="target"/> along the best route.</summary>
    public int Ticks(Cell from, Cell target)
    {
        if (from == target) return 0;
        return Field(target)[_map.Index(Clamp(from))];
    }

    /// <summary>Next cell on the best route (the target itself when adjacent; <paramref name="from"/> when there).</summary>
    public Cell NextStep(Cell from, Cell target)
    {
        if (from == target) return from;
        var field = Field(target);
        int targetBuilding = _map.BuildingAt(target);
        Cell best = from;
        long bestValue = long.MaxValue;
        foreach (var (dx, dy) in Steps)
        {
            var n = new Cell(from.X + dx, from.Y + dy);
            if (!_map.InBounds(n) || !_map.CanStep(from, n) || field[_map.Index(n)] == int.MaxValue) continue;
            long v = (long)EnterCost(n, targetBuilding) + field[_map.Index(n)];
            if (v < bestValue)
            {
                bestValue = v;
                best = n;
            }
        }
        return best;
    }

    /// <summary>Cells of the route from <paramref name="from"/> (excluded) to <paramref name="target"/> (included).</summary>
    public List<Cell> Route(Cell from, Cell target)
    {
        var route = new List<Cell>();
        var c = from;
        for (int guard = 0; c != target && guard < _map.Width * _map.Height; guard++)
        {
            c = NextStep(c, target);
            route.Add(c);
        }
        return route;
    }

    private Cell Clamp(Cell c) => new(Math.Clamp(c.X, 0, _map.Width - 1), Math.Clamp(c.Y, 0, _map.Height - 1));

    private int[] Field(Cell target)
    {
        if (_version != _map.Version)
        {
            _fields.Clear();
            _fieldOrder.Clear();
            _version = _map.Version;
        }
        target = Clamp(target);
        int key = _map.Index(target);
        if (_fields.TryGetValue(key, out var cached)) return cached;

        int n = _map.Width * _map.Height;
        int targetBuilding = _map.BuildingAt(target);
        var dist = new int[n];
        Array.Fill(dist, int.MaxValue);
        dist[key] = 0;
        var queue = new PriorityQueue<int, long>();
        queue.Enqueue(key, key);
        while (queue.TryDequeue(out int v, out long priority))
        {
            if (priority / n != dist[v]) continue;   // stale entry
            var vc = _map.CellAt(v);
            int stepCost = EnterCost(vc, targetBuilding);   // a neighbour u reaches v by entering v
            int nd = dist[v] + stepCost;
            foreach (var (dx, dy) in Steps)
            {
                var uc = new Cell(vc.X + dx, vc.Y + dy);
                if (!_map.InBounds(uc) || !_map.CanStep(uc, vc)) continue;
                int u = _map.Index(uc);
                if (nd >= dist[u]) continue;
                dist[u] = nd;
                queue.Enqueue(u, (long)nd * n + u);   // ties by cell index: deterministic
            }
        }
        _fields[key] = dist;
        _fieldOrder.Enqueue(key);
        if (_fieldOrder.Count > MaxFields) _fields.Remove(_fieldOrder.Dequeue());
        return dist;
    }
}
