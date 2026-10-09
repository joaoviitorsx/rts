using System.Linq;
using Godot;
using Ironvale.Game.UI;
using Ironvale.Sim.Commands;

namespace Ironvale.Game;

/// <summary>
/// Dev-only end-to-end check (run with "-- --smoke"): drives the real input path — build mode, mouse click on
/// the ground, building selection, assignment through a command — then prints SMOKE OK/FAIL and quits.
/// </summary>
public partial class SmokeTest : Node
{
    private SimHost _host = null!;
    private BuildController _build = null!;
    private CameraRig _camera = null!;
    private WorldView _view = null!;
    private int _frame;
    private int _buildingsBefore;
    private string _failure = "";

    public void Init(SimHost host, BuildController build, CameraRig camera, WorldView view)
    {
        _host = host;
        _build = build;
        _camera = camera;
        _view = view;
    }

    public override void _Process(double delta)
    {
        _frame++;
        var w = _host.World;
        switch (_frame)
        {
            case 10:
                // Build a house where the screen centre hits the ground (left of the hall: free cells).
                _camera.FocusOn(_view.CellCenter(new Ironvale.Sim.Map.Cell(20, 20)), 40);
                break;
            case 20:
                _buildingsBefore = w.Buildings.Count;
                _build.Begin(w.Content.Building("house"));
                Click(GetViewport().GetVisibleRect().Size / 2);
                break;
            case 30:
                if (w.Buildings.Count != _buildingsBefore + 1) Fail("click in build mode did not place a building");
                _host.Send(new AssignHousehold(w.Households[5].Id, w.SeatBuilding!.Id));   // carrier for the materials
                w.StepDays(10);   // finish construction
                break;
            case 40:
            {
                var hall = w.SeatBuilding!;
                _camera.FocusOn(_view.FootprintCenter(hall), 40);
                break;
            }
            case 50:
                Click(_camera.Camera.UnprojectPosition(_view.FootprintCenter(w.SeatBuilding!)));
                break;
            case 55:
                if (_view.SelectedBuildingId != w.SeatBuilding!.Id) Fail($"click did not select the hall (got {_view.SelectedBuildingId})");
                _host.Send(new AssignHousehold(w.Households[0].Id, w.SeatBuilding!.Id));
                break;
            case 80:   // > 1 sim tick at 1x so the command is applied
                if (w.CarrierOf(w.Households[0].Id) is null) Fail("assignment did not create a carrier");
                if (!w.Buildings.Last().IsActive) Fail("house was not completed");
                GD.Print(_failure.Length == 0 ? "SMOKE OK" : $"SMOKE FAIL: {_failure}");
                GetTree().Quit(_failure.Length == 0 ? 0 : 1);
                break;
        }
    }

    private void Fail(string reason) => _failure += (_failure.Length > 0 ? "; " : "") + reason;

    private void Click(Vector2 viewportPosition)
    {
        // Input events are in window coordinates; the root viewport may be stretched (canvas_items).
        var position = GetViewport().GetScreenTransform() * viewportPosition;
        Input.WarpMouse(position);
        var press = new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, Position = position, GlobalPosition = position };
        var release = new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = false, Position = position, GlobalPosition = position };
        Input.ParseInputEvent(press);
        Input.ParseInputEvent(release);
    }
}
