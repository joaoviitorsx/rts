using System.Collections.Generic;
using System.Linq;
using Godot;
using Ironvale.Sim;
using Ironvale.Sim.Map;
using Ironvale.Sim.Population;
using Ironvale.Sim.Time;
using Noise = Ironvale.Sim.Map.Noise;

namespace Ironvale.Game.Visual;

/// <summary>
/// Briefing step 2 ("jogável com primitivas"): a plain but readable picture of a generated map — terraces as flat
/// steps with stone walls, water by depth, sand; trees, stumps, bushes, mushrooms, stones and deposits as MultiMesh
/// primitives; logs and carcasses on the ground; animals and the ox. The real look (Terrain3D, contour cliffs,
/// Stylized Nature forests, Quaternius animals) is step 4. Reads the world only.
/// </summary>
public partial class WorldPrimitives : Node3D
{
    private SimHost _host = null!;
    private VisualCatalog _catalog = null!;
    private readonly Dictionary<string, MultiMeshInstance3D> _layers = new();
    private int _natureVersion = -1;
    private string _groundSig = "";
    private Season _season;
    private double _timer;
    private readonly Dictionary<int, Node3D> _creatures = new();
    private readonly Dictionary<int, (Cell Next, int Total)> _steps = new();
    private readonly Dictionary<int, AnimationPlayer?> _players = new();

    private static readonly Color GrassLow = new("6e9e35");
    private static readonly Color Sand = new("e4d09b");
    private static readonly Color Cliff = new("d6d3c6");
    private static readonly Color RampColor = new("b89a6a");
    private static readonly Color WaterShallow = new("7dd8d4");
    private static readonly Color WaterDeep = new("1f87aa");

    /// <summary>Off when the real look (NatureLook) draws trees, stones and items; animals and the ox stay here.</summary>
    public bool NatureVisible { get; set; } = true;

    public void Init(SimHost host, VisualCatalog catalog)
    {
        _host = host;
        _catalog = catalog;
        Name = "WorldPrimitives";
    }

    // ------------------------------------------------------------------------------------------------ terrain

