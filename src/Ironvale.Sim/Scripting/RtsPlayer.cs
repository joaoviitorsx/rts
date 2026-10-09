using Ironvale.Sim.Buildings;
using Ironvale.Sim.Commands;
using Ironvale.Sim.Content;
using Ironvale.Sim.Map;
using Ironvale.Sim.Population;
using Ironvale.Sim.Time;

namespace Ironvale.Sim.Scripting;

/// <summary>
/// The RTS opening played by script (GDD v0.3 §3, briefing step 3 gate; base of the step-5 balance players): the band
/// gathers what the next building needs, builds campfire → depot → houses → woodcutter → tents…, hunts when food runs
/// low, splits firewood before the cold, the ox drags the felled logs, and each new family takes the next ladder-1 job
/// (hunting camp, field, a carrier, woodcutter, gatherer). Acts only through commands, once a day — like a decent newcomer
/// who reads the objective card, not a speedrunner.
/// </summary>
public sealed class RtsPlayer : IScriptedPlayer
{
    private static readonly string[] BuildOrder =
    {
        "campfire", "depot", "house", "hunting_camp", "house", "field", "house", "woodcutter", "house", "gatherer", "house", "tent",
    };

    private int _next;

    public string Id => "rts";

    public void Start(World w) => Daily(w);

    public void Daily(World w)
    {
        if (w.Terrain is not { } t) return;
        var bal = w.Content.Balance;
        int wood = w.Content.Resource("wood").Index, stone = w.Content.Resource("stone").Index;
        int hides = w.Content.Resource("hides").Index, food = w.Content.Resource("food").Index;
        int firewood = w.Content.Resource("firewood").Index;

        PlaceNext(w, t);
        AssignFamilies(w);

        // Roles for the band this moment, most urgent first; each colonist (by id) gets the next role. Orders change
        // only when a colonist's current work is not its role (and never while it carries something).
        var cal = w.Calendar;
        var start = t.Start;
        var probe = w.Units.FirstOrDefault(u => u.IsColonist) ?? w.Units[0];
        int mouths = w.Units.Count(u => u.IsColonist && u.Controllable) + w.Households.Sum(h => h.Members);
        long foodDays = w.StorageStock(food).Milli / Math.Max(1, bal.FoodPerMemberPerDay.Milli * mouths);
        bool coldSoon = cal.Season == Season.Autumn || cal.Season == Season.Winter
                        || (cal.Season == Season.Summer && cal.MonthOfYear % SimTime.MonthsPerSeason == SimTime.MonthsPerSeason - 1);
        var store = w.Buildings.Where(b => b.IsActive && b.IsStorage).OrderBy(b => b.Def.Uncovered ? 1 : 0).ThenBy(b => b.Id).FirstOrDefault();
        long firewoodTarget = 120 + 40L * w.Households.Count;
        var site = w.Buildings.FirstOrDefault(b => !b.IsActive);
        // Prey: the nearest deer (meat and hides), or a rabbit when it is much closer.
        var deer = w.Animals.Where(a => a.Huntable && a.Pos.Manhattan(start) < 70)
            .OrderBy(a => a.Pos.Manhattan(start) * (a.Kind == FaunaKind.Deer ? 1 : 3)).FirstOrDefault();

        var roles = new List<OrderKind?>();
        bool stoneShort = NeedsOf(w, stone) > 0;
        if (stoneShort) roles.AddRange(new OrderKind?[] { null, null });   // null = stone: the site waits for it
        bool materialsReady = site is not null && Enumerable.Range(0, site.Def.Cost.Length).All(r =>
            site.Stock.Get(r) + w.StorageStock(r) >= site.Def.Cost[r]);
        if (site is not null && materialsReady) roles.AddRange(new OrderKind?[] { OrderKind.Build, OrderKind.Build });
        bool hungry = foodDays < 40, needHides = NeedsOf(w, hides) > 0;
        int hunters = foodDays < 12 ? 6 : foodDays < 25 ? 4 : hungry || needHides ? 2 : 0;
        if (deer is not null) roles.AddRange(Enumerable.Repeat<OrderKind?>(OrderKind.Hunt, hunters));
        if (coldSoon && store is not null && w.StorageStock(firewood).WholeUnits < firewoodTarget && w.StorageStock(wood).WholeUnits >= 6)
            roles.AddRange(new OrderKind?[] { OrderKind.Split, OrderKind.Split });
        bool woodShort = w.StorageStock(wood).WholeUnits < 40 + NeedsOf(w, wood);

        foreach (var u in w.Units.Where(u => u.IsColonist && u.Controllable).OrderBy(u => u.Id))
        {
            if (u.IsCarrying || u.Order is { Kind: OrderKind.Pickup } && !u.Confused) continue;   // finish hauling (meat, logs)
            OrderKind? role = roles.Count > 0 ? roles[0] : OrderKind.Gather;
            bool stoneRole = roles.Count > 0 && roles[0] is null;
            if (stoneRole) role = null;
            if (roles.Count > 0) roles.RemoveAt(0);
            var current = u.Order?.Kind;
            switch (role)
            {
                case OrderKind.Build when site is not null:
                    if (current != OrderKind.Build || u.Order!.Value.TargetId != site.Id) w.Enqueue(new OrderUnits(new[] { u.Id }, OrderKind.Build, site.Center, site.Id));
                    break;
                case OrderKind.Hunt when deer is not null:
                    if (current != OrderKind.Hunt && current != OrderKind.Pickup || u.Confused) w.Enqueue(new OrderUnits(new[] { u.Id }, OrderKind.Hunt, deer.Pos, deer.Id));
                    break;
                case OrderKind.Split when store is not null:
                    if (current != OrderKind.Split) w.Enqueue(new OrderUnits(new[] { u.Id }, OrderKind.Split, store.Center, store.Id));
                    break;
                case null when stoneRole:
                    if ((current != OrderKind.Gather || u.Confused) && w.NearestNode(NodeKind.Stone, start, 40, u) is { } rock)
                        w.Enqueue(new OrderUnits(new[] { u.Id }, OrderKind.Gather, rock));
                    break;
                default:
                    // Wood (trees and the logs they leave) — or, with wood to spare or food short, berries in season.
                    if ((!woodShort || hungry) && cal.Season is Season.Summer or Season.Autumn && w.NearestNode(NodeKind.Bush, start, 30, u) is { } bush)
                    {
                        if (current != OrderKind.Gather || u.Confused) w.Enqueue(new OrderUnits(new[] { u.Id }, OrderKind.Gather, bush));
                    }
                    else if ((current is not (OrderKind.Gather or OrderKind.Pickup) || u.Confused)
                             && w.NearestNode(NodeKind.Tree, start, 40, u) is { } tree)
                        w.Enqueue(new OrderUnits(new[] { u.Id }, OrderKind.Gather, tree));
                    break;
            }
        }

        // Firewood from the woodcutter (2A recipe switch): from late summer until the target, then back to logs.
        bool wantFirewood = coldSoon && w.StorageStock(firewood).WholeUnits < firewoodTarget;
        foreach (var cutter in w.Buildings.Where(b => b.IsActive && b.Def.Id == "woodcutter"))
        {
            string recipe = wantFirewood ? "split_firewood" : "chop_wood";
            if (cutter.Recipe?.Id != recipe) w.Enqueue(new SetRecipe(cutter.Id, recipe));
        }

        // 5. The ox drags felled logs home.
        if (w.Units.FirstOrDefault(u => u.Kind == UnitKind.Ox && (u.Order is null || u.Confused)) is { } ox
            && w.GroundItems.Where(g => g.Resource == wood).OrderBy(g => g.Cell.Manhattan(ox.Pos)).FirstOrDefault() is { } log)
            w.Enqueue(new OrderUnits(new[] { ox.Id }, OrderKind.Pickup, log.Cell, log.Id));
    }

