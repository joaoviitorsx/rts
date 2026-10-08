namespace Ironvale.Sim.Telemetry;

/// <summary>One day of flows/stocks per resource (milli units).</summary>
public sealed class DailySample
{
    public long Day { get; init; }
    public required long[] Produced { get; init; }
    public required long[] Consumed { get; init; }
    /// <summary>In storage buildings (celeiro, Salão).</summary>
    public required long[] Stored { get; init; }
    /// <summary>In producer buildings' output buffers, waiting for carriers.</summary>
    public required long[] Local { get; init; }
    public required long[] Transit { get; init; }
    public int Population { get; init; }
}

public sealed class MonthlySample
{
    public long MonthIndex { get; init; }
    public required long[] Produced { get; init; }
    public required long[] Consumed { get; init; }
    public required long[] StoredAtEnd { get; init; }
    public int PopulationAtEnd { get; init; }
}

/// <summary>
/// Time series per resource (daily ring of the last year + monthly history since the start) and
/// the deadlock detector. Not part of the state hash: it observes, never decides.
/// </summary>
public sealed class TelemetryRecorder
{
    public const int DailyCapacity = SimTime.DaysPerYear;

    private readonly int _resources;
    private readonly DailySample?[] _ring = new DailySample?[DailyCapacity];
    private int _ringNext;
    private int _ringCount;

    internal long[] TodayProduced;
    internal long[] TodayConsumed;
    internal long[] MonthProduced;
    internal long[] MonthConsumed;
    internal bool ActivityToday;

    public List<MonthlySample> Monthly { get; } = new();
    public int FrozenDays { get; internal set; }
    public bool Deadlocked { get; internal set; }
    public long DeadlockDay { get; internal set; } = -1;

    public TelemetryRecorder(int resourceCount)
    {
        _resources = resourceCount;
        TodayProduced = new long[resourceCount];
        TodayConsumed = new long[resourceCount];
        MonthProduced = new long[resourceCount];
        MonthConsumed = new long[resourceCount];
    }

    /// <summary>Daily samples, oldest first (up to one year).</summary>
    public IEnumerable<DailySample> Daily
    {
        get
        {
            int start = (_ringNext - _ringCount + DailyCapacity) % DailyCapacity;
            for (int i = 0; i < _ringCount; i++) yield return _ring[(start + i) % DailyCapacity]!;
        }
    }

    public DailySample? LastDay => _ringCount == 0 ? null : _ring[(_ringNext - 1 + DailyCapacity) % DailyCapacity];

    internal void OnProduced(int r, Qty q, bool economic)
    {
        TodayProduced[r] += q.Milli;
        if (economic && q.IsPositive) ActivityToday = true;
    }

    internal void OnConsumed(int r, Qty q, bool fromStorage)
    {
        TodayConsumed[r] += q.Milli;
        if (fromStorage && q.IsPositive) ActivityToday = true;
    }

    internal void OnDelivery() => ActivityToday = true;

    internal void AddDaily(DailySample sample)
    {
        _ring[_ringNext] = sample;
        _ringNext = (_ringNext + 1) % DailyCapacity;
        _ringCount = Math.Min(_ringCount + 1, DailyCapacity);
    }

    internal void CloseDay(World w, in Calendar cal)
    {
        var stored = new long[_resources];
        var local = new long[_resources];
        var transit = new long[_resources];
        foreach (var b in w.Buildings)
        {
            var target = b.IsStorage ? stored : b.IsProducer ? local : null;
            if (target is null) continue;
            for (int r = 0; r < _resources; r++) target[r] += b.Stock.Get(r).Milli;
        }
        foreach (var s in w.Shipments) transit[s.Resource] += s.Amount.Milli;

        AddDaily(new DailySample
        {
            Day = cal.TotalDays,
            Produced = (long[])TodayProduced.Clone(),
            Consumed = (long[])TodayConsumed.Clone(),
            Stored = stored,
            Local = local,
            Transit = transit,
            Population = w.Households.Count,
        });

        for (int r = 0; r < _resources; r++)
        {
            MonthProduced[r] += TodayProduced[r];
            MonthConsumed[r] += TodayConsumed[r];
        }
        Array.Clear(TodayProduced);
        Array.Clear(TodayConsumed);

        if (cal.DayOfMonth == SimTime.DaysPerMonth - 1)
        {
            Monthly.Add(new MonthlySample
            {
                MonthIndex = cal.Tick / SimTime.TicksPerMonth,
                Produced = (long[])MonthProduced.Clone(),
                Consumed = (long[])MonthConsumed.Clone(),
                StoredAtEnd = stored,
                PopulationAtEnd = w.Households.Count,
            });
            Array.Clear(MonthProduced);
            Array.Clear(MonthConsumed);
        }

        // Deadlock: people exist, but nothing is produced by buildings, nothing is delivered and
        // nothing leaves storage for a whole window (stocks frozen, everyone only subsisting).
        if (w.Households.Count > 0 && !ActivityToday) FrozenDays++;
        else FrozenDays = 0;
        if (!Deadlocked && FrozenDays >= w.Content.Balance.DeadlockWindowDays)
        {
            Deadlocked = true;
            DeadlockDay = cal.TotalDays;
            w.Emit(new SimAlert(cal.Tick, $"Economia travada há {FrozenDays} dias"));
        }
        ActivityToday = false;
    }
}
