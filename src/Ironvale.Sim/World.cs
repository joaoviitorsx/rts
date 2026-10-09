using Ironvale.Sim.Systems;

namespace Ironvale.Sim;

/// <summary>
/// Root of the simulation state. Pure C#: no Godot, no threads, no wall clock.
/// The view reads it through public getters (setters are internal) and changes it only via <see cref="Enqueue"/>.
/// </summary>
public sealed class World
{
    private readonly List<Household> _households = new();
    private readonly List<Building> _buildings = new();
    private readonly List<Carrier> _carriers = new();
    private readonly List<Shipment> _shipments = new();
    private readonly List<Policy> _policies = new();
    private readonly List<PolicyLogEntry> _policyLog = new();
    private readonly Dictionary<int, Household> _householdById = new();
    private readonly Dictionary<int, Building> _buildingById = new();
    private readonly Dictionary<int, Shipment> _shipmentById = new();
    private readonly List<SimCommand> _pending = new();
    private readonly List<SimEvent> _events = new();
    private readonly Scheduler _scheduler;

    public ContentDb Content { get; }
    public ulong Seed { get; }
    public string ScenarioId { get; }
    public long Tick { get; internal set; }
    public long StartTick { get; }
    public GridMap Map { get; }
    /// <summary>Routes and walking times (cached distance fields; not part of the saved state).</summary>
    public Pathfinder Paths { get; }
    public RngStreams Rng { get; }
    public Ledger Ledger { get; }
    public TelemetryRecorder Telemetry { get; }
    public int NextId { get; internal set; } = 1;

    /// <summary>Events are only buffered when a consumer (the view) drains them.</summary>
    public bool CollectEvents { get; set; }

    public IReadOnlyList<Household> Households => _households;
    public IReadOnlyList<Building> Buildings => _buildings;
    public IReadOnlyList<Carrier> Carriers => _carriers;
    public IReadOnlyList<Shipment> Shipments => _shipments;
    public IReadOnlyList<Policy> Policies => _policies;
    public IReadOnlyList<PolicyLogEntry> PolicyLog => _policyLog;
    public IReadOnlyList<ISimSystem> Systems => _scheduler.Systems;

    public Calendar Calendar => new(Tick);
    public long ElapsedTicks => Tick - StartTick;
    public bool IsCollapsed => _households.Count == 0;

    internal World(ContentDb content, ulong seed, string scenarioId, int mapW, int mapH, long startTick)
    {
        Content = content;
        Seed = seed;
        ScenarioId = scenarioId;
        StartTick = startTick;
        Tick = startTick;
        Map = new GridMap(mapW, mapH);
        Paths = new Pathfinder(Map, content.Balance);
        Rng = new RngStreams(seed);
        Ledger = new Ledger(content.ResourceCount);
        Telemetry = new TelemetryRecorder(content.ResourceCount);
        _scheduler = CreateScheduler();
    }

    /// <summary>Fixed system order. Changing it changes results (and should bump the save version).</summary>
    private static Scheduler CreateScheduler()
    {
        var s = new Scheduler();
        s.Register(new HouseholdStateSystem());   // Production, Hourly (first: who works this hour)
        s.Register(new ConstructionSystem());     // Production, Daily
        s.Register(new HarvestSystem());          // Production, Seasonal
        s.Register(new ProductionSystem());       // Production, Hourly
        s.Register(new TransportSystem());        // Transport, Tick
        s.Register(new ConsumptionSystem());      // Consumption, Daily (+ subsistence)
        s.Register(new ToolWearSystem());         // Consumption, Daily
        s.Register(new NeedsSystem());            // Needs, Daily
        s.Register(new PolicySystem());           // Decisions, Daily
        return s;
    }

