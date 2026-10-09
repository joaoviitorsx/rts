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
        if (w.PlacementError(def, Origin, rot) is { } error) return error;
        // Nothing is paid here: carriers bring the materials to the site and builders use them (Marco 2A.1).
        w.AddBuilding(def, Origin, rot, active: false);
        return null;
    }
}

/// <summary>Lays road on free cells (Marco 2A.3). Stone is paid per cell, from storage; occupied cells are skipped.</summary>
public sealed record PlaceRoad(Cell[] Cells) : SimCommand
{
    internal override string? Apply(World w)
    {
        var cells = Cells.Distinct().Where(c => w.Map.InBounds(c) && w.Map.BuildingAt(c) == 0 && !w.Map.IsRoad(c)
                                                && w.RoadAllowed(c)).ToList();
        if (cells.Count == 0) return "nenhuma célula livre para estrada";
        int stone = w.Content.Resource("stone").Index;
        var cost = w.Content.Balance.RoadStonePerCell * cells.Count;
        if (w.StorageFree(stone) < cost) return $"faltam {w.Content.Resources[stone].Name} ({w.StorageFree(stone)}/{cost})";
        var center = cells[cells.Count / 2];
        w.RecordConsumed(stone, w.TakeFromStorages(stone, cost, center), fromStorage: true);
        foreach (var c in cells) w.Map.SetRoad(c, true);
        return null;
    }
}

/// <summary>Removes road cells (no refund).</summary>
public sealed record RemoveRoad(Cell[] Cells) : SimCommand
{
    internal override string? Apply(World w)
    {
        var cells = Cells.Distinct().Where(w.Map.IsRoad).ToList();
        if (cells.Count == 0) return "nenhuma estrada aqui";
        foreach (var c in cells) w.Map.SetRoad(c, false);
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
        bool moved = h.HasJob;   // a reallocation, not a first hire: that is what the reeve learns from
        w.Assign(h, b, AssignmentSource.Player, 0);
        if (moved && b.IsProducer && b.Recipe is { } recipe) w.ObservePlayerAction(PrimaryOutput(recipe));
        return null;
    }

    internal static int PrimaryOutput(RecipeDef recipe) => Array.FindIndex(recipe.OutputPerWorkerHour, q => q.IsPositive);
}

/// <summary>Accepts the reeve's offer: the suggested decree is issued as is.</summary>
public sealed record AcceptSuggestion(int SuggestionId) : SimCommand
{
    internal override string? Apply(World w)
    {
        var s = w.Suggestion;
        if (s is null || s.Id != SuggestionId) return "sugestão não existe mais";
        w.Suggestion = null;
        if (w.Policies.Any(p => p.Resource == s.Resource)) return $"já existe um decreto para {w.Content.Resources[s.Resource].Name}";
        w.AddPolicy(w.Content.Policies[0], s.Resource, s.Min, s.Max);
        return null;
    }
}

/// <summary>"Agora não" (snooze) or "Nunca" (never again for this resource).</summary>
public sealed record DismissSuggestion(int SuggestionId, bool Forever) : SimCommand
{
    internal override string? Apply(World w)
    {
        var s = w.Suggestion;
        if (s is null || s.Id != SuggestionId) return "sugestão não existe mais";
        w.Suggestion = null;
        w.MuteSuggestions(s.Resource, Forever);
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
        bool changed = b.Recipe != recipe;
        w.ChangeRecipe(b, recipe);
        if (changed && b.AssignedCount > 0) w.ObservePlayerAction(AssignHousehold.PrimaryOutput(recipe));
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
