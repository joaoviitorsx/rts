using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Ironvale.Sim;
using Ironvale.Sim.Map;
using Noise = Ironvale.Sim.Map.Noise;

namespace Ironvale.Game.World3D;

/// <summary>
/// The look of a generated map (GDD v0.3 §12, briefing step 4a) with the approved look-dev pipeline:
/// <list type="bullet">
/// <item>Terrain3D ground (VillageTerrain + cozy_ground shader) whose heights come from the sim's terraces. On a cliff
/// edge the shared vertex takes the lower height, so the slope falls inside the upper cell; the shader turns steep
/// ground into rock.</item>
/// <item>Faceted limestone walls over those slopes (cliff.gdshader); their top line is the terrain's own top edge, so
/// the two meet without a gap. Irregularity is noise in the view only.</item>
/// <item>The ground mask (path/sand, dirt, fields, shade) painted from the sim and baked once, read by ground and grass.</item>
/// <item>Stylized water over sea and lakes (water.gdshader) with the bed carved below it.</item>
/// </list>
/// Reads the world only. Rebuilt when the world is replaced (load / new game).
/// </summary>
public partial class WorldLook : Node3D
{
    private const int MaskPixelsPerMeter = 2;
    /// <summary>Dev fallback (--generated-cliffs): the faceted walls of step 4a instead of the Kenney cliff modules.</summary>
    private static bool GeneratedCliffs => OS.GetCmdlineUserArgs().Contains("--generated-cliffs");
    private const float CliffMin = Ground.LevelHeight * 0.55f;

    private Node3D? _terrain;
    private RefCounted? _mask;
    private Node? _baker;
    private int _width, _height;   // metres
    private float[] _heights = Array.Empty<float>();   // per 1-m vertex, (w+1)·(h+1)

    public void Build(World w, float cellSize)
    {
        foreach (var child in GetChildren()) child.QueueFree();
        var t = w.Terrain!;
        _width = Mathf.RoundToInt(t.Width * cellSize);
        _height = Mathf.RoundToInt(t.Height * cellSize);
        ComputeHeights(t, cellSize);
        BuildTerrain();
        BuildMask(w, t, cellSize);
        AddChild(GeneratedCliffs ? BuildCliffs(t, cellSize) : BuildKenneyCliffs(t, cellSize));
        AddChild(BuildWater(t, cellSize));
        BuildGrass();
    }

    /// <summary>The cartoon grass carpet (same as the village) on the relief: streamed around the camera, no blades
    /// on paths, sand, fields, water or cliff faces.</summary>
    private void BuildGrass()
    {
        if (_mask is null || OS.GetCmdlineUserArgs().Contains("--no-grass")) return;
        var grass = (Node3D)GD.Load<GDScript>("res://game/vegetation/GrassCarpet.gd").New();
        grass.Name = "Grass";
        AddChild(grass);
        grass.Call("set_heights", _heights, _width, _height);
        grass.Call("stream", _mask, new Rect2(0, 0, _width, _height), this, 7);
    }

    /// <summary>Ground height at a world point (bilinear over the 1-m vertex grid) — what the eye sees.</summary>
    public float HeightAt(float x, float z)
    {
        if (_heights.Length == 0) return 0;
        x = Mathf.Clamp(x, 0, _width - 0.001f);
        z = Mathf.Clamp(z, 0, _height - 0.001f);
        int x0 = (int)x, z0 = (int)z;
        float fx = x - x0, fz = z - z0;
        float H(int vx, int vz) => _heights[vz * (_width + 1) + vx];
        return Mathf.Lerp(Mathf.Lerp(H(x0, z0), H(x0 + 1, z0), fx), Mathf.Lerp(H(x0, z0 + 1), H(x0 + 1, z0 + 1), fx), fz);
    }

    // ------------------------------------------------------------------------------------------------ heights

