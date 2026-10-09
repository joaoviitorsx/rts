using System;
using System.Linq;
using Godot;
using Ironvale.Sim.Commands;

namespace Ironvale.Game.UI;

/// <summary>Policies + administrator log (what is automated and what it decided — guide §6.3).</summary>
public partial class PoliciesPanel : PanelContainer
{
    public event Action? Closed;
    public Action<SimCommand> Send { get; set; } = _ => { };

    private OptionButton _resource = null!;
    private SpinBox _threshold = null!;
    private VBoxContainer _list = null!;
    private RichTextLabel _log = null!;
    private string _signature = "";
    private string _resourcesSig = "";

    public override void _Ready()
    {
        ThemeTypeVariation = "PanelPrimary";
        MouseFilter = MouseFilterEnum.Stop;
        var box = new VBoxContainer();
        AddChild(box);
        box.AddChild(UiNodes.Header(UiText.T("policies.title"), () => Closed?.Invoke()));
        var create = new HBoxContainer();
        create.AddChild(UiNodes.Label(UiText.T("policies.keep")));
        _resource = new OptionButton { FocusMode = FocusModeEnum.None };
        create.AddChild(_resource);
        create.AddChild(UiNodes.Label(UiText.T("policies.above")));
        _threshold = new SpinBox { MinValue = 0, MaxValue = 100000, Step = 10, Value = 100 };
        create.AddChild(_threshold);
        create.AddChild(UiNodes.Button(UiText.T("policies.create"), () =>
        {
            if (_resource.ItemCount == 0) return;
            Send(new CreatePolicy("keep_above", _resource.GetItemMetadata(_resource.Selected).AsString(), (long)_threshold.Value));
        }));
        box.AddChild(create);
        _list = new VBoxContainer();
        box.AddChild(_list);
        box.AddChild(UiNodes.Label(UiText.T("policies.log"), "SecondaryLabel"));
        _log = new RichTextLabel { CustomMinimumSize = new Vector2(0, 220), ScrollFollowing = true, FitContent = false };
        box.AddChild(_log);
    }

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
        string sig = string.Join('|', snap.Policies.Select(p => $"{p.Id}:{p.Enabled}:{p.Threshold}"));
        if (sig != _signature)
        {
            _signature = sig;
            UiNodes.Clear(_list);
            foreach (var p in snap.Policies)
            {
                var row = new HBoxContainer();
                var enabled = new CheckBox { ButtonPressed = p.Enabled, TooltipText = UiText.T("policies.enabled"), FocusMode = FocusModeEnum.None };
                int id = p.Id;
                enabled.Toggled += on => Send(new SetPolicyEnabled(id, on));
                row.AddChild(enabled);
                row.AddChild(UiNodes.Label(UiText.T("policies.row", p.Id, p.ResourceName), expand: true));
                var threshold = new SpinBox { MinValue = 0, MaxValue = 100000, Step = 10, Value = p.Threshold };
                threshold.ValueChanged += v => Send(new SetPolicyThreshold(id, (long)v));
                row.AddChild(threshold);
                row.AddChild(UiNodes.Button("✕", () => Send(new RemovePolicy(id))));
                _list.AddChild(row);
            }
        }
        _log.Text = string.Join('\n', snap.PolicyLog.Select(e => e.Text));
    }
}
