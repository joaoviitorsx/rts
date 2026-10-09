using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Ironvale.Sim;
using Ironvale.Sim.Commands;
using Ironvale.Sim.Map;
using Ironvale.Sim.Population;

namespace Ironvale.Game;

/// <summary>
/// RTS controls of the opening (GDD v0.3 §4.1–§4.2, briefing step 2). Selection lives in the view only:
/// click a unit · drag a box · Shift adds/removes · double-click picks the visible colonists doing the same thing ·
/// Esc clears. Right click sends one order to the selection, chosen by what is under the cursor: deer/rabbit → hunt,
/// wolf → scare, something on the ground → pick up, construction site → build, storage → deposit (Ctrl: split logs
/// into firewood), tree/stone/bush/mushrooms → gather, anything else → move. Shift + right click queues.
/// Inactive while placing a building or a road; clicks on nothing fall through to building selection.
/// </summary>
public partial class UnitController : Node
{
    private const float PickPixels = 30f;
    private const float DragPixels = 8f;

    private SimHost _host = null!;
    private CameraRig _camera = null!;
    private WorldView _view = null!;
    private BuildController _build = null!;
    private Vector2? _press;
    private bool _dragging;
    private Panel _box = null!;

    public HashSet<int> Selected => _view.SelectedUnits;
    public event Action? SelectionChanged;

    public void Init(SimHost host, CameraRig camera, WorldView view, BuildController build)
    {
        _host = host;
        _camera = camera;
        _view = view;
        _build = build;
        var layer = new CanvasLayer { Layer = 5 };
        AddChild(layer);
        _box = new Panel { ThemeTypeVariation = "SelectionBox", Visible = false, MouseFilter = Control.MouseFilterEnum.Ignore,
            Theme = GD.Load<Theme>("res://ui/theme/main_theme.tres") };
        layer.AddChild(_box);
        _host.WorldReplaced += () => SetSelection(Array.Empty<int>(), add: false);
    }

    private bool Active => !_host.IsBusy && _build.Selected is null && !_build.RoadMode && _host.World.Units.Count > 0;

