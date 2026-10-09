namespace Ironvale.Sim;

/// <summary>
/// The economy on a generated map (GDD v0.3 §9, plan b2): placement rules, site clearing, woodcutters felling real
/// trees, quarries emptying their outcrop, field yield by fertility. Every method is a no-op (or the flat answer)
/// when the map is flat, so the 2A rules and hashes are untouched there.
/// </summary>
public sealed partial class World
{
    /// <summary>Why <paramref name="def"/> cannot go at <paramref name="origin"/>, or null if it can.</summary>
    public string? PlacementError(BuildingDef def, Cell origin, int rotation)
    {
        if (!Map.CanPlace(def, origin, rotation))
            return Terrain is null ? "local ocupado ou fora do mapa" : "local ocupado, fora do mapa, na água ou em terreno irregular";
        if (Nature is not { } nature) return null;

        var (w, h) = GridMap.Footprint(def, rotation);
        if (def.Harvests == HarvestSource.Outcrop)
        {
            var deposit = nature.DepositAt(origin);
            if (deposit is not { Kind: DepositKind.Outcrop } || deposit.Origin != origin || w != Deposit.Size || h != Deposit.Size)
                return $"{def.Name} precisa ficar exatamente sobre um afloramento de pedra";
            if (deposit.Units <= 0) return "afloramento esgotado";
            if (_buildings.Any(b => b.Origin == origin && b.Def.Harvests == HarvestSource.Outcrop)) return "afloramento já tem pedreira";
            return null;
        }
        for (int y = origin.Y; y < origin.Y + h; y++)
        for (int x = origin.X; x < origin.X + w; x++)
            if (nature.DepositAt(new Cell(x, y)) is not null) return "há uma jazida neste lugar";
        return null;
    }

    public bool CanPlace(BuildingDef def, Cell origin, int rotation) => PlacementError(def, origin, rotation) is null;

    /// <summary>Roads skip water, trees, stones, bushes and deposits (ramps are fine); always true on the flat map.</summary>
    public bool RoadAllowed(Cell c) =>
        Terrain is not { } t || (!t.IsWater(c) && Nature!.At(c).Kind == NodeKind.None && Nature.DepositAt(c) is null);

    // ------------------------------------------------------------ site clearing (D5: trees become site work)

    private long SiteClearingWorkMilli(BuildingDef def, Cell origin, int rotation)
    {
        if (Nature is not { } nature) return 0;
        var bal = Content.Balance;
        long work = 0;
        foreach (var c in FootprintCells(def, origin, rotation))
        {
            var node = nature.At(c);
            work += node.Kind switch
            {
                NodeKind.Tree when Nature.StageOf(node, Tick, bal) != TreeStage.Stump => bal.ClearTreeHours,
                NodeKind.None => 0,
                _ => bal.ClearNodeHours,
            } * (long)Permille.One;
        }
        return work;
    }

    /// <summary>Builders finished clearing: nodes go, mature trees' wood and stones go to the nearest storage.</summary>
    internal void FinishClearing(Building site)
    {
        site.ClearWorkMilli = 0;
        if (Nature is not { } nature) return;
        var bal = Content.Balance;
        int wood = Content.Resource("wood").Index, stone = Content.Resource("stone").Index;
        foreach (var c in FootprintCells(site.Def, site.Origin, site.Rotation))
        {
            var node = nature.At(c);
            if (node.Kind == NodeKind.None) continue;
            if (node.Kind == NodeKind.Tree && Nature.StageOf(node, Tick, bal) == TreeStage.Mature)
                RecordProduced(wood, AddToStorages(wood, Qty.Units(bal.TreeWood), site.Center), economic: true);
            else if (node.Kind == NodeKind.Stone)
                RecordProduced(stone, AddToStorages(stone, Qty.Units(node.Amount), site.Center), economic: true);
            nature.Clear(c);
        }
    }

    private static IEnumerable<Cell> FootprintCells(BuildingDef def, Cell origin, int rotation)
    {
        var (w, h) = GridMap.Footprint(def, rotation);
        for (int y = origin.Y; y < origin.Y + h; y++)
        for (int x = origin.X; x < origin.X + w; x++)
            yield return new Cell(x, y);
    }

    // ------------------------------------------------------------ harvesting

    /// <summary>
    /// Takes from the world at least <paramref name="needMilli"/> for <paramref name="b"/> if it can (fells the next tree
    /// / breaks stone off the outcrop). Returns what the building may produce now (≤ its budget).
    /// </summary>
    internal long HarvestAvailable(Building b, long needMilli)
    {
        if (Nature is not { } nature || b.Def.Harvests == HarvestSource.None) return needMilli;
        while (b.HarvestBudgetMilli < needMilli)
        {
            if (b.Def.Harvests == HarvestSource.Trees)
            {
                if (!FellNextTree(b)) break;
            }
            else
            {
                var deposit = nature.DepositAt(b.Origin);
                if (deposit is null || deposit.Units <= 0) break;
                long take = Math.Min(deposit.Units, 10);
                deposit.Units -= take;
                b.HarvestBudgetMilli += take * Permille.One;
            }
        }
        b.HarvestExhausted = b.HarvestBudgetMilli < needMilli;
        return Math.Min(needMilli, b.HarvestBudgetMilli);
    }

