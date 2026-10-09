namespace Ironvale.Sim.Systems;

/// <summary>At each season start, fields convert the work of the season that just ended into crops.</summary>
public sealed class HarvestSystem : ISimSystem
{
    public string Name => "harvest";
    public Phase Phase => Phase.Production;
    public Frequency Frequency => Frequency.Seasonal;

    public void Run(World w, in Calendar cal)
    {
        int variance = w.Content.Balance.HarvestVariancePermille;
        foreach (var b in w.Buildings)
        {
            var recipe = b.Recipe;
            if (!b.IsActive || recipe is not { Kind: RecipeKind.Seasonal }) continue;
            if (!recipe.IsWorkSeason(cal.PreviousSeason) || b.SeasonalWorkMilli <= 0) continue;

            int delta = variance > 0 ? w.Rng.Get(RngStreams.Harvest).Range(-variance, variance) : 0;
            var amount = new Qty(b.SeasonalWorkMilli * (Permille.One + delta) / Permille.One);
            if (w.Terrain is not null) amount = amount.MulPermille(w.FertilityPermille(b));   // GDD v0.3 §9
            int r = Array.FindIndex(recipe.OutputPerWorkerHour, q => q.IsPositive);
            var added = b.Stock.AddUpTo(r, amount);
            w.RecordProduced(r, added, economic: true);
            b.SeasonalWorkMilli = 0;

            if (added < amount)
                w.Emit(new SimAlert(cal.Tick, $"{w.DescribeBuilding(b)}: colheita perdida ({amount - added}), estoque cheio"));
        }
    }
}
