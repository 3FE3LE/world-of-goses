#nullable enable

using System.Linq;
using Godot;

namespace WorldofGoses.Ui;

/// <summary>
/// Writes the active lineage's panel surface into the project theme, so lineage
/// identity arrives through the theme instead of around it.
/// </summary>
/// <remarks>
/// <para>
/// Until now every panel that wanted lineage chrome called
/// <c>AddThemeStyleboxOverride("panel", LineageThemeRegistry.GetStyleBox(...))</c>
/// on itself — fourteen callsites doing the same thing. That had three costs. A
/// local override beats the theme, so `default_theme.tres` was not actually the
/// visual authority it claims to be. Most consumers applied the override once in
/// <c>_Ready</c> and never again, so changing lineage mid-session left them showing
/// the previous one. And it was fourteen chances to do it slightly differently.
/// </para>
/// <para>
/// The reason they all had to override is duller than it looks: the theme
/// registers <c>Panel</c> — the <see cref="Godot.Panel"/> control — but never
/// registered <c>PanelContainer</c>, which is what these surfaces actually are. A
/// bare <c>PanelContainer</c> therefore fell through to the engine's own grey
/// stylebox, and overriding was the only way to look like the project at all. The
/// theme now registers it, and this painter repaints that one entry when the
/// lineage changes.
/// </para>
/// <para>
/// Content margins are normalised to the neutral surface's values rather than
/// inherited from the lineage asset, which carries 8/7. Lineage themes may change
/// palette, borders, corners and fills; the invariants forbid them changing
/// minimum sizes or hierarchy, and padding is layout. So the frame changes and the
/// metrics do not.
/// </para>
/// </remarks>
public static class LineageThemePainter
{
    /// <summary>Content padding of the neutral card surface, in `default_theme.tres`.</summary>
    private const float CardMarginHorizontal = 14f;
    private const float CardMarginVertical = 12f;

    /// <summary>Theme entries repainted per lineage. All are card-weight surfaces.</summary>
    private static readonly string[] RepaintedTypes = { "PanelContainer", "Panel", "PanelCard" };

    private static StyleBox? _neutral;

    /// <summary>
    /// Repaints the project theme for <paramref name="lineage"/>. Safe to call
    /// before any lineage is chosen: the registry resolves the neutral surface.
    /// </summary>
    public static void Repaint(string lineage)
    {
        Theme? theme = ThemeDB.Singleton?.GetProjectTheme();
        if (theme is null) return;

        // Remember the authored neutral surface the first time through, so leaving
        // a lineage restores the composite rather than whatever the last lineage
        // left behind.
        _neutral ??= theme.GetStylebox("panel", "PanelCard");

        StyleBox? source = _neutral;

        // Only the eight real lineages get a lineage frame. Asking the registry
        // for anything else returns its *fallback*, which is
        // `slate_raised_dark` -- the raised button texture, chosen as a
        // last-resort so a surface is never unstyled. It is not a card surface,
        // and painting it over `PanelCard` replaces the authored composite with a
        // mid-tone slate, quietly undoing the panel chrome for the default
        // lineage. Measured, not guessed: it moved the rail's frame from
        // (158,135,92) to (161,192,202).
        if (LineageThemeRegistry.AvailableLineages.Contains(lineage))
        {
            source = LineageThemeRegistry.TryGetStyleBox(lineage, LineageThemeRegistry.ComponentPanel)
                     ?? _neutral;
        }
        if (source is null) return;

        StyleBox surface = (StyleBox)source.Duplicate();
        surface.ContentMarginLeft = CardMarginHorizontal;
        surface.ContentMarginRight = CardMarginHorizontal;
        surface.ContentMarginTop = CardMarginVertical;
        surface.ContentMarginBottom = CardMarginVertical;

        foreach (string type in RepaintedTypes)
        {
            theme.SetStylebox("panel", type, surface);
        }
    }
}
