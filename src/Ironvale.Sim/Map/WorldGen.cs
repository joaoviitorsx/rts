namespace Ironvale.Sim.Map;

public sealed class GeneratedWorld
{
    public required Terrain Terrain { get; init; }
    public required Nature Nature { get; init; }
}

/// <summary>
/// Procedural map (GDD v0.3 §8): terraces, coast/lakes, ramps, fertility, forests, bushes, mushrooms, loose stones,
/// deposits, fauna spawns and a guaranteed good start. Integer-only and seeded: the same (seed, size, start tick,
/// balance) always gives the same map. A seed that cannot satisfy the start guarantees retries with the next
/// attempt (deterministically).
/// </summary>
public static class WorldGen
{
    public const int MaxAttempts = 16;

    public static GeneratedWorld Generate(ulong seed, int width, int height, long startTick, BalanceDef bal)
    {
        if (width < 96 || height < 96) throw new ArgumentException("generated maps need at least 96×96 cells");
        var failures = new List<string>();
        for (int attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var gen = new Generator(seed, attempt, width, height, startTick, bal);
            if (gen.Run())
            {
                gen.Result().Terrain.Info = gen.Result().Terrain.Info with { Retries = string.Join(", ", failures) };
                return gen.Result();
            }
            failures.Add(gen.Failure);
        }
        throw new InvalidOperationException($"seed {seed}: no playable map in {MaxAttempts} attempts ({string.Join(", ", failures)})");
    }

    /// <summary>FNV-1a over every generated layer (tests and the CLI compare maps with it).</summary>
    public static ulong Fingerprint(GeneratedWorld g)
    {
        var t = g.Terrain;
        ulong h = Fnv64.Hash(t.Level);
        h = Fnv64.Hash(MemoryMarshalBytes(t.GroundAt), h);
        h = Fnv64.Hash(t.Depth, h);
        h = Fnv64.Hash(MemoryMarshalBytes(t.Ramp), h);
        h = Fnv64.Hash(t.Fertility, h);
        h = Fnv64.Hash(t.Forest, h);
        var buf = new byte[18];
        for (int i = 0; i < t.Width * t.Height; i++)
        {
            var n = g.Nature.At(i);
            buf[0] = (byte)n.Kind;
            buf[1] = n.Variant;
            BitConverter.TryWriteBytes(buf.AsSpan(2, 8), n.Tick);
            BitConverter.TryWriteBytes(buf.AsSpan(10, 4), n.Amount);
            BitConverter.TryWriteBytes(buf.AsSpan(14, 4), i);
            h = Fnv64.Hash(buf, h);
        }
        foreach (var d in g.Nature.Deposits)
            h = Fnv64.Hash(System.Text.Encoding.ASCII.GetBytes($"{d.Kind}{d.Origin}{d.Units}{d.Rich}"), h);
        foreach (var f in g.Nature.Fauna)
            h = Fnv64.Hash(System.Text.Encoding.ASCII.GetBytes($"{f.Kind}{f.Cell}{f.Count}"), h);
        return Fnv64.Hash(System.Text.Encoding.ASCII.GetBytes($"{t.Start}{t.Levels}"), h);
    }

    private static ReadOnlySpan<byte> MemoryMarshalBytes<T>(T[] array) where T : unmanaged =>
        System.Runtime.InteropServices.MemoryMarshal.AsBytes(array.AsSpan());

    // ================================================================================================ generator

    private sealed class Generator
    {
        // Tunables of generator v1 (changing any of them = bump Terrain.GeneratorVersion).
        private const int SeaLevel = 15000;          // elevation below which coast cells flood
        private const int CoastWidth = 30;           // cells over which a coast side slopes into the sea
        private const int RampSpacing = 22;          // min Chebyshev distance between generated ramps
        private const int MinRegion = 60;            // terrace patches smaller than this merge into neighbours
        private const int ClearingHalf = 7;          // start clearing: (2·7+1)² flat cells
        private const int EdgeMargin = 28;           // start keeps away from the map edge

        private readonly int _w, _h, _n, _attempt;
        private readonly long _startTick;
        private readonly BalanceDef _bal;
        private readonly uint _s;
        private readonly Pcg32 _rng;
        private readonly Terrain _t;
        private readonly Nature _nat;
        private readonly int[] _elev;
        private readonly bool[] _ocean;
        private readonly List<Cell> _ramps = new();
        private int _coast, _lakes, _forestPermille;
        private DepositKind _rich;
        private bool _pond;
        private int _reachPermille;

        public Generator(ulong seed, int attempt, int w, int h, long startTick, BalanceDef bal)
        {
            _w = w;
            _h = h;
            _n = w * h;
            _attempt = attempt;
            _startTick = startTick;
            _bal = bal;
            ulong x = seed ^ Fnv64.Hash($"worldgen/{attempt}");
            _s = (uint)SplitMix64.Next(ref x);
            _rng = new Pcg32(SplitMix64.Next(ref x), SplitMix64.Next(ref x));
            _t = new Terrain(w, h);
            _nat = new Nature(w, h);
            _elev = new int[_n];
            _ocean = new bool[_n];
        }

        private GeneratedWorld? _result;
        public GeneratedWorld Result() => _result ??= new() { Terrain = _t, Nature = _nat };
        public string Failure { get; private set; } = "";

