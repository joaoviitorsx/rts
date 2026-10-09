namespace Ironvale.Sim.Commands;

/// <summary>
/// The only way the view changes the simulation. Queued with <see cref="World.Enqueue"/> and applied at the
/// start of the next <see cref="World.Step"/>, in arrival order. Validation happens inside the sim.
/// </summary>
public abstract record SimCommand
{
    /// <returns>null when applied, otherwise the rejection reason.</returns>
    internal abstract string? Apply(World world);
}

public sealed record PlaceBuilding(string DefId, Cell Origin, int Rotation) : SimCommand
{
    internal override string? Apply(World w)
    {
        if (!w.Content.TryBuilding(DefId, out var def)) return $"edifício desconhecido '{DefId}'";
        if (!def.Buildable) return $"{def.Name} não pode ser construído";
        int rot = ((Rotation % 4) + 4) % 4;
        if (!w.Map.CanPlace(def, Origin, rot)) return "local ocupado ou fora do mapa";
        // Nothing is paid here: carriers bring the materials to the site and builders use them (Marco 2A.1).
        w.AddBuilding(def, Origin, rot, active: false);
        return null;
    }
}

public sealed record CancelConstruction(int BuildingId) : SimCommand
{
    internal override string? Apply(World w)
    {
        var b = w.GetBuilding(BuildingId);
        if (b is null) return "edifício não existe";
        if (b.IsActive) return "só obras em andamento podem ser canceladas";
        w.CancelSite(b);
        return null;
    }
}

public sealed record AssignHousehold(int HouseholdId, int BuildingId) : SimCommand
{
    internal override string? Apply(World w)
    {
        var h = w.GetHousehold(HouseholdId);
        var b = w.GetBuilding(BuildingId);
        if (h is null) return "família não existe";
        if (b is null) return "edifício não existe";
        if (!b.IsActive)
        {
            if (h.JobBuildingId == b.Id) return null;
            if (w.Households.Count(x => x.JobBuildingId == b.Id) >= w.Content.Balance.MaxBuildersPerSite)
                return "obra já tem o máximo de construtores";
            w.AssignBuilder(h, b);
            return null;
        }
        if (h.JobBuildingId == b.Id)
        {
            h.AssignedBy = AssignmentSource.Player;   // player takes ownership of an existing assignment
            h.AssignedByPolicyId = 0;
            return null;
        }
        if (b.FreeSlotIndex() < 0) return "sem vagas";
        w.Assign(h, b, AssignmentSource.Player, 0);
        return null;
    }
}

public sealed record UnassignHousehold(int HouseholdId) : SimCommand
{
    internal override string? Apply(World w)
    {
        var h = w.GetHousehold(HouseholdId);
        if (h is null) return "família não existe";
        if (!h.HasJob) return "família sem emprego";
        w.Unassign(h);
        return null;
    }
}

public sealed record SetRecipe(int BuildingId, string RecipeId) : SimCommand
{
    internal override string? Apply(World w)
    {
        var b = w.GetBuilding(BuildingId);
        if (b is null) return "edifício não existe";
        var recipe = b.Def.Recipes.FirstOrDefault(r => r.Id == RecipeId);
        if (recipe is null) return $"{b.Def.Name} não faz '{RecipeId}'";
        w.ChangeRecipe(b, recipe);
        return null;
    }
}

/// <summary>Decree "keep resource between Min and Max" (units). Max must be greater than Min.</summary>
public sealed record CreatePolicy(string DefId, string ResourceId, long MinUnits, long MaxUnits) : SimCommand
{
    internal override string? Apply(World w)
    {
        if (!w.Content.TryPolicy(DefId, out var def)) return $"decreto desconhecido '{DefId}'";
        if (!w.Content.TryResource(ResourceId, out var res)) return $"recurso desconhecido '{ResourceId}'";
        if (PolicyBand.Validate(MinUnits, MaxUnits) is { } error) return error;
        if (w.Policies.Any(p => p.Def == def && p.Resource == res.Index))
            return $"já existe um decreto para {res.Name}";
        w.AddPolicy(def, res.Index, Qty.Units(MinUnits), Qty.Units(MaxUnits));
        return null;
    }
}

public sealed record SetPolicyBand(int PolicyId, long MinUnits, long MaxUnits) : SimCommand
{
    internal override string? Apply(World w)
    {
        var p = w.GetPolicy(PolicyId);
        if (p is null) return "decreto não existe";
        if (PolicyBand.Validate(MinUnits, MaxUnits) is { } error) return error;
        p.Min = Qty.Units(MinUnits);
        p.Max = Qty.Units(MaxUnits);
        p.LastBlockedReason = "";
        w.LogPolicy(p, "band_changed", w.Content.Resources[p.Resource].Name, Systems.PolicySystem.Units(p.Min), Systems.PolicySystem.Units(p.Max));
        return null;
    }
}

internal static class PolicyBand
{
    public static string? Validate(long min, long max) =>
        min < 0 ? "mínimo negativo" : max <= min ? "o máximo precisa ser maior que o mínimo" : null;
}

public sealed record SetPolicyEnabled(int PolicyId, bool Enabled) : SimCommand
{
    internal override string? Apply(World w)
    {
        var p = w.GetPolicy(PolicyId);
        if (p is null) return "decreto não existe";
        p.Enabled = Enabled;
        return null;
    }
}

public sealed record RemovePolicy(int PolicyId) : SimCommand
{
    internal override string? Apply(World w)
    {
        var p = w.GetPolicy(PolicyId);
        if (p is null) return "decreto não existe";
        w.RemovePolicyInternal(p);
        return null;
    }
}
