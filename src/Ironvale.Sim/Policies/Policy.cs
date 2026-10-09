namespace Ironvale.Sim.Policies;

/// <summary>
/// Decree "keep resource X between Min and Max" (Marco 2A.2): below Min the reeve recruits families to produce it,
/// above Max it releases them, in between it does nothing (explicit hysteresis band chosen by the player).
/// </summary>
public sealed class Policy
{
    public int Id { get; internal set; }
    public PolicyDef Def { get; internal set; } = null!;
    public bool Enabled { get; internal set; } = true;
    public int Resource { get; internal set; }
    public Qty Min { get; internal set; }
    public Qty Max { get; internal set; }
    public long CreatedTick { get; internal set; }
    /// <summary>Last "can't act" reason, so the log is not spammed daily with the same message.</summary>
    internal string LastBlockedReason { get; set; } = "";
    public string BlockedReason => LastBlockedReason;
}

/// <summary>
/// One line of the reeve's account book. <see cref="Key"/> + <see cref="Args"/> let the UI translate it
/// ("log.&lt;key&gt;" in ui.csv); <see cref="Text"/> is the Portuguese rendering for the CLI and tests.
/// </summary>
public readonly record struct PolicyLogEntry(long Tick, int PolicyId, string Key, string[] Args)
{
    public string Text => PolicyLogText.Format(Key, Args);
}

/// <summary>Portuguese templates of the account book (same argument order as the ui.csv "log.*" keys).</summary>
public static class PolicyLogText
{
    private static readonly Dictionary<string, string> Templates = new(StringComparer.Ordinal)
    {
        ["created"] = "Decreto criado: manter {0} entre {1} e {2}",
        ["band_changed"] = "Decreto alterado: manter {0} entre {1} e {2}",
        ["recipe_changed"] = "{0} passou a fazer {1}",
        ["recruited"] = "Família {0} → {1}{2}: {3} {4} abaixo do mínimo {5}",
        ["released"] = "Família {0} liberada de {1}: {2} {3} acima do máximo {4}",
        ["blocked_max"] = "{0} {1} abaixo do mínimo {2}, mas já uso o máximo de {3} famílias",
        ["blocked_no_slot"] = "{0} {1} abaixo do mínimo {2}, mas não há vaga produtiva para {0}",
        ["blocked_no_household"] = "{0} {1} abaixo do mínimo {2}, mas não há família disponível",
    };

    public static string Format(string key, string[] args) =>
        Templates.TryGetValue(key, out var t) ? string.Format(System.Globalization.CultureInfo.InvariantCulture, t, args) : key;
}
