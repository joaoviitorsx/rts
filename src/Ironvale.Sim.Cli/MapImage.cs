using System.IO.Compression;
using Ironvale.Sim.Content;
using Ironvale.Sim.Map;

/// <summary>
/// Top-down PNG of a generated map (CLI <c>--map-png</c>): terraces, cliffs, ramps, water by depth, sand, trees
/// (autumn tint by hash, as the view will do), bushes, stones, deposits, fauna spawns and the start. For quick review
/// of the generator without the engine.
/// </summary>
public static class MapImage
{
    public static void Write(string path, GeneratedWorld g, long now, BalanceDef bal, int scale = 3, bool fertility = false)
    {
        var t = g.Terrain;
        var nat = g.Nature;
        int w = t.Width * scale, h = t.Height * scale;
        var px = new byte[w * h * 3];

        void Fill(int cx, int cy, (int r, int g, int b) c, int inset = 0)
        {
            for (int y = cy * scale + inset; y < (cy + 1) * scale - inset; y++)
            for (int x = cx * scale + inset; x < (cx + 1) * scale - inset; x++)
            {
                int o = (y * w + x) * 3;
                px[o] = (byte)c.r;
                px[o + 1] = (byte)c.g;
                px[o + 2] = (byte)c.b;
            }
        }

        for (int y = 0; y < t.Height; y++)
        for (int x = 0; x < t.Width; x++)
        {
            var c = new Cell(x, y);
            var color = Ground(t, c, fertility);
            Fill(x, y, color);
            if (fertility || t.IsWater(c)) continue;
            var node = nat.At(c);
            switch (node.Kind)
            {
                case NodeKind.Tree:
                    var stage = Nature.StageOf(node, now, bal);
                    if (stage == TreeStage.Stump) Fill(x, y, (120, 95, 60), scale / 3);
                    else Fill(x, y, TreeColor(node, c, stage), stage == TreeStage.Mature ? 0 : scale / 3);
                    break;
                case NodeKind.Bush: Fill(x, y, (205, 60, 75), scale / 3); break;
                case NodeKind.Mushroom: Fill(x, y, (235, 225, 200), scale / 3); break;
                case NodeKind.Stone: Fill(x, y, (150, 150, 150), scale / 3); break;
            }
        }
        foreach (var d in nat.Deposits)
        {
            var color = d.Kind switch { DepositKind.Outcrop => (115, 115, 122), DepositKind.Coal => (35, 35, 35), _ => (165, 80, 50) };
            for (int y = 0; y < Deposit.Size; y++)
            for (int x = 0; x < Deposit.Size; x++)
                Fill(d.Origin.X + x, d.Origin.Y + y, d.Rich && (x != 1 || y != 1) ? (240, 200, 40) : color);
        }
        foreach (var f in nat.Fauna)
        {
            var color = f.Kind switch { FaunaKind.Deer => (150, 95, 50), FaunaKind.Rabbit => (245, 245, 245), _ => (70, 70, 85) };
            for (int k = 0; k < f.Count; k++) Fill(f.Cell.X + k % 3 - 1, f.Cell.Y + k / 3, color);
        }
        for (int dy = -1; dy <= 1; dy++)
        for (int dx = -1; dx <= 1; dx++)
            Fill(t.Start.X + dx, t.Start.Y + dy, dx == 0 || dy == 0 ? (255, 255, 255) : (230, 40, 40));

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllBytes(path, Png(w, h, px));
    }

    private static (int, int, int) Ground(Terrain t, Cell c, bool fertility)
    {
        if (t.IsWater(c))
        {
            int d = Math.Min(t.DepthAt(c), 8);
            return (Lerp(125, 25, d, 8), Lerp(220, 135, d, 8), Lerp(212, 170, d, 8));
        }
        if (fertility)
        {
            int f = t.FertilityAt(c);
            return (Lerp(170, 60, f, 255), Lerp(140, 170, f, 255), Lerp(80, 40, f, 255));
        }
        if (t.IsRamp(c)) return (195, 160, 105);
        // Top of a cliff: light stone, like the walls in the reference.
        foreach (var (dx, dy) in Terrain.Dirs)
        {
            var n = new Cell(c.X + dx, c.Y + dy);
            if (t.InBounds(n) && !t.IsWater(n) && t.LevelAt(n) < t.LevelAt(c) && !t.IsRamp(n)) return (214, 211, 198);
        }
        if (t.GroundOf(c) == Ironvale.Sim.Map.Ground.Sand) return (228, 208, 155);
        int l = t.LevelAt(c);
        return (105 + l * 14, 156 + l * 12, 52 + l * 8);
    }

    private static (int, int, int) TreeColor(NatureNode node, Cell c, TreeStage stage)
    {
        int roll = Noise.Roll(c.X, c.Y, 0xC0105u) % 100;
        (int, int, int) color = node.Species switch
        {
            TreeSpecies.Pine => (38, 82, 52),
            TreeSpecies.Birch => (110, 160, 60),
            _ => roll < 14 ? (225, 150, 40) : roll < 24 ? (230, 195, 55) : roll < 26 ? (200, 85, 50) : (52, 112, 42),
        };
        return stage == TreeStage.Mature ? color : (color.Item1 + 40, color.Item2 + 40, color.Item3 + 20);
    }

    private static int Lerp(int a, int b, int t, int max) => a + (b - a) * t / max;

    // ------------------------------------------------------------------ minimal PNG writer (RGB, no filter)

    private static byte[] Png(int w, int h, byte[] rgb)
    {
        using var raw = new MemoryStream();
        for (int y = 0; y < h; y++)
        {
            raw.WriteByte(0);
            raw.Write(rgb, y * w * 3, w * 3);
        }
        using var z = new MemoryStream();
        using (var zs = new ZLibStream(z, CompressionLevel.Optimal, leaveOpen: true)) raw.WriteTo(zs);

        using var png = new MemoryStream();
        png.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        var ihdr = new byte[13];
        BigEndian(ihdr, 0, w);
        BigEndian(ihdr, 4, h);
        ihdr[8] = 8;    // bit depth
        ihdr[9] = 2;    // RGB
        Chunk(png, "IHDR", ihdr);
        Chunk(png, "IDAT", z.ToArray());
        Chunk(png, "IEND", Array.Empty<byte>());
        return png.ToArray();
    }

    private static void Chunk(Stream s, string type, byte[] data)
    {
        var len = new byte[4];
        BigEndian(len, 0, data.Length);
        s.Write(len);
        var typed = new byte[4 + data.Length];
        System.Text.Encoding.ASCII.GetBytes(type).CopyTo(typed, 0);
        data.CopyTo(typed, 4);
        s.Write(typed);
        var crc = new byte[4];
        BigEndian(crc, 0, (int)Crc32(typed));
        s.Write(crc);
    }

    private static void BigEndian(byte[] b, int o, int v)
    {
        b[o] = (byte)(v >> 24);
        b[o + 1] = (byte)(v >> 16);
        b[o + 2] = (byte)(v >> 8);
        b[o + 3] = (byte)v;
    }

    private static uint Crc32(byte[] data)
    {
        uint crc = 0xFFFFFFFFu;
        foreach (byte b in data)
        {
            crc ^= b;
            for (int k = 0; k < 8; k++) crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
        }
        return ~crc;
    }
}
