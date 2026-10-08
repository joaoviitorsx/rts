namespace Ironvale.Sim.Systems;

/// <summary>
/// Evaluates "keep resource X above N" policies daily (TDD Q7). At most one move per policy per
/// evaluation; never touches households assigned by the player; hysteresis avoids oscillation.
/// Every action is logged in plain language (the administrator explains itself, GDD §5.6).
/// </summary>
public sealed class PolicySystem : ISimSystem
{
    public string Name => "policy";
    public Phase Phase => Phase.Decisions;
    public Frequency Frequency => Frequency.Daily;

    public void Run(World w, in Calendar cal)
    {
        foreach (var p in w.Policies.ToArray())
        {
            if (!p.Enabled) continue;
            var stock = w.StorageStockIncludingTransit(p.Resource);
            if (stock < p.Threshold) Recruit(w, p, stock, cal);
            else if (stock > p.ReleaseAbove) Release(w, p, stock);
            else p.LastBlockedReason = "";
        }
    }

    private static string ResName(World w, Policy p) => w.Content.Resources[p.Resource].Name;

    private static void Recruit(World w, Policy p, Qty stock, in Calendar cal)
    {
        int mine = w.Households.Count(h => h.AssignedBy == AssignmentSource.Policy && h.AssignedByPolicyId == p.Id);
        if (mine >= p.Def.MaxHouseholds)
        {
            Blocked(w, p, $"{ResName(w, p)} {stock} < {p.Threshold}, mas já uso o máximo de {p.Def.MaxHouseholds} famílias");
            return;
        }

        var target = FindProducer(w, p, cal.Season);
        if (target is null)
        {
            Blocked(w, p, $"{ResName(w, p)} {stock} < {p.Threshold}, mas não há vaga produtiva para {ResName(w, p)}");
            return;
        }

        var candidate = FindCandidate(w, p);
        if (candidate is null)
        {
            Blocked(w, p, $"{ResName(w, p)} {stock} < {p.Threshold}, mas não há família disponível");
            return;
        }

        var (building, recipe) = target.Value;
        if (building.Recipe != recipe)
        {
            w.ChangeRecipe(building, recipe);
            w.LogPolicy(p, $"{w.DescribeBuilding(building)} passou a fazer {recipe.Name}");
        }
        string from = candidate.HasJob ? $" (saiu de {w.DescribeBuilding(w.GetBuilding(candidate.JobBuildingId)!)})" : "";
        w.Assign(candidate, building, AssignmentSource.Policy, p.Id);
        p.LastBlockedReason = "";
        w.LogPolicy(p, $"Família {candidate.Name} → {w.DescribeBuilding(building)}{from}: {ResName(w, p)} {stock} < {p.Threshold}");
    }

    private static void Release(World w, Policy p, Qty stock)
    {
        var h = w.Households.LastOrDefault(h => h.AssignedBy == AssignmentSource.Policy && h.AssignedByPolicyId == p.Id);
        if (h is null) return;
        var b = w.GetBuilding(h.JobBuildingId)!;
        w.Unassign(h);
        w.LogPolicy(p, $"Família {h.Name} liberada de {w.DescribeBuilding(b)}: {ResName(w, p)} {stock} > {p.ReleaseAbove}");
    }

    private static void Blocked(World w, Policy p, string reason)
    {
        // Same reason as last time → stay quiet (no daily spam).
        if (p.LastBlockedReason == reason) return;
        p.LastBlockedReason = reason;
        w.LogPolicy(p, reason);
    }

    /// <summary>
    /// A producer of the resource that is productive now and has a free slot. If none, an empty producer
    /// that could switch to a recipe for this resource.
    /// </summary>
    private static (Building, RecipeDef)? FindProducer(World w, Policy p, Season season)
    {
        foreach (var b in w.Buildings)
        {
            if (b.IsProductiveIn(season) && b.Recipe!.Produces(p.Resource) && b.FreeSlotIndex() >= 0)
                return (b, b.Recipe);
        }
        foreach (var b in w.Buildings)
        {
            if (!b.IsActive || !b.IsProducer || b.AssignedCount > 0) continue;
            var recipe = b.Def.Recipes.FirstOrDefault(r =>
                r.Produces(p.Resource) && (r.Kind == RecipeKind.Continuous || r.IsWorkSeason(season)));
            if (recipe is not null) return (b, recipe);
        }
        return null;
    }

    /// <summary>Unemployed first, then unowned workers, then households of other policies that have slack.</summary>
    private static Household? FindCandidate(World w, Policy p)
    {
        var idle = w.Households.FirstOrDefault(h => !h.HasJob);
        if (idle is not null) return idle;

        var unowned = w.Households.FirstOrDefault(h => h.HasJob && h.AssignedBy == AssignmentSource.None
            && w.GetBuilding(h.JobBuildingId) is { IsStorage: false });
        if (unowned is not null) return unowned;

        return w.Households.LastOrDefault(h =>
        {
            if (h.AssignedBy != AssignmentSource.Policy || h.AssignedByPolicyId == p.Id) return false;
            var other = w.GetPolicy(h.AssignedByPolicyId);
            return other is null || !other.Enabled || w.StorageStockIncludingTransit(other.Resource) >= other.Threshold;
        });
    }
}
