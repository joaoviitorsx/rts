namespace Ironvale.Sim.Map;

public readonly record struct Cell(int X, int Y)
{
    public int Manhattan(Cell other) => Math.Abs(X - other.X) + Math.Abs(Y - other.Y);

    /// <summary>One 4-neighbour step toward target: X first, then Y (deterministic L-shaped path).</summary>
    public Cell StepToward(Cell target)
    {
        if (X != target.X) return new Cell(X + Math.Sign(target.X - X), Y);
        if (Y != target.Y) return new Cell(X, Y + Math.Sign(target.Y - Y));
        return this;
    }
}

/// <summary>Square tile grid. Each cell stores the id of the building occupying it (0 = free).</summary>
public sealed class GridMap
{
    private readonly int[] _occupancy;

    public int Width { get; }
    public int Height { get; }

    public GridMap(int width, int height)
    {
        Width = width;
        Height = height;
        _occupancy = new int[width * height];
    }

    public bool InBounds(Cell c) => c.X >= 0 && c.Y >= 0 && c.X < Width && c.Y < Height;

    public int BuildingAt(Cell c) => InBounds(c) ? _occupancy[c.Y * Width + c.X] : 0;

    public static (int w, int h) Footprint(BuildingDef def, int rotation) =>
        (rotation & 1) == 0 ? (def.FootprintW, def.FootprintH) : (def.FootprintH, def.FootprintW);

    public bool CanPlace(BuildingDef def, Cell origin, int rotation)
    {
        var (w, h) = Footprint(def, rotation);
        for (int y = origin.Y; y < origin.Y + h; y++)
        for (int x = origin.X; x < origin.X + w; x++)
        {
            var c = new Cell(x, y);
            if (!InBounds(c) || BuildingAt(c) != 0) return false;
        }
        return true;
    }

    internal void Fill(BuildingDef def, Cell origin, int rotation, int buildingId)
    {
        var (w, h) = Footprint(def, rotation);
        for (int y = origin.Y; y < origin.Y + h; y++)
        for (int x = origin.X; x < origin.X + w; x++)
            _occupancy[y * Width + x] = buildingId;
    }
}
