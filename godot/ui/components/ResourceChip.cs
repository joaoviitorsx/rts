using Godot;

namespace Ironvale.Game.UI;

/// <summary>Top-bar resource: icon · value · daily trend; tooltip explains production, consumption and runway (guide §3.1).</summary>
public partial class ResourceChip : PanelContainer
{
    private TextureRect _icon = null!;
    private Label _name = null!;
    private Label _value = null!;
    private Label _trend = null!;

    public override void _Ready()
    {
        ThemeTypeVariation = "ChipPanel";
        MouseFilter = MouseFilterEnum.Stop;
        var row = new HBoxContainer();
        AddChild(row);
        _icon = new TextureRect { ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, CustomMinimumSize = new Vector2(24, 24),
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, Visible = false };
        row.AddChild(_icon);
        _name = UiNodes.Label("", "SecondaryLabel");
        row.AddChild(_name);
        _value = UiNodes.Label("");
        row.AddChild(_value);
        _trend = UiNodes.Label("");
        row.AddChild(_trend);
    }

    public void Bind(ResourceSnap r)
    {
        var icon = IconRegistry.Get("res." + r.Id);
        _icon.Texture = icon;
        _icon.Visible = icon is not null;
        _name.Visible = icon is null;          // text label stands in until icons exist
        _name.Text = r.Name;
        _value.Text = (r.Stock / 1000).ToString();
        long net = r.Net / 1000;
        _trend.Text = net > 0 ? $"↑+{net}" : net < 0 ? $"↓{net}" : "";
        _trend.ThemeTypeVariation = net >= 0 ? "TrendUp" : "TrendDown";
        string runway = double.IsInfinity(r.DaysLeft) ? UiText.T("ui.resource.stable")
            : UiText.T("ui.resource.lasts", System.Math.Floor(r.DaysLeft));
        TooltipText = UiText.T("ui.resource.tooltip", r.Name, r.Stock / 1000, r.ProducedDay / 1000, r.ConsumedDay / 1000, runway);
    }
}
