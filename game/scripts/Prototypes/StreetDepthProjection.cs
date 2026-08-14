#nullable enable
using Godot;

namespace WorldofGoses.Prototypes;

/// <summary>
/// Pure pseudo-3D depth projection for the macro city's "calles" (depth
/// rows) — Atari-racing-style perspective (Pole Position/Out Run): farther
/// rows render smaller AND narrower, not just uniformly smaller, and row
/// spacing itself compresses with depth so distant streets read as
/// converging rather than merely shrinking. Still pure 2D (sprites/tiles
/// rescaled by code) — never a 3D/2.5D projection. See
/// docs/presentation/visual-language.md,
/// "Ciudad macro (perspectiva por calles)".
///
/// <paramref name="depth"/> is a continuous float, not the integer street
/// index, so <see cref="MacroStreetWorld"/> can drive a handful of discrete
/// intermediate depths during a street-change transition without this
/// helper needing to know about transitions at all.
/// </summary>
public static class StreetDepthProjection
{
    // 2026-07-31: flattened again per user feedback — the original factors (0.85/0.80)
    // plus an 80px row step made the "road" tall and steep, so rows far
    // from the viewer diverged in aspect (horizontal shrinking much faster
    // than vertical) enough to read as visibly "stretched" rather than
    // gently receding. A smaller gap between the two factors keeps the
    // required non-uniform shrink (horizontal still shrinks faster — see
    // StreetDepthProjectionTests.FartherRows_ShrinkNonUniformly) but far
    // more gradually; raising the horizon shortens the total vertical
    // throw, i.e. a shallower incline. The pixel-staircase rendering
    // technique itself (DrawPixelStaircaseTrapezoid) is untouched by this —
    // it only consumes whatever screen coordinates these formulas produce.
    public const float VerticalDepthFactor = 0.90f;
    public const float HorizontalDepthFactor = 0.88f;
    private const float BaseRowSpacingPx = 58f;
    private const float HorizonY = 200f;

    // Keep the focused street plus the two completed streets immediately in
    // front of it. The fourth foreground street crosses the near plane.
    public const float NearClipDepth = -3f;

    // Thirteen street bands total remain renderable at an integer camera
    // anchor: two foreground bands (-2/-1), the focused band (0), and ten
    // receding bands (1..10). That is four complete three-street parcel rows
    // plus the leading band of the next row. Camera movement shifts this
    // window through a larger semantic city before the original projection
    // would collapse rows onto its authored y=200 horizon.
    public const float FarClipDepth = 11f;

    public static float VerticalScale(float depth) => Mathf.Pow(VerticalDepthFactor, depth);

    public static float HorizontalScale(float depth) => Mathf.Pow(HorizontalDepthFactor, depth);

    public static bool IsVisibleDepth(float depth) =>
        depth > NearClipDepth && depth < FarClipDepth;

    /// <summary>
    /// Screen-space Y for a given depth: the closest row (depth 0) sits at
    /// <paramref name="baseY"/>; farther rows accumulate shrinking spacing
    /// (a geometric series in <see cref="VerticalDepthFactor"/>) so they
    /// converge toward a horizon without ever reaching it.
    /// </summary>
    public static float RowScreenY(float depth, float baseY)
    {
        float totalSpacing = BaseRowSpacingPx
            * VerticalDepthFactor
            * (1f - Mathf.Pow(VerticalDepthFactor, depth))
            / (1f - VerticalDepthFactor);
        return Mathf.Max(baseY - totalSpacing, HorizonY);
    }

    /// <summary>
    /// Projects a point at <paramref name="depth"/> with
    /// <paramref name="lateralOffset"/> (row-local, unscaled logical pixels
    /// from the row's center) to a screen position and non-uniform scale.
    /// </summary>
    public static (Vector2 Position, Vector2 Scale) Project(
        float depth,
        float lateralOffset,
        float centerX,
        float baseY)
    {
        float verticalScale = VerticalScale(depth);
        float horizontalScale = HorizontalScale(depth);
        float screenY = RowScreenY(depth, baseY);
        float screenX = centerX + lateralOffset * horizontalScale;
        return (new Vector2(screenX, screenY), new Vector2(horizontalScale, verticalScale));
    }
}
