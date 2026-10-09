using System.Diagnostics;
using System.Globalization;
using System.Text;
using Ironvale.Sim;
using Ironvale.Sim.Content;
using Ironvale.Sim.Save;
using Ironvale.Sim.Scripting;
using Ironvale.Sim.Time;

// Headless runner: simulates N years without rendering and prints a yearly summary.
//   dotnet run --project src/Ironvale.Sim.Cli -- --years 50 --seed 42 [--player passive|naive|optimal] [--no-roads]
//       [--csv out/] [--save out/run.ivsave] [--no-opening]
//       [--session-log out/sessions/naive_42.csv]   (playtest CSV of the scripted run, for tools/analyze_playtest.py)
//   dotnet run --project src/Ironvale.Sim.Cli -- --balance-report docs/balance_report.md [--years 3]
//       (every scripted player × seeds 42/7/123: survival, crises 1–3, deadlock, commute; GDD v0.2 §4/§6)

var opts = ParseArgs(args);
string dataDir = opts.GetValueOrDefault("data") ?? DataPaths.FindDataDirectory(Directory.GetCurrentDirectory());
string scenarioId = opts.GetValueOrDefault("scenario") ?? "mvp_start";
ulong seed = ulong.Parse(opts.GetValueOrDefault("seed") ?? "42", CultureInfo.InvariantCulture);
int years = int.Parse(opts.GetValueOrDefault("years") ?? "50", CultureInfo.InvariantCulture);

var (content, scenario) = DataPaths.LoadWithScenario(dataDir, scenarioId);
if (opts.GetValueOrDefault("balance-report") is { } reportPath)
{
    // Flat map (the given scenario) and, unless --no-generated, the same opening on generated maps (GDD v0.3).
    var generated = opts.ContainsKey("no-generated") ? null : DataPaths.LoadWithScenario(dataDir, "mvp_generated").Scenario;
    BalanceReport.Write(reportPath, content, scenario, int.Parse(opts.GetValueOrDefault("years") ?? "3", CultureInfo.InvariantCulture), generated);
    return 0;
}
if (opts.GetValueOrDefault("opening-report") is { } openingPath)
{
    // RTS opening on generated maps (default scenario wild_start): timeline to the end of the first winter.
    var (oc, os) = DataPaths.LoadWithScenario(dataDir, opts.GetValueOrDefault("scenario") ?? "wild_start");
    var oseeds = (opts.GetValueOrDefault("seeds") ?? "42,7,123").Split(',').Select(ulong.Parse).ToList();
    var oplayers = (opts.GetValueOrDefault("players") ?? "passive,rts").Split(',').ToList();
    OpeningReport.Write(openingPath, oc, os, oseeds, oplayers);
    return 0;
}
if (opts.GetValueOrDefault("map-png") is { } mapPng)
{
    // Generated map only (no simulation): --map-png out/map.png [--map-size 192] [--fertility]. "{seed}" in the path
    // is replaced, and --seeds 1,2,3 writes one image per seed.
    int size = int.Parse(opts.GetValueOrDefault("map-size") ?? "192", CultureInfo.InvariantCulture);
    var seeds = (opts.GetValueOrDefault("seeds") ?? seed.ToString(CultureInfo.InvariantCulture)).Split(',').Select(ulong.Parse);
    foreach (var s in seeds)
    {
        var timer = Stopwatch.StartNew();
        var g = Ironvale.Sim.Map.WorldGen.Generate(s, size, size, 0, content.Balance);
        timer.Stop();
        string file = mapPng.Replace("{seed}", s.ToString(CultureInfo.InvariantCulture));
        if (!opts.ContainsKey("no-image")) MapImage.Write(file, g, 0, content.Balance, fertility: opts.ContainsKey("fertility"));
        foreach (var problem in Ironvale.Sim.Map.WorldCheck.Problems(g, 0, content.Balance)) Console.WriteLine($"  PROBLEM seed {s}: {problem}");
        var i = g.Terrain.Info;
        Console.WriteLine($"seed {s}: {timer.ElapsedMilliseconds} ms, attempt {i.Attempt}, coast {i.CoastSides}, lakes {i.Lakes}, " +
                          $"levels {i.Levels}, forest {i.ForestPermille}‰, rich {i.RichDeposit}, reach {i.ReachablePermille}‰, " +
                          $"pond {i.StartPond}, retries [{i.Retries}], start {g.Terrain.Start}, trees {g.Nature.Count(Ironvale.Sim.Map.NodeKind.Tree)} -> {file}");
    }
    return 0;
}
var world = World.Create(content, scenario, seed);
IScriptedPlayer? player = opts.ContainsKey("no-opening") ? null
    : ScriptedPlayers.Create(opts.GetValueOrDefault("player") ?? "optimal", roads: !opts.ContainsKey("no-roads"));
SessionRecorder? session = null;
if (opts.GetValueOrDefault("session-log") is { } sessionPath)
{
    // Scripted session in the playtest CSV format; "real" seconds = game time at 1x (1 day = 10 s).
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(sessionPath))!);
    session = new SessionRecorder(new StreamWriter(sessionPath), () => world.ElapsedTicks / (double)SimTime.TicksPerSecondAt1x);
    world.CollectEvents = true;
    world.CommandEnqueued += c => session.Command(world, c);
}
if (opts.ContainsKey("list-buildings")) world.CollectEvents = true;
player?.Start(world);
if (opts.ContainsKey("list-buildings"))
{
    // Debug: what the scripted opening placed (and what was refused) — applying now is what the first step does.
    world.ApplyPendingCommands();
    foreach (var e in world.DrainEvents())
        if (e is Ironvale.Sim.Events.CommandRejected rej) Console.WriteLine($"  rejected at start: {rej}");
    foreach (var b in world.Buildings) Console.WriteLine($"  {b.Def.Id} at {b.Origin} active={b.IsActive}");
}
var watch = new CrisisWatch();