        private Cell C(int i) => new(i % _w, i / _w);
        private int I(int x, int y) => y * _w + x;
        private bool In(int x, int y) => x >= 0 && y >= 0 && x < _w && y < _h;
        private bool Land(int i) => _t.GroundAt[i] != Ground.Water;

        public bool Run()
        {
            ChooseParameters();
            Elevation();
            Ocean();
            Terraces();
            Lakes();
            WaterLayers();
            Ramps();
            Fertility();
            Vegetation();
            Outcrops();
            if (!ChooseStart()) return Fail("no start");
            ClearStart();
            if (!Connect()) return Fail($"reach {_reachPermille}");
            if (!Guarantee()) return Fail("guarantees");
            _t.Info = new WorldGenInfo
            {
                CoastSides = _coast, Lakes = _lakes, Levels = _t.Levels, ForestPermille = _forestPermille,
                RichDeposit = _rich, Attempt = _attempt, ReachablePermille = _reachPermille, StartPond = _pond,
            };
            return true;
        }

        private bool Fail(string why)
        {
            Failure = why;
            return false;
        }

        // -------------------------------------------------------------------------------------- parameters

        private void ChooseParameters()
        {
            int mode = _rng.NextInt(100);
            if (mode < 40) _coast = 1 << _rng.NextInt(4);
            else if (mode < 65)
            {
                int s = _rng.NextInt(4);
                _coast = (1 << s) | (1 << ((s + 1) % 4));
            }
            else _coast = 0;
            _lakes = _coast == 0 ? _rng.Range(1, 2) : _rng.Range(0, 1);
            _t.Levels = _rng.Range(3, 4);
            _forestPermille = _rng.Range(350, 550);
            _rich = (DepositKind)_rng.Range(1, 3);
        }

        // -------------------------------------------------------------------------------------- relief

        private void Elevation()
        {
            for (int y = 0; y < _h; y++)
            for (int x = 0; x < _w; x++)
            {
                // Domain warp (±10 cells) bends the contour lines so terraces curve like the reference.
                long wx = Noise.Fbm(x, y, 48, 2, _s + 11) - Noise.One / 2;
                long wy = Noise.Fbm(x, y, 48, 2, _s + 12) - Noise.One / 2;
                long fx = ((long)x << Noise.Shift) + wx * 20, fy = ((long)y << Noise.Shift) + wy * 20;
                long e = Noise.Fbm(fx, fy, 96, 4, _s + 1);
                for (int side = 0; side < 4; side++)
                {
                    if ((_coast & (1 << side)) == 0) continue;
                    int d = side switch { 0 => y, 1 => _w - 1 - x, 2 => _h - 1 - y, _ => x };
                    long jag = (Noise.Fbm(x, y, 24, 2, _s + 20 + (uint)side) - Noise.One / 2) * 8;  // ±4 cells
                    long df = ((long)d << Noise.Shift) + jag;
                    long cw = (long)CoastWidth << Noise.Shift;
                    if (df >= cw) continue;
                    long f = Math.Clamp((cw - df) * Noise.One / cw, 0, Noise.One);
                    e = e * (Noise.One - (f * f >> Noise.Shift)) >> Noise.Shift;
                }
                _elev[I(x, y)] = (int)e;
            }
        }

        /// <summary>Sea = low cells connected to a coast edge.</summary>
        private void Ocean()
        {
            var queue = new Queue<int>();
            for (int side = 0; side < 4; side++)
            {
                if ((_coast & (1 << side)) == 0) continue;
                int len = side % 2 == 0 ? _w : _h;
                for (int k = 0; k < len; k++)
                {
                    var (x, y) = side switch { 0 => (k, 0), 1 => (_w - 1, k), 2 => (k, _h - 1), _ => (0, k) };
                    int i = I(x, y);
                    if (_elev[i] < SeaLevel && !_ocean[i])
                    {
                        _ocean[i] = true;
                        queue.Enqueue(i);
                    }
                }
            }
            while (queue.TryDequeue(out int i))
            {
                var c = C(i);
                foreach (var (dx, dy) in Terrain.Dirs)
                {
                    int x = c.X + dx, y = c.Y + dy;
                    if (!In(x, y)) continue;
                    int j = I(x, y);
                    if (_ocean[j] || _elev[j] >= SeaLevel) continue;
                    _ocean[j] = true;
                    queue.Enqueue(j);
                }
            }
            for (int i = 0; i < _n; i++)
                if (_ocean[i]) _t.GroundAt[i] = Ground.Water;
        }

