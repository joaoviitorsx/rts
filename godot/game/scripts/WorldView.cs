using System.Collections.Generic;
using System.Linq;
using Godot;
using Ironvale.Sim;
using Ironvale.Sim.Buildings;
using Ironvale.Sim.Logistics;
using Cell = Ironvale.Sim.Map.Cell;
using Ironvale.Sim.Population;

namespace Ironvale.Game;

/// <summary>
/// Read-only mirror of the world: ground, building nodes (synced by id every frame), stock piles and one
/// cube per household (MultiMesh). Never mutates the simulation.
/// </summary>
public partial class WorldView : Node3D
{
    private SimHost _host = null!;
    private VisualCatalog _catalog = null!;
    private readonly Dictionary<int, BuildingNode> _buildings = new();
    private MultiMeshInstance3D _agents = null!;
    private MeshInstance3D _ground = null!;
    private Node3D _selection = null!;

    private sealed class BuildingNode
    {
        public required Node3D Root;
        public required Node3D Visual;
        public required MeshInstance3D Pile;
        public required Label3D Label;
        public bool WasActive;
    }

    public int SelectedBuildingId { get; set; }

    public void Init(SimHost host, VisualCatalog catalog)
    {
        _host = host;
        _catalog = catalog;
        _host.WorldReplaced += Rebuild;

        _agents = new MultiMeshInstance3D
        {
            Name = "Agents",
            Multimesh = new MultiMesh
            {
                TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
                UseColors = true,
                Mesh = new BoxMesh { Size = _catalog.Agent("carrier").size },
            },
            MaterialOverride = new StandardMaterial3D { VertexColorUseAsAlbedo = true, Roughness = 0.8f },
        };
        AddChild(_agents);

        _selection = _catalog.Primitive("box", new Vector3(1, 0.08f, 1), new Color(1, 0.9f, 0.3f, 0.6f), ghost: true);
        _selection.Visible = false;
        AddChild(_selection);
        Rebuild();
    }

    private void Rebuild()
    {
        foreach (var node in _buildings.Values) node.Root.QueueFree();
        _buildings.Clear();
        _ground?.QueueFree();
        _ground = CreateGround(_host.World.Map);
        AddChild(_ground);
        Sync();
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
        UpdateAgents(w);
    }

    private BuildingNode CreateBuildingNode(Building b)
    {
        var (wCells, hCells) = b.Size;
        var footprint = new Vector2(wCells, hCells) * _catalog.CellSize;
        var root = new Node3D { Name = $"Building_{b.Id}", Position = FootprintCenter(b) };
        var visual = _catalog.CreateBuilding(b.Def.Id, footprint);
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
        AddChild(root);
        return new BuildingNode { Root = root, Visual = visual, Pile = pile, Label = label };
    }

    private void UpdateBuildingNode(World w, Building b, BuildingNode node)
    {
        if (!b.IsActive)
        {
            float progress = b.Def.BuildDays == 0 ? 1 : (float)b.BuildProgressDays / b.Def.BuildDays;
            node.Visual.Scale = new Vector3(1, Mathf.Lerp(0.15f, 0.9f, progress), 1);
            node.Label.Text = $"🔨 {b.Def.Name} {b.BuildProgressDays}/{b.Def.BuildDays}d";
            node.Label.Visible = true;
        }
        else
        {
            if (!node.WasActive) node.Visual.Scale = Vector3.One;
            bool isProducer = b.IsProducer;
            node.Label.Visible = isProducer || b.IsStorage;
            node.Label.Text = isProducer
                ? $"{b.Def.Name} {b.AssignedCount}/{b.Def.JobSlots}"
                : b.IsStorage ? $"{b.Def.Name} {b.Stock.Total.WholeUnits}/{b.Stock.Capacity.WholeUnits}" : "";
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
        var mm = _agents.Multimesh;
        int count = w.Households.Count + w.Carriers.Count(c => c.Retiring);
        if (mm.InstanceCount != count) mm.InstanceCount = count;

        var carrierColor = _catalog.Agent("carrier").color;
        var workingColor = _catalog.Agent("working").color;
        var idleColor = _catalog.Agent("subsisting").color;
        float half = _catalog.Agent("carrier").size.Y / 2;
        float alpha = _host.TickAlpha;
        int ticksPerCell = w.Content.Balance.CarrierTicksPerCell;

        int i = 0;
        foreach (var h in w.Households)
        {
            Vector3 pos;
            Color color;
            var carrier = h.State == HouseholdState.Hauling ? w.CarrierOf(h.Id) : null;
            if (carrier is not null)
            {
                pos = CarrierPosition(carrier, alpha, ticksPerCell);
                color = carrierColor;
            }
            else if (h.State == HouseholdState.Working && w.GetBuilding(h.JobBuildingId) is { } job)
            {
                pos = AroundBuilding(job, h.Id);
                color = workingColor;
            }
            else
            {
                var home = w.GetBuilding(h.HomeId) ?? w.SeatBuilding;
                pos = home is null ? Vector3.Zero : AroundBuilding(home, h.Id);
                color = idleColor;
            }
            mm.SetInstanceTransform(i, new Transform3D(Basis.Identity, pos + new Vector3(0, half, 0)));
            mm.SetInstanceColor(i, color);
            i++;
        }
        foreach (var c in w.Carriers)
        {
            if (!c.Retiring) continue;
            mm.SetInstanceTransform(i, new Transform3D(Basis.Identity, CarrierPosition(c, alpha, ticksPerCell) + new Vector3(0, half, 0)));
            mm.SetInstanceColor(i, carrierColor.Darkened(0.3f));
            i++;
        }
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
