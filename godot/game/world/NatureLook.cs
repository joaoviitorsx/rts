using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Ironvale.Sim;
using Ironvale.Sim.Map;
using Noise = Ironvale.Sim.Map.Noise;

namespace Ironvale.Game.World3D;

/// <summary>
/// Living nature of a generated map with the Stylized Nature MegaKit (CC0) through the approved look-dev pipeline
/// (plant palette, impostors far away). Every tree of the sim is one instance — species, stage, a stump when felled, an
/// autumn tint on some crowns (Koastalia mix) — and bushes (with fruit in season), mushrooms, loose stones, deposits and
/// what lies on the ground use the kit's models too. Static dressing (rocks at cliff feet, ferns in the woods, tufts and
/// flowers in the meadows) is scattered once. 64-m tiles are rebuilt only when their content changes.
/// </summary>
public partial class NatureLook : Node3D
{
    private const int TileCells = 32;
    private const float ImpostorDistance = 110f;
    private const float Hysteresis = 5f;
    private const string E = "res://assets/environment/";

    private static readonly string[] Oaks = { "ENV_Oak_A", "ENV_Oak_B", "ENV_Oak_C", "ENV_Oak_D", "ENV_Oak_E" };
    private static readonly string[] Pines = { "ENV_Pine_A", "ENV_Pine_B", "ENV_Pine_C", "ENV_Pine_D", "ENV_Pine_E" };
    private static readonly string[] Birches = { "ENV_TwistedTree_A", "ENV_TwistedTree_B" };
    private static readonly string[] Rocks = { "ENV_Rock_A", "ENV_Rock_B", "ENV_Rock_C" };
    private static readonly Color[] Autumn = { new("e0952c"), new("e6c142"), new("c4552f") };

    private SimHost _host = null!;
    private float _cs;
    private GDScript _clusters = null!, _palette = null!, _impostors = null!;
    private readonly Dictionary<(int, int), Node3D> _tiles = new();
    private readonly Dictionary<(int, int), long> _signatures = new();
    private readonly Dictionary<string, (Mesh Mesh, Transform3D Xform)> _meshes = new();
    private double _timer;

    public void Init(SimHost host, float cellSize)
    {
        _host = host;
        _cs = cellSize;
        _clusters = GD.Load<GDScript>("res://game/vegetation/Clusters.gd");
        _palette = GD.Load<GDScript>("res://game/vegetation/PlantPalette.gd");
        _impostors = GD.Load<GDScript>("res://game/vegetation/TreeImpostors.gd");
        Name = "NatureLook";
    }

    public override void _Process(double delta)
    {
        if (_host is null || _host.IsBusy || _host.World.Nature is null) return;
        _timer -= delta;
        if (_timer > 0) return;
        _timer = 0.4;
        Refresh(_host.World);
    }

    public void Refresh(World w)
    {
        var t = w.Terrain!;
        for (int ty = 0; ty * TileCells < t.Height; ty++)
        for (int tx = 0; tx * TileCells < t.Width; tx++)
        {
            long sig = Signature(w, tx, ty);
            if (_signatures.TryGetValue((tx, ty), out var old) && old == sig) continue;
            _signatures[(tx, ty)] = sig;
            if (_tiles.Remove((tx, ty), out var node)) node.QueueFree();
            var tile = BuildTile(w, tx, ty);
            AddChild(tile);
            _tiles[(tx, ty)] = tile;
        }
    }

    /// <summary>What a tile shows: node kinds/stages (tree stage changes only every few days), fruit, items, deposits.</summary>
    private static long Signature(World w, int tx, int ty)
    {
        var nature = w.Nature!;
        var bal = w.Content.Balance;
        unchecked
        {
            long h = 1469598103934665603;
            for (int y = ty * TileCells; y < Math.Min(w.Map.Height, (ty + 1) * TileCells); y++)
            for (int x = tx * TileCells; x < Math.Min(w.Map.Width, (tx + 1) * TileCells); x++)
            {
                var c = new Cell(x, y);
                var n = nature.At(c);
                if (n.Kind == NodeKind.None) continue;
                int state = n.Kind == NodeKind.Tree ? (int)Nature.StageOf(n, w.Tick, bal)
                    : n.Kind is NodeKind.Bush or NodeKind.Mushroom ? (w.Gatherable(c) ? 1 : 0) : n.Amount;
                h = (h ^ (x * 131 + y * 7919 + (int)n.Kind * 31 + state)) * 1099511628211;
            }
            foreach (var g in w.GroundItems)
                if (g.Cell.X / TileCells == tx && g.Cell.Y / TileCells == ty) h = (h ^ (g.Id * 17 + g.Amount.Milli)) * 1099511628211;
            foreach (var d in nature.Deposits)
                if (d.Origin.X / TileCells == tx && d.Origin.Y / TileCells == ty) h = (h ^ (d.Index * 13 + d.Units / 50)) * 1099511628211;
            return h;
        }
    }

