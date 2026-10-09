using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using Ironvale.Sim;
using Ironvale.Sim.Commands;
using Ironvale.Sim.Events;
using Ironvale.Sim.Scripting;

namespace Ironvale.Game;

/// <summary>
/// Playtest instrumentation (observes only): one CSV per session in user://playtest/ (format: <see cref="SessionRecorder"/>,
/// v2 — commands, notable events, a daily state row and crisis rows). Read by tools/analyze_playtest.py.
/// Windows: %APPDATA%\Godot\app_userdata\Ironvale\playtest · Linux: ~/.local/share/godot/app_userdata/Ironvale/playtest
/// </summary>
public sealed class SessionLog : IDisposable
{
    private readonly SessionRecorder? _recorder;
    private readonly ulong _startMsec = Time.GetTicksMsec();

    public SessionLog()
    {
        DirAccess.MakeDirRecursiveAbsolute("user://playtest");
        string stamp = Time.GetDatetimeStringFromSystem().Replace(':', '-');
        string path = ProjectSettings.GlobalizePath($"user://playtest/session_{stamp}.csv");
        try
        {
            _recorder = new SessionRecorder(new StreamWriter(path), () => (Time.GetTicksMsec() - _startMsec) / 1000.0);
        }
        catch (IOException e)
        {
            GD.PushWarning($"playtest log disabled: {e.Message}");
        }
    }

    public void Command(World w, SimCommand c) => _recorder?.Command(w, c);
    public void Events(World w, IEnumerable<SimEvent> events) => _recorder?.Events(w, events);
    public void Tick(World w) => _recorder?.Tick(w);

    public void Dispose() => _recorder?.Dispose();
}