    internal void SpendHarvest(Building b, long milli)
    {
        if (Nature is null || b.Def.Harvests == HarvestSource.None) return;
        b.HarvestBudgetMilli -= milli;
    }

    /// <summary>Fells the mature tree closest (walking) to the building within its radius; it becomes a stump.</summary>
    private bool FellNextTree(Building b)
    {
        var target = NearestMatureTree(b);
        if (target is not { } cell) return false;
        var nature = Nature!;
        var node = nature.At(cell);
        long regrow = Tick + (long)Content.Balance.TreeStumpDays * SimTime.TicksPerDay;
        nature.Set(nature.Index(cell), node with { Tick = regrow });
        b.HarvestBudgetMilli += (long)Content.Balance.TreeWood * Permille.One;
        b.HarvestTarget = nature.Index(cell) + 1;
        Emit(new TreeFelled(Tick, b.Id, cell));
        return true;
    }

    /// <summary>Mature tree within <see cref="BuildingDef.WorkRadius"/> with the shortest walk to the building (ties by cell).</summary>
    public Cell? NearestMatureTree(Building b)
    {
        if (Nature is not { } nature) return null;
        int r = b.Def.WorkRadius;
        var center = b.Center;
        Cell? best = null;
        int bestTicks = int.MaxValue;
        for (int y = Math.Max(0, center.Y - r); y <= Math.Min(Map.Height - 1, center.Y + r); y++)
        for (int x = Math.Max(0, center.X - r); x <= Math.Min(Map.Width - 1, center.X + r); x++)
        {
            var c = new Cell(x, y);
            var node = nature.At(c);
            if (node.Kind != NodeKind.Tree || Nature.StageOf(node, Tick, Content.Balance) != TreeStage.Mature) continue;
            int ticks = Paths.Ticks(c, center);
            if (ticks < bestTicks)
            {
                bestTicks = ticks;
                best = c;
            }
        }
        return best;
    }

    /// <summary>
    /// Share (‰) of the output a harvester keeps after walking to its tree and back: balance.harvestWalkPermillePerCell
    /// per cell between the building and the current tree, capped (1000 = no loss, the flat map and quarries).
    /// </summary>
    public int HarvestEfficiencyPermille(Building b)
    {
        if (Nature is null || b.Def.Harvests != HarvestSource.Trees || b.HarvestTarget == 0) return Permille.One;
        var tree = Map.CellAt(b.HarvestTarget - 1);
        int cells = tree.Manhattan(b.Center);
        var bal = Content.Balance;
        return Permille.One - Math.Min(bal.HarvestWalkMaxPermille, cells * bal.HarvestWalkPermillePerCell);
    }

    /// <summary>For the building panel: mature trees, growing trees (saplings/young/stumps) and mean distance in the radius.</summary>
    public (int Mature, int Growing, int MeanCells) TreesAround(Building b)
    {
        if (Nature is not { } nature) return (0, 0, 0);
        int r = b.Def.WorkRadius, mature = 0, growing = 0;
        long dist = 0;
        var center = b.Center;
        for (int y = Math.Max(0, center.Y - r); y <= Math.Min(Map.Height - 1, center.Y + r); y++)
        for (int x = Math.Max(0, center.X - r); x <= Math.Min(Map.Width - 1, center.X + r); x++)
        {
            var c = new Cell(x, y);
            var node = nature.At(c);
            if (node.Kind != NodeKind.Tree) continue;
            if (Nature.StageOf(node, Tick, Content.Balance) == TreeStage.Mature)
            {
                mature++;
                dist += c.Manhattan(center);
            }
            else growing++;
        }
        return (mature, growing, mature == 0 ? 0 : (int)(dist / mature));
    }

    // ------------------------------------------------------------ fields

    /// <summary>Mean fertility (‰ yield) under the building: 600–1300 on generated maps, 1000 on the flat map.</summary>
    public int FertilityPermille(Building b) => FertilityPermille(b.Def, b.Origin, b.Rotation);

    public int FertilityPermille(BuildingDef def, Cell origin, int rotation)
    {
        if (Terrain is not { } t) return Permille.One;
        long sum = 0;
        int n = 0;
        foreach (var c in FootprintCells(def, origin, rotation))
        {
            if (!t.InBounds(c)) continue;
            sum += t.FertilityPermille(c);
            n++;
        }
        return n == 0 ? Permille.One : (int)(sum / n);
    }
}
