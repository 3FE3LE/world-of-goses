using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// Structural guards for the compact HUD profile in <c>default_theme.tres</c>.
/// </summary>
/// <remarks>
/// <para>
/// These parse the theme as text, the same way <see cref="HudCompositionTests"/>
/// parses <c>CityPrototype.tscn</c>, because the test project has no Godot runtime
/// and the facts worth protecting are declarative anyway.
/// </para>
/// <para>
/// The load-bearing one is <see cref="ScreenVariations_AreUnchangedByTheHudProfile"/>.
/// The compact profile exists precisely so the HUD can shrink <em>without</em> the
/// screens shrinking with it, and the failure mode is silent: someone reaches for
/// "the body text size", edits <c>BodyText</c> instead of <c>HudBody</c>, and every
/// modal in the game quietly loses two points with no test and no fixture to notice.
/// </para>
/// </remarks>
public sealed class HudThemeVariationTests
{
    private const string Jacquard12 = "4_jacquard12";
    private const string Jersey = "2_jersey";
    private const string Micro5 = "3_micro5";

    /// <summary>
    /// Rendered cap height, in pixels, that no ordinary HUD text may fall under.
    /// </summary>
    /// <remarks>
    /// This replaces a floor expressed in <c>font_size</c>. That floor was signed
    /// off against Pixelify Sans, whose cap is 0.70 em; it stopped meaning anything
    /// the moment the HUD carried families with different cap ratios, because
    /// Micro 5 at 22 px and Jersey 10 at 22 px do not draw the same size of letter.
    /// The number below is what the shipped HUD already measured: Jersey 10 at
    /// 16 px — <c>HudLabel</c>, <c>HudButton</c> — renders an 8.57 px cap, and it
    /// was the smallest text in the profile long before this change. The old
    /// "14 px" never corresponded to 9.8 px of cap anywhere except in the two
    /// Pixelify rows it was written for.
    /// </remarks>
    private const double CapHeightFloor = 8.5;

    /// <summary>
    /// The two slots that go under <see cref="CapHeightFloor"/>, named rather than
    /// pattern-matched so that adding a third is a visible edit to this list.
    /// </summary>
    /// <remarks>
    /// Both sit inside a box that no ordinary HUD text has to fit: the progress
    /// readout inside an 11 px bar (<c>Tokens.HudBarHeightCard</c>) and the count
    /// inside an 18 px pill (<c>Tokens.HudBadgeHeight</c>). Micro 5 at its native
    /// 11 px grid is the only font in the project that renders a legible figure in
    /// that space with whole pixels — every other family is off-grid there. A 5 px
    /// cap is deliberately below the floor and carries its own capture.
    /// </remarks>
    private static readonly Dictionary<string, double> BelowFloorByDesign = new(StringComparer.Ordinal)
    {
        ["HudBadgeNumeric"] = 5.0,
        ["HudProgress"] = 5.0,
    };

    public static TheoryData<string, string, int> HudTextVariations() => new()
    {
        { "HudBrand", Jacquard12, 20 },
        { "HudHeader", Jersey, 18 },
        { "HudLabel", Jersey, 16 },
        { "HudBody", Micro5, 22 },
        { "HudNumeric", Micro5, 22 },
        { "HudCaption", Jersey, 16 },
        { "HudBadgeNumeric", Micro5, 11 },
    };

    public static TheoryData<string, string> HudChromeVariations() => new()
    {
        { "HudSurface", "PanelContainer" },
        { "HudInset", "PanelContainer" },
        { "HudHeaderSurface", "PanelContainer" },
        { "HudCard", "PanelContainer" },
        { "HudDock", "PanelContainer" },
        { "HudBadge", "PanelContainer" },
        { "HudButton", "Button" },
        { "HudButtonSelected", "Button" },
        { "HudButtonDanger", "Button" },
        { "HudCollapsibleHeader", "Button" },
        { "HudProgress", "ProgressBar" },
        { "HudSeparator", "HSeparator" },
    };

    [Theory]
    [MemberData(nameof(HudTextVariations))]
    public void HudTextVariation_UsesItsDeclaredFontAndSize(string variation, string font, int size)
    {
        Dictionary<string, string> theme = ReadTheme();

        Assert.Equal("&\"Label\"", theme[$"{variation}/base_type"]);
        Assert.Equal($"ExtResource(\"{font}\")", theme[$"{variation}/fonts/font"]);
        Assert.Equal(size.ToString(), theme[$"{variation}/font_sizes/font_size"]);
    }

