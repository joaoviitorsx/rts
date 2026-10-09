using System;
using Godot;

namespace Ironvale.Game.UI;

/// <summary>
/// Player UI preferences kept in user://settings.cfg (not in saves). UI scale 80–150% (UI_UX_guide §7) is applied as
/// the root viewport's content scale factor, so only 2D (HUD) changes — the 3D world is untouched.
/// <para>The applied scale is the player's choice, raised so the smallest text is at least <see cref="MinTextPx"/>
/// on screen (guide §7: never below 12 px) and capped so the HUD still fits the window.</para>
/// </summary>
public static class UiSettings
{
    private const string Path = "user://settings.cfg";
    public const float MinScale = 0.8f;
    public const float MaxScale = 1.5f;
    public const float Step = 0.1f;
    /// <summary>Smallest font the player UI uses (theme SecondaryLabel; the 12 px chart is debug-only).</summary>
    public const int SmallestFont = 14;
    public const float MinTextPx = 12f;

    /// <summary>Smallest logical HUD area that still fits every bar and panel (base 1920×1080 layout).</summary>
    private static readonly Vector2 MinLogical = new(1480, 820);   // measured worst case (panels open) ≈ 1436 × 808

    /// <summary>What the player asked for.</summary>
    public static float Scale { get; private set; } = 1f;
    /// <summary>What is applied (see the class summary).</summary>
    public static float Applied { get; private set; } = 1f;
    /// <summary>Scale needed in this window for the smallest text to reach <see cref="MinTextPx"/>.</summary>
    public static float LegibleMin { get; private set; } = 1f;
    /// <summary>Largest scale at which the HUD still fits this window.</summary>
    public static float FitMax { get; private set; } = MaxScale;

    public static event Action? Changed;

    /// <summary>Linear volumes 0–1: master and sound effects ("SFX" bus).</summary>
    public static float MasterVolume { get; private set; } = 0.8f;
    public static float EffectsVolume { get; private set; } = 0.8f;

    private static void ApplyVolumes()
    {
        AudioServer.SetBusVolumeDb(0, Mathf.LinearToDb(Mathf.Max(MasterVolume, 0.0001f)));
        AudioServer.SetBusMute(0, MasterVolume <= 0.001f);
        int sfx = AudioServer.GetBusIndex("SFX");
        if (sfx >= 0)
        {
            AudioServer.SetBusVolumeDb(sfx, Mathf.LinearToDb(Mathf.Max(EffectsVolume, 0.0001f)));
            AudioServer.SetBusMute(sfx, EffectsVolume <= 0.001f);
        }
    }

    public static void SetVolumes(float master, float effects)
    {
        MasterVolume = Mathf.Clamp(master, 0f, 1f);
        EffectsVolume = Mathf.Clamp(effects, 0f, 1f);
        ApplyVolumes();
        Save("audio", "master", MasterVolume);
        Save("audio", "effects", EffectsVolume);
    }

    private static void Save(string section, string key, float value)
    {
        var cfg = new ConfigFile();
        cfg.Load(Path);
        cfg.SetValue(section, key, value);
        cfg.Save(Path);
    }

    public static void Load(Window root)
    {
        var cfg = new ConfigFile();
        if (cfg.Load(Path) == Error.Ok)
        {
            Scale = Mathf.Clamp((float)cfg.GetValue("ui", "scale", 1f).AsDouble(), MinScale, MaxScale);
            MasterVolume = Mathf.Clamp((float)cfg.GetValue("audio", "master", 0.8f).AsDouble(), 0f, 1f);
            EffectsVolume = Mathf.Clamp((float)cfg.GetValue("audio", "effects", 0.8f).AsDouble(), 0f, 1f);
        }
        ApplyVolumes();
        Apply(root);
        root.SizeChanged += () => Apply(root);
    }

    /// <summary>canvas_items + expand: on-screen size = logical × stretch × factor, stretch = min(w/1920, h/1080).</summary>
    private static void Apply(Window root)
    {
        Vector2 window = root.Size;
        float stretch = Mathf.Min(window.X / 1920f, window.Y / 1080f);
        if (stretch <= 0) return;
        LegibleMin = MinTextPx / (SmallestFont * stretch);
        FitMax = Mathf.Min(window.X / (stretch * MinLogical.X), window.Y / (stretch * MinLogical.Y));
        // Legibility first, then fit: on a tiny window fitting wins (a cut-off HUD is worse than small text).
        Applied = Mathf.Min(Mathf.Max(Scale, LegibleMin), FitMax);
        root.ContentScaleFactor = Applied;
        Changed?.Invoke();
    }

    /// <summary>Smallest text size on screen right now, in pixels.</summary>
    public static float SmallestTextPx(Window root)
    {
        Vector2 window = root.Size;
        return SmallestFont * Mathf.Min(window.X / 1920f, window.Y / 1080f) * Applied;
    }

    public static void Set(Window root, float scale)
    {
        Scale = Mathf.Clamp(MathF.Round(scale * 10f) / 10f, MinScale, MaxScale);
        Apply(root);
        var cfg = new ConfigFile();
        cfg.Load(Path);
        cfg.SetValue("ui", "scale", Scale);
        cfg.Save(Path);
    }

    public static void Change(Window root, int steps) => Set(root, Scale + steps * Step);

    public static int Percent => (int)MathF.Round(Scale * 100);
    public static int AppliedPercent => (int)MathF.Round(Applied * 100);
}
