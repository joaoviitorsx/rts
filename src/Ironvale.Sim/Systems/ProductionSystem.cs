namespace Ironvale.Sim.Systems;

/// <summary>
/// Hourly output = Σ(workers × productivity) × rate. Continuous recipes fill the building's own stock
/// (production stops when it is full: physical economy); seasonal recipes accumulate work until harvest.
/// </summary>
public sealed class ProductionSystem : ISimSystem
{
    public string Name => "production";
    public Phase Phase => Phase.Production;
    public Frequency Frequency => Frequency.Hourly;

    public void Run(World w, in Calendar cal)
    {
        foreach (var b in w.Buildings)
        {
            if (!b.IsProductiveIn(cal.Season)) continue;
            var recipe = b.Recipe!;

            long workerPermille = 0;
            foreach (int id in b.Slots)
            {
                var h = w.GetHousehold(id);
                if (h is null || h.State != HouseholdState.Working) continue;
                workerPermille += (long)h.Workers * h.ProductivityPermille;
                if (recipe.UsesTools) h.ToolHoursToday++;
            }
            if (workerPermille == 0) continue;

            for (int r = 0; r < recipe.OutputPerWorkerHour.Length; r++)
            {
                long rate = recipe.OutputPerWorkerHour[r].Milli;
                if (rate <= 0) continue;
                long micro = rate * workerPermille + b.RemainderMicro[r];
                long milli = micro / Permille.One;
                long remainder = micro % Permille.One;

                if (recipe.Kind == RecipeKind.Seasonal)
                {
                    b.SeasonalWorkMilli += milli;
                    b.RemainderMicro[r] = remainder;
                    continue;
                }

                var added = b.Stock.AddUpTo(r, new Qty(milli));
                b.RemainderMicro[r] = added.Milli == milli ? remainder : 0;
                w.RecordProduced(r, added, economic: true);
            }
        }
    }
}