    // ------------------------------------------------------------------------------------------------ tiles

    private sealed class Batch
    {
        public readonly List<Transform3D> Xforms = new();
        public readonly List<Color> Tints = new();
    }

    private Node3D BuildTile(World w, int tx, int ty)
    {
        var tile = new Node3D { Name = $"Tile_{tx}_{ty}" };
        var nature = w.Nature!;
        var bal = w.Content.Balance;
        var trees = new Dictionary<string, Batch>();
        var plants = new Dictionary<(string Scene, int Slot), Batch>();
        var props = new Dictionary<string, Batch>();

        for (int y = ty * TileCells; y < Math.Min(w.Map.Height, (ty + 1) * TileCells); y++)
        for (int x = tx * TileCells; x < Math.Min(w.Map.Width, (tx + 1) * TileCells); x++)
        {
            var c = new Cell(x, y);
            var n = nature.At(c);
            if (n.Kind == NodeKind.None) continue;
            int roll = Noise.Roll(x, y, 0xC0105u);
            var pos = new Vector3((x + 0.3f + (roll % 40) / 100f) * _cs, 0, (y + 0.3f + (roll / 40 % 40) / 100f) * _cs);
            pos.Y = Ground.HeightAtWorld(pos.X, pos.Z);
            var basis = Basis.Identity.Rotated(Vector3.Up, roll % 360 * Mathf.Pi / 180f);
            switch (n.Kind)
            {
                case NodeKind.Tree:
                {
                    var stage = Nature.StageOf(n, w.Tick, bal);
                    if (stage == TreeStage.Stump)
                    {
                        Add(props, E + "ENV_Stump_A.tscn", new Transform3D(basis.Scaled(Vector3.One * 0.9f), pos));
                        break;
                    }
                    string[] set = n.Species switch { TreeSpecies.Pine => Pines, TreeSpecies.Birch => Birches, _ => Oaks };
                    string scene = E + set[roll / 7 % set.Length] + ".tscn";
                    float s = stage switch { TreeStage.Mature => 0.85f + (roll % 30) / 100f, TreeStage.Young => 0.55f, _ => 0.28f };
                    int r = roll % 100;
                    var tint = n.Species == TreeSpecies.Pine || r >= 26 ? new Color(0, 0, 0, 0)
                        : new Color(Autumn[r < 14 ? 0 : r < 24 ? 1 : 2], 0.85f);
                    var batch = Get(trees, scene);
                    batch.Xforms.Add(new Transform3D(basis.Scaled(Vector3.One * s), pos));
                    batch.Tints.Add(tint);
                    break;
                }
                case NodeKind.Bush:
                    if (w.Gatherable(c)) Add(plants, (E + "ENV_Bush_B.tscn", 1), new Transform3D(basis.Scaled(Vector3.One * 0.9f), pos));
                    else Add(plants, (E + "ENV_Bush_A.tscn", 0), new Transform3D(basis.Scaled(Vector3.One * 0.85f), pos));
                    break;
                case NodeKind.Mushroom:
                    if (w.Gatherable(c)) Add(props, E + "ENV_Mushroom_A.tscn", new Transform3D(basis.Scaled(Vector3.One * 1.2f), pos));
                    break;
                case NodeKind.Stone:
                    Add(props, E + Rocks[roll % Rocks.Length] + ".tscn", new Transform3D(basis.Scaled(Vector3.One * (0.32f + n.Amount * 0.03f)), pos));
                    break;
            }
        }
        foreach (var d in nature.Deposits.Where(d => d.Origin.X / TileCells == tx && d.Origin.Y / TileCells == ty))
        {
            var center = new Vector3((d.Origin.X + 1.5f) * _cs, 0, (d.Origin.Y + 1.5f) * _cs);
            center.Y = Ground.HeightAtWorld(center.X, center.Z) - 0.2f;
            float k = d.InitialUnits > 0 ? Mathf.Clamp((float)d.Units / d.InitialUnits, 0.35f, 1f) : 1f;
            string scene = E + (d.Index % 2 == 0 ? "ENV_RockGroup_A" : "ENV_RockGroup_B") + ".tscn";
            string key = scene + "|" + d.Kind;
            Add(props, key, new Transform3D(Basis.Identity.Rotated(Vector3.Up, d.Index).Scaled(new Vector3(1.6f, 1.6f * k, 1.6f)), center));
        }
        foreach (var g in w.GroundItems.Where(g => g.Cell.X / TileCells == tx && g.Cell.Y / TileCells == ty))
        {
            var p = new Vector3((g.Cell.X + 0.5f) * _cs, 0, (g.Cell.Y + 0.5f) * _cs);
            p.Y = Ground.HeightAtWorld(p.X, p.Z);
            string id = w.Content.Resources[g.Resource].Id;
            string scene = id switch
            {
                "wood" => "res://assets/props/PROP_Carry_Log.tscn",
                "stone" => E + "ENV_Pebble_A.tscn",
                "hides" => "res://assets/props/PROP_Carry_Basket.tscn",
                _ => "res://assets/props/PROP_Carry_Sack.tscn",
            };
            float s = id == "wood" ? 4.5f : 1.6f;   // a felled log lies full length
            var basis = id == "wood" ? new Basis(Vector3.Up, g.Id * 0.7f) * new Basis(Vector3.Forward, Mathf.Pi / 2) : Basis.Identity;
            Add(props, scene, new Transform3D(basis.Scaled(Vector3.One * s), p + new Vector3(0, 0.15f, 0)));
        }

        foreach (var (scene, batch) in trees) AddTrees(tile, scene, batch);
        foreach (var ((scene, slot), batch) in plants) AddPlants(tile, scene, slot, batch.Xforms, false);
        foreach (var (key, batch) in props) AddProps(tile, key, batch.Xforms);
        return tile;
    }

