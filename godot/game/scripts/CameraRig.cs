using Godot;

namespace Ironvale.Game;

/// <summary>
/// Official RTS camera: perspective FOV 32°, pitch 50° (owner: 45–55°, 30–35°), smooth pan and zoom,
/// rotation in 90° steps. No pixel snapping (cozy 3D direction).
/// Controls: WASD/arrows/screen edge/middle-drag = pan · wheel = zoom · Q/E = rotate.
/// </summary>
public partial class CameraRig : Node3D
{
    [Export] public float Fov { get; set; } = 32f;
    [Export] public float PitchDegrees { get; set; } = 50f;
    [Export] public float MinDistance { get; set; } = 18f;
    [Export] public float MaxDistance { get; set; } = 140f;
    [Export] public float PanSpeed { get; set; } = 1.1f;      // fraction of distance per second
    [Export] public float EdgePanMargin { get; set; } = 8f;
    [Export] public bool EdgePan { get; set; } = true;
    [Export] public float Smoothing { get; set; } = 10f;

    private Camera3D _camera = null!;
    private Vector3 _targetPos;
    private float _distance = 60f;
    private float _targetDistance = 60f;
    private float _yaw;
    private float _targetYaw;
    private bool _dragging;
    private Rect2 _bounds = new(0, 0, 128, 128);

    public Camera3D Camera => _camera;

    public override void _Ready()
    {
        _camera = new Camera3D { Fov = Fov, Current = true, Far = 1000f };
        AddChild(_camera);
        _targetPos = Position;
        _yaw = _targetYaw = Mathf.DegToRad(45);
        ApplyTransform();
    }

    public void SetBounds(Rect2 boundsXZ) => _bounds = boundsXZ;

    /// <summary>GDScript-friendly variant (nullable args are not exposed to GDScript).</summary>
    public void FocusAt(Vector3 point, float distance) => FocusOn(point, distance);

    public void FocusOn(Vector3 point, float? distance = null)
    {
        _targetPos = point with { Y = 0 };
        Position = _targetPos;
        if (distance is { } d) _distance = _targetDistance = Mathf.Clamp(d, MinDistance, MaxDistance);
        ApplyTransform();
    }

    public override void _UnhandledInput(InputEvent e)
    {
        switch (e)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.WheelUp, Pressed: true }:
                _targetDistance = Mathf.Clamp(_targetDistance * 0.88f, MinDistance, MaxDistance);
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.WheelDown, Pressed: true }:
                _targetDistance = Mathf.Clamp(_targetDistance / 0.88f, MinDistance, MaxDistance);
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Middle } mb:
                _dragging = mb.Pressed;
                break;
            case InputEventMouseMotion motion when _dragging:
                // Drag the ground: move opposite to the mouse, scaled by zoom.
                float k = _distance * 0.0016f;
                _targetPos += PlanarRight() * (-motion.Relative.X * k) + PlanarForward() * (motion.Relative.Y * k);
                break;
            case InputEventKey { Pressed: true, Echo: false, Keycode: Key.Q }:
                _targetYaw -= Mathf.Pi / 2;
                break;
            case InputEventKey { Pressed: true, Echo: false, Keycode: Key.E }:
                _targetYaw += Mathf.Pi / 2;
                break;
        }
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        var move = Vector2.Zero;
        if (Input.IsKeyPressed(Key.W) || Input.IsKeyPressed(Key.Up)) move.Y += 1;
        if (Input.IsKeyPressed(Key.S) || Input.IsKeyPressed(Key.Down)) move.Y -= 1;
        if (Input.IsKeyPressed(Key.D) || Input.IsKeyPressed(Key.Right)) move.X += 1;
        if (Input.IsKeyPressed(Key.A) || Input.IsKeyPressed(Key.Left)) move.X -= 1;

        if (EdgePan && DisplayServer.WindowIsFocused())
        {
            var vp = GetViewport();
            var mouse = vp.GetMousePosition();
            var size = vp.GetVisibleRect().Size;
            if (mouse.X >= 0 && mouse.Y >= 0 && mouse.X <= size.X && mouse.Y <= size.Y)
            {
                if (mouse.X < EdgePanMargin) move.X -= 1;
                if (mouse.X > size.X - EdgePanMargin) move.X += 1;
                if (mouse.Y < EdgePanMargin) move.Y += 1;
                if (mouse.Y > size.Y - EdgePanMargin) move.Y -= 1;
            }
        }

        if (move != Vector2.Zero)
        {
            move = move.Normalized() * PanSpeed * _distance * dt;
            _targetPos += PlanarRight() * move.X + PlanarForward() * move.Y;
        }

        _targetPos.X = Mathf.Clamp(_targetPos.X, _bounds.Position.X, _bounds.End.X);
        _targetPos.Z = Mathf.Clamp(_targetPos.Z, _bounds.Position.Y, _bounds.End.Y);

        float t = 1f - Mathf.Exp(-Smoothing * dt);
        Position = Position.Lerp(_targetPos, t);
        _distance = Mathf.Lerp(_distance, _targetDistance, t);
        _yaw = Mathf.LerpAngle(_yaw, _targetYaw, t);
        ApplyTransform();
    }

    private Vector3 PlanarForward() => new(-Mathf.Sin(_yaw), 0, -Mathf.Cos(_yaw));
    private Vector3 PlanarRight() => new(Mathf.Cos(_yaw), 0, -Mathf.Sin(_yaw));

    private void ApplyTransform()
    {
        if (_camera is null) return;
        float pitch = Mathf.DegToRad(PitchDegrees);
        var back = -PlanarForward();
        var offset = back * (Mathf.Cos(pitch) * _distance) + Vector3.Up * (Mathf.Sin(pitch) * _distance);
        _camera.GlobalPosition = GlobalPosition + offset;
        _camera.LookAt(GlobalPosition, Vector3.Up);
    }

    /// <summary>Mouse ray hit on the ground plane (Y = 0).</summary>
    public Vector3? GroundUnderMouse(Vector2? screenPosition = null)
    {
        var mouse = screenPosition ?? GetViewport().GetMousePosition();
        var from = _camera.ProjectRayOrigin(mouse);
        var dir = _camera.ProjectRayNormal(mouse);
        if (Mathf.Abs(dir.Y) < 1e-4f) return null;
        float t = -from.Y / dir.Y;
        return t < 0 ? null : from + dir * t;
    }
}
