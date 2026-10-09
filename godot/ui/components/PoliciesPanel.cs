using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Ironvale.Sim.Commands;

namespace Ironvale.Game.UI;

/// <summary>
/// Decrees "keep X between Min and Max" + the reeve's account book (what is automated and what it decided —
/// guide §6.3). Each row shows the live state (recruiting / releasing / inside the band / why it can't act).
/// </summary>
public partial class PoliciesPanel : PanelContainer
{
    public event Action? Closed;
    public Action<SimCommand> Send { get; set; } = _ => { };

    private OptionButton _resource = null!;
    private SpinBox _min = null!;
    private SpinBox _max = null!;
    private VBoxContainer _list = null!;
    private RichTextLabel _log = null!;
    private string _signature = "";
    private string _resourcesSig = "";
    private int _defaultBand = 250;
    private Label _ca = null!;
    private readonly Dictionary<int, Label> _stateLabels = new();

    public override void _Ready()
    {
        ThemeTypeVariation = "PanelPrimary";
        MouseFilter = MouseFilterEnum.Stop;
        var box = new VBoxContainer();
        AddChild(box);
        box.AddChild(UiNodes.Header(UiText.T("policies.title"), () => Closed?.Invoke()));
        _ca = UiNodes.Label("", "SecondaryLabel", wrap: true);
        box.AddChild(_ca);
        var create = new HBoxContainer();
        create.AddChild(UiNodes.Label(UiText.T("policies.keep")));
        _resource = new OptionButton { FocusMode = FocusModeEnum.None };
        create.AddChild(_resource);
        create.AddChild(UiNodes.Label(UiText.T("policies.between")));
        _min = Spin(100);
        _max = Spin(125);
        _min.ValueChanged += v =>
        {
            if (_max.Value <= v) _max.Value = Math.Ceiling(v * (1000 + _defaultBand) / 1000.0 / 10) * 10 + (v == 0 ? 10 : 0);
        };
        create.AddChild(_min);
        create.AddChild(UiNodes.Label(UiText.T("policies.and")));
        create.AddChild(_max);
        create.AddChild(UiNodes.Button(UiText.T("policies.create"), () =>
        {
            if (_resource.ItemCount == 0) return;
            Send(new CreatePolicy("keep_above", _resource.GetItemMetadata(_resource.Selected).AsString(),
                (long)_min.Value, (long)_max.Value));
        }, UiText.T("policies.create.tooltip")));
        box.AddChild(create);
        _list = new VBoxContainer();
        box.AddChild(_list);
        box.AddChild(UiNodes.Label(UiText.T("policies.log"), "SecondaryLabel"));
        _log = new RichTextLabel { CustomMinimumSize = new Vector2(0, 220), ScrollFollowing = true, FitContent = false };
        box.AddChild(_log);
    }

    private static SpinBox Spin(double value) =>
        new() { MinValue = 0, MaxValue = 100000, Step = 1, CustomArrowStep = 10, Value = value, CustomMinimumSize = new Vector2(96, 0) };

    public void Bind(UiSnapshot snap)
    {
        string rsig = string.Join(',', snap.Resources.Select(r => r.Id));
        if (rsig != _resourcesSig)
        {
            _resourcesSig = rsig;
            _resource.Clear();
            foreach (var r in snap.Resources)
            {
                _resource.AddItem(r.Name);
                _resource.SetItemMetadata(_resource.ItemCount - 1, r.Id);
            }
        }
        if (snap.Policies.Count > 0) _defaultBand = snap.Policies[0].DefaultBandPermille;
        bool over = snap.AdminUsed > snap.AdminCapacity;
        _ca.Text = UiText.T("policies.ca", snap.AdminUsed, snap.AdminCapacity, over ? UiText.T("policies.ca.over") : "");
        _ca.ThemeTypeVariation = over ? "WarningLabel" : "SecondaryLabel";

        // Rows are rebuilt only when the decrees themselves change, so editing a SpinBox is never interrupted.
        string sig = string.Join('|', snap.Policies.Select(p => $"{p.Id}:{p.Enabled}:{p.Min}:{p.Max}"));
        if (sig != _signature)
        {
            _signature = sig;
            UiNodes.Clear(_list);
            _stateLabels.Clear();
            foreach (var p in snap.Policies) _list.AddChild(Row(p));
        }
        foreach (var p in snap.Policies)
        {
            if (!_stateLabels.TryGetValue(p.Id, out var label)) continue;
            label.Text = UiText.T("policies.state." + p.State, p.ResourceName, p.Stock, p.Min, p.Max);
            label.ThemeTypeVariation = p.State.StartsWith("blocked") ? "WarningLabel" : "SecondaryLabel";
        }
        _log.Text = string.Join('\n', snap.PolicyLog.Select(e => e.Text));
    }

    private Control Row(PolicySnap p)
    {
        int id = p.Id;
        var column = new VBoxContainer();
        var row = new HBoxContainer();
        var enabled = new CheckBox { ButtonPressed = p.Enabled, TooltipText = UiText.T("policies.enabled"), FocusMode = FocusModeEnum.None };
        enabled.Toggled += on => Send(new SetPolicyEnabled(id, on));
        row.AddChild(enabled);
        row.AddChild(UiNodes.Label(UiText.T("policies.row", p.Id, p.ResourceName, p.CaCost), expand: true));
        var min = Spin(p.Min);
        var max = Spin(p.Max);
        min.TooltipText = UiText.T("policies.min.tooltip");
        max.TooltipText = UiText.T("policies.max.tooltip");
        min.ValueChanged += v => Send(new SetPolicyBand(id, (long)v, (long)max.Value));
        max.ValueChanged += v => Send(new SetPolicyBand(id, (long)min.Value, (long)v));
        row.AddChild(min);
        row.AddChild(UiNodes.Label(UiText.T("policies.and")));
        row.AddChild(max);
        row.AddChild(UiNodes.Button("✕", () => Send(new RemovePolicy(id)), UiText.T("policies.remove")));
        column.AddChild(row);
        var state = UiNodes.Label("", "SecondaryLabel", wrap: true);
        _stateLabels[id] = state;
        column.AddChild(state);
        return column;
    }
}
