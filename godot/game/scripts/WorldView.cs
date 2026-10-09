using System.Collections.Generic;
using System.Linq;
using Godot;
using Ironvale.Game.Visual;
using Ironvale.Sim;
using Ironvale.Sim.Buildings;
using Ironvale.Sim.Logistics;
using Cell = Ironvale.Sim.Map.Cell;
using Ironvale.Sim.Population;
using Ironvale.Sim.Time;

namespace Ironvale.Game;

/// <summary>
/// Read-only mirror of the world: ground, building scenes (synced by id every frame), construction sites,
/// stock piles and one animated villager per household. Never mutates the simulation.
/// </summary>
public partial class WorldView : Node3D
{
    private SimHost _host = null!;
    private VisualCatalog _catalog = null!;
    private readonly Dictionary<int, BuildingNode> _buildings = new();
    private Node3D _agents = null!;
    private readonly Dictionary<int, Agent> _agentNodes = new();
    private MeshInstance3D _ground = null!;
    private Node3D _selection = null!;
    private MultiMeshInstance3D _roads = null!;
    private int _roadVersion = -1;
    private readonly Dictionary<(Cell From, Cell To, int Version), (List<Cell> Cells, int[] Cost)> _routes = new();
    private static readonly Color RoadColor = new(0.66f, 0.55f, 0.38f);

    private sealed class BuildingNode
    {
        public required Node3D Root;
        public required Node3D Visual;
        public required MeshInstance3D Pile;
        public required Label3D Label;
        public Node3D? Site;
        public CpuParticles3D? SiteDust;
        public bool WasActive;
    }

    private sealed class Agent
    {
        public required Node3D Root;
        public required ModularCharacter Character;
        public required VillagerLook Look;
        public Vector3 LastPosition;
        public bool Seen;
    }

    /// <summary>Most emotion balloons on screen at once (guide: don't clutter); happy ones rotate, one at a time.</summary>
    private const int MaxEmotes = 4;
    private double _emoteTimer;
    /// <summary>Dev proportion study (--head-scale=N); 1 = the model as authored.</summary>
    public static float HeadScale { get; set; } = 1f;
    private readonly Dictionary<int, OmniLight3D> _windowLights = new();
    private static readonly Color WindowColor = new(1f, 0.72f, 0.42f);

    public int SelectedBuildingId { get; set; }

    public void Init(SimHost host, VisualCatalog catalog)
    {
        _host = host;
        _catalog = catalog;
        _host.WorldReplaced += Rebuild;

        _agents = new Node3D { Name = "Agents" };
        AddChild(_agents);

        _roads = new MultiMeshInstance3D
        {
            Name = "Roads",
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            Multimesh = new MultiMesh
            {
                TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
                Mesh = new BoxMesh { Size = new Vector3(_catalog.CellSize, 0.04f, _catalog.CellSize), Material = _catalog.Material(RoadColor) },
            },
        };
        AddChild(_roads);

        _selection = _catalog.Primitive("box", new Vector3(1, 0.08f, 1), new Color(1, 0.9f, 0.3f, 0.6f), ghost: true);
        _selection.Visible = false;
        AddChild(_selection);
        Rebuild();
    }

    private bool _rebuilding;

    private void Rebuild()
    {
        _rebuilding = true;
        foreach (var node in _buildings.Values) node.Root.QueueFree();
        _buildings.Clear();
        foreach (var agent in _agentNodes.Values) agent.Root.QueueFree();
        _agentNodes.Clear();
        _ground?.QueueFree();
        _ground = CreateGround(_host.World.Map);
        AddChild(_ground);
        _roadVersion = -1;
        Sync();
        _rebuilding = false;
    }

    public override void _Process(double delta)
    {
        if (_host is null || _host.IsBusy) return;
        Sync();
    }

