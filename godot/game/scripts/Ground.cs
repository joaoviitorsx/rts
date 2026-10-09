using System;
using Godot;
using Ironvale.Sim.Map;

namespace Ironvale.Game;

/// <summary>
/// Height of the ground in the view (GDD v0.3 §8): flat map → 0; generated map → terrace level × <see cref="LevelHeight"/>,
/// ramps a third or two thirds up, water a little below its level. Visual only: the sim knows levels, not metres.
/// <see cref="VisualCatalog.CellToWorld"/> and the camera's mouse picking go through here, so buildings, villagers,
/// ghosts and selection follow the terraces without each knowing about them.
/// </summary>
public static class Ground
{
    public const float LevelHeight = 2.5f;
    public const float WaterDrop = 0.45f;

    public static Terrain? Terrain { get; set; }
    /// <summary>Height of the drawn ground (smooth ramps, cliff slopes), when a generated map's look is built.</summary>
    public static Func<float, float, float>? Visual { get; set; }
    public static float CellSize { get; set; } = 2f;

    /// <summary>Walkable surface height of a cell (water: its surface).</summary>
    public static float CellHeight(int x, int y)
    {
        if (Terrain is not { } t) return 0f;
        var c = new Cell(Mathf.Clamp(x, 0, t.Width - 1), Mathf.Clamp(y, 0, t.Height - 1));
        float h = t.LevelAt(c) * LevelHeight;
        if (t.IsWater(c)) return h - WaterDrop;
        int d = t.RampDir(c);
        if (d < 0) return h;
        var (dx, dy) = Terrain.Dirs[d];
        var up = new Cell(c.X + dx, c.Y + dy);
        bool front = t.InBounds(up) && t.LevelAt(up) > t.LevelAt(c);
        return h + LevelHeight * (front ? 2f / 3f : 1f / 3f);
    }

    public static float HeightAtWorld(float x, float z) =>
        Visual?.Invoke(x, z) ?? CellHeight(Mathf.FloorToInt(x / CellSize), Mathf.FloorToInt(z / CellSize));

    /// <summary>Height at the centre of a cell (where things stand): the drawn ground when there is one.</summary>
    public static float CellCenterHeight(int x, int y) =>
        Visual?.Invoke((x + 0.5f) * CellSize, (y + 0.5f) * CellSize) ?? CellHeight(x, y);

    /// <summary>First ground hit along a ray (terraces: marched in 0.5 m steps, then refined).</summary>
    public static Vector3? Raycast(Vector3 from, Vector3 dir)
    {
        if (Terrain is null)
        {
            if (Mathf.Abs(dir.Y) < 1e-4f) return null;
            float t0 = -from.Y / dir.Y;
            return t0 < 0 ? null : from + dir * t0;
        }
        float prev = 0;
        for (float t = 0.5f; t < 1200f; t += 0.5f)
        {
            var p = from + dir * t;
            if (p.Y > HeightAtWorld(p.X, p.Z)) { prev = t; continue; }
            float lo = prev, hi = t;
            for (int i = 0; i < 12; i++)
            {
                float mid = (lo + hi) / 2;
                var q = from + dir * mid;
                if (q.Y > HeightAtWorld(q.X, q.Z)) lo = mid; else hi = mid;
            }
            return from + dir * hi;
        }
        return null;
    }
}
