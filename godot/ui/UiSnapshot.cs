using System;
using System.Collections.Generic;
using System.Linq;
using Ironvale.Sim;
using Ironvale.Sim.Buildings;
using Ironvale.Sim.Content;
using Ironvale.Sim.Population;
using Ironvale.Sim.Time;

namespace Ironvale.Game.UI;

// Read-only data the UI renders (UI_UX_guide §8.1 rule 5: the UI only reads snapshots and sends commands).
// Built by UiSnapshotBuilder from the world a few times per second; holds no references to sim objects.

public sealed record ResourceSnap(string Id, string Name, long Stock, long ProducedDay, long ConsumedDay, double DaysLeft)
{
    public long Net => ProducedDay - ConsumedDay;
}

public sealed record HouseholdSnap(int Id, string Name, int Members, int JobBuildingId, string JobName, string State,
    int ToolPercent, int ProductivityPercent, int FoodDeficitDays, int ColdDeficitDays, bool Homeless,
    int CommutePercent, double FreeHours, double GardenFood);

public sealed record SlotSnap(int HouseholdId, string Name, string Source);

public sealed record RecipeSnap(string Id, string Name);

/// <summary>Construction material: on site / on the way / total needed (whole units).</summary>
public sealed record MaterialSnap(string Name, long OnSite, long Incoming, long Cost);

public sealed record BuildingSnap(int Id, string DefId, string Name, string Status, bool Active, int BuildProgressDays,
    int BuildDays, bool IsStorage, bool IsProducer, bool IsHousing, string? RecipeId, IReadOnlyList<RecipeSnap> Recipes,
    IReadOnlyList<(string Name, long Amount)> Stock, long StockTotal, long Capacity, IReadOnlyList<SlotSnap> Slots,
    IReadOnlyList<string> Residents, int HousingCapacity, long ExpectedHarvest, bool SeasonalRecipe,
    IReadOnlyList<MaterialSnap> Materials, int Builders, int MaxBuilders, string SiteIssue, string SiteIssueArg,
    int CommutePercent, IReadOnlyList<MaterialSnap> Inputs);

/// <summary>State: disabled · recruiting · releasing · in_band · above_max · blocked_* (why it can't act).</summary>
public sealed record PolicySnap(int Id, string ResourceId, string ResourceName, long Min, long Max, bool Enabled,
    long Stock, string State, int DefaultBandPermille);

public enum AlertSeverity { Info, Warning, Critical }

/// <summary>Fixed format (guide §3.4): severity · problem · deadline · action.</summary>
public sealed record AlertSnap(string Key, AlertSeverity Severity, string Text, int FocusBuildingId);

/// <summary>The next goal shown in the objective card (GDD v0.2 §4.1: always a visible next step).</summary>
public sealed record ObjectiveSnap(string Key, string Text, int Index, int Count);

public sealed record DailySnap(long[] Produced, long[] Consumed, long[] Stored, long[] Local, long[] Transit);

public sealed class UiSnapshot
{
    public required long Tick { get; init; }
    public required int Year { get; init; }
    public required Season Season { get; init; }
    public required int Month { get; init; }
    public required int Day { get; init; }
    public required int DaysToWinter { get; init; }
    public required int Speed { get; init; }
    public required int Population { get; init; }
    public required IReadOnlyList<ResourceSnap> Resources { get; init; }
    public required IReadOnlyList<HouseholdSnap> Households { get; init; }
    public required IReadOnlyList<BuildingSnap> Buildings { get; init; }
    public required IReadOnlyList<PolicySnap> Policies { get; init; }
    public required IReadOnlyList<(long Tick, string Text)> PolicyLog { get; init; }
    public required IReadOnlyList<AlertSnap> Alerts { get; init; }
    public required IReadOnlyList<DailySnap> Daily { get; init; }
    public required int Shipments { get; init; }
    public required int FrozenDays { get; init; }
    public required bool Deadlocked { get; init; }
    public required ObjectiveSnap? Objective { get; init; }

    public BuildingSnap? Building(int id) => Buildings.FirstOrDefault(b => b.Id == id);
}

