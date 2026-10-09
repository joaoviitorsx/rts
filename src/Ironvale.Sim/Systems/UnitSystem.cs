namespace Ironvale.Sim.Systems;

/// <summary>
/// Units under direct control (GDD v0.3 §4, briefing step 2), every tick, in id order: walk, work, carry the load to
/// the nearest storage and come back. Everything is physical: what is gathered lives in the unit's hands (or on the
/// ground) until it is unloaded in a storage. After a node is used up a colonist only looks for the same kind within
/// balance.autoContinueCells; with nothing there it waits with a "?" (the friction the first delegation removes).
/// </summary>
public sealed class UnitSystem : ISimSystem
{
    public string Name => "units";
    public Phase Phase => Phase.Transport;
    public Frequency Frequency => Frequency.Tick;

    public void Run(World w, in Calendar cal)
    {
        if (w.Units.Count == 0) return;
        foreach (var u in w.Units.ToArray())
        {
            if (!w.Units.Contains(u)) continue;
            Update(w, u);
        }
    }

    private static void Update(World w, Unit u)
    {
        if (u.Step == UnitStep.Fleeing)
        {
            var refuge = w.GetBuilding(u.HelperId);
            if (refuge is null || w.WalkToward(u, refuge.Center, refuge.Id) != World.Walk.Walking) Finish(u);
            return;
        }
        if (u.Order is null)
        {
            if (u.Queue.Count > 0)
            {
                var next = u.Queue[0];
                u.Queue.RemoveAt(0);
                Start(u, next);
            }
            else
            {
                if (u.IsMoving) w.WalkToward(u, u.Next);   // finish the step under way
                return;
            }
        }
        if (u.Step == UnitStep.Delivering)
        {
            Deliver(w, u);
            return;
        }

        var order = u.Order!.Value;
        switch (order.Kind)
        {
            case OrderKind.Move:
                if (w.WalkToward(u, order.Cell) != World.Walk.Walking) Finish(u);
                break;
            case OrderKind.Gather: Gather(w, u, order); break;
            case OrderKind.Pickup: Pickup(w, u, order); break;
            case OrderKind.Deposit:
                if (!u.IsCarrying) Finish(u);
                else
                {
                    u.HelperId = order.TargetId;
                    u.Step = UnitStep.Delivering;
                }
                break;
            case OrderKind.Hunt: Hunt(w, u, order); break;
            case OrderKind.Build: Build(w, u, order); break;
            case OrderKind.Scare: Scare(w, u, order); break;
            case OrderKind.Split: Split(w, u, order); break;
        }
    }

    internal static void Start(Unit u, UnitOrder order)
    {
        u.Order = order;
        u.Step = UnitStep.Going;
        u.WorkTicks = 0;
        u.HelperId = 0;
        u.Confused = false;
        u.ContinueKind = NodeKind.None;
    }

    internal static void Finish(Unit u)
    {
        u.Order = null;
        u.Step = UnitStep.Idle;
        u.WorkTicks = 0;
        u.HelperId = 0;
    }

    /// <summary>Order done: keep working nearby (auto-continue) or wait confused.</summary>
    private static void Continue(World w, Unit u)
    {
        var kind = u.ContinueKind;
        if (kind != NodeKind.None && u.Queue.Count == 0
            && w.NearestNode(kind, u.LastNode, w.Content.Balance.AutoContinueCells, u) is { } next)
        {
            Start(u, new UnitOrder(OrderKind.Gather, next, 0));
            return;
        }
        bool confused = kind != NodeKind.None && u.Queue.Count == 0;
        Finish(u);
        u.Confused = confused;
    }

    // ------------------------------------------------------------ carrying the load home

    private static void Deliver(World w, Unit u)
    {
        if (!u.IsCarrying)
        {
            u.Step = UnitStep.Going;
            return;
        }
        var store = w.GetBuilding(u.HelperId);
        if (store is null || !store.IsActive || !store.IsStorage || !store.Stock.Space.IsPositive)
        {
            store = w.StorageFor(u);
            if (store is null)
            {
                w.DropLoad(u);   // nowhere to put it: it stays on the ground, recoverable
                u.Step = UnitStep.Going;
                return;
            }
            u.HelperId = store.Id;
        }
        switch (w.WalkToward(u, store.Center, store.Id))
        {
            case World.Walk.Walking: return;
            case World.Walk.Blocked:
                w.DropLoad(u);
                u.Step = UnitStep.Going;
                return;
        }
        var added = store.Stock.AddUpTo(u.CarryResource, u.CarryAmount);
        u.CarryAmount -= added;
        if (u.CarryAmount.IsPositive)
        {
            u.HelperId = 0;   // full: the next storage
            return;
        }
        u.CarryResource = -1;
        u.HelperId = 0;
        u.Step = UnitStep.Going;
        if (u.Order is { Kind: OrderKind.Deposit }) Finish(u);
    }