    private void Sync()
    {
        var w = _host.World;
        var alive = new HashSet<int>();
        foreach (var b in w.Buildings)
        {
            alive.Add(b.Id);
            if (!_buildings.TryGetValue(b.Id, out var node))
            {
                node = CreateBuildingNode(b);
                _buildings[b.Id] = node;
            }
            UpdateBuildingNode(w, b, node);
        }
        foreach (var id in _buildings.Keys.Where(id => !alive.Contains(id)).ToList())
        {
            _buildings[id].Root.QueueFree();
            _buildings.Remove(id);
        }
        UpdateSelection(w);
        UpdateRoads(w);
        UpdateAgents(w);
    }

    /// <summary>Road cells as one MultiMesh, rebuilt only when the map changes.</summary>
    private void UpdateRoads(World w)
    {
        if (_roadVersion == w.Map.Version) return;
        _roadVersion = w.Map.Version;
        _routes.Clear();
        var cells = w.Map.Roads.ToList();
        var mm = _roads.Multimesh;
        mm.InstanceCount = cells.Count;
        for (int i = 0; i < cells.Count; i++)
            mm.SetInstanceTransform(i, new Transform3D(Basis.Identity, CellCenter(cells[i]) + new Vector3(0, 0.02f, 0)));
    }

    /// <summary>
    /// Where a worker is during its commute (null when on site or at home): it walks the sim's route home → work in
    /// the morning window and back in the evening one. The day is 4 s at 1x, so commuters move fast (2B adds routines).
    /// </summary>
    private (Vector3 Pos, Vector3 Dir)? CommutePosition(World w, Household h, Building work, float alpha)
    {
        int c = w.CommuteMilliTicks(h);
        if (c <= 0) return null;
        float day = SimTime.TicksPerDay * 1000f;
        float t = (w.Tick % SimTime.TicksPerDay + alpha) * 1000f;
        var home = w.HomeCellOf(h);
        float f;
        Cell from, to;
        if (t < c) { f = t / c; from = home; to = work.Center; }
        else if (t >= day - c) { f = (t - (day - c)) / c; from = work.Center; to = home; }
        else return null;

        var key = (from, to, w.Map.Version);
        if (!_routes.TryGetValue(key, out var route))
        {
            var cells = w.Paths.Route(from, to);
            var cost = new int[cells.Count];
            int sum = 0, target = w.Map.BuildingAt(to);
            for (int i = 0; i < cells.Count; i++) cost[i] = sum += w.Paths.EnterCost(cells[i], target);
            route = (cells, cost);
            _routes[key] = route;
        }
        if (route.Cells.Count == 0) return null;
        float goal = Mathf.Clamp(f, 0, 1) * route.Cost[^1];
        int k = 0;
        while (k < route.Cost.Length - 1 && route.Cost[k] < goal) k++;
        float prevCost = k == 0 ? 0 : route.Cost[k - 1];
        var a = k == 0 ? CellCenter(from) : CellCenter(route.Cells[k - 1]);
        var b = CellCenter(route.Cells[k]);
        float s = route.Cost[k] > prevCost ? (goal - prevCost) / (route.Cost[k] - prevCost) : 1;
        return (a.Lerp(b, Mathf.Clamp(s, 0, 1)), b - a);
    }

