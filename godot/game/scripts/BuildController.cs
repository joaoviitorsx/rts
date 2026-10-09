using System;
using System.Collections.Generic;
using Godot;
using Ironvale.Sim.Commands;
using Ironvale.Sim.Content;
using Cell = Ironvale.Sim.Map.Cell;
using GridMap = Ironvale.Sim.Map.GridMap;

namespace Ironvale.Game;

/// <summary>
/// Build mode (ghost on the grid, R to rotate, left click to place, right click/Esc to cancel), road mode
/// (drag an L-shaped line; Ctrl+drag removes) and building selection otherwise. Sends commands; never edits the world.
/// </summary>
public partial class BuildController : Node3D
{
    private SimHost _host = null!;
    private VisualCatalog _catalog = null!;
    private CameraRig _camera = null!;
    private WorldView _view = null!;
    private Node3D? _ghost;
    private string? _ghostDefId;
    private int _rotation;
    private Cell _cell;
    private bool _valid;

    public BuildingDef? Selected { get; private set; }
    public bool RoadMode { get; private set; }
    private Cell? _roadStart;
    private MultiMeshInstance3D? _roadGhost;

    public event Action<int>? BuildingSelected;
    public event Action? BuildModeChanged;

    public void Init(SimHost host, VisualCatalog catalog, CameraRig camera, WorldView view)
    {
        _host = host;
        _catalog = catalog;
        _camera = camera;
        _view = view;
    }

    public void BeginRoad()
    {
        Cancel();
        RoadMode = true;
        _roadGhost = new MultiMeshInstance3D
        {
            Name = "RoadGhost",
            Multimesh = new MultiMesh
            {
                TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
                UseColors = true,
                Mesh = new BoxMesh { Size = new Vector3(_catalog.CellSize * 0.9f, 0.08f, _catalog.CellSize * 0.9f),
                    Material = new StandardMaterial3D { VertexColorUseAsAlbedo = true, Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                        ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded } },
            },
        };
        AddChild(_roadGhost);
        BuildModeChanged?.Invoke();
    }

    /// <summary>Cells of an L-shaped road from a to b (horizontal first, then vertical).</summary>
    public static Cell[] RoadLine(Cell a, Cell b)
    {
        var cells = new List<Cell>();
        int sx = Math.Sign(b.X - a.X), sy = Math.Sign(b.Y - a.Y);
        for (int x = a.X; ; x += sx) { cells.Add(new Cell(x, a.Y)); if (x == b.X) break; }
        for (int y = a.Y + sy; sy != 0; y += sy) { cells.Add(new Cell(b.X, y)); if (y == b.Y) break; }
        return cells.ToArray();
    }

    private Cell? CellUnder(Vector2? screen)
    {
        if (_camera.GroundUnderMouse(screen) is not { } hit) return null;
        float cs = _catalog.CellSize;
        return new Cell(Mathf.FloorToInt(hit.X / cs), Mathf.FloorToInt(hit.Z / cs));
    }

    private void UpdateRoadGhost()
    {
        if (_roadGhost is null) return;
        var mm = _roadGhost.Multimesh;
        var end = CellUnder(null);
        if (end is null) { mm.InstanceCount = 0; return; }
        var cells = _roadStart is { } s ? RoadLine(s, end.Value) : new[] { end.Value };
        bool removing = Input.IsKeyPressed(Key.Ctrl);
        var map = _host.World.Map;
        mm.InstanceCount = cells.Length;
        for (int i = 0; i < cells.Length; i++)
        {
            var c = cells[i];
            bool free = map.InBounds(c) && map.BuildingAt(c) == 0;
            var color = removing ? new Color(1f, 0.5f, 0.3f, 0.6f)
                : !free ? new Color(1f, 0.3f, 0.3f, 0.5f)
                : map.IsRoad(c) ? new Color(1f, 1f, 1f, 0.25f) : new Color(0.95f, 0.85f, 0.5f, 0.7f);
            mm.SetInstanceTransform(i, new Transform3D(Basis.Identity, _view.CellCenter(c) + new Vector3(0, 0.06f, 0)));
            mm.SetInstanceColor(i, color);
        }
    }

    public void Begin(BuildingDef def)
    {
        if (RoadMode) Cancel();
        Selected = def;
        _rotation = 0;
        RebuildGhost();
        BuildModeChanged?.Invoke();
    }