    private static bool MustUnloadFor(World w, Unit u, int resource) =>
        u.IsCarrying && (u.CarryResource != resource || !w.RoomFor(u, resource).IsPositive);

    // ------------------------------------------------------------ gathering

    private static int ResourceOf(World w, NodeKind kind) => kind switch
    {
        NodeKind.Tree => w.Content.Resource("wood").Index,
        NodeKind.Stone => w.Content.Resource("stone").Index,
        _ => w.Content.Resource("food").Index,
    };

    private static void Gather(World w, Unit u, UnitOrder order)
    {
        var nature = w.Nature!;
        var node = nature.At(order.Cell);
        if (!w.Gatherable(order.Cell))
        {
            u.LastNode = order.Cell;
            if (u.ContinueKind == NodeKind.None && node.Kind != NodeKind.None) u.ContinueKind = node.Kind;
            Continue(w, u);
            return;
        }
        int r = ResourceOf(w, node.Kind);
        if (node.Kind != NodeKind.Tree && MustUnloadFor(w, u, r))
        {
            u.Step = UnitStep.Delivering;
            return;
        }
        if (u.Step == UnitStep.Going)
        {
            var walk = w.WalkToward(u, order.Cell);
            if (walk == World.Walk.Blocked) Finish(u);
            if (walk != World.Walk.Arrived) return;
            u.Step = UnitStep.Working;
            u.WorkTicks = 0;
        }
        var bal = w.Content.Balance;
        if (++u.WorkTicks < (node.Kind == NodeKind.Tree ? bal.ChopTicks : bal.GatherTicks)) return;
        u.WorkTicks = 0;
        u.LastNode = order.Cell;
        u.ContinueKind = node.Kind;
        int index = nature.Index(order.Cell);
        switch (node.Kind)
        {
            case NodeKind.Tree:
            {
                // Felled: a log on the ground (12 wood) the colonist carries off in pieces — or the ox drags whole.
                nature.Set(index, node with { Tick = w.Tick + (long)bal.TreeStumpDays * SimTime.TicksPerDay });
                var log = w.AddGroundItem(order.Cell, r, Qty.Units(bal.TreeWood));
                w.RecordProduced(r, Qty.Units(bal.TreeWood), economic: true);
                w.Emit(new TreeFelled(w.Tick, 0, order.Cell));
                var keep = u.ContinueKind;
                Start(u, new UnitOrder(OrderKind.Pickup, order.Cell, log.Id));
                u.ContinueKind = keep;
                u.LastNode = order.Cell;
                return;
            }
            case NodeKind.Stone:
            {
                var take = Qty.Min(Qty.Units(node.Amount), w.RoomFor(u, r));
                long left = node.Amount - take.WholeUnits;
                if (left > 0) nature.Set(index, node with { Amount = (int)left });
                else nature.Clear(order.Cell);
                w.Load(u, r, take);
                w.RecordProduced(r, take, economic: true);
                break;
            }
            default:
            {
                var food = Qty.Units(node.Kind == NodeKind.Bush ? bal.BushFood : bal.MushroomFood);
                var take = Qty.Min(food, w.RoomFor(u, r));
                nature.Set(index, node with { Tick = w.Tick });   // picked this season
                w.Load(u, r, take);
                w.RecordProduced(r, take, economic: true);
                break;
            }
        }
        u.Step = UnitStep.Delivering;
    }

    private static void Pickup(World w, Unit u, UnitOrder order)
    {
        var item = w.GetGroundItem(order.TargetId);
        if (item is null)
        {
            if (u.IsCarrying)
            {
                u.Step = UnitStep.Delivering;
                return;
            }
            Continue(w, u);
            return;
        }
        if (!w.CarryCapacity(u, item.Resource).IsPositive)
        {
            Finish(u);
            return;
        }
        if (MustUnloadFor(w, u, item.Resource))
        {
            u.Step = UnitStep.Delivering;
            return;
        }
        if (u.Step == UnitStep.Going)
        {
            var walk = w.WalkToward(u, item.Cell);
            if (walk == World.Walk.Blocked) Finish(u);
            if (walk != World.Walk.Arrived) return;
            u.Step = UnitStep.Working;
            u.WorkTicks = 0;
        }
        if (++u.WorkTicks < w.Content.Balance.HandleTicks) return;
        var take = Qty.Min(item.Amount, w.RoomFor(u, item.Resource));
        item.Amount -= take;
        if (!item.Amount.IsPositive) w.RemoveGroundItem(item);
        w.Load(u, item.Resource, take);
        u.WorkTicks = 0;
        u.Step = UnitStep.Delivering;
    }

