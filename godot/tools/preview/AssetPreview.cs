using System.Linq;
using Godot;

namespace Ironvale.Game.Tools;

/// <summary>
/// Dev tool: lays out every scene of an asset category on a grid with labels and a villager for scale.
/// godot-mono --path godot res://tools/preview/asset_preview.tscn -- --category=buildings --spacing=12 --distance=60
/// </summary>
public partial class AssetPreview : Node3D
{
    public override void _Ready()
    {
        var args = OS.GetCmdlineUserArgs();
        string Arg(string key, string fallback) =>
            args.FirstOrDefault(a => a.StartsWith($"--{key}="))?.Split('=', 2)[1] ?? fallback;

        string category = Arg("category", "buildings");
        string filter = Arg("filter", "");
        float spacing = float.Parse(Arg("spacing", "12"), System.Globalization.CultureInfo.InvariantCulture);
        float distance = float.Parse(Arg("distance", "60"), System.Globalization.CultureInfo.InvariantCulture);
        float yaw = float.Parse(Arg("yaw", "45"), System.Globalization.CultureInfo.InvariantCulture);

        AddChild(new WorldEnvironment
        {
            Environment = new Environment
            {
                BackgroundMode = Environment.BGMode.Sky,
                Sky = new Sky { SkyMaterial = new ProceduralSkyMaterial() },
                AmbientLightSource = Environment.AmbientSource.Sky,
                TonemapMode = Environment.ToneMapper.Filmic,
                SsaoEnabled = true,
            },
        });
        AddChild(new DirectionalLight3D { RotationDegrees = new Vector3(-55, -35, 0), ShadowEnabled = true, LightEnergy = 1.1f });
        AddChild(new MeshInstance3D
        {
            Mesh = new PlaneMesh { Size = new Vector2(400, 400) },
            MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.56f, 0.72f, 0.42f), Roughness = 1 },
        });

        var files = DirAccess.GetFilesAt($"res://assets/{category}")
            .Where(f => f.EndsWith(".tscn") && f.Contains(filter)).OrderBy(f => f).ToArray();
        int cols = Mathf.CeilToInt(Mathf.Sqrt(files.Length));
        for (int i = 0; i < files.Length; i++)
        {
            var pos = new Vector3(i % cols * spacing, 0, i / cols * spacing);
            var node = GD.Load<PackedScene>($"res://assets/{category}/{files[i]}").Instantiate<Node3D>();
            node.Position = pos;
            AddChild(node);
            AddChild(new Label3D
            {
                Text = files[i].Replace(".tscn", ""), Position = pos + new Vector3(0, 0.1f, spacing * 0.42f),
                RotationDegrees = new Vector3(-90, 0, 0), PixelSize = 0.008f, FontSize = 64, OutlineSize = 12,
            });
            if (category != "characters" && ResourceLoader.Exists("res://assets/characters/CHR_Villager_Base.tscn"))
            {
                var guy = GD.Load<PackedScene>("res://assets/characters/CHR_Villager_Base.tscn").Instantiate<Node3D>();
                guy.Position = pos + new Vector3(spacing * 0.32f, 0, spacing * 0.3f);
                AddChild(guy);
            }
        }

        float extent = (cols - 1) * spacing;
        var center = new Vector3(extent / 2, 0, (Mathf.CeilToInt(files.Length / (float)cols) - 1) * spacing / 2);
        float pitch = Mathf.DegToRad(57), y = Mathf.DegToRad(yaw);
        var back = new Vector3(Mathf.Sin(y), 0, Mathf.Cos(y));
        var cam = new Camera3D { Fov = 30, Current = true, Far = 2000 };
        AddChild(cam);
        cam.GlobalPosition = center + back * (Mathf.Cos(pitch) * distance) + Vector3.Up * (Mathf.Sin(pitch) * distance);
        cam.LookAt(center, Vector3.Up);
    }
}
