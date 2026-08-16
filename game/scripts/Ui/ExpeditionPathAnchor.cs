#nullable enable
using Godot;
using WorldofGoses.Prototypes;

namespace WorldofGoses.Ui;

/// <summary>
/// Where the expedition path's depth grammar is pinned inside the
/// Control that draws it.
///
/// <para>
/// The renderer used to hardcode a 1280x720 anchor — centre X 640,
/// base Y 460 — while the stage Control it draws into is the middle
/// column of the live view, roughly 800x460. Every band was therefore
/// composed for a rectangle that does not exist: the trapezoid ran
/// off the right edge and the horizon sat below the bottom. Deriving
/// the anchor from the Control's own size is what makes the drawing
/// land where the player is looking.
/// </para>
/// </summary>
public readonly record struct ExpeditionPathAnchor(
    float CenterX,
    float BaseY,
    float HalfWidthPx,
    float HorizonY,
    float RowSpacingPx)
{
    /// <summary>
    /// Fraction of the stage height the playable band sits at. Below
    /// it there is room for the foreground fringe, which is drawn at
    /// a negative depth and therefore lower than the playable band.
    /// </summary>
    public const float BaseYRatio = 0.86f;

    /// <summary>
    /// How wide the playable band is relative to the stage. Wider
    /// than the stage on purpose: the band is a road the world scrolls
    /// along, so its edges belong off-screen rather than framed.
    /// </summary>
    public const float HalfWidthRatio = 0.62f;

    /// <summary>The 1280x720 anchor the fixtures were authored against.
    /// Used when a caller has no Control to measure.</summary>
    public static ExpeditionPathAnchor Default { get; } = For(new Vector2(1280f, 720f));

    /// <summary>Derives the anchor from the size of the Control that
    /// will draw the path.</summary>
    public static ExpeditionPathAnchor For(Vector2 stageSize)
    {
        float width = Mathf.Max(stageSize.X, 1f);
        float height = Mathf.Max(stageSize.Y, 1f);
        return new ExpeditionPathAnchor(
            CenterX: width * 0.5f,
            BaseY: height * BaseYRatio,
            HalfWidthPx: width * HalfWidthRatio,
            HorizonY: height * StreetDepthProjection.HorizonRatio,
            RowSpacingPx: height * StreetDepthProjection.RowSpacingRatio);
    }
}