    public static MeshInstance3D BuildTerrain(Terrain t, float cs)
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        for (int y = 0; y < t.Height; y++)
        for (int x = 0; x < t.Width; x++)
        {
            var c = new Cell(x, y);
            float h = Ground.CellHeight(x, y);
            Quad(st, new Vector3(x * cs, h, y * cs), new Vector3(cs, 0, 0), new Vector3(0, 0, cs), TopColor(t, c), Vector3.Up);
            // Walls toward lower neighbours (east and south sides of this cell, west/north of the next).
            foreach (var (dx, dy) in Terrain.Dirs)
            {
                int nx = x + dx, ny = y + dy;
                if (nx < 0 || ny < 0 || nx >= t.Width || ny >= t.Height) continue;
                float nh = Ground.CellHeight(nx, ny);
                if (nh >= h - 0.01f) continue;
                bool cliff = h - nh > Ground.LevelHeight * 0.5f && !t.IsWater(c);
                var color = cliff ? Cliff : t.IsWater(c) ? WaterShallow : RampColor;
                Wall(st, x, y, dx, dy, nh, h, cs, color);
            }
        }
        st.GenerateNormals();
        var mesh = st.Commit();
        return new MeshInstance3D
        {
            Name = "TerrainPrimitive",
            Mesh = mesh,
            MaterialOverride = new StandardMaterial3D
            {
                VertexColorUseAsAlbedo = true, VertexColorIsSrgb = true, Roughness = 1f,
                CullMode = BaseMaterial3D.CullModeEnum.Disabled,
            },
        };
    }

    private static Color TopColor(Terrain t, Cell c)
    {
        if (t.IsWater(c)) return WaterShallow.Lerp(WaterDeep, Mathf.Clamp(t.DepthAt(c) / 6f, 0, 1));
        if (t.GroundOf(c) == Sim.Map.Ground.Sand) return Sand;
        if (t.IsRamp(c)) return RampColor;
        float k = t.LevelAt(c) * 0.06f + (Noise.Roll(c.X, c.Y, 7u) % 100) / 2500f;
        return GrassLow.Lightened(k);
    }

    private static void Quad(SurfaceTool st, Vector3 o, Vector3 u, Vector3 v, Color color, Vector3 normal)
    {
        st.SetColor(color);
        st.AddVertex(o); st.AddVertex(o + u); st.AddVertex(o + v);
        st.AddVertex(o + u); st.AddVertex(o + u + v); st.AddVertex(o + v);
    }

    private static void Wall(SurfaceTool st, int x, int y, int dx, int dy, float low, float high, float cs, Color color)
    {
        // Edge between this cell and the neighbour in (dx, dy), as a vertical quad facing the neighbour.
        Vector3 a, b;
        if (dx == 1) { a = new Vector3((x + 1) * cs, 0, y * cs); b = new Vector3((x + 1) * cs, 0, (y + 1) * cs); }
        else if (dx == -1) { a = new Vector3(x * cs, 0, (y + 1) * cs); b = new Vector3(x * cs, 0, y * cs); }
        else if (dy == 1) { a = new Vector3((x + 1) * cs, 0, (y + 1) * cs); b = new Vector3(x * cs, 0, (y + 1) * cs); }
        else { a = new Vector3(x * cs, 0, y * cs); b = new Vector3((x + 1) * cs, 0, y * cs); }
        var up = new Vector3(0, high, 0);
        var dn = new Vector3(0, low, 0);
        st.SetColor(color);
        st.AddVertex(a + up); st.AddVertex(a + dn); st.AddVertex(b + up);
        st.AddVertex(b + up); st.AddVertex(a + dn); st.AddVertex(b + dn);
    }

    // ------------------------------------------------------------------------------------------------ nature

    public override void _Process(double delta)
    {
        if (_host is null || _host.IsBusy || _host.World.Nature is not { } nature) return;
        var w = _host.World;
        UpdateCreatures(w);
        foreach (var layer in _layers.Values) layer.Visible = NatureVisible;
        if (!NatureVisible) return;
        _timer -= delta;
        if (_timer > 0) return;
        _timer = 0.5;
        string groundSig = string.Join(',', w.GroundItems.Select(g => $"{g.Id}:{g.Amount.Milli}"));
        var season = w.Calendar.Season;
        if (nature.Version == _natureVersion && groundSig == _groundSig && season == _season) return;
        _natureVersion = nature.Version;
        _groundSig = groundSig;
        _season = season;
        RebuildNature(w, nature);
    }

    private void RebuildNature(World w, Nature nature)
    {
        float cs = _catalog.CellSize;
        var canopies = new List<(Transform3D, Color)>();
        var trunks = new List<(Transform3D, Color)>();
        var round = new List<(Transform3D, Color)>();
        var blocks = new List<(Transform3D, Color)>();
        var bal = w.Content.Balance;
        var t = w.Terrain!;
        for (int y = 0; y < t.Height; y++)
        for (int x = 0; x < t.Width; x++)
        {
            var c = new Cell(x, y);
            var node = nature.At(c);
            if (node.Kind == NodeKind.None) continue;
            int roll = Noise.Roll(x, y, 0xC0105u);
            var p = _catalog.CellToWorld(x, y) + new Vector3(cs * (0.3f + (roll % 40) / 100f), 0, cs * (0.3f + (roll / 40 % 40) / 100f));
            switch (node.Kind)
            {
                case NodeKind.Tree:
                {
                    var stage = Nature.StageOf(node, w.Tick, bal);
                    float s = stage switch { TreeStage.Mature => 1f, TreeStage.Young => 0.65f, TreeStage.Sapling => 0.35f, _ => 0f };
                    if (stage == TreeStage.Stump)
                    {
                        trunks.Add((new Transform3D(Basis.Identity.Scaled(new Vector3(1.1f, 0.18f, 1.1f)), p + new Vector3(0, 0.1f, 0)), new Color("8a6a45")));
                        break;
                    }
                    trunks.Add((new Transform3D(Basis.Identity.Scaled(new Vector3(s, s, s)), p + new Vector3(0, 0.6f * s, 0)), new Color("6b4a2e")));
                    canopies.Add((new Transform3D(Basis.Identity.Scaled(new Vector3(s, s * (node.Species == TreeSpecies.Pine ? 1.5f : 1f), s)),
                        p + new Vector3(0, 2.0f * s, 0)), CanopyColor(node.Species, roll)));
                    break;
                }
                case NodeKind.Bush:
                    bool fruit = w.Gatherable(c);
                    round.Add((new Transform3D(Basis.Identity.Scaled(new Vector3(0.9f, 0.6f, 0.9f)), p + new Vector3(0, 0.35f, 0)),
                        fruit ? new Color("c2453f") : new Color("4f7d34")));
                    break;
                case NodeKind.Mushroom:
                    if (w.Gatherable(c))
                        round.Add((new Transform3D(Basis.Identity.Scaled(new Vector3(0.35f, 0.25f, 0.35f)), p + new Vector3(0, 0.15f, 0)), new Color("efe6d0")));
                    break;
                case NodeKind.Stone:
                    blocks.Add((new Transform3D(Basis.Identity.Rotated(Vector3.Up, roll % 90 * 0.07f).Scaled(new Vector3(0.7f, 0.45f, 0.6f)),
                        p + new Vector3(0, 0.2f, 0)), new Color("9a9a96")));
                    break;
            }
        }
        foreach (var d in nature.Deposits)
        {
            var center = _catalog.CellToWorld(d.Origin.X, d.Origin.Y) + new Vector3(1.5f, 0, 1.5f) * cs;
            var color = d.Kind switch { DepositKind.Outcrop => new Color("8c8c90"), DepositKind.Coal => new Color("2b2b2b"), _ => new Color("a2553a") };
            float k = d.InitialUnits > 0 ? Mathf.Clamp((float)d.Units / d.InitialUnits, 0.2f, 1f) : 1f;
            blocks.Add((new Transform3D(Basis.Identity.Scaled(new Vector3(4.5f, 2.2f * k, 4.5f)), center + new Vector3(0, 1.1f * k, 0)), color));
        }
        foreach (var g in w.GroundItems)
        {
            var p = _catalog.CellToWorld(g.Cell.X, g.Cell.Y) + new Vector3(cs / 2, 0.25f, cs / 2);
            var color = _catalog.ResourceColor(w.Content.Resources[g.Resource].Id);
            float size = Mathf.Clamp(g.Amount.WholeUnits / 6f, 0.4f, 1.4f);
            blocks.Add((new Transform3D(Basis.Identity.Scaled(new Vector3(size * 1.6f, 0.4f, 0.5f)), p), color));
        }
        Layer("trunks", new CylinderMesh { TopRadius = 0.15f, BottomRadius = 0.2f, Height = 1.2f, RadialSegments = 6, Rings = 1 }, trunks);
        Layer("canopies", new SphereMesh { Radius = 1.1f, Height = 2.0f, RadialSegments = 8, Rings = 5 }, canopies);
        Layer("round", new SphereMesh { Radius = 0.5f, Height = 1f, RadialSegments = 8, Rings = 4 }, round);
        Layer("blocks", new BoxMesh { Size = Vector3.One }, blocks);
    }

    private static Color CanopyColor(TreeSpecies species, int roll)
    {
        int r = roll % 100;
        return species switch
        {
            TreeSpecies.Pine => new Color("2f5a3a"),
            TreeSpecies.Birch => new Color("8db84a"),
            _ => r < 14 ? new Color("e0952c") : r < 24 ? new Color("e6c142") : r < 26 ? new Color("c4552f") : new Color("4f8a33"),
        };
    }

    private void Layer(string key, Mesh mesh, List<(Transform3D T, Color C)> items)
    {
        if (!_layers.TryGetValue(key, out var node))
        {
            node = new MultiMeshInstance3D { Name = key, CastShadow = key == "canopies" ? GeometryInstance3D.ShadowCastingSetting.On : GeometryInstance3D.ShadowCastingSetting.Off };
            node.MaterialOverride = new StandardMaterial3D { VertexColorUseAsAlbedo = true, VertexColorIsSrgb = true, Roughness = 0.95f };
            AddChild(node);
            _layers[key] = node;
        }
        var mm = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform3D, UseColors = true, Mesh = mesh, InstanceCount = items.Count };
        for (int i = 0; i < items.Count; i++)
        {
            mm.SetInstanceTransform(i, items[i].T);
            mm.SetInstanceColor(i, items[i].C);
        }
        node.Multimesh = mm;
    }

    // ------------------------------------------------------------------------------------------------ creatures

    /// <summary>Animals and the ox (colonists use the villager models, in WorldView). Deer, wolf and ox are the
    /// Quaternius models (assets/characters/animals); the rabbit has no model yet and stays a primitive.</summary>
    private void UpdateCreatures(World w)
    {
        var alive = new HashSet<int>();
        float alpha = _host.TickAlpha;
        foreach (var a in w.Animals)
        {
            alive.Add(a.Id);
            var node = a.Kind switch
            {
                FaunaKind.Deer => Creature(a.Id, "ANM_Deer"),
                FaunaKind.Wolf => Creature(a.Id, "ANM_Wolf"),
                _ => Creature(a.Id, ("capsule", new Vector3(0.35f, 0.6f, 0.35f), new Color("efeae0"))),
            };
            Move(node, Interpolate(a.Id, a.Pos, a.Next, a.StepTicks, alpha));
            Animate(a.Id, node, a.IsMoving ? (a.State == AnimalState.Fleeing ? "Gallop" : "Walk")
                : a.State == AnimalState.Grazing && a.Id % 3 != 0 ? "Eating" : "Idle");
        }
        foreach (var u in w.Units.Where(u => u.Kind == UnitKind.Ox))
        {
            alive.Add(u.Id);
            var node = Creature(u.Id, "ANM_Ox");
            Move(node, Interpolate(u.Id, u.Pos, u.Next, u.StepTicks, alpha));
            Animate(u.Id, node, u.Next != u.Pos ? "Walk" : "Idle");
        }
        foreach (var id in _creatures.Keys.Where(id => !alive.Contains(id)).ToList())
        {
            _creatures[id].QueueFree();
            _creatures.Remove(id);
            _steps.Remove(id);
            _players.Remove(id);
        }
    }

    private static void Move(Node3D node, Vector3 pos)
    {
        var dir = pos - node.Position;
        node.Position = pos;
        if (dir.X * dir.X + dir.Z * dir.Z > 1e-5f) node.Rotation = new Vector3(0, Mathf.Atan2(dir.X, dir.Z), 0);
    }

    private void Animate(int id, Node3D node, string clip)
    {
        if (!_players.TryGetValue(id, out var ap))
            _players[id] = ap = node.FindChild("AnimationPlayer", true, false) as AnimationPlayer;
        if (ap is null || ap.CurrentAnimation == clip || !ap.HasAnimation(clip)) return;
        var anim = ap.GetAnimation(clip);
        anim.LoopMode = Animation.LoopModeEnum.Linear;
        ap.Play(clip, 0.25);
        if (ap.CurrentAnimationPosition == 0) ap.Seek(id * 0.37 % anim.Length);   // herds out of step
    }

    private Node3D Creature(int id, string asset)
    {
        if (_creatures.TryGetValue(id, out var node)) return node;
        node = GD.Load<PackedScene>($"res://assets/characters/animals/{asset}.tscn").Instantiate<Node3D>();
        node.Name = $"Creature{id}";
        AddChild(node);
        _creatures[id] = node;
        return node;
    }

    private Node3D Creature(int id, (string Kind, Vector3 Size, Color Color) look)
    {
        if (_creatures.TryGetValue(id, out var node)) return node;
        node = new Node3D { Name = $"Creature{id}" };
        var body = _catalog.Primitive(look.Kind, look.Size, look.Color);
        if (look.Kind == "capsule") body.RotationDegrees = new Vector3(90, 0, 0);
        body.Position = new Vector3(0, look.Size.X * 0.6f, 0);
        node.AddChild(body);
        AddChild(node);
        _creatures[id] = node;
        return node;
    }

    /// <summary>Between the cell it stands on and the one it walks into (the step's total length is remembered here).</summary>
    public Vector3 Interpolate(int id, Cell pos, Cell next, int stepTicks, float alpha)
    {
        float cs = _catalog.CellSize;
        var from = _catalog.CellToWorld(pos.X, pos.Y) + new Vector3(cs / 2, 0, cs / 2);
        if (next == pos) return from;
        if (!_steps.TryGetValue(id, out var s) || s.Next != next) _steps[id] = s = (next, Mathf.Max(stepTicks, 1));
        float t = Mathf.Clamp(1f - (stepTicks - alpha) / s.Total, 0f, 1f);
        var to = _catalog.CellToWorld(next.X, next.Y) + new Vector3(cs / 2, 0, cs / 2);
        return from.Lerp(to, t);
    }
}