    /// <summary>One site at a time, in <see cref="BuildOrder"/>; a tent waits for its hides.</summary>
    private void PlaceNext(World w, Terrain t)
    {
        if (_next >= BuildOrder.Length || w.Buildings.Any(b => !b.IsActive)) return;
        var def = w.Content.Building(BuildOrder[_next]);
        for (int r = 0; r < def.Cost.Length; r++)
            if (def.Cost[r].IsPositive && w.Content.Resources[r].Id == "hides" && w.StorageStock(r) < def.Cost[r]) return;
        if (Placement.Find(w, def.Id, Placement.FlatHallCenter) is { } spot)
        {
            w.Enqueue(new PlaceBuilding(def.Id, spot, 0));
            w.ApplyPendingCommands();
        }
        _next++;
    }

    /// <summary>What the open site (or the next planned building) still lacks of a resource, in whole units.</summary>
    private long NeedsOf(World w, int resource)
    {
        var site = w.Buildings.FirstOrDefault(b => !b.IsActive);
        var def = site?.Def ?? (_next < BuildOrder.Length ? w.Content.Building(BuildOrder[_next]) : null);
        if (def is null) return 0;
        long have = w.StorageStock(resource).WholeUnits + (site?.Stock.Get(resource).WholeUnits ?? 0);
        return Math.Max(0, def.Cost[resource].WholeUnits - have);
    }

    /// <summary>Families take the ladder-1 jobs in order: woodcutter, one carrier, gatherer, hunting camp, then any free slot.</summary>
    private static void AssignFamilies(World w)
    {
        foreach (var h in w.Households.Where(h => !h.HasJob).OrderBy(h => h.Id).ToList())
        {
            bool carrier = w.Buildings.Any(b => b.IsActive && b.IsStorage && b.AssignedCount > 0);
            // One family per kind of work, food first: hunting camp, field, a carrier, woodcutter, gatherer; then a
            // second family where food comes from. Idle families help on building sites meanwhile.
            var job = Empty(w, "hunting_camp") ?? Empty(w, "field")
                      ?? (carrier ? null : w.Buildings.Where(b => b.IsActive && b.IsStorage && b.FreeSlotIndex() >= 0)
                          .OrderBy(b => b.Def.Uncovered ? 1 : 0).ThenBy(b => b.Id).FirstOrDefault())
                      ?? Empty(w, "woodcutter") ?? Empty(w, "gatherer")
                      ?? Free(w, "field") ?? Free(w, "gatherer") ?? Free(w, "hunting_camp");
            if (job is null) return;
            w.Enqueue(new AssignHousehold(h.Id, job.Id));
            w.ApplyPendingCommands();
        }
    }

    private static Building? Empty(World w, string defId) =>
        w.Buildings.Where(b => b.IsActive && b.Def.Id == defId && b.AssignedCount == 0).OrderBy(b => b.Id).FirstOrDefault();

    private static Building? Free(World w, string defId) =>
        w.Buildings.Where(b => b.IsActive && b.Def.Id == defId && b.FreeSlotIndex() >= 0).OrderBy(b => b.Id).FirstOrDefault();
}