    private BuildingNode CreateBuildingNode(Building b)
    {
        var (wCells, hCells) = b.Size;
        var footprint = new Vector2(wCells, hCells) * _catalog.CellSize;
        var root = new Node3D { Name = $"Building_{b.Id}", Position = FootprintCenter(b) };
        var visual = _catalog.CreateBuilding(b.Def.Id, footprint, variantSeed: b.Id);
        if (b.Rotation % 2 == 1) visual.RotationDegrees = new Vector3(0, 90, 0);
        root.AddChild(visual);

        var pile = _catalog.Primitive("box", new Vector3(0.9f, 1, 0.9f), Colors.White);
        pile.Position = new Vector3(footprint.X / 2 - 0.6f, 0, footprint.Y / 2 + 0.6f);
        root.AddChild(pile);

        var label = new Label3D
        {
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            FontSize = 40,
            OutlineSize = 12,
            PixelSize = 0.02f,
            Position = new Vector3(0, 7f, 0),
            NoDepthTest = true,
            Modulate = new Color(1, 1, 1, 0.95f),
        };
        root.AddChild(label);
        Node3D? site = null;
        if (!b.IsActive && _catalog.CreateConstructionSite(footprint) is { } s)
        {
            site = s;
            root.AddChild(site);
        }
        AddChild(root);
        if (!_rebuilding && !b.IsActive)
        {
            // Placed just now (not loaded): hammer + dust + pop (UI_UX_guide §5.3).
            Visual.Fx.Pop(root);
            Visual.Fx.Dust(root, footprint, continuous: false);
            Audio.Sfx.PlayAt("build_place", root.GlobalPosition);
        }
        return new BuildingNode { Root = root, Visual = visual, Pile = pile, Label = label, Site = site, WasActive = b.IsActive };
    }

    private void UpdateBuildingNode(World w, Building b, BuildingNode node)
    {
        if (!b.IsActive)
        {
            float progress = b.Def.BuildDays == 0 ? 1 : (float)b.BuildProgressDays / b.Def.BuildDays;
            node.Visual.Scale = new Vector3(1, Mathf.Lerp(0.15f, 0.9f, progress), 1);
            node.Label.Text = $"🔨 {Ironvale.Game.UI.UiText.Bld(b.Def)} {b.BuildProgressDays}/{b.Def.BuildDays}d";
            node.Label.Visible = true;
            // Light dust while someone is building.
            bool working = w.BuildersAt(b) > 0 && b.CanProgress;
            if (working && node.SiteDust is null)
            {
                var (wc, hc) = b.Size;
                node.SiteDust = Visual.Fx.Dust(node.Root, new Vector2(wc, hc) * _catalog.CellSize, continuous: true);
            }
            if (node.SiteDust is not null) node.SiteDust.Emitting = working;
        }
        else
        {
            if (!node.WasActive)
            {
                node.Visual.Scale = Vector3.One;
                node.Site?.QueueFree();
                node.Site = null;
                node.SiteDust?.QueueFree();
                node.SiteDust = null;
                // Completed just now: pop + puff + sound.
                var (wc, hc) = b.Size;
                Visual.Fx.Pop(node.Root);
                Visual.Fx.Dust(node.Root, new Vector2(wc, hc) * _catalog.CellSize, continuous: false);
                Audio.Sfx.PlayAt("site_complete", node.Root.GlobalPosition);
            }
            bool isProducer = b.IsProducer;
            node.Label.Visible = isProducer || b.IsStorage;
            node.Label.Text = isProducer
                ? $"{Ironvale.Game.UI.UiText.Bld(b.Def)} {b.AssignedCount}/{b.Def.JobSlots}"
                : b.IsStorage ? $"{Ironvale.Game.UI.UiText.Bld(b.Def)} {b.Stock.Total.WholeUnits}/{b.Stock.Capacity.WholeUnits}" : "";
        }
        node.WasActive = b.IsActive;

        // Stock pile: height = fill ratio, colour = most abundant resource (economia visível).
        var cap = b.Stock.Capacity;
        if (cap.IsPositive && b.Stock.Total.IsPositive)
        {
            int top = 0;
            for (int r = 1; r < w.Content.ResourceCount; r++)
                if (b.Stock.Get(r) > b.Stock.Get(top)) top = r;
            float fill = Mathf.Clamp((float)(b.Stock.Total.Milli / (double)cap.Milli), 0.03f, 1f);
            node.Pile.Visible = true;
            float height = fill * 2.5f;
            node.Pile.Scale = new Vector3(1, height, 1);
            node.Pile.Position = node.Pile.Position with { Y = height / 2 };   // keep the base on the ground
            node.Pile.MaterialOverride = _catalog.Material(_catalog.ResourceColor(w.Content.Resources[top].Id));
        }
        else
        {
            node.Pile.Visible = false;
        }
    }

