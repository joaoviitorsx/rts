namespace Ironvale.Sim.Map;

public enum NodeKind : byte { None, Tree, Bush, Mushroom, Stone }

public enum TreeSpecies : byte { Oak, Birch, Pine }

public enum TreeStage : byte { Stump, Sapling, Young, Mature }

public enum DepositKind : byte { None, Outcrop, Coal, Iron }

public enum FaunaKind : byte { Deer, Rabbit, Wolf }

/// <summary>
/// One resource node in a cell (GDD v0.3 §7). Growth is implicit in <see cref="Tick"/>, so time passing never has to
/// touch the map:
/// <list type="bullet">
/// <item>Tree: tick it was planted (a future tick = still a stump until then).</item>
/// <item>Bush / mushroom: tick of the last harvest.</item>
/// <item>Stone: <see cref="Amount"/> units left.</item>
/// </list>
/// </summary>
public readonly record struct NatureNode(NodeKind Kind, byte Variant, long Tick, int Amount)
{
    public static readonly NatureNode Empty = default;
    public TreeSpecies Species => (TreeSpecies)Variant;
}

/// <summary>A 3×3 deposit: an outcrop for a quarry, or coal/iron (visible, mined later).</summary>
public sealed class Deposit
{
    public const int Size = 3;
    public int Index { get; internal init; }
    public DepositKind Kind { get; internal init; }
    public Cell Origin { get; internal init; }
    public long InitialUnits { get; internal set; }
    public long Units { get; internal set; }
    /// <summary>The map's rich deposit (one per map): the year-2 specialisation (GDD v0.3 §7).</summary>
    public bool Rich { get; internal set; }
    public Cell Center => new(Origin.X + Size / 2, Origin.Y + Size / 2);
    public bool Contains(Cell c) => c.X >= Origin.X && c.Y >= Origin.Y && c.X < Origin.X + Size && c.Y < Origin.Y + Size;
}

/// <summary>Where a group of wild animals starts (fauna becomes simulated in plan stage b4).</summary>
public readonly record struct FaunaSpawn(FaunaKind Kind, Cell Cell, int Count);

/// <summary>
/// The living part of a generated map: trees, bushes, mushrooms, loose stones and deposits. The generated layout is
/// rebuilt from the seed; only cells that differ from it (<see cref="Changes"/>) and deposit amounts are saved and
/// hashed.
/// </summary>
public sealed class Nature
{
    private readonly NodeKind[] _kind;
    private readonly byte[] _variant;
    private readonly long[] _tick;
    private readonly int[] _amount;
    private readonly SortedDictionary<int, NatureNode> _changes = new();
    private readonly List<Deposit> _deposits = new();
    private readonly short[] _depositAt;

    public int Width { get; }
    public int Height { get; }
    public IReadOnlyList<Deposit> Deposits => _deposits;
    public IReadOnlyList<FaunaSpawn> Fauna { get; internal set; } = Array.Empty<FaunaSpawn>();
    /// <summary>Cell index → node, for every cell that differs from the generated layout.</summary>
    public IReadOnlyDictionary<int, NatureNode> Changes => _changes;
    /// <summary>Bumped on every change (views rebuild what they show).</summary>
    public int Version { get; private set; }

    internal Nature(int width, int height)
    {
        Width = width;
        Height = height;
        int n = width * height;
        _kind = new NodeKind[n];
        _variant = new byte[n];
        _tick = new long[n];
        _amount = new int[n];
        _depositAt = new short[n];
    }

    public int Index(Cell c) => c.Y * Width + c.X;

    public NatureNode At(int index) =>
        _changes.TryGetValue(index, out var changed) ? changed
            : new NatureNode(_kind[index], _variant[index], _tick[index], _amount[index]);

    public NatureNode At(Cell c) => At(Index(c));

    /// <summary>Deposit covering the cell, or null.</summary>
    public Deposit? DepositAt(Cell c)
    {
        int d = _depositAt[Index(c)];
        return d == 0 ? null : _deposits[d - 1];
    }

    /// <summary>Changes a node during play (stored as a difference from the generated layout).</summary>
    internal void Set(int index, NatureNode node)
    {
        var generated = new NatureNode(_kind[index], _variant[index], _tick[index], _amount[index]);
        if (node == generated) _changes.Remove(index);
        else _changes[index] = node;
        Version++;
    }

    internal void Clear(Cell c) => Set(Index(c), NatureNode.Empty);

    // ------------------------------------------------------------ generation (writes the base layout)

    internal void Generate(int index, NatureNode node)
    {
        _kind[index] = node.Kind;
        _variant[index] = node.Variant;
        _tick[index] = node.Tick;
        _amount[index] = node.Amount;
    }

    internal Deposit AddDeposit(DepositKind kind, Cell origin, long units, bool rich)
    {
        var d = new Deposit { Index = _deposits.Count, Kind = kind, Origin = origin, InitialUnits = units, Units = units, Rich = rich };
        _deposits.Add(d);
        for (int y = 0; y < Deposit.Size; y++)
        for (int x = 0; x < Deposit.Size; x++)
            _depositAt[(origin.Y + y) * Width + origin.X + x] = (short)(d.Index + 1);
        return d;
    }

    // ------------------------------------------------------------ queries

    public static TreeStage StageOf(NatureNode tree, long now, BalanceDef bal)
    {
        long age = now - tree.Tick;
        if (age < 0) return TreeStage.Stump;
        long days = age / SimTime.TicksPerDay;
        if (days >= bal.TreeMatureDays) return TreeStage.Mature;
        return days >= bal.TreeMatureDays / 4 ? TreeStage.Young : TreeStage.Sapling;
    }

    public int Count(NodeKind kind)
    {
        int n = 0;
        for (int i = 0; i < _kind.Length; i++)
            if (At(i).Kind == kind) n++;
        return n;
    }
}
