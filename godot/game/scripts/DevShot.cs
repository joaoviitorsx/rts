using Godot;

namespace Ironvale.Game;

/// <summary>
/// Dev capture (--shot=PATH): saves the real window framebuffer after a short warm-up and quits. Unlike
/// --write-movie it keeps the true window size, so 2D UI is checked exactly as a player sees it.
/// </summary>
public partial class DevShot : Node
{
    public string Path { get; set; } = "";
    private int _frames;

    public override void _Process(double delta)
    {
        // Wait for a background fast-forward (--away) to finish before counting frames.
        if (GetTree().Root.FindChild("SimHost", true, false) is SimHost { IsBusy: true }) { _frames = 0; return; }
        if (++_frames != 60) return;
        var image = GetViewport().GetTexture().GetImage();
        var error = image.SavePng(Path);
        GD.Print(error == Error.Ok ? $"SHOT {Path} {image.GetSize()}" : $"SHOT FAILED {error}");
        GD.Print($"UI scale applied {Ironvale.Game.UI.UiSettings.AppliedPercent}% (asked {Ironvale.Game.UI.UiSettings.Percent}%), " +
                 $"smallest text {Ironvale.Game.UI.UiSettings.SmallestTextPx(GetTree().Root):0.0} px");
        GetTree().Quit();
    }
}
