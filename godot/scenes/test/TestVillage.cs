using System.Collections.Generic;
using System.Linq;
using Godot;
using Ironvale.Game.Visual;

namespace Ironvale.Game.Test;

/// <summary>
/// TEST_VILLAGE_01 runtime: paints the terrain (dirt under buildings, road), spawns 20 animated villagers
/// (walkers on the road, some carrying, plus workers at their stations), sets the official camera and lets you
/// compare material styles.
/// Keys: F6/F7/F8 = style A/B/C · Z = cycle zoom · G = ground palette.
/// Args (after "--"): --zoom=near|mid|far --style=A|B|C --ground=moss|meadow
/// </summary>
public partial class TestVillage : Node3D
{
    private const string MaleScene = "res://assets/characters/CHR_Villager_Base.tscn";
    private const string FemaleScene = "res://assets/characters/CHR_Villager_Base_F.tscn";
    private static readonly Dictionary<string, float> Zooms = new() { ["near"] = 26f, ["mid"] = 60f, ["far"] = 125f };

    private sealed class Walker
    {
        public required ModularCharacter Character;
        public required Curve3D Curve;
        public float Offset;
        public float Direction;
        public float Speed;
        public float Lane;
    }

    private readonly List<Walker> _walkers = new();
    private CameraRig _camera = null!;
    private Label _styleLabel = null!;
    private int _zoomIndex = 1;
    private string _ground = "moss";
    private MaterialStyle.Style _style;
    private readonly RandomNumberGenerator _rng = new() { Seed = 7 };

    public override void _Ready()
    {
        PerfProbe.AttachIfRequested(this);
        var argv = OS.GetCmdlineUserArgs();
        if (!argv.Contains("--no-terrain")) PaintTerrain(scatter: !argv.Contains("--no-scatter"));
        if (!argv.Contains("--no-villagers")) SpawnVillagers();
        if (argv.Contains("--no-shadows")) GetNode<DirectionalLight3D>("Sun").ShadowEnabled = false;

        _camera = GetNode<CameraRig>("CameraRig");
        _camera.EdgePan = false;
        _camera.SetBounds(new Rect2(60, 95, 145, 75));

        var ui = new CanvasLayer();
        AddChild(ui);
        var margin = new MarginContainer();
        margin.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopLeft);
        ui.AddChild(margin);
        _styleLabel = new Label();
        margin.AddChild(_styleLabel);

