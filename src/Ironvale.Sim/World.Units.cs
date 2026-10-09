namespace Ironvale.Sim;

public enum Weather { Clear, Cloudy, Rain, Snow }

/// <summary>
/// The RTS opening (GDD v0.3 §4–§9, briefing step 2): units under direct control, wild animals, resources lying on
/// the ground and the weather. Everything here exists only on generated maps; on the flat map the lists stay empty and
/// no random stream is touched, so the 2A results and hashes do not move.
/// </summary>
public sealed partial class World
{
    private readonly List<Unit> _units = new();
    private readonly List<Animal> _animals = new();
    private readonly List<GroundItem> _ground = new();

    public IReadOnlyList<Unit> Units => _units;
    public IReadOnlyList<Animal> Animals => _animals;
    public IReadOnlyList<GroundItem> GroundItems => _ground;

    public Weather WeatherToday { get; internal set; }
    /// <summary>Tomorrow's weather is known a day ahead (clouds on the horizon: "chuva chegando").</summary>
    public Weather WeatherTomorrow { get; internal set; }
    /// <summary>Has it rained yet (the first rain is guaranteed by balance.firstRainDay)?</summary>
    public bool RainSeen { get; internal set; }

    public Unit? GetUnit(int id) => _units.FirstOrDefault(u => u.Id == id);
    public Animal? GetAnimal(int id) => _animals.FirstOrDefault(a => a.Id == id);
    public GroundItem? GetGroundItem(int id) => _ground.FirstOrDefault(g => g.Id == id);

    public int ColonistCount => _units.Count(u => u.IsColonist && u.Controllable);

    // ------------------------------------------------------------ creation

    /// <summary>Scenario units around the start clearing, animals at the generated spawns, the first forecast.</summary>
    internal void SpawnOpening(ScenarioDef scenario)
    {
        if (Terrain is not { } t) return;
        var spots = FreeCellsAround(t.Start, scenario.Units.Count);
        for (int i = 0; i < scenario.Units.Count; i++)
        {
            var su = scenario.Units[i];
            AddUnit(new Unit
            {
                Id = NewId(), Kind = su.Kind == "ox" ? UnitKind.Ox : UnitKind.Colonist, Name = su.Name,
                Pos = spots[i], Next = spots[i],
            });
        }
        var fauna = Nature!.Fauna;
        for (int herd = 0; herd < fauna.Count; herd++)
            foreach (var cell in FreeCellsAround(fauna[herd].Cell, fauna[herd].Count))
                AddAnimal(fauna[herd].Kind, herd, fauna[herd].Cell, cell);
        WeatherToday = Weather.Clear;
        WeatherTomorrow = Ironvale.Sim.Systems.WeatherSystem.Roll(this, Calendar.TotalDays + 1);
    }

    /// <summary>Walkable cells without buildings, spiralling out from <paramref name="center"/> (deterministic).</summary>
    internal List<Cell> FreeCellsAround(Cell center, int count)
    {
        var list = new List<Cell>();
        for (int r = 0; list.Count < count && r < 40; r++)
        for (int y = center.Y - r; y <= center.Y + r && list.Count < count; y++)
        for (int x = center.X - r; x <= center.X + r && list.Count < count; x++)
        {
            if (Math.Max(Math.Abs(x - center.X), Math.Abs(y - center.Y)) != r) continue;
            var c = new Cell(x, y);
            if (!Map.InBounds(c) || Map.BuildingAt(c) != 0 || Terrain is { } t && t.IsWater(c)) continue;
            list.Add(c);
        }
        while (list.Count < count) list.Add(center);
        return list;
    }

    internal void AddUnit(Unit u) => _units.Add(u);

    internal Animal AddAnimal(FaunaKind kind, int herd, Cell home, Cell at)
    {
        var a = new Animal { Id = NewId(), Kind = kind, Herd = herd, Home = home, Pos = at, Next = at, Goal = at };
        _animals.Add(a);
        return a;
    }

