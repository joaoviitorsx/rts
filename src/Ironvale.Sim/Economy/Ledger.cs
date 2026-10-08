namespace Ironvale.Sim.Economy;

/// <summary>
/// Conservation bookkeeping: for every resource,
/// Σ building stocks + Σ shipments == initial + produced − consumed. Checked by tests every day.
/// </summary>
public sealed class Ledger
{
    internal long[] Initial { get; }
    internal long[] Produced { get; }
    internal long[] Consumed { get; }

    public Ledger(int resourceCount)
    {
        Initial = new long[resourceCount];
        Produced = new long[resourceCount];
        Consumed = new long[resourceCount];
    }

    public Qty InitialOf(int r) => new(Initial[r]);
    public Qty ProducedOf(int r) => new(Produced[r]);
    public Qty ConsumedOf(int r) => new(Consumed[r]);
    public Qty ExpectedOf(int r) => new(Initial[r] + Produced[r] - Consumed[r]);
}
