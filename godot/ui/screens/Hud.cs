using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Ironvale.Sim.Commands;
using Ironvale.Sim.Events;
using Ironvale.Sim.Save;

namespace Ironvale.Game.UI;

/// <summary>
/// Main HUD following UI_UX_guide §2.2/§8.1: one Theme on the root, layout only with containers/anchors
/// (top: resources/date/speed · left: alerts (max 4) · right: one contextual panel at a time · bottom: build menu
/// and panel buttons), reads only <see cref="UiSnapshot"/> and sends commands. Refreshes 5×/s, not every frame.
/// </summary>
public partial class Hud : CanvasLayer
{
    private const string ThemePath = "res://ui/theme/main_theme.tres";
    private const double RefreshSeconds = 0.2;
    private const int MaxAlerts = 4;
    private static readonly PackedScene ChipScene = GD.Load<PackedScene>("res://ui/components/ResourceChip.tscn");
    private static readonly PackedScene AlertScene = GD.Load<PackedScene>("res://ui/components/AlertCard.tscn");

    private SimHost _host = null!;
    private BuildController _build = null!;
    private WorldView _view = null!;
    private CameraRig _camera = null!;

    private HBoxContainer _chips = null!;
    private Label _date = null!;
    private Label _winter = null!;
    private Label _population = null!;
    private readonly List<Button> _speedButtons = new();
    private VBoxContainer _alerts = null!;
    private Label _objectiveTitle = null!;
    private Label _ca = null!;
    private PanelContainer _suggestionCard = null!;
    private Label _suggestionWhy = null!;
    private Label _suggestionWhat = null!;
    private Label _suggestionCost = null!;
    private Label _suggestionLose = null!;
    private int _suggestionId;
    private SettingsPanel _settings = null!;
    private AwayPanel _away = null!;
    private Label _objective = null!;
    private BuildingPanel _buildingPanel = null!;
    private FamiliesPanel _families = null!;
    private PoliciesPanel _policies = null!;
    private DebugPanel? _debug;
    private Control? _activePanel;
    private int _selectedBuilding;
    private double _timer;
    private UiSnapshot? _snap;

    private readonly HashSet<string> _dismissed = new();
    private readonly HashSet<string> _alertKeysHeard = new();
    private readonly List<(string Key, AlertSeverity Severity, string Text, double Expires)> _transient = new();
    private int _transientCounter;

    public void Init(SimHost host, BuildController build, WorldView view, CameraRig camera)
    {
        _host = host;
        _build = build;
        _view = view;
        _camera = camera;
        _host.EventsReceived += OnEvents;
        _host.Message += text => PushTransient(AlertSeverity.Info, text);
        _host.WorldReplaced += () => { CloseActivePanel(); Refresh(); };
        _build.BuildingSelected += id => { if (id == 0) CloseActivePanel(); else OpenBuilding(id); };

        UiSettings.Load(GetTree().Root);
        var root = new Control { Name = "Root", MouseFilter = Control.MouseFilterEnum.Ignore, Theme = GD.Load<Theme>(ThemePath) };
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);
        var margin = new MarginContainer { ThemeTypeVariation = "ScreenMargin", MouseFilter = Control.MouseFilterEnum.Ignore };
        margin.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        root.AddChild(margin);
        var column = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        margin.AddChild(column);

        column.AddChild(BuildTopBar());

