namespace Ironvale.Sim.Policies;

/// <summary>A manual move the player made toward producing <see cref="Resource"/> while it was at <see cref="StockUnits"/>.</summary>
public readonly record struct PlayerAction(long Tick, int Resource, long StockUnits);

/// <summary>
/// "The game learns from you" (GDD v0.2 §3.2): after repeated manual moves toward the same resource, the reeve offers
/// to keep it between <see cref="Min"/> and <see cref="Max"/> as a decree.
/// </summary>
public sealed class DecreeSuggestion
{
    public int Id { get; internal set; }
    public int Resource { get; internal set; }
    public Qty Min { get; internal set; }
    public Qty Max { get; internal set; }
    /// <summary>How many manual moves led to it, and the average stock at those moments (for the explanation).</summary>
    public int Actions { get; internal set; }
    public long AverageStockUnits { get; internal set; }
    public long OfferedTick { get; internal set; }
    /// <summary>The minimum was raised above the observed stock to cover part of the winter (P21).</summary>
    public bool WinterAdjusted { get; internal set; }
}
