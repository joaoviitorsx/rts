namespace Ironvale.Sim.Systems;

/// <summary>
/// Hourly: each builder adds its productivity as work to its site, but a site can't run ahead of the materials
/// carried to it (work cap = required × share of materials on site). When the work is done the materials are
/// consumed and the building becomes active (Marco 2A.1).
/// </summary>
public sealed class ConstructionSystem : ISimSystem
{
    public string Name => "construction";
    public Phase Phase => Phase.Production;
    public Frequency Frequency => Frequency.Hourly;

    public void Run(World w, in Calendar cal)
    {
        foreach (var b in w.Buildings.ToArray())
        {
            if (!b.CanProgress) continue;
            long work = 0;
            foreach (var h in w.Households)
            {
                if (h.State != HouseholdState.Building || h.BuildSiteId != b.Id) continue;
                int onSite = w.OnSitePermille(h, cal.HourOfDay);
                if (onSite == 0) continue;   // walking to or from the site
                work += (long)h.ProductivityPermille * onSite / Permille.One;
                h.ToolHoursToday++;
            }
            if (work == 0) continue;
            b.BuildWorkMilli = Math.Min(b.BuildWorkMilli + work, b.BuildWorkCapMilli);
            if (b.BuildWorkMilli >= b.RequiredBuildWorkMilli) w.CompleteConstruction(b);
        }
    }
}
