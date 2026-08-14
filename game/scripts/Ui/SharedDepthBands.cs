#nullable enable
using Godot;

namespace WorldofGoses.Ui;

/// <summary>
/// Shared, engine-agnostic primitives that draw the depth-band spatial
/// grammar documented in <c>docs/engineering/architecture.md</c> §10 "Spatial
/// grammar". Anything that paints to a <see cref="CanvasItem"/> using
/// the same pseudo-3D band projection as the macro and the expedition
/// path renderer reaches through here, instead of duplicating the
/// trapezoid rasterizer or the pixel-snap math per consumer.
///
/// <para>
/// The helpers in this class are deliberately <b>neutral</b>:
/// </para>
/// <list type="bullet">
///   <item>They carry no knowledge of <c>CityMacroSnapshot</c>,
///         parcels, buildings, <c>TerrainWearGrid</c>, navigation
///         occupancy, citizens, or any other urban concept.</item>
///   <item>They do not know whether the renderer consuming them is
///         the macro street view or the expedition path renderer.
///         There is no <c>isExpedition</c> flag and no
///         <c>drawBuildings</c> switch.</item>
///   <item>They sit on top of <see cref="StreetDepthProjection"/>,
///         which already exposes the band projection itself;
///         <see cref="WorldofGoses.Prototypes.MacroProjectionHelpers"/>
///         forwards to that helper for its one-line shape.</item>
/// </list>
///
/// <para>
/// The macro street renderer owns its own urban semantics:
/// parcels, trees, territory tints, building textures and the band
/// occupancy used by routing. Those stay where they are;
/// only the byte-for-byte neutral geometry moves here.
/// </para>
/// </summary>
public static class SharedDepthBands
{
    /// <summary>Snaps a value to the nearest grid step.
    /// The chunky pixel step is deliberately coarser than the
    /// underlying geometry so the floor's staircase edges climb in
    /// whole-pixel treads instead of betraying the pixel art with a
    /// smooth diagonal.</summary>
    public static float SnapPixel(float value, float pixelStepPx) =>
        Mathf.Round(value / pixelStepPx) * pixelStepPx;

    /// <summary>Approximates a perspective trapezoid as a "staircase" of
    /// small, axis-aligned, pixel-snapped rectangles. The function
    /// works for any caller that needs a perspective band painted
    /// from a single atlas sample: ground tiles for the macro and
    /// for the expedition path alike.
    /// <para>
    /// The math used to live on <c>MacroStreetRenderer.DrawPixelStaircaseTrapezoid</c>
    /// and now has a single home so the macro renderer and the
    /// expedition path renderer cannot drift between two copies.
    /// </para></summary>
    public static void DrawStaircaseTrapezoid(
        CanvasItem canvas,
        float yNear, float yFar,
        float xLeftNear, float xRightNear,
        float xLeftFar, float xRightFar,
        Texture2D atlas,
        Rect2 sourceRegion,
        float pixelStepPx,
        int maxStripes = 32)
    {
        float height = yNear - yFar;
        int stripes = Mathf.Clamp(Mathf.RoundToInt(height / pixelStepPx), 1, maxStripes);
        for (int i = 0; i < stripes; i++)
        {
            float tNear = i / (float)stripes;
            float tFar = (i + 1) / (float)stripes;
            float stripeBottom = SnapPixel(Mathf.Lerp(yNear, yFar, tNear), pixelStepPx);
            float stripeTop = SnapPixel(Mathf.Lerp(yNear, yFar, tFar), pixelStepPx);
            float left = SnapPixel(Mathf.Lerp(xLeftNear, xLeftFar, tNear), pixelStepPx);
            float right = SnapPixel(Mathf.Lerp(xRightNear, xRightFar, tNear), pixelStepPx);
            if (right <= left || stripeBottom <= stripeTop) continue;
            var stripeSource = new Rect2(
                sourceRegion.Position.X,
                sourceRegion.Position.Y + sourceRegion.Size.Y * tNear,
                sourceRegion.Size.X,
                sourceRegion.Size.Y * (tFar - tNear));
            canvas.DrawTextureRectRegion(
                atlas,
                new Rect2(new Vector2(left, stripeTop), new Vector2(right - left, stripeBottom - stripeTop)),
                stripeSource);
        }
    }
}
