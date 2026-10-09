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

        if (OS.GetCmdlineUserArgs().FirstOrDefault(x => x.StartsWith("--locale=")) is { } locale)
            TranslationServer.SetLocale(locale[9..]);   // dev: check translations (e.g. --locale=en)
        AddChild(new Audio.Sfx { Name = "Sfx" });
        UiJuice.Attach(GetTree());
        var ui = GD.Load<PackedScene>("res://ui/screens/hud.tscn").Instantiate<Hud>();
        AddChild(ui);
        ui.Init(host, build, view, camera);

        ApplyCommandLine(host);
        PerfProbe.AttachIfRequested(this);
        if (OS.GetCmdlineUserArgs().Contains("--debug")) ui.ToggleDebug();
        foreach (var arg in OS.GetCmdlineUserArgs())
        {
            if (arg.StartsWith("--window=") && arg[9..].Split('x') is [var ws, var hs] && int.TryParse(ws, out int ww) && int.TryParse(hs, out int wh))
                DisplayServer.WindowSetSize(new Vector2I(ww, wh));   // dev: check the HUD at other window sizes
            if (arg.StartsWith("--panel=")) ui.OpenPanel(arg[8..]);
            if (arg.StartsWith("--shot=")) AddChild(new DevShot { Name = "DevShot", Path = arg[7..] });
        }
        if (OS.GetCmdlineUserArgs().Contains("--smoke"))
        {
            var smoke = new SmokeTest { Name = "SmokeTest" };
            AddChild(smoke);
            smoke.Init(host, build, camera, view);
        }
    }

    /// <summary>
    /// Dev/smoke-test switches (after "--"): --opening (scripted MVP opening, with roads unless --no-roads), --player=passive|naive|optimal, --speed=N, --days=N (pre-simulate), --debug (open debug panel), --panel=families|policies|building:ID,
    /// --shot=PATH (save the real window image after ~1 s and quit), --smoke (input end-to-end check).
    /// Example: godot-mono --path godot -- --opening --days=60 --speed=8
    /// </summary>
    private static void ApplyCommandLine(SimHost host)
    {
        var args = OS.GetCmdlineUserArgs();
        IScriptedPlayer? player = args.Contains("--opening") ? new OptimalPlayer(roads: !args.Contains("--no-roads")) : null;
        if (args.FirstOrDefault(x => x.StartsWith("--player=")) is { } p) player = ScriptedPlayers.Create(p[9..], roads: !args.Contains("--no-roads"));
        player?.Start(host.World);
        foreach (var arg in args)
        {
            if (arg.StartsWith("--speed=") && int.TryParse(arg[8..], out int speed)) host.SetSpeed(speed);
            if (arg.StartsWith("--days=") && int.TryParse(arg[7..], out int days))
                for (int d = 0; d < days; d++)
                {
                    host.World.StepDays(1);
                    player?.Daily(host.World);   // the scripted player keeps playing during the pre-simulation
                }
        }
    }
}