        var args = OS.GetCmdlineUserArgs();
        string zoom = args.FirstOrDefault(a => a.StartsWith("--zoom="))?[7..] ?? "mid";
        _zoomIndex = Mathf.Max(0, Zooms.Keys.ToList().IndexOf(zoom));
        SetZoom(_zoomIndex);
        _ground = args.FirstOrDefault(a => a.StartsWith("--ground="))?[9..] ?? "moss";
        GetNode("VillageTerrain").Call("apply_palette", _ground);
        string style = args.FirstOrDefault(a => a.StartsWith("--style="))?[8..] ?? "A";
        SetStyle(style switch { "B" => MaterialStyle.Style.CozyMatte, "C" => MaterialStyle.Style.Painted, _ => MaterialStyle.Style.Original });
    }

    private void SetZoom(int index)
    {
        _zoomIndex = index % Zooms.Count;
        _camera.FocusOn(new Vector3(130, 0, 128), Zooms.Values.ElementAt(_zoomIndex));
    }

    private void SetStyle(MaterialStyle.Style style)
    {
        var env = GetNode<WorldEnvironment>("WorldEnvironment").Environment;
        foreach (var group in new[] { "Buildings", "Nature", "Props", "Villagers" })
            MaterialStyle.Apply(GetNode(group), style, env);
        _style = style;
        UpdateLabel();
    }

    private void UpdateLabel()
    {
        _styleLabel.Text = $"Material: {MaterialStyle.Describe(_style)} · Chão: {_ground}   [F6 A · F7 B · F8 C · Z zoom · G chão]";
    }

    public override void _UnhandledKeyInput(InputEvent e)
    {
        if (e is not InputEventKey { Pressed: true, Echo: false } key) return;
        switch (key.Keycode)
        {
            case Key.F6: SetStyle(MaterialStyle.Style.Original); break;
            case Key.F7: SetStyle(MaterialStyle.Style.CozyMatte); break;
            case Key.F8: SetStyle(MaterialStyle.Style.Painted); break;
            case Key.Z: SetZoom(_zoomIndex + 1); break;
            case Key.G:
                _ground = _ground == "moss" ? "meadow" : "moss";
                GetNode("VillageTerrain").Call("apply_palette", _ground);
                UpdateLabel();
                break;
        }
    }

    private void PaintTerrain(bool scatter)
    {
        var terrain = GetNode("VillageTerrain");
        if (!terrain.HasMethod("paint_rect") || !(bool)terrain.Call("is_available")) return;
        const int dirt = 1, path = 2, plowed = 3;
        foreach (var node in GetNode("Buildings").GetChildren().OfType<Node3D>())
        {
            var size = node.GetMeta("footprint", new Vector2(4, 4)).AsVector2();
            if (Mathf.RoundToInt(node.RotationDegrees.Y / 90f) % 2 != 0) size = new Vector2(size.Y, size.X);
            bool field = node.Name.ToString().StartsWith("BLD_Field");
            terrain.Call("paint_rect", node.Position, size + (field ? new Vector2(1, 1) : new Vector2(1.5f, 1.5f)), field ? plowed : dirt);
        }
        foreach (var road in new[] { "Road", "RoadHall" })
        {
            var curve = GetNode<Path3D>(road).Curve;
            terrain.Call("paint_path", curve.GetBakedPoints(), 3.2f, path);
        }
        terrain.Call("commit");
        if (!scatter) return;

        // Vegetation recipe: meadow patches, flower meadows, path/building edges, undergrowth under trees.
        var trees = new Godot.Collections.Array();
        foreach (var t in GetNode("Nature").GetChildren().OfType<Node3D>())
            if (t.Name.ToString() is var n && (n.StartsWith("ENV_Oak") || n.StartsWith("ENV_Pine") || n.StartsWith("ENV_Twisted")))
                trees.Add(t.Position);
        var specs = new Godot.Collections.Array
        {
            Spec("ENV_Grass_A", 2600, "cluster", clusters: 45, radius: 7),
            Spec("ENV_Grass_C", 2000, "cluster", clusters: 40, radius: 6),
            Spec("ENV_Grass_B", 1100, "cluster", clusters: 25, radius: 4),
            Spec("ENV_Grass_D", 900, "cluster", clusters: 25, radius: 4),
            Spec("ENV_Grass_A", 700, "uniform"),
            Spec("ENV_GroundLeaf_A", 500, "cluster", clusters: 30, radius: 3),
            Spec("ENV_GroundLeaf_B", 300, "cluster", clusters: 20, radius: 3),
            Spec("ENV_Clover_A", 450, "cluster", clusters: 22, radius: 3),
            Spec("ENV_Clover_B", 350, "cluster", clusters: 18, radius: 3),
            Spec("ENV_FlowerSingle_A", 280, "cluster", clusters: 14, radius: 3.5f),
            Spec("ENV_FlowerSingle_B", 280, "cluster", clusters: 14, radius: 3.5f),
            Spec("ENV_Flower_A", 120, "cluster", clusters: 12, radius: 3),
            Spec("ENV_Flower_B", 120, "cluster", clusters: 12, radius: 3),
            Spec("ENV_Petal_A", 160, "cluster", clusters: 14, radius: 3),
            Spec("ENV_Petal_B", 160, "cluster", clusters: 14, radius: 3),
            // edges of paths and dirt yards
            Spec("ENV_Grass_B", 900, "edge"),
            Spec("ENV_Bush_A", 70, "edge", scale: (0.7f, 1.2f)),
            Spec("ENV_Bush_B", 50, "edge", scale: (0.7f, 1.2f)),
            Spec("ENV_Plant_A", 160, "edge"),
            Spec("ENV_Pebble_A", 50, "edge", scale: (0.6f, 1.0f)),
            Spec("ENV_Pebble_C", 50, "edge", scale: (0.6f, 1.0f)),
            // undergrowth around trees
            Spec("ENV_Fern_A", 140, "around", radius: 5, points: trees),
            Spec("ENV_Plant_B", 40, "around", radius: 5, points: trees),
            Spec("ENV_Plant_A", 160, "around", radius: 4, points: trees),
            Spec("ENV_GroundLeaf_B", 220, "around", radius: 4, points: trees),
            Spec("ENV_Mushroom_A", 70, "around", radius: 3, points: trees),
            Spec("ENV_Bush_A", 60, "around", radius: 6, points: trees, scale: (0.8f, 1.4f)),
        };
        int placed = (int)terrain.Call("scatter", specs, new Rect2(58, 93, 150, 80), 3);
        GD.Print($"TestVillage: scattered {placed} decorations");
    }

    private void SpawnVillagers()
    {
        var parent = GetNode<Node3D>("Villagers");
        var road = GetNode<Path3D>("Road").Curve;
        var roadHall = GetNode<Path3D>("RoadHall").Curve;
        for (int i = 0; i < 14; i++)
        {
            var curve = i % 5 == 4 ? roadHall : road;
            var character = Spawn(parent, i);
            _walkers.Add(new Walker
            {
                Character = character,
                Curve = curve,
                Offset = _rng.RandfRange(0, curve.GetBakedLength()),
                Direction = i % 2 == 0 ? 1 : -1,
                Speed = _rng.RandfRange(1.0f, 1.4f),
                Lane = i % 2 == 0 ? 0.8f : -0.8f,
            });
            character.Play(i % 3 == 0 ? "walk_carry" : "walk");
            character.Desync(_rng.Randf());
        }

        // Workers at their stations: (building, offset from its centre, animation)
        var stations = new (string building, Vector3 offset, string anim)[]
        {
            ("BLD_Woodcutter_A", new Vector3(-1.2f, 0, -2.6f), "chop"),
            ("BLD_Woodcutter_A", new Vector3(1.5f, 0, -3.0f), "idle_carry"),
            ("BLD_Field_A", new Vector3(-1.5f, 0, 0.5f), "plant_seed"),
            ("BLD_Field_A_2", new Vector3(1.0f, 0, -1.0f), "watering"),
            ("BLD_Market_A", new Vector3(0.5f, 0, 0.6f), "talk"),
            ("BLD_Smithy_A", new Vector3(0.6f, 0, 3.4f), "mine"),
        };
        for (int i = 0; i < stations.Length; i++)
        {
            var (name, offset, anim) = stations[i];
            var building = GetNode("Buildings").GetChildren().OfType<Node3D>().FirstOrDefault(n => n.Name == name);
            if (building is null) continue;
            var character = Spawn(parent, 20 + i);
            var pos = building.Position + offset.Rotated(Vector3.Up, building.Rotation.Y);
            character.Position = pos;
            character.LookAt(building.Position with { Y = 0 }, Vector3.Up, useModelFront: true);
            character.Play(anim);
            character.Desync(_rng.Randf());
        }
    }

    private static Godot.Collections.Dictionary Spec(string scene, int count, string mode, int clusters = 0,
        float radius = 4f, Godot.Collections.Array? points = null, (float min, float max)? scale = null)
    {
        var d = new Godot.Collections.Dictionary
        {
            ["scene"] = $"res://assets/environment/{scene}.tscn", ["count"] = count, ["mode"] = mode, ["radius"] = radius,
        };
        if (clusters > 0) d["clusters"] = clusters;
        if (points is not null) d["points"] = points;
        if (scale is { } sc) d["scale"] = new Godot.Collections.Array { sc.min, sc.max };
        return d;
    }

    private ModularCharacter Spawn(Node3D parent, int index)
    {
        var scene = GD.Load<PackedScene>(index % 3 == 1 ? FemaleScene : MaleScene);
        var character = scene.Instantiate<ModularCharacter>();
        parent.AddChild(character);
        character.ApplyHairColor(ModularCharacter.HairPalette[index % ModularCharacter.HairPalette.Length]);
        return character;
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        foreach (var w in _walkers)
        {
            float length = w.Curve.GetBakedLength();
            w.Offset += w.Direction * w.Speed * dt;
            if (w.Offset > length || w.Offset < 0)
            {
                w.Direction = -w.Direction;
                w.Lane = -w.Lane;
                w.Offset = Mathf.Clamp(w.Offset, 0, length);
            }
            var here = w.Curve.SampleBaked(w.Offset);
            var ahead = w.Curve.SampleBaked(Mathf.Clamp(w.Offset + w.Direction * 0.5f, 0, length));
            var forward = (ahead - here) with { Y = 0 };
            if (forward.LengthSquared() < 1e-6f) continue;
            forward = forward.Normalized();
            var side = forward.Cross(Vector3.Up) * w.Lane;
            w.Character.Position = (here + side) with { Y = 0 };
            w.Character.Rotation = new Vector3(0, Mathf.Atan2(forward.X, forward.Z), 0);
        }
    }
}
