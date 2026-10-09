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
            // RTS opening: colonists ordered to build count as a share of a household each (GDD v0.3 §4.2).
            foreach (var u in w.Units)
                if (u.Order is { Kind: OrderKind.Build } o && o.TargetId == b.Id && u.Step == UnitStep.Working
                    && w.Map.BuildingAt(u.Pos) == b.Id)
                    work += w.Content.Balance.ColonistBuildPermille;
            if (work == 0) continue;
            if (b.ClearWorkMilli > 0)
            {
                // Generated maps: fell the trees and lift the stones under the site first (GDD v0.3 D5).
                long clear = Math.Min(work, b.ClearWorkMilli);
                b.ClearWorkMilli -= clear;
                work -= clear;
                if (b.ClearWorkMilli == 0) w.FinishClearing(b);
                if (work == 0) continue;
            }
            b.BuildWorkMilli = Math.Min(b.BuildWorkMilli + work, b.BuildWorkCapMilli);
            if (b.BuildWorkMilli >= b.RequiredBuildWorkMilli) w.CompleteConstruction(b);
        }
    }
}
