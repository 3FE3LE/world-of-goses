#nullable enable
using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Ui;

namespace WorldofGoses.Prototypes;

/// <summary>
/// Pure projection helpers for the macro street view (A4). Every collaborator
/// composing <see cref="MacroStreetLiveView"/> — the camera, the journey
/// presenter, the placement presenter, the renderer — reads from here so
/// the projection formula lives in one place.
///
/// <see cref="MacroStreetLiveView"/> keeps one-line forwarders so the
/// existing in-class usage (<c>ProjectedRowScreenY(d)</c> with the static
/// <c>BaseY</c>) and the existing test surface keep compiling unchanged.
/// </summary>
internal static class MacroProjectionHelpers
{
    /// <summary>Whether a relative depth is inside the visible thirteen-street
    /// window (the two foreground streets, the focused street, and ten
    /// receding streets).</summary>
    public static bool IsProjectedDepthVisible(float relativeDepth) =>
        StreetDepthProjection.IsVisibleDepth(relativeDepth);

    /// <summary>Screen-space Y for a row at the given relative depth.</summary>
    public static float ProjectedRowScreenY(float relativeDepth, float baseY) =>
        StreetDepthProjection.RowScreenY(relativeDepth, baseY);

    /// <summary>Horizontal scale at a given depth (the X shrink toward the
    /// horizon).</summary>
    public static float HorizontalScale(float relativeDepth) =>
        StreetDepthProjection.HorizontalScale(relativeDepth);

    /// <summary>Full projection of a depth + lateral offset to screen
    /// position and scale.</summary>
    public static (Vector2 Position, Vector2 Scale) Project(
        float relativeDepth,
        float lateralOffset,
        float centerX,
        float baseY) =>
        StreetDepthProjection.Project(relativeDepth, lateralOffset, centerX, baseY);

    /// <summary>Depth at which sprites anchor within their calle's lot: half
    /// a tile behind the calle's own near edge, i.e. near the lot's front
    /// rather than its back.</summary>
    public static float AnchorDepth(float streetDepth) =>
        streetDepth + 0.5f / ParcelGrid.TilesPerStandardLot;

    /// <summary>Snaps a value to the nearest grid step. The chunky pixel
    /// step is deliberately coarser than the underlying geometry so the
    /// floor's staircase edges climb in whole-pixel treads instead of
    /// betraying the pixel art with a smooth diagonal. Forwards to
    /// <see cref="SharedDepthBands.SnapPixel"/> so the macro and the
    /// future expedition path renderer share one snap helper.</summary>
    public static float SnapPixel(float value, float pixelStepPx) =>
        SharedDepthBands.SnapPixel(value, pixelStepPx);

    /// <summary>Draw order for a street band at a given depth. Nearer to
    /// camera means a larger z, and it is the <em>same</em> function for
    /// street bands and for citizen carriers — which is the whole point:
    /// before this they were ordered on two incomparable axes, so a citizen
    /// always won.</summary>
    public static int DepthToZ(float depth, int streetCount, float cameraDepthAnchor) =>
        Mathf.Clamp(
            Mathf.RoundToInt((streetCount - (depth + cameraDepthAnchor)) * MacroViewConstants.BandZStep),
            MacroViewConstants.ZIndexMin,
            MacroViewConstants.ZIndexMax);

    /// <summary>Draw order for a citizen. A citizen stands on the walkable
    /// front band of its lot, in front of whatever that lot holds, so it
    /// takes its own band's order plus one step. Anything on a nearer band
    /// still wins, which is the case that was broken.</summary>
    public static int CitizenZ(float depth, int streetCount, float cameraDepthAnchor) =>
        DepthToZ(depth, streetCount, cameraDepthAnchor) + 1;
}
