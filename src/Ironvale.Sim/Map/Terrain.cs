namespace Ironvale.Sim.Map;

public enum Ground : byte { Grass, Sand, Water }

/// <summary>
/// Static layers of a generated map (GDD v0.3 §8): terrace level, ground, water depth, ramps, fertility and forest
/// density. Rebuilt from seed + <see cref="GeneratorVersion"/> on load, never saved. What changes during play
/// (trees, stones, deposits) lives in <see cref="Nature"/>.
/// <para>Movement: water is never entered; neighbours on different levels are joined only by a ramp — a cell on the
/// lower level whose <see cref="RampDir"/> points at the higher neighbour.</para>
/// </summary>
public sealed class Terrain
{
    /// <summary>Bump whenever generation changes: older saves are then rejected (they would rebuild another map).</summary>
    public const int GeneratorVersion = 1;

    /// <summary>N, E, S, W (index = ramp direction).</summary>
    public static readonly (int Dx, int Dy)[] Dirs = { (0, -1), (1, 0), (0, 1), (-1, 0) };

    internal readonly byte[] Level;
    internal readonly Ground[] GroundAt;
    internal readonly byte[] Depth;
    internal readonly sbyte[] Ramp;
    internal readonly byte[] Fertility;
    internal readonly byte[] Forest;

    public int Width { get; }
    public int Height { get; }
    /// <summary>Number of terrace levels (3 or 4).</summary>
    public int Levels { get; internal set; }
    /// <summary>Centre of the starting clearing.</summary>
    public Cell Start { get; internal set; }
    public WorldGenInfo Info { get; internal set; } = new();

    internal Terrain(int width, int height)
    {
        Width = width;
        Height = height;
        int n = width * height;
        Level = new byte[n];
        GroundAt = new Ground[n];
        Depth = new byte[n];
        Ramp = new sbyte[n];
        Array.Fill(Ramp, (sbyte)-1);
        Fertility = new byte[n];
        Forest = new byte[n];
    }

    public int Index(Cell c) => c.Y * Width + c.X;
    public bool InBounds(Cell c) => c.X >= 0 && c.Y >= 0 && c.X < Width && c.Y < Height;

    public int LevelAt(Cell c) => Level[Index(c)];
    public Ground GroundOf(Cell c) => GroundAt[Index(c)];
    public bool IsWater(Cell c) => GroundAt[Index(c)] == Ground.Water;
    /// <summary>Cells from the shore (0 on land).</summary>
    public int DepthAt(Cell c) => Depth[Index(c)];
    /// <summary>Direction (index into <see cref="Dirs"/>) the ramp climbs toward, or −1.</summary>
    public int RampDir(Cell c) => Ramp[Index(c)];
    public bool IsRamp(Cell c) => Ramp[Index(c)] >= 0;
    /// <summary>0..255 raw fertility.</summary>
    public int FertilityAt(Cell c) => Fertility[Index(c)];
    /// <summary>Field yield multiplier in ‰: 600 (poor) .. 1300 (rich).</summary>
    public int FertilityPermille(Cell c) => 600 + Fertility[Index(c)] * 700 / 255;
    /// <summary>Forest density 0..255 (0 = outside a forest patch).</summary>
    public int ForestAt(Cell c) => Forest[Index(c)];

    /// <summary>Land that may hold a building: not water, not a ramp.</summary>
    public bool IsBuildable(Cell c) => GroundAt[Index(c)] != Ground.Water && Ramp[Index(c)] < 0;

    /// <summary>Can a walker step from <paramref name="a"/> into the 4-neighbour <paramref name="b"/>?</summary>
    public bool CanStep(Cell a, Cell b)
    {
        int ia = Index(a), ib = Index(b);
        if (GroundAt[ib] == Ground.Water || GroundAt[ia] == Ground.Water) return false;
        int la = Level[ia], lb = Level[ib];
        if (la == lb) return true;
        if (Math.Abs(la - lb) != 1) return false;
        // The lower cell must be a ramp facing the higher one.
        var (low, high, il) = la < lb ? (a, b, ia) : (b, a, ib);
        int d = Ramp[il];
        return d >= 0 && low.X + Dirs[d].Dx == high.X && low.Y + Dirs[d].Dy == high.Y;
    }
}

/// <summary>What the dice chose for this map (for screenshots, reports and tests of variety).</summary>
public sealed record WorldGenInfo
{
    /// <summary>Bit mask of coast sides: 1 = N, 2 = E, 4 = S, 8 = W.</summary>
    public int CoastSides { get; init; }
    public int Lakes { get; init; }
    public int Levels { get; init; }
    public int ForestPermille { get; init; }
    public DepositKind RichDeposit { get; init; }
    /// <summary>Generation attempt that produced a playable map (0 = first).</summary>
    public int Attempt { get; init; }
    /// <summary>Share (‰) of the land reachable on foot from the start.</summary>
    public int ReachablePermille { get; init; }
    /// <summary>Pond dug next to the start because no water was close enough.</summary>
    public bool StartPond { get; init; }
    /// <summary>Why earlier attempts were rejected (empty if the first one worked).</summary>
    public string Retries { get; init; } = "";
}