    private static Batch Get<TKey>(Dictionary<TKey, Batch> d, TKey key) where TKey : notnull
    {
        if (!d.TryGetValue(key, out var b)) d[key] = b = new Batch();
        return b;
    }

    private static void Add<TKey>(Dictionary<TKey, Batch> d, TKey key, Transform3D x) where TKey : notnull => Get(d, key).Xforms.Add(x);

    // ------------------------------------------------------------------------------------------------ multimeshes

    private (Mesh Mesh, Transform3D Xform) Source(string scene, int slot, bool tree, bool plant)
    {
        string key = $"{scene}|{slot}|{tree}|{plant}";
        if (_meshes.TryGetValue(key, out var cached)) return cached;
        var src = (Godot.Collections.Dictionary)_clusters.Call("first_mesh", scene);
        if (src.Count == 0) return _meshes[key] = (new BoxMesh(), Transform3D.Identity);
        var mesh = (Mesh)src["mesh"];
        if (plant) mesh = (Mesh)_palette.Call("convert_mesh", mesh, slot, tree);
        return _meshes[key] = (mesh, (Transform3D)src["xform"]);
    }

    private static MultiMeshInstance3D Instance(Mesh mesh, IReadOnlyList<Transform3D> xforms, Transform3D local, IReadOnlyList<Color>? tints)
    {
        var mm = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D, UseCustomData = tints is not null,
            Mesh = mesh, InstanceCount = xforms.Count,
        };
        for (int i = 0; i < xforms.Count; i++)
        {
            mm.SetInstanceTransform(i, xforms[i] * local);
            if (tints is not null) mm.SetInstanceCustomData(i, tints[i]);
        }
        return new MultiMeshInstance3D { Multimesh = mm };
    }

    private void AddTrees(Node3D tile, string scene, Batch batch)
    {
        var (mesh, local) = Source(scene, 0, tree: true, plant: true);
        var near = Instance(mesh, batch.Xforms, local, batch.Tints);
        near.CastShadow = GeometryInstance3D.ShadowCastingSetting.On;
        tile.AddChild(near);
        if (!(bool)_impostors.Call("available", scene)) return;
        var quad = (Mesh)_impostors.Call("_mesh", scene);
        var far = Instance(quad, batch.Xforms, Transform3D.Identity, batch.Tints);
        far.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        far.VisibilityRangeBegin = ImpostorDistance;
        far.VisibilityRangeBeginMargin = Hysteresis;
        near.VisibilityRangeEnd = ImpostorDistance;
        near.VisibilityRangeEndMargin = Hysteresis;
        tile.AddChild(far);
    }

    private void AddPlants(Node3D parent, string scene, int slot, IReadOnlyList<Transform3D> xforms, bool shadows, float fade = 140f)
    {
        if (xforms.Count == 0) return;
        var (mesh, local) = Source(scene, slot, tree: false, plant: true);
        var node = Instance(mesh, xforms, local, null);
        node.CastShadow = shadows ? GeometryInstance3D.ShadowCastingSetting.On : GeometryInstance3D.ShadowCastingSetting.Off;
        node.VisibilityRangeEnd = fade;
        node.VisibilityRangeEndMargin = 10f;
        node.VisibilityRangeFadeMode = GeometryInstance3D.VisibilityRangeFadeModeEnum.Self;
        parent.AddChild(node);
    }

    private void AddProps(Node3D tile, string key, IReadOnlyList<Transform3D> xforms)
    {
        string scene = key.Split('|')[0];
        var (mesh, local) = Source(scene, 0, tree: false, plant: false);
        var node = Instance(mesh, xforms, local, null);
        node.CastShadow = GeometryInstance3D.ShadowCastingSetting.On;
        if (key.EndsWith("|Coal") || key.EndsWith("|Iron"))
            node.MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = key.EndsWith("|Coal") ? new Color("3a3836") : new Color("9c5a3c"), Roughness = 0.9f,
            };
        tile.AddChild(node);
    }

    // ------------------------------------------------------------------------------------------------ static dressing

    /// <summary>
    /// Once per map: rocks along the feet of the cliffs (hide the seams), ferns and tufts in the woods, flower and
    /// tuft clusters in the meadows, pebbles on the beaches. Deterministic from the cell hashes.
    /// </summary>
    public void Dress(World w)
    {
        var t = w.Terrain!;
        var cliffRocks = new Dictionary<string, List<Transform3D>>();
        var plantsList = new Dictionary<(string, int), List<Transform3D>>();
        void Put<TK>(Dictionary<TK, List<Transform3D>> d, TK k, Transform3D x) where TK : notnull
        {
            if (!d.TryGetValue(k, out var l)) d[k] = l = new List<Transform3D>();
            l.Add(x);
        }
        for (int y = 0; y < t.Height; y++)
        for (int x = 0; x < t.Width; x++)
        {
            var c = new Cell(x, y);
            if (t.IsWater(c)) continue;
            int roll = Noise.Roll(x, y, 0xD7E55u);
            var p = new Vector3((x + 0.2f + (roll % 60) / 100f) * _cs, 0, (y + 0.2f + (roll / 60 % 60) / 100f) * _cs);
            p.Y = Ground.HeightAtWorld(p.X, p.Z);
            var basis = Basis.Identity.Rotated(Vector3.Up, roll % 628 / 100f);
            bool cliffFoot = Terrain.Dirs.Any(d => t.InBounds(new Cell(x + d.Dx, y + d.Dy))
                && t.LevelAt(new Cell(x + d.Dx, y + d.Dy)) > t.LevelAt(c) && !t.IsRamp(c));
            int k = roll % 100;
            if (cliffFoot && k < 55)
                Put(cliffRocks, E + Rocks[roll / 100 % Rocks.Length] + ".tscn", new Transform3D(basis.Scaled(Vector3.One * (0.55f + k / 100f)), p - new Vector3(0, 0.25f, 0)));
            else if (t.GroundOf(c) == Sim.Map.Ground.Sand && k < 6)
                Put(cliffRocks, E + "ENV_Pebble_B.tscn", new Transform3D(basis.Scaled(Vector3.One * 1.4f), p));
            else if (t.ForestAt(c) > 60 && k < 18 && w.Nature!.At(c).Kind == NodeKind.None)
                Put(plantsList, (E + (k < 9 ? "ENV_Fern_A" : "ENV_Grass_D") + ".tscn", 0), new Transform3D(basis.Scaled(Vector3.One * 0.8f), p));
            else if (t.ForestAt(c) == 0 && t.GroundOf(c) == Sim.Map.Ground.Grass && !t.IsRamp(c))
            {
                int patch = Noise.Fbm(x, y, 10, 2, 0x5EEDu);
                if (patch > Noise.One * 62 / 100 && k < 22)
                    Put(plantsList, (E + (k < 8 ? "ENV_FlowerSingle_A" : k < 12 ? "ENV_FlowerSingle_B" : "ENV_Grass_B") + ".tscn", k < 8 ? 0 : 1),
                        new Transform3D(basis.Scaled(Vector3.One * (k < 12 ? 0.14f : 0.9f)), p));
                else if (k < 3)
                    Put(plantsList, (E + "ENV_Clover_A.tscn", 0), new Transform3D(basis.Scaled(Vector3.One * 0.9f), p));
            }
        }
        var dressing = new Node3D { Name = "Dressing" };
        AddChild(dressing);
        foreach (var (scene, xs) in cliffRocks) AddProps(dressing, scene, xs);
        foreach (var ((scene, slot), xs) in plantsList) AddPlants(dressing, scene, slot, xs, false, scene.Contains("Flower") ? 90f : 120f);
    }
}
