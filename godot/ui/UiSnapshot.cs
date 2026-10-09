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
    int ToolPercent, int ProductivityPercent, int FoodDeficitDays, int ColdDeficitDays, bool Homeless);

public sealed record SlotSnap(int HouseholdId, string Name, string Source);

public sealed record RecipeSnap(string Id, string Name);

public sealed record BuildingSnap(int Id, string DefId, string Name, string Status, bool Active, int BuildProgressDays,
    int BuildDays, bool IsStorage, bool IsProducer, bool IsHousing, string? RecipeId, IReadOnlyList<RecipeSnap> Recipes,
    IReadOnlyList<(string Name, long Amount)> Stock, long StockTotal, long Capacity, IReadOnlyList<SlotSnap> Slots,
    IReadOnlyList<string> Residents, int HousingCapacity, long ExpectedHarvest, bool SeasonalRecipe);

public sealed record PolicySnap(int Id, string ResourceId, string ResourceName, long Threshold, bool Enabled);

public enum AlertSeverity { Info, Warning, Critical }

/// <summary>Fixed format (guide §3.4): severity · problem · deadline · action.</summary>
public sealed record AlertSnap(string Key, AlertSeverity Severity, string Text, int FocusBuildingId);

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
                h.HomeId == 0);
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
            Policies = w.Policies.Select(p => new PolicySnap(p.Id, content.Resources[p.Resource].Id,
                content.Resources[p.Resource].Name, p.Threshold.WholeUnits, p.Enabled)).ToList(),
            PolicyLog = w.PolicyLog.TakeLast(30).Select(e => (e.Tick, e.Text)).ToList(),
            Alerts = Alerts(w, resources, daysToWinter, tr),
            Daily = w.Telemetry.Daily.Select(d => new DailySnap(d.Produced, d.Consumed, d.Stored, d.Local, d.Transit)).ToList(),
            Shipments = w.Shipments.Count,
            FrozenDays = w.Telemetry.FrozenDays,
            Deadlocked = w.Telemetry.Deadlocked,
        };
    }

    private static BuildingSnap BuildingSnapOf(World w, Building b, Calendar cal)
    {
        var def = b.Def;
        string status;
        if (!b.IsActive) status = "construction";
        else if (b.IsProducer && b.AssignedCount == 0) status = "no_workers";
        else if (b.IsProducer && !b.IsProductiveIn(cal.Season)) status = "off_season";
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

        return new BuildingSnap(b.Id, def.Id, def.Name, status, b.IsActive, b.BuildProgressDays, def.BuildDays,
            b.IsStorage, b.IsProducer, def.Has(BuildingRole.Housing), b.Recipe?.Id,
            def.Recipes.Select(r => new RecipeSnap(r.Id, r.Name)).ToList(), stock, b.Stock.Total.WholeUnits,
            b.Stock.Capacity.WholeUnits, slots,
            w.Households.Where(h => h.HomeId == b.Id).Select(h => h.Name).ToList(), def.HousingCapacity,
            b.SeasonalWorkMilli / 1000, b.Recipe?.Kind == RecipeKind.Seasonal);
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
        if (w.Telemetry.Deadlocked)
            list.Add(new AlertSnap("deadlock", AlertSeverity.Critical, tr("alert.deadlock"), 0));
        foreach (var h in w.Households.Where(h => h.FoodDeficitDays > 5).Take(1))
            list.Add(new AlertSnap($"hunger{h.Id}", AlertSeverity.Warning, string.Format(tr("alert.hunger"), h.Name, h.FoodDeficitDays), 0));
        return list;
    }
}
