namespace Ironvale.Sim.Events;

/// <summary>Notifications for the view (sim → view). Only collected when <see cref="World.CollectEvents"/> is on.</summary>
public abstract record SimEvent(long Tick);

public sealed record BuildingPlaced(long Tick, int BuildingId) : SimEvent(Tick);
public sealed record BuildingCompleted(long Tick, int BuildingId) : SimEvent(Tick);
public sealed record BuildingRemoved(long Tick, int BuildingId) : SimEvent(Tick);
public sealed record HouseholdAssigned(long Tick, int HouseholdId, int BuildingId) : SimEvent(Tick);
public sealed record HouseholdLeft(long Tick, int HouseholdId, string Name, string Reason) : SimEvent(Tick);
public sealed record PolicyActed(long Tick, int PolicyId, string Text) : SimEvent(Tick);
public sealed record CommandRejected(long Tick, string Command, string Reason) : SimEvent(Tick);
public sealed record SimAlert(long Tick, string Text) : SimEvent(Tick);
