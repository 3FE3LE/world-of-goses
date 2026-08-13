#nullable enable
using Godot;
using WorldofGoses.Prototypes;

namespace WorldofGoses.Ui;

/// <summary>
/// Pseudo-3D depth-band stage for the expedition path. Built as a
/// presentation helper (not a Godot <see cref="Node"/>): the owning
/// <c>ExpeditionStage</c> reads the rendered terrain rects and asks
/// <see cref="SharedDepthBands"/> to draw each row, then projects
/// combatants and the objective through
/// <see cref="StreetDepthProjection"/> on the playable band.
///
/// <para>
/// The path stage shares the macro's projection vocabulary without
/// inheriting any urban semantics: there is no parcel, no tree, no
/// building, no plot, no territory tint and no road occupancy here.
/// Chunk logic arrives in #22; this slice paints one static finite
/// strip so we can prove the new grammar in isolation before
/// recycling and parallax land.
/// </para>
/// </summary>
public static class ExpeditionPathRenderer
{
    /// <summary>How many rows the static slice paints.</summary>
    public const int RowCount = 6;

    /// <summary>Bottom of the playable band (matches macro BaseY).</summary>
    public const float BaseY = 460f;

    /// <summary>Stage centre X; matched to the 1280x720 anchor.</summary>
    public const float CenterX = 640f;

    /// <summary>Width in pixels the projected playable band spans.</summary>
    public const float HalfWidthPx = 380f;

    /// <summary>Step used by the pixel staircase trapezoid.</summary>
    public const float PixelStep = 2f;

    /// <summary>Identifier of the playable band from
    /// <see cref="StreetDepthProjection"/>-relative depth: the
    /// expedition party and enemies share this single Y.</summary>
    public const float PlayableDepth = 0f;

    /// <summary>
    /// Computes the screen Y of a depth-band row inside this stage.
    /// Forwarded to <see cref="StreetDepthProjection"/> so the
    /// formula lives in one place; the only domain knowledge here
    /// is which <paramref name="baseY"/> to anchor the horizon to.
    /// </summary>
    public static float RowScreenY(float depth) =>
        StreetDepthProjection.RowScreenY(depth, BaseY);

    /// <summary>Horizontal scale on the playable band (depth 0).</summary>
    public static float PlayableHorizontalScale() =>
        StreetDepthProjection.HorizontalScale(PlayableDepth);

    /// <summary>Vertical scale on the playable band (depth 0).</summary>
    public static float PlayableVerticalScale() =>
        StreetDepthProjection.VerticalScale(PlayableDepth);

    /// <summary>
    /// Whether the row at <paramref name="depth"/> lives inside the
    /// static stage window. A finite slice, not the macro's
    /// thirteen-street anchor.
    /// </summary>
    public static bool IsRowVisible(float depth) =>
        depth >= 0f && depth < RowCount;

    /// <summary>
    /// Maps the authoritative one-dimensional domain X onto the
    /// playable-band screen X. Travels and combat
    /// <see cref="Domain.Combat.CombatParticipantState.PositionX"/>
    /// remain the only source of truth; this method is the
    /// read-only projection of that value to the stage.
    /// </summary>
    public static float ProjectDomainXToStageX(double domainMinimumX, double domainMaximumX, double domainX)
    {
        if (domainMaximumX <= domainMinimumX)
            throw new System.ArgumentOutOfRangeException(nameof(domainMaximumX));
        float horizontalScale = PlayableHorizontalScale();
        float ratio = (float)((domainX - domainMinimumX) / (domainMaximumX - domainMinimumX));
        float halfWidth = HalfWidthPx * horizontalScale;
        return CenterX - halfWidth + ratio * (halfWidth * 2f);
    }
}
