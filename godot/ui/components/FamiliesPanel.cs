using System;
using System.Linq;
using Godot;

namespace Ironvale.Game.UI;

/// <summary>Families: what each one does and whether it is fine (guide §2.3 "Painel de família").</summary>
public partial class FamiliesPanel : PanelContainer
{
    public event Action? Closed;
    public event Action<int>? FocusBuilding;

    private VBoxContainer _list = null!;
    private string _signature = "";

    public override void _Ready()
    {
        ThemeTypeVariation = "PanelPrimary";
        MouseFilter = MouseFilterEnum.Stop;
        var box = new VBoxContainer();
        AddChild(box);
        box.AddChild(UiNodes.Header(UiText.T("families.title"), () => Closed?.Invoke()));
        var scroll = new ScrollContainer { CustomMinimumSize = new Vector2(0, 420), HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        box.AddChild(scroll);
        _list = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        scroll.AddChild(_list);
    }

    public void Bind(UiSnapshot snap)
    {
        string sig = string.Join('|', snap.Households.Select(h =>
            $"{h.Id}:{h.JobBuildingId}:{h.State}:{h.ToolPercent / 5}:{h.ProductivityPercent / 5}:{h.FoodDeficitDays}:{h.ColdDeficitDays}:{h.Homeless}"));
        if (sig == _signature) return;
        _signature = sig;
        UiNodes.Clear(_list);
        foreach (var h in snap.Households)
        {
            string warn = (h.FoodDeficitDays > 0 ? UiText.T("families.hunger", h.FoodDeficitDays) : "") +
                          (h.ColdDeficitDays > 0 ? UiText.T("families.cold", h.ColdDeficitDays) : "") +
                          (h.Homeless ? UiText.T("families.homeless") : "");
            var row = new VBoxContainer();
            var title = new HBoxContainer();
            title.AddChild(UiNodes.Label(UiText.T("families.row", h.Name, h.Members,
                h.JobBuildingId == 0 ? UiText.T("building.no_job") : h.JobName, UiText.T("state." + h.State)), expand: true));
            if (h.JobBuildingId != 0)
            {
                int target = h.JobBuildingId;
                title.AddChild(UiNodes.Button(UiText.T("ui.alert.go"), () => FocusBuilding?.Invoke(target)));
            }
            row.AddChild(title);
            row.AddChild(UiNodes.Label(UiText.T("families.detail", h.ToolPercent, h.ProductivityPercent, warn),
                warn.Length > 0 ? "WarningLabel" : "SecondaryLabel"));
            _list.AddChild(row);
        }
    }
}
