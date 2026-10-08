namespace Ironvale.Sim.Logistics;

public enum CarrierPhase { Idle, ToPickup, Loading, ToDropoff, Unloading, Returning }

/// <summary>
/// A household employed at a storage building, walking cell by cell. While carrying, the resource lives
/// only in its <see cref="Shipment"/> (in transit), never in two places and never teleported.
/// </summary>
public sealed class Carrier
{
    public int Id { get; internal set; }
    /// <summary>0 when the household was unassigned mid-delivery: the carrier finishes and then disappears.</summary>
    public int HouseholdId { get; internal set; }
    public int BaseId { get; internal set; }
    public CarrierPhase Phase { get; internal set; }
    public Cell Pos { get; internal set; }
    public Cell Target { get; internal set; }
    /// <summary>Progress (ticks) of the step from <see cref="Pos"/> to <see cref="NextCell"/>.</summary>
    public int StepTicks { get; internal set; }
    public int WaitTicks { get; internal set; }
    public int PickupId { get; internal set; }
    public int DropoffId { get; internal set; }
    public int Resource { get; internal set; } = -1;
    public Qty Amount { get; internal set; }
    public int ShipmentId { get; internal set; }

    public bool Retiring => HouseholdId == 0;
    public Cell NextCell => Pos.StepToward(Target);
    public bool IsMoving => Phase is CarrierPhase.ToPickup or CarrierPhase.ToDropoff or CarrierPhase.Returning && Pos != Target;
}

public sealed class Shipment
{
    public int Id { get; internal set; }
    public int Resource { get; internal set; }
    public Qty Amount { get; internal set; }
    public int FromId { get; internal set; }
    public int ToId { get; internal set; }
    public int CarrierId { get; internal set; }
}
