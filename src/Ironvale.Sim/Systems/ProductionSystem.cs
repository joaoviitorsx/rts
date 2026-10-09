namespace Ironvale.Sim.Systems;

/// <summary>
/// Hourly output = Σ(workers × productivity × share of the hour on site) × rate (commute: 2A.3). Continuous recipes fill the building's own stock
/// (production stops when it is full: physical economy); seasonal recipes accumulate work until harvest.
/// </summary>
public sealed class ProductionSystem : ISimSystem
{
    /// <summary>Most output (milli) the input buffer allows this hour.</summary>
    private static long InputLimitMilli(Building b, RecipeDef recipe)
    {
        long cap = long.MaxValue;
        for (int i = 0; i < recipe.InputPerOutput.Length; i++)
        {
            long per = recipe.InputPerOutput[i].Milli;   // milli input per unit (1000 milli) of output
            if (per > 0) cap = Math.Min(cap, b.InputStock.Get(i).Milli * Permille.One / per);
        }
        return cap;
    }

    private static void ConsumeInputs(World w, Building b, RecipeDef recipe, Qty output)
    {
        for (int i = 0; i < recipe.InputPerOutput.Length; i++)
        {
            long per = recipe.InputPerOutput[i].Milli;
            if (per <= 0) continue;
            var used = b.InputStock.RemoveUpTo(i, new Qty(output.Milli * per / Permille.One));
            w.RecordConsumed(i, used, fromStorage: true);
        }
    }

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
                int onSite = w.OnSitePermille(h, cal.HourOfDay);
                w.Telemetry.OnShiftHour(h.Workers, onSite);
                if (onSite == 0) continue;   // walking to or from work
                workerPermille += (long)h.Workers * h.ProductivityPermille * onSite / Permille.One;
                if (recipe.UsesTools) h.ToolHoursToday++;
            }
            if (workerPermille == 0) continue;
            // Generated maps: woodcutters walk to their tree and back (GDD v0.3 §9).
            workerPermille = workerPermille * w.HarvestEfficiencyPermille(b) / Permille.One;

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

                if (recipe.HasInputs)
                {
                    long cap = InputLimitMilli(b, recipe);
                    if (milli > cap) { milli = cap; remainder = 0; }   // short of inputs: make what they allow
                }
                // Generated maps: only what was felled / broken off can be produced (flat map: no harvest source).
                if (w.Nature is not null && b.Def.Harvests != HarvestSource.None)
                {
                    long harvest = w.HarvestAvailable(b, Math.Min(milli, b.Stock.Space.Milli));
                    if (milli > harvest) { milli = harvest; remainder = 0; }
                }
                var added = b.Stock.AddUpTo(r, new Qty(milli));
                b.RemainderMicro[r] = added.Milli == milli ? remainder : 0;
                w.SpendHarvest(b, added.Milli);
                w.RecordProduced(r, added, economic: true);
                if (recipe.HasInputs) ConsumeInputs(w, b, recipe, added);
            }
        }
    }
}
