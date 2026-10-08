using System.Collections.Generic;
using Godot;

namespace Ironvale.Game;

/// <summary>
/// Reads game/visuals/visual_catalog.json and builds the visual node for a simulation id.
/// This is the only place that knows what a "woodcutter" looks like: swapping primitives for GLB scenes
/// later means editing the JSON, not the simulation or the rest of the view.
/// </summary>
public sealed class VisualCatalog
{
    private const string CatalogPath = "res://game/visuals/visual_catalog.json";

    private readonly Godot.Collections.Dictionary _buildings;
    private readonly Godot.Collections.Dictionary _agents;
    private readonly Godot.Collections.Dictionary _resourceColors;
    private readonly Dictionary<string, Material> _materials = new();

    public float CellSize { get; }

    public VisualCatalog()
    {
        string text = FileAccess.GetFileAsString(CatalogPath);
        var json = new Json();
        // Godot's JSON parser doesn't accept comments: strip // lines first.
        var lines = new List<string>();
        foreach (var line in text.Split('\n'))
            if (!line.TrimStart().StartsWith("//")) lines.Add(line);
        if (json.Parse(string.Join('\n', lines)) != Error.Ok)
            throw new System.InvalidOperationException($"{CatalogPath}:{json.GetErrorLine()}: {json.GetErrorMessage()}");
        var root = json.Data.AsGodotDictionary();
        CellSize = (float)root["cellSize"].AsDouble();
        _buildings = root["buildings"].AsGodotDictionary();
        _agents = root["agents"].AsGodotDictionary();
        _resourceColors = root["resourceColors"].AsGodotDictionary();
    }

    public Vector3 CellToWorld(int x, int y) => new(x * CellSize, 0, y * CellSize);

    public Color ResourceColor(string resourceId) =>
        _resourceColors.TryGetValue(resourceId, out var c) ? new Color(c.AsString()) : Colors.White;

    public (Vector3 size, Color color) Agent(string kind)
    {
        var e = _agents[kind].AsGodotDictionary();
        return (ToVector3(e["size"]), new Color(e["color"].AsString()));
    }

    /// <summary>Visual for a building, centred on its footprint, base at Y = 0.</summary>
    public Node3D CreateBuilding(string defId, Vector2 footprintMeters, bool ghost = false)
    {
        var root = new Node3D { Name = defId };
        if (!_buildings.TryGetValue(defId, out var entryVariant))
        {
            root.AddChild(Primitive("box", new Vector3(footprintMeters.X * 0.9f, 1.5f, footprintMeters.Y * 0.9f),
                Colors.Magenta, ghost));
            return root;
        }

        var entry = entryVariant.AsGodotDictionary();
        if (entry.TryGetValue("scene", out var scenePath))
        {
            var scene = GD.Load<PackedScene>(scenePath.AsString());
            root.AddChild(scene.Instantiate<Node3D>());
            return root;
        }

        var size = ToVector3(entry["size"]);
        var body = Primitive(entry["primitive"].AsString(), size, new Color(entry["color"].AsString()), ghost);
        root.AddChild(body);
        if (entry.TryGetValue("roof", out var roofVariant))
        {
            var roof = roofVariant.AsGodotDictionary();
            float h = (float)roof["height"].AsDouble();
            var roofNode = Primitive(roof["primitive"].AsString(), new Vector3(size.X * 1.1f, h, size.Z * 1.1f),
                new Color(roof["color"].AsString()), ghost);
            roofNode.Position = new Vector3(0, size.Y + h / 2, 0);
            root.AddChild(roofNode);
        }
        return root;
    }

    /// <summary>Primitive mesh with its base on Y = 0.</summary>
    public MeshInstance3D Primitive(string kind, Vector3 size, Color color, bool ghost = false)
    {
        Mesh mesh = kind switch
        {
            "cylinder" => new CylinderMesh { TopRadius = size.X / 2, BottomRadius = size.X / 2, Height = size.Y },
            "cone" => new CylinderMesh { TopRadius = 0.05f, BottomRadius = size.X / 2, Height = size.Y },
            "capsule" => new CapsuleMesh { Radius = size.X / 2, Height = size.Y },
            "prism" => new PrismMesh { Size = size },
            _ => new BoxMesh { Size = size },
        };
        return new MeshInstance3D
        {
            Mesh = mesh,
            Position = new Vector3(0, size.Y / 2, 0),
            MaterialOverride = Material(color, ghost),
        };
    }

    public Material Material(Color color, bool ghost = false)
    {
        string key = color.ToHtml() + (ghost ? "_g" : "");
        if (_materials.TryGetValue(key, out var m)) return m;
        var mat = new StandardMaterial3D
        {
            AlbedoColor = ghost ? new Color(color, 0.45f) : color,
            Roughness = 0.9f,
            Transparency = ghost ? BaseMaterial3D.TransparencyEnum.Alpha : BaseMaterial3D.TransparencyEnum.Disabled,
        };
        _materials[key] = mat;
        return mat;
    }

    private static Vector3 ToVector3(Variant v)
    {
        var a = v.AsGodotArray();
        return new Vector3((float)a[0].AsDouble(), (float)a[1].AsDouble(), (float)a[2].AsDouble());
    }
}
