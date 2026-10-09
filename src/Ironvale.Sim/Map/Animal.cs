namespace Ironvale.Sim.Map;

public enum AnimalState { Grazing, Walking, Fleeing, Stalking, Retreating }

/// <summary>
/// A wild animal of a generated map (GDD v0.3 §9): deer and rabbits wander around their herd's home and flee from
/// colonists; wolves roam in the cold seasons, scare lone colonists and back off from groups. Hunted animals leave a
/// carcass on the ground.
/// </summary>
public sealed class Animal
{
    public int Id { get; internal set; }
    public FaunaKind Kind { get; internal set; }
    /// <summary>Index of the spawn (herd) it belongs to: home and reproduction.</summary>
    public int Herd { get; internal set; }
    public Cell Home { get; internal set; }
    public Cell Pos { get; internal set; }
    public Cell Next { get; internal set; }
    public int StepTicks { get; internal set; }
    public Cell Goal { get; internal set; }
    public AnimalState State { get; internal set; }
    /// <summary>Ticks to keep fleeing / grazing / staying away (wolves after being scared).</summary>
    public int Timer { get; internal set; }
    /// <summary>Unit this animal flees from or this wolf stalks (0 = none).</summary>
    public int OtherId { get; internal set; }

    public bool IsMoving => Next != Pos;
    public bool Huntable => Kind is FaunaKind.Deer or FaunaKind.Rabbit;
}