        /// <summary>Quantise land elevation into levels by quantiles, then smooth into long, calm cliff lines.</summary>
        private void Terraces()
        {
            var land = new List<int>();
            for (int i = 0; i < _n; i++)
                if (!_ocean[i]) land.Add(_elev[i]);
            land.Sort();
            int[] quantiles = _t.Levels == 3 ? new[] { 420, 780 } : new[] { 320, 600, 840 };
            var thresholds = quantiles.Select(q => land[Math.Min(land.Count - 1, land.Count * q / 1000)]).ToArray();
            for (int i = 0; i < _n; i++)
            {
                if (_ocean[i]) continue;
                int l = 0;
                foreach (int th in thresholds)
                    if (_elev[i] >= th) l++;
                _t.Level[i] = (byte)l;
            }

            var next = new byte[_n];
            var counts = new int[_t.Levels];
            for (int pass = 0; pass < 3; pass++)
            {
                Array.Copy(_t.Level, next, _n);
                for (int y = 0; y < _h; y++)
                for (int x = 0; x < _w; x++)
                {
                    int i = I(x, y);
                    if (_ocean[i]) continue;
                    Array.Clear(counts);
                    for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if ((dx | dy) == 0 || !In(x + dx, y + dy)) continue;
                        int j = I(x + dx, y + dy);
                        if (!_ocean[j]) counts[_t.Level[j]]++;
                    }
                    if (counts[_t.Level[i]] >= 3) continue;
                    int best = _t.Level[i];
                    for (int l = 0; l < counts.Length; l++)
                        if (counts[l] > counts[best]) best = l;
                    next[i] = (byte)best;
                }
                Array.Copy(next, _t.Level, _n);
            }
            for (int pass = 0; pass < 2; pass++) MergeSmallRegions();
        }

        private void MergeSmallRegions()
        {
            var comp = new int[_n];
            Array.Fill(comp, -1);
            var cells = new List<int>();
            var queue = new Queue<int>();
            var counts = new int[_t.Levels];
            int id = 0;
            for (int start = 0; start < _n; start++)
            {
                if (_ocean[start] || comp[start] >= 0) continue;
                cells.Clear();
                comp[start] = id;
                queue.Enqueue(start);
                Array.Clear(counts);
                while (queue.TryDequeue(out int i))
                {
                    cells.Add(i);
                    var c = C(i);
                    foreach (var (dx, dy) in Terrain.Dirs)
                    {
                        int x = c.X + dx, y = c.Y + dy;
                        if (!In(x, y)) continue;
                        int j = I(x, y);
                        if (_ocean[j]) continue;
                        if (_t.Level[j] != _t.Level[start]) counts[_t.Level[j]]++;
                        else if (comp[j] < 0)
                        {
                            comp[j] = id;
                            queue.Enqueue(j);
                        }
                    }
                }
                id++;
                if (cells.Count >= MinRegion) continue;
                int best = -1;
                for (int l = 0; l < counts.Length; l++)
                    if (counts[l] > 0 && (best < 0 || counts[l] > counts[best])) best = l;
                if (best < 0) continue;
                foreach (int i in cells) _t.Level[i] = (byte)best;
            }
        }

        // -------------------------------------------------------------------------------------- water

        private void Lakes()
        {
            var centers = new List<Cell>();
            for (int lake = 0; lake < _lakes; lake++)
            {
                for (int tries = 0; tries < 300; tries++)
                {
                    int r = _rng.Range(5, 10);
                    int cx = _rng.Range(20 + r, _w - 21 - r), cy = _rng.Range(20 + r, _h - 21 - r);
                    if (centers.Any(o => Math.Max(Math.Abs(o.X - cx), Math.Abs(o.Y - cy)) < 36)) continue;
                    int ex = _rng.Range(650, 1350), ey = 2000 - ex;   // stretch ‰ (x·y ≈ constant area)
                    int reach = r * Math.Max(ex, ey) / 1000 * 3 / 2 + 3;
                    if (!UniformDisk(cx, cy, reach, _t.Level[I(cx, cy)])) continue;
                    if (NearOcean(cx, cy, reach + 10)) continue;
                    CarveWater(cx, cy, r, ex, ey, reach, _s + 30 + (uint)lake);
                    centers.Add(new Cell(cx, cy));
                    break;
                }
            }
            _lakes = centers.Count;
        }

        private bool UniformDisk(int cx, int cy, int r, int level)
        {
            for (int y = cy - r; y <= cy + r; y++)
            for (int x = cx - r; x <= cx + r; x++)
            {
                if ((x - cx) * (x - cx) + (y - cy) * (y - cy) > r * r) continue;
                if (!In(x, y)) return false;
                int i = I(x, y);
                if (!Land(i) || _t.Level[i] != level || _t.Ramp[i] >= 0) return false;
            }
            return true;
        }

        private bool NearOcean(int cx, int cy, int r)
        {
            for (int y = Math.Max(0, cy - r); y <= Math.Min(_h - 1, cy + r); y++)
            for (int x = Math.Max(0, cx - r); x <= Math.Min(_w - 1, cx + r); x++)
                if (_ocean[I(x, y)]) return true;
            return false;
        }

        /// <summary>
        /// Organic lake: an ellipse (stretch <paramref name="ex"/>/<paramref name="ey"/> ‰) whose radius wobbles 45–155 %
        /// with noise, kept inside <paramref name="reach"/>; only the part connected to the centre is kept.
        /// </summary>
        private void CarveWater(int cx, int cy, int r, int ex, int ey, int reach, uint seed)
        {
            var lake = new bool[_n];
            for (int y = cy - reach; y <= cy + reach; y++)
            for (int x = cx - reach; x <= cx + reach; x++)
            {
                if (!In(x, y)) continue;
                long dx = (long)(x - cx) * 1000 / ex, dy = (long)(y - cy) * 1000 / ey;
                long d2 = dx * dx + dy * dy;
                long factor = 450 + Noise.Fbm(x, y, 10, 3, seed) * 1100L / Noise.One;   // ‰ of r
                if (d2 * 1_000_000 <= (long)r * r * factor * factor && (x - cx) * (x - cx) + (y - cy) * (y - cy) < reach * reach)
                    lake[I(x, y)] = true;
            }
            lake[I(cx, cy)] = true;
            var dist = Bfs(new[] { I(cx, cy) }, (_, j) => lake[j]);
            for (int i = 0; i < _n; i++)
                if (dist[i] != int.MaxValue) _t.GroundAt[i] = Ground.Water;
        }

