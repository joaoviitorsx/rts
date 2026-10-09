namespace Ironvale.Sim.Systems;

/// <summary>
/// Daily weather of a generated map (GDD v0.3 §5): rain by season (snow in winter), known a day ahead, and the first
/// rain guaranteed by balance.firstRainDay so the opening's first goal — get the supplies under a roof — always comes
/// from the world. On rainy days uncovered storages (the ground pile) lose food and firewood. Flat map: never runs.
/// </summary>
public sealed class WeatherSystem : ISimSystem
{
    public string Name => "weather";
    public Phase Phase => Phase.Production;
    public Frequency Frequency => Frequency.Daily;

    public void Run(World w, in Calendar cal)
    {
        if (w.Terrain is null) return;
        w.WeatherToday = w.WeatherTomorrow;
        if (w.WeatherToday is Weather.Rain or Weather.Snow) w.RainSeen = true;
        w.WeatherTomorrow = Roll(w, cal.TotalDays + 1);
        if (w.WeatherToday == Weather.Rain) Spoil(w);
    }

    /// <summary>Weather of absolute day <paramref name="day"/> (called once per day, in order: deterministic).</summary>
    internal static Weather Roll(World w, long day)
    {
        var bal = w.Content.Balance;
        var season = new Calendar(day * SimTime.TicksPerDay).Season;
        long fromStart = day - w.StartTick / SimTime.TicksPerDay;
        if (!w.RainSeen && fromStart >= bal.FirstRainDay && season != Season.Winter) return Weather.Rain;
        var rng = w.Rng.Get(RngStreams.Weather);
        int roll = rng.NextInt(Permille.One);
        if (roll < bal.RainPermille[(int)season]) return season == Season.Winter ? Weather.Snow : Weather.Rain;
        return roll < bal.RainPermille[(int)season] + 250 ? Weather.Cloudy : Weather.Clear;
    }

    private static void Spoil(World w)
    {
        int permille = w.Content.Balance.OpenPileSpoilPermille;
        if (permille <= 0) return;
        var spoils = new[] { w.Content.Resource("food").Index, w.Content.Resource("firewood").Index };
        foreach (var b in w.Buildings)
        {
            if (!b.IsActive || !b.IsStorage || !b.Def.Uncovered) continue;
            foreach (int r in spoils)
            {
                var loss = b.Stock.RemoveUpTo(r, b.Stock.Free(r).MulPermille(permille));
                if (!loss.IsPositive) continue;
                w.RecordConsumed(r, loss, fromStorage: true);
                w.Emit(new Spoiled(w.Tick, b.Id, r, loss));
            }
        }
    }
}
