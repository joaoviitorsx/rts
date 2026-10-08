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
        for (int r = 0; r < def.Cost.Length; r++)
        {
            if (w.StorageFree(r) < def.Cost[r])
                return $"faltam {w.Content.Resources[r].Name} ({w.StorageFree(r)}/{def.Cost[r]})";
        }
        for (int r = 0; r < def.Cost.Length; r++)
        {
            if (!def.Cost[r].IsPositive) continue;
            var taken = w.TakeFromStorages(r, def.Cost[r], Origin);
            w.RecordConsumed(r, taken, fromStorage: true);
        }
        w.AddBuilding(def, Origin, rot, active: def.BuildDays <= 0);
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
        w.RemoveBuilding(b);
        for (int r = 0; r < b.Def.Cost.Length; r++)
        {
            if (!b.Def.Cost[r].IsPositive) continue;
            var added = w.AddToStorages(r, b.Def.Cost[r], b.Center);
            w.RecordProduced(r, added, economic: false);
        }
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
        if (!b.IsActive) return "edifício ainda em obra";
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

public sealed record CreatePolicy(string DefId, string ResourceId, long ThresholdUnits) : SimCommand
{
    internal override string? Apply(World w)
    {
        if (!w.Content.TryPolicy(DefId, out var def)) return $"política desconhecida '{DefId}'";
        if (!w.Content.TryResource(ResourceId, out var res)) return $"recurso desconhecido '{ResourceId}'";
        if (ThresholdUnits < 0) return "limiar negativo";
        if (w.Policies.Any(p => p.Def == def && p.Resource == res.Index))
            return $"já existe uma política para {res.Name}";
        w.AddPolicy(def, res.Index, Qty.Units(ThresholdUnits));
        return null;
    }
}

public sealed record SetPolicyThreshold(int PolicyId, long ThresholdUnits) : SimCommand
{
    internal override string? Apply(World w)
    {
        var p = w.GetPolicy(PolicyId);
        if (p is null) return "política não existe";
        if (ThresholdUnits < 0) return "limiar negativo";
        p.Threshold = Qty.Units(ThresholdUnits);
        p.LastBlockedReason = "";
        return null;
    }
}

public sealed record SetPolicyEnabled(int PolicyId, bool Enabled) : SimCommand
{
    internal override string? Apply(World w)
    {
        var p = w.GetPolicy(PolicyId);
        if (p is null) return "política não existe";
        p.Enabled = Enabled;
        return null;
    }
}

public sealed record RemovePolicy(int PolicyId) : SimCommand
{
    internal override string? Apply(World w)
    {
        var p = w.GetPolicy(PolicyId);
        if (p is null) return "política não existe";
        w.RemovePolicyInternal(p);
        return null;
    }
}
