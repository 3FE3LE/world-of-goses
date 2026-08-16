using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// Structural guards for the per-biome ground profiles in
/// <c>game/assets/terrain/biomes/</c>.
/// </summary>
/// <remarks>
/// <para>
/// The load-bearing one is <see cref="EveryFillTile_TilesWithItself"/>. A fill
/// tile is repeated hundreds of times across the floor, so it has to match its
/// own opposite edge; most coloured bands in a sheet are autotile sets whose
/// tiles carry a corner or edge of the neighbouring material, and repeating one
/// shows the cut as a regular bite. That mistake shipped twice — orange 1026
/// was an inner corner, and magenta 1197 and teal 466 were withdrawn for the
/// same reason — and both times a human had to notice it in a capture.
/// </para>
/// <para>
/// The separation is not marginal. The eleven fills in use score 0 to 0.25 on
/// the metric below; 1026, 1197 and 466 score 18, 18 and 61. The threshold sits
/// two orders of magnitude away from both.
/// </para>
/// <para>
/// These parse the <c>.tres</c> as text and decode the PNG by hand for the same
/// reason <see cref="HudThemeVariationTests"/> parses the theme as text: the
/// test project has no Godot runtime, and the facts worth protecting are
/// declarative. The decoder handles 8-bit non-interlaced RGB/RGBA, which is
/// what the pipeline produces; anything else fails loudly rather than silently
/// passing.
/// </para>
/// </remarks>
public sealed class GroundAtlasTests
{
    private static readonly string[] Lineages =
    {
        "ardhen", "eirune", "kovari", "myrven", "vaelun", "orveth", "caelith", "theryn",
    };

    /// <summary>
    /// Mean per-channel disagreement between a tile's opposite edges, above
    /// which it is not a fill.
    /// </summary>
    /// <remarks>
    /// Four measured populations set this, not a guess:
    /// <list type="bullet">
    ///   <item>Kenney's flat swatches, the fills in use: <b>0 – 0.25</b>.</item>
    ///   <item>Hand-painted grass authored for this project: <b>3.5 – 4.8</b>.
    ///     Texture across the tile means opposite edges are close rather than
    ///     identical; at 4 each edge pixel differs by about 1.6% per channel,
    ///     which is not visible.</item>
    ///   <item>Kenney's autotile corners — 1026, 1197, 466, all withdrawn after
    ///     rendering them: <b>18 – 61</b>.</item>
    ///   <item>Sparse props, which cannot be fills anyway because
    ///     <see cref="EveryDeclaredTile_IsOpaque"/> rejects transparency.</item>
    /// </list>
    /// Eight sits 1.7x above the worst legitimate fill and 2.3x below the
    /// cheapest known mistake. The first draft was 2.0, calibrated only on
    /// Kenney's machine-flat swatches, and it rejected every hand-painted tile
    /// in the first authored sheet.
    /// </remarks>
    private const double SeamTolerance = 8.0;

    public static TheoryData<string> AllLineages()
    {
        var data = new TheoryData<string>();
        foreach (string lineage in Lineages) data.Add(lineage);
        return data;
    }

    [Theory]
    [MemberData(nameof(AllLineages))]
    public void EveryLineage_HasAGroundProfile(string lineage)
    {
        string path = ProfilePath(lineage);
        Assert.True(
            File.Exists(path),
            $"{lineage} has no ground profile at {path}. TerrainAtlas.GroundProfilePathFor "
            + "composes this path, so a missing file is a lineage that renders no floor.");
    }

