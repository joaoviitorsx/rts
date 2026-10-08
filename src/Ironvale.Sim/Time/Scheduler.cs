namespace Ironvale.Sim.Time;

public enum Frequency { Tick, Hourly, Daily, Weekly, Monthly, Seasonal, Yearly }

/// <summary>Fixed order inside one step (GDD v0.2 §7). Order = determinism.</summary>
public enum Phase { Production, Transport, Consumption, Needs, Decisions }

public interface ISimSystem
{
    string Name { get; }
    Phase Phase { get; }
    Frequency Frequency { get; }
    void Run(World world, in Calendar cal);
}

/// <summary>
/// Runs registered systems by phase, then by registration order, each only when its frequency fires.
/// </summary>
public sealed class Scheduler
{
    private readonly List<ISimSystem> _registered = new();
    private ISimSystem[] _ordered = Array.Empty<ISimSystem>();

    public IReadOnlyList<ISimSystem> Systems => _ordered;

    public void Register(ISimSystem system)
    {
        _registered.Add(system);
        // OrderBy is stable: registration order is kept inside a phase.
        _ordered = _registered.OrderBy(s => (int)s.Phase).ToArray();
    }

    public static bool Fires(Frequency frequency, in Calendar cal) => frequency switch
    {
        Frequency.Tick => true,
        Frequency.Hourly => cal.IsHourStart,
        Frequency.Daily => cal.IsDayStart,
        Frequency.Weekly => cal.IsWeekStart,
        Frequency.Monthly => cal.IsMonthStart,
        Frequency.Seasonal => cal.IsSeasonStart,
        Frequency.Yearly => cal.IsYearStart,
        _ => false,
    };

    public void RunStep(World world, in Calendar cal)
    {
        foreach (var system in _ordered)
        {
            if (Fires(system.Frequency, cal)) system.Run(world, cal);
        }
    }
}