        var middle = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill, MouseFilter = Control.MouseFilterEnum.Ignore };
        column.AddChild(middle);
        var leftColumn = new VBoxContainer { CustomMinimumSize = new Vector2(380, 0), MouseFilter = Control.MouseFilterEnum.Ignore };
        middle.AddChild(leftColumn);
        leftColumn.AddChild(BuildObjectiveCard());
        leftColumn.AddChild(BuildSuggestionCard());
        _alerts = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        leftColumn.AddChild(_alerts);
        middle.AddChild(UiNodes.Spacer());
        var right = new VBoxContainer { CustomMinimumSize = new Vector2(440, 0), MouseFilter = Control.MouseFilterEnum.Ignore };
        middle.AddChild(right);
        _buildingPanel = AddPanel(right, new BuildingPanel());
        _buildingPanel.Send = _host.Send;
        _buildingPanel.Closed += CloseActivePanel;
        _families = AddPanel(right, new FamiliesPanel());
        _families.Closed += CloseActivePanel;
        _families.FocusBuilding += id => { FocusOnBuilding(id); OpenBuilding(id); };
        _away = AddPanel(right, new AwayPanel());
        _away.Closed += CloseActivePanel;
        _away.Away += years => _host.FastForwardWithChronicle(years);
        _host.ChronicleReady += c =>
        {
            ShowPanel(_away);
            _away.ShowChronicle(c);
            Audio.Sfx.Play("suggestion");
        };
        _settings = AddPanel(right, new SettingsPanel());
        _settings.Closed += CloseActivePanel;
        _policies = AddPanel(right, new PoliciesPanel());
        _policies.Send = cmd =>
        {
            _host.Send(cmd);
            if (cmd is CreatePolicy) DecreeStamp();
        };
        _policies.Closed += CloseActivePanel;
        if (OS.IsDebugBuild())   // telemetry never ships to players
        {
            _debug = AddPanel(right, new DebugPanel());
            _debug.Closed += CloseActivePanel;
            _debug.AdvanceYears += years => _host.AdvanceYears(years);
            _debug.ShowHash += () => PushTransient(AlertSeverity.Info, $"Hash: {SaveSerializer.StateHashHex(_host.World)}");
            _debug.NewGame += _host.NewGame;
        }

        column.AddChild(BuildBottomBar());
        Refresh();
    }

    private T AddPanel<T>(Container parent, T panel) where T : Control
    {
        panel.Visible = false;
        panel.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
        parent.AddChild(panel);
        return panel;
    }

    private Control BuildTopBar()
    {
        var bar = new PanelContainer { ThemeTypeVariation = "TopBar" };
        var row = new HBoxContainer();
        bar.AddChild(row);
        _chips = new HBoxContainer();
        row.AddChild(_chips);
        row.AddChild(UiNodes.Spacer());
        _population = UiNodes.Label("", "SecondaryLabel");
        row.AddChild(_population);
        _ca = UiNodes.Label("", "SecondaryLabel");
        _ca.TooltipText = UiText.T("ui.top.ca.tooltip");
        _ca.MouseFilter = Control.MouseFilterEnum.Stop;
        row.AddChild(_ca);
        _date = UiNodes.Label("");
        row.AddChild(_date);
        _winter = UiNodes.Label("", "SecondaryLabel");
        _winter.MouseFilter = Control.MouseFilterEnum.Stop;
        row.AddChild(_winter);
        foreach (int speed in SimHost.Speeds)
        {
            int s = speed;
            var b = UiNodes.Button(speed == 0 ? "⏸" : $"{speed}x", () => _host.SetSpeed(s),
                speed == 0 ? UiText.T("ui.speed.pause") : UiText.T("ui.speed.x", speed));
            b.ToggleMode = true;
            b.CustomMinimumSize = new Vector2(36, 0);   // Fitts: close speed targets (36 keeps the bar inside 1440 logical px)
            _speedButtons.Add(b);
            row.AddChild(b);
        }
        return bar;
    }

    /// <summary>Always-visible next goal (GDD v0.2 §4.1; feel rule: a next objective is always on screen).</summary>
    private Control BuildObjectiveCard()
    {
        var card = new PanelContainer { ThemeTypeVariation = "PanelPrimary" };
        var box = new VBoxContainer();
        card.AddChild(box);
        _objectiveTitle = UiNodes.Label("", "SecondaryLabel");
        box.AddChild(_objectiveTitle);
        _objective = UiNodes.Label("", wrap: true);
        box.AddChild(_objective);
        return card;
    }

    /// <summary>The reeve's decree offer (guide §3.3): why · what · cost · what you give up · [Criar] [Agora não] [Nunca].</summary>
    private Control BuildSuggestionCard()
    {
        _suggestionCard = new PanelContainer { ThemeTypeVariation = "AlertInfo", Visible = false };
        var box = new VBoxContainer();
        _suggestionCard.AddChild(box);
        box.AddChild(UiNodes.Label(UiText.T("suggestion.title"), "HeaderLabel"));
        _suggestionWhy = UiNodes.Label("", "SecondaryLabel", wrap: true);
        box.AddChild(_suggestionWhy);
        _suggestionWhat = UiNodes.Label("", wrap: true);
        box.AddChild(_suggestionWhat);
        _suggestionCost = UiNodes.Label("", "SecondaryLabel", wrap: true);
        box.AddChild(_suggestionCost);
        _suggestionLose = UiNodes.Label("", "SecondaryLabel", wrap: true);
        box.AddChild(_suggestionLose);
        var buttons = new HBoxContainer();
        buttons.AddChild(UiNodes.Button(UiText.T("suggestion.accept"), () =>
        {
            _host.Send(new AcceptSuggestion(_suggestionId));
            DecreeStamp();
        }));
        buttons.AddChild(UiNodes.Button(UiText.T("suggestion.later"), () => _host.Send(new DismissSuggestion(_suggestionId, false))));
        buttons.AddChild(UiNodes.Button(UiText.T("suggestion.never"), () => _host.Send(new DismissSuggestion(_suggestionId, true))));
        box.AddChild(buttons);
        return _suggestionCard;
    }

    /// <summary>Decree issued: stamp sound + the CA meter pulses (guide §5.3).</summary>
    private void DecreeStamp()
    {
        Audio.Sfx.Play("decree_stamp");
        _ca.PivotOffset = _ca.Size / 2;
        var tw = _ca.CreateTween().SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        tw.TweenProperty(_ca, "scale", Vector2.One * 1.35f, 0.1f);
        tw.TweenProperty(_ca, "scale", Vector2.One, 0.18f);
    }

    private void BindSuggestion(UiSnapshot s)
    {
        _suggestionCard.Visible = s.Suggestion is not null;
        if (s.Suggestion is not { } g || g.Id == _suggestionId) return;
        _suggestionId = g.Id;
        Audio.Sfx.Play("suggestion");
        _suggestionWhy.Text = UiText.T("suggestion.why", g.ResourceName, g.Actions, g.AverageStock);
        _suggestionWhat.Text = UiText.T("suggestion.what", g.ResourceName, g.Min, g.Max)
                               + (g.WinterAdjusted ? "\n" + UiText.T("suggestion.winter") : "");
        _suggestionCost.Text = UiText.T("suggestion.cost", g.CaCost, s.AdminUsed, s.AdminCapacity);
        _suggestionLose.Text = UiText.T("suggestion.lose", g.ResourceName, g.MaxHouseholds);
    }

    private Control BuildBottomBar()
    {
        var row = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        var left = new PanelContainer { ThemeTypeVariation = "PanelPrimary" };
        var leftRow = new HBoxContainer();
        left.AddChild(leftRow);
        leftRow.AddChild(UiNodes.Button(UiText.T("ui.bottom.families") + " (F)", () => TogglePanel(_families)));
        leftRow.AddChild(UiNodes.Button(UiText.T("ui.bottom.policies") + " (P)", () => TogglePanel(_policies)));
        leftRow.AddChild(new VSeparator());
        leftRow.AddChild(UiNodes.Button(UiText.T("ui.save"), _host.QuickSave, UiText.T("ui.save.tooltip")));
        leftRow.AddChild(UiNodes.Button(UiText.T("ui.load"), _host.QuickLoad, UiText.T("ui.load.tooltip")));
        leftRow.AddChild(new VSeparator());
        leftRow.AddChild(UiNodes.Button(UiText.T("ui.bottom.away"), () => TogglePanel(_away), UiText.T("away.tooltip")));
        leftRow.AddChild(UiNodes.Button(UiText.T("ui.bottom.settings"), () => TogglePanel(_settings), UiText.T("settings.scale.tooltip")));
        row.AddChild(left);
        row.AddChild(UiNodes.Spacer());

        var buildPanel = new PanelContainer { ThemeTypeVariation = "PanelPrimary" };
        var buildRow = new HBoxContainer();
        buildPanel.AddChild(buildRow);
        buildRow.AddChild(UiNodes.Label(UiText.T("ui.bottom.build"), "SecondaryLabel"));
        foreach (var def in _host.Content.Buildings.Where(d => d.Buildable))
        {
            string cost = string.Join(", ", def.Cost.Select((q, r) => (q, r)).Where(x => x.q.IsPositive)
                .Select(x => $"{x.q} {UiText.Res(_host.Content.Resources[x.r])}"));
            var d = def;
            buildRow.AddChild(UiNodes.Button(UiText.Bld(def), () => _build.Begin(d), UiText.T("ui.build.tooltip", UiText.Bld(def), cost, def.BuildDays)));
        }
        buildRow.AddChild(new VSeparator());
        buildRow.AddChild(UiNodes.Button(UiText.T("ui.build.road"), _build.BeginRoad,
            UiText.T("ui.build.road.tooltip", _host.Content.Balance.RoadStonePerCell)));
        row.AddChild(buildPanel);
        row.AddChild(UiNodes.Spacer());
        if (OS.IsDebugBuild())
        {
            var dbg = new PanelContainer { ThemeTypeVariation = "PanelPrimary" };
            dbg.AddChild(UiNodes.Button(UiText.T("ui.bottom.debug") + " (F12)", () => { if (_debug is not null) TogglePanel(_debug); }));
            row.AddChild(dbg);
        }
        return row;
    }

    // ------------------------------------------------------------------ panels

    private void TogglePanel(Control panel)
    {
        if (_activePanel == panel) CloseActivePanel();
        else ShowPanel(panel);
    }

    private void ShowPanel(Control panel)
    {
        if (_activePanel != panel) Audio.Sfx.Play("panel_open");
        if (_activePanel is not null && _activePanel != panel) _activePanel.Visible = false;   // one big panel at a time
        _activePanel = panel;
        panel.Visible = true;
        Refresh();
    }

    private void OpenBuilding(int id)
    {
        _selectedBuilding = id;
        _view.SelectedBuildingId = id;
        ShowPanel(_buildingPanel);
    }

    private void CloseActivePanel()
    {
        if (_activePanel is not null) Audio.Sfx.Play("panel_close");
        if (_activePanel is not null) _activePanel.Visible = false;
        _activePanel = null;
        _selectedBuilding = 0;
        _view.SelectedBuildingId = 0;
    }

    private void FocusOnBuilding(int id)
    {
        var b = _host.World.GetBuilding(id);
        if (b is not null) _camera.FocusOn(_view.FootprintCenter(b));
    }

    // ------------------------------------------------------------------ refresh

    public override void _Process(double delta)
    {
        if (_host.IsBusy)
        {
            _date.Text = UiText.T("ui.top.simulating", (int)(_host.AdvanceProgress * 100));
            return;
        }
        _timer -= delta;
        if (_timer > 0) return;
        _timer = RefreshSeconds;
        Refresh();

    }

    private void Refresh()
    {
        _snap = _host.BuildUiSnapshot();
        var s = _snap;
        while (_chips.GetChildCount() < s.Resources.Count) _chips.AddChild(ChipScene.Instantiate<ResourceChip>());
        for (int i = 0; i < s.Resources.Count; i++) ((ResourceChip)_chips.GetChild(i)).Bind(s.Resources[i]);
        _population.Text = UiText.T("ui.top.population", s.Population);
        _date.Text = UiText.T("ui.top.date", s.Year, UiText.T("ui.season." + s.Season), s.Month, s.Day);
        _ca.Text = UiText.T("ui.top.ca", s.AdminUsed, s.AdminCapacity);
        _ca.ThemeTypeVariation = s.AdminUsed > s.AdminCapacity ? "WarningLabel" : "SecondaryLabel";
        BindSuggestion(s);
        if (s.Objective is { } o)
        {
            _objectiveTitle.Text = UiText.T("objective.title", o.Index, o.Count);
            _objective.Text = o.Text;
        }
        _winter.Text = s.DaysToWinter == 0 ? UiText.T("ui.top.winter_now") : UiText.T("ui.top.winter_in", s.DaysToWinter);
        _winter.TooltipText = s.DaysToWinter == 0 ? UiText.T("ui.top.winter_now") : UiText.T("ui.top.winter_in.tooltip", s.DaysToWinter);
        for (int i = 0; i < _speedButtons.Count; i++) _speedButtons[i].SetPressedNoSignal(SimHost.Speeds[i] == s.Speed);

        if (_activePanel == _buildingPanel)
        {
            var b = s.Building(_selectedBuilding);
            if (b is null) CloseActivePanel();
            else _buildingPanel.Bind(b, s);
        }
        else if (_activePanel == _families) _families.Bind(s);
        else if (_activePanel == _policies) _policies.Bind(s);
        else if (_debug is not null && _activePanel == _debug) _debug.Bind(s);   // no debug panel in player builds
        RefreshAlerts(s);
    }

    private void RefreshAlerts(UiSnapshot s)
    {
        double now = Time.GetTicksMsec() / 1000.0;
        _transient.RemoveAll(t => t.Expires < now);
        var active = new HashSet<string>(s.Alerts.Select(a => a.Key));
        _dismissed.RemoveWhere(k => !active.Contains(k) && !k.StartsWith("t"));   // a cleared condition may alert again
        var shown = s.Alerts.Where(a => !_dismissed.Contains(a.Key))
            .Select(a => (a.Key, a.Severity, a.Text, CanGo: a.FocusBuildingId != 0))
            .Concat(_transient.Where(t => !_dismissed.Contains(t.Key)).Select(t => (t.Key, t.Severity, t.Text, CanGo: false)))
            .OrderByDescending(a => a.Severity)
            .Take(MaxAlerts)
            .ToList();
        if (shown.Any(a => a.Severity >= AlertSeverity.Warning && !_alertKeysHeard.Contains(a.Key)))
            Audio.Sfx.Play("alert");
        foreach (var a in shown) _alertKeysHeard.Add(a.Key);
        while (_alerts.GetChildCount() < shown.Count)
        {
            var card = AlertScene.Instantiate<AlertCard>();
            _alerts.AddChild(card);
            card.Dismissed += () => { _dismissed.Add(card.Key); Refresh(); };
        }
        for (int i = 0; i < _alerts.GetChildCount(); i++)
        {
            var card = (AlertCard)_alerts.GetChild(i);
            card.Visible = i < shown.Count;
            if (i < shown.Count) card.Bind(shown[i].Key, shown[i].Severity, shown[i].Text, shown[i].CanGo);
        }
    }

    private void PushTransient(AlertSeverity severity, string text)
    {
        _transient.Add(($"t{_transientCounter++}", severity, text, Time.GetTicksMsec() / 1000.0 + 5.0));
        if (_snap is not null) RefreshAlerts(_snap);
    }

    private void OnEvents(List<SimEvent> events)
    {
        foreach (var e in events)
        {
            switch (e)
            {
                case CommandRejected r:
                    PushTransient(AlertSeverity.Warning, UiText.T("event.rejected", r.Reason));
                    Audio.Sfx.Play("build_error");
                    break;
                case HouseholdLeft l: PushTransient(AlertSeverity.Critical, UiText.T("event.left", l.Name, l.Reason)); break;
                case BuildingCompleted c:
                    PushTransient(AlertSeverity.Info, UiText.T("event.completed", _host.World.GetBuilding(c.BuildingId) is { } done ? UiText.Bld(done.Def) : "?"));
                    break;
                case SimAlert a: PushTransient(AlertSeverity.Warning, a.Text); break;
            }
        }
    }

    // ------------------------------------------------------------------ input (guide §5.2)

    public override void _UnhandledKeyInput(InputEvent e)
    {
        if (e is not InputEventKey { Pressed: true, Echo: false } key) return;
        switch (key.Keycode)
        {
            case Key.Equal or Key.KpAdd when key.CtrlPressed: UiSettings.Change(GetTree().Root, +1); break;
            case Key.Minus or Key.KpSubtract when key.CtrlPressed: UiSettings.Change(GetTree().Root, -1); break;
            case Key.Space: _host.SetSpeed(_host.Paused ? 1 : 0); break;
            case Key.Key1: _host.SetSpeed(1); break;
            case Key.Key2: _host.SetSpeed(2); break;
            case Key.Key3: _host.SetSpeed(4); break;
            case Key.Key4: _host.SetSpeed(8); break;
            case Key.F: TogglePanel(_families); break;
            case Key.P: TogglePanel(_policies); break;
            case Key.F12 when _debug is not null: TogglePanel(_debug); break;
            case Key.F5: _host.QuickSave(); break;
            case Key.F9: _host.QuickLoad(); break;
            case Key.Escape when _build.Selected is null && !_build.RoadMode && _activePanel is not null: CloseActivePanel(); break;
            default: return;
        }
        GetViewport().SetInputAsHandled();
        Refresh();
    }

    public void ToggleDebug()
    {
        if (_debug is not null) TogglePanel(_debug);
    }

    /// <summary>Dev switch --panel=families|policies|building:&lt;id&gt; (captures and checks).</summary>
    public void OpenPanel(string name)
    {
        if (name == "families") ShowPanel(_families);
        else if (name == "settings") ShowPanel(_settings);
        else if (name == "away") ShowPanel(_away);
        else if (name == "policies") ShowPanel(_policies);
        else if (name.StartsWith("building:") && int.TryParse(name[9..], out int id)) OpenBuilding(id);
        else if (name.StartsWith("building:") && _host.World.Buildings.FirstOrDefault(b => b.Def.Id == name[9..]) is { } first)
        {
            OpenBuilding(first.Id);
            FocusOnBuilding(first.Id);   // dev captures look at it
        }
    }
}
