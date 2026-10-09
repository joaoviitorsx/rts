using System.Globalization;
using System.Text;
using Ironvale.Sim;
using Ironvale.Sim.Content;
using Ironvale.Sim.Events;
using Ironvale.Sim.Scripting;
using Ironvale.Sim.Time;

/// <summary>
/// RTS opening (GDD v0.3 §3/§14, briefing step 3 gate): each scripted player × seed played from minute 0 to the end of
/// the first winter, as a timeline in minutes at 1x (1 day = 10 s): first of each building, families formed/arrived,
/// first delegation, departures; then the band at the start and end of winter. Markdown to the given path.
///   dotnet run --project src/Ironvale.Sim.Cli -- --opening-report docs/reports/opening_report.md [--seeds 42,7,123]
/// </summary>
internal static class OpeningReport
{
    private const int CallsPerDay = 4;   // a player who looks at the band every ~2.5 s of play

    public static void Write(string path, ContentDb content, ScenarioDef scenario, IReadOnlyList<ulong> seeds, IReadOnlyList<string> players)
    {
        var md = new StringBuilder();
        md.AppendLine($"# Abertura RTS — relatório ({scenario.Id})");
        md.AppendLine();
        md.AppendLine("Minutos de jogo em 1x (1 dia = 10 s). Do minuto 0 ao fim do 1º inverno. Gerado por `--opening-report`.");
        foreach (var id in players)
        foreach (var seed in seeds)
        {
            var w = World.Create(content, scenario, seed);
            w.CollectEvents = true;
            var player = ScriptedPlayers.Create(id);
            player.Start(w);
            var timeline = new List<(double Min, string What)>();
            var firstBuilt = new HashSet<string>();
            bool delegated = false;
            string? winterStart = null;
            long winterEndTick = WinterEndTick(w);
            int food = content.Resource("food").Index, firewood = content.Resource("firewood").Index, wood = content.Resource("wood").Index;
            double Min() => w.ElapsedTicks / (double)SimTime.TicksPerSecondAt1x / 60.0;
            string Band() => $"{w.Units.Count(u => u.IsColonist && u.Controllable)} colonos, {w.Households.Count} famílias " +
                             $"({w.Households.Sum(h => h.Members)} pessoas), comida {w.StorageStock(food).WholeUnits}, " +
                             $"lenha {w.StorageStock(firewood).WholeUnits}, madeira {w.StorageStock(wood).WholeUnits}";
            while (w.Tick < winterEndTick)
            {
                w.StepTicks(SimTime.TicksPerDay / CallsPerDay);
                foreach (var e in w.DrainEvents())
                {
                    switch (e)
                    {
                        case BuildingCompleted c when w.GetBuilding(c.BuildingId) is { } b && firstBuilt.Add(b.Def.Id):
                            timeline.Add((Min(), $"1ª {b.Def.Name}"));
                            break;
                        case FamilyFormed f:
                            timeline.Add((Min(), $"família formada: {f.Name}"));
                            break;
                        case FamilyArrived a:
                            timeline.Add((Min(), $"família chegou: {a.Name} ({a.Members})"));
                            break;
                        case HouseholdAssigned ha when !delegated && w.GetBuilding(ha.BuildingId) is { Def.Harvests: not HarvestSource.None } hut:
                            delegated = true;
                            timeline.Add((Min(), $"**1ª delegação** ({hut.Def.Name})"));
                            break;
                        case UnitLeft l:
                            timeline.Add((Min(), $"colono saiu: {l.Name} ({l.Reason})"));
                            break;
                        case HouseholdLeft l:
                            timeline.Add((Min(), $"família saiu: {l.Name} ({l.Reason})"));
                            break;
                    }
                }
                if (winterStart is null && w.Calendar.IsWinter)
                {
                    winterStart = Band();
                    timeline.Add((Min(), "**início do inverno**"));
                }
                if (Environment.GetEnvironmentVariable("OPENING_TRACE") == "1" && w.Tick % (5 * SimTime.TicksPerDay) == 0 && w.Tick < 140 * SimTime.TicksPerDay)
                    Console.WriteLine($"d{w.Tick / SimTime.TicksPerDay} {Band()} sites[{string.Join(",", w.Buildings.Where(b => !b.IsActive).Select(b => $"{b.Def.Id}:{b.MaterialPermille}‰ clr{b.ClearWorkMilli}"))}] " +
                        $"orders[{string.Join(",", w.Units.GroupBy(u => u.Order?.Kind.ToString() ?? "none").Select(g => $"{g.Key}:{g.Count()}"))}] " +
                        $"confused {w.Units.Count(u => u.Confused)} deer {w.Animals.Count(a => a.Kind == Ironvale.Sim.Map.FaunaKind.Deer)} " +
                        $"food+{w.Ledger.ProducedOf(food).WholeUnits} -{w.Ledger.ConsumedOf(food).WholeUnits} ground {w.GroundItems.Where(g => g.Resource == food).Sum(g => g.Amount.WholeUnits)} " +
                        $"huts[{string.Join(",", w.Buildings.Where(b => b.IsActive && b.IsProducer).Select(b => $"{b.Def.Id}:{b.AssignedCount}w food{b.Stock.Get(food).WholeUnits} wood{b.Stock.Get(wood).WholeUnits} exh{(b.HarvestExhausted ? 1 : 0)}"))}] " +
                        $"hh[{string.Join(",", w.Households.Select(h => $"{h.State}:{(h.HasJob ? w.GetBuilding(h.JobBuildingId)!.Def.Id : "-")}"))}]");
                if (Environment.GetEnvironmentVariable("OPENING_TRACE") == "2" && w.Tick % (5 * SimTime.TicksPerDay) == 0 && w.Buildings.FirstOrDefault(b => !b.IsActive) is { } st)
                    Console.WriteLine($"d{w.Tick / SimTime.TicksPerDay} site {st.Def.Id}@{st.Origin} need[{string.Join(",", Enumerable.Range(0, content.ResourceCount).Where(r => st.Def.Cost[r].IsPositive).Select(r => $"{content.Resources[r].Id}:{st.Stock.Get(r)}/{st.Def.Cost[r]} store {w.StorageStock(r)}"))}] " +
                        string.Join(" ", w.Units.Where(u => u.Order is { Kind: Ironvale.Sim.Population.OrderKind.Build }).Select(u => $"[{u.Name} {u.Step} {u.Pos} carry {u.CarryResource}:{u.CarryAmount} conf {u.Confused} helper {u.HelperId}]")));
                if (w.Tick % SimTime.TicksPerDay < SimTime.TicksPerDay / CallsPerDay) player.Daily(w);
                else if (player is RtsPlayer) player.Daily(w);
            }
            md.AppendLine();
            md.AppendLine($"## {id} · seed {seed}");
            md.AppendLine();
            foreach (var (min, what) in timeline) md.AppendLine($"- {min.ToString("0.0", CultureInfo.InvariantCulture)} min — {what}");
            md.AppendLine();
            md.AppendLine($"- No início do inverno: {winterStart ?? "—"}");
            md.AppendLine($"- No fim do inverno: {Band()}");
            md.AppendLine($"- Estado: `{Ironvale.Sim.Save.SaveSerializer.StateHashHex(w)}`");
            Console.WriteLine($"{id} seed {seed}: delegation {(delegated ? "yes" : "no")}, end: {Band()}");
        }
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, md.ToString());
        Console.WriteLine($"opening report -> {path}");
    }

    /// <summary>Tick at which the first winter ends (start of the following spring).</summary>
    private static long WinterEndTick(World w)
    {
        long t = w.Tick - w.Tick % SimTime.TicksPerYear + SimTime.TicksPerYear;
        return t;
    }
}
