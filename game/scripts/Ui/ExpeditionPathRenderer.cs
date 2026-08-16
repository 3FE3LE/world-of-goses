#nullable enable
using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Prototypes;

namespace WorldofGoses.Ui;

/// <summary>
/// Pseudo-3D depth-band grammar for the expedition path. Built as a
/// presentation helper (not a Godot <see cref="Node"/>): the owning
/// <c>ExpeditionStage</c> asks this class which rows exist, which one
/// is playable, and how fast each plane slides, then hands the answers
/// to <see cref="SharedDepthBands"/> to paint.
///
/// <para>
/// The path stage shares the macro's projection vocabulary without
/// inheriting any urban semantics: there is no parcel, no tree, no
/// building, no plot, no territory tint and no road occupancy here.
/// </para>
///
/// <para>
/// <b>One playable band.</b> Party, enemies, the objective and the
/// path tile all resolve their row through <see cref="PlayableDepth"/>
/// and <see cref="IsPlayableDepth"/>. There was briefly a second,
/// unwritten rule — the terrain marked <c>depth == RowCount - 1</c> as
/// the path — which put the worn-footprint tile on the row nearest the
/// horizon while everyone walked on the row nearest the camera. Two
/// authorities for one band is how that happens, so there is now one,
/// and callers are expected to ask rather than to know (#27).
/// </para>
/// </summary>
public static class ExpeditionPathRenderer
{
    /// <summary>
    /// How many ground rows the stage paints: one whole parcel, deep.
    /// </summary>
    /// <remarks>
    /// A parcel is <c>LotsPerAxis</c> lots of <c>TilesPerStandardLot</c> tiles,
    /// so nine rows — the same block the macro city is built from. It was six,
    /// a number with no relationship to anything, which is part of why the path
    /// and the city read as different places rather than the same world seen
    /// from a different distance.
    /// </remarks>
    public const int RowCount = ParcelGrid.LotsPerAxis * ParcelGrid.TilesPerStandardLot;

    /// <summary>Step used by the pixel staircase trapezoid.</summary>
    public const float PixelStep = 2f;

    /// <summary>
    /// The one band gameplay stands on: the calle in front of the parcel's
    /// second row of lots.
    /// </summary>
    /// <remarks>
    /// Row 3 — the first tile of lot row 1 — because that is what a calle is in
    /// the macro: <c>MacroStreetRenderer.DrawTiledFloor</c> paints three tile
    /// rows per lot row and only row 0 of each is walkable, the band that wears
    /// into a path. Rows 0-2 are therefore the lot row in front of the party,
    /// 4-5 the depth of its own lot, and 6-8 the lot row behind. Larger depths
    /// converge upward toward the horizon.
    /// </remarks>
    public const float PlayableDepth = ParcelGrid.TilesPerStandardLot;

    /// <summary>
    /// The fringe in front of the party. Negative depth is nearer than
    /// the playable band, which is what puts it lower on screen and
    /// draws it larger.
    /// </summary>
    public const float ForegroundDepth = -1f;

    /// <summary>
    /// Screen pixels per world unit, normalised so the playable band draws 1:1.
    /// </summary>
    /// <remarks>
    /// It was the literal <c>1f</c>, which happened to make the band 1:1 only
    /// because the band was depth 0 and depth 0 has a horizontal scale of one.
    /// Moving the band back to the calle at row 3 dropped its scale to 0.88³,
    /// which would have narrowed the combat arena by a third and made the same
    /// travel read a third slower — a perspective change smuggled in as a
    /// geometry change. Dividing it out keeps the band's own spread exactly
    /// what it was and leaves the rows in front larger and those behind
    /// smaller, which is the whole point of moving it.
    /// </remarks>
    public static readonly float PixelsPerUnit =
        1f / StreetDepthProjection.HorizontalScale(PlayableDepth);

    /// <summary>Whether <paramref name="depth"/> is the playable band.
    /// The single question every consumer asks; nobody re-derives the
    /// answer from a row index.</summary>
    public static bool IsPlayableDepth(float depth) =>
        Mathf.IsEqualApprox(depth, PlayableDepth);

