using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Ironvale.Game.UI;

/// <summary>
/// Selected units of the RTS opening (GDD v0.3 §4.1): one line each — name · what they do · load · hunger/cold — and
/// "Parar". Placeholder in the 2A HUD style; the HUD v2 version is briefing step 6 ("painel de seleção de colonos").
/// </summary>
public partial class UnitPanel : PanelContainer
{
    public event Action? Stop;
    private VBoxContainer _body = null!;
    private string _signature = "";

    public override void _Ready()
    {
        ThemeTypeVariation = "PanelPrimary";
        MouseFilter = MouseFilterEnum.Stop;
        _body = new VBoxContainer();
        AddChild(_body);
    }

    public void Bind(UiSnapshot s, HashSet<int> selected)
    {
        var units = s.Units.Where(u => selected.Contains(u.Id)).ToList();
        Visible = units.Count > 0;
        string sig = string.Join('|', units.Select(u => $"{u.Id}{u.Task}{u.CarryUnits}{u.Confused}{u.FoodDeficitDays}{u.ColdDeficitDays}"));
        if (sig == _signature) return;
        _signature = sig;
        UiNodes.Clear(_body);
        if (units.Count == 0) return;
        _body.AddChild(UiNodes.Label(UiText.T("units.title", units.Count), "HeaderLabel"));
        foreach (var u in units.Take(10))
        {
            string line = $"{u.Name} — {UiText.T("units.task." + u.Task)}";
            if (u.CarryUnits > 0) line += " · " + UiText.T("units.carry", u.CarryUnits, u.CarryName);
            _body.AddChild(UiNodes.Label(line, u.Confused ? "WarningLabel" : null));
            if (u.FoodDeficitDays > 0) _body.AddChild(UiNodes.Label(UiText.T("units.hungry", u.FoodDeficitDays), "WarningLabel"));
            if (u.ColdDeficitDays > 0) _body.AddChild(UiNodes.Label(UiText.T("units.cold", u.ColdDeficitDays), "WarningLabel"));
        }
        if (units.Count > 10) _body.AddChild(UiNodes.Label(UiText.T("units.more", units.Count - 10), "SecondaryLabel"));
        _body.AddChild(UiNodes.Label(UiText.T("units.hint"), "SecondaryLabel", wrap: true));
        _body.AddChild(UiNodes.Button(UiText.T("units.stop"), () => Stop?.Invoke()));
    }
}