    /// <summary>Height of a cell's ground: the walkable surface, or the bed under water (deeper offshore).</summary>
    private static float CellGround(Terrain t, int x, int y)
    {
        x = Mathf.Clamp(x, 0, t.Width - 1);
        y = Mathf.Clamp(y, 0, t.Height - 1);
        var c = new Cell(x, y);
        float h = Ground.CellHeight(x, y);
        if (t.IsWater(c)) h -= 0.35f + Mathf.Min(t.DepthAt(c), 6) * 0.35f;
        return h;
    }

    private void ComputeHeights(Terrain t, float cs)
    {
        int vw = _width + 1, vh = _height + 1;
        _heights = new float[vw * vh];
        int per = Mathf.RoundToInt(cs);   // vertices per cell edge (cell 2 m → 2)
        for (int vz = 0; vz < vh; vz++)
        for (int vx = 0; vx < vw; vx++)
        {
            bool edgeX = vx % per == 0, edgeZ = vz % per == 0;
            int cx = vx / per, cz = vz / per;
            float h;
            if (!edgeX && !edgeZ) h = CellGround(t, cx, cz);
            else
            {
                // Shared by 2 or 4 cells: smooth where they are close (ramps, shores), lower one across a cliff.
                float min = float.MaxValue, max = float.MinValue, sum = 0;
                int n = 0;
                for (int dz = edgeZ ? -1 : 0; dz <= 0; dz++)
                for (int dx = edgeX ? -1 : 0; dx <= 0; dx++)
                {
                    float c = CellGround(t, cx + dx, cz + dz);
                    min = Mathf.Min(min, c);
                    max = Mathf.Max(max, c);
                    sum += c;
                    n++;
                }
                // Kenney cliff modules stand in the lower cell: the plateau reaches the boundary and the 1-m slope
                // falls into the lower cell, behind the module. The generated walls (fallback) want the opposite.
                h = max - min >= CliffMin ? (GeneratedCliffs ? min : max) : sum / n;
            }
            _heights[vz * vw + vx] = h;
        }
    }

    private void BuildTerrain()
    {
        var script = GD.Load<GDScript>("res://game/terrain/VillageTerrain.gd");
        _terrain = (Node3D)script.New();
        _terrain.Name = "Ground";
        _terrain.Set("cover", new Rect2(0, 0, _width + 1, _height + 1));
        _terrain.Set("palette", "koastalia");
        AddChild(_terrain);   // its _ready builds Terrain3D with the cozy shader
        var terrain = _terrain.Get("terrain").AsGodotObject();
        if (terrain is null) return;   // GDExtension missing: VillageTerrain fell back to a plane
        var img = Image.CreateEmpty(_width + 1, _height + 1, false, Image.Format.Rf);
        for (int z = 0; z <= _height; z++)
        for (int x = 0; x <= _width; x++)
            img.SetPixel(x, z, new Color(_heights[z * (_width + 1) + x], 0, 0));
        var data = terrain.Get("data").AsGodotObject();
        data.Call("import_images", new Godot.Collections.Array { img, new Variant(), new Variant() }, Vector3.Zero, 0.0f, 1.0f);
        _terrain.Call("commit");
    }

    // ------------------------------------------------------------------------------------------------ ground mask

