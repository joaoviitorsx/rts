using System;
using System.Linq;
using Godot;
using Ironvale.Sim.Scripting;

namespace Ironvale.Game.UI;

/// <summary>
/// "Enquanto você estava fora" (Marco 2B): fast-forward 1/5/10 years with the reeve governing alone, then read the
/// chronicle of what happened (Ironvale.Sim.Scripting.Chronicle). Sends one request; never touches the world.
/// </summary>
public partial class AwayPanel : PanelContainer
{
    public event Action? Closed;
    public event Action<int>? Away;

    private VBoxContainer _chronicle = null!;
    private HBoxContainer _choices = null!;

    public override void _Ready()
    {
        ThemeTypeVariation = "PanelPrimary";
        MouseFilter = MouseFilterEnum.Stop;
        var box = new VBoxContainer();
        AddChild(box);
        box.AddChild(UiNodes.Header(UiText.T("away.title"), () => Closed?.Invoke()));
        box.AddChild(UiNodes.Label(UiText.T("away.intro"), "SecondaryLabel", wrap: true));
        _choices = new HBoxContainer();
        foreach (int years in new[] { 1, 5, 10 })
            _choices.AddChild(UiNodes.Button(UiText.T("away.years", years), () => Away?.Invoke(years)));
        box.AddChild(_choices);
        box.AddChild(new HSeparator());
        var scroll = new ScrollContainer { CustomMinimumSize = new Vector2(0, 360), HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        _chronicle = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        scroll.AddChild(_chronicle);
        box.AddChild(scroll);
        _chronicle.AddChild(UiNodes.Label(UiText.T("away.empty"), "SecondaryLabel", wrap: true));
    }

    /// <summary>Shows a finished chronicle (most memorable moments, oldest first).</summary>
    public void ShowChronicle(Chronicle c)
    {
        UiNodes.Clear(_chronicle);
        _chronicle.AddChild(UiNodes.Label(UiText.T("away.summary", c.Years, c.StartPopulation, c.EndPopulation, c.EndDecrees), wrap: true));
        var moments = c.Highlights(12);
        if (moments.Count == 0) _chronicle.AddChild(UiNodes.Label(UiText.T("away.quiet"), "SecondaryLabel", wrap: true));
        foreach (var m in moments)
        {
            string when = UiText.T("away.when", UiText.T("ui.season." + m.Season), m.Year);
            string text = string.Format(UiText.T("chronicle." + m.Key), m.Args.Cast<object>().ToArray());
            _chronicle.AddChild(UiNodes.Label($"{when} — {text}", m.Weight >= 4 ? "WarningLabel" : null, wrap: true));
        }
    }
}
