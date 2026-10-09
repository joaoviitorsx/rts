using System;
using System.Linq;
using Godot;

namespace Ironvale.Game.UI;

/// <summary>Dev-only telemetry (guide §2.3: never shipped to players): flows table, yearly chart, advance N years.</summary>
public partial class DebugPanel : PanelContainer
{
    public event Action? Closed;
    public event Action<int>? AdvanceYears;
    public event Action? ShowHash;
    public event Action? NewGame;

    private GridContainer _table = null!;
    private OptionButton _chartResource = null!;
    private SeriesChart _chart = null!;
    private SpinBox _years = null!;
    private Label _status = null!;
    private bool _resourcesFilled;

    public override void _Ready()
    {
        ThemeTypeVariation = "PanelPrimary";
        MouseFilter = MouseFilterEnum.Stop;
        var box = new VBoxContainer { CustomMinimumSize = new Vector2(600, 0) };
        AddChild(box);
        box.AddChild(UiNodes.Header(UiText.T("debug.title"), () => Closed?.Invoke()));
        box.AddChild(UiNodes.Label(UiText.T("debug.flows"), "SecondaryLabel"));
        _table = new GridContainer { Columns = 8 };
        box.AddChild(_table);
        var chartRow = new HBoxContainer();
        chartRow.AddChild(UiNodes.Label(UiText.T("debug.chart"), "SecondaryLabel"));
        _chartResource = new OptionButton { FocusMode = FocusModeEnum.None };
        chartRow.AddChild(_chartResource);
        box.AddChild(chartRow);
        _chart = new SeriesChart { CustomMinimumSize = new Vector2(580, 140) };
        box.AddChild(_chart);
        var actions = new HBoxContainer();
        actions.AddChild(UiNodes.Label(UiText.T("debug.advance")));
        _years = new SpinBox { MinValue = 1, MaxValue = 200, Value = 10 };
        actions.AddChild(_years);
        actions.AddChild(UiNodes.Label(UiText.T("debug.years")));
        actions.AddChild(UiNodes.Button(UiText.T("debug.advance_button"), () => AdvanceYears?.Invoke((int)_years.Value)));
        actions.AddChild(UiNodes.Button(UiText.T("debug.hash"), () => ShowHash?.Invoke()));
        actions.AddChild(UiNodes.Button(UiText.T("debug.new_game"), () => NewGame?.Invoke()));
        box.AddChild(actions);
        _status = UiNodes.Label("", "SecondaryLabel", wrap: true);
        box.AddChild(_status);
    }

    public void Bind(UiSnapshot snap)
    {
        if (!_resourcesFilled)
        {
            foreach (var r in snap.Resources) _chartResource.AddItem(r.Name);
            _chartResource.Select(Math.Max(0, snap.Resources.ToList().FindIndex(r => r.Id == "food")));
            _resourcesFilled = true;
        }
        UiNodes.Clear(_table);
        foreach (var h in new[] { "resource", "prod", "cons", "prod30", "cons30", "stored", "local", "transit" })
            _table.AddChild(UiNodes.Label(UiText.T("debug.col." + h), "SecondaryLabel"));
        var last = snap.Daily.LastOrDefault();
        var month = snap.Daily.TakeLast(30).ToList();
        for (int r = 0; r < snap.Resources.Count; r++)
        {
            int ri = r;
            double Avg(Func<DailySnap, long> f) => month.Count == 0 ? 0 : month.Average(d => (double)f(d)) / 1000.0;
            _table.AddChild(UiNodes.Label(snap.Resources[r].Name));
            _table.AddChild(UiNodes.Label(last is null ? "-" : $"{last.Produced[r] / 1000.0:0.#}"));
            _table.AddChild(UiNodes.Label(last is null ? "-" : $"{last.Consumed[r] / 1000.0:0.#}"));
            _table.AddChild(UiNodes.Label($"{Avg(d => d.Produced[ri]):0.#}"));
            _table.AddChild(UiNodes.Label($"{Avg(d => d.Consumed[ri]):0.#}"));
            _table.AddChild(UiNodes.Label(last is null ? "-" : $"{last.Stored[r] / 1000.0:0}"));
            _table.AddChild(UiNodes.Label(last is null ? "-" : $"{last.Local[r] / 1000.0:0}"));
            _table.AddChild(UiNodes.Label(last is null ? "-" : $"{last.Transit[r] / 1000.0:0}"));
        }
        int res = Math.Max(0, _chartResource.Selected);
        _chart.SetSeries(new[]
        {
            new SeriesChart.Series(UiText.T("debug.series.stock"), "series_stock", snap.Daily.Select(d => d.Stored[res] / 1000.0).ToList()),
            new SeriesChart.Series(UiText.T("debug.series.prod"), "series_produced", snap.Daily.Select(d => d.Produced[res] / 1000.0).ToList()),
            new SeriesChart.Series(UiText.T("debug.series.cons"), "series_consumed", snap.Daily.Select(d => d.Consumed[res] / 1000.0).ToList()),
        });
        _status.Text = UiText.T("debug.status", snap.Tick, snap.Shipments, snap.FrozenDays,
            UiText.T(snap.Deadlocked ? "debug.yes" : "debug.no"));
    }
}
