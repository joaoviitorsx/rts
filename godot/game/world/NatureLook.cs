using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Ironvale.Sim;
using Ironvale.Sim.Map;
using Noise = Ironvale.Sim.Map.Noise;

namespace Ironvale.Game.World3D;

/// <summary>
/// Living nature of a generated map with the whole Kenney Nature Kit 2.1 (CC0, art direction of 09/10/2026), recoloured
/// to the cozy palette (KenneyPalette). Every tree of the sim is one instance — species, stage, a stump when felled,
/// "_fall" crowns for the autumn mix and "_dark" ones for depth — and bushes (red berries in season), mushrooms, loose
/// stones, deposits and what lies on the ground use the kits' models. Static dressing (rocks at cliff feet, lilies on
/// lakes, logs, mushrooms and bushes in the woods, flower and grass patches in the meadows) is scattered once.
/// 64-m tiles are rebuilt only when their content changes.
/// </summary>
public partial class NatureLook : Node3D
{
    private const int TileCells = 32;

    private const string K = "res://assets/environment/kenney/K_";
    private const string KS = "res://assets/props/kenney/KS_";
    // Kenney Nature Kit 2.1 (whole kit, decision 09/10/2026): shapes per species; "_dark" adds depth, "_fall" is the
    // autumn mix (≈ 25 % of broadleaf crowns, like the Koastalia reference).
    private static readonly string[] Oaks = { "tree_default", "tree_oak", "tree_fat", "tree_detailed", "tree_simple", "tree_tall", "tree_plateau" };
    private static readonly string[] Pines = { "tree_pineRoundA", "tree_pineRoundB", "tree_pineRoundC", "tree_pineRoundD", "tree_pineTallA",
        "tree_pineTallB", "tree_pineDefaultA", "tree_pineDefaultB" };
    private static readonly string[] Birches = { "tree_thin", "tree_cone" };
    private static readonly string[] Stumps = { "stump_round", "stump_old", "stump_roundDetailed" };
    private static readonly string[] SmallRocks = { "rock_smallA", "rock_smallB", "rock_smallC", "rock_smallD", "rock_smallE", "stone_smallA", "stone_smallC" };
    private static readonly string[] TallRocks = { "rock_tallA", "rock_tallB", "rock_tallC", "rock_tallD", "rock_tallE", "rock_tallF" };

    private SimHost _host = null!;
    private float _cs;
    private GDScript _clusters = null!, _palette = null!;
    private readonly Dictionary<(int, int), Node3D> _tiles = new();
    private readonly Dictionary<(int, int), long> _signatures = new();
    private readonly Dictionary<string, (Mesh Mesh, Transform3D Xform)> _meshes = new();
    private double _timer;

    public void Init(SimHost host, float cellSize)
    {
        _host = host;
        _cs = cellSize;
        _clusters = GD.Load<GDScript>("res://game/vegetation/Clusters.gd");
        _palette = GD.Load<GDScript>("res://game/visual/KenneyPalette.gd");
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
    }