        private int[] Bfs(IEnumerable<int> sources, Func<int, int, bool> canEnter)
        {
            var dist = new int[_n];
            Array.Fill(dist, int.MaxValue);
            var queue = new Queue<int>();
            foreach (int s in sources)
            {
                dist[s] = 0;
                queue.Enqueue(s);
            }
            while (queue.TryDequeue(out int i))
            {
                var c = C(i);
                foreach (var (dx, dy) in Terrain.Dirs)
                {
                    int x = c.X + dx, y = c.Y + dy;
                    if (!In(x, y)) continue;
                    int j = I(x, y);
                    if (dist[j] != int.MaxValue || !canEnter(i, j)) continue;
                    dist[j] = dist[i] + 1;
                    queue.Enqueue(j);
                }
            }
            return dist;
        }

        /// <summary>Depth of water (cells from shore) and the sand band: 2–3 cells on sea beaches, 1 around lakes.</summary>
        private void WaterLayers()
        {
            var shore = Enumerable.Range(0, _n).Where(Land);
            var depth = Bfs(shore, (_, j) => !Land(j));
            for (int i = 0; i < _n; i++)
                if (!Land(i)) _t.Depth[i] = (byte)Math.Min(255, depth[i]);

            var fromSea = Bfs(Enumerable.Range(0, _n).Where(i => _ocean[i]), (_, j) => Land(j));
            var fromLake = Bfs(Enumerable.Range(0, _n).Where(i => !Land(i) && !_ocean[i]), (_, j) => Land(j));
            for (int i = 0; i < _n; i++)
            {
                if (!Land(i)) continue;
                var c = C(i);
                int band = Math.Clamp(3 + (Noise.Fbm(c.X, c.Y, 14, 2, _s + 35) - Noise.One * 4 / 10) * 5 / Noise.One, 2, 5);   // 2–5 cells
                if ((fromSea[i] <= band && _t.Level[i] == 0) || fromLake[i] <= 1) _t.GroundAt[i] = Ground.Sand;
            }
        }

        // -------------------------------------------------------------------------------------- ramps

        private void Ramps()
        {
            foreach (int i in Shuffled())
            {
                var c = C(i);
                if (_ramps.Any(r => Math.Max(Math.Abs(r.X - c.X), Math.Abs(r.Y - c.Y)) < RampSpacing)) continue;
                for (int d = 0; d < 4; d++)
                    if (TryRamp(c, d, width: 1, depth: 2)) break;
            }
        }

        /// <summary>
        /// Ramp at the lower cell <paramref name="c"/> climbing toward direction <paramref name="d"/>:
        /// (2·width+1) cells along the cliff, <paramref name="depth"/> rows deep. False if the cliff there is not straight.
        /// </summary>
        private bool TryRamp(Cell c, int d, int width, int depth)
        {
            var (dx, dy) = Terrain.Dirs[d];
            var (px, py) = (dy, dx);   // perpendicular (|px|+|py| == 1)
            int level = _t.Level[I(c.X, c.Y)];
            for (int k = -width; k <= width; k++)
            {
                int x = c.X + px * k, y = c.Y + py * k;
                if (!RampOk(x, y, level) || !In(x + dx, y + dy)) return false;
                int up = I(x + dx, y + dy);
                if (!Land(up) || _t.Level[up] != level + 1) return false;
                for (int b = 1; b < depth; b++)
                    if (!RampOk(x - dx * b, y - dy * b, level)) return false;
            }
            for (int k = -width; k <= width; k++)
            for (int b = 0; b < depth; b++)
            {
                int j = I(c.X + px * k - dx * b, c.Y + py * k - dy * b);
                _t.Ramp[j] = (sbyte)d;
                _nat.Generate(j, NatureNode.Empty);
            }
            _ramps.Add(c);
            return true;
        }

        private bool _startChosen;

        /// <summary>Inside the start clearing (plus a 1-cell rim): nothing may change the ground there once chosen.</summary>
        private bool InClearing(int x, int y) =>
            _startChosen && Math.Max(Math.Abs(x - _t.Start.X), Math.Abs(y - _t.Start.Y)) <= ClearingHalf + 1;

        private bool RampOk(int x, int y, int level)
        {
            if (!In(x, y) || InClearing(x, y)) return false;
            int i = I(x, y);
            return Land(i) && _t.Level[i] == level && _t.Ramp[i] < 0 && _nat.DepositAt(new Cell(x, y)) is null;
        }

