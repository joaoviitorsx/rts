using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Ironvale.Sim.Save;

public sealed class SaveException(string message) : Exception(message);

/// <summary>
/// Versioned save: JSON envelope (format, saveVersion, contentHash) gzip-compressed.
/// The sim deals in bytes; file paths are the view's business.
/// </summary>
public static class SaveSerializer
{
    public const string FormatId = "ironvale-save";
    public const int CurrentVersion = 1;
    public const string GameVersion = "0.1.0";

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    /// <summary>Ordered chain: index i migrates version i+1 → i+2. Empty while only v1 exists.</summary>
    private static readonly List<Func<JsonObject, JsonObject>> Migrations = new();

    // ------------------------------------------------------------------ save

    public static byte[] Save(World w, DateTime? createdUtc = null)
    {
        var file = new SaveFile
        {
            SaveVersion = CurrentVersion,
            GameVersion = GameVersion,
            ContentHash = w.Content.Hash,
            CreatedUtc = (createdUtc ?? DateTime.UtcNow).ToString("O"),
            State = ToDto(w),
            Telemetry = ToDto(w.Telemetry, w.Content),
        };
        byte[] json = JsonSerializer.SerializeToUtf8Bytes(file, Options);
        using var output = new MemoryStream();
        using (var gz = new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true))
            gz.Write(json);
        return output.ToArray();
    }

    /// <summary>Hash of the canonical simulation state (no telemetry, no metadata).</summary>
    public static ulong StateHash(World w) =>
        Fnv64.Hash(JsonSerializer.SerializeToUtf8Bytes(ToDto(w), Options));

    public static string StateHashHex(World w) => Fnv64.ToHex(StateHash(w));

    private static StateDto ToDto(World w)
    {
        var content = w.Content;
        string Res(int r) => content.Resources[r].Id;

        SortedDictionary<string, long> Map(long[] values)
        {
            var map = new SortedDictionary<string, long>(StringComparer.Ordinal);
            for (int r = 0; r < values.Length; r++)
                if (values[r] != 0) map[Res(r)] = values[r];
            return map;
        }

        return new StateDto
        {
            Seed = w.Seed,
            Tick = w.Tick,
            StartTick = w.StartTick,
            ScenarioId = w.ScenarioId,
            MapWidth = w.Map.Width,
            MapHeight = w.Map.Height,
            NextId = w.NextId,
            Rng = w.Rng.All.Select(kv => new RngDto { Name = kv.Key, State = kv.Value.State, Inc = kv.Value.Inc }).ToList(),
            Households = w.Households.Select(h => new HouseholdDto
            {
                Id = h.Id, Name = h.Name, Members = h.Members, Workers = h.Workers, HomeId = h.HomeId,
                JobBuildingId = h.JobBuildingId, AssignedBy = h.AssignedBy.ToString(),
                AssignedByPolicyId = h.AssignedByPolicyId, ToolCondition = h.ToolCondition,
                FoodDeficitDays = h.FoodDeficitDays, ColdDeficitDays = h.ColdDeficitDays,
                Productivity = h.ProductivityPermille, State = h.State.ToString(), ToolHoursToday = h.ToolHoursToday,
            }).ToList(),
            Buildings = w.Buildings.Select(b => new BuildingDto
            {
                Id = b.Id, Def = b.Def.Id, X = b.Origin.X, Y = b.Origin.Y, Rotation = b.Rotation,
                Active = b.IsActive, BuildProgressDays = b.BuildProgressDays,
                Stock = Map(b.Stock.AmountsRaw), Reserved = Map(b.Stock.ReservedRaw), Incoming = b.Stock.Incoming.Milli,
                Slots = b.Slots.ToArray(), Recipe = b.Recipe?.Id, SeasonalWorkMilli = b.SeasonalWorkMilli,
                RemainderMicro = Map(b.RemainderMicro),
            }).ToList(),
            Carriers = w.Carriers.Select(c => new CarrierDto
            {
                Id = c.Id, HouseholdId = c.HouseholdId, BaseId = c.BaseId, Phase = c.Phase.ToString(),
                X = c.Pos.X, Y = c.Pos.Y, TargetX = c.Target.X, TargetY = c.Target.Y,
                StepTicks = c.StepTicks, WaitTicks = c.WaitTicks, PickupId = c.PickupId, DropoffId = c.DropoffId,
                Resource = c.Resource >= 0 ? Res(c.Resource) : null, Amount = c.Amount.Milli, ShipmentId = c.ShipmentId,
            }).ToList(),
            Shipments = w.Shipments.Select(s => new ShipmentDto
            {
                Id = s.Id, Resource = Res(s.Resource), Amount = s.Amount.Milli,
                FromId = s.FromId, ToId = s.ToId, CarrierId = s.CarrierId,
            }).ToList(),
            Policies = w.Policies.Select(p => new PolicyDto
            {
                Id = p.Id, Def = p.Def.Id, Enabled = p.Enabled, Resource = Res(p.Resource),
                Threshold = p.Threshold.Milli, CreatedTick = p.CreatedTick, LastBlockedReason = p.LastBlockedReason,
            }).ToList(),
            Ledger = new LedgerDto
            {
                Initial = Map(w.Ledger.Initial), Produced = Map(w.Ledger.Produced), Consumed = Map(w.Ledger.Consumed),
            },
            PolicyLog = w.PolicyLog.Select(e => new PolicyLogDto { Tick = e.Tick, PolicyId = e.PolicyId, Text = e.Text }).ToList(),
        };
    }

    private static TelemetryDto ToDto(TelemetryRecorder t, ContentDb content) => new()
    {
        Resources = content.Resources.Select(r => r.Id).ToList(),
        Daily = t.Daily.Select(d => new DailyDto
        {
            Day = d.Day, Produced = d.Produced, Consumed = d.Consumed, Stored = d.Stored,
            Local = d.Local, Transit = d.Transit, Population = d.Population,
        }).ToList(),
        Monthly = t.Monthly.Select(m => new MonthlyDto
        {
            MonthIndex = m.MonthIndex, Produced = m.Produced, Consumed = m.Consumed,
            StoredAtEnd = m.StoredAtEnd, PopulationAtEnd = m.PopulationAtEnd,
        }).ToList(),
        TodayProduced = t.TodayProduced, TodayConsumed = t.TodayConsumed,
        MonthProduced = t.MonthProduced, MonthConsumed = t.MonthConsumed,
        ActivityToday = t.ActivityToday, FrozenDays = t.FrozenDays, Deadlocked = t.Deadlocked, DeadlockDay = t.DeadlockDay,
    };

    // ------------------------------------------------------------------ load

    public sealed record LoadResult(World World, IReadOnlyList<string> Warnings);

    public static LoadResult Load(byte[] data, ContentDb content)
    {
        byte[] json = IsGzip(data) ? Gunzip(data) : data;
        JsonObject root;
        try
        {
            root = JsonNode.Parse(json)?.AsObject() ?? throw new SaveException("empty save");
        }
        catch (JsonException e)
        {
            throw new SaveException($"corrupted save: {e.Message}");
        }

        if ((string?)root["format"] != FormatId) throw new SaveException("not an Ironvale save");
        int version = (int?)root["saveVersion"] ?? throw new SaveException("missing saveVersion");
        if (version > CurrentVersion) throw new SaveException($"save v{version} is newer than this game (v{CurrentVersion})");
        while (version < CurrentVersion)
        {
            root = Migrations[version - 1](root);
            version++;
            root["saveVersion"] = version;
        }

        var file = root.Deserialize<SaveFile>(Options) ?? throw new SaveException("empty save");
        var warnings = new List<string>();
        if (file.ContentHash != content.Hash)
            warnings.Add($"conteúdo diferente do salvo ({file.ContentHash} ≠ {content.Hash}); valores podem divergir");

        var w = FromDto(file.State, content);
        if (file.Telemetry is not null) RestoreTelemetry(w.Telemetry, file.Telemetry, content, warnings);
        return new LoadResult(w, warnings);
    }

    private static World FromDto(StateDto s, ContentDb content)
    {
        int Res(string id) => content.TryResource(id, out var r) ? r.Index : throw new SaveException($"unknown resource '{id}'");

        long[] Arr(SortedDictionary<string, long> map)
        {
            var arr = new long[content.ResourceCount];
            foreach (var (id, v) in map) arr[Res(id)] = v;
            return arr;
        }

        T Enum<T>(string text) where T : struct, System.Enum =>
            System.Enum.TryParse<T>(text, out var v) ? v : throw new SaveException($"invalid {typeof(T).Name} '{text}'");

        var w = new World(content, s.Seed, s.ScenarioId, s.MapWidth, s.MapHeight, s.StartTick)
        {
            Tick = s.Tick,
            NextId = s.NextId,
        };
        foreach (var r in s.Rng) w.Rng.Restore(r.Name, r.State, r.Inc);

        foreach (var d in s.Buildings)
        {
            if (!content.TryBuilding(d.Def, out var def)) throw new SaveException($"unknown building '{d.Def}'");
            var b = new Building
            {
                Id = d.Id, Def = def, Origin = new Cell(d.X, d.Y), Rotation = d.Rotation, IsActive = d.Active,
                BuildProgressDays = d.BuildProgressDays,
                Stock = new Stockpile(content.ResourceCount, def.StockCapacity),
                Slots = d.Slots.ToArray(),
                Recipe = d.Recipe is null ? null : def.Recipes.FirstOrDefault(r => r.Id == d.Recipe)
                    ?? throw new SaveException($"{d.Def} has no recipe '{d.Recipe}'"),
                SeasonalWorkMilli = d.SeasonalWorkMilli,
                RemainderMicro = Arr(d.RemainderMicro),
            };
            b.Stock.Restore(Arr(d.Stock), Arr(d.Reserved), d.Incoming);
            w.InsertBuilding(b);
            w.Map.Fill(def, b.Origin, b.Rotation, b.Id);
        }

        foreach (var d in s.Households)
        {
            w.AddHousehold(new Household
            {
                Id = d.Id, Name = d.Name, Members = d.Members, Workers = d.Workers, HomeId = d.HomeId,
                JobBuildingId = d.JobBuildingId, AssignedBy = Enum<AssignmentSource>(d.AssignedBy),
                AssignedByPolicyId = d.AssignedByPolicyId, ToolCondition = d.ToolCondition,
                FoodDeficitDays = d.FoodDeficitDays, ColdDeficitDays = d.ColdDeficitDays,
                ProductivityPermille = d.Productivity, State = Enum<HouseholdState>(d.State),
                ToolHoursToday = d.ToolHoursToday,
            });
        }

        foreach (var d in s.Carriers)
        {
            w.InsertCarrier(new Carrier
            {
                Id = d.Id, HouseholdId = d.HouseholdId, BaseId = d.BaseId, Phase = Enum<CarrierPhase>(d.Phase),
                Pos = new Cell(d.X, d.Y), Target = new Cell(d.TargetX, d.TargetY), StepTicks = d.StepTicks,
                WaitTicks = d.WaitTicks, PickupId = d.PickupId, DropoffId = d.DropoffId,
                Resource = d.Resource is null ? -1 : Res(d.Resource), Amount = new Qty(d.Amount), ShipmentId = d.ShipmentId,
            });
        }

        foreach (var d in s.Shipments)
        {
            w.InsertShipment(new Shipment
            {
                Id = d.Id, Resource = Res(d.Resource), Amount = new Qty(d.Amount),
                FromId = d.FromId, ToId = d.ToId, CarrierId = d.CarrierId,
            });
        }

        foreach (var d in s.Policies)
        {
            if (!content.TryPolicy(d.Def, out var def)) throw new SaveException($"unknown policy '{d.Def}'");
            w.InsertPolicy(new Policy
            {
                Id = d.Id, Def = def, Enabled = d.Enabled, Resource = Res(d.Resource),
                Threshold = new Qty(d.Threshold), CreatedTick = d.CreatedTick, LastBlockedReason = d.LastBlockedReason,
            });
        }

        Array.Copy(Arr(s.Ledger.Initial), w.Ledger.Initial, content.ResourceCount);
        Array.Copy(Arr(s.Ledger.Produced), w.Ledger.Produced, content.ResourceCount);
        Array.Copy(Arr(s.Ledger.Consumed), w.Ledger.Consumed, content.ResourceCount);
        foreach (var e in s.PolicyLog) w.InsertPolicyLog(new PolicyLogEntry(e.Tick, e.PolicyId, e.Text));
        return w;
    }

    private static void RestoreTelemetry(TelemetryRecorder t, TelemetryDto d, ContentDb content, List<string> warnings)
    {
        if (!d.Resources.SequenceEqual(content.Resources.Select(r => r.Id)))
        {
            warnings.Add("telemetria descartada: lista de recursos mudou");
            return;
        }
        foreach (var x in d.Daily)
        {
            t.AddDaily(new DailySample
            {
                Day = x.Day, Produced = x.Produced, Consumed = x.Consumed, Stored = x.Stored,
                Local = x.Local, Transit = x.Transit, Population = x.Population,
            });
        }
        foreach (var x in d.Monthly)
        {
            t.Monthly.Add(new MonthlySample
            {
                MonthIndex = x.MonthIndex, Produced = x.Produced, Consumed = x.Consumed,
                StoredAtEnd = x.StoredAtEnd, PopulationAtEnd = x.PopulationAtEnd,
            });
        }
        t.TodayProduced = d.TodayProduced;
        t.TodayConsumed = d.TodayConsumed;
        t.MonthProduced = d.MonthProduced;
        t.MonthConsumed = d.MonthConsumed;
        t.ActivityToday = d.ActivityToday;
        t.FrozenDays = d.FrozenDays;
        t.Deadlocked = d.Deadlocked;
        t.DeadlockDay = d.DeadlockDay;
    }

    private static bool IsGzip(byte[] data) => data.Length > 2 && data[0] == 0x1f && data[1] == 0x8b;

    private static byte[] Gunzip(byte[] data)
    {
        using var input = new GZipStream(new MemoryStream(data), CompressionMode.Decompress);
        using var output = new MemoryStream();
        input.CopyTo(output);
        return output.ToArray();
    }
}
