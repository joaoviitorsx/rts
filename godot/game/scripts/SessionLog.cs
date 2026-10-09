using System;
using System.Collections.Generic;
using Godot;
using Ironvale.Sim.Commands;
using Ironvale.Sim.Events;

namespace Ironvale.Game;

/// <summary>
/// Playtest instrumentation (observes only): one CSV per session in user://playtest/ with real seconds, game day,
/// every command the player sent and the notable sim events (decree offers, families leaving, completions, alerts).
/// Feeds the UI guide §9.3 / GDD §8.3 metrics: clicks per minute, decrees issued, suggestions accepted, crises.
/// Windows: %APPDATA%\Godot\app_userdata\Ironvale\playtest · Linux: ~/.local/share/godot/app_userdata/Ironvale/playtest
/// </summary>
public sealed class SessionLog : IDisposable
{
    private readonly FileAccess? _file;
    private readonly ulong _startMsec = Time.GetTicksMsec();

    public SessionLog()
    {
        DirAccess.MakeDirRecursiveAbsolute("user://playtest");
        string stamp = Time.GetDatetimeStringFromSystem().Replace(':', '-');
        _file = FileAccess.Open($"user://playtest/session_{stamp}.csv", FileAccess.ModeFlags.Write);
        _file?.StoreLine("real_s,game_day,kind,detail");
    }

    public void Command(SimCommand c, long gameDay) => Write(gameDay, "command", c.ToString());

    public void Events(IEnumerable<SimEvent> events, long gameDay)
    {
        foreach (var e in events)
        {
            string? detail = e switch
            {
                SuggestionOffered s => $"suggestion_offered {s.SuggestionId}",
                HouseholdLeft l => $"household_left {l.Name} {l.Reason}",
                BuildingCompleted b => $"building_completed {b.BuildingId}",
                SimAlert a => $"alert {a.Text}",
                CommandRejected r => $"rejected {r.Command} {r.Reason}",
                _ => null,
            };
            if (detail is not null) Write(gameDay, "event", detail);
        }
    }

    private void Write(long gameDay, string kind, string detail)
    {
        if (_file is null) return;
        double s = (Time.GetTicksMsec() - _startMsec) / 1000.0;
        _file.StoreLine(string.Create(System.Globalization.CultureInfo.InvariantCulture,
            $"{s:0.0},{gameDay},{kind},\"{detail.Replace("\"", "'")}\""));
        _file.Flush();
    }

    public void Dispose() => _file?.Close();
}