    public void Cancel()
    {
        RoadMode = false;
        _roadStart = null;
        _roadGhost?.QueueFree();
        _roadGhost = null;
        Selected = null;
        _ghost?.QueueFree();
        _ghost = null;
        _ghostDefId = null;
        BuildModeChanged?.Invoke();
    }

    private void RebuildGhost()
    {
        _ghost?.QueueFree();
        if (Selected is null) return;
        var (w, h) = GridMap.Footprint(Selected, _rotation);
        _ghost = new Node3D { Name = "Ghost" };
        var visual = _catalog.CreateBuilding(Selected.Id, new Vector2(w, h) * _catalog.CellSize, ghost: true);
        if (_rotation % 2 == 1) visual.RotationDegrees = new Vector3(0, 90, 0);
        _ghost.AddChild(visual);
        _ghost.AddChild(_catalog.Primitive("box", new Vector3(w * _catalog.CellSize, 0.06f, h * _catalog.CellSize), Colors.White, ghost: true));
        _ghostDefId = Selected.Id;
        AddChild(_ghost);
    }

    public override void _Process(double delta)
    {
        if (RoadMode && !_host.IsBusy) UpdateRoadGhost();
        if (Selected is null || _ghost is null || _host.IsBusy) return;
        UpdateGhost(null);
    }

    /// <summary>Snaps the ghost to the grid under the given screen position (or the mouse) and validates it.</summary>
    private void UpdateGhost(Vector2? screenPosition)
    {
        if (Selected is null || _ghost is null) return;
        if (_camera.GroundUnderMouse(screenPosition) is not { } hit) return;

        var (w, h) = GridMap.Footprint(Selected, _rotation);
        float cs = _catalog.CellSize;
        // Centre the footprint on the mouse.
        _cell = new Cell(Mathf.FloorToInt(hit.X / cs - (w - 1) / 2f), Mathf.FloorToInt(hit.Z / cs - (h - 1) / 2f));
        _ghost.Position = _catalog.CellToWorld(_cell.X, _cell.Y) + new Vector3(w, 0, h) * (cs / 2);

        var world = _host.World;
        _valid = world.Map.CanPlace(Selected, _cell, _rotation);   // materials come later, carried to the site

        var tint = _valid ? new Color(0.5f, 1f, 0.5f) : new Color(1f, 0.4f, 0.4f);
        _ghost.GetChild<MeshInstance3D>(1).MaterialOverride = _catalog.Material(tint, ghost: true);
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (_host.IsBusy) return;
        if (RoadMode)
        {
            switch (e)
            {
                case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } down:
                    _roadStart = CellUnder(down.Position);
                    GetViewport().SetInputAsHandled();
                    break;
                case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false } up when _roadStart is { } start:
                    if (CellUnder(up.Position) is { } end)
                    {
                        var cells = RoadLine(start, end);
                        _host.Send(Input.IsKeyPressed(Key.Ctrl) ? new RemoveRoad(cells) : new PlaceRoad(cells));
                    }
                    _roadStart = null;   // stay in road mode for the next segment
                    GetViewport().SetInputAsHandled();
                    break;
                case InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true }:
                case InputEventKey { Pressed: true, Keycode: Key.Escape }:
                    Cancel();
                    GetViewport().SetInputAsHandled();
                    break;
            }
            return;
        }
        if (Selected is not null)
        {
            switch (e)
            {
                case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } click:
                    UpdateGhost(click.Position);   // use the click position, not last frame's mouse
                    if (_valid)
                    {
                        _host.Send(new PlaceBuilding(Selected.Id, _cell, _rotation));
                        if (!Input.IsKeyPressed(Key.Shift)) Cancel();   // Shift = keep placing
                    }
                    GetViewport().SetInputAsHandled();
                    break;
                case InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true }:
                case InputEventKey { Pressed: true, Keycode: Key.Escape }:
                    Cancel();
                    GetViewport().SetInputAsHandled();
                    break;
                case InputEventKey { Pressed: true, Echo: false, Keycode: Key.R }:
                    _rotation = (_rotation + 1) % 4;
                    RebuildGhost();
                    GetViewport().SetInputAsHandled();
                    break;
            }
            return;
        }

        if (e is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } mb && _camera.GroundUnderMouse(mb.Position) is { } hit)
        {
            float cs = _catalog.CellSize;
            int id = _host.World.Map.BuildingAt(new Cell(Mathf.FloorToInt(hit.X / cs), Mathf.FloorToInt(hit.Z / cs)));
            _view.SelectedBuildingId = id;
            BuildingSelected?.Invoke(id);
        }
    }
}