    private void BuildMask(World w, Terrain t, float cs)
    {
        var script = GD.Load<GDScript>("res://game/terrain/GroundMask.gd");
        _mask = (RefCounted)script.New(new Rect2(0, 0, _width, _height), MaskPixelsPerMeter);
        int mw = _width * MaskPixelsPerMeter, mh = _height * MaskPixelsPerMeter;
        var plowed = new float[mw * mh];
        var path = new float[mw * mh];
        var dirt = new float[mw * mh];
        var shade = new float[mw * mh];
        float pxPerCell = cs * MaskPixelsPerMeter;
        for (int y = 0; y < mh; y++)
        for (int x = 0; x < mw; x++)
        {
            var c = new Cell((int)(x / pxPerCell), (int)(y / pxPerCell));
            int i = y * mw + x;
            if (t.IsWater(c)) dirt[i] = 1f;                                  // no grass under water
            else if (t.GroundOf(c) == Sim.Map.Ground.Sand) path[i] = 0.75f; // beach: light earth
            else if (t.IsRamp(c)) path[i] = 0.55f;                            // trodden ramps
            if (w.Map.IsRoad(c)) path[i] = 1f;
            if (t.ForestAt(c) > 0) shade[i] = Mathf.Min(1f, t.ForestAt(c) / 255f * 1.2f) * 0.7f;
        }
        foreach (var b in w.Buildings)
        {
            var (bw, bh) = b.Size;
            bool field = b.Def.Recipes.Count > 0 && b.Def.Recipes[0].Kind == Ironvale.Sim.Content.RecipeKind.Seasonal;
            int x0 = (int)(b.Origin.X * pxPerCell), y0 = (int)(b.Origin.Y * pxPerCell);
            for (int y = y0; y < Mathf.Min(mh, y0 + bh * pxPerCell); y++)
            for (int x = x0; x < Mathf.Min(mw, x0 + bw * pxPerCell); x++)
                (field ? plowed : dirt)[y * mw + x] = 1f;
        }
        var ch = (Godot.Collections.Array)_mask.Get("ch");
        ch[0] = plowed;
        ch[1] = path;
        ch[2] = dirt;
        ch[3] = shade;
        _mask.Call("blur", 1, 0.8);
        _mask.Call("blur", 2, 0.5);
        _mask.Call("blur", 3, 1.2, 1);
        _mask.Call("publish");
        _baker = (Node)GD.Load<GDScript>("res://game/terrain/GroundColorBaker.gd").New();
        AddChild(_baker);
        _baker.Call("bake", new Vector2I(mw, mh));
    }

    /// <summary>The grass carpet and decoration read the same mask (no blades on paths, sand, fields, water).</summary>
    public RefCounted? Mask => _mask;

    // ------------------------------------------------------------------------------------------------ cliffs

