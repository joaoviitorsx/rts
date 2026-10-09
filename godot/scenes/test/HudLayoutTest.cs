using Godot;
using Ironvale.Game.UI;

namespace Ironvale.Game.Test;

/// <summary>
/// HUD v2 step 2 (docs/ui/HUD_v2_spec.md §7): the empty HUD structure at an exact window size, with the same UI scale
/// and font floor the game would apply there (<see cref="UiSettings.ApplyFor"/>), over a village capture standing in
/// for the 3D world. Renders in a SubViewport, so 1280×720, 1920×1080 and 2560×1440 are exact on any monitor.
/// Args after "--": --size=WxH · --shot=PATH · --backdrop=res://… (image behind the HUD).
/// </summary>
public partial class HudLayoutTest : Node
{
    private SubViewport _viewport = null!;
    private string? _shot;
    private int _frames;

    public override void _Ready()
    {
        var size = new Vector2I(1920, 1080);
        string backdrop = "res://scenes/test/hud_backdrop.jpg";
        foreach (var arg in OS.GetCmdlineUserArgs())
        {
            if (arg.StartsWith("--size=") && arg[7..].Split('x') is [var w, var h] && int.TryParse(w, out int wi) && int.TryParse(h, out int hi))
                size = new Vector2I(wi, hi);
            if (arg.StartsWith("--shot=")) _shot = arg[7..];
            if (arg.StartsWith("--backdrop=")) backdrop = arg[11..];
        }

        UiSettings.ApplyFor(size);
        float stretch = Mathf.Min(size.X / 1920f, size.Y / 1080f);
        var logical = new Vector2(size.X, size.Y) / (stretch * UiSettings.Applied);   // canvas_items + expand + content scale
        _viewport = new SubViewport
        {
            Size = size,
            Size2DOverride = new Vector2I(Mathf.RoundToInt(logical.X), Mathf.RoundToInt(logical.Y)),
            Size2DOverrideStretch = true,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
        };
        AddChild(_viewport);
        if (ResourceLoader.Exists(backdrop))
        {
            var world = new TextureRect
            {
                Texture = GD.Load<Texture2D>(backdrop),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
            };
            world.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            _viewport.AddChild(world);
        }
        var hud = GD.Load<PackedScene>("res://ui/screens/hud_v2.tscn").Instantiate<HudV2>();
        _viewport.AddChild(hud);
        hud.ShowZones();
        GD.Print($"size {size} · UI scale {UiSettings.AppliedPercent}% · logical {logical.X:0}×{logical.Y:0} · font floor {UiSettings.FontFloor} · " +
                 $"smallest text {UiSettings.SmallestFont * stretch * UiSettings.Applied:0.0} px → {System.Math.Max(UiSettings.SmallestFont, UiSettings.FontFloor) * stretch * UiSettings.Applied:0.0} px");

        var view = new TextureRect
        {
            Texture = _viewport.GetTexture(),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        };
        view.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        var layer = new CanvasLayer { Layer = 10 };
        AddChild(layer);
        layer.AddChild(view);
    }

    public override void _Process(double delta)
    {
        if (_shot is null || ++_frames != 30) return;
        var image = _viewport.GetTexture().GetImage();
        GD.Print(image.SavePng(_shot) == Error.Ok ? $"SHOT {_shot} {image.GetSize()}" : "SHOT FAILED");
        GetTree().Quit();
    }
}
