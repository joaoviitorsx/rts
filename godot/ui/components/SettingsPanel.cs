using System;
using Godot;

namespace Ironvale.Game.UI;

/// <summary>Settings (guide §7 accessibility): UI scale 80–150%, with what is actually applied in this window and why.</summary>
public partial class SettingsPanel : PanelContainer
{
    public event Action? Closed;

    private OptionButton _scale = null!;
    private Label _applied = null!;

    public override void _Ready()
    {
        ThemeTypeVariation = "PanelPrimary";
        MouseFilter = MouseFilterEnum.Stop;
        var box = new VBoxContainer();
        AddChild(box);
        box.AddChild(UiNodes.Header(UiText.T("settings.title"), () => Closed?.Invoke()));
        var row = new HBoxContainer();
        row.AddChild(UiNodes.Label(UiText.T("settings.scale"), expand: true));
        _scale = new OptionButton { FocusMode = FocusModeEnum.None, TooltipText = UiText.T("settings.scale.tooltip") };
        for (int p = 80; p <= 150; p += 10) _scale.AddItem($"{p}%", p);
        _scale.ItemSelected += i => UiSettings.Set(GetTree().Root, _scale.GetItemId((int)i) / 100f);
        row.AddChild(_scale);
        box.AddChild(row);
        _applied = UiNodes.Label("", "SecondaryLabel", wrap: true);
        box.AddChild(_applied);
        UiSettings.Changed += Refresh;
        Refresh();
    }

    public override void _ExitTree() => UiSettings.Changed -= Refresh;

    private void Refresh()
    {
        if (_scale is null) return;
        _scale.Select(_scale.GetItemIndex(UiSettings.Percent));
        string why = UiSettings.AppliedPercent > UiSettings.Percent ? UiText.T("settings.applied.legible")
            : UiSettings.AppliedPercent < UiSettings.Percent ? UiText.T("settings.applied.fit") : "";
        _applied.Text = UiText.T("settings.applied", UiSettings.AppliedPercent, Mathf.RoundToInt(UiSettings.SmallestTextPx(GetTree().Root)), why);
    }
}
