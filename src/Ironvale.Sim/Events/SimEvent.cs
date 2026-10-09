namespace Ironvale.Sim.Events;

/// <summary>Notifications for the view (sim → view). Only collected when <see cref="World.CollectEvents"/> is on.</summary>
public abstract record SimEvent(long Tick);

public sealed record BuildingPlaced(long Tick, int BuildingId) : SimEvent(Tick);
public sealed record BuildingCompleted(long Tick, int BuildingId) : SimEvent(Tick);
/// <summary>A woodcutter felled a tree (generated maps): the view swaps it for a stump.</summary>
public sealed record TreeFelled(long Tick, int BuildingId, Cell Cell) : SimEvent(Tick);
public sealed record BuildingRemoved(long Tick, int BuildingId) : SimEvent(Tick);
public sealed record HouseholdAssigned(long Tick, int HouseholdId, int BuildingId) : SimEvent(Tick);
public sealed record HouseholdLeft(long Tick, int HouseholdId, string Name, string Reason) : SimEvent(Tick);
public sealed record PolicyActed(long Tick, int PolicyId, string Text) : SimEvent(Tick);
public sealed record CommandRejected(long Tick, string Command, string Reason) : SimEvent(Tick);
public sealed record SimAlert(long Tick, string Text) : SimEvent(Tick);
public sealed record SuggestionOffered(long Tick, int SuggestionId) : SimEvent(Tick);

// ---- RTS opening (GDD v0.3): generated maps only
public sealed record UnitLeft(long Tick, int UnitId, string Name, string Reason) : SimEvent(Tick);
/// <summary>A wolf scared a lone colonist: the load fell where they stood.</summary>
public sealed record UnitScared(long Tick, int UnitId, int WolfId) : SimEvent(Tick);
public sealed record AnimalKilled(long Tick, int AnimalId, int UnitId, Cell Cell) : SimEvent(Tick);
/// <summary>Rain spoiled part of an uncovered pile.</summary>
public sealed record Spoiled(long Tick, int BuildingId, int Resource, Qty Amount) : SimEvent(Tick);