    private void UpdateSelection(World w)
    {
        var b = w.GetBuilding(SelectedBuildingId);
        _selection.Visible = b is not null;
        if (b is null) return;
        var (wc, hc) = b.Size;
        _selection.Position = FootprintCenter(b) + new Vector3(0, 0.02f, 0);
        _selection.Scale = new Vector3(wc * _catalog.CellSize + 0.4f, 1, hc * _catalog.CellSize + 0.4f);
    }

    private void UpdateAgents(World w)
    {
        foreach (var a in _agentNodes.Values) a.Seen = false;
        float alpha = _host.TickAlpha;
        float dt = (float)GetProcessDeltaTime();
        var season = w.Calendar.Season;

        foreach (var h in w.Households)
        {
            var carrier = h.State == HouseholdState.Hauling ? w.CarrierOf(h.Id) : null;
            if (carrier is not null)
            {
                UpdateCarrierAgent(w, h.Id, carrier, alpha, dt);
                continue;
            }
            var agent = GetAgent(h.Id);
            var work = w.WorkplaceOf(h);
            if (work is not null && CommutePosition(w, h, work, alpha) is { } walk)
            {
                Place(agent, walk.Pos, walk.Dir);
                agent.Character.Play("run");
            }
            else if (work is not null)
            {
                var pos = AroundBuilding(work, h.Id);
                Place(agent, pos, FootprintCenter(work) - pos);
                agent.Character.Play(work.IsActive ? WorkAnimation(work, season) : "interact");
            }
            else
            {
                var home = w.GetBuilding(h.HomeId) ?? w.SeatBuilding;
                var pos = home is null ? Vector3.Zero : AroundBuilding(home, h.Id);
                Place(agent, pos, null);
                agent.Character.Play(agent.Look.IdleClip(Time.GetTicksMsec() / 1000.0));
            }
            agent.Look.Carry("");
        }
        UpdateEmotes(w);
        UpdateWindowLights(w, alpha);
        double now = Time.GetTicksMsec() / 1000.0;
        foreach (var a in _agentNodes.Values) a.Look.Animate(now);
        foreach (var c in w.Carriers)
            if (c.Retiring) UpdateCarrierAgent(w, -c.Id, c, alpha, dt);

        foreach (var id in _agentNodes.Where(kv => !kv.Value.Seen).Select(kv => kv.Key).ToList())
        {
            _agentNodes[id].Root.QueueFree();
            _agentNodes.Remove(id);
        }
    }

    /// <summary>
    /// Emotion balloons, 2×/s: negative feelings first (hunger > cold > tired), then at most one happy villager that
    /// rotates every few seconds; never more than <see cref="MaxEmotes"/> on screen.
    /// </summary>
    private void UpdateEmotes(World w)
    {
        _emoteTimer -= GetProcessDeltaTime();
        if (_emoteTimer > 0) return;
        _emoteTimer = 0.5;
        var wanted = new List<(int Key, VillagerLook.Emote E)>();
        foreach (var h in w.Households)
        {
            var e = VillagerLook.Feeling(w, h);
            if (e != VillagerLook.Emote.None && _agentNodes.ContainsKey(h.Id)) wanted.Add((h.Id, e));
        }
        var shown = wanted.Where(x => x.E != VillagerLook.Emote.Happy).OrderBy(x => x.E).ThenBy(x => x.Key).Take(MaxEmotes).ToList();
        var happy = wanted.Where(x => x.E == VillagerLook.Emote.Happy).ToList();
        if (shown.Count < MaxEmotes && happy.Count > 0)
            shown.Add(happy[(int)(Time.GetTicksMsec() / 5000) % happy.Count]);
        var keys = shown.ToDictionary(x => x.Key, x => x.E);
        foreach (var (key, agent) in _agentNodes)
            agent.Look.ShowEmote(keys.GetValueOrDefault(key, VillagerLook.Emote.None));
    }

