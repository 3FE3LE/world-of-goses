using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    private const string Geist = "1_geist";
    private const string Jersey = "2_jersey";
    private const string Pixelify = "3_pixelify";

    public static TheoryData<string, string, int> HudTextVariations() => new()
    {
        { "HudBrand", Geist, 20 },
        { "HudHeader", Jersey, 18 },
        { "HudLabel", Jersey, 16 },
        { "HudBody", Pixelify, 16 },
        { "HudNumeric", Pixelify, 16 },
        { "HudCaption", Pixelify, 14 },
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

    [Fact]
    public void HudTypography_StaysInsideTheThreeProjectFamilies()
    {
        Dictionary<string, string> theme = ReadTheme();

        string[] allowed = { $"ExtResource(\"{Geist}\")", $"ExtResource(\"{Jersey}\")", $"ExtResource(\"{Pixelify}\")" };
        foreach ((string key, string value) in theme.Where(e => e.Key.StartsWith("Hud", StringComparison.Ordinal)
                                                               && e.Key.EndsWith("/fonts/font", StringComparison.Ordinal)))
        {
            Assert.True(
                allowed.Contains(value),
                $"{key} resolves to {value}. The project ships exactly three families "
                + "(UI_PATTERNS.md §5); a fourth cannot arrive through the HUD.");
        }
    }

    [Fact]
    public void HudTypography_NeverFallsBelowFourteen()
    {
        Dictionary<string, string> theme = ReadTheme();

        foreach ((string key, string value) in theme.Where(e => e.Key.StartsWith("Hud", StringComparison.Ordinal)
                                                               && e.Key.EndsWith("/font_sizes/font_size", StringComparison.Ordinal)))
        {
            int size = int.Parse(value);
            Assert.True(
                size >= 14,
                $"{key} is {size}. Fourteen is the floor this profile was signed off at, "
                + "read in a real 1280x720 capture; anything smaller needs its own capture "
                + "and its own sign-off, not a quiet edit.");
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

    private static readonly Regex EntryPattern = new(
        @"^([A-Za-z0-9_]+(?:/[A-Za-z0-9_]+)+)\s*=\s*(.+)$",
        RegexOptions.CultureInvariant);

    private static readonly Regex ExternalResourcePattern = new(
        @"^\[ext_resource[^\]]*path=""([^""]+)""[^\]]*id=""([^""]+)""",
        RegexOptions.CultureInvariant);
}
