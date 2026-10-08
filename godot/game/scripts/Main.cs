using System.Linq;
using Godot;
using Ironvale.Game.UI;
using Ironvale.Sim.Scripting;

namespace Ironvale.Game;

/// <summary>Composition root of the Marco 1 view: wires sim host, world view, camera, build tool and UI.</summary>
public partial class Main : Node3D
{
    public override void _Ready()
    {
        var host = new SimHost { Name = "SimHost" };
        AddChild(host);   // _Ready loads content and creates the world

        var catalog = new VisualCatalog();

        AddChild(new WorldEnvironment
        {
            Environment = new Environment
            {
                BackgroundMode = Environment.BGMode.Sky,
                Sky = new Sky
                {
                    SkyMaterial = new ProceduralSkyMaterial
                    {
                        SkyTopColor = new Color(0.45f, 0.65f, 0.9f),
                        SkyHorizonColor = new Color(0.85f, 0.9f, 0.95f),
                        GroundHorizonColor = new Color(0.75f, 0.8f, 0.7f),
                    },
                },
                AmbientLightSource = Environment.AmbientSource.Sky,
                AmbientLightEnergy = 0.9f,
                TonemapMode = Environment.ToneMapper.Filmic,
                SsaoEnabled = true,
            },
        });
        AddChild(new DirectionalLight3D
        {
            RotationDegrees = new Vector3(-55, -35, 0),
            LightEnergy = 1.15f,
            LightColor = new Color(1f, 0.96f, 0.88f),
            ShadowEnabled = true,
        });

        var view = new WorldView { Name = "WorldView" };
        AddChild(view);
        view.Init(host, catalog);

        var camera = new CameraRig { Name = "CameraRig" };
        AddChild(camera);
        var map = host.World.Map;
        camera.SetBounds(new Rect2(0, 0, map.Width * catalog.CellSize, map.Height * catalog.CellSize));
        if (host.World.SeatBuilding is { } seat) camera.FocusOn(view.FootprintCenter(seat), 40);

        var build = new BuildController { Name = "BuildController" };
        AddChild(build);
        build.Init(host, catalog, camera, view);

        var ui = new GameUI { Name = "UI" };
        AddChild(ui);
        ui.Init(host, build, view);

        ApplyCommandLine(host);
        PerfProbe.AttachIfRequested(this);
        if (OS.GetCmdlineUserArgs().Contains("--debug")) ui.ToggleDebug();
        if (OS.GetCmdlineUserArgs().Contains("--smoke"))
        {
            var smoke = new SmokeTest { Name = "SmokeTest" };
            AddChild(smoke);
            smoke.Init(host, build, camera, view);
        }
    }

    /// <summary>
    /// Dev/smoke-test switches (after "--"): --opening (scripted MVP opening), --speed=N, --days=N (pre-simulate), --debug (open debug panel), --smoke (input end-to-end check).
    /// Example: godot-mono --path godot -- --opening --days=60 --speed=8
    /// </summary>
    private static void ApplyCommandLine(SimHost host)
    {
        var args = OS.GetCmdlineUserArgs();
        if (args.Contains("--opening")) MvpOpening.Apply(host.World);
        foreach (var arg in args)
        {
            if (arg.StartsWith("--speed=") && int.TryParse(arg[8..], out int speed)) host.SetSpeed(speed);
            if (arg.StartsWith("--days=") && int.TryParse(arg[7..], out int days)) host.World.StepDays(days);
        }
    }
}