        private IEnumerable<int> Shuffled()
        {
            var order = Enumerable.Range(0, _n).ToArray();
            for (int i = _n - 1; i > 0; i--)
            {
                int j = _rng.NextInt(i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }
            return order;
        }

        // -------------------------------------------------------------------------------------- living layers

        private int[]? _distWater;

        private void Fertility()
        {
            _distWater = Bfs(Enumerable.Range(0, _n).Where(i => !Land(i)), (_, _) => true);
            for (int y = 0; y < _h; y++)
            for (int x = 0; x < _w; x++)
            {
                int i = I(x, y);
                if (_t.GroundAt[i] != Ground.Grass) continue;
                int f = Noise.Fbm(x, y, 48, 3, _s + 60) >> 8;
                if (_distWater[i] <= 12) f += (12 - _distWater[i]) * 4;
                if (_t.Level[i] == 0) f += 20;
                else if (_t.Level[i] == _t.Levels - 1) f -= 25;
                _t.Fertility[i] = (byte)Math.Clamp(f, 0, 255);
            }
        }

        private bool Eligible(int i) => _t.GroundAt[i] == Ground.Grass && _t.Ramp[i] < 0;

        private void Vegetation()
        {
            var forestN = new int[_n];
            var values = new List<int>();
            for (int y = 0; y < _h; y++)
            for (int x = 0; x < _w; x++)
            {
                long wx = Noise.Fbm(x, y, 32, 2, _s + 51) - Noise.One / 2;
                long fx = ((long)x << Noise.Shift) + wx * 12, fy = ((long)y << Noise.Shift) - wx * 12;
                int i = I(x, y);
                forestN[i] = Noise.Fbm(fx, fy, 40, 3, _s + 50);
                if (Eligible(i)) values.Add(forestN[i]);
            }
            values.Sort();
            int thr = values[Math.Min(values.Count - 1, values.Count * (1000 - _forestPermille) / 1000)];
            int top = values[^1];
            long mature = (long)_bal.TreeMatureDays * SimTime.TicksPerDay;

            for (int y = 0; y < _h; y++)
            for (int x = 0; x < _w; x++)
            {
                int i = I(x, y);
                if (!Eligible(i) || _nat.DepositAt(new Cell(x, y)) is not null) continue;
                int f = forestN[i];
                int roll = Noise.Roll(x, y, _s + 70) * 1000 / Noise.One;        // ‰
                int roll2 = Noise.Roll(x, y, _s + 71) * 1000 / Noise.One;
                int roll3 = Noise.Roll(x, y, _s + 72);
                int t = 0;                                                        // ‰ depth into the patch
                if (f >= thr)
                {
                    t = (int)((long)(f - thr) * 1000 / Math.Max(1, top - thr));
                    _t.Forest[i] = (byte)Math.Clamp(1 + t * 254 / 1000, 1, 255);
                }
                int pTree = f >= thr ? 420 + 530 * t / 1000 : (_t.Fertility[i] > 100 ? 12 : 4);
                if (roll < pTree)
                {
                    long planted = roll2 < 850
                        ? _startTick - mature - (long)(roll3 % 720) * SimTime.TicksPerDay
                        : _startTick - (long)(roll3 % _bal.TreeMatureDays) * SimTime.TicksPerDay;
                    _nat.Generate(i, new NatureNode(NodeKind.Tree, (byte)Species(x, y, i, roll2), planted, 0));
                    continue;
                }
                int edge = f - thr;
                if (edge is > -2600 and < 1500 && roll2 < 70) _nat.Generate(i, new NatureNode(NodeKind.Bush, 0, long.MinValue / 2, 0));
                else if (f >= thr && t > 450 && roll2 < 120) _nat.Generate(i, new NatureNode(NodeKind.Mushroom, 0, long.MinValue / 2, 0));
                else if (roll2 < (NextToHigher(x, y) ? 90 : 5))
                    _nat.Generate(i, new NatureNode(NodeKind.Stone, (byte)(roll3 % 4), 0, _bal.LooseStoneUnits));
            }
        }

        private TreeSpecies Species(int x, int y, int i, int roll2)
        {
            int moist = Noise.Fbm(x, y, 64, 3, _s + 40);
            int pick = (roll2 * 7) % 1000;
            if (_t.Level[i] == _t.Levels - 1 && pick < 600) return TreeSpecies.Pine;
            if (moist > Noise.One * 6 / 10 && _distWater![i] <= 14 && pick < 500) return TreeSpecies.Birch;
            if (moist < Noise.One * 4 / 10 && pick < 350) return TreeSpecies.Pine;
            return TreeSpecies.Oak;
        }

        /// <summary>A neighbour (8-way) on a higher level: the cell sits at the foot of a cliff.</summary>
        private bool NextToHigher(int x, int y)
        {
            int level = _t.Level[I(x, y)];
            for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
            {
                if (!In(x + dx, y + dy)) continue;
                int j = I(x + dx, y + dy);
                if (Land(j) && _t.Level[j] > level) return true;
            }
            return false;
        }

        // -------------------------------------------------------------------------------------- deposits

        private bool AreaFree(int ox, int oy, int size = Deposit.Size)
        {
            if (!In(ox, oy) || !In(ox + size - 1, oy + size - 1)) return false;
            int level = _t.Level[I(ox, oy)];
            for (int y = oy; y < oy + size; y++)
            for (int x = ox; x < ox + size; x++)
            {
                int i = I(x, y);
                if (!Eligible(i) || _t.Level[i] != level || _nat.DepositAt(new Cell(x, y)) is not null || InClearing(x, y)) return false;
            }
            return true;
        }

        private bool AtCliffFoot(int ox, int oy)
        {
            for (int y = oy; y < oy + Deposit.Size; y++)
            for (int x = ox; x < ox + Deposit.Size; x++)
                if (NextToHigher(x, y)) return true;
            return false;
        }

        private Deposit PlaceDeposit(DepositKind kind, Cell origin)
        {
            long units = kind == DepositKind.Outcrop ? _bal.OutcropUnits : _bal.OreUnits;
            for (int y = origin.Y; y < origin.Y + Deposit.Size; y++)
            for (int x = origin.X; x < origin.X + Deposit.Size; x++)
                _nat.Generate(I(x, y), NatureNode.Empty);
            return _nat.AddDeposit(kind, origin, units, rich: false);
        }

        private bool FarFromDeposits(Cell c, int min) =>
            _nat.Deposits.All(d => Math.Max(Math.Abs(d.Origin.X - c.X), Math.Abs(d.Origin.Y - c.Y)) >= min);

        private void Outcrops()
        {
            int want = _rng.Range(2, 4);
            foreach (int i in Shuffled())
            {
                if (_nat.Deposits.Count >= want) break;
                var c = C(i);
                if (c.X < 8 || c.Y < 8 || c.X > _w - 12 || c.Y > _h - 12) continue;
                if (!AreaFree(c.X, c.Y) || !FarFromDeposits(c, 28)) continue;
                bool top = _t.Level[i] == _t.Levels - 1;
                if (!AtCliffFoot(c.X, c.Y) && !(top && Noise.Roll(c.X, c.Y, _s + 80) < Noise.One * 3 / 10)) continue;
                PlaceDeposit(DepositKind.Outcrop, c);
            }
        }

        // -------------------------------------------------------------------------------------- start

        private bool ChooseStart()
        {
            // Square sums per level (flat, buildable cells) and per node kind.
            var flat = new int[_t.Levels][];
            for (int l = 0; l < _t.Levels; l++)
                flat[l] = Sums(i => Eligible(i) && _t.Level[i] == l && _nat.DepositAt(C(i)) is null);
            var trees = Sums(i => _nat.At(i).Kind == NodeKind.Tree);
            var stones = Sums(i => _nat.At(i).Kind == NodeKind.Stone);
            var bushes = Sums(i => _nat.At(i).Kind == NodeKind.Bush);
            var distTree = Bfs(Enumerable.Range(0, _n).Where(i => _nat.At(i).Kind == NodeKind.Tree), (_, _) => true);
            var depositCells = Enumerable.Range(0, _n).Where(i => _nat.DepositAt(C(i)) is not null).ToList();
            var distOut = depositCells.Count > 0 ? Bfs(depositCells, (_, _) => true) : null;

            long bestScore = long.MinValue;
            int best = -1;
            int side = 2 * ClearingHalf + 1;
            for (int y = EdgeMargin; y < _h - EdgeMargin; y++)
            for (int x = EdgeMargin; x < _w - EdgeMargin; x++)
            {
                int i = I(x, y);
                if (!Eligible(i)) continue;
                int l = _t.Level[i];
                if (Box(flat[l], x, y, ClearingHalf) != side * side) continue;
                int dw = _distWater![i], df = distTree[i];
                int dout = distOut?[i] ?? 999;
                // Water and an outcrop are guaranteed afterwards (pond / extra outcrop): here they only weigh in.
                if (dw < 5 || df is < 3 or > 18) continue;
                long score = 1000 - Math.Abs(dw - 10) * 20L - Math.Max(0, dw - 15) * 30L - Math.Abs(df - 7) * 20L
                             + Math.Min(Box(stones, x, y, 15), 15) * 6L + Math.Min(Box(bushes, x, y, 20), 10) * 5L
                             - Box(trees, x, y, 6) * 3L - (Math.Abs(x - _w / 2) + Math.Abs(y - _h / 2)) * 2L
                             - Math.Abs(Math.Min(dout, 60) - 22) * 2L + (l == 0 ? 10 : 0);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = i;
                }
            }
            if (best < 0) return false;
            _t.Start = C(best);
            _startChosen = true;
            return true;
        }