public static class UiSnapshotBuilder
{
    public static UiSnapshot Build(World w, int speed, Func<string, string> tr)
    {
        var cal = w.Calendar;
        var content = w.Content;
        var last = w.Telemetry.LastDay;
        var month = w.Telemetry.Daily.TakeLast(30).ToList();

        var resources = content.Resources.Select(r =>
        {
            long stock = w.StorageStock(r.Index).Milli;
            long prod = last?.Produced[r.Index] ?? 0;
            long cons = last?.Consumed[r.Index] ?? 0;
            double avgNet = month.Count == 0 ? 0 : month.Average(d => (double)(d.Produced[r.Index] - d.Consumed[r.Index]));
            double daysLeft = avgNet < 0 ? stock / -avgNet : double.PositiveInfinity;
            return new ResourceSnap(r.Id, r.Name, stock, prod, cons, daysLeft);
        }).ToList();

        var households = w.Households.Select(h =>
        {
            var job = w.GetBuilding(h.JobBuildingId);
            return new HouseholdSnap(h.Id, h.Name, h.Members, h.JobBuildingId, job is null ? "" : w.DescribeBuilding(job),
                h.State.ToString(), h.ToolCondition / 10, h.ProductivityPermille / 10, h.FoodDeficitDays, h.ColdDeficitDays,
                h.HomeId == 0, w.CommutePermille(h) / 10, w.FreeMilliHours(h) / 1000.0, h.GardenFoodToday.AsDouble);
        }).ToList();

        var buildings = w.Buildings.Select(b => BuildingSnapOf(w, b, cal)).ToList();

        int daysToWinter = cal.IsWinter ? 0
            : (int)((9 - cal.MonthOfYear + 12) % 12) * SimTime.DaysPerMonth - cal.DayOfMonth;

        return new UiSnapshot
        {
            Tick = w.Tick,
            Year = cal.Year,
            Season = cal.Season,
            Month = cal.MonthOfYear + 1,
            Day = cal.DayOfMonth + 1,
            DaysToWinter = daysToWinter,
            Speed = speed,
            Population = w.Households.Count,
            Resources = resources,
            Households = households,
            Buildings = buildings,
            Policies = w.Policies.Select(p => PolicySnapOf(w, p)).ToList(),
            PolicyLog = w.PolicyLog.TakeLast(30).Select(e => (e.Tick, LogText(e, tr))).ToList(),
            Alerts = Alerts(w, resources, daysToWinter, tr),
            Daily = w.Telemetry.Daily.Select(d => new DailySnap(d.Produced, d.Consumed, d.Stored, d.Local, d.Transit)).ToList(),
            Shipments = w.Shipments.Count,
            FrozenDays = w.Telemetry.FrozenDays,
            Deadlocked = w.Telemetry.Deadlocked,
            Objective = NextObjective(w, cal, tr),
        };
    }

    private static PolicySnap PolicySnapOf(World w, Ironvale.Sim.Policies.Policy p)
    {
        var res = w.Content.Resources[p.Resource];
        var stock = w.StorageStockIncludingTransit(p.Resource);
        bool owns = w.Households.Any(h => h.AssignedBy == AssignmentSource.Policy && h.AssignedByPolicyId == p.Id);
        string state = !p.Enabled ? "disabled"
            : stock < p.Min ? (p.BlockedReason.Length > 0 ? p.BlockedReason : "recruiting")
            : stock > p.Max ? (owns ? "releasing" : "above_max")
            : "in_band";
        return new PolicySnap(p.Id, res.Id, res.Name, p.Min.WholeUnits, p.Max.WholeUnits, p.Enabled, stock.WholeUnits,
            state, p.Def.HysteresisPermille);
    }

    /// <summary>Account-book line in the UI language ("log.&lt;key&gt;"), falling back to the sim's Portuguese text.</summary>
    private static string LogText(Ironvale.Sim.Policies.PolicyLogEntry e, Func<string, string> tr)
    {
        string key = "log." + e.Key, template = tr(key);
        if (template == key) return e.Text;
        try { return string.Format(template, e.Args); }
        catch (FormatException) { return e.Text; }
    }

