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
        var characters = new List<string>();
        if (root.TryGetValue("characters", out var chars))
            foreach (var c in chars.AsGodotArray()) characters.Add(c.AsString());
        CharacterScenes = characters;
        ConstructionScene = root.TryGetValue("construction", out var site) ? site.AsString() : null;
    }

    /// <summary>Construction site visual, stretched to the footprint (the site scene is authored at 4×4 m).</summary>
    public Node3D? CreateConstructionSite(Vector2 footprintMeters)
    {
        if (ConstructionScene is null) return null;
        var node = GD.Load<PackedScene>(ConstructionScene).Instantiate<Node3D>();
        node.Scale = new Vector3(footprintMeters.X / 4f, 1f, footprintMeters.Y / 4f);
        return node;
    }

    /// <summary>Semi-transparent preview material on every mesh of a scene (build ghost).</summary>
    private void MakeGhost(Node node)
    {
        var mat = Material(new Color(0.85f, 0.95f, 1f), ghost: true);
        foreach (var child in node.FindChildren("*", "GeometryInstance3D", true, false))
        {
            var g = (GeometryInstance3D)child;
            g.MaterialOverride = mat;
            g.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        }
    }

    /// <summary>World position of a cell's corner, at the ground's height (terraces on generated maps, see <see cref="Ground"/>).</summary>
    public Vector3 CellToWorld(int x, int y) => new(x * CellSize, Ground.CellCenterHeight(x, y), y * CellSize);

    public Color ResourceColor(string resourceId) =>
        _resourceColors.TryGetValue(resourceId, out var c) ? new Color(c.AsString()) : Colors.White;

    public (Vector3 size, Color color) Agent(string kind)
    {
        var e = _agents[kind].AsGodotDictionary();
        return (ToVector3(e["size"]), new Color(e["color"].AsString()));
    }

    /// <summary>Character scenes for villagers (variety picked by household id).</summary>
    public IReadOnlyList<string> CharacterScenes { get; }

    /// <summary>Scene shown while a building is under construction (scaled to the footprint).</summary>
    public string? ConstructionScene { get; }

    /// <summary>
    /// Visual for a building, centred on its footprint, base at Y = 0. <paramref name="variantSeed"/> picks
    /// among "variants" deterministically (e.g. the building id). Ghosts always use the first variant.
    /// </summary>
    public Node3D CreateBuilding(string defId, Vector2 footprintMeters, bool ghost = false, int variantSeed = 0)
    {
        var root = new Node3D { Name = defId };
        if (!_buildings.TryGetValue(defId, out var entryVariant))
        {
            root.AddChild(Primitive("box", new Vector3(footprintMeters.X * 0.9f, 1.5f, footprintMeters.Y * 0.9f),
                Colors.Magenta, ghost));
            return root;
        }

        var entry = entryVariant.AsGodotDictionary();
        string? scenePath = null;
        if (entry.TryGetValue("scene", out var single)) scenePath = single.AsString();
        else if (entry.TryGetValue("variants", out var variants))
        {
            var list = variants.AsGodotArray();
            scenePath = list[ghost ? 0 : Mathf.PosMod(variantSeed, list.Count)].AsString();
        }
        if (scenePath is not null)
        {
            var node = GD.Load<PackedScene>(scenePath).Instantiate<Node3D>();
            if (ghost) MakeGhost(node);
            root.AddChild(node);
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