        /// <summary>Summed-area table (size (w+1)·(h+1)).</summary>
        private int[] Sums(Func<int, bool> pred)
        {
            int sw = _w + 1;
            var s = new int[sw * (_h + 1)];
            for (int y = 0; y < _h; y++)
            for (int x = 0; x < _w; x++)
                s[(y + 1) * sw + x + 1] = (pred(I(x, y)) ? 1 : 0) + s[y * sw + x + 1] + s[(y + 1) * sw + x] - s[y * sw + x];
            return s;
        }

        /// <summary>Count in the square of half-size <paramref name="r"/> around (x, y), clipped to the map.</summary>
        private int Box(int[] s, int x, int y, int r)
        {
            int sw = _w + 1;
            int x0 = Math.Max(0, x - r), y0 = Math.Max(0, y - r), x1 = Math.Min(_w, x + r + 1), y1 = Math.Min(_h, y + r + 1);
            return s[y1 * sw + x1] - s[y0 * sw + x1] - s[y1 * sw + x0] + s[y0 * sw + x0];
        }

        private void ClearStart()
        {
            var s = _t.Start;
            for (int y = s.Y - ClearingHalf - 1; y <= s.Y + ClearingHalf + 1; y++)
            for (int x = s.X - ClearingHalf - 1; x <= s.X + ClearingHalf + 1; x++)
            {
                int d2 = (x - s.X) * (x - s.X) + (y - s.Y) * (y - s.Y);
                var node = _nat.At(I(x, y));
                if (d2 <= 25 || (node.Kind == NodeKind.Tree && d2 <= 49)) _nat.Generate(I(x, y), NatureNode.Empty);
            }
        }

