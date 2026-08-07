#nullable enable
using System;
using System.Collections.Generic;
using Godot;
using WorldofGoses.Domain;

namespace WorldofGoses;

/// <summary>
/// Centralised registry of per-lineage visual assets. Resolves a
/// (lineage, component) pair to a <see cref="StyleBox"/> that
/// nodes can apply via <c>AddThemeStyleboxOverride</c> or a custom
/// property. The registry does not recolour or rebuild anything: it
/// loads the <c>.stylebox.tres</c> files exported by Sixteen Pixel
/// Perfect and applies an explicit fallback chain.
/// </summary>
public static class LineageThemeRegistry
{
    /// <summary>Folder under res:// that holds the per-lineage assets.</summary>
    public const string LineagesFolder = "res://assets/ui/lineages/";

    public const string SystemDefaultLineage = "default";

    /// <summary>Single component currently bundled in the lineage packs.</summary>
    public const string ComponentPanel = "panel";

    /// <summary>Fallback component used when the requested one is missing.</summary>
    public const string ComponentDefault = "panel";

    public const string ComponentPanelInset = "panel_inset";
    public const string ComponentButton = "button";
    public const string ComponentButtonPrimary = "button_primary";
    public const string ComponentButtonSecondary = "button_secondary";
    public const string ComponentTooltip = "tooltip";
    public const string ComponentModal = "modal";
    public const string ComponentStatusBar = "status_bar";
    public const string ComponentSidebar = "sidebar";
    public const string ComponentTab = "tab";
    public const string ComponentResourceChip = "resource_chip";
    public const string ComponentProgressBar = "progress_bar";
    public const string ComponentPortraitFrame = "portrait_frame";
    public const string ComponentIconContainer = "icon_container";
    public const string ComponentSelectionFrame = "selection_frame";
    public const string ComponentDivider = "divider";

    /// <summary>
    /// Neutral chrome used whenever no lineage skin applies. This used to be
    /// the yellow Kenney 9-slice, which made it the single most visible
    /// surface in the game for entirely accidental reasons: the active lineage
    /// starts as <see cref="SystemDefaultLineage"/>, that id was never a key of
    /// <see cref="StyleboxByLineage"/>, so every panel resolved through this
    /// path until a hero existed — the whole onboarding, the first night and
    /// the pre-hero macro view. Worse, most consumers apply the stylebox once
    /// in <c>_Ready</c> and never refresh, so they stayed yellow for the rest
    /// of the session. The neutral surface is now slate, and
    /// <see cref="SystemDefaultLineage"/> is an explicit entry below rather
    /// than an accidental miss.
    /// </summary>
    public const string DefaultPanelStyleboxPath =
        "res://assets/ui/kenney-pixel-adventure/9-slice/slate_raised_dark.tres";

    private static readonly Dictionary<string, string> StyleboxByLineage = new(StringComparer.Ordinal)
    {
        ["ardhen"] = "res://assets/ui/lineages/ardhen/panel/panel.stylebox.tres",
        ["eirune"] = "res://assets/ui/lineages/eirune/panel/panel.stylebox.tres",
        ["kovari"] = "res://assets/ui/lineages/kovari/panel/panel.stylebox.tres",
        ["myrven"] = "res://assets/ui/lineages/myrven/panel/panel.stylebox.tres",
        ["vaelun"] = "res://assets/ui/lineages/vaelun/panel/panel.stylebox.tres",
        ["orveth"] = "res://assets/ui/lineages/orveth/panel/panel.stylebox.tres",
        ["caelith"] = "res://assets/ui/lineages/caelith/panel/panel.stylebox.tres",
        ["theryn"] = "res://assets/ui/lineages/theryn/panel/panel.stylebox.tres",
    };

    /// <summary>
    /// Icon accent colour per lineage. Returned to <c>CanvasItem.Modulate</c>
    /// so white-filled SVG icons take the linaje's tone without changing
    /// geometry. Keep these muted so they read as accents rather than
    /// full backgrounds; saturation is intentional. The fallback
    /// matches the project default cream so unknown lineages never
    /// produce a black icon.
    /// </summary>
    public static readonly Color DefaultIconAccent = new(0.95f, 0.92f, 0.83f);