    internal void InsertAnimal(Animal a) => _animals.Add(a);

    internal void RemoveAnimal(Animal a) => _animals.Remove(a);

    /// <summary>A colonist leaves the band (hunger/cold): the load stays on the ground where they stood.</summary>
    internal void RemoveUnit(Unit u, string reason)
    {
        DropLoad(u);
        _units.Remove(u);
        Emit(new UnitLeft(Tick, u.Id, u.Name, reason));
    }

    // ------------------------------------------------------------ ground items

    internal GroundItem AddGroundItem(Cell cell, int resource, Qty amount)
    {
        // Same resource on the same cell piles up (a load dropped where a log lies).
        var existing = _ground.FirstOrDefault(g => g.Cell == cell && g.Resource == resource);
        if (existing is not null)
        {
            existing.Amount += amount;
            return existing;
        }
        var item = new GroundItem { Id = NewId(), Cell = cell, Resource = resource, Amount = amount };
        _ground.Add(item);
        return item;
    }

    internal void InsertGroundItem(GroundItem g) => _ground.Add(g);

    internal void RemoveGroundItem(GroundItem g) => _ground.Remove(g);

    internal void DropLoad(Unit u)
    {
        if (!u.IsCarrying) return;
        AddGroundItem(u.Pos, u.CarryResource, u.CarryAmount);
        u.CarryResource = -1;
        u.CarryAmount = Qty.Zero;
    }

    // ------------------------------------------------------------ carrying

    /// <summary>How much of <paramref name="r"/> this unit can carry per trip (0 = it cannot).</summary>
    public Qty CarryCapacity(Unit u, int r) =>
        u.Kind == UnitKind.Ox ? Content.Resources[r].OxCarry : Content.Resources[r].ColonistCarry;

    /// <summary>Room left in the hands for <paramref name="r"/> (0 when carrying something else).</summary>
    internal Qty RoomFor(Unit u, int r) =>
        u.IsCarrying && u.CarryResource != r ? Qty.Zero : Qty.Max(Qty.Zero, CarryCapacity(u, r) - u.CarryAmount);

    internal void Load(Unit u, int r, Qty q)
    {
        if (!q.IsPositive) return;
        u.CarryResource = r;
        u.CarryAmount += q;
    }

    /// <summary>Nearest active storage with room for the unit's load (Manhattan, ties by id), or null.</summary>
    internal Building? StorageFor(Unit u) =>
        StoragesByDistance(u.Pos).FirstOrDefault(b => b.Stock.Space.IsPositive);

    // ------------------------------------------------------------ walking

    internal enum Walk { Arrived, Walking, Blocked }

    /// <summary>
    /// One tick of walking toward <paramref name="goal"/> (a building's cells count as arrived when
    /// <paramref name="building"/> ≠ 0). Steps follow the cached distance fields, so the route needs no saving.
    /// </summary>
    internal Walk WalkToward(Unit u, Cell goal, int building = 0)
    {
        if (u.IsMoving)
        {
            if (--u.StepTicks > 0) return Walk.Walking;
            u.Pos = u.Next;
        }
        if (building != 0 ? Map.BuildingAt(u.Pos) == building : u.Pos == goal) return Walk.Arrived;
        var next = Paths.NextStep(u.Pos, goal);
        if (next == u.Pos) return Walk.Blocked;
        StartStep(u, next, building);
        return Walk.Walking;
    }

    /// <summary>Chasing a moving target: greedy steps (no new distance field per animal step); the field only when stuck.</summary>
    internal Walk ChaseToward(Unit u, Cell goal)
    {
        if (u.IsMoving)
        {
            if (--u.StepTicks > 0) return Walk.Walking;
            u.Pos = u.Next;
        }
        if (u.Pos == goal) return Walk.Arrived;
        var next = GreedyStep(u.Pos, goal);
        if (next == u.Pos) next = Paths.NextStep(u.Pos, goal);
        if (next == u.Pos) return Walk.Blocked;
        StartStep(u, next, 0);
        return Walk.Walking;
    }

