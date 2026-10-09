using System.Globalization;
using System.Text;
using Ironvale.Sim;
using Ironvale.Sim.Content;
using Ironvale.Sim.Scripting;
using Ironvale.Sim.Time;

/// <summary>
/// Runs every scripted player (passive, naive, optimal, optimal without roads) over a few seeds and writes
/// docs/balance_report.md: survival, departures, when crises 1–3 appear (GDD v0.2 §4/§6), deadlock, commute.
/// </summary>
internal static class BalanceReport
{
    private static readonly ulong[] Seeds = { 42, 7, 123 };

    public static string When(long? day) => day is { } d
        ? string.Create(CultureInfo.InvariantCulture, $"day {d} (~{CrisisWatch.Minutes(d):0} min)")
        : "—";

    public static double CommutePct(World w) => w.Telemetry.ShiftHoursPermille == 0 ? 0
        : 100.0 * w.Telemetry.CommuteHoursPermille / w.Telemetry.ShiftHoursPermille;

    private sealed record Row(string Player, ulong Seed, int Pop, int MinPop, int Departures, long? FirstDeparture,
        long? Crisis1, long? Crisis2, long? Crisis3, bool Deadlock, double Commute, double FoodEnd, double FirewoodEnd,
        long StartTick, long QuietDays);

    public static void Write(string path, ContentDb content, ScenarioDef scenario, int years)
    {
        var rows = new List<Row>();
        var players = new List<(string Label, Func<IScriptedPlayer> Make)>
        {
            ("passive", () => ScriptedPlayers.Create("passive")),
            ("naive", () => ScriptedPlayers.Create("naive")),
            ("optimal", () => ScriptedPlayers.Create("optimal", roads: true)),
            ("optimal_no_roads", () => ScriptedPlayers.Create("optimal", roads: false)),
        };
        foreach (var (label, make) in players)
        foreach (var seed in Seeds)
        {
            var w = World.Create(content, scenario, seed);
            var player = make();
            player.Start(w);
            var watch = new CrisisWatch();
            for (int d = 0; d < years * SimTime.DaysPerYear && !w.IsCollapsed; d++)
            {
                w.StepDays(1);
                player.Daily(w);
                watch.Observe(w);
            }
            rows.Add(new Row(label, seed, w.Households.Count, watch.MinPopulation == int.MaxValue ? 0 : watch.MinPopulation,
                watch.Departures, watch.FirstDepartureDay, watch.FirewoodDay, watch.ToolsDay, watch.WinterHungerDay,
                w.Telemetry.Deadlocked, CommutePct(w), w.StorageStock(content.Resource("food").Index).AsDouble,
                w.StorageStock(content.Resource("firewood").Index).AsDouble, w.StartTick, watch.LongestQuietDays));
            Console.WriteLine($"{label,-17} seed {seed,3}: pop {w.Households.Count} departures {watch.Departures} commute {CommutePct(w):0.0}%");
        }

        string At(long? day, long startTick)
        {
            if (day is not { } d) return "—";
            var cal = new Ironvale.Sim.Time.Calendar(startTick + d * SimTime.TicksPerDay);
            string season = cal.Season switch
            {
                Season.Spring => "primavera", Season.Summer => "verão", Season.Autumn => "outono", _ => "inverno",
            };
            return string.Create(CultureInfo.InvariantCulture, $"ano {cal.Year} {season} (~{CrisisWatch.Minutes(d):0} min)");
        }

        var md = new StringBuilder();
        md.AppendLine("# Relatório de balanceamento (gerado pela CLI)");
        md.AppendLine();
        md.AppendLine($"> `dotnet run --project src/Ironvale.Sim.Cli -- --balance-report docs/balance_report.md --years {years}`");
        md.AppendLine($"> Conteúdo `{content.Hash}` · cenário `{scenario.Id}` · {years} anos · seeds {string.Join(", ", Seeds)}.");
        md.AppendLine("> Minutos = tempo de jogo a 1x (1 dia = 4 s). Crises (GDD v0.2 §6): **1** lenha abaixo da demanda do inverno no");
        md.AppendLine("> outono · **2** sem ferramentas de reserva e condição média < 50% · **3** família com fome no inverno.");
        md.AppendLine("> Trajeto = % das horas de turno dos produtores gastas andando (ida e volta).");
        md.AppendLine();
        md.AppendLine("## Por cenário (média das seeds; crises = primeira ocorrência entre as seeds)");
        md.AppendLine();
        md.AppendLine("| Jogador | Sobreviveu (seeds) | Famílias no fim (mín.) | Partidas | Crise 1 lenha | Crise 2 ferramentas | Crise 3 fome no inverno | Deadlock | Trajeto médio | Maior trecho sem acontecimento |");
        md.AppendLine("|---|---|---|---|---|---|---|---|---|---|");
        foreach (var g in rows.GroupBy(r => r.Player))
        {
            var list = g.ToList();
            string First(Func<Row, long?> f)
            {
                var hit = list.Where(r => f(r) is not null).OrderBy(r => f(r)).FirstOrDefault();
                return hit is null ? "—" : $"{At(f(hit), hit.StartTick)} ({list.Count(r => f(r) is not null)}/{list.Count})";
            }
            md.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {g.Key} | {list.Count(r => r.Pop > 0)}/{list.Count} | {list.Average(r => r.Pop):0.#} ({list.Min(r => r.MinPop)}) | " +
                $"{list.Average(r => r.Departures):0.#} | {First(r => r.Crisis1)} | {First(r => r.Crisis2)} | {First(r => r.Crisis3)} | " +
                $"{(list.Any(r => r.Deadlock) ? "sim" : "não")} | {list.Average(r => r.Commute):0.0}% | " +
                $"{list.Max(r => r.QuietDays)} dias (~{CrisisWatch.Minutes(list.Max(r => r.QuietDays)):0.#} min) |"));
        }
        md.AppendLine();
        md.AppendLine("## Por seed");
        md.AppendLine();
        md.AppendLine("| Jogador | Seed | Famílias | Partidas (1ª) | Crise 1 | Crise 2 | Crise 3 | Comida no fim | Lenha no fim | Trajeto |");
        md.AppendLine("|---|---|---|---|---|---|---|---|---|---|");
        foreach (var r in rows)
            md.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {r.Player} | {r.Seed} | {r.Pop} | {r.Departures} ({At(r.FirstDeparture, r.StartTick)}) | {At(r.Crisis1, r.StartTick)} | " +
                $"{At(r.Crisis2, r.StartTick)} | {At(r.Crisis3, r.StartTick)} | {r.FoodEnd:0} | {r.FirewoodEnd:0} | {r.Commute:0.0}% |"));
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, md.ToString());
        Console.WriteLine($"report: {path}");
    }
}