    /// <summary>
    /// The HUD draws with the three compact-scale families and no other.
    /// </summary>
    /// <remarks>
    /// The project ships six faces, but three of them are screen-scale or
    /// ceremonial — Jacquard 24 for the 36-48 px titles, Jersey 15 for the 22-26 px
    /// chrome, Jacquarda Bastarda 9 for the founder. Each is drawn on a coarser
    /// grid than the compact profile can host: Jersey 15's native em is 27 px, so
    /// at a 16 px HUD row it renders 0.59 px per design pixel and the stems drop
    /// out. Keeping them out of <c>Hud*</c> is what stops a screen-scale face from
    /// arriving in the HUD by way of a copied line.
    /// </remarks>
    [Fact]
    public void HudTypography_StaysInsideTheCompactScaleFamilies()
    {
        Dictionary<string, string> theme = ReadTheme();

        string[] allowed = { $"ExtResource(\"{Jacquard12}\")", $"ExtResource(\"{Jersey}\")", $"ExtResource(\"{Micro5}\")" };
        foreach ((string key, string value) in theme.Where(e => e.Key.StartsWith("Hud", StringComparison.Ordinal)
                                                               && e.Key.EndsWith("/fonts/font", StringComparison.Ordinal)))
        {
            Assert.True(
                allowed.Contains(value),
                $"{key} resolves to {value}. The compact profile hosts Jacquard 12, "
                + "Jersey 10 and Micro 5 (UI_PATTERNS.md §5.0); the screen-scale faces "
                + "render sub-pixel at HUD sizes and cannot arrive through the HUD.");
        }
    }

    /// <summary>
    /// No HUD text renders a smaller letter than the profile was signed off at.
    /// </summary>
    /// <remarks>
    /// The assertion is on rendered cap height rather than on <c>font_size</c>,
    /// and the ratio is read out of the font file rather than restated here, so a
    /// family swap cannot leave a stale constant behind. See
    /// <see cref="CapHeightFloor"/> for why the previous font_size floor stopped
    /// describing anything real.
    /// </remarks>
    [Fact]
    public void HudTypography_NeverRendersBelowTheSignedCapHeight()
    {
        Dictionary<string, string> theme = ReadTheme();
        Dictionary<string, string> resources = ReadExternalResources();

        foreach ((string key, string value) in theme.Where(e => e.Key.StartsWith("Hud", StringComparison.Ordinal)
                                                               && e.Key.EndsWith("/font_sizes/font_size", StringComparison.Ordinal)))
        {
            string variation = key[..key.IndexOf('/', StringComparison.Ordinal)];
            if (!theme.TryGetValue($"{variation}/fonts/font", out string? fontRef)) continue;

            double capRatio = ReadCapRatio(ResolveFontPath(fontRef, resources));
            double renderedCap = int.Parse(value) * capRatio;
            double floor = BelowFloorByDesign.TryGetValue(variation, out double exempt) ? exempt : CapHeightFloor;

            Assert.True(
                renderedCap >= floor - 0.01,
                $"{variation} renders a {renderedCap:0.00} px cap, under its {floor:0.00} px floor. "
                + "The floor is what a real 1280x720 capture was signed off at; going under it "
                + "needs its own capture and its own sign-off, not a quiet edit.");
        }
    }

    /// <summary>Every font the theme names is a file that exists.</summary>
    [Fact]
    public void EveryFontTheThemeNames_ExistsOnDisk()
    {
        Dictionary<string, string> resources = ReadExternalResources();
        Dictionary<string, string> theme = ReadTheme();

        IEnumerable<string> referenced = theme
            .Where(e => e.Key.EndsWith("/fonts/font", StringComparison.Ordinal))
            .Select(e => e.Value)
            .Distinct();

        foreach (string fontRef in referenced)
        {
            string path = ResolveFontPath(fontRef, resources);
            Assert.True(File.Exists(path), $"{fontRef} points at {path}, which does not exist.");
        }
    }

