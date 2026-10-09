using System.Globalization;

namespace Ironvale.Sim.Scripting;

/// <summary>
/// Playtest session log (one CSV per session): <c>real_s,game_day,kind,detail</c>. Shared by the game (real seconds) and
/// the CLI (scripted players, 1x-pace seconds) so tools/analyze_playtest.py reads both the same way. Observes only.
/// <list type="bullet">
/// <item>v1 (playtest-2A candidate build): <c>command</c> rows and <c>event</c> rows.</item>
/// <item>v2: also a first <c>meta</c> row (<c>format=2</c>), one <c>state</c> row per game day and a <c>crisis</c> row the
/// first time each crisis of GDD v0.2 §6 appears (<see cref="CrisisWatch"/>).</item>
/// </list>
/// </summary>
public sealed class SessionRecorder(TextWriter output, Func<double> realSeconds) : IDisposable
{
    public const string Header = "real_s,game_day,kind,detail";
    public const int FormatVersion = 2;

    private readonly CrisisWatch _watch = new();
    private bool _started;
    private long _lastDay = -1;
    private bool _firewood, _tools, _hunger, _suggestion;

    private void Start(World w)
    {
        if (_started) return;
        _started = true;
        output.WriteLine(Header);
        Write(w, "meta", $"format={FormatVersion} scenario={w.ScenarioId} seed={w.Seed}");
    }

    public void Command(World w, SimCommand c)
    {
        Start(w);
        Write(w, "command", c.ToString());
    }

    public void Events(World w, IEnumerable<SimEvent> events)
    {
        Start(w);
        foreach (var e in events)
            if (Describe(e) is { } detail) Write(w, "event", detail);
    }

    /// <summary>Call every frame/step: writes the daily state and crisis rows when the game day changes.</summary>
    public void Tick(World w)
    {
        Start(w);
        long day = w.Calendar.TotalDays;
        if (day == _lastDay) return;
        _lastDay = day;
        _watch.Observe(w);
        int decrees = w.Policies.Count(p => p.Enabled);
        Write(w, "state", string.Create(CultureInfo.InvariantCulture,
            $"pop={w.Households.Count} decrees={decrees} ca={w.AdminUsed}/{w.AdminCapacity} " +
            $"food={Units(w, "food")} firewood={Units(w, "firewood")} tools={Units(w, "tools")}"));
        Crisis(w, ref _firewood, _watch.FirewoodDay, "firewood");
        Crisis(w, ref _tools, _watch.ToolsDay, "tools");
        Crisis(w, ref _hunger, _watch.WinterHungerDay, "winter_hunger");
        Crisis(w, ref _suggestion, _watch.SuggestionDay, "suggestion_pending");
    }

    private void Crisis(World w, ref bool written, long? day, string name)
    {
        if (written || day is null) return;
        written = true;
        Write(w, name == "suggestion_pending" ? "state" : "crisis", name);
    }

    private static long Units(World w, string res) => w.StorageStock(w.Content.Resource(res).Index).WholeUnits;

    /// <summary>The notable sim events (same set the candidate build logs).</summary>
    public static string? Describe(SimEvent e) => e switch
    {
        SuggestionOffered s => $"suggestion_offered {s.SuggestionId}",
        HouseholdLeft l => $"household_left {l.Name} {l.Reason}",
        BuildingCompleted b => $"building_completed {b.BuildingId}",
        SimAlert a => $"alert {a.Text}",
        CommandRejected r => $"rejected {r.Command} {r.Reason}",
        _ => null,
    };

    private void Write(World w, string kind, string detail)
    {
        output.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"{realSeconds():0.0},{w.Calendar.TotalDays},{kind},\"{detail.Replace("\"", "'")}\""));
        output.Flush();
    }

    public void Dispose() => output.Dispose();
}