    public static World Create(ContentDb content, ScenarioDef scenario, ulong seed)
    {
        var w = new World(content, seed, scenario.Id, scenario.MapWidth, scenario.MapHeight,
            (long)scenario.StartMonth * SimTime.TicksPerMonth);

        foreach (var sb in scenario.Buildings)
        {
            if (!w.Map.CanPlace(sb.Def, sb.Origin, sb.Rotation))
                throw new ContentException($"scenario {scenario.Id}: cannot place {sb.Def.Id} at {sb.Origin}");
            w.AddBuilding(sb.Def, sb.Origin, sb.Rotation, active: true);
        }

        var firstStorage = w._buildings.FirstOrDefault(b => b.IsStorage)
            ?? throw new ContentException($"scenario {scenario.Id}: needs a storage building for the initial stock");
        for (int r = 0; r < scenario.Stock.Length; r++)
        {
            var added = firstStorage.Stock.AddUpTo(r, scenario.Stock[r]);
            if (added != scenario.Stock[r])
                throw new ContentException($"scenario {scenario.Id}: initial stock exceeds {firstStorage.Def.Id} capacity");
            w.Ledger.Initial[r] += added.Milli;
        }

        foreach (var sh in scenario.Households)
        {
            w.AddHousehold(new Household
            {
                Id = w.NewId(),
                Name = sh.Name,
                Members = sh.Members,
                Workers = sh.Workers,
                ToolCondition = scenario.HouseholdToolCondition,
            });
        }
        return w;
    }

    // ---------------------------------------------------------------- stepping

    public void Enqueue(SimCommand command) => _pending.Add(command);

    /// <summary>
    /// Applies queued commands now, at the current tick, without advancing time. Same result as letting the next
    /// <see cref="Step"/> apply them (it would, before this tick's systems), so the view can answer instantly even
    /// while paused (game feel: response &lt; 100 ms).
    /// </summary>
    public void ApplyPendingCommands() => ApplyCommands();

    public void Step()
    {
        ApplyCommands();
        var cal = new Calendar(Tick);
        _scheduler.RunStep(this, cal);
        if (cal.IsDayEnd) Telemetry.CloseDay(this, cal);
        Tick++;
    }

    public void StepTicks(long ticks)
    {
        for (long i = 0; i < ticks; i++) Step();
    }

    public void StepDays(int days) => StepTicks((long)days * SimTime.TicksPerDay);

    public void StepYears(int years) => StepTicks((long)years * SimTime.TicksPerYear);

    private void ApplyCommands()
    {
        if (_pending.Count == 0) return;
        var batch = _pending.ToArray();
        _pending.Clear();
        foreach (var cmd in batch)
        {
            string? error = cmd.Apply(this);
            if (error is not null) Emit(new CommandRejected(Tick, cmd.GetType().Name, error));
        }
    }

    public List<SimEvent> DrainEvents()
    {
        var copy = new List<SimEvent>(_events);
        _events.Clear();
        return copy;
    }

    internal void Emit(SimEvent e)
    {
        if (CollectEvents) _events.Add(e);
    }

    // ---------------------------------------------------------------- lookups

    public Household? GetHousehold(int id) => _householdById.GetValueOrDefault(id);
    public Building? GetBuilding(int id) => _buildingById.GetValueOrDefault(id);
    public Shipment? GetShipment(int id) => _shipmentById.GetValueOrDefault(id);
    public Policy? GetPolicy(int id) => _policies.FirstOrDefault(p => p.Id == id);
    public Carrier? CarrierOf(int householdId) => _carriers.FirstOrDefault(c => c.HouseholdId == householdId);

    public Building? SeatBuilding => _buildings.FirstOrDefault(b => b.Def.Has(BuildingRole.Seat));

    /// <summary>Total stock of a resource in active storage buildings (including reserved).</summary>
    public Qty StorageStock(int r)
    {
        long sum = 0;
        foreach (var b in _buildings)
            if (b.IsActive && b.IsStorage) sum += b.Stock.Get(r).Milli;
        return new Qty(sum);
    }

    /// <summary>Unreserved stock of a resource in active storage buildings.</summary>
    public Qty StorageFree(int r)
    {
        long sum = 0;
        foreach (var b in _buildings)
            if (b.IsActive && b.IsStorage) sum += b.Stock.Free(r).Milli;
        return new Qty(sum);
    }

    /// <summary>Stock in storage plus what carriers are bringing to storage right now (not to sites).</summary>
    public Qty StorageStockIncludingTransit(int r)
    {
        long sum = StorageStock(r).Milli;
        foreach (var s in _shipments)
            if (s.Resource == r && GetBuilding(s.ToId) is { IsStorage: true }) sum += s.Amount.Milli;
        return new Qty(sum);
    }

    /// <summary>Resource <paramref name="r"/> carriers are bringing (or about to bring) to a building (site or input buffer).</summary>
    public Qty SiteIncoming(Building site, int r)
    {
        long sum = 0;
        foreach (var c in _carriers)
            if (c.DropoffId == site.Id && c.Resource == r && c.Phase is not (CarrierPhase.Idle or CarrierPhase.Returning))
                sum += c.Amount.Milli;
        return new Qty(sum);
    }