    // ------------------------------------------------------------ hunting

    private static void Hunt(World w, Unit u, UnitOrder order)
    {
        var prey = w.GetAnimal(order.TargetId);
        if (prey is null || !prey.Huntable)
        {
            Finish(u);
            return;
        }
        if (u.IsCarrying)
        {
            u.Step = UnitStep.Delivering;
            return;
        }
        var bal = w.Content.Balance;
        if (u.Step == UnitStep.Going)
        {
            if (World.Chebyshev(u.Pos, prey.Pos) > bal.HuntRangeCells || u.IsMoving)
            {
                if (w.ChaseToward(u, prey.Pos) == World.Walk.Blocked) Finish(u);
                return;
            }
            u.Step = UnitStep.Working;
            u.WorkTicks = 0;
        }
        if (World.Chebyshev(u.Pos, prey.Pos) > bal.HuntRangeCells)
        {
            u.Step = UnitStep.Going;   // it ran out of range: chase again
            return;
        }
        if (++u.WorkTicks < bal.HuntShotTicks) return;
        u.WorkTicks = 0;
        if (w.Rng.Get(RngStreams.Hunt).NextInt(Permille.One) >= bal.HuntHitPermille)
        {
            FaunaSystem.Startle(w, prey, u);   // missed: it bolts
            u.Step = UnitStep.Going;
            return;
        }
        // Hit: the carcass stays on the ground; the hunter carries the meat first, then the hides.
        int food = w.Content.Resource("food").Index, hides = w.Content.Resource("hides").Index;
        bool deer = prey.Kind == FaunaKind.Deer;
        var meat = w.AddGroundItem(prey.Pos, food, Qty.Units(deer ? bal.DeerFood : bal.RabbitFood));
        w.RecordProduced(food, Qty.Units(deer ? bal.DeerFood : bal.RabbitFood), economic: true);
        GroundItem? skin = null;
        if (deer && bal.DeerHides > 0)
        {
            skin = w.AddGroundItem(prey.Pos, hides, Qty.Units(bal.DeerHides));
            w.RecordProduced(hides, Qty.Units(bal.DeerHides), economic: true);
        }
        w.Emit(new AnimalKilled(w.Tick, prey.Id, u.Id, prey.Pos));
        w.RemoveAnimal(prey);
        Start(u, new UnitOrder(OrderKind.Pickup, meat.Cell, meat.Id));
        if (skin is not null) u.Queue.Insert(0, new UnitOrder(OrderKind.Pickup, skin.Cell, skin.Id));
    }

    // ------------------------------------------------------------ building

    private static void Build(World w, Unit u, UnitOrder order)
    {
        var site = w.GetBuilding(order.TargetId);
        if (site is null || site.IsActive)
        {
            if (u.IsCarrying) u.Step = UnitStep.Delivering;
            else Finish(u);
            return;
        }
        // Bringing material: unload it on the site.
        if (u.IsCarrying)
        {
            int r = u.CarryResource;
            // Room for this load: the cost minus what is on site and what carriers have reserved (not other builders:
            // whoever arrives first unloads).
            var room = Qty.Max(Qty.Zero, site.Def.Cost[r] - site.Stock.Get(r) - w.CarrierIncoming(site, r));
            if (!room.IsPositive)
            {
                u.Step = UnitStep.Delivering;   // not needed here: back to storage
                return;
            }
            var walk = w.WalkToward(u, site.Center, site.Id);
            if (walk == World.Walk.Blocked) u.Step = UnitStep.Delivering;
            if (walk != World.Walk.Arrived) return;
            var added = site.Stock.AddUpTo(r, Qty.Min(u.CarryAmount, room));
            u.CarryAmount -= added;
            if (!u.CarryAmount.IsPositive) u.CarryResource = -1;
            else u.Step = UnitStep.Delivering;
            return;
        }
        if (site.ClearWorkMilli > 0 || site.CanProgress)
        {
            if (u.Step != UnitStep.Working)
            {
                var walk = w.WalkToward(u, site.Center, site.Id);
                if (walk == World.Walk.Blocked) Finish(u);
                if (walk != World.Walk.Arrived) return;
                u.Step = UnitStep.Working;   // ConstructionSystem counts the work hourly
            }
            u.Confused = false;
            return;
        }
        // Materials missing on the site: fetch one from storage (no carriers in the opening).
        u.Step = UnitStep.Fetching;
        if (u.HelperId == 0 || w.GetBuilding(u.HelperId) is not { } from || !NeededFrom(w, site, from, out _))
        {
            var source = w.StoragesByDistance(site.Center).FirstOrDefault(b => NeededFrom(w, site, b, out _));
            if (source is null)
            {
                u.Confused = true;   // waits at the site for material
                u.Step = UnitStep.Going;
                return;
            }
            u.HelperId = source.Id;
            from = source;
        }
        u.Confused = false;
        var go = w.WalkToward(u, from.Center, from.Id);
        if (go == World.Walk.Blocked) Finish(u);
        if (go != World.Walk.Arrived) return;
        NeededFrom(w, site, from, out int res);
        var take = Qty.Min(Qty.Min(w.SiteNeed(site, res), from.Stock.Free(res)), w.CarryCapacity(u, res));
        var removed = from.Stock.RemoveUpTo(res, take);
        w.Load(u, res, removed);
        u.HelperId = 0;
        u.Step = UnitStep.Going;
    }

