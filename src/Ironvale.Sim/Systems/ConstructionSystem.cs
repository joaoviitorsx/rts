namespace Ironvale.Sim.Systems;

/// <summary>Construction sites progress one day at a time (cost was paid on placement — TDD Q6e).</summary>
public sealed class ConstructionSystem : ISimSystem
{
    public string Name => "construction";
    public Phase Phase => Phase.Production;
    public Frequency Frequency => Frequency.Daily;

    public void Run(World w, in Calendar cal)
    {
        foreach (var b in w.Buildings)
        {
            if (b.IsActive) continue;
            b.BuildProgressDays++;
            if (b.BuildProgressDays >= b.Def.BuildDays)
            {
                b.IsActive = true;
                w.Emit(new BuildingCompleted(cal.Tick, b.Id));
            }
        }
    }
}