    /// <summary>Material still to be sent to a site: cost − on site − on the way.</summary>
    public Qty SiteNeed(Building site, int r) =>
        site.IsActive ? Qty.Zero : Qty.Max(Qty.Zero, site.Def.Cost[r] - site.Stock.Get(r) - SiteIncoming(site, r));

    /// <summary>
    /// What carriers should still bring to <paramref name="b"/>: site materials, or recipe inputs (2A.4) — those only
    /// once the buffer (with what is on the way) drops below half its target, then topped up to the target, so
    /// carriers don't spend every trip on trickles and still haul the output away.
    /// </summary>
    public Qty DeliveryNeed(Building b, int r)
    {
        if (!b.IsActive) return SiteNeed(b, r);
        var target = b.InputTarget(r);
        var have = b.InputStock.Get(r) + SiteIncoming(b, r);
        return have.Milli * 2 >= target.Milli ? Qty.Zero : target - have;
    }

    /// <summary>Households working on a site this hour (assigned by the player or helping).</summary>
    public int BuildersAt(Building site)
    {
        int n = 0;
        foreach (var h in _households)
            if (h.BuildSiteId == site.Id || h.JobBuildingId == site.Id) n++;
        return n;
    }

    /// <summary>Ticks to walk into <paramref name="cell"/> on the way to <paramref name="target"/>.</summary>
    public int StepTicksInto(Cell cell, Cell target) => Paths.EnterCost(cell, Map.BuildingAt(target));

    /// <summary>Where the household works this hour (job building, or the site it helps), or null.</summary>
    public Building? WorkplaceOf(Household h) => h.State switch
    {
        HouseholdState.Working => GetBuilding(h.JobBuildingId),
        HouseholdState.Building => GetBuilding(h.BuildSiteId),
        _ => null,
    };

    private const int DayMilliTicks = SimTime.TicksPerDay * Permille.One;

    /// <summary>
    /// One-way commute home → workplace in milli-ticks of the day: walking ticks of the route × commuteTicksPermille,
    /// capped at half a day. Carriers walk as their job, so they have no separate commute (2A.3 simplification).
    /// </summary>
    public int CommuteMilliTicks(Household h)
    {
        if (h.State is not (HouseholdState.Working or HouseholdState.Building)) return 0;
        var work = WorkplaceOf(h);
        return work is null ? 0 : CommuteMilliTicks(HomeCellOf(h), work.Center);
    }

    public int CommuteMilliTicks(Cell home, Cell work) =>
        (int)Math.Min((long)Paths.Ticks(home, work) * Content.Balance.CommuteTicksPermille, DayMilliTicks / 2);

    /// <summary>
    /// Share (‰) of hour <paramref name="hourOfDay"/> the household spends at its workplace: the shift is
    /// [commute, day − commute) — walking there in the morning and back home before the day ends.
    /// </summary>
    public int OnSitePermille(Household h, int hourOfDay)
    {
        int c = CommuteMilliTicks(h);
        int start = hourOfDay * SimTime.TicksPerHour * Permille.One, end = start + SimTime.TicksPerHour * Permille.One;
        int overlap = Math.Min(end, DayMilliTicks - c) - Math.Max(start, c);
        return overlap <= 0 ? 0 : overlap / SimTime.TicksPerHour;
    }

    /// <summary>Free hours today (milli-hours): balance.gardenFreeHours minus the round-trip commute.</summary>
    public int FreeMilliHours(Household h) =>
        Math.Max(0, Content.Balance.GardenFreeHours * Permille.One - 2 * CommuteMilliTicks(h) / SimTime.TicksPerHour);

    /// <summary>Food the yard garden can give today (none without a house or in winter).</summary>
    public Qty GardenFood(Household h, in Calendar cal) =>
        h.HomeId == 0 || cal.IsWinter ? Qty.Zero
            : new Qty(Content.Balance.GardenFoodPerHour.Milli * FreeMilliHours(h) / Permille.One);

    /// <summary>Share (‰) of the shift spent walking (round trip).</summary>
    public int CommutePermille(Household h) => (int)(2L * CommuteMilliTicks(h) * Permille.One / DayMilliTicks);