    /// <summary>Houses with families light a warm lantern at dusk and fade it out in the morning (fades, never blinks).</summary>
    private void UpdateWindowLights(World w, float alpha)
    {
        float t = (w.Tick % SimTime.TicksPerDay + alpha) / SimTime.TicksPerDay;
        float dusk = Mathf.Max(Mathf.SmoothStep(0.72f, 0.95f, t), 1f - Mathf.SmoothStep(0f, 0.12f, t));
        foreach (var b in w.Buildings)
        {
            if (!b.IsActive || !b.Def.Has(Ironvale.Sim.Content.BuildingRole.Housing) || !_buildings.TryGetValue(b.Id, out var node)) continue;
            bool lived = w.Households.Any(h => h.HomeId == b.Id);
            if (!_windowLights.TryGetValue(b.Id, out var light))
            {
                // A porch lantern by the door side (+Z, like DoorOf): a warm pool on the ground, visible from the camera.
                var (_, hc) = b.Size;
                light = new OmniLight3D { LightColor = WindowColor, OmniRange = 4.5f, ShadowEnabled = false,
                    Position = new Vector3(0, 1.9f, hc * _catalog.CellSize * 0.5f + 0.7f) };
                node.Root.AddChild(light);
                _windowLights[b.Id] = light;
            }
            float target = lived ? dusk * 3.0f : 0f;
            light.LightEnergy = Mathf.Lerp(light.LightEnergy, target, 0.15f);
            light.Visible = light.LightEnergy > 0.02f;
        }
        foreach (var id in _windowLights.Keys.Where(id => w.GetBuilding(id) is null).ToList())
        {
            if (IsInstanceValid(_windowLights[id])) _windowLights[id].QueueFree();
            _windowLights.Remove(id);
        }
    }

    private void UpdateCarrierAgent(World w, int key, Carrier c, float alpha, float dt)
    {
        var agent = GetAgent(key);
        agent.Look.Carry(c.ShipmentId != 0 && c.Resource >= 0 ? w.Content.Resources[c.Resource].Id : "");
        var pos = CarrierPosition(c, alpha, w.StepTicksInto(c.NextCell, c.Target));
        float speed = dt > 0 ? agent.LastPosition.DistanceTo(pos) / dt : 0;
        Place(agent, pos, pos - agent.LastPosition);
        bool loaded = c.ShipmentId != 0;
        string anim = c.IsMoving
            ? (loaded ? "walk_carry" : speed > 3f ? "run" : "walk")
            : c.Phase is CarrierPhase.Loading or CarrierPhase.Unloading ? "pickup" : loaded ? "idle_carry" : "idle";
        // Walk ≈ 1 m/s, run ≈ 5 m/s in the clips: scale so feet roughly match the (time-compressed) sim speed.
        float rate = anim switch { "walk" or "walk_carry" => speed / 1.0f, "run" => speed / 5.4f, _ => 1f };
        agent.Character.Play(anim, Mathf.Clamp(rate, 0.6f, 3f));
    }

    private static string WorkAnimation(Building job, Season season) => job.Def.Id switch
    {
        "woodcutter" => "chop",
        "field" => season == Season.Spring ? "plant_seed" : "watering",
        _ => "interact",
    };

    private Agent GetAgent(int key)
    {
        if (_agentNodes.TryGetValue(key, out var agent))
        {
            agent.Seen = true;
            return agent;
        }
        var scenes = _catalog.CharacterScenes;
        var root = GD.Load<PackedScene>(scenes[Mathf.PosMod(key, scenes.Count)]).Instantiate<Node3D>();
        _agents.AddChild(root);
        var character = (ModularCharacter)root;
        character.ApplyHairColor(ModularCharacter.HairPalette[Mathf.PosMod(key * 7, ModularCharacter.HairPalette.Length)]);
        if (!Mathf.IsEqualApprox(HeadScale, 1f) && character.Skeleton is { } sk)
            sk.AddChild(new HeadScaleModifier { HeadScale = HeadScale });
        agent = new Agent { Root = root, Character = character, Seen = true, Look = new VillagerLook(root, key) };
        _agentNodes[key] = agent;
        return agent;
    }

