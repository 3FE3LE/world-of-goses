#nullable enable
using Godot;
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
    /// <summary>How many ground rows the stage paints behind and on
    /// the playable band.</summary>
    public const int RowCount = 6;

    /// <summary>Step used by the pixel staircase trapezoid.</summary>
    public const float PixelStep = 2f;

    /// <summary>
    /// The one band gameplay stands on. Depth 0 is the row nearest the
    /// camera: <see cref="StreetDepthProjection.RowScreenY"/> anchors
    /// depth 0 at the base and lets larger depths converge upward
    /// toward the horizon.
    /// </summary>
    public const float PlayableDepth = 0f;

    /// <summary>
    /// The fringe in front of the party. Negative depth is nearer than
    /// the playable band, which is what puts it lower on screen and
    /// draws it larger.
    /// </summary>
    public const float ForegroundDepth = -1f;

    /// <summary>How many world units one pixel of the playable band
    /// covers. One, so the combat arena keeps the spread it had when
    /// the stage normalised the battlefield onto the band.</summary>
    public const float PixelsPerUnit = 1f;

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
        return depth >= RowCount - 2 ? ExpeditionPathLayer.Distance : ExpeditionPathLayer.Rear;
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
    public static float ParallaxFactorForDepth(float depth) =>
        LayerForDepth(depth) switch
        {
            ExpeditionPathLayer.Distance => ExpeditionPathParallax.DistanceFactor,
            ExpeditionPathLayer.Foreground => ExpeditionPathParallax.ForegroundFactor,
            _ => StreetDepthProjection.HorizontalScale(depth),
        };

    /// <summary>
    /// Computes the screen Y of a depth-band row inside this stage.
    /// Forwarded to <see cref="StreetDepthProjection"/> so the
    /// formula lives in one place; the only domain knowledge here
    /// is which base Y to anchor the horizon to.
    /// </summary>
    public static float RowScreenY(float depth, in ExpeditionPathAnchor anchor) =>
        StreetDepthProjection.RowScreenY(depth, anchor.BaseY);

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