        // -------------------------------------------------------------------------------------- reachability

        private int[] Walk() => Bfs(new[] { I(_t.Start.X, _t.Start.Y) }, (i, j) => _t.CanStep(C(i), C(j)));

        /// <summary>Adds ramps between what the start reaches and the rest until ≥ 95 % of the land is reachable.</summary>
        private bool Connect()
        {
            int land = 0;
            for (int i = 0; i < _n; i++)
                if (Land(i)) land++;
            for (int iter = 0; iter < 400; iter++)
            {
                var reach = Walk();
                int reached = 0;
                for (int i = 0; i < _n; i++)
                    if (reach[i] != int.MaxValue) reached++;
                _reachPermille = (int)(reached * 1000L / land);
                if (_reachPermille >= 990 || !AddBridgeRamp(reach)) break;
            }
            return _reachPermille >= 950;
        }

        private bool AddBridgeRamp(int[] reach)
        {
            for (int i = 0; i < _n; i++)
            {
                if (reach[i] == int.MaxValue || !Land(i)) continue;
                var a = C(i);
                foreach (var (dx, dy) in Terrain.Dirs)
                {
                    int x = a.X + dx, y = a.Y + dy;
                    if (!In(x, y)) continue;
                    int j = I(x, y);
                    if (reach[j] != int.MaxValue || !Land(j) || Math.Abs(_t.Level[i] - _t.Level[j]) != 1) continue;
                    var (low, high) = _t.Level[i] < _t.Level[j] ? (a, new Cell(x, y)) : (new Cell(x, y), a);
                    int d = Array.IndexOf(Terrain.Dirs, (high.X - low.X, high.Y - low.Y));
                    if (_t.Ramp[I(low.X, low.Y)] >= 0) continue;
                    if (InClearing(low.X, low.Y)) continue;
                    if (!TryRamp(low, d, 1, 2) && !TryRamp(low, d, 0, 1))
                    {
                        _t.Ramp[I(low.X, low.Y)] = (sbyte)d;   // last resort: a one-cell ramp
                        _nat.Generate(I(low.X, low.Y), NatureNode.Empty);
                    }
                    return true;
                }
            }
            return false;
        }

        // -------------------------------------------------------------------------------------- guarantees

        private bool Guarantee()
        {
            var dist = Walk();
            if (!Enumerable.Range(0, _n).Any(i => dist[i] <= 15 && NextToWater(i)))
            {
                if (!DigPond(dist)) return false;
                dist = Walk();
            }
            if (!_nat.Deposits.Any(d => dist[I(d.Center.X, d.Center.Y)] <= 30))
            {
                var spot = DepositSpot(dist, 14, 30, preferCliff: false);
                if (spot is null) return false;
                PlaceDeposit(DepositKind.Outcrop, spot.Value);
            }
            foreach (var kind in new[] { DepositKind.Coal, DepositKind.Iron })
            {
                var spot = DepositSpot(dist, 40, int.MaxValue - 1, preferCliff: true) ?? FarthestSpot(dist);
                if (spot is null) return false;
                PlaceDeposit(kind, spot.Value);
            }
            // After the deposits (placing one clears the nodes under it).
            Ensure(dist, i => IsMatureTree(i), 25, 4, 14, i => _nat.Generate(i,
                new NatureNode(NodeKind.Tree, (byte)TreeSpecies.Oak, _startTick - (long)(_bal.TreeMatureDays + 30) * SimTime.TicksPerDay, 0)));
            Ensure(dist, i => _nat.At(i).Kind == NodeKind.Stone, 10, 4, 15,
                i => _nat.Generate(i, new NatureNode(NodeKind.Stone, (byte)(i % 4), 0, _bal.LooseStoneUnits)));
            Ensure(dist, i => _nat.At(i).Kind == NodeKind.Bush, 6, 6, 20,
                i => _nat.Generate(i, new NatureNode(NodeKind.Bush, 0, long.MinValue / 2, 0)));
            MarkRich(dist);
            Fauna(dist);
            return true;
        }

        private bool IsMatureTree(int i)
        {
            var node = _nat.At(i);
            return node.Kind == NodeKind.Tree && Nature.StageOf(node, _startTick, _bal) == TreeStage.Mature;
        }

        private bool NextToWater(int i)
        {
            if (!Land(i)) return false;
            var c = C(i);
            foreach (var (dx, dy) in Terrain.Dirs)
                if (In(c.X + dx, c.Y + dy) && !Land(I(c.X + dx, c.Y + dy))) return true;
            return false;
        }

        private bool Free(int i) => Eligible(i) && _nat.At(i).Kind == NodeKind.None && _nat.DepositAt(C(i)) is null
                                    && !InClearing(i % _w, i / _w);

        /// <summary>Tops up a resource near the start: at least <paramref name="want"/> within [min, max] walking cells.</summary>
        private void Ensure(int[] dist, Func<int, bool> has, int want, int min, int max, Action<int> add)
        {
            int have = Enumerable.Range(0, _n).Count(i => dist[i] >= min && dist[i] <= max && has(i));
            if (have >= want) return;
            foreach (int i in Shuffled())
            {
                if (have >= want) break;
                if (dist[i] < min || dist[i] > max || !Free(i)) continue;
                add(i);
                have++;
            }
        }

