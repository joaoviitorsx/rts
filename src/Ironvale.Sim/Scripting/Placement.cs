namespace Ironvale.Sim.Scripting;

/// <summary>
/// Where a scripted player puts a building. The 2A layouts are absolute cells of the flat map (hall centred at
/// <see cref="FlatHallCenter"/>); on the flat map they are used as they are (results unchanged). On a generated map
/// the same offset from the hall is the wish, and the nearest valid spot is searched around it — woodcutters
/// prefer mature trees in reach, fields fertile soil, quarries the nearest outcrop. Commands are applied at once so
/// the next search sees the previous building.
/// </summary>
public static class Placement
{
    public static readonly Cell FlatHallCenter = new(31, 31);
    private const int SearchRadius = 30;

    public static void Place(World w, string defId, Cell flatOrigin)
    {
        if (w.Terrain is null)
        {
            w.Enqueue(new PlaceBuilding(defId, flatOrigin, 0));
            return;
        }
        if (Find(w, defId, flatOrigin) is { } origin)
        {
            w.Enqueue(new PlaceBuilding(defId, origin, 0));
            w.ApplyPendingCommands();
        }
    }

    public static Cell? Find(World w, string defId, Cell flatOrigin)
    {
        var def = w.Content.Building(defId);
        var hall = w.SeatBuilding?.Center ?? w.Terrain!.Start;
        if (def.Harvests == HarvestSource.Outcrop)
        {
            return w.Nature!.Deposits
                .Where(d => d.Kind == DepositKind.Outcrop && w.CanPlace(def, d.Origin, 0) && w.Paths.Ticks(d.Center, hall) != int.MaxValue)
                .OrderBy(d => w.Paths.Ticks(d.Center, hall)).ThenBy(d => d.Index)
                .Select(d => (Cell?)d.Origin).FirstOrDefault();
        }

        var want = new Cell(flatOrigin.X + hall.X - FlatHallCenter.X, flatOrigin.Y + hall.Y - FlatHallCenter.Y);
        Cell? best = null;
        long bestScore = long.MinValue;
        for (int y = want.Y - SearchRadius; y <= want.Y + SearchRadius; y++)
        for (int x = want.X - SearchRadius; x <= want.X + SearchRadius; x++)
        {
            var c = new Cell(x, y);
            if (!w.Map.InBounds(c) || !w.CanPlace(def, c, 0)) continue;
            var (fw, fh) = GridMap.Footprint(def, 0);
            var center = new Cell(x + fw / 2, y + fh / 2);
            int ticks = w.Paths.Ticks(center, hall);
            if (ticks == int.MaxValue) continue;   // cut off by water or cliffs
            // Same layout as on the flat map, but no one builds across a cliff from the hall: detours cost.
            int detour = Math.Max(0, ticks / w.Content.Balance.TicksPerCellOffroad - center.Manhattan(hall));
            long score = -10L * c.Manhattan(want) - 30L * detour - TreesUnder(w, def, c) * 25L;
            if (def.Harvests == HarvestSource.Trees) score += MatureTreesNear(w, center, def.WorkRadius) * 4L;
            if (def.Recipes.Any(r => r.Kind == RecipeKind.Seasonal)) score += w.FertilityPermille(def, c, 0) / 4;
            if (score > bestScore)
            {
                bestScore = score;
                best = c;
            }
        }
        return best;
    }

    private static int TreesUnder(World w, BuildingDef def, Cell origin)
    {
        var (fw, fh) = GridMap.Footprint(def, 0);
        int n = 0;
        for (int y = origin.Y; y < origin.Y + fh; y++)
        for (int x = origin.X; x < origin.X + fw; x++)
            if (w.Nature!.At(new Cell(x, y)).Kind != NodeKind.None) n++;
        return n;
    }

    private static int MatureTreesNear(World w, Cell center, int r)
    {
        int n = 0;
        for (int y = center.Y - r; y <= center.Y + r; y++)
        for (int x = center.X - r; x <= center.X + r; x++)
        {
            var c = new Cell(x, y);
            if (!w.Map.InBounds(c)) continue;
            var node = w.Nature!.At(c);
            if (node.Kind == NodeKind.Tree && Nature.StageOf(node, w.Tick, w.Content.Balance) == TreeStage.Mature) n++;
        }
        return n;
    }

    /// <summary>Generated maps: a road from the hall toward each building (at most <paramref name="maxCells"/> cells in all).</summary>
    public static void RoadsFromHall(World w, int maxCells)
    {
        var hall = w.SeatBuilding;
        if (w.Terrain is null || hall is null) return;
        var cells = new List<Cell>();
        foreach (var b in w.Buildings.Where(b => b.Id != hall.Id).OrderBy(b => b.Id))
            foreach (var c in w.Paths.Route(hall.Center, b.Center))
                if (w.Map.BuildingAt(c) == 0 && w.RoadAllowed(c) && !cells.Contains(c)) cells.Add(c);
        if (cells.Count > 0) w.Enqueue(new PlaceRoad(cells.Take(maxCells).ToArray()));
    }
}
