namespace Ironvale.Sim.Buildings;

public sealed class Building
{
    public int Id { get; internal set; }
    public BuildingDef Def { get; internal set; } = null!;
    public Cell Origin { get; internal set; }
    public int Rotation { get; internal set; }
    public bool IsActive { get; internal set; }
    /// <summary>
    /// Construction work done, in milli household-hours (1 household at 100% for 1 hour = 1000).
    /// While under construction, <see cref="Stock"/> holds the delivered materials (capacity = total cost).
    /// </summary>
    public long BuildWorkMilli { get; internal set; }
    public Stockpile Stock { get; internal set; } = null!;
    /// <summary>Inputs waiting to be used by the recipe (smithy: wood, stone). Empty capacity for other buildings.</summary>
    public Stockpile InputStock { get; internal set; } = null!;

    /// <summary>Where carriers unload when this building is the destination: site materials, recipe inputs or storage.</summary>
    public Stockpile DeliveryStock => IsActive && IsProducer ? InputStock : Stock;

    /// <summary>How much of input <paramref name="r"/> the current recipe wants buffered (capacity split among its inputs).</summary>
    public Qty InputTarget(int r)
    {
        if (!IsActive || Recipe is not { HasInputs: true } recipe || !recipe.InputPerOutput[r].IsPositive) return Qty.Zero;
        int n = recipe.InputPerOutput.Count(q => q.IsPositive);
        return new Qty(Def.InputCapacity.Milli / n);
    }
    /// <summary>Household id per job slot (0 = free).</summary>
    internal int[] Slots { get; set; } = Array.Empty<int>();
    public RecipeDef? Recipe { get; internal set; }
    /// <summary>Seasonal recipes: work accumulated (in milli of output) until the harvest.</summary>
    public long SeasonalWorkMilli { get; internal set; }
    /// <summary>Generated maps: resource already taken from the world and not yet produced (a felled tree's wood,
    /// stone broken off the outcrop), in milli.</summary>
    public long HarvestBudgetMilli { get; internal set; }
    /// <summary>Generated maps: cell index + 1 of the tree being felled (0 = none yet).</summary>
    public int HarvestTarget { get; internal set; }
    /// <summary>Generated maps: the work radius has nothing left to harvest (no mature tree / outcrop exhausted).</summary>
    public bool HarvestExhausted { get; internal set; }
    /// <summary>Generated maps: work (milli household-hours) still needed to clear trees/bushes/stones off the site.</summary>
    public long ClearWorkMilli { get; internal set; }
    /// <summary>Fire (campfire): burning today (it had firewood). Warms colonists and keeps wolves away.</summary>
    public bool Burning { get; internal set; }
    /// <summary>Sub-milli production remainder per resource (micro units), so fractions are not lost.</summary>
    internal long[] RemainderMicro { get; set; } = Array.Empty<long>();

    public IReadOnlyList<int> SlotHouseholds => Slots;

    public (int w, int h) Size => GridMap.Footprint(Def, Rotation);

    public Cell Center
    {
        get
        {
            var (w, h) = Size;
            return new Cell(Origin.X + w / 2, Origin.Y + h / 2);
        }
    }

    public int AssignedCount => Slots.Count(id => id != 0);

    public int FreeSlotIndex() => Array.IndexOf(Slots, 0);

    public long RequiredBuildWorkMilli => Math.Max(1, (long)Def.BuildDays * SimTime.HoursPerDay * Permille.One);

    /// <summary>Whole days of single-household work done (for display).</summary>
    public int BuildProgressDays => (int)(BuildWorkMilli / (SimTime.HoursPerDay * Permille.One));

    /// <summary>Share of the materials on site (min over the cost's resources), 1000 = all delivered.</summary>
    public int MaterialPermille
    {
        get
        {
            long min = Permille.One;
            for (int r = 0; r < Def.Cost.Length; r++)
            {
                long cost = Def.Cost[r].Milli;
                if (cost <= 0) continue;
                min = Math.Min(min, Stock.Get(r).Milli * Permille.One / cost);
            }
            return (int)min;
        }
    }

    /// <summary>Work allowed by the materials on site: building can't run ahead of its deliveries.</summary>
    public long BuildWorkCapMilli => RequiredBuildWorkMilli * MaterialPermille / Permille.One;

    /// <summary>A site where a builder's hour would count (materials allow more work).</summary>
    public bool CanProgress => !IsActive && (ClearWorkMilli > 0 || BuildWorkMilli < BuildWorkCapMilli);

    public bool IsStorage => Def.Has(BuildingRole.Storage);
    public bool IsProducer => Def.Has(BuildingRole.Producer);

    /// <summary>True if this producer can produce in the given season with its current recipe.</summary>
    public bool IsProductiveIn(Season season) =>
        IsActive && IsProducer && Recipe is not null &&
        (Recipe.Kind == RecipeKind.Continuous || Recipe.IsWorkSeason(season));
}