    [Theory]
    [MemberData(nameof(HudChromeVariations))]
    public void HudChromeVariation_IsRegisteredOnItsBaseType(string variation, string baseType)
    {
        Dictionary<string, string> theme = ReadTheme();

        Assert.True(
            theme.TryGetValue($"{variation}/base_type", out string? declared),
            $"{variation} is not registered in default_theme.tres.");
        Assert.Equal($"&\"{baseType}\"", declared);
    }

    /// <summary>
    /// Every HUD surface and button draws an authored Kenney frame, not a rectangle
    /// the theme drew itself.
    /// </summary>
    /// <remarks>
    /// The two exemptions are deliberate and named rather than pattern-matched, so
    /// adding a third is a visible edit to this list. <c>HudSeparator</c> is a
    /// <c>StyleBoxLine</c> because a rounded 10x10 outline cannot draw a straight
    /// one-pixel rule, and the progress <em>fill</em> is a flat colour bar with no
    /// border to author — stretching a 9-slice across a six-pixel interior would
    /// repeat its corner along the length.
    /// </remarks>
    [Fact]
    public void HudChrome_DrawsAuthoredFramesRatherThanFlatBoxes()
    {
        Dictionary<string, string> theme = ReadTheme();
        var exempt = new HashSet<string>(StringComparer.Ordinal)
        {
            "HudSeparator/styles/separator",
            "HudProgress/styles/fill",
        };

        foreach ((string key, string value) in theme.Where(e => e.Key.StartsWith("Hud", StringComparison.Ordinal)
                                                               && e.Key.Contains("/styles/", StringComparison.Ordinal)))
        {
            // A focus ring is a transparent helper, not a surface; the whole project
            // shares one and it is allowed to be flat.
            if (key.EndsWith("/focus", StringComparison.Ordinal) || exempt.Contains(key)) continue;

            Assert.True(
                value.StartsWith("ExtResource(", StringComparison.Ordinal),
                $"{key} is {value}. Visible HUD chrome resolves to a composited Kenney "
                + "frame; StyleBoxFlat is reserved for transparent helpers and for the "
                + "primitives the pack genuinely lacks, which are listed in this test.");
        }
    }

    [Fact]
    public void EveryStyleboxTheHudNames_ExistsOnDisk()
    {
        string themePath = ResolveThemePath();
        Dictionary<string, string> resources = ReadExternalResources();
        string projectRoot = Path.GetDirectoryName(themePath)!;

        foreach ((string id, string path) in resources.Where(r => r.Value.Contains("/composites/", StringComparison.Ordinal)))
        {
            string relative = path.Replace("res://", string.Empty).Replace('/', Path.DirectorySeparatorChar);
            string absolute = Path.Combine(GameRoot(projectRoot), relative);
            Assert.True(File.Exists(absolute), $"ExtResource {id} points at {path}, which does not exist.");
        }
    }

    /// <summary>
    /// The compact profile must not have moved anything the screens depend on.
    /// </summary>
    [Theory]
    [InlineData("BodyText/font_sizes/font_size", "18")]
    [InlineData("BodySmall/font_sizes/font_size", "16")]
    [InlineData("PanelTitle/font_sizes/font_size", "26")]
    [InlineData("SectionTitle/font_sizes/font_size", "22")]
    [InlineData("ButtonText/font_sizes/font_size", "20")]
    [InlineData("ButtonPrimary/font_sizes/font_size", "22")]
    [InlineData("ScreenTitle/font_sizes/font_size", "36")]
    [InlineData("GameTitle/font_sizes/font_size", "48")]
    [InlineData("Label/font_sizes/font_size", "18")]
    [InlineData("PanelCard/styles/panel", "ExtResource(\"25_panel_card\")")]
    [InlineData("OverlayPanel/styles/panel", "ExtResource(\"26_panel_elevated\")")]
    [InlineData("PanelContainer/styles/panel", "ExtResource(\"25_panel_card\")")]
    public void ScreenVariations_AreUnchangedByTheHudProfile(string key, string expected)
    {
        Dictionary<string, string> theme = ReadTheme();

        Assert.True(theme.TryGetValue(key, out string? actual), $"{key} disappeared from default_theme.tres.");
        Assert.Equal(expected, actual);
    }

