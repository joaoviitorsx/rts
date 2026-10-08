using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Ironvale.Sim;
using Ironvale.Sim.Buildings;
using Ironvale.Sim.Commands;
using Ironvale.Sim.Content;
using Ironvale.Sim.Events;
using Ironvale.Sim.Population;
using Ironvale.Sim.Save;
using Ironvale.Sim.Telemetry;
using Ironvale.Sim.Time;

namespace Ironvale.Game.UI;

/// <summary>
/// HUD (resources, date, speed), build bar, building inspector, families/policies panel,
/// debug panel (flows per resource, chart, "advance N years") and toasts.
/// Reads the world, sends commands. Built in code to keep the scene files trivial.
/// </summary>
public partial class GameUI : CanvasLayer
{
    private SimHost _host = null!;
    private BuildController _build = null!;
    private WorldView _view = null!;
    private double _refreshTimer;

    // HUD
    private readonly Dictionary<int, Label> _resourceLabels = new();
    private Label _dateLabel = null!;
    private Label _popLabel = null!;
    private readonly List<Button> _speedButtons = new();

    // Panels
    private PanelContainer _inspector = null!;
    private VBoxContainer _inspectorBody = null!;
    private string _inspectorSignature = "";
    private int _inspectedId;
    private PanelContainer _sidePanel = null!;
    private VBoxContainer _familiesBody = null!;
    private VBoxContainer _policiesList = null!;
    private RichTextLabel _policyLog = null!;
    private string _policiesSignature = "";
    private OptionButton _policyResource = null!;
    private SpinBox _policyThreshold = null!;

    // Debug
    private PanelContainer _debug = null!;
    private GridContainer _flowTable = null!;
    private OptionButton _chartResource = null!;
    private SeriesChart _chart = null!;
    private SpinBox _advanceYears = null!;
    private Label _debugStatus = null!;
    private RichTextLabel _eventLog = null!;

    // Toasts
    private VBoxContainer _toasts = null!;