    private static readonly Dictionary<string, Color> IconAccentByLineage = new(StringComparer.Ordinal)
    {
        // Copper for Ardhen (memory, effort, repair). Pulled to ~20° hue so it
        // reads as copper rather than as a third amber: Ardhen, Orveth and
        // Vaelun previously sat inside a 10° band (34°/44°/42°), with Orveth
        // and Vaelun only 2° apart, and no viewer could tell whose accent
        // they were looking at. The three now occupy copper / gold / khaki.
        ["ardhen"] = new(0.69f, 0.40f, 0.25f),
        // Soft teal for Eirune (water, growth, symbiosis).
        ["eirune"] = new(0.31f, 0.62f, 0.56f),
        // Blue-grey for Kovari (modular, mechanical, repair).
        ["kovari"] = new(0.42f, 0.54f, 0.68f),
        // Muted purple for Myrven (layers, performance, mediation).
        ["myrven"] = new(0.54f, 0.42f, 0.65f),
        // Sand and khaki for Vaelun (route, signal, refuge). Pushed to ~62°,
        // the olive end of its own description, and kept low-saturation so it
        // reads dusty next to Orveth's rich gold.
        ["vaelun"] = new(0.71f, 0.72f, 0.42f),
        // Muted gold for Orveth (contract, reserve, exchange). Held at ~45°
        // and made the purest of the three: Orveth is the gold lineage, so it
        // keeps the hue while its neighbours move away from it.
        ["orveth"] = new(0.81f, 0.66f, 0.19f),
        // Pale blue for Caelith (node, synthesis, diagnosis).
        ["caelith"] = new(0.48f, 0.72f, 0.85f),
        // Soft red for Theryn (pulse, empathy, ceremony).
        ["theryn"] = new(0.77f, 0.42f, 0.42f),
    };

    /// <summary>Returns the icon accent colour for the active lineage.</summary>
    public static Color IconAccent
    {
        get
        {
            if (IconAccentByLineage.TryGetValue(_activeLineage, out var accent)) return accent;
            return DefaultIconAccent;
        }
    }

    /// <summary>Variant of <see cref="IconAccent"/> that pins the lineage explicitly.</summary>
    public static Color GetIconAccent(string lineage)
    {
        string normalised = (lineage ?? string.Empty).ToLowerInvariant();
        if (IconAccentByLineage.TryGetValue(normalised, out var accent)) return accent;
        return DefaultIconAccent;
    }

    private static readonly Dictionary<string, StyleBoxTexture> Cache = new(StringComparer.Ordinal);

    private static string _activeLineage = SystemDefaultLineage;

    /// <summary>Fired whenever <see cref="SetActiveLineage"/> successfully changes the active lineage.</summary>
    public static event Action<string>? ActiveLineageChanged;

    /// <summary>Forwarded by the Godot autoload to the development log.</summary>
    public static event Action<string>? FallbackUsed;

    public static IReadOnlyCollection<string> AvailableLineages => StyleboxByLineage.Keys;

    public static string ActiveLineage
    {
        get => _activeLineage;
        set => SetActiveLineage(value);
    }

    /// <summary>Returns the canonical, lower-case identifier for a domain <see cref="LineageId"/>.</summary>
    public static string IdOf(LineageId lineage) => lineage.Value.ToLowerInvariant();

    /// <summary>
    /// Sets the active lineage. Invalid identifiers fall back to
    /// the system default and emit a single dev warning. The cache is
    /// preserved so re-selecting a lineage is allocation-free.
    /// </summary>
    public static void SetActiveLineage(string lineage)
    {
        string normalised = (lineage ?? string.Empty).ToLowerInvariant();
        if (!StyleboxByLineage.ContainsKey(normalised))
        {
            FallbackUsed?.Invoke(
                $"LineageThemeRegistry: unknown lineage '{lineage}', using the project default theme.");
            normalised = SystemDefaultLineage;
        }
        if (normalised == _activeLineage) return;
        _activeLineage = normalised;
        ActiveLineageChanged?.Invoke(normalised);
    }

    /// <summary>
    /// Returns a <see cref="StyleBox"/> for the requested component
    /// under the active lineage. The fallback chain is:
    /// 1) exact match in the active lineage;
    /// 2) other components in the active lineage (panel fallback);
    /// 3) same component in the system default lineage.
    /// 4) system default panel.
    /// </summary>
    public static StyleBox? TryGetStyleBox(string componentId) =>
        TryGetStyleBox(_activeLineage, componentId);