    private void StartStep(Unit u, Cell next, int building)
    {
        var bal = Content.Balance;
        int permille = u.Kind == UnitKind.Ox ? bal.OxStepPermille : bal.ColonistStepPermille;
        u.Next = next;
        u.StepTicks = Math.Max(1, Paths.EnterCost(next, building) * permille / Permille.One);
    }

    /// <summary>The 4-neighbour that gets closest (Manhattan) to <paramref name="goal"/>, or <paramref name="from"/>.</summary>
    internal Cell GreedyStep(Cell from, Cell goal)
    {
        Cell best = from;
        int bestD = from.Manhattan(goal);
        foreach (var (dx, dy) in Terrain.Dirs)
        {
            var n = new Cell(from.X + dx, from.Y + dy);
            if (!Map.InBounds(n) || !Map.CanStep(from, n)) continue;
            int d = n.Manhattan(goal);
            if (d < bestD)
            {
                bestD = d;
                best = n;
            }
        }
        return best;
    }

    public static int Chebyshev(Cell a, Cell b) => Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));

    // ------------------------------------------------------------ gatherable nodes

    /// <summary>Can a colonist take something from this node right now (mature tree, stone left, fruit/mushrooms in season)?</summary>
    public bool Gatherable(Cell c)
    {
        if (Nature is not { } nature || !Map.InBounds(c)) return false;
        var node = nature.At(c);
        var cal = Calendar;
        return node.Kind switch
        {
            NodeKind.Tree => Nature.StageOf(node, Tick, Content.Balance) == TreeStage.Mature,
            NodeKind.Stone => node.Amount > 0,
            NodeKind.Bush => cal.Season is Season.Summer or Season.Autumn && node.Tick < SeasonStartTick(cal),
            NodeKind.Mushroom => cal.Season == Season.Autumn && node.Tick < SeasonStartTick(cal),
            _ => false,
        };
    }

    private static long SeasonStartTick(in Calendar cal) => cal.Tick - cal.Tick % SimTime.TicksPerSeason;

    /// <summary>Another unit already goes for this node.</summary>
    internal bool Reserved(Cell c, Unit except) =>
        _units.Any(u => u != except && u.Order is { Kind: OrderKind.Gather } o && o.Cell == c);

    /// <summary>
    /// Nearest gatherable node of <paramref name="kind"/> within <paramref name="radius"/> (Chebyshev) of
    /// <paramref name="from"/>, not taken by another unit (Manhattan, ties by cell index).
    /// </summary>
    public Cell? NearestNode(NodeKind kind, Cell from, int radius, Unit except, HashSet<Cell>? taken = null)
    {
        if (Nature is not { } nature) return null;
        Cell? best = null;
        int bestD = int.MaxValue;
        for (int y = Math.Max(0, from.Y - radius); y <= Math.Min(Map.Height - 1, from.Y + radius); y++)
        for (int x = Math.Max(0, from.X - radius); x <= Math.Min(Map.Width - 1, from.X + radius); x++)
        {
            var c = new Cell(x, y);
            if (nature.At(c).Kind != kind || !Gatherable(c) || Reserved(c, except) || taken?.Contains(c) == true) continue;
            int d = c.Manhattan(from);
            if (d < bestD)
            {
                bestD = d;
                best = c;
            }
        }
        return best;
    }

    internal bool ColonistsNear(Cell c, int radius, int atLeast) =>
        _units.Count(u => u.IsColonist && u.Controllable && Chebyshev(u.Pos, c) <= radius) >= atLeast;

    /// <summary>An active fire burning today within <paramref name="radius"/> cells.</summary>
    internal bool FireNear(Cell c, int radius) =>
        _buildings.Any(b => b.IsActive && b.Burning && b.Def.Has(BuildingRole.Fire) && Chebyshev(b.Center, c) <= radius);
}