var resources = content.Resources;
Console.WriteLine($"Ironvale sim — scenario={scenarioId} seed={seed} years={years} content={content.Hash}");
var header = new StringBuilder("year  pop ");
foreach (var r in resources) header.Append($"| {r.Id,-8} stock    prod    cons ");
Console.WriteLine(header);

var sw = Stopwatch.StartNew();
var csv = new StringBuilder("year,population," + string.Join(',', resources.SelectMany(r =>
    new[] { $"{r.Id}_stock", $"{r.Id}_produced", $"{r.Id}_consumed" })) + ",deadlocked\n");
long[] lastProduced = new long[resources.Count];
long[] lastConsumed = new long[resources.Count];

for (int y = 1; y <= years; y++)
{
    for (int d = 0; d < SimTime.DaysPerYear; d++)
    {
        world.StepDays(1);
        if (session is not null)
        {
            session.Events(world, world.DrainEvents());
            session.Tick(world);
        }
        player?.Daily(world);
        if (opts.GetValueOrDefault("trace") is { } traceDef && d % 10 == 0)
            foreach (var b in world.Buildings.Where(b => b.Def.Id == traceDef))
                Console.WriteLine($"  day {world.Calendar.TotalDays} {b.Def.Id}#{b.Id} active={b.IsActive} clear={b.ClearWorkMilli} " +
                                  $"work={b.BuildWorkMilli}/{b.RequiredBuildWorkMilli} mat={b.MaterialPermille} workers={b.AssignedCount} " +
                                  $"stock={b.Stock.Total} budget={b.HarvestBudgetMilli} exhausted={b.HarvestExhausted} eff={world.HarvestEfficiencyPermille(b)} " +
                                  $"pop={world.Households.Count}");
        watch.Observe(world);
    }
    var line = new StringBuilder($"{world.Calendar.Year - 1,4}  {world.Households.Count,3} ");
    var row = new StringBuilder($"{y},{world.Households.Count}");
    for (int r = 0; r < resources.Count; r++)
    {
        long produced = world.Ledger.ProducedOf(r).Milli - lastProduced[r];
        long consumed = world.Ledger.ConsumedOf(r).Milli - lastConsumed[r];
        lastProduced[r] = world.Ledger.ProducedOf(r).Milli;
        lastConsumed[r] = world.Ledger.ConsumedOf(r).Milli;
        double stock = world.StorageStock(r).AsDouble;
        line.Append($"| {"",-8}{stock,6:0} {produced / 1000.0,7:0} {consumed / 1000.0,7:0} ");
        row.Append(FormattableString.Invariant($",{stock:0.###},{produced / 1000.0:0.###},{consumed / 1000.0:0.###}"));
    }
    row.Append($",{world.Telemetry.Deadlocked}\n");
    csv.Append(row);
    Console.WriteLine(line);
    if (world.IsCollapsed)
    {
        Console.WriteLine($"!! population reached zero in year {y}");
        break;
    }
}
sw.Stop();
session?.Dispose();

Console.WriteLine();
Console.WriteLine($"ticks: {world.ElapsedTicks:N0} in {sw.Elapsed.TotalSeconds:0.00}s " +
                  $"({world.ElapsedTicks / Math.Max(sw.Elapsed.TotalSeconds, 1e-9):N0} ticks/s)");
Console.WriteLine($"population: {world.Households.Count}  deadlock: {(world.Telemetry.Deadlocked ? $"YES (day {world.Telemetry.DeadlockDay})" : "no")}");
Console.WriteLine($"player: {player?.Id ?? "none"}  departures: {watch.Departures}  " +
                  $"crisis1 firewood: {BalanceReport.When(watch.FirewoodDay)}  crisis2 tools: {BalanceReport.When(watch.ToolsDay)}  " +
                  $"crisis3 winter hunger: {BalanceReport.When(watch.WinterHungerDay)}  commute: {BalanceReport.CommutePct(world):0.0}% of shifts");
Console.WriteLine($"state hash: {SaveSerializer.StateHashHex(world)}");
Console.WriteLine("last policy log:");
foreach (var e in world.PolicyLog.TakeLast(8)) Console.WriteLine($"  [{new Ironvale.Sim.Time.Calendar(e.Tick)}] {e.Text}");

if (opts.GetValueOrDefault("csv") is { } csvDir)
{
    Directory.CreateDirectory(csvDir);
    string path = Path.Combine(csvDir, $"yearly_seed{seed}.csv");
    File.WriteAllText(path, csv.ToString());
    Console.WriteLine($"csv: {path}");
}
if (opts.GetValueOrDefault("save") is { } savePath)
{
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(savePath))!);
    File.WriteAllBytes(savePath, SaveSerializer.Save(world));
    Console.WriteLine($"save: {savePath}");
}
return world.Telemetry.Deadlocked || world.IsCollapsed ? 2 : 0;

static Dictionary<string, string?> ParseArgs(string[] args)
{
    var result = new Dictionary<string, string?>();
    for (int i = 0; i < args.Length; i++)
    {
        if (!args[i].StartsWith("--")) continue;
        string key = args[i][2..];
        string? value = i + 1 < args.Length && !args[i + 1].StartsWith("--") ? args[++i] : null;
        result[key] = value;
    }
    return result;
}