    public override void _UnhandledInput(InputEvent e)
    {
        if (!Active) return;
        switch (e)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true, DoubleClick: true } dbl when UnitAt(dbl.Position) is { } u:
                SelectSameTask(u);
                GetViewport().SetInputAsHandled();
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } down:
                _press = down.Position;
                _dragging = false;
                if (UnitAt(down.Position) is { } hit)
                {
                    bool shift = Input.IsKeyPressed(Key.Shift);
                    if (shift && Selected.Contains(hit.Id)) { Selected.Remove(hit.Id); SelectionChanged?.Invoke(); }
                    else SetSelection(new[] { hit.Id }, add: shift);
                    _press = null;
                    GetViewport().SetInputAsHandled();
                }
                break;
            case InputEventMouseMotion motion when _press is { } start:
                if (!_dragging && motion.Position.DistanceTo(start) > DragPixels) _dragging = true;
                if (_dragging) ShowBox(start, motion.Position);
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false } up when _press is { } from:
                if (_dragging)
                {
                    var rect = new Rect2(from, Vector2.Zero).Expand(up.Position);
                    var ids = _host.World.Units.Where(u => u.Controllable && rect.HasPoint(ScreenOf(u))).Select(u => u.Id).ToList();
                    SetSelection(ids, add: Input.IsKeyPressed(Key.Shift));
                    GetViewport().SetInputAsHandled();
                }
                else if (!Input.IsKeyPressed(Key.Shift) && Selected.Count > 0)
                    SetSelection(Array.Empty<int>(), add: false);   // a click on the ground drops the selection
                _press = null;
                _dragging = false;
                _box.Visible = false;
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true } right when Selected.Count > 0:
                Order(right.Position, queue: Input.IsKeyPressed(Key.Shift), split: Input.IsKeyPressed(Key.Ctrl));
                GetViewport().SetInputAsHandled();
                break;
            case InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape } when Selected.Count > 0:
                SetSelection(Array.Empty<int>(), add: false);
                GetViewport().SetInputAsHandled();
                break;
        }
    }

    public void SetSelection(IEnumerable<int> ids, bool add)
    {
        if (!add) Selected.Clear();
        foreach (int id in ids) Selected.Add(id);
        SelectionChanged?.Invoke();
        if (Selected.Count > 0) Audio.Sfx.Play("ui_click");
    }

    public void Stop()
    {
        if (Selected.Count > 0) _host.Send(new StopUnits(Selected.ToArray()));
    }

    private void SelectSameTask(Unit unit)
    {
        var kind = unit.Order?.Kind;
        var screen = GetViewport().GetVisibleRect();
        var ids = _host.World.Units.Where(u => u.Controllable && u.Kind == unit.Kind && u.Order?.Kind == kind && screen.HasPoint(ScreenOf(u)))
            .Select(u => u.Id);
        SetSelection(ids, add: false);
    }

    private void ShowBox(Vector2 a, Vector2 b)
    {
        var rect = new Rect2(a, Vector2.Zero).Expand(b);
        _box.Position = rect.Position;
        _box.Size = rect.Size;
        _box.Visible = true;
    }

    // ------------------------------------------------------------------------------------------------ picking

    private Vector3 WorldOf(Cell c) => _view.CellCenter(c);

    private Vector2 ScreenOf(Unit u) => _camera.Camera.UnprojectPosition(WorldOf(u.Pos) + new Vector3(0, 0.9f, 0));

    private Unit? UnitAt(Vector2 screen) =>
        _host.World.Units.Where(u => u.Controllable)
            .Select(u => (u, d: ScreenOf(u).DistanceTo(screen)))
            .Where(x => x.d <= PickPixels && !_camera.Camera.IsPositionBehind(WorldOf(x.u.Pos)))
            .OrderBy(x => x.d).Select(x => x.u).FirstOrDefault();

    private Animal? AnimalAt(Vector2 screen) =>
        _host.World.Animals.Select(a => (a, d: _camera.Camera.UnprojectPosition(WorldOf(a.Pos) + new Vector3(0, 0.6f, 0)).DistanceTo(screen)))
            .Where(x => x.d <= PickPixels).OrderBy(x => x.d).Select(x => x.a).FirstOrDefault();

    private void Order(Vector2 screen, bool queue, bool split)
    {
        var w = _host.World;
        var ids = Selected.ToArray();
        SimCommand? cmd = null;
        if (AnimalAt(screen) is { } animal)
            cmd = new OrderUnits(ids, animal.Huntable ? OrderKind.Hunt : OrderKind.Scare, animal.Pos, animal.Id, queue);
        else if (_camera.GroundUnderMouse(screen) is { } hit)
        {
            float cs = _view.CellSize;
            var cell = new Cell(Mathf.FloorToInt(hit.X / cs), Mathf.FloorToInt(hit.Z / cs));
            var item = w.GroundItems.Where(g => World.Chebyshev(g.Cell, cell) <= 1).OrderBy(g => g.Cell.Manhattan(cell)).ThenBy(g => g.Id).FirstOrDefault();
            var building = w.GetBuilding(w.Map.BuildingAt(cell));
            if (item is not null) cmd = new OrderUnits(ids, OrderKind.Pickup, item.Cell, item.Id, queue);
            else if (building is { IsActive: false }) cmd = new OrderUnits(ids, OrderKind.Build, building.Center, building.Id, queue);
            else if (building is { IsStorage: true }) cmd = new OrderUnits(ids, split ? OrderKind.Split : OrderKind.Deposit, building.Center, building.Id, queue);
            else if (GatherableNear(w, cell) is { } node) cmd = new OrderUnits(ids, OrderKind.Gather, node, 0, queue);
            else if (w.Map.InBounds(cell)) cmd = new OrderUnits(ids, OrderKind.Move, cell, 0, queue);
        }
        if (cmd is null) return;
        _host.Send(cmd);
        Audio.Sfx.Play("ui_click");
        _view.FlashOrder(cmd is OrderUnits o ? o.Cell : default);
    }

    /// <summary>The clicked cell, or the nearest gatherable node right next to it (trees are drawn off-centre).</summary>
    private static Cell? GatherableNear(World w, Cell cell)
    {
        if (w.Gatherable(cell)) return cell;
        Cell? best = null;
        for (int dy = -1; dy <= 1; dy++)
        for (int dx = -1; dx <= 1; dx++)
        {
            var c = new Cell(cell.X + dx, cell.Y + dy);
            if (w.Map.InBounds(c) && w.Gatherable(c) && best is null) best = c;
        }
        return best;
    }
}