    /// <summary>
    /// One wall per cell edge where the ground drops ≥ <see cref="CliffMin"/>: from the upper terrain's top line
    /// (half a cell inside the upper cell, exactly the terrain's vertices) down to the lower ground, a little proud of
    /// the slope, in 3×2 facets whose inner vertices are pushed out by world-position noise (identical on shared edges).
    /// </summary>
    private MeshInstance3D BuildCliffs(Terrain t, float cs)
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        float half = cs / 2f;
        for (int y = 0; y < t.Height; y++)
        for (int x = 0; x < t.Width; x++)
        {
            float hu = CellGround(t, x, y);
            foreach (var (dx, dy) in Terrain.Dirs)
            {
                int nx = x + dx, ny = y + dy;
                if (nx < 0 || ny < 0 || nx >= t.Width || ny >= t.Height) continue;
                if (t.IsWater(new Cell(x, y))) continue;
                float hl = CellGround(t, nx, ny);
                if (hu - hl < CliffMin) continue;
                // Edge on the boundary (between this cell and the lower neighbour), oriented along the boundary.
                var n = new Vector3(dx, 0, dy);                    // outward (toward the lower cell)
                var along = new Vector3(-dy, 0, dx);
                var mid = new Vector3((x + 0.5f + dx * 0.5f) * cs, 0, (y + 0.5f + dy * 0.5f) * cs);
                var a = mid - along * half;
                var b = mid + along * half;
                Wall(st, a, b, n, hu, hl, half);
            }
        }
        st.GenerateNormals();
        return new MeshInstance3D
        {
            Name = "Cliffs",
            Mesh = st.Commit(),
            MaterialOverride = new ShaderMaterial { Shader = GD.Load<Shader>("res://game/world/cliff.gdshader") },
            CastShadow = GeometryInstance3D.ShadowCastingSetting.On,
        };
    }

    private static void Wall(SurfaceTool st, Vector3 a, Vector3 b, Vector3 outward, float top, float bottom, float inset)
    {
        const int cols = 3, rows = 3;
        var grid = new Vector3[cols + 1, rows + 1];
        for (int i = 0; i <= cols; i++)
        for (int j = 0; j <= rows; j++)
        {
            float u = (float)i / cols, v = (float)j / rows;   // v: 0 top … 1 bottom
            var p = a.Lerp(b, u);
            // Top row = the terrain's top line (inset into the upper cell by the slope's run); bottom = boundary, proud.
            var basePos = p - outward * (inset * (1 - v)) + outward * (0.12f * v);
            basePos.Y = Mathf.Lerp(top + 0.02f, bottom - 0.25f, v);
            if (j > 0 && j < rows || (j == rows))
            {
                float k = Noise.Roll(Mathf.RoundToInt(basePos.X * 3), Mathf.RoundToInt(basePos.Z * 3) + Mathf.RoundToInt(basePos.Y * 7), 0xC11Fu) / 65535f;
                basePos += outward * (k * 0.45f * (j == rows ? 0.5f : 1f));
                basePos.Y += (k - 0.5f) * 0.25f;
            }
            grid[i, j] = basePos;
        }
        st.SetColor(new Color(top / 20f, 0, 0));
        for (int i = 0; i < cols; i++)
        for (int j = 0; j < rows; j++)
        {
            var p00 = grid[i, j]; var p10 = grid[i + 1, j]; var p01 = grid[i, j + 1]; var p11 = grid[i + 1, j + 1];
            st.AddVertex(p00); st.AddVertex(p01); st.AddVertex(p10);
            st.AddVertex(p10); st.AddVertex(p01); st.AddVertex(p11);
        }
    }

    /// <summary>
    /// Terrace edges with the Kenney Nature Kit cliff modules (P38): per lower cell, a straight module on each side
    /// that faces a higher terrace (stacked per level, the ramp's own climbing side left open), and an outer-corner
    /// piece where only the diagonal is higher — the marching-squares cases of the cell grid. One MultiMesh per module.
    /// </summary>
    private Node3D BuildKenneyCliffs(Terrain t, float cs)
    {
        var straight = new List<Transform3D>();
        var corner = new List<Transform3D>();
        float[] dirAngle = { Mathf.Pi, Mathf.Pi / 2, 0f, -Mathf.Pi / 2 };   // N, E, S, W: rotate the kit's +Z wall to face the higher cell
        for (int y = 0; y < t.Height; y++)
        for (int x = 0; x < t.Width; x++)
        {
            var c = new Cell(x, y);
            int level = t.LevelAt(c);
            float baseY = level * Ground.LevelHeight;
            var center = new Vector3((x + 0.5f) * cs, 0, (y + 0.5f) * cs);
            bool[] up = new bool[4];
            for (int d = 0; d < 4; d++)
            {
                var (dx, dy) = Terrain.Dirs[d];
                var n = new Cell(x + dx, y + dy);
                if (!t.InBounds(n) || t.IsWater(n)) continue;
                int diff = t.LevelAt(n) - level;
                if (diff <= 0) continue;
                up[d] = true;
                if (t.RampDir(c) == d && diff == 1) continue;   // the ramp climbs here
                for (int k = 0; k < diff; k++)
                    straight.Add(new Transform3D(new Basis(Vector3.Up, dirAngle[d]), center + new Vector3(0, baseY + k * Ground.LevelHeight, 0)));
            }
            // Outer corners of the higher region: the diagonal is higher, both sides next to it are not.
            (int Dx, int Dy, int A, int B, float Angle)[] diagonals =
            {
                (-1, 1, 3, 2, 0f), (1, 1, 1, 2, Mathf.Pi / 2), (1, -1, 1, 0, Mathf.Pi), (-1, -1, 3, 0, -Mathf.Pi / 2),
            };
            foreach (var (ddx, ddy, a, b, angle) in diagonals)
            {
                var n = new Cell(x + ddx, y + ddy);
                if (!t.InBounds(n) || t.IsWater(n) || up[a] || up[b]) continue;
                int diff = t.LevelAt(n) - level;
                for (int k = 0; k < diff; k++)
                    corner.Add(new Transform3D(new Basis(Vector3.Up, angle), center + new Vector3(0, baseY + k * Ground.LevelHeight, 0)));
            }
        }
        var root = new Node3D { Name = "CliffModules" };
        var clusters = GD.Load<GDScript>("res://game/vegetation/Clusters.gd");
        var palette = GD.Load<GDScript>("res://game/visual/KenneyPalette.gd");
        foreach (var (scene, list) in new[] { ("res://assets/environment/kenney/K_cliff_rock.tscn", straight), ("res://assets/environment/kenney/K_cliff_corner_rock.tscn", corner) })
        {
            if (list.Count == 0) continue;
            var src = (Godot.Collections.Dictionary)clusters.Call("first_mesh", scene);
            if (src.Count == 0) continue;
            var mesh = (Mesh)((Mesh)src["mesh"]).Duplicate();
            for (int i = 0; i < mesh.GetSurfaceCount(); i++)
                if (palette.Call("material", mesh.SurfaceGetMaterial(i)?.ResourceName ?? "").AsGodotObject() is Material m) mesh.SurfaceSetMaterial(i, m);
            var local = (Transform3D)src["xform"];
            var mm = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform3D, Mesh = mesh, InstanceCount = list.Count };
            for (int i = 0; i < list.Count; i++) mm.SetInstanceTransform(i, list[i] * local);
            root.AddChild(new MultiMeshInstance3D { Multimesh = mm, CastShadow = GeometryInstance3D.ShadowCastingSetting.On });
        }
        return root;
    }

    // ------------------------------------------------------------------------------------------------ water

    private static MeshInstance3D BuildWater(Terrain t, float cs)
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        for (int y = 0; y < t.Height; y++)
        for (int x = 0; x < t.Width; x++)
        {
            var c = new Cell(x, y);
            bool water = t.IsWater(c);
            bool shore = !water && NextToWater(t, c);
            if (!water && !shore) continue;
            // Water cells, plus a one-cell skirt over the shore so the surface reaches under the beach.
            float h = water ? Ground.CellHeight(x, y) : WaterLevelNear(t, c);
            var o = new Vector3(x * cs, h, y * cs);
            st.AddVertex(o); st.AddVertex(o + new Vector3(cs, 0, 0)); st.AddVertex(o + new Vector3(0, 0, cs));
            st.AddVertex(o + new Vector3(cs, 0, 0)); st.AddVertex(o + new Vector3(cs, 0, cs)); st.AddVertex(o + new Vector3(0, 0, cs));
        }
        st.GenerateNormals();
        return new MeshInstance3D
        {
            Name = "Water",
            Mesh = st.Commit(),
            MaterialOverride = new ShaderMaterial { Shader = GD.Load<Shader>("res://game/world/water.gdshader") },
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
    }

    private static bool NextToWater(Terrain t, Cell c)
    {
        foreach (var (dx, dy) in Terrain.Dirs)
        {
            var n = new Cell(c.X + dx, c.Y + dy);
            if (t.InBounds(n) && t.IsWater(n)) return true;
        }
        return false;
    }

    private static float WaterLevelNear(Terrain t, Cell c)
    {
        foreach (var (dx, dy) in Terrain.Dirs)
        {
            var n = new Cell(c.X + dx, c.Y + dy);
            if (t.InBounds(n) && t.IsWater(n)) return Ground.CellHeight(n.X, n.Y);
        }
        return Ground.CellHeight(c.X, c.Y);
    }
}
