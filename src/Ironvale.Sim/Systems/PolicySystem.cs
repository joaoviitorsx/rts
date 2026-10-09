namespace Ironvale.Sim.Systems;

/// <summary>
/// Evaluates "keep resource X between Min and Max" decrees daily (TDD Q7, Marco 2A.2). At most one move per decree
/// per evaluation; never touches households assigned by the player; the Min–Max band avoids oscillation.
/// Every action is logged in plain language (the reeve explains itself, GDD §5.6).
/// </summary>
public sealed class PolicySystem : ISimSystem
{
    public string Name => "policy";
    public Phase Phase => Phase.Decisions;
    public Frequency Frequency => Frequency.Daily;

    public void Run(World w, in Calendar cal)
    {
        int overload = w.AdminOverload;
        var bal = w.Content.Balance;
        foreach (var p in w.Policies.ToArray())
        {
            if (!p.Enabled) continue;
            // Over capacity the reeve gets slow (evaluates every 1 + n·delay days) and errs (n·error ‰ per action).
            if (overload > 0 && (cal.TotalDays + p.Id) % (1 + overload * bal.AdminOverloadDelayDays) != 0) continue;
            var stock = w.StorageStockIncludingTransit(p.Resource);
            bool wantsToAct = stock < p.Min || (stock > p.Max && w.Households.Any(h => h.AssignedByPolicyId == p.Id));
            if (overload > 0 && wantsToAct
                && w.Rng.Get(RngStreams.Admin).NextInt(Permille.One) < overload * bal.AdminOverloadErrorPermille)
            {
                Blocked(w, p, "overloaded", ResName(w, p), Units(stock), w.AdminUsed.ToString(), w.AdminCapacity.ToString());
                continue;
            }
            if (stock < p.Min) Recruit(w, p, stock, cal);
            else if (stock > p.Max) Release(w, p, stock);
            else p.LastBlockedReason = "";
        }
    }

    private static string ResName(World w, Policy p) => w.Content.Resources[p.Resource].Name;

    /// <summary>Whole units for the account book (players read 1890, not 1890.948).</summary>
    internal static string Units(Qty q) => q.WholeUnits.ToString(System.Globalization.CultureInfo.InvariantCulture);

    private static void Recruit(World w, Policy p, Qty stock, in Calendar cal)
    {
        string res = ResName(w, p), st = Units(stock), min = Units(p.Min);
        int mine = w.Households.Count(h => h.AssignedBy == AssignmentSource.Policy && h.AssignedByPolicyId == p.Id);
        if (mine >= p.Def.MaxHouseholds)
        {
            Blocked(w, p, "blocked_max", res, st, min, p.Def.MaxHouseholds.ToString());
            return;
        }

        var target = FindProducer(w, p, cal.Season);
        if (target is null)
        {
            Blocked(w, p, "blocked_no_slot", res, st, min);
            return;
        }

        var candidate = FindCandidate(w, p);
        if (candidate is null)
        {
            Blocked(w, p, "blocked_no_household", res, st, min);
            return;
        }

        var (building, recipe) = target.Value;
        if (building.Recipe != recipe)
        {
            w.ChangeRecipe(building, recipe);
            w.LogPolicy(p, "recipe_changed", w.DescribeBuilding(building), recipe.Name);
        }
        var from = w.GetBuilding(candidate.JobBuildingId);
        string fromText = from is null ? "" : $" (saiu de {w.DescribeBuilding(from)})";
        w.Assign(candidate, building, AssignmentSource.Policy, p.Id);
        p.LastBlockedReason = "";
        w.LogPolicy(p, "recruited", candidate.Name, w.DescribeBuilding(building), fromText, res, st, min);
    }

    private static void Release(World w, Policy p, Qty stock)
    {
        var h = w.Households.LastOrDefault(h => h.AssignedBy == AssignmentSource.Policy && h.AssignedByPolicyId == p.Id);
        if (h is null) return;
        var b = w.GetBuilding(h.JobBuildingId)!;
        w.Unassign(h);
        w.LogPolicy(p, "released", h.Name, w.DescribeBuilding(b), ResName(w, p), Units(stock), Units(p.Max));
    }

    private static void Blocked(World w, Policy p, string reasonKey, params string[] args)
    {
        // Same reason as last time → stay quiet (no daily spam while the stock keeps changing).
        if (p.LastBlockedReason == reasonKey) return;
        p.LastBlockedReason = reasonKey;
        w.LogPolicy(p, reasonKey, args);
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

    /// <summary>Unemployed first, then unowned workers, then households of other decrees that are inside their band.</summary>
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
            return other is null || !other.Enabled || w.StorageStockIncludingTransit(other.Resource) >= other.Min;
        });
    }
}
