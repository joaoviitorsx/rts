namespace Ironvale.Sim.Economy;

/// <summary>
/// A resource lying on the ground (GDD v0.3 §4.3): a felled log, a carcass (food, hides), a load dropped by a scared
/// colonist. Counted by the ledger like any stock; units pick it up and carry it to storage.
/// </summary>
public sealed class GroundItem
{
    public int Id { get; internal set; }
    public Cell Cell { get; internal set; }
    public int Resource { get; internal set; }
    public Qty Amount { get; internal set; }
}