    private Node3D BuildTile(World w, int tx, int ty)
    {
        var tile = new Node3D { Name = $"Tile_{tx}_{ty}" };
        var nature = w.Nature!;
        var bal = w.Content.Balance;
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
                        Add(props, K + Stumps[roll % Stumps.Length] + ".tscn", new Transform3D(basis.Scaled(Vector3.One * 0.9f), pos));
                        break;
                    }
                    string[] set = n.Species switch { TreeSpecies.Pine => Pines, TreeSpecies.Birch => Birches, _ => Oaks };
                    string model = stage == TreeStage.Sapling ? "tree_small" : set[roll / 7 % set.Length];
                    int r = roll % 100;
                    if (n.Species != TreeSpecies.Pine && model != "tree_cone" || model == "tree_cone")
                        model += r < 25 ? "_fall" : r < 55 ? "_dark" : "";
                    if (n.Species == TreeSpecies.Pine && model.EndsWith("_dark")) model = model[..^5];
                    if (model == "tree_fat_dark") model = "tree_fat_darkh";   // the kit's own file name
                    float s = stage switch { TreeStage.Mature => 0.85f + (roll % 30) / 100f, TreeStage.Young => 0.6f, _ => 0.7f };
                    Add(props, K + model + ".tscn", new Transform3D(basis.Scaled(Vector3.One * s), pos));
                    break;
                }
                case NodeKind.Bush:
                    Add(props, K + (roll % 3 == 0 ? "plant_bushDetailed" : "plant_bushLarge") + ".tscn", new Transform3D(basis.Scaled(Vector3.One * 0.9f), pos));
                    if (w.Gatherable(c))   // berries in season: little red dots on the bush
                        Add(props, K + "flower_redA.tscn", new Transform3D(basis.Scaled(Vector3.One * 1.2f), pos + new Vector3(0, 0.55f, 0)));
                    break;
                case NodeKind.Mushroom:
                    if (w.Gatherable(c)) Add(props, K + (roll % 2 == 0 ? "mushroom_redGroup" : "mushroom_tanGroup") + ".tscn", new Transform3D(basis, pos));
                    break;
                case NodeKind.Stone:
                    Add(props, K + SmallRocks[roll % SmallRocks.Length] + ".tscn", new Transform3D(basis.Scaled(Vector3.One * (0.8f + n.Amount * 0.06f)), pos));
                    break;
            }
        }
        foreach (var d in nature.Deposits.Where(d => d.Origin.X / TileCells == tx && d.Origin.Y / TileCells == ty))
        {
            // A cluster of tall rocks over the 3×3 cells (lower as it is mined); coal and iron get a tinted stone.
            float k = d.InitialUnits > 0 ? Mathf.Clamp((float)d.Units / d.InitialUnits, 0.35f, 1f) : 1f;
            for (int i = 0; i < 5; i++)
            {
                var p = new Vector3((d.Origin.X + 0.6f + (i * 37 % 19) / 10f) * _cs, 0, (d.Origin.Y + 0.6f + (i * 53 % 17) / 9f) * _cs);
                p.Y = Ground.HeightAtWorld(p.X, p.Z) - 0.2f;
                string key = K + TallRocks[(d.Index + i) % TallRocks.Length] + ".tscn" + (d.Kind == DepositKind.Outcrop ? "" : "|" + d.Kind);
                Add(props, key, new Transform3D(Basis.Identity.Rotated(Vector3.Up, d.Index + i).Scaled(new Vector3(1.4f, 1.6f * k, 1.4f)), p));
            }
        }
        foreach (var g in w.GroundItems.Where(g => g.Cell.X / TileCells == tx && g.Cell.Y / TileCells == ty))
        {
            var p = new Vector3((g.Cell.X + 0.5f) * _cs, 0, (g.Cell.Y + 0.5f) * _cs);
            p.Y = Ground.HeightAtWorld(p.X, p.Z);
            string id = w.Content.Resources[g.Resource].Id;
            string scene = id switch
            {
                "wood" => K + "log_large.tscn",
                "stone" => KS + "resource-stone.tscn",
                "hides" => KS + "bedroll-packed.tscn",
                "firewood" => K + "log_stack.tscn",
                _ => KS + "box.tscn",
            };
            float s = 1f;
            var basis = new Basis(Vector3.Up, g.Id * 0.7f);
            Add(props, scene, new Transform3D(basis.Scaled(Vector3.One * s), p));
        }

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

    /// <summary>First mesh of an asset scene (with the scene's scale) as a copy recoloured to the cozy palette.</summary>
    private (Mesh Mesh, Transform3D Xform) Source(string scene)
    {
        if (_meshes.TryGetValue(scene, out var cached)) return cached;
        if (!ResourceLoader.Exists(scene))
        {
            GD.PushWarning($"NatureLook: missing {scene}");
            return _meshes[scene] = (new BoxMesh(), Transform3D.Identity);
        }
        var src = (Godot.Collections.Dictionary)_clusters.Call("first_mesh", scene);
        if (src.Count == 0) return _meshes[scene] = (new BoxMesh(), Transform3D.Identity);
        var mesh = (Mesh)((Mesh)src["mesh"]).Duplicate();
        for (int i = 0; i < mesh.GetSurfaceCount(); i++)
        {
            var original = mesh.SurfaceGetMaterial(i);
            var mapped = _palette.Call("material", original?.ResourceName ?? "").AsGodotObject() as Material;
            if (mapped is not null) mesh.SurfaceSetMaterial(i, mapped);
        }
        return _meshes[scene] = (mesh, (Transform3D)src["xform"]);
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

    private void AddProps(Node3D tile, string key, IReadOnlyList<Transform3D> xforms, float fade = 0f, bool shadows = true)
    {
        if (xforms.Count == 0) return;
        string scene = key.Split('|')[0];
        var (mesh, local) = Source(scene);
        var node = Instance(mesh, xforms, local, null);
        node.CastShadow = shadows ? GeometryInstance3D.ShadowCastingSetting.On : GeometryInstance3D.ShadowCastingSetting.Off;
        if (fade > 0)
        {
            node.VisibilityRangeEnd = fade;
            node.VisibilityRangeEndMargin = 10f;
            node.VisibilityRangeFadeMode = GeometryInstance3D.VisibilityRangeFadeModeEnum.Self;
        }
        if (key.EndsWith("|Coal") || key.EndsWith("|Iron"))
            node.MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = key.EndsWith("|Coal") ? new Color("4a4744") : new Color("a8634a"), Roughness = 0.9f,
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
        var dress = new Dictionary<string, List<Transform3D>>();
        var small = new Dictionary<string, List<Transform3D>>();   // fades out at distance
        void Put(Dictionary<string, List<Transform3D>> d, string k, Transform3D x)
        {
            if (!d.TryGetValue(k, out var l)) d[k] = l = new List<Transform3D>();
            l.Add(x);
        }
        string[] flowers = { "flower_purpleA", "flower_purpleB", "flower_redA", "flower_redB", "flower_yellowA", "flower_yellowB", "flower_yellowC" };
        string[] grass = { "grass", "grass_large", "grass_leafs", "grass_leafsLarge", "plant_flatShort" };
        for (int y = 0; y < t.Height; y++)
        for (int x = 0; x < t.Width; x++)
        {
            var c = new Cell(x, y);
            int roll = Noise.Roll(x, y, 0xD7E55u);
            int k = roll % 100;
            var p = new Vector3((x + 0.2f + (roll % 60) / 100f) * _cs, 0, (y + 0.2f + (roll / 60 % 60) / 100f) * _cs);
            p.Y = Ground.HeightAtWorld(p.X, p.Z);
            var basis = Basis.Identity.Rotated(Vector3.Up, roll % 628 / 100f);
            if (t.IsWater(c))
            {
                if (t.DepthAt(c) <= 2 && k < 12 && t.LevelAt(c) > 0)   // lilies on lakes
                    Put(small, K + (k < 6 ? "lily_large" : "lily_small") + ".tscn", new Transform3D(basis, new Vector3(p.X, Ground.CellHeight(x, y) + 0.03f, p.Z)));
                continue;
            }
            bool cliffFoot = !t.IsRamp(c) && Terrain.Dirs.Any(d => t.InBounds(new Cell(x + d.Dx, y + d.Dy)) && t.LevelAt(new Cell(x + d.Dx, y + d.Dy)) > t.LevelAt(c));
            bool cliffTop = Terrain.Dirs.Any(d => t.InBounds(new Cell(x + d.Dx, y + d.Dy)) && !t.IsWater(new Cell(x + d.Dx, y + d.Dy))
                && t.LevelAt(new Cell(x + d.Dx, y + d.Dy)) < t.LevelAt(c) && !t.IsRamp(new Cell(x + d.Dx, y + d.Dy)));
            bool free = w.Nature!.At(c).Kind == NodeKind.None && w.Nature.DepositAt(c) is null;
            if (cliffFoot && k < 45)
                Put(dress, K + SmallRocks[roll / 100 % SmallRocks.Length] + ".tscn", new Transform3D(basis.Scaled(Vector3.One * (0.9f + k / 60f)), p));
            else if (cliffTop && k < 10 && free)
                Put(small, K + grass[roll / 100 % grass.Length] + ".tscn", new Transform3D(basis, p));
            else if (t.GroundOf(c) == Sim.Map.Ground.Sand)
            {
                if (k < 5) Put(dress, KS + (k < 3 ? "rock-sand-a" : "rock-sand-b") + ".tscn", new Transform3D(basis.Scaled(Vector3.One * 0.6f), p));
            }
            else if (t.ForestAt(c) > 40 && free)
            {
                if (k < 4) Put(dress, K + (k < 2 ? "log" : "stump_oldTall") + ".tscn", new Transform3D(basis, p));
                else if (k < 14) Put(small, K + (k < 9 ? "plant_bushSmall" : "grass_leafsLarge") + ".tscn", new Transform3D(basis, p));
                else if (k < 17) Put(small, K + (k < 16 ? "mushroom_tan" : "mushroom_red") + ".tscn", new Transform3D(basis, p));
            }
            else if (t.ForestAt(c) == 0 && free && !t.IsRamp(c))
            {
                int patch = Noise.Fbm(x, y, 10, 2, 0x5EEDu);
                if (patch > Noise.One * 60 / 100 && k < 30)
                    Put(small, K + (k < 14 ? flowers[roll / 100 % flowers.Length] : grass[roll / 100 % grass.Length]) + ".tscn", new Transform3D(basis, p));
                else if (k < 4)
                    Put(small, K + grass[roll / 100 % grass.Length] + ".tscn", new Transform3D(basis, p));
            }
        }
        var node = new Node3D { Name = "Dressing" };
        AddChild(node);
        foreach (var (scene, xs) in dress) AddProps(node, scene, xs, 220f);
        foreach (var (scene, xs) in small) AddProps(node, scene, xs, 110f, shadows: false);
    }
}
