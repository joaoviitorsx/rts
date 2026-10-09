using System.Text;
using System.Text.Json;

namespace Ironvale.Sim.Content;

public sealed class ContentException(string message) : Exception(message);

/// <summary>
/// Parses data/*.json into a <see cref="ContentDb"/>. Takes file contents (not paths) so the Godot view
/// can feed files read through res:// and tests/CLI can read from disk.
/// </summary>
public static class ContentLoader
{
    public const string ResourcesFile = "resources.json";
    public const string BuildingsFile = "buildings.json";
    public const string RecipesFile = "recipes.json";
    public const string PoliciesFile = "policies.json";
    public const string BalanceFile = "balance.json";

    public static readonly string[] RequiredFiles =
        { ResourcesFile, BuildingsFile, RecipesFile, PoliciesFile, BalanceFile };

    private static readonly JsonDocumentOptions JsonOptions = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>Loads from a directory on disk (tests, CLI, editor runs).</summary>
    public static ContentDb LoadFromDirectory(string dataDir)
    {
        var files = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var name in RequiredFiles)
        {
            string path = Path.Combine(dataDir, name);
            if (!File.Exists(path)) throw new ContentException($"{name}: file not found in {dataDir}");
            files[name] = File.ReadAllText(path);
        }
        return Load(files);
    }

    public static ContentDb Load(IReadOnlyDictionary<string, string> files)
    {
        string Text(string name) =>
            files.TryGetValue(name, out var t) ? t : throw new ContentException($"{name}: missing");

        var resources = ParseResources(Text(ResourcesFile));
        var resIndex = resources.ToDictionary(r => r.Id, r => r.Index, StringComparer.Ordinal);
        var recipes = ParseRecipes(Text(RecipesFile), resIndex);
        var recipeById = recipes.ToDictionary(r => r.Id, StringComparer.Ordinal);
        var buildings = ParseBuildings(Text(BuildingsFile), resIndex, recipeById);
        var policies = ParsePolicies(Text(PoliciesFile));
        var balance = ParseBalance(Text(BalanceFile));

        // Hash over normalized contents in fixed file order (saves record it).
        ulong hash = 14695981039346656037UL;
        foreach (var name in RequiredFiles)
        {
            hash = Fnv64.Hash(Encoding.UTF8.GetBytes(name), hash);
            hash = Fnv64.Hash(Encoding.UTF8.GetBytes(Text(name).Replace("\r\n", "\n")), hash);
        }

        return new ContentDb(resources, buildings, recipes, policies, balance, "fnv64:" + Fnv64.ToHex(hash));
    }

    public static ScenarioDef LoadScenario(string json, ContentDb content, string fileName = "scenario")
    {
        var ctx = new Ctx(fileName);
        using var doc = Parse(json, ctx);
        var root = doc.RootElement;
        var resIndex = content.Resources.ToDictionary(r => r.Id, r => r.Index, StringComparer.Ordinal);

        var map = ctx.Obj(root, "map");
        var buildings = new List<ScenarioBuilding>();
        foreach (var (el, i) in ctx.Arr(root, "buildings").Select((e, i) => (e, i)))
        {
            var c = ctx.At($"buildings[{i}]");
            string def = c.Str(el, "def");
            if (!content.TryBuilding(def, out var bdef)) throw c.Error($"unknown building '{def}'");
            var origin = c.IntPair(el, "origin");
            buildings.Add(new ScenarioBuilding
            {
                Def = bdef,
                Origin = new Cell(origin.a, origin.b),
                Rotation = c.OptInt(el, "rotation", 0),
            });
        }

        var households = new List<ScenarioHousehold>();
        foreach (var (el, i) in ctx.Arr(root, "households").Select((e, i) => (e, i)))
        {
            var c = ctx.At($"households[{i}]");
            households.Add(new ScenarioHousehold
            {
                Name = c.Str(el, "name"),
                Members = c.PositiveInt(el, "members"),
                Workers = c.PositiveInt(el, "workers"),
            });
        }

        int startMonth = ctx.OptInt(root, "startMonth", 0);
        if (startMonth is < 0 or >= SimTime.MonthsPerYear) throw ctx.Error("startMonth must be 0..11");

        return new ScenarioDef
        {
            Id = ctx.Str(root, "id"),
            Name = ctx.Str(root, "name"),
            MapWidth = ctx.PositiveInt(map, "width"),
            MapHeight = ctx.PositiveInt(map, "height"),
            StartMonth = startMonth,
            Buildings = buildings,
            Stock = ctx.QtyMap(root, "stock", resIndex),
            Households = households,
            HouseholdToolCondition = Permille.Clamp(ctx.OptInt(root, "householdToolCondition", Permille.One)),
        };
    }

    private static List<ResourceDef> ParseResources(string json)
    {
        var ctx = new Ctx(ResourcesFile);
        using var doc = Parse(json, ctx);
        var list = new List<ResourceDef>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var el in ctx.Arr(doc.RootElement, "resources"))
        {
            var c = ctx.At($"resources[{list.Count}]");
            string id = c.Str(el, "id");
            if (!seen.Add(id)) throw c.Error($"duplicate id '{id}'");
            var carry = Qty.FromDouble(c.Num(el, "carryPerTrip"));
            if (!carry.IsPositive) throw c.Error("carryPerTrip must be > 0");
            list.Add(new ResourceDef { Index = list.Count, Id = id, Name = c.Str(el, "name"), CarryPerTrip = carry });
        }
        if (list.Count == 0) throw ctx.Error("no resources defined");
        return list;
    }

    private static List<RecipeDef> ParseRecipes(string json, Dictionary<string, int> resIndex)
    {
        var ctx = new Ctx(RecipesFile);
        using var doc = Parse(json, ctx);
        var list = new List<RecipeDef>();
        foreach (var el in ctx.Arr(doc.RootElement, "recipes"))
        {
            var c = ctx.At($"recipes[{list.Count}]");
            string kindText = c.Str(el, "kind");
            var kind = kindText switch
            {
                "continuous" => RecipeKind.Continuous,
                "seasonal" => RecipeKind.Seasonal,
                _ => throw c.Error($"kind must be 'continuous' or 'seasonal', got '{kindText}'"),
            };
            var outputs = c.QtyMap(el, "outputs", resIndex);
            var inputs = c.QtyMap(el, "inputs", resIndex, optional: true);
            int outputCount = outputs.Count(q => q.IsPositive);
            if (outputCount == 0) throw c.Error("outputs must have at least one positive entry");
            if (kind == RecipeKind.Seasonal && outputCount != 1) throw c.Error("seasonal recipes must have exactly one output");
            if (inputs.Any(q => q.IsPositive) && (kind != RecipeKind.Continuous || outputCount != 1))
                throw c.Error("recipes with inputs must be continuous with exactly one output");

            var seasons = new bool[SimTime.SeasonsPerYear];
            if (el.TryGetProperty("workSeasons", out var ws))
            {
                foreach (var s in ws.EnumerateArray())
                    seasons[(int)ParseSeason(s.GetString(), c)] = true;
            }
            else
            {
                Array.Fill(seasons, true);
            }

            list.Add(new RecipeDef
            {
                Index = list.Count,
                Id = c.Str(el, "id"),
                Name = c.Str(el, "name"),
                Kind = kind,
                OutputPerWorkerHour = outputs,
                InputPerOutput = inputs,
                UsesTools = c.OptBool(el, "usesTools", false),
                WorkSeasons = seasons,
            });
        }
        return list;
    }

    private static List<BuildingDef> ParseBuildings(
        string json, Dictionary<string, int> resIndex, Dictionary<string, RecipeDef> recipes)
    {
        var ctx = new Ctx(BuildingsFile);
        using var doc = Parse(json, ctx);
        var list = new List<BuildingDef>();
        foreach (var el in ctx.Arr(doc.RootElement, "buildings"))
        {
            var c = ctx.At($"buildings[{list.Count}]");
            var roles = BuildingRole.None;
            foreach (var r in c.Arr(el, "roles"))
            {
                roles |= r.GetString() switch
                {
                    "seat" => BuildingRole.Seat,
                    "housing" => BuildingRole.Housing,
                    "producer" => BuildingRole.Producer,
                    "storage" => BuildingRole.Storage,
                    var other => throw c.Error($"unknown role '{other}'"),
                };
            }
            if (roles.HasFlag(BuildingRole.Producer) && roles.HasFlag(BuildingRole.Storage))
                throw c.Error("a building cannot be both producer and storage");

            var recipeList = new List<RecipeDef>();
            if (el.TryGetProperty("recipes", out var recEl))
            {
                foreach (var r in recEl.EnumerateArray())
                {
                    string rid = r.GetString() ?? "";
                    if (!recipes.TryGetValue(rid, out var rdef)) throw c.Error($"unknown recipe '{rid}'");
                    recipeList.Add(rdef);
                }
            }
            if (roles.HasFlag(BuildingRole.Producer) && recipeList.Count == 0) throw c.Error("producer needs recipes");

            var fp = c.IntPair(el, "footprint");
            if (fp.a <= 0 || fp.b <= 0) throw c.Error("footprint must be positive");

            list.Add(new BuildingDef
            {
                Index = list.Count,
                Id = c.Str(el, "id"),
                Name = c.Str(el, "name"),
                FootprintW = fp.a,
                FootprintH = fp.b,
                Buildable = c.OptBool(el, "buildable", true),
                Cost = c.QtyMap(el, "cost", resIndex, optional: true),
                BuildDays = c.OptInt(el, "buildDays", 1),
                Roles = roles,
                JobSlots = c.OptInt(el, "jobSlots", 0),
                Recipes = recipeList,
                OutputCapacity = Qty.FromDouble(c.OptNum(el, "outputCapacity", 0)),
                StorageCapacity = Qty.FromDouble(c.OptNum(el, "storageCapacity", 0)),
                InputCapacity = Qty.FromDouble(c.OptNum(el, "inputCapacity", 0)),
                HousingCapacity = c.OptInt(el, "housingCapacity", 0),
            });
        }
        return list;
    }

    private static List<PolicyDef> ParsePolicies(string json)
    {
        var ctx = new Ctx(PoliciesFile);
        using var doc = Parse(json, ctx);
        var list = new List<PolicyDef>();
        foreach (var el in ctx.Arr(doc.RootElement, "policies"))
        {
            var c = ctx.At($"policies[{list.Count}]");
            string kind = c.Str(el, "kind");
            list.Add(new PolicyDef
            {
                Index = list.Count,
                Id = c.Str(el, "id"),
                Name = c.Str(el, "name"),
                Kind = kind == "keep_above" ? PolicyKind.KeepAbove : throw c.Error($"unknown kind '{kind}'"),
                CaCost = c.OptInt(el, "caCost", 0),
                HysteresisPermille = c.OptInt(el, "hysteresisPermille", 250),
                MaxHouseholds = c.OptInt(el, "maxHouseholds", 4),
            });
        }
        return list;
    }

    private static BalanceDef ParseBalance(string json)
    {
        var c = new Ctx(BalanceFile);
        using var doc = Parse(json, c);
        var el = doc.RootElement;
        return new BalanceDef
        {
            FoodPerMemberPerDay = Qty.FromDouble(c.Num(el, "foodPerMemberPerDay")),
            FirewoodPerHouseholdPerWinterDay = Qty.FromDouble(c.Num(el, "firewoodPerHouseholdPerWinterDay")),
            HomelessHeatPermille = c.PositiveInt(el, "homelessHeatPermille"),
            ToolWearPerWorkDayPermille = c.PositiveInt(el, "toolWearPerWorkDayPermille"),
            ToolFactorFloorPermille = Permille.Clamp(c.PositiveInt(el, "toolFactorFloorPermille")),
            HungryFactorPermille = Permille.Clamp(c.PositiveInt(el, "hungryFactorPermille")),
            ColdFactorPermille = Permille.Clamp(c.PositiveInt(el, "coldFactorPermille")),
            LeaveAfterDeficitDays = c.PositiveInt(el, "leaveAfterDeficitDays"),
            SubsistenceFoodCoverPermille = Permille.Clamp(c.OptInt(el, "subsistenceFoodCoverPermille", 0)),
            SubsistenceFirewoodCoverPermille = Permille.Clamp(c.OptInt(el, "subsistenceFirewoodCoverPermille", 0)),
            TicksPerCellRoad = c.PositiveInt(el, "ticksPerCellRoad"),
            TicksPerCellOffroad = c.PositiveInt(el, "ticksPerCellOffroad"),
            RoadStonePerCell = Qty.FromDouble(c.OptNum(el, "roadStonePerCell", 1)),
            CommuteTicksPermille = c.OptInt(el, "commuteTicksPermille", 70),
            AutoRehome = c.OptInt(el, "autoRehome", 1) != 0,
            GardenFreeHours = c.OptInt(el, "gardenFreeHours", 2),
            GardenFoodPerHour = Qty.FromDouble(c.OptNum(el, "gardenFoodPerHour", 0.15)),
            RehomeMinGainTicks = c.OptInt(el, "rehomeMinGainTicks", 6),
            CarrierLoadTicks = c.PositiveInt(el, "carrierLoadTicks"),
            MinPickup = Qty.FromDouble(c.Num(el, "minPickup")),
            HaulUrgentDays = c.OptInt(el, "haulUrgentDays", 20),
            HarvestVariancePermille = Permille.Clamp(c.OptInt(el, "harvestVariancePermille", 0)),
            DeadlockWindowDays = c.PositiveInt(el, "deadlockWindowDays"),
            PolicyLogMax = c.PositiveInt(el, "policyLogMax"),
            AutoBuilders = c.OptInt(el, "autoBuilders", 1) != 0,
            MaxBuildersPerSite = Math.Max(1, c.OptInt(el, "maxBuildersPerSite", 3)),
        };
    }

    private static Season ParseSeason(string? s, Ctx c) => s switch
    {
        "spring" => Season.Spring,
        "summer" => Season.Summer,
        "autumn" => Season.Autumn,
        "winter" => Season.Winter,
        _ => throw c.Error($"unknown season '{s}'"),
    };

    private static JsonDocument Parse(string json, Ctx ctx)
    {
        try { return JsonDocument.Parse(json, JsonOptions); }
        catch (JsonException e) { throw ctx.Error($"invalid JSON: {e.Message}"); }
    }

    /// <summary>Error context: file + JSON path, so content errors point at the exact field.</summary>
    private readonly struct Ctx(string file, string path = "")
    {
        public Ctx At(string path) => new(file, path);

        public ContentException Error(string msg) =>
            new($"{file}: {(path.Length > 0 ? path + ": " : "")}{msg}");

        private JsonElement Req(JsonElement el, string name) =>
            el.ValueKind == JsonValueKind.Object && el.TryGetProperty(name, out var v)
                ? v : throw Error($"missing field '{name}'");

        public string Str(JsonElement el, string name)
        {
            var v = Req(el, name);
            return v.ValueKind == JsonValueKind.String && v.GetString() is { Length: > 0 } s
                ? s : throw Error($"'{name}' must be a non-empty string");
        }

        public double Num(JsonElement el, string name)
        {
            var v = Req(el, name);
            if (v.ValueKind != JsonValueKind.Number) throw Error($"'{name}' must be a number");
            double d = v.GetDouble();
            return d >= 0 && double.IsFinite(d) ? d : throw Error($"'{name}' must be >= 0");
        }

        public double OptNum(JsonElement el, string name, double fallback) =>
            el.TryGetProperty(name, out _) ? Num(el, name) : fallback;

        public int PositiveInt(JsonElement el, string name)
        {
            var v = Req(el, name);
            return v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out int i) && i > 0
                ? i : throw Error($"'{name}' must be a positive integer");
        }

        public int OptInt(JsonElement el, string name, int fallback)
        {
            if (!el.TryGetProperty(name, out var v)) return fallback;
            return v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out int i) && i >= 0
                ? i : throw Error($"'{name}' must be a non-negative integer");
        }

        public bool OptBool(JsonElement el, string name, bool fallback)
        {
            if (!el.TryGetProperty(name, out var v)) return fallback;
            return v.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => throw Error($"'{name}' must be a boolean"),
            };
        }

        public JsonElement Obj(JsonElement el, string name)
        {
            var v = Req(el, name);
            return v.ValueKind == JsonValueKind.Object ? v : throw Error($"'{name}' must be an object");
        }

        public JsonElement.ArrayEnumerator Arr(JsonElement el, string name)
        {
            var v = Req(el, name);
            return v.ValueKind == JsonValueKind.Array ? v.EnumerateArray() : throw Error($"'{name}' must be an array");
        }

        public (int a, int b) IntPair(JsonElement el, string name)
        {
            var v = Req(el, name);
            if (v.ValueKind != JsonValueKind.Array || v.GetArrayLength() != 2)
                throw Error($"'{name}' must be [int, int]");
            return (v[0].GetInt32(), v[1].GetInt32());
        }

        public Qty[] QtyMap(JsonElement el, string name, Dictionary<string, int> resIndex, bool optional = false)
        {
            var result = new Qty[resIndex.Count];
            if (!el.TryGetProperty(name, out var map))
            {
                if (optional) return result;
                throw Error($"missing field '{name}'");
            }
            if (map.ValueKind != JsonValueKind.Object) throw Error($"'{name}' must be an object of resource → number");
            foreach (var prop in map.EnumerateObject())
            {
                if (!resIndex.TryGetValue(prop.Name, out int idx)) throw Error($"'{name}': unknown resource '{prop.Name}'");
                if (prop.Value.ValueKind != JsonValueKind.Number || prop.Value.GetDouble() < 0)
                    throw Error($"'{name}.{prop.Name}' must be a number >= 0");
                result[idx] = Qty.FromDouble(prop.Value.GetDouble());
            }
            return result;
        }
    }
}