    public void Init(SimHost host, BuildController build, WorldView view)
    {
        _host = host;
        _build = build;
        _view = view;
        _host.EventsReceived += OnEvents;
        _host.Message += Toast;
        _host.WorldReplaced += () =>
        {
            _inspectorSignature = "";
            _policiesSignature = "";
            Inspect(0);
            Refresh();
        };
        _build.BuildingSelected += Inspect;

        var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore, Theme = CreateTheme() };
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);

        root.AddChild(CreateTopBar());
        root.AddChild(CreateBuildBar());
        root.AddChild(_inspector = CreateInspector());
        root.AddChild(_sidePanel = CreateSidePanel());
        root.AddChild(_debug = CreateDebugPanel());

        _toasts = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        _toasts.SetAnchorsPreset(Control.LayoutPreset.CenterTop);
        _toasts.Position = new Vector2(-200, 56);
        _toasts.CustomMinimumSize = new Vector2(400, 0);
        root.AddChild(_toasts);

        Inspect(0);
        Refresh();
    }

    // ================================================================ layout

    private Control CreateTopBar()
    {
        var panel = new PanelContainer();
        panel.SetAnchorsPreset(Control.LayoutPreset.TopWide);
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 10);
        panel.AddChild(row);

        foreach (var r in _host.Content.Resources)
        {
            var label = new Label { TooltipText = r.Name, MouseFilter = Control.MouseFilterEnum.Pass };
            _resourceLabels[r.Index] = label;
            row.AddChild(label);
        }
        row.AddChild(new VSeparator());
        _popLabel = new Label();
        row.AddChild(_popLabel);
        row.AddChild(new VSeparator());
        _dateLabel = new Label { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        row.AddChild(_dateLabel);

        foreach (int speed in SimHost.Speeds)
        {
            var b = new Button { Text = speed == 0 ? "⏸" : $"{speed}x", ToggleMode = true, FocusMode = Control.FocusModeEnum.None };
            b.CustomMinimumSize = new Vector2(34, 0);
            b.TooltipText = speed == 0 ? "Pausa (Espaço)" : $"Velocidade {speed}x (teclas 1–4)";
            b.Pressed += () => _host.SetSpeed(speed);
            _speedButtons.Add(b);
            row.AddChild(b);
        }
        row.AddChild(new VSeparator());
        row.AddChild(MakeButton("Painel (P)", () => _sidePanel.Visible = !_sidePanel.Visible));
        row.AddChild(MakeButton("Debug", () => _debug.Visible = !_debug.Visible));
        row.AddChild(MakeButton("Salvar", _host.QuickSave));
        row.AddChild(MakeButton("Carregar", _host.QuickLoad));
        return panel;
    }

    private Control CreateBuildBar()
    {
        var panel = new PanelContainer();
        panel.SetAnchorsPreset(Control.LayoutPreset.CenterBottom);
        panel.GrowHorizontal = Control.GrowDirection.Both;
        panel.GrowVertical = Control.GrowDirection.Begin;
        var row = new HBoxContainer();
        panel.AddChild(row);
        row.AddChild(new Label { Text = "Construir:" });
        foreach (var def in _host.Content.Buildings.Where(d => d.Buildable))
        {
            string cost = string.Join(", ", def.Cost.Select((q, r) => (q, r)).Where(x => x.q.IsPositive)
                .Select(x => $"{x.q} {_host.Content.Resources[x.r].Name}"));
            var b = MakeButton(def.Name, () => _build.Begin(def));
            b.TooltipText = $"{def.Name}\nCusto: {cost}\nObra: {def.BuildDays} dias\nR = girar · Shift = construir vários · botão direito = cancelar";
            row.AddChild(b);
        }
        return panel;
    }

    private PanelContainer CreateInspector()
    {
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(320, 0) };
        panel.SetAnchorsPreset(Control.LayoutPreset.TopRight);
        panel.Position = new Vector2(-330, 50);
        panel.GrowHorizontal = Control.GrowDirection.Begin;
        var scroll = new ScrollContainer { CustomMinimumSize = new Vector2(320, 420), HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        panel.AddChild(scroll);
        _inspectorBody = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        scroll.AddChild(_inspectorBody);
        return panel;
    }

    private PanelContainer CreateSidePanel()
    {
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(380, 0), Visible = true };
        panel.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        panel.Position = new Vector2(8, 50);
        var tabs = new TabContainer { CustomMinimumSize = new Vector2(380, 460) };
        panel.AddChild(tabs);

        var families = new ScrollContainer { Name = "Famílias", HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        _familiesBody = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        families.AddChild(_familiesBody);
        tabs.AddChild(families);

        var policies = new VBoxContainer { Name = "Políticas" };
        var create = new HBoxContainer();
        create.AddChild(new Label { Text = "Manter" });
        _policyResource = new OptionButton();
        foreach (var r in _host.Content.Resources) _policyResource.AddItem(r.Name, r.Index);
        create.AddChild(_policyResource);
        create.AddChild(new Label { Text = "acima de" });
        _policyThreshold = new SpinBox { MinValue = 0, MaxValue = 100000, Step = 10, Value = 100 };
        create.AddChild(_policyThreshold);
        create.AddChild(MakeButton("Criar", () =>
        {
            var res = _host.Content.Resources[_policyResource.GetSelectedId()];
            _host.Send(new CreatePolicy("keep_above", res.Id, (long)_policyThreshold.Value));
        }));
        policies.AddChild(create);
        _policiesList = new VBoxContainer();
        policies.AddChild(_policiesList);
        policies.AddChild(new Label { Text = "Registro do administrador:" });
        _policyLog = new RichTextLabel { CustomMinimumSize = new Vector2(360, 200), ScrollFollowing = true, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        policies.AddChild(_policyLog);
        tabs.AddChild(policies);
        return panel;
    }

    private PanelContainer CreateDebugPanel()
    {
        var panel = new PanelContainer { Visible = false, Position = new Vector2(396, 50) };
        var box = new VBoxContainer { CustomMinimumSize = new Vector2(640, 0) };
        panel.AddChild(box);

        box.AddChild(new Label { Text = "Fluxos por recurso (último dia · média 30 dias)" });
        _flowTable = new GridContainer { Columns = 8 };
        box.AddChild(_flowTable);

        var chartRow = new HBoxContainer();
        chartRow.AddChild(new Label { Text = "Gráfico (último ano):" });
        _chartResource = new OptionButton();
        foreach (var r in _host.Content.Resources) _chartResource.AddItem(r.Name, r.Index);
        _chartResource.Select(_host.Content.Resource("food").Index);
        chartRow.AddChild(_chartResource);
        box.AddChild(chartRow);
        _chart = new SeriesChart { CustomMinimumSize = new Vector2(620, 140) };
        box.AddChild(_chart);

        var advance = new HBoxContainer();
        advance.AddChild(new Label { Text = "Avançar" });
        _advanceYears = new SpinBox { MinValue = 1, MaxValue = 200, Value = 10 };
        advance.AddChild(_advanceYears);
        advance.AddChild(new Label { Text = "anos" });
        advance.AddChild(MakeButton("Avançar (headless)", () => _host.AdvanceYears((int)_advanceYears.Value)));
        advance.AddChild(MakeButton("Hash do estado", () => Toast($"Hash: {SaveSerializer.StateHashHex(_host.World)}")));
        advance.AddChild(MakeButton("Novo jogo", _host.NewGame));
        box.AddChild(advance);
        _debugStatus = new Label();
        box.AddChild(_debugStatus);
        _eventLog = new RichTextLabel { CustomMinimumSize = new Vector2(620, 90), ScrollFollowing = true };
        box.AddChild(_eventLog);
        return panel;
    }

    // ================================================================ refresh

    public override void _Process(double delta)
    {
        if (_host.IsBusy)
        {
            _dateLabel.Text = $"Simulando… {_host.AdvanceProgress:P0}";
            return;
        }
        _refreshTimer -= delta;
        if (_refreshTimer > 0) return;
        _refreshTimer = 0.2;
        Refresh();
    }

    private void Refresh()
    {
        var w = _host.World;
        var last = w.Telemetry.LastDay;
        foreach (var (r, label) in _resourceLabels)
        {
            var res = w.Content.Resources[r];
            string trend = "";
            if (last is not null)
            {
                long net = last.Produced[r] - last.Consumed[r];
                trend = net > 0 ? " ↑" : net < 0 ? " ↓" : "";
            }
            label.Text = $"{res.Name}: {w.StorageStock(r).WholeUnits}{trend}";
        }
        _popLabel.Text = $"Famílias: {w.Households.Count}";
        _dateLabel.Text = SimHost.DateText(w.Calendar) + (w.Calendar.IsWinter ? "  ❄" : "");
        for (int i = 0; i < _speedButtons.Count; i++) _speedButtons[i].SetPressedNoSignal(SimHost.Speeds[i] == _host.Speed);

        RefreshInspector(w);
        RefreshFamilies(w);
        RefreshPolicies(w);
        if (_debug.Visible) RefreshDebug(w);
    }

    // ---------------------------------------------------------------- inspector

    private void Inspect(int buildingId)
    {
        _inspectedId = buildingId;
        _view.SelectedBuildingId = buildingId;
        _inspectorSignature = "";
        _inspector.Visible = buildingId != 0;
        if (buildingId != 0) RefreshInspector(_host.World);
    }

    private void RefreshInspector(World w)
    {
        var b = w.GetBuilding(_inspectedId);
        if (b is null)
        {
            _inspector.Visible = false;
            return;
        }
        string signature = $"{b.Id}|{b.IsActive}|{b.BuildProgressDays}|{b.Recipe?.Id}|{string.Join(',', b.SlotHouseholds)}|" +
                           $"{string.Join(',', Enumerable.Range(0, w.Content.ResourceCount).Select(r => b.Stock.Get(r).WholeUnits))}|" +
                           $"{w.Households.Count}|{string.Join(',', w.Households.Select(h => h.JobBuildingId))}";
        if (signature == _inspectorSignature) return;
        _inspectorSignature = signature;
        Clear(_inspectorBody);

        _inspectorBody.AddChild(new Label { Text = $"{b.Def.Name} #{b.Id}", ThemeTypeVariation = "HeaderMedium" });
        if (!b.IsActive)
        {
            _inspectorBody.AddChild(new Label { Text = $"Em obra: {b.BuildProgressDays}/{b.Def.BuildDays} dias" });
            _inspectorBody.AddChild(MakeButton("Cancelar obra (reembolsa)", () => _host.Send(new CancelConstruction(b.Id))));
            return;
        }

        if (b.Def.Has(BuildingRole.Housing))
        {
            var residents = w.Households.Where(h => h.HomeId == b.Id).Select(h => h.Name).ToList();
            _inspectorBody.AddChild(new Label { Text = $"Moradores ({residents.Count}/{b.Def.HousingCapacity}): {string.Join(", ", residents)}" });
        }

        if (b.Def.Recipes.Count > 1)
        {
            var recipes = new OptionButton();
            foreach (var r in b.Def.Recipes) recipes.AddItem(r.Name);
            recipes.Select(b.Def.Recipes.ToList().IndexOf(b.Recipe!));
            recipes.ItemSelected += i => _host.Send(new SetRecipe(b.Id, b.Def.Recipes[(int)i].Id));
            var row = new HBoxContainer();
            row.AddChild(new Label { Text = "Produz:" });
            row.AddChild(recipes);
            _inspectorBody.AddChild(row);
        }
        else if (b.Recipe is not null)
        {
            string season = b.Recipe.Kind == RecipeKind.Seasonal ? " (trabalho na primavera/verão, colheita na virada da estação)" : "";
            _inspectorBody.AddChild(new Label { Text = $"Produz: {b.Recipe.Name}{season}", AutowrapMode = TextServer.AutowrapMode.WordSmart });
        }

        if (b.Stock.Capacity.IsPositive)
        {
            _inspectorBody.AddChild(new Label { Text = $"Estoque {b.Stock.Total.WholeUnits}/{b.Stock.Capacity.WholeUnits}:" });
            for (int r = 0; r < w.Content.ResourceCount; r++)
            {
                if (!b.Stock.Get(r).IsPositive) continue;
                _inspectorBody.AddChild(new Label { Text = $"   {w.Content.Resources[r].Name}: {b.Stock.Get(r)}" });
            }
            if (b.SeasonalWorkMilli > 0)
                _inspectorBody.AddChild(new Label { Text = $"   colheita prevista: ~{b.SeasonalWorkMilli / 1000}" });
        }

        if (b.Def.JobSlots > 0)
        {
            string role = b.IsStorage ? "Carregadores" : "Trabalhadores";
            _inspectorBody.AddChild(new HSeparator());
            _inspectorBody.AddChild(new Label { Text = $"{role} ({b.AssignedCount}/{b.Def.JobSlots}):" });
            foreach (int id in b.SlotHouseholds)
            {
                var row = new HBoxContainer();
                var h = w.GetHousehold(id);
                row.AddChild(new Label { Text = h is null ? "   (vaga livre)" : $"   Família {h.Name} — {Source(h)}", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
                if (h is not null) row.AddChild(MakeButton("Remover", () => _host.Send(new UnassignHousehold(h.Id))));
                _inspectorBody.AddChild(row);
            }

            if (b.FreeSlotIndex() >= 0)
            {
                var pick = new OptionButton { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
                var candidates = w.Households.Where(h => h.JobBuildingId != b.Id).ToList();
                foreach (var h in candidates)
                {
                    var job = w.GetBuilding(h.JobBuildingId);
                    pick.AddItem($"{h.Name} ({(job is null ? "sem emprego" : job.Def.Name)})", h.Id);
                }
                var row = new HBoxContainer();
                row.AddChild(pick);
                row.AddChild(MakeButton("Designar", () =>
                {
                    if (pick.ItemCount > 0) _host.Send(new AssignHousehold(pick.GetSelectedId(), b.Id));
                }));
                _inspectorBody.AddChild(row);
            }
        }
    }

    private static string Source(Household h) => h.AssignedBy switch
    {
        AssignmentSource.Player => "manual",
        AssignmentSource.Policy => $"política #{h.AssignedByPolicyId}",
        _ => "livre",
    };

    // ---------------------------------------------------------------- families / policies

    private void RefreshFamilies(World w)
    {
        if (!_sidePanel.Visible) return;
        Clear(_familiesBody);
        foreach (var h in w.Households)
        {
            var job = w.GetBuilding(h.JobBuildingId);
            string state = h.State switch
            {
                HouseholdState.Working => "trabalhando",
                HouseholdState.Hauling => "carregando",
                _ => "subsistência",
            };
            string warn = (h.FoodDeficitDays > 0 ? $" · fome {h.FoodDeficitDays}d" : "") +
                          (h.ColdDeficitDays > 0 ? $" · frio {h.ColdDeficitDays}d" : "") +
                          (h.HomeId == 0 ? " · sem casa" : "");
            var label = new Label
            {
                Text = $"{h.Name} ({h.Members}) — {(job is null ? "sem emprego" : w.DescribeBuilding(job))} · {state}\n" +
                       $"   ferramenta {h.ToolCondition / 10}% · produtividade {h.ProductivityPermille / 10}%{warn}",
            };
            if (warn.Length > 0) label.Modulate = new Color(1, 0.75f, 0.6f);
            _familiesBody.AddChild(label);
        }
    }

    private void RefreshPolicies(World w)
    {
        if (!_sidePanel.Visible) return;
        string signature = string.Join('|', w.Policies.Select(p => $"{p.Id}:{p.Enabled}:{p.Threshold.Milli}"));
        if (signature != _policiesSignature)
        {
            _policiesSignature = signature;
            Clear(_policiesList);
            foreach (var p in w.Policies)
            {
                var row = new HBoxContainer();
                var enabled = new CheckBox { ButtonPressed = p.Enabled, TooltipText = "Ativa" };
                enabled.Toggled += on => _host.Send(new SetPolicyEnabled(p.Id, on));
                row.AddChild(enabled);
                row.AddChild(new Label { Text = $"#{p.Id} Manter {w.Content.Resources[p.Resource].Name} ≥", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
                var threshold = new SpinBox { MinValue = 0, MaxValue = 100000, Step = 10, Value = p.Threshold.WholeUnits };
                threshold.ValueChanged += v => _host.Send(new SetPolicyThreshold(p.Id, (long)v));
                row.AddChild(threshold);
                row.AddChild(MakeButton("✕", () => _host.Send(new RemovePolicy(p.Id))));
                _policiesList.AddChild(row);
            }
        }
        _policyLog.Text = string.Join('\n', w.PolicyLog.TakeLast(30).Select(e => $"[{ShortDate(e.Tick)}] {e.Text}"));
    }

    // ---------------------------------------------------------------- debug

    private void RefreshDebug(World w)
    {
        Clear(_flowTable);
        foreach (var h in new[] { "Recurso", "Prod/dia", "Cons/dia", "Prod 30d", "Cons 30d", "Armazém", "Local", "Trânsito" })
            _flowTable.AddChild(new Label { Text = h, Modulate = new Color(1, 0.9f, 0.6f) });

        var days = w.Telemetry.Daily.ToList();
        var last = days.LastOrDefault();
        var month = days.TakeLast(30).ToList();
        for (int r = 0; r < w.Content.ResourceCount; r++)
        {
            double Avg(Func<DailySampleView, long> f) => month.Count == 0 ? 0 : month.Average(d => f(new DailySampleView(d, r))) / 1000.0;
            _flowTable.AddChild(new Label { Text = w.Content.Resources[r].Name });
            _flowTable.AddChild(new Label { Text = last is null ? "-" : $"{last.Produced[r] / 1000.0:0.#}" });
            _flowTable.AddChild(new Label { Text = last is null ? "-" : $"{last.Consumed[r] / 1000.0:0.#}" });
            _flowTable.AddChild(new Label { Text = $"{Avg(d => d.Produced):0.#}" });
            _flowTable.AddChild(new Label { Text = $"{Avg(d => d.Consumed):0.#}" });
            _flowTable.AddChild(new Label { Text = last is null ? "-" : $"{last.Stored[r] / 1000.0:0}" });
            _flowTable.AddChild(new Label { Text = last is null ? "-" : $"{last.Local[r] / 1000.0:0}" });
            _flowTable.AddChild(new Label { Text = last is null ? "-" : $"{last.Transit[r] / 1000.0:0}" });
        }

        int res = _chartResource.GetSelectedId();
        _chart.SetSeries(new[]
        {
            new SeriesChart.Series("Armazém", new Color(0.95f, 0.85f, 0.4f), days.Select(d => d.Stored[res] / 1000.0).ToList()),
            new SeriesChart.Series("Produção/dia", new Color(0.5f, 0.9f, 0.5f), days.Select(d => d.Produced[res] / 1000.0).ToList()),
            new SeriesChart.Series("Consumo/dia", new Color(0.95f, 0.5f, 0.45f), days.Select(d => d.Consumed[res] / 1000.0).ToList()),
        });

        _debugStatus.Text = $"Tick {w.Tick} · {w.Shipments.Count} cargas em trânsito · " +
                            $"economia parada há {w.Telemetry.FrozenDays} dias · deadlock: {(w.Telemetry.Deadlocked ? "SIM" : "não")}";
    }

    private readonly record struct DailySampleView(DailySample Sample, int Resource)
    {
        public long Produced => Sample.Produced[Resource];
        public long Consumed => Sample.Consumed[Resource];
    }

    // ---------------------------------------------------------------- events & input

    private void OnEvents(List<SimEvent> events)
    {
        foreach (var e in events)
        {
            string? text = e switch
            {
                CommandRejected r => $"Não foi possível: {r.Reason}",
                HouseholdLeft l => $"A família {l.Name} foi embora ({l.Reason})",
                BuildingCompleted c => $"{_host.World.GetBuilding(c.BuildingId)?.Def.Name ?? "Edifício"} concluído",
                SimAlert a => a.Text,
                _ => null,
            };
            if (text is null) continue;
            if (e is CommandRejected or HouseholdLeft or SimAlert) Toast(text);
            _eventLog.AppendText($"[{ShortDate(e.Tick)}] {text}\n");
        }
    }

    public override void _UnhandledKeyInput(InputEvent e)
    {
        if (e is not InputEventKey { Pressed: true, Echo: false } key) return;
        switch (key.Keycode)
        {
            case Key.Space: _host.SetSpeed(_host.Paused ? 1 : 0); break;
            case Key.Key1: _host.SetSpeed(1); break;
            case Key.Key2: _host.SetSpeed(2); break;
            case Key.Key3: _host.SetSpeed(4); break;
            case Key.Key4: _host.SetSpeed(8); break;
            case Key.F3: _debug.Visible = !_debug.Visible; break;
            case Key.P: _sidePanel.Visible = !_sidePanel.Visible; break;
            case Key.F5: _host.QuickSave(); break;
            case Key.F9: _host.QuickLoad(); break;
            case Key.Escape when _build.Selected is null: Inspect(0); break;
            default: return;
        }
        GetViewport().SetInputAsHandled();
        Refresh();
    }

    public void ToggleDebug()
    {
        _debug.Visible = !_debug.Visible;
        Refresh();
    }

    private void Toast(string text)
    {
        var label = new Label { Text = text, HorizontalAlignment = HorizontalAlignment.Center };
        label.AddThemeColorOverride("font_outline_color", Colors.Black);
        label.AddThemeConstantOverride("outline_size", 6);
        _toasts.AddChild(label);
        var tween = label.CreateTween();
        tween.TweenInterval(3.0);
        tween.TweenProperty(label, "modulate:a", 0.0, 0.8);
        tween.TweenCallback(Callable.From(label.QueueFree));
    }

    // ---------------------------------------------------------------- helpers

    private string ShortDate(long tick)
    {
        var c = new Calendar(tick);
        return $"A{c.Year} M{c.MonthOfYear + 1} D{c.DayOfMonth + 1}";
    }

    private static Theme CreateTheme()
    {
        var theme = new Theme();
        theme.SetStylebox("panel", "PanelContainer", new StyleBoxFlat
        {
            BgColor = new Color(0.16f, 0.18f, 0.16f, 0.93f),
            ContentMarginLeft = 8, ContentMarginRight = 8, ContentMarginTop = 6, ContentMarginBottom = 6,
            CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6, CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6,
        });
        return theme;
    }

    private static Button MakeButton(string text, Action onPressed)
    {
        var b = new Button { Text = text, FocusMode = Control.FocusModeEnum.None };
        b.Pressed += onPressed;
        return b;
    }

    private static void Clear(Node node)
    {
        foreach (var child in node.GetChildren())
        {
            node.RemoveChild(child);
            child.QueueFree();
        }
    }
}
