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

    /// <summary>Stock in storage plus what carriers are bringing to storage right now.</summary>
    public Qty StorageStockIncludingTransit(int r)
    {
        long sum = StorageStock(r).Milli;
        foreach (var s in _shipments)
            if (s.Resource == r) sum += s.Amount.Milli;
        return new Qty(sum);
    }

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
            Stock = new Stockpile(Content.ResourceCount, def.StockCapacity),
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
            };
            _carriers.Add(c);
        }
        HouseholdStateSystem.Refresh(this, h, Calendar);
        Emit(new HouseholdAssigned(Tick, h.Id, b.Id));
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
                GetBuilding(c.DropoffId)?.Stock.CancelIncoming(c.Amount);
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

    internal Policy AddPolicy(PolicyDef def, int resource, Qty threshold)
    {
        var p = new Policy { Id = NewId(), Def = def, Resource = resource, Threshold = threshold, CreatedTick = Tick };
        _policies.Add(p);
        LogPolicy(p, $"Política criada: manter {Content.Resources[resource].Name} acima de {threshold}");
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

    internal void LogPolicy(Policy p, string text)
    {
        _policyLog.Add(new PolicyLogEntry(Tick, p.Id, text));
        int max = Content.Balance.PolicyLogMax;
        if (_policyLog.Count > max) _policyLog.RemoveRange(0, _policyLog.Count - max);
        Emit(new PolicyActed(Tick, p.Id, text));
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