    private static Dictionary<string, string> ReadTheme()
    {
        var entries = new Dictionary<string, string>(StringComparer.Ordinal);
        bool inResource = false;
        foreach (string line in File.ReadAllLines(ResolveThemePath()))
        {
            if (line.StartsWith("[resource]", StringComparison.Ordinal)) { inResource = true; continue; }
            if (!inResource) continue;

            Match match = EntryPattern.Match(line);
            if (match.Success) entries[match.Groups[1].Value] = match.Groups[2].Value.Trim();
        }
        Assert.NotEmpty(entries);
        return entries;
    }

    private static Dictionary<string, string> ReadExternalResources()
    {
        var resources = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string line in File.ReadAllLines(ResolveThemePath()))
        {
            Match match = ExternalResourcePattern.Match(line);
            if (match.Success) resources[match.Groups[2].Value] = match.Groups[1].Value;
        }
        return resources;
    }

    private static string GameRoot(string themeDirectory)
    {
        var directory = new DirectoryInfo(themeDirectory);
        while (directory is not null && !string.Equals(directory.Name, "game", StringComparison.OrdinalIgnoreCase))
        {
            directory = directory.Parent;
        }
        Assert.NotNull(directory);
        return directory!.FullName;
    }

    private static string ResolveThemePath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            string candidate = Path.Combine(
                directory.FullName, "game", "assets", "ui", "default_theme.tres");
            if (File.Exists(candidate)) return candidate;
            directory = directory.Parent;
        }
        throw new FileNotFoundException("Could not locate default_theme.tres.");
    }

    private static string ResolveFontPath(string fontReference, Dictionary<string, string> resources)
    {
        Match match = ExtResourceReferencePattern.Match(fontReference);
        Assert.True(match.Success, $"{fontReference} is not an ExtResource reference.");

        string id = match.Groups[1].Value;
        Assert.True(
            resources.TryGetValue(id, out string? resourcePath),
            $"ExtResource {id} is used by the theme but never declared in it.");

        string relative = resourcePath!.Replace("res://", string.Empty).Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(GameRoot(Path.GetDirectoryName(ResolveThemePath())!), relative);
    }

    /// <summary>
    /// Cap height as a fraction of the em, read out of the font's own tables.
    /// </summary>
    /// <remarks>
    /// Reading it beats restating it: the six families range from Micro 5 at
    /// 0.4545 em to Jacquarda Bastarda 9 at 0.6923 em, so a hard-coded table would
    /// be six numbers that no longer describe the files the moment one is swapped.
    /// Only <c>head</c> (units per em) and <c>OS/2</c> (cap height) are needed, and
    /// both sit at fixed offsets, so this stays a few lines rather than a parser.
    /// </remarks>
    private static double ReadCapRatio(string fontPath)
    {
        byte[] data = File.ReadAllBytes(fontPath);
        var tables = new Dictionary<string, int>(StringComparer.Ordinal);
        int tableCount = ReadUInt16(data, 4);
        for (int i = 0; i < tableCount; i++)
        {
            int entry = 12 + (16 * i);
            tables[Encoding.ASCII.GetString(data, entry, 4)] = ReadInt32(data, entry + 8);
        }

        int unitsPerEm = ReadUInt16(data, tables["head"] + 18);
        int os2 = tables["OS/2"];
        Assert.True(
            ReadUInt16(data, os2) >= 2,
            $"{Path.GetFileName(fontPath)} carries an OS/2 table older than version 2, which has no cap height.");

        return (double)ReadInt16(data, os2 + 88) / unitsPerEm;
    }

    private static int ReadUInt16(byte[] data, int offset) => (data[offset] << 8) | data[offset + 1];

    private static int ReadInt16(byte[] data, int offset)
    {
        int value = ReadUInt16(data, offset);
        return value >= 0x8000 ? value - 0x10000 : value;
    }

    private static int ReadInt32(byte[] data, int offset) =>
        (data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3];

    private static readonly Regex ExtResourceReferencePattern = new(
        @"^ExtResource\(""([^""]+)""\)$",
        RegexOptions.CultureInvariant);

    private static readonly Regex EntryPattern = new(
        @"^([A-Za-z0-9_]+(?:/[A-Za-z0-9_]+)+)\s*=\s*(.+)$",
        RegexOptions.CultureInvariant);

    private static readonly Regex ExternalResourcePattern = new(
        @"^\[ext_resource[^\]]*path=""([^""]+)""[^\]]*id=""([^""]+)""",
        RegexOptions.CultureInvariant);
}