    /// <summary>Variant of <see cref="TryGetStyleBox(string)"/> that pins the lineage explicitly.</summary>
    public static StyleBox? TryGetStyleBox(string lineage, string componentId)
    {
        string requestedLineage = (lineage ?? string.Empty).ToLowerInvariant();
        string component = string.IsNullOrEmpty(componentId) ? ComponentDefault : componentId.ToLowerInvariant();

        // The system default is not a lineage — it must never appear in
        // AvailableLineages — but it is a legitimate, very common request:
        // every surface built before a hero exists asks for it. Resolve it
        // explicitly to the neutral chrome instead of letting it miss the
        // dictionary and fall through to LoadDefault by accident.
        if (requestedLineage == SystemDefaultLineage)
        {
            return TryLoad(requestedLineage, DefaultPanelStyleboxPath, out StyleBoxTexture? neutral)
                ? neutral
                : null;
        }

        if (StyleboxByLineage.TryGetValue(requestedLineage, out string? exactPath) && component == ComponentPanel)
        {
            if (TryLoad(requestedLineage, exactPath, out StyleBoxTexture? exact)) return exact;
        }

        if (StyleboxByLineage.TryGetValue(requestedLineage, out string? fallbackPath))
        {
            if (TryLoad(requestedLineage, fallbackPath, out StyleBoxTexture? fallback))
            {
                if (component != ComponentPanel)
                {
                    FallbackUsed?.Invoke(
                        $"LineageThemeRegistry: '{requestedLineage}' has no '{componentId}', using '{ComponentPanel}'.");
                }
                return fallback;
            }
        }

        if (component != ComponentPanel)
        {
            FallbackUsed?.Invoke(
                $"LineageThemeRegistry: '{requestedLineage}' has no usable assets for '{componentId}', using the project default.");
        }
        return null;
    }

    /// <summary>Defaulted variant: returns the system default panel when nothing in the requested lineage matches.</summary>
    public static StyleBox GetStyleBox(string componentId) =>
        TryGetStyleBox(componentId) ?? LoadDefault();

    /// <summary>Defaulted variant: lineage explicit.</summary>
    public static StyleBox GetStyleBox(string lineage, string componentId) =>
        TryGetStyleBox(lineage, componentId) ?? LoadDefault();

    public static bool HasLineage(string lineage) =>
        StyleboxByLineage.ContainsKey((lineage ?? string.Empty).ToLowerInvariant());

    public static string GetStyleboxPath(string lineage, string component)
    {
        string requestedLineage = (lineage ?? string.Empty).ToLowerInvariant();
        string requestedComponent = (component ?? string.Empty).ToLowerInvariant();
        return StyleboxByLineage.TryGetValue(requestedLineage, out string? path)
            && requestedComponent == ComponentPanel
                ? path
                : StyleboxByLineage.TryGetValue(requestedLineage, out path)
                    ? path
                    : DefaultPanelStyleboxPath;
    }

    private static bool TryLoad(string lineage, string path, out StyleBoxTexture? stylebox)
    {
        string cacheKey = lineage + "::" + path;
        if (Cache.TryGetValue(cacheKey, out StyleBoxTexture? cached))
        {
            stylebox = cached;
            return true;
        }
        var resource = ResourceLoader.Load<StyleBoxTexture>(path);
        if (resource is null)
        {
            stylebox = null;
            return false;
        }
        Cache[cacheKey] = resource;
        stylebox = resource;
        return true;
    }

    private static StyleBoxTexture LoadDefault()
    {
        if (Cache.TryGetValue("__default::" + DefaultPanelStyleboxPath, out StyleBoxTexture? cached)) return cached;
        var resource = ResourceLoader.Load<StyleBoxTexture>(DefaultPanelStyleboxPath);
        if (resource is null)
        {
            throw new InvalidOperationException(
                $"LineageThemeRegistry: default panel stylebox missing at {DefaultPanelStyleboxPath}.");
        }
        Cache["__default::" + DefaultPanelStyleboxPath] = resource;
        return resource;
    }

    /// <summary>Test-only helper that clears the resource cache. Production code should not call this.</summary>
    internal static void ClearCache() => Cache.Clear();
}
