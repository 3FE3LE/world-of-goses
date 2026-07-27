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
/// docs/world-of-goses-design-bible/08_VISUAL_UI_AND_ASSET_GUIDELINES.md,
/// "Ciudad macro (perspectiva por calles)".
///
/// <paramref name="depth"/> is a continuous float, not the integer street
/// index, so <see cref="MacroStreetWorld"/> can drive a handful of discrete
/// intermediate depths during a street-change transition without this
/// helper needing to know about transitions at all.
/// </summary>
public static class StreetDepthProjection
{
    public const float VerticalDepthFactor = 0.85f;
    public const float HorizontalDepthFactor = 0.80f;
    private const float BaseRowSpacingPx = 96f;
    private const float HorizonY = 80f;

    public static float VerticalScale(float depth) => Mathf.Pow(VerticalDepthFactor, depth);

    public static float HorizontalScale(float depth) => Mathf.Pow(HorizontalDepthFactor, depth);

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