    /// <summary>Which plane a ground row belongs to.</summary>
    public static ExpeditionPathLayer LayerForDepth(float depth)
    {
        if (depth < PlayableDepth) return ExpeditionPathLayer.Foreground;
        if (IsPlayableDepth(depth)) return ExpeditionPathLayer.Playable;
        // The lot row behind the party's own is decoration and moves as a
        // backdrop; the two rows between are still its lot's depth. Derived
        // from the parcel rather than from `RowCount - 2`, so the boundary
        // stays on a lot edge if the parcel ever changes shape.
        return depth >= PlayableDepth + ParcelGrid.TilesPerStandardLot
            ? ExpeditionPathLayer.Distance
            : ExpeditionPathLayer.Rear;
    }

    /// <summary>
    /// How fast a plane slides against the world offset.
    ///
    /// <para>Ground rows use their own perspective compression: a row
    /// farther from the camera covers more world per pixel, so it
    /// moves slower without anyone tuning a number. The backdrop and
    /// the fringe own no world coordinate, so they take the authored
    /// factors from <see cref="ExpeditionPathParallax"/> instead.</para>
    /// </summary>
    public static float ParallaxFactorForDepth(float depth)
    {
        // Every row of the parcel owns a world coordinate, so every one of them
        // takes its own perspective compression and recedes at its own rate.
        // Only the fringe in front of it does not exist in the world at all,
        // and only that one takes an authored factor.
        //
        // This used to key off the layer, which was harmless while a single
        // row sat in front of the playable band. With the band moved back to
        // the calle — nine parcel rows, playable at 3 — rows 0-2 all became
        // Foreground and would have slid at one flat authored speed, so three
        // receding rows would have moved as one flat card.
        if (depth < 0f) return ExpeditionPathParallax.ForegroundFactor;
        return StreetDepthProjection.HorizontalScale(depth);
    }

    /// <summary>
    /// Computes the screen Y of a depth-band row inside this stage.
    /// Forwarded to <see cref="StreetDepthProjection"/> so the
    /// formula lives in one place; the only domain knowledge here
    /// is which base Y to anchor the horizon to.
    /// </summary>
    public static float RowScreenY(float depth, in ExpeditionPathAnchor anchor) =>
        StreetDepthProjection.RowScreenY(
            depth, anchor.BaseY, anchor.HorizonY, anchor.RowSpacingPx);

    /// <summary>Screen Y of the playable band — the row party, enemies
    /// and the objective all stand on.</summary>
    public static float PlayableScreenY(in ExpeditionPathAnchor anchor) =>
        RowScreenY(PlayableDepth, anchor);

    /// <summary>Horizontal scale on the playable band.</summary>
    public static float PlayableHorizontalScale() =>
        StreetDepthProjection.HorizontalScale(PlayableDepth);

    /// <summary>Vertical scale on the playable band.</summary>
    public static float PlayableVerticalScale() =>
        StreetDepthProjection.VerticalScale(PlayableDepth);

    /// <summary>
    /// Whether the row at <paramref name="depth"/> lives inside the
    /// stage window, fringe included.
    /// </summary>
    public static bool IsRowVisible(float depth) =>
        depth >= ForegroundDepth && depth < RowCount;

    /// <summary>
    /// Projects an authoritative one-dimensional world X onto the
    /// screen, for a plane moving at <paramref name="parallaxFactor"/>.
    ///
    /// <para>This is the only world-to-screen rule on the path. The
    /// travel <c>Travel.PositionX</c> and combat
    /// <see cref="Domain.Combat.CombatParticipantState.PositionX"/>
    /// remain the sole authority for where anything *is*; this decides
    /// only where that is drawn, and it decides it the same way for
    /// terrain, props, combatants and the objective — which is what
    /// keeps them standing on the same ground.</para>
    /// </summary>
    public static float WorldToScreenX(
        double worldX,
        long worldOffsetUnits,
        float parallaxFactor,
        in ExpeditionPathAnchor anchor) =>
        anchor.CenterX
        + ((float)worldX * PixelsPerUnit * parallaxFactor)
        - (ExpeditionPathParallax.LayerOffset(worldOffsetUnits, parallaxFactor) * PixelsPerUnit);

    /// <summary>Screen X of a world coordinate on the playable band.
    /// The overload every gameplay consumer wants.</summary>
    public static float PlayableScreenX(
        double worldX,
        long worldOffsetUnits,
        in ExpeditionPathAnchor anchor) =>
        WorldToScreenX(
            worldX,
            worldOffsetUnits,
            ParallaxFactorForDepth(PlayableDepth),
            anchor);
}
