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
    /// <summary>Household id per job slot (0 = free).</summary>
    internal int[] Slots { get; set; } = Array.Empty<int>();
    public RecipeDef? Recipe { get; internal set; }
    /// <summary>Seasonal recipes: work accumulated (in milli of output) until the harvest.</summary>
    public long SeasonalWorkMilli { get; internal set; }
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
    public bool CanProgress => !IsActive && BuildWorkMilli < BuildWorkCapMilli;

    public bool IsStorage => Def.Has(BuildingRole.Storage);
    public bool IsProducer => Def.Has(BuildingRole.Producer);

    /// <summary>True if this producer can produce in the given season with its current recipe.</summary>
    public bool IsProductiveIn(Season season) =>
        IsActive && IsProducer && Recipe is not null &&
        (Recipe.Kind == RecipeKind.Continuous || Recipe.IsWorkSeason(season));
}
