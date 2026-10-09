using Godot;
using Ironvale.Sim.Time;

namespace Ironvale.Game;

/// <summary>Feeds WorldSky.gd from the sim every frame: time of day, today's and tomorrow's weather, the camera's focus.</summary>
public partial class SkyDriver : Node
{
    public SimHost Host { get; set; } = null!;
    public Node3D Sky { get; set; } = null!;
    public CameraRig Camera { get; set; } = null!;
    /// <summary>Dev (--weather=Rain|Snow|Cloudy|Clear): show that weather (view only, the sim is untouched).</summary>
    private readonly string? _forced = System.Linq.Enumerable.FirstOrDefault(OS.GetCmdlineUserArgs(), a => a.StartsWith("--weather="))?[10..];

    public override void _Process(double delta)
    {
        if (Host?.World is not { } w || w.Terrain is null) return;
        float day = (w.Tick % SimTime.TicksPerDay + Host.TickAlpha) / SimTime.TicksPerDay;
        string today = _forced ?? w.WeatherToday.ToString();
        Sky.Call("set_clock", day, today, w.WeatherTomorrow.ToString(), Camera.GlobalPosition, delta);
    }
}