        private bool DigPond(int[] dist)
        {
            // The pond (water r ≈ 3, sand to r ≈ 4) sits just outside the clearing: its shore within ~15 cells.
            foreach (int i in Enumerable.Range(0, _n).Where(i => dist[i] is >= 13 and <= 18).OrderBy(i => dist[i]).ThenBy(i => i))
            {
                var c = C(i);
                if (Math.Max(Math.Abs(c.X - _t.Start.X), Math.Abs(c.Y - _t.Start.Y)) < ClearingHalf + 7) continue;
                if (!UniformDisk(c.X, c.Y, 5, _t.Level[i])) continue;
                for (int y = c.Y - 5; y <= c.Y + 5; y++)
                for (int x = c.X - 5; x <= c.X + 5; x++)
                {
                    int d2 = (x - c.X) * (x - c.X) + (y - c.Y) * (y - c.Y);
                    int j = I(x, y);
                    if (d2 <= 10)
                    {
                        _t.GroundAt[j] = Ground.Water;
                        _t.Depth[j] = (byte)(d2 <= 2 ? 3 : d2 <= 5 ? 2 : 1);
                        _nat.Generate(j, NatureNode.Empty);
                    }
                    else if (d2 <= 18)
                    {
                        _t.GroundAt[j] = Ground.Sand;
                        _t.Fertility[j] = 0;
                        _nat.Generate(j, NatureNode.Empty);
                    }
                }
                _pond = true;
                return true;
            }
            return false;
        }

        private Cell? DepositSpot(int[] dist, int min, int max, bool preferCliff)
        {
            Cell? fallback = null;
            foreach (int i in Shuffled())
            {
                var c = C(i);
                int center = In(c.X + 1, c.Y + 1) ? dist[I(c.X + 1, c.Y + 1)] : int.MaxValue;
                if (center < min || center > max || !AreaFree(c.X, c.Y) || !FarFromDeposits(c, 16)) continue;
                if (!preferCliff || AtCliffFoot(c.X, c.Y)) return c;
                fallback ??= c;
            }
            return fallback;
        }

        private Cell? FarthestSpot(int[] dist)
        {
            Cell? best = null;
            int bestD = -1;
            for (int i = 0; i < _n; i++)
            {
                var c = C(i);
                if (!In(c.X + 2, c.Y + 2)) continue;
                int d = dist[I(c.X + 1, c.Y + 1)];
                if (d == int.MaxValue || d <= bestD || !AreaFree(c.X, c.Y) || !FarFromDeposits(c, 16)) continue;
                bestD = d;
                best = c;
            }
            return best;
        }

        /// <summary>One rich deposit per map: the farthest of the chosen kind (outcrops: never the start's).</summary>
        private void MarkRich(int[] dist)
        {
            var rich = _nat.Deposits.Where(d => d.Kind == _rich)
                .OrderByDescending(d => dist[I(d.Center.X, d.Center.Y)] == int.MaxValue ? -1 : dist[I(d.Center.X, d.Center.Y)])
                .ThenBy(d => d.Index).First();
            rich.Rich = true;
            rich.InitialUnits *= _bal.RichDepositFactor;
            rich.Units = rich.InitialUnits;
        }

        private void Fauna(int[] dist)
        {
            var spawns = new List<FaunaSpawn>();
            bool Spaced(Cell c, int min) => spawns.All(s => Math.Max(Math.Abs(s.Cell.X - c.X), Math.Abs(s.Cell.Y - c.Y)) >= min);
            var order = Shuffled().ToArray();

            Cell? Find(int min, int max, bool forest, int spacing)
            {
                foreach (int i in order)
                {
                    var c = C(i);
                    if (dist[i] < min || dist[i] > max || !Eligible(i) || (forest && _t.Forest[i] == 0) || !Spaced(c, spacing)) continue;
                    return c;
                }
                return null;
            }

            // A herd of deer within reach of the start is guaranteed (hunting starts in the first minutes).
            var first = Find(15, 30, forest: true, 0) ?? Find(15, 30, forest: false, 0);
            if (first is { } f) spawns.Add(new FaunaSpawn(FaunaKind.Deer, f, _rng.Range(4, 6)));
            for (int k = _rng.Range(1, 3); k > 0; k--)
                if (Find(35, int.MaxValue - 1, true, 30) is { } c) spawns.Add(new FaunaSpawn(FaunaKind.Deer, c, _rng.Range(3, 6)));
            for (int k = _rng.Range(2, 4); k > 0; k--)
                if (Find(10, int.MaxValue - 1, false, 16) is { } c) spawns.Add(new FaunaSpawn(FaunaKind.Rabbit, c, _rng.Range(3, 5)));
            for (int k = _rng.Range(1, 2); k > 0; k--)
                if ((Find(60, int.MaxValue - 1, true, 30) ?? Find(45, int.MaxValue - 1, false, 30)) is { } c)
                    spawns.Add(new FaunaSpawn(FaunaKind.Wolf, c, _rng.Range(2, 3)));
            _nat.Fauna = spawns;
        }
    }
}