    /// <summary>First material the site still needs that <paramref name="store"/> has unreserved.</summary>
    private static bool NeededFrom(World w, Building site, Building store, out int resource)
    {
        resource = -1;
        if (!store.IsActive || !store.IsStorage) return false;
        for (int r = 0; r < w.Content.ResourceCount; r++)
        {
            if (!w.SiteNeed(site, r).IsPositive || !store.Stock.Free(r).IsPositive) continue;
            resource = r;
            return true;
        }
        return false;
    }

    // ------------------------------------------------------------ wolves, firewood

    private static void Scare(World w, Unit u, UnitOrder order)
    {
        var wolf = w.GetAnimal(order.TargetId);
        if (wolf is null || wolf.State == AnimalState.Retreating)
        {
            Finish(u);
            return;
        }
        if (World.Chebyshev(u.Pos, wolf.Pos) > 3 || u.IsMoving)
        {
            if (w.ChaseToward(u, wolf.Pos) == World.Walk.Blocked) Finish(u);
            return;
        }
        if (w.ColonistsNear(wolf.Pos, 4, w.Content.Balance.WolfScareGroup))
        {
            FaunaSystem.Retreat(w, wolf);
            Finish(u);
        }
    }

    private static void Split(World w, Unit u, UnitOrder order)
    {
        var store = w.GetBuilding(order.TargetId);
        if (store is null || !store.IsActive || !store.IsStorage)
        {
            Finish(u);
            return;
        }
        if (u.IsCarrying)
        {
            u.Step = UnitStep.Delivering;
            return;
        }
        if (u.Step == UnitStep.Going)
        {
            var walk = w.WalkToward(u, store.Center, store.Id);
            if (walk == World.Walk.Blocked) Finish(u);
            if (walk != World.Walk.Arrived) return;
            u.Step = UnitStep.Working;
            u.WorkTicks = 0;
        }
        if (++u.WorkTicks < w.Content.Balance.SplitTicksPerUnit) return;
        u.WorkTicks = 0;
        int wood = w.Content.Resource("wood").Index, firewood = w.Content.Resource("firewood").Index;
        if (store.Stock.Free(wood) < Qty.Units(1))
        {
            Finish(u);
            u.Confused = true;
            return;
        }
        // Same total: a log becomes a load of firewood in place.
        store.Stock.RemoveUpTo(wood, Qty.Units(1));
        store.Stock.AddUpTo(firewood, Qty.Units(1));
        w.RecordConsumed(wood, Qty.Units(1), fromStorage: true);
        w.RecordProduced(firewood, Qty.Units(1), economic: true);
    }

    /// <summary>A wolf got close to a lone colonist: the load falls, the colonist runs to the nearest storage.</summary>
    internal static void Scared(World w, Unit u, Animal wolf)
    {
        w.DropLoad(u);
        u.Queue.Clear();
        Finish(u);
        u.Step = UnitStep.Fleeing;
        u.HelperId = w.StoragesByDistance(u.Pos).FirstOrDefault()?.Id ?? 0;
        u.Next = u.Pos;
        u.StepTicks = 0;
        w.Emit(new UnitScared(w.Tick, u.Id, wolf.Id));
    }
}