    public Cell HomeCellOf(Household h) =>
        GetBuilding(h.HomeId)?.Center ?? SeatBuilding?.Center ?? new Cell(Map.Width / 2, Map.Height / 2);

    public string DescribeBuilding(Building b) => $"{b.Def.Name} #{b.Id}";

    // ---------------------------------------------------------------- mutations (sim-internal)

    internal int NewId() => NextId++;

    internal Building AddBuilding(BuildingDef def, Cell origin, int rotation, bool active)
    {
        var b = new Building
        {
            Id = NewId(),
            Def = def,
            Origin = origin,
            Rotation = rotation,
            IsActive = active,
            Stock = new Stockpile(Content.ResourceCount, active ? def.StockCapacity : def.TotalCost),
            InputStock = new Stockpile(Content.ResourceCount, def.InputCapacity),
            Slots = new int[def.JobSlots],
            Recipe = def.Recipes.Count > 0 ? def.Recipes[0] : null,
            RemainderMicro = new long[Content.ResourceCount],
        };
        InsertBuilding(b);
        Map.Fill(def, origin, rotation, b.Id);
        Emit(new BuildingPlaced(Tick, b.Id));
        return b;
    }

    internal void InsertBuilding(Building b)
    {
        _buildings.Add(b);
        _buildingById[b.Id] = b;
    }

    internal void RemoveBuilding(Building b)
    {
        foreach (var h in _households.Where(h => h.JobBuildingId == b.Id).ToList()) Unassign(h);
        foreach (var h in _households.Where(h => h.HomeId == b.Id)) h.HomeId = 0;
        _buildings.Remove(b);
        _buildingById.Remove(b.Id);
        Map.Fill(b.Def, b.Origin, b.Rotation, 0);
        Emit(new BuildingRemoved(Tick, b.Id));
    }

    internal void AddHousehold(Household h)
    {
        _households.Add(h);
        _householdById[h.Id] = h;
    }

    internal void RemoveHousehold(Household h, string reason)
    {
        if (h.HasJob) Unassign(h);
        _households.Remove(h);
        _householdById.Remove(h.Id);
        Emit(new HouseholdLeft(Tick, h.Id, h.Name, reason));
    }

    internal void Assign(Household h, Building b, AssignmentSource source, int policyId)
    {
        if (h.HasJob) Unassign(h);
        int slot = b.FreeSlotIndex();
        if (slot < 0) throw new InvalidOperationException($"{DescribeBuilding(b)} has no free slot");
        b.Slots[slot] = h.Id;
        h.JobBuildingId = b.Id;
        h.AssignedBy = source;
        h.AssignedByPolicyId = policyId;
        if (b.IsStorage)
        {
            var c = new Carrier
            {
                Id = NewId(),
                HouseholdId = h.Id,
                BaseId = b.Id,
                Phase = CarrierPhase.Idle,
                Pos = b.Center,
                Target = b.Center,
                NextCell = b.Center,
            };
            _carriers.Add(c);
        }
        HouseholdStateSystem.Refresh(this, h, Calendar);
        Emit(new HouseholdAssigned(Tick, h.Id, b.Id));
    }

    /// <summary>Player puts a household on a construction site (no job slot: sites have their own builder cap).</summary>
    internal void AssignBuilder(Household h, Building site)
    {
        if (h.HasJob) Unassign(h);
        h.JobBuildingId = site.Id;
        h.AssignedBy = AssignmentSource.Player;
        h.AssignedByPolicyId = 0;
        HouseholdStateSystem.Refresh(this, h, Calendar);
        Emit(new HouseholdAssigned(Tick, h.Id, site.Id));
    }

    /// <summary>Work done: materials on site are consumed, the stock becomes the building's own, builders go free.</summary>
    internal void CompleteConstruction(Building b)
    {
        for (int r = 0; r < Content.ResourceCount; r++)
        {
            var q = b.Stock.RemoveUpTo(r, b.Stock.Get(r));
            RecordConsumed(r, q, fromStorage: true);
        }
        b.Stock = new Stockpile(Content.ResourceCount, b.Def.StockCapacity);
        b.IsActive = true;
        foreach (var h in _households.ToArray())
        {
            if (h.JobBuildingId == b.Id) Unassign(h);
            else if (h.BuildSiteId == b.Id)
            {
                h.BuildSiteId = 0;
                h.State = HouseholdState.Subsisting;
            }
        }
        Emit(new BuildingCompleted(Tick, b.Id));
    }