    [Fact]
    public void TheBiomeDirectory_HoldsNothingButTheEightProfiles()
    {
        string[] found = Directory
            .GetFiles(BiomeDirectory(), "*.tres")
            .Select(Path.GetFileNameWithoutExtension)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray()!;

        string[] expected = Lineages
            .Select(lineage => lineage + "_ground")
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expected, found);
    }

    [Theory]
    [MemberData(nameof(AllLineages))]
    public void EveryProfile_PointsAtASheetThatExists(string lineage)
    {
        GroundProfile profile = ReadProfile(lineage);
        Assert.True(
            File.Exists(profile.AtlasPath),
            $"{lineage}'s profile names {profile.AtlasPath}, which does not exist.");
    }

    /// <summary>
    /// No declared id falls outside its own sheet. Catches the failure mode a
    /// new tileset makes easy: the sheet shrinks, the ids do not.
    /// </summary>
    [Theory]
    [MemberData(nameof(AllLineages))]
    public void EveryDeclaredTile_IsInsideItsSheet(string lineage)
    {
        GroundProfile profile = ReadProfile(lineage);
        Png sheet = Png.Read(profile.AtlasPath);

        int rows = (sheet.Height + profile.Separation) / profile.Stride;
        int maxId = (profile.Columns * rows) - 1;

        foreach (int id in profile.AllTiles())
        {
            Assert.True(
                id >= 0 && id <= maxId,
                $"{lineage} declares tile {id}, but its sheet holds {profile.Columns}x{rows} "
                + $"tiles (max id {maxId}).");

            int x = (id % profile.Columns) * profile.Stride;
            int y = (id / profile.Columns) * profile.Stride;
            Assert.True(
                x + profile.TileSize <= sheet.Width && y + profile.TileSize <= sheet.Height,
                $"{lineage}'s tile {id} runs off the sheet at ({x},{y}).");
        }
    }

    /// <summary>
    /// Every declared tile draws something. An id that lands on an empty cell
    /// is invisible rather than wrong, which is the harder bug to see.
    /// </summary>
    [Theory]
    [MemberData(nameof(AllLineages))]
    public void EveryDeclaredTile_IsOpaque(string lineage)
    {
        GroundProfile profile = ReadProfile(lineage);
        Png sheet = Png.Read(profile.AtlasPath);

        foreach (int id in profile.AllTiles())
        {
            int opaque = 0;
            for (int row = 0; row < profile.TileSize; row++)
            {
                for (int column = 0; column < profile.TileSize; column++)
                {
                    (_, _, _, int alpha) = TilePixel(sheet, profile, id, column, row);
                    if (alpha > 0) opaque++;
                }
            }

            int total = profile.TileSize * profile.TileSize;
            Assert.True(
                opaque == total,
                $"{lineage}'s tile {id} is {total - opaque}/{total} transparent. Ground has "
                + "no holes; a partly transparent tile is an id that landed on the wrong cell.");
        }
    }

    /// <summary>
    /// A fill and a path tile both repeat, so both must match their own
    /// opposite edge. See the remarks on this class for why this is the test
    /// that matters.
    /// </summary>
    [Theory]
    [MemberData(nameof(AllLineages))]
    public void EveryFillTile_TilesWithItself(string lineage)
    {
        GroundProfile profile = ReadProfile(lineage);
        Png sheet = Png.Read(profile.AtlasPath);

        foreach (int id in profile.AllTiles())
        {
            double seam = SeamDiscontinuity(sheet, profile, id);
            Assert.True(
                seam <= SeamTolerance,
                $"{lineage}'s tile {id} scores {seam:0.00} against a {SeamTolerance:0.00} "
                + "tolerance: its opposite edges disagree, so repeating it shows the cut. "
                + "This is what an autotile corner or edge looks like from here — the fill "
                + "is somewhere else in the block.");
        }
    }

    /// <summary>
    /// Mean per-channel difference between left and right columns and between
    /// top and bottom rows — what a viewer sees at the join when the tile is
    /// laid next to a copy of itself.
    /// </summary>
    private static double SeamDiscontinuity(Png sheet, GroundProfile profile, int id)
    {
        double total = 0;
        int size = profile.TileSize;
        for (int i = 0; i < size; i++)
        {
            (int lr, int lg, int lb, _) = TilePixel(sheet, profile, id, 0, i);
            (int rr, int rg, int rb, _) = TilePixel(sheet, profile, id, size - 1, i);
            (int tr, int tg, int tb, _) = TilePixel(sheet, profile, id, i, 0);
            (int br, int bg, int bb, _) = TilePixel(sheet, profile, id, i, size - 1);

            total += Math.Abs(lr - rr) + Math.Abs(lg - rg) + Math.Abs(lb - rb);
            total += Math.Abs(tr - br) + Math.Abs(tg - bg) + Math.Abs(tb - bb);
        }
        return total / (size * 2 * 3);
    }

    private static (int R, int G, int B, int A) TilePixel(
        Png sheet, GroundProfile profile, int id, int column, int row) =>
        sheet.At(
            ((id % profile.Columns) * profile.Stride) + column,
            ((id / profile.Columns) * profile.Stride) + row);

    // ---------- Reading the profile ----------

    private sealed record GroundProfile(
        string AtlasPath, int TileSize, int Separation, int Columns, int[] Fill, int Path)
    {
        public int Stride => TileSize + Separation;

        public IEnumerable<int> AllTiles() => Fill.Append(Path).Distinct();
    }

    private static GroundProfile ReadProfile(string lineage)
    {
        string text = File.ReadAllText(ProfilePath(lineage));

        Match atlas = Regex.Match(text, @"\[ext_resource type=""Texture2D""[^\]]*path=""([^""]+)""");
        Assert.True(atlas.Success, $"{lineage}'s profile declares no Texture2D atlas.");

        return new GroundProfile(
            AtlasPath: ResolveResPath(atlas.Groups[1].Value),
            TileSize: ReadInt(text, "TileSize", lineage),
            Separation: ReadInt(text, "Separation", lineage, fallback: 0),
            Columns: ReadInt(text, "Columns", lineage),
            Fill: ReadIntArray(text, "Fill", lineage),
            Path: ReadInt(text, "Path", lineage));
    }

    private static int ReadInt(string text, string key, string lineage, int? fallback = null)
    {
        Match match = Regex.Match(text, $@"(?m)^{Regex.Escape(key)}\s*=\s*(-?\d+)\s*$");
        if (!match.Success && fallback is { } value) return value;
        Assert.True(match.Success, $"{lineage}'s profile declares no {key}.");
        return int.Parse(match.Groups[1].Value);
    }

    private static int[] ReadIntArray(string text, string key, string lineage)
    {
        Match match = Regex.Match(
            text, $@"(?m)^{Regex.Escape(key)}\s*=\s*PackedInt32Array\(([^)]*)\)\s*$");
        Assert.True(match.Success, $"{lineage}'s profile declares no {key} array.");

        string body = match.Groups[1].Value.Trim();
        if (body.Length == 0) return Array.Empty<int>();
        return body.Split(',').Select(part => int.Parse(part.Trim())).ToArray();
    }

    private static string BiomeDirectory() =>
        Path.Combine(GameRoot(), "assets", "terrain", "biomes");

    private static string ProfilePath(string lineage) =>
        Path.Combine(BiomeDirectory(), $"{lineage}_ground.tres");

    private static string ResolveResPath(string resPath) => Path.Combine(
        GameRoot(),
        resPath.Replace("res://", string.Empty).Replace('/', Path.DirectorySeparatorChar));

    private static string GameRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            string candidate = Path.Combine(directory.FullName, "game", "project.godot");
            if (File.Exists(candidate)) return Path.Combine(directory.FullName, "game");
            directory = directory.Parent;
        }
        throw new FileNotFoundException("Could not locate the Godot project root.");
    }

    // ---------- Reading the sheet ----------

    /// <summary>
    /// The smallest PNG reader that can answer "what colour is this pixel":
    /// 8-bit, non-interlaced, RGB or RGBA. Deliberately not a dependency —
    /// <c>ZLibStream</c> ships with .NET and the un-filtering is five cases.
    /// </summary>
    private sealed class Png
    {
        private readonly byte[] _pixels;
        private readonly int _bytesPerPixel;

        public int Width { get; }
        public int Height { get; }

        private Png(int width, int height, int bytesPerPixel, byte[] pixels)
        {
            Width = width;
            Height = height;
            _bytesPerPixel = bytesPerPixel;
            _pixels = pixels;
        }

        public (int R, int G, int B, int A) At(int x, int y)
        {
            int offset = ((y * Width) + x) * _bytesPerPixel;
            return (
                _pixels[offset],
                _pixels[offset + 1],
                _pixels[offset + 2],
                _bytesPerPixel == 4 ? _pixels[offset + 3] : 255);
        }

        public static Png Read(string path)
        {
            byte[] file = File.ReadAllBytes(path);
            Assert.True(
                file.Length > 8 && file[1] == 'P' && file[2] == 'N' && file[3] == 'G',
                $"{path} is not a PNG.");

            int width = 0, height = 0, bytesPerPixel = 0;
            var compressed = new MemoryStream();

            int cursor = 8;
            while (cursor + 8 <= file.Length)
            {
                int length = ReadBigEndian(file, cursor);
                string type = System.Text.Encoding.ASCII.GetString(file, cursor + 4, 4);
                int body = cursor + 8;

                if (type == "IHDR")
                {
                    width = ReadBigEndian(file, body);
                    height = ReadBigEndian(file, body + 4);
                    int bitDepth = file[body + 8];
                    int colorType = file[body + 9];
                    int interlace = file[body + 12];

                    Assert.True(bitDepth == 8, $"{path} is {bitDepth}-bit; this reader wants 8.");
                    Assert.True(interlace == 0, $"{path} is interlaced; this reader wants a flat scan.");
                    Assert.True(
                        colorType is 2 or 6,
                        $"{path} has colour type {colorType}; this reader wants RGB (2) or RGBA (6).");
                    bytesPerPixel = colorType == 6 ? 4 : 3;
                }
                else if (type == "IDAT")
                {
                    compressed.Write(file, body, length);
                }
                else if (type == "IEND")
                {
                    break;
                }

                cursor = body + length + 4;
            }

            compressed.Position = 0;
            using var inflate = new ZLibStream(compressed, CompressionMode.Decompress);
            using var raw = new MemoryStream();
            inflate.CopyTo(raw);

            return new Png(width, height, bytesPerPixel, Unfilter(raw.ToArray(), width, height, bytesPerPixel));
        }

        /// <summary>Reverses the five per-scanline PNG filters, in place.</summary>
        private static byte[] Unfilter(byte[] raw, int width, int height, int bytesPerPixel)
        {
            int stride = width * bytesPerPixel;
            var output = new byte[stride * height];

            for (int row = 0; row < height; row++)
            {
                int rawRow = row * (stride + 1);
                byte filter = raw[rawRow];
                int outRow = row * stride;

                for (int i = 0; i < stride; i++)
                {
                    int left = i >= bytesPerPixel ? output[outRow + i - bytesPerPixel] : 0;
                    int up = row > 0 ? output[outRow - stride + i] : 0;
                    int upLeft = row > 0 && i >= bytesPerPixel
                        ? output[outRow - stride + i - bytesPerPixel]
                        : 0;
                    int value = raw[rawRow + 1 + i];

                    output[outRow + i] = filter switch
                    {
                        0 => (byte)value,
                        1 => (byte)(value + left),
                        2 => (byte)(value + up),
                        3 => (byte)(value + ((left + up) / 2)),
                        4 => (byte)(value + Paeth(left, up, upLeft)),
                        _ => throw new InvalidDataException($"Unknown PNG filter {filter}."),
                    };
                }
            }
            return output;
        }

        private static int Paeth(int a, int b, int c)
        {
            int p = a + b - c;
            int pa = Math.Abs(p - a), pb = Math.Abs(p - b), pc = Math.Abs(p - c);
            if (pa <= pb && pa <= pc) return a;
            return pb <= pc ? b : c;
        }

        private static int ReadBigEndian(byte[] data, int offset) =>
            (data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3];
    }
}
