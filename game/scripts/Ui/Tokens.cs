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

    /// <summary>Gap inside a dense list or a stack of short rows.</summary>
    public const int SpacingRelaxed = 10;

    /// <summary>Gap between the rows of a panel's body.</summary>
    public const int SpacingComfortable = 12;

    /// <summary>Inset of a panel's content from its frame.</summary>
    public const int SpacingWide = 16;

    /// <summary>Gap between the sections of a screen, and their vertical inset.</summary>
    public const int SpacingSection = 20;

    /// <summary>Horizontal inset of a full screen's content.</summary>
    public const int SpacingBlock = 24;

    /// <summary>Gap between the independent groups on one row, e.g. HUD chips.</summary>
    public const int SpacingLoose = 18;

    // NOTE: the values above name what the code already uses; they are not yet a
    // rhythm. A survey of the 71 literal `AddThemeConstantOverride` calls found
    // 2, 4, 6, 8, 10, 12, 16, 18, 20, 22, 24 and 28 — a near-continuous spread
    // rather than a scale, with 18 sitting awkwardly between 16 and 20. Naming
    // them is safe and makes a future re-scale one edit per token. Collapsing
    // them onto a single step moves layout metrics on surfaces that no
    // visual-regression fixture renders, so it belongs to its own pass with the
    // component showcase open.

    /// <summary>
    /// Edge length of an inline icon, and of the cell reserved for it.
    /// </summary>
    /// <remarks>
    /// This is the icons' real imported size: `svg/scale=1.0` on a 24×24 source.
    /// It was 12, which reserved half the space the glyph actually occupied —
    /// <see cref="Godot.TextureRect.StretchModeEnum.Keep"/> draws a texture at its
    /// natural size no matter how small the rect is, so every status chip's icon
    /// overflowed right into its own label and down into the chip below. The
    /// alternative, scaling 24 px pixel art to 12, is a 0.5× downscale that eats
    /// one-pixel strokes; reserving the true size costs 12 px of width per chip and
    /// keeps the glyphs intact.
    /// </remarks>
    public const int IconInline = 24;

    /// <summary>
    /// Height of a status chip: the icon's own height, which a 16 px label clears.
    /// Matches the 28 px status strip once its 2 px content margins are added.
    /// </summary>
    public const int ChipHeight = 24;

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