    /// <summary>
    /// Cancels a site: carriers bound to it are released or redirected (cargo goes to the nearest storage with
    /// space), delivered materials go back to storage; only what fits nowhere is lost (ledger records it).
    /// </summary>
    internal void CancelSite(Building site)
    {
        foreach (var c in _carriers.ToArray())
        {
            if (c.DropoffId != site.Id) continue;
            if (c.Phase is CarrierPhase.ToPickup or CarrierPhase.Loading)
            {
                GetBuilding(c.PickupId)?.Stock.Unreserve(c.Resource, c.Amount);
                site.Stock.CancelIncoming(c.Amount);
                ResetCarrier(c);
                continue;
            }
            var shipment = GetShipment(c.ShipmentId)!;
            site.Stock.CancelIncoming(c.Amount);
            var dest = StoragesByDistance(c.Pos).FirstOrDefault(s => s.Stock.Space >= c.Amount);
            if (dest is not null)
            {
                dest.Stock.TryReserveIncoming(c.Amount);
                c.DropoffId = dest.Id;
                shipment.ToId = dest.Id;
                c.Target = dest.Center;
                c.Phase = CarrierPhase.ToDropoff;
            }
            else
            {
                RecordConsumed(shipment.Resource, shipment.Amount, fromStorage: false);
                Emit(new SimAlert(Tick, $"{shipment.Amount} {Content.Resources[shipment.Resource].Name} perdidos: armazéns cheios"));
                RemoveShipment(shipment);
                ResetCarrier(c);
            }
        }
        for (int r = 0; r < Content.ResourceCount; r++)
        {
            var q = site.Stock.RemoveUpTo(r, site.Stock.Get(r));
            var lost = q - AddToStorages(r, q, site.Center);
            if (lost.IsPositive)
            {
                RecordConsumed(r, lost, fromStorage: false);
                Emit(new SimAlert(Tick, $"{lost} {Content.Resources[r].Name} perdidos: armazéns cheios"));
            }
        }
        foreach (var h in _households)
            if (h.BuildSiteId == site.Id) h.BuildSiteId = 0;
        RemoveBuilding(site);
    }

    /// <summary>Drops the carrier's current job (no cargo) and sends it home.</summary>
    private void ResetCarrier(Carrier c)
    {
        c.ShipmentId = 0;
        c.PickupId = 0;
        c.DropoffId = 0;
        c.Resource = -1;
        c.Amount = Qty.Zero;
        if (c.Retiring)
        {
            _carriers.Remove(c);
            return;
        }
        c.Target = GetBuilding(c.BaseId)?.Center ?? c.Pos;
        c.Phase = c.Pos == c.Target ? CarrierPhase.Idle : CarrierPhase.Returning;
    }

    internal void Unassign(Household h)
    {
        var b = GetBuilding(h.JobBuildingId);
        if (b is not null)
        {
            int slot = Array.IndexOf(b.Slots, h.Id);
            if (slot >= 0) b.Slots[slot] = 0;
        }
        var carrier = CarrierOf(h.Id);
        if (carrier is not null) RetireCarrier(carrier);
        h.JobBuildingId = 0;
        h.AssignedBy = AssignmentSource.None;
        h.AssignedByPolicyId = 0;
        h.State = HouseholdState.Subsisting;
        Emit(new HouseholdAssigned(Tick, h.Id, 0));
    }

    internal void ChangeRecipe(Building b, RecipeDef recipe)
    {
        if (b.Recipe == recipe) return;
        b.Recipe = recipe;
        b.SeasonalWorkMilli = 0;
        Array.Clear(b.RemainderMicro);
        foreach (int id in b.Slots)
            if (GetHousehold(id) is { } h) HouseholdStateSystem.Refresh(this, h, Calendar);
    }

    /// <summary>
    /// Carrier loses its household. Without cargo it vanishes (releasing reservations);
    /// with cargo it finishes the delivery first so the resource is never lost or teleported.
    /// </summary>
    internal void RetireCarrier(Carrier c)
    {
        switch (c.Phase)
        {
            case CarrierPhase.ToPickup:
            case CarrierPhase.Loading:
                GetBuilding(c.PickupId)?.Stock.Unreserve(c.Resource, c.Amount);
                GetBuilding(c.DropoffId)?.DeliveryStock.CancelIncoming(c.Amount);
                _carriers.Remove(c);
                break;
            case CarrierPhase.ToDropoff:
            case CarrierPhase.Unloading:
                c.HouseholdId = 0;
                break;
            default:
                _carriers.Remove(c);
                break;
        }
    }

