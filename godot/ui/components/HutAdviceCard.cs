using System.Collections.Generic;
using System.Linq;
using Godot;
using Ironvale.Sim.Commands;
using Ironvale.Sim.Map;
using Ironvale.Sim.Population;
using Ironvale.Sim.Scripting;
using Ironvale.Sim.Time;

namespace Ironvale.Game.UI;

/// <summary>
/// Step 0 → 1 of the delegation ladder (GDD v0.3 §4.6): after <c>suggestAfterActions</c> gather/hunt orders of one
/// kind within <c>suggestWindowDays</c>, and only while a family has no job, offers the hut that does it alone:
/// "Seus colonos vivem cortando árvores…" · [Mostrar onde] [Agora não] [Nunca]. Advice, not world state: it learns
/// from the commands this session sent (the session log has them), so saves and hashes are untouched.
/// </summary>
public partial class HutAdviceCard : PanelContainer
{
    private static readonly string[] Huts = { "woodcutter", "gatherer", "hunting_camp" };

    private SimHost _host = null!;
    private BuildController _build = null!;
    private CameraRig _camera = null!;
    private WorldView _view = null!;
    private readonly Dictionary<string, List<long>> _orders = Huts.ToDictionary(h => h, _ => new List<long>());
    private readonly Dictionary<string, long> _snoozedUntil = new();
    private readonly HashSet<string> _muted = new();
    private string? _shown;
    private Label _why = null!;
    private Label _what = null!;
    private double _timer;

    public void Init(SimHost host, BuildController build, CameraRig camera, WorldView view)
    {
        _host = host;
        _build = build;
        _camera = camera;
        _view = view;
        ThemeTypeVariation = "AlertInfo";
        Visible = false;
        var box = new VBoxContainer();
        AddChild(box);
        box.AddChild(UiNodes.Label(UiText.T("advice.title"), "HeaderLabel"));
        _why = UiNodes.Label("", "SecondaryLabel", wrap: true);
        box.AddChild(_why);
        _what = UiNodes.Label("", wrap: true);
        box.AddChild(_what);
        var buttons = new HBoxContainer();
        buttons.AddChild(UiNodes.Button(UiText.T("advice.show"), ShowWhere));
        buttons.AddChild(UiNodes.Button(UiText.T("suggestion.later"), () => Snooze(_host.World.Content.Balance.SuggestSnoozeDays)));
        buttons.AddChild(UiNodes.Button(UiText.T("suggestion.never"), () =>
        {
            if (_shown is not null) _muted.Add(_shown);
            Hide(null);
        }));
        box.AddChild(buttons);
        host.CommandSent += OnCommand;
        host.WorldReplaced += () =>
        {
            foreach (var list in _orders.Values) list.Clear();
            _snoozedUntil.Clear();
            Hide(null);
        };
    }

    private void OnCommand(SimCommand command)
    {
        var w = _host.World;
        if (w.Terrain is null || command is not OrderUnits o) return;
        string? hut = o.Kind switch
        {
            OrderKind.Hunt => "hunting_camp",
            OrderKind.Gather => w.Nature?.At(o.Cell).Kind switch
            {
                NodeKind.Tree => "woodcutter",
                NodeKind.Bush or NodeKind.Mushroom => "gatherer",
                _ => null,
            },
            _ => null,
        };
        if (hut is not null) _orders[hut].Add(w.Tick);
    }

    public override void _Process(double delta)
    {
        if (_host is null || _host.IsBusy) return;
        _timer -= delta;
        if (_timer > 0) return;
        _timer = 0.5;
        var w = _host.World;
        if (w.Terrain is null) return;
        var bal = w.Content.Balance;
        long window = (long)bal.SuggestWindowDays * SimTime.TicksPerDay;
        bool idleFamily = w.Households.Any(h => !h.HasJob);
        string? pick = null;
        foreach (var hut in Huts)
        {
            _orders[hut].RemoveAll(t => t < w.Tick - window);
            if (pick is null && idleFamily && !_muted.Contains(hut) && w.Tick >= _snoozedUntil.GetValueOrDefault(hut)
                && _orders[hut].Count >= bal.SuggestAfterActions && w.Buildings.All(b => b.Def.Id != hut))
                pick = hut;
        }
        if (pick == _shown) return;
        if (pick is null)
        {
            Hide(null);
            return;
        }
        _shown = pick;
        var def = w.Content.Building(pick);
        _why.Text = UiText.T($"advice.{pick}.why", _orders[pick].Count, bal.SuggestWindowDays);
        _what.Text = UiText.T($"advice.{pick}.what", UiText.Bld(def), def.WorkRadius);
        Visible = true;
        Audio.Sfx.Play("suggestion");
    }

    /// <summary>"Mostrar onde": the camera goes to the best spot (most trees / bushes / game in reach) and the build tool
    /// opens with the hut, so one click places it.</summary>
    private void ShowWhere()
    {
        if (_shown is null) return;
        var w = _host.World;
        var def = w.Content.Building(_shown);
        if (Placement.Find(w, _shown, Placement.FlatHallCenter) is { } spot)
            _camera.FocusOn(_view.CellCenter(new Cell(spot.X + def.FootprintW / 2, spot.Y + def.FootprintH / 2)), 45);
        _build.Begin(def);
        Snooze(1);
    }

    private void Snooze(int days)
    {
        if (_shown is not null) _snoozedUntil[_shown] = _host.World.Tick + (long)days * SimTime.TicksPerDay;
        Hide(null);
    }

    private void Hide(string? keep)
    {
        _shown = keep;
        Visible = false;
    }
}
