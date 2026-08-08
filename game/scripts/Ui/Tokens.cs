#nullable enable

namespace WorldofGoses.Ui;

/// <summary>
/// The spacing and control-size vocabulary the UI is built from.
/// </summary>
/// <remarks>
/// <para>
/// Typography is fully centralised: every Label and Button names a
/// <c>default_theme.tres</c> variation, and the repository contains zero
/// <c>AddThemeFontOverride</c> or <c>AddThemeFontSizeOverride</c> calls. Spacing
/// is the opposite — 84 <c>AddThemeConstantOverride</c> calls across 24 files,
/// almost all of them <c>separation</c> or <c>margin_*</c>, each picking its own
/// number. This type is the missing half: one named scale, so a panel asks for
/// <see cref="SpacingBase"/> rather than re-deciding what 8 means.
/// </para>
/// <para>
/// These are the values already in the code, named — not new values. Renumbering
/// spacing moves layout metrics, which needs a visual-regression pass; naming it
/// does not. Grow this type when a constant gains a second consumer, and not
/// before: a token nothing reads is the speculative abstraction
/// <c>ARCHITECTURE.md</c> §12 rules out.
/// </para>
/// </remarks>
public static class Tokens
{
    /// <summary>Gap inside a compact pairing, such as an icon and its label.</summary>
    public const int SpacingTight = 4;

    /// <summary>The default gap between related controls.</summary>
    public const int SpacingBase = 8;

    /// <summary>Gap between the independent groups on one row, e.g. HUD chips.</summary>
    public const int SpacingLoose = 18;

    /// <summary>
    /// Edge length of an inline icon that sits inside a line of text. Smaller
    /// than the 24 px icons in <see cref="IconPaths"/>, which are sized for
    /// buttons; a status chip's glyph reads next to 16 px type.
    /// </summary>
    public const int IconInline = 12;

    /// <summary>Height of a status chip, sized so a 16 px label clears its box.</summary>
    public const int ChipHeight = 20;

    /// <summary>
    /// Height of an interactive control. Shared with
    /// <see cref="ActionButton.DefaultHeight"/> so the two cannot drift.
    /// <c>StandardButtons</c> still hardcodes 44 and some scenes 36 or 38;
    /// converging them is a metric change and belongs to its own pass.
    /// </summary>
    public const int ControlHeight = 40;

    /// <summary>
    /// Width of the navigation rail. One value, not a compact/expanded pair.
    /// </summary>
    /// <remarks>
    /// A rail that widens at higher resolutions cannot work in this project.
    /// <c>project.godot</c> uses <c>stretch/mode=canvas_items</c> on a 16:9 base of
    /// 1280x720, so 1920x1080 is the *same* logical viewport drawn at 1.5x, not
    /// more space: <c>GetVisibleRect().Size.X</c> reads 1280 at both official review
    /// sizes. There is no extra room to expand into, and keying off the window size
    /// instead would shrink the UI relative to the world — which contradicts the
    /// uniform integer-ish scaling the presentation invariants ask for.
    /// </remarks>
    public const int RailWidth = 56;
}