    private static BuildingSnap BuildingSnapOf(World w, Building b, Calendar cal)
    {
        var def = b.Def;
        string status;
        string issueArg0 = "";
        if (!b.IsActive) status = "construction";
        else if (b.IsProducer && b.AssignedCount == 0) status = "no_workers";
        else if (b.IsProducer && !b.IsProductiveIn(cal.Season)) status = "off_season";
        else if (b.Recipe is { HasInputs: true } rec && MissingInput(w, b, rec) is { } missingName)
        {
            status = "no_input";
            issueArg0 = missingName;
        }
        else if (b.Stock.Capacity.IsPositive && b.Stock.Space.Milli <= 0) status = "full";
        else status = b.IsProducer || b.IsStorage ? "working" : "ok";

        var stock = new List<(string, long)>();
        for (int r = 0; r < w.Content.ResourceCount; r++)
            if (b.Stock.Get(r).IsPositive) stock.Add((w.Content.Resources[r].Name, b.Stock.Get(r).WholeUnits));

        var slots = b.SlotHouseholds.Select(id =>
        {
            var h = w.GetHousehold(id);
            return h is null ? new SlotSnap(0, "", "") : new SlotSnap(h.Id, h.Name, h.AssignedBy switch
            {
                AssignmentSource.Player => "player",
                AssignmentSource.Policy => "policy",
                _ => "free",
            });
        }).ToList();

        var materials = new List<MaterialSnap>();
        string issue = "", issueArg = issueArg0;
        if (!b.IsActive) (issue, issueArg) = SiteIssueOf(w, b, materials);
        var inputs = new List<MaterialSnap>();
        if (b.IsActive && b.Recipe is { HasInputs: true })
            for (int r = 0; r < w.Content.ResourceCount; r++)
                if (b.InputTarget(r).IsPositive)
                    inputs.Add(new MaterialSnap(w.Content.Resources[r].Name, b.InputStock.Get(r).WholeUnits,
                        w.SiteIncoming(b, r).WholeUnits, b.InputTarget(r).WholeUnits));

        return new BuildingSnap(b.Id, def.Id, def.Name, status, b.IsActive, b.BuildProgressDays, def.BuildDays,
            b.IsStorage, b.IsProducer, def.Has(BuildingRole.Housing), b.Recipe?.Id,
            def.Recipes.Select(r => new RecipeSnap(r.Id, r.Name)).ToList(), stock, b.Stock.Total.WholeUnits,
            b.Stock.Capacity.WholeUnits, slots,
            w.Households.Where(h => h.HomeId == b.Id).Select(h => h.Name).ToList(), def.HousingCapacity,
            b.SeasonalWorkMilli / 1000, b.Recipe?.Kind == RecipeKind.Seasonal,
            materials, b.IsActive ? 0 : w.BuildersAt(b), w.Content.Balance.MaxBuildersPerSite, issue, issueArg,
            CommutePercentAt(w, b), inputs);
    }

    /// <summary>First input the recipe can't make one unit from (null when it can work).</summary>
    private static string? MissingInput(World w, Building b, RecipeDef recipe)
    {
        for (int r = 0; r < recipe.InputPerOutput.Length; r++)
            if (recipe.InputPerOutput[r].IsPositive && b.InputStock.Get(r) < recipe.InputPerOutput[r])
                return w.Content.Resources[r].Name;
        return null;
    }

    /// <summary>Average share of the shift the people working here spend walking (0 when nobody works here).</summary>
    private static int CommutePercentAt(World w, Building b)
    {
        var workers = w.Households.Where(h => w.WorkplaceOf(h) == b).ToList();
        return workers.Count == 0 ? 0 : (int)workers.Average(h => w.CommutePermille(h)) / 10;
    }

    /// <summary>
    /// Why a site is stopped, most actionable first: a material missing from storage → nobody hauling →
    /// nobody building → only waiting for deliveries already on the way. Fills <paramref name="materials"/>.
    /// </summary>
    private static (string Issue, string Arg) SiteIssueOf(World w, Building b, List<MaterialSnap> materials)
    {
        string missing = "";
        bool needsHauling = false;
        for (int r = 0; r < w.Content.ResourceCount; r++)
        {
            var cost = b.Def.Cost[r];
            if (!cost.IsPositive) continue;
            var res = w.Content.Resources[r];
            materials.Add(new MaterialSnap(res.Name, b.Stock.Get(r).WholeUnits, w.SiteIncoming(b, r).WholeUnits, cost.WholeUnits));
            var need = w.SiteNeed(b, r);
            if (!need.IsPositive) continue;
            if (w.StorageFree(r) < need && missing.Length == 0) missing = res.Name;
            needsHauling = true;
        }
        if (missing.Length > 0) return ("no_material", missing);
        if (needsHauling && !w.Carriers.Any(c => !c.Retiring)) return ("no_carriers", "");
        if (b.CanProgress && w.BuildersAt(b) == 0) return ("no_builders", "");
        if (!b.CanProgress) return ("waiting", "");
        return ("", "");
    }

