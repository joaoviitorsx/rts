namespace Ironvale.Sim.Content;

/// <summary>Immutable content tables. Indices follow file order and are stable for a given content hash.</summary>
public sealed class ContentDb
{
    private readonly Dictionary<string, ResourceDef> _resources;
    private readonly Dictionary<string, BuildingDef> _buildings;
    private readonly Dictionary<string, RecipeDef> _recipes;
    private readonly Dictionary<string, PolicyDef> _policies;

    public IReadOnlyList<ResourceDef> Resources { get; }
    public IReadOnlyList<BuildingDef> Buildings { get; }
    public IReadOnlyList<RecipeDef> Recipes { get; }
    public IReadOnlyList<PolicyDef> Policies { get; }
    public BalanceDef Balance { get; }
    public string Hash { get; }

    public int ResourceCount => Resources.Count;

    internal ContentDb(
        IReadOnlyList<ResourceDef> resources,
        IReadOnlyList<BuildingDef> buildings,
        IReadOnlyList<RecipeDef> recipes,
        IReadOnlyList<PolicyDef> policies,
        BalanceDef balance,
        string hash)
    {
        Resources = resources;
        Buildings = buildings;
        Recipes = recipes;
        Policies = policies;
        Balance = balance;
        Hash = hash;
        _resources = resources.ToDictionary(r => r.Id, StringComparer.Ordinal);
        _buildings = buildings.ToDictionary(b => b.Id, StringComparer.Ordinal);
        _recipes = recipes.ToDictionary(r => r.Id, StringComparer.Ordinal);
        _policies = policies.ToDictionary(p => p.Id, StringComparer.Ordinal);
    }

    public ResourceDef Resource(string id) =>
        _resources.TryGetValue(id, out var r) ? r : throw new KeyNotFoundException($"Unknown resource '{id}'");

    public BuildingDef Building(string id) =>
        _buildings.TryGetValue(id, out var b) ? b : throw new KeyNotFoundException($"Unknown building '{id}'");

    public RecipeDef Recipe(string id) =>
        _recipes.TryGetValue(id, out var r) ? r : throw new KeyNotFoundException($"Unknown recipe '{id}'");

    public PolicyDef Policy(string id) =>
        _policies.TryGetValue(id, out var p) ? p : throw new KeyNotFoundException($"Unknown policy '{id}'");

    public bool TryResource(string id, out ResourceDef def) => _resources.TryGetValue(id, out def!);
    public bool TryBuilding(string id, out BuildingDef def) => _buildings.TryGetValue(id, out def!);
    public bool TryRecipe(string id, out RecipeDef def) => _recipes.TryGetValue(id, out def!);
    public bool TryPolicy(string id, out PolicyDef def) => _policies.TryGetValue(id, out def!);

    public Qty[] NewResourceArray() => new Qty[ResourceCount];
}