    private static void Place(Agent agent, Vector3 position, Vector3? facing)
    {
        if (agent.LastPosition == Vector3.Zero) agent.LastPosition = position;
        agent.Root.Position = position;
        if (facing is { } f && f.LengthSquared() > 1e-6f)
        {
            float target = Mathf.Atan2(f.X, f.Z);   // models face +Z
            agent.Root.Rotation = new Vector3(0, Mathf.LerpAngle(agent.Root.Rotation.Y, target, 0.25f), 0);
        }
        agent.LastPosition = position;
    }

    private Vector3 CarrierPosition(Carrier c, float alpha, int ticksPerCell)
    {
        var from = CellCenter(c.Pos);
        if (!c.IsMoving)
        {
            // Standing still (idle/loading/unloading) happens at a building centre: show it at the door instead.
            var at = _host.World.Map.BuildingAt(c.Pos);
            return _host.World.GetBuilding(at) is { } b ? DoorOf(b, c.Id) : from;
        }
        float t = Mathf.Clamp((c.StepTicks + alpha) / ticksPerCell, 0, 1);
        return from.Lerp(CellCenter(c.NextCell), t);
    }

    public Vector3 CellCenter(Cell c) =>
        _catalog.CellToWorld(c.X, c.Y) + new Vector3(_catalog.CellSize / 2, 0, _catalog.CellSize / 2);

    public Vector3 FootprintCenter(Building b)
    {
        var (wc, hc) = b.Size;
        return _catalog.CellToWorld(b.Origin.X, b.Origin.Y) + new Vector3(wc, 0, hc) * (_catalog.CellSize / 2);
    }

    /// <summary>A spot just outside the building footprint, spread by id.</summary>
    private Vector3 AroundBuilding(Building b, int id)
    {
        var (wc, hc) = b.Size;
        float radius = Mathf.Max(wc, hc) * _catalog.CellSize * 0.5f + 0.9f;
        return FootprintCenter(b) + Offset(id, radius);
    }

    private Vector3 DoorOf(Building b, int id)
    {
        var (_, hc) = b.Size;
        float spread = (id % 3 - 1) * 0.8f;
        return FootprintCenter(b) + new Vector3(spread, 0, hc * _catalog.CellSize * 0.5f + 0.7f);
    }

    private static Vector3 Offset(int id, float radius)
    {
        float angle = id * 2.399963f;   // golden angle: spreads households around a point
        return new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
    }

    private MeshInstance3D CreateGround(Ironvale.Sim.Map.GridMap map)
    {
        float cs = _catalog.CellSize;
        var size = new Vector2(map.Width * cs, map.Height * cs);
        var shader = new Shader
        {
            Code = """
                shader_type spatial;
                uniform vec3 grass : source_color = vec3(0.56, 0.72, 0.42);
                uniform vec3 line_color : source_color = vec3(0.47, 0.62, 0.36);
                uniform float cell = 2.0;
                varying vec3 world_pos;
                void vertex() { world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz; }
                void fragment() {
                    vec2 g = abs(fract(world_pos.xz / cell - 0.5) - 0.5) / fwidth(world_pos.xz / cell);
                    float line = 1.0 - min(min(g.x, g.y), 1.0);
                    ALBEDO = mix(grass, line_color, line * 0.5);
                    ROUGHNESS = 1.0;
                }
                """,
        };
        return new MeshInstance3D
        {
            Name = "Ground",
            Mesh = new PlaneMesh { Size = size },
            Position = new Vector3(size.X / 2, 0, size.Y / 2),
            MaterialOverride = new ShaderMaterial { Shader = shader },
        };
    }
}