    internal void InsertCarrier(Carrier c) => _carriers.Add(c);

    internal void RemoveCarrier(Carrier c) => _carriers.Remove(c);

    internal Shipment AddShipment(int resource, Qty amount, int fromId, int toId, int carrierId)
    {
        var s = new Shipment
        {
            Id = NewId(), Resource = resource, Amount = amount, FromId = fromId, ToId = toId, CarrierId = carrierId,
        };
        InsertShipment(s);
        return s;
    }

    internal void InsertShipment(Shipment s)
    {
        _shipments.Add(s);
        _shipmentById[s.Id] = s;
    }

    internal void RemoveShipment(Shipment s)
    {
        _shipments.Remove(s);
        _shipmentById.Remove(s.Id);
    }

    internal Policy AddPolicy(PolicyDef def, int resource, Qty min, Qty max)
    {
        var p = new Policy { Id = NewId(), Def = def, Resource = resource, Min = min, Max = max, CreatedTick = Tick };
        _policies.Add(p);
        LogPolicy(p, "created", Content.Resources[resource].Name, PolicySystem.Units(min), PolicySystem.Units(max));
        return p;
    }

    internal void InsertPolicy(Policy p) => _policies.Add(p);

    /// <summary>Households the policy placed stay at their jobs but become unowned (other policies may take them).</summary>
    internal void RemovePolicyInternal(Policy p)
    {
        foreach (var h in _households)
        {
            if (h.AssignedBy == AssignmentSource.Policy && h.AssignedByPolicyId == p.Id)
            {
                h.AssignedBy = AssignmentSource.None;
                h.AssignedByPolicyId = 0;
            }
        }
        _policies.Remove(p);
    }

    internal void LogPolicy(Policy p, string key, params string[] args)
    {
        var entry = new PolicyLogEntry(Tick, p.Id, key, args);
        _policyLog.Add(entry);
        int max = Content.Balance.PolicyLogMax;
        if (_policyLog.Count > max) _policyLog.RemoveRange(0, _policyLog.Count - max);
        Emit(new PolicyActed(Tick, p.Id, entry.Text));
    }

    internal void InsertPolicyLog(PolicyLogEntry e) => _policyLog.Add(e);

    /// <summary>Active storage buildings, nearest to <paramref name="from"/> first (ties by id).</summary>
    internal List<Building> StoragesByDistance(Cell from) =>
        _buildings.Where(b => b.IsActive && b.IsStorage)
            .OrderBy(b => b.Center.Manhattan(from)).ThenBy(b => b.Id)
            .ToList();

    /// <summary>Abstract withdrawal (no carrier) from storages, nearest first. Returns what was taken.</summary>
    internal Qty TakeFromStorages(int r, Qty amount, Cell near)
    {
        var left = amount;
        foreach (var b in StoragesByDistance(near))
        {
            if (!left.IsPositive) break;
            left -= b.Stock.RemoveUpTo(r, left);
        }
        return amount - left;
    }

    /// <summary>Takes exactly <paramref name="amount"/> or nothing.</summary>
    internal bool TryTakeExactFromStorages(int r, Qty amount, Cell near)
    {
        if (StorageFree(r) < amount) return false;
        TakeFromStorages(r, amount, near);
        return true;
    }

    internal Qty AddToStorages(int r, Qty amount, Cell near)
    {
        var left = amount;
        foreach (var b in StoragesByDistance(near))
        {
            if (!left.IsPositive) break;
            left -= b.Stock.AddUpTo(r, left);
        }
        return amount - left;
    }

    internal void RecordProduced(int r, Qty q, bool economic)
    {
        if (!q.IsPositive) return;
        Ledger.Produced[r] += q.Milli;
        Telemetry.OnProduced(r, q, economic);
    }

    internal void RecordConsumed(int r, Qty q, bool fromStorage)
    {
        if (!q.IsPositive) return;
        Ledger.Consumed[r] += q.Milli;
        Telemetry.OnConsumed(r, q, fromStorage);
    }
}