    /// <summary>
    /// First unmet goal of the opening (presentation-level, derived from the world like the alerts): carriers →
    /// houses → field → woodcutter → firewood for winter → a decree → granary → smithy + quarry → first winter.
    /// </summary>
    private static ObjectiveSnap? NextObjective(World w, Calendar cal, Func<string, string> tr)
    {
        int Active(string def) => w.Buildings.Count(b => b.IsActive && b.Def.Id == def);
        int Workers(string def) => w.Buildings.Where(b => b.IsActive && b.Def.Id == def).Sum(b => b.AssignedCount);
        int carriers = w.Carriers.Count(c => !c.Retiring);
        var firewood = w.StorageStock(w.Content.Resource("firewood").Index).WholeUnits;
        long winterNeed = (long)(w.Content.Balance.FirewoodPerHouseholdPerWinterDay.AsDouble * w.Households.Count * 90);
        var steps = new (string Key, bool Done, object[] Args)[]
        {
            ("carriers", carriers >= 2, new object[] { carriers }),
            ("houses", Active("house") >= 3, new object[] { Active("house") }),
            ("field", Workers("field") >= 2, new object[] { Workers("field") }),
            ("woodcutter", Workers("woodcutter") >= 1, Array.Empty<object>()),
            ("firewood", firewood >= winterNeed || cal.Year > 1, new object[] { firewood, winterNeed }),
            ("decree", w.Policies.Count > 0, Array.Empty<object>()),
            ("granary", Active("granary") >= 1, Array.Empty<object>()),
            ("tools", Active("smithy") >= 1 && Active("quarry") >= 1, Array.Empty<object>()),
            ("winter", cal.Year > 1, Array.Empty<object>()),
        };
        for (int i = 0; i < steps.Length; i++)
        {
            if (steps[i].Done) continue;
            return new ObjectiveSnap(steps[i].Key, string.Format(tr("objective." + steps[i].Key), steps[i].Args), i + 1, steps.Length);
        }
        return new ObjectiveSnap("grow", tr("objective.grow"), steps.Length, steps.Length);
    }

    /// <summary>Presentation-level warnings derived from the snapshot numbers (no game rules live here).</summary>
    private static List<AlertSnap> Alerts(World w, List<ResourceSnap> res, int daysToWinter, Func<string, string> tr)
    {
        var list = new List<AlertSnap>();
        var food = res.First(r => r.Id == "food");
        var firewood = res.First(r => r.Id == "firewood");
        if (food.DaysLeft < 10)
            list.Add(new AlertSnap("food", AlertSeverity.Critical, string.Format(tr("alert.food_out"), Math.Floor(food.DaysLeft)), 0));
        else if (food.DaysLeft < 30)
            list.Add(new AlertSnap("food", AlertSeverity.Warning, string.Format(tr("alert.food_low"), Math.Floor(food.DaysLeft)), 0));
        if (daysToWinter is > 0 and <= 30 && firewood.Stock / 1000.0 < w.Households.Count * 90 * 0.6)
            list.Add(new AlertSnap("firewood", AlertSeverity.Warning,
                string.Format(tr("alert.firewood_winter"), firewood.Stock / 1000, daysToWinter), 0));
        foreach (var b in w.Buildings.Where(b => !b.IsActive))
        {
            var (issue, arg) = SiteIssueOf(w, b, new List<MaterialSnap>());
            if (issue is "no_carriers" or "no_material")
            {
                list.Add(new AlertSnap($"site_{issue}", AlertSeverity.Warning, string.Format(tr("alert.site_" + issue), b.Def.Name, arg), b.Id));
                break;
            }
        }
        if (w.Telemetry.Deadlocked)
            list.Add(new AlertSnap("deadlock", AlertSeverity.Critical, tr("alert.deadlock"), 0));
        foreach (var h in w.Households.Where(h => h.FoodDeficitDays > 5).Take(1))
            list.Add(new AlertSnap($"hunger{h.Id}", AlertSeverity.Warning, string.Format(tr("alert.hunger"), h.Name, h.FoodDeficitDays), 0));
        return list;
    }
}
