namespace Ironvale.Sim.Policies;

/// <summary>"Manter recurso X acima de N" — reallocates households automatically (GDD §4.1 nível 2).</summary>
public sealed class Policy
{
    public int Id { get; internal set; }
    public PolicyDef Def { get; internal set; } = null!;
    public bool Enabled { get; internal set; } = true;
    public int Resource { get; internal set; }
    public Qty Threshold { get; internal set; }
    public long CreatedTick { get; internal set; }
    /// <summary>Last "can't act" reason, so the log is not spammed daily with the same message.</summary>
    internal string LastBlockedReason { get; set; } = "";

    public Qty ReleaseAbove => Threshold.MulPermille(Permille.One + Def.HysteresisPermille);
}

public readonly record struct PolicyLogEntry(long Tick, int PolicyId, string Text);
