using Godot;
using Ironvale.Game.Audio;

namespace Ironvale.Game.UI;

/// <summary>
/// Button feedback for every button that enters the tree (UI_UX_guide §5.3): hover = slight brighten + scale 1.03 +
/// soft click; press = sink (scale 0.97) + wooden "toc"; all tweens 120–180 ms. Scale/modulate don't affect layout.
/// </summary>
public static class UiJuice
{
    private static readonly Color Hover = new(1.08f, 1.08f, 1.08f);

    public static void Attach(SceneTree tree)
    {
        tree.NodeAdded += node => { if (node is BaseButton b) Wire(b); };
        foreach (var node in tree.Root.FindChildren("*", "BaseButton", true, false)) Wire((BaseButton)node);
    }

    private static void Wire(BaseButton b)
    {
        if (b.HasMeta("juice")) return;
        b.SetMeta("juice", true);
        b.MouseEntered += () =>
        {
            if (b.Disabled) return;
            To(b, Hover, 1.03f, 0.12f);
            Sfx.Play("ui_hover");
        };
        b.MouseExited += () => To(b, Colors.White, 1f, 0.12f);
        b.ButtonDown += () =>
        {
            To(b, Colors.White, 0.97f, 0.06f);
            Sfx.Play("ui_click");
        };
        b.ButtonUp += () => To(b, b.IsHovered() ? Hover : Colors.White, b.IsHovered() ? 1.03f : 1f, 0.18f);
    }

    private static void To(Control c, Color modulate, float scale, float seconds)
    {
        if (!c.IsInsideTree()) return;
        c.PivotOffset = c.Size / 2;
        var tw = c.CreateTween().SetParallel().SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        tw.TweenProperty(c, "modulate", modulate, seconds);
        tw.TweenProperty(c, "scale", Vector2.One * scale, seconds);
    }
}
