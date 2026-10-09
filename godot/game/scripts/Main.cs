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
        // Dev: --scenario=wild_start (RTS opening on a generated map, GDD v0.3); the flat 2A map stays the default for now.
        if (OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--scenario=")) is { } scenarioArg)
            host.ScenarioId = scenarioArg[11..];
        if (OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--seed=")) is { } seedArg && ulong.TryParse(seedArg[7..], out ulong seedValue))
            host.Seed = seedValue;   // dev: another generated map
        AddChild(host);   // _Ready loads content and creates the world
        Ground.Terrain = host.World.Terrain;

        var catalog = new VisualCatalog();
        Ground.CellSize = catalog.CellSize;

        Node3D? sky = null;
        if (host.World.Terrain is not null)
        {
            // Generated maps: Sky3D day/night + clouds + rain driven by the sim (world look 4c); cozy grade inside.
            sky = (Node3D)GD.Load<GDScript>("res://game/world/WorldSky.gd").New();
            sky.Name = "WorldSky";
            AddChild(sky);
        }
        else AddChild(new WorldEnvironment
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
        if (host.World.Terrain is null) AddChild(new DirectionalLight3D
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
        else if (host.World.Terrain is { } terrain) camera.FocusOn(view.CellCenter(terrain.Start), 55);   // the band's clearing

        if (sky is not null) AddChild(new SkyDriver { Name = "SkyDriver", Host = host, Sky = sky, Camera = camera });

        var build = new BuildController { Name = "BuildController" };
        AddChild(build);
        build.Init(host, catalog, camera, view);
        // After the build tool: unhandled input reaches later siblings first, so unit clicks win over building selection.
        var units = new UnitController { Name = "UnitController" };
        AddChild(units);
        units.Init(host, camera, view, build);

        if (OS.GetCmdlineUserArgs().FirstOrDefault(x => x.StartsWith("--locale=")) is { } locale)
            TranslationServer.SetLocale(locale[9..]);   // dev: check translations (e.g. --locale=en)
        AddChild(new Audio.Sfx { Name = "Sfx" });
        UiJuice.Attach(GetTree());
        var ui = GD.Load<PackedScene>("res://ui/screens/hud.tscn").Instantiate<Hud>();
        AddChild(ui);
        ui.Units = units;
        ui.Init(host, build, view, camera);

        ApplyCommandLine(host);
        PerfProbe.AttachIfRequested(this);
        if (OS.GetCmdlineUserArgs().Contains("--debug")) ui.ToggleDebug();
        foreach (var arg in OS.GetCmdlineUserArgs())
        {
            if (arg.StartsWith("--window=") && arg[9..].Split('x') is [var ws, var hs] && int.TryParse(ws, out int ww) && int.TryParse(hs, out int wh))
                DisplayServer.WindowSetSize(new Vector2I(ww, wh));   // dev: check the HUD at other window sizes
            if (arg.StartsWith("--panel=")) ui.OpenPanel(arg[8..]);
            if (arg.StartsWith("--zoom=") && float.TryParse(arg[7..], System.Globalization.CultureInfo.InvariantCulture, out float zoom))
                camera.CallDeferred(CameraRig.MethodName.FocusAt, camera.Position, zoom);   // dev: close-up captures
            if (arg.StartsWith("--away=") && int.TryParse(arg[7..], out int awayYears))
                host.FastForwardWithChronicle(awayYears);   // dev: "while you were away" capture
            if (arg.StartsWith("--head-scale=") && float.TryParse(arg[13..], System.Globalization.CultureInfo.InvariantCulture, out float headScale))
                WorldView.HeadScale = headScale;   // dev: proportion study (not the default)
            if (arg.StartsWith("--shot=")) AddChild(new DevShot { Name = "DevShot", Path = arg[7..] });
            if (arg == "--no-hud") ui.Visible = false;   // dev: clean world captures
            if (arg.StartsWith("--view=") && float.TryParse(arg[7..], System.Globalization.CultureInfo.InvariantCulture, out float viewDist)
                && host.World.Terrain is { } tv)   // dev: the start seen from this distance (Koastalia framing ≈ 75)
                camera.FocusOn(view.CellCenter(tv.Start), viewDist);
            if (arg == "--select-all")   // dev: selection rings and the selection panel in captures
                units.SetSelection(host.World.Units.Where(u => u.Controllable).Select(u => u.Id), add: false);
            if (arg.StartsWith("--focus=") && arg[8..].Split(',') is [var fx, var fy] && int.TryParse(fx, out int cx) && int.TryParse(fy, out int cy))
                camera.FocusOn(view.CellCenter(new Ironvale.Sim.Map.Cell(cx, cy)), 26);   // dev: look at a cell
            if (arg.StartsWith("--focus-cliff") && host.World.Terrain is { } tc)   // dev: nearest terrace edge (…-south: facing the camera)
            {
                bool south = arg == "--focus-cliff-south";
                var near = Enumerable.Range(0, tc.Width * tc.Height).Select(i => new Ironvale.Sim.Map.Cell(i % tc.Width, i / tc.Width))
                    .Where(c => !tc.IsWater(c) && Ironvale.Sim.Map.Terrain.Dirs.Where(d => !south || d == (0, 1)).Any(d => tc.InBounds(new Ironvale.Sim.Map.Cell(c.X + d.Dx, c.Y + d.Dy))
                        && tc.LevelAt(new Ironvale.Sim.Map.Cell(c.X + d.Dx, c.Y + d.Dy)) < tc.LevelAt(c) && !tc.IsRamp(new Ironvale.Sim.Map.Cell(c.X + d.Dx, c.Y + d.Dy))))
                    .OrderBy(c => c.Manhattan(tc.Start)).First();
                camera.FocusOn(view.CellCenter(near), 42);
            }
            if (arg == "--focus-units" && host.World.Units.Count > 0)   // dev: look at the band wherever it went
                camera.FocusOn(view.CellCenter(host.World.Units[0].Pos), 30);
            if (arg.StartsWith("--focus-fauna=") && host.World.Animals.FirstOrDefault(an => an.Kind.ToString() == arg[14..]) is { } animal)
                camera.FocusOn(view.CellCenter(animal.Pos), 18);   // dev: look at the first deer / wolf / rabbit
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
    /// <summary>
    /// Dev (--rts-demo, with --scenario=wild_start): the band busy for captures and checks — three chop, two hunt the
    /// nearest deer, two gather stone, the ox heads for the trees; one stays by the pile.
    /// </summary>
    private static void RtsDemo(Ironvale.Sim.World w)
    {
        var colonists = w.Units.Where(u => u.IsColonist).Select(u => u.Id).ToArray();
        if (w.Terrain is not { } t || colonists.Length < 7) return;
        var any = w.Units[0];
        if (w.NearestNode(Ironvale.Sim.Map.NodeKind.Tree, t.Start, 30, any) is { } tree)
        {
            w.Enqueue(new Ironvale.Sim.Commands.OrderUnits(colonists[..3], Ironvale.Sim.Population.OrderKind.Gather, tree));
            var ox = w.Units.First(u => u.Kind == Ironvale.Sim.Population.UnitKind.Ox);
            w.Enqueue(new Ironvale.Sim.Commands.OrderUnits(new[] { ox.Id }, Ironvale.Sim.Population.OrderKind.Move, tree));
        }
        if (w.Animals.Where(a => a.Huntable).OrderBy(a => a.Pos.Manhattan(t.Start)).FirstOrDefault() is { } deer)
            w.Enqueue(new Ironvale.Sim.Commands.OrderUnits(colonists[3..5], Ironvale.Sim.Population.OrderKind.Hunt, deer.Pos, deer.Id));
        if (w.NearestNode(Ironvale.Sim.Map.NodeKind.Stone, t.Start, 30, any) is { } stone)
            w.Enqueue(new Ironvale.Sim.Commands.OrderUnits(colonists[5..7], Ironvale.Sim.Population.OrderKind.Gather, stone));
    }

    /// <summary>
    /// Dev (--camp-demo, with --scenario=wild_camp_dev): campfire, covered depot and tent placed around the start and
    /// built by the band (step 3 captures). Combine with --days=N to see them finished.
    /// </summary>
    private static void CampDemo(Ironvale.Sim.World w)
    {
        var band = w.Units.Where(u => u.IsColonist).Select(u => u.Id).ToArray();
        if (w.Terrain is not { } t || band.Length < 8) return;
        var spots = new[] { ("campfire", 3, 3, band[..2]), ("depot", -5, 0, band[2..5]), ("tent", 1, 6, band[5..8]) };
        foreach (var (id, dx, dy, who) in spots)
        {
            var def = w.Content.Building(id);
            var near = new Ironvale.Sim.Map.Cell(t.Start.X + dx, t.Start.Y + dy);
            var spot = Enumerable.Range(0, 21 * 21).Select(i => new Ironvale.Sim.Map.Cell(near.X + i % 21 - 10, near.Y + i / 21 - 10))
                .Where(c => w.CanPlace(def, c, 0)).OrderBy(c => c.Manhattan(near)).FirstOrDefault();
            w.Enqueue(new Ironvale.Sim.Commands.PlaceBuilding(id, spot, 0));
            w.ApplyPendingCommands();
            var site = w.Buildings.Last(b => b.Def.Id == id);
            w.Enqueue(new Ironvale.Sim.Commands.OrderUnits(who, Ironvale.Sim.Population.OrderKind.Build, site.Center, site.Id));
        }
    }

    private static void ApplyCommandLine(SimHost host)
    {
        var args = OS.GetCmdlineUserArgs();
        IScriptedPlayer? player = args.Contains("--opening") ? new OptimalPlayer(roads: !args.Contains("--no-roads")) : null;
        if (args.FirstOrDefault(x => x.StartsWith("--player=")) is { } p) player = ScriptedPlayers.Create(p[9..], roads: !args.Contains("--no-roads"));
        player?.Start(host.World);
        if (args.Contains("--rts-demo")) RtsDemo(host.World);
        if (args.Contains("--camp-demo")) CampDemo(host.World);
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
