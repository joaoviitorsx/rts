using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Ironvale.Game;

/// <summary>
/// Dev tool: measures real frame times for N seconds (after a warm-up), prints average FPS, 1% low and the
/// slowest frame, then quits. Run without --write-movie: "-- --perf=10".
/// </summary>
public partial class PerfProbe : Node
{
    private const double WarmUp = 2.0;
    private double _duration;
    private double _elapsed;
    private readonly List<double> _frames = new();

    public static void AttachIfRequested(Node parent)
    {
        var arg = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--perf="));
        if (arg is null || !double.TryParse(arg[7..], System.Globalization.CultureInfo.InvariantCulture, out double s)) return;
        var vs = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--vsync="))?[8..];
        if (vs is not null)
            DisplayServer.WindowSetVsyncMode(vs switch
            {
                "off" => DisplayServer.VSyncMode.Disabled,
                "mailbox" => DisplayServer.VSyncMode.Mailbox,
                "adaptive" => DisplayServer.VSyncMode.Adaptive,
                _ => DisplayServer.VSyncMode.Enabled,
            });
        parent.AddChild(new PerfProbe { Name = "PerfProbe", _duration = s, ProcessMode = ProcessModeEnum.Always });
    }

    public override void _Process(double delta)
    {
        _elapsed += delta;
        if (_elapsed < WarmUp) return;
        _frames.Add(delta * 1000.0);
        if (_elapsed < WarmUp + _duration) return;

        var hitches = _frames.Select((f, i) => (f, i)).Where(x => x.f > 33).ToList();
        GD.Print($"PERF hitches>33ms={hitches.Count} at frames: " +
                 string.Join(" ", hitches.Take(25).Select(h => $"#{h.i}:{h.f:0}ms")));
        var sorted = _frames.OrderByDescending(f => f).ToList();
        double avg = _frames.Average();
        double low1 = sorted.Take(Mathf.Max(1, sorted.Count / 100)).Average();
        GD.Print($"PERF frames={_frames.Count} avg_fps={1000.0 / avg:0.0} avg_ms={avg:0.00} " +
                 $"1%low_fps={1000.0 / low1:0.0} worst_ms={sorted[0]:0.0} " +
                 $"draw_calls={Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame)} " +
                 $"objects={Performance.GetMonitor(Performance.Monitor.RenderTotalObjectsInFrame)} " +
                 $"primitives={Performance.GetMonitor(Performance.Monitor.RenderTotalPrimitivesInFrame)} " +
                 $"vsync={DisplayServer.WindowGetVsyncMode()} gc={System.GC.CollectionCount(0)}/{System.GC.CollectionCount(1)}/{System.GC.CollectionCount(2)}");
        GetTree().Quit();
    }
}
