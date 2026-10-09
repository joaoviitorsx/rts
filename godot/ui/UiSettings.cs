using System;
using Godot;

namespace Ironvale.Game.UI;

/// <summary>
/// Player UI preferences kept in user://settings.cfg (not in saves). UI scale 80–150% (UI_UX_guide §7) applied as the
/// root viewport's content scale factor, so only 2D (HUD) grows — the 3D world is untouched.
/// </summary>
public static class UiSettings
{
    private const string Path = "user://settings.cfg";
    public const float MinScale = 0.8f;
    public const float MaxScale = 1.5f;
    public const float Step = 0.1f;

    /// <summary>Smallest logical HUD area that still fits every bar and panel (base 1920×1080 layout).</summary>
    private static readonly Vector2 MinLogical = new(1440, 760);

    /// <summary>What the player asked for.</summary>
    public static float Scale { get; private set; } = 1f;
    /// <summary>What is applied: the request, capped so the HUD still fits the current window.</summary>
    public static float Applied { get; private set; } = 1f;

    public static void Load(Window root)
    {
        var cfg = new ConfigFile();
        if (cfg.Load(Path) == Error.Ok) Scale = Mathf.Clamp((float)cfg.GetValue("ui", "scale", 1f).AsDouble(), MinScale, MaxScale);
        Apply(root);
        root.SizeChanged += () => Apply(root);
    }

    /// <summary>canvas_items + expand: logical size = window / (stretch × factor); keep it ≥ <see cref="MinLogical"/>.</summary>
    private static void Apply(Window root)
    {
        Vector2 window = root.Size;
        float stretch = Mathf.Min(window.X / 1920f, window.Y / 1080f);
        float max = stretch <= 0 ? Scale : Mathf.Min(window.X / (stretch * MinLogical.X), window.Y / (stretch * MinLogical.Y));
        Applied = Mathf.Max(MinScale, Mathf.Min(Scale, max));
        root.ContentScaleFactor = Applied;
    }

    /// <summary>Changes the scale by <paramref name="delta"/> steps (wrapping around when <paramref name="wrap"/>) and saves it.</summary>
    public static float Change(Window root, int delta, bool wrap = false)
    {
        float next = MathF.Round((Scale + delta * Step) * 10f) / 10f;
        if (wrap && next > MaxScale + 0.001f) next = MinScale;
        Scale = Mathf.Clamp(next, MinScale, MaxScale);
        Apply(root);
        var cfg = new ConfigFile();
        cfg.Load(Path);
        cfg.SetValue("ui", "scale", Scale);
        cfg.Save(Path);
        return Scale;
    }

    public static int Percent => (int)MathF.Round(Scale * 100);
    public static int AppliedPercent => (int)MathF.Round(Applied * 100);
}
