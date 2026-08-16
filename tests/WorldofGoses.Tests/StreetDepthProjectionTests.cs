using System.Linq;
using Godot;
using WorldofGoses.Prototypes;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// First direct coverage for the pure projection math behind the pseudo-3D
/// macro view (H-32) — the audit found the live view relied on it with no
/// tests at all.
/// </summary>
public class StreetDepthProjectionTests
{
    [Fact]
    public void FartherRows_ShrinkNonUniformly()
    {
        // Horizontal must shrink FASTER than vertical so distant streets
        // read as converging perspective, not just smaller sprites
        // (design bible §08, "Ciudad macro (perspectiva por calles)").
        float verticalNear = StreetDepthProjection.VerticalScale(1f);
        float horizontalNear = StreetDepthProjection.HorizontalScale(1f);

        Assert.True(horizontalNear < verticalNear);
        Assert.True(StreetDepthProjection.VerticalScale(3f) < verticalNear);
        Assert.True(StreetDepthProjection.HorizontalScale(3f) < horizontalNear);
    }

    [Fact]
    public void RowScreenY_ConvergesTowardTheHorizonWithoutCrossingIt()
    {
        const float baseY = 580f;
        float previous = StreetDepthProjection.RowScreenY(0f, baseY);
        Assert.Equal(baseY, previous);

        for (int depth = 1; depth <= 40; depth++)
        {
            float current = StreetDepthProjection.RowScreenY(depth, baseY);
            Assert.True(current <= previous, $"row {depth} must not move down-screen");
            Assert.True(current >= 80f, "rows never cross the horizon");
            previous = current;
        }
    }

    /// <summary>
    /// The ground's apparent incline, guarded as a ratio rather than as a pixel
    /// count.
    /// </summary>
    /// <remarks>
    /// The tilt is how far a street advances up the screen relative to how wide
    /// a lot is: a lot drawn 96 across and 55.7 tall reads as ground receding,
    /// and the same lot drawn 96 across and 40 tall would read as a wall. This
    /// used to assert the pixel step directly, which passed only for one world
    /// scale — moving the world to a 32 px tile grid changed the step from 52.2
    /// to 55.7 while the incline stayed identical, and the old assertion could
    /// not tell those two facts apart. The ratio is the invariant; the pixels
    /// follow whatever <c>LotUnitPx</c> is.
    /// </remarks>
    [Fact]
    public void AdjacentRows_KeepTheGroundsIncline()
    {
        const float baseY = 580f;

        float firstRowStep = baseY - StreetDepthProjection.RowScreenY(1f, baseY);
        float incline = firstRowStep / MacroViewConstants.LotUnitPx;

        Assert.InRange(incline, 0.57f, 0.59f);
    }

    [Fact]
    public void DepthWindow_KeepsTwoForegroundRowsAndDropsTheFourthPosition()
    {
        Assert.False(StreetDepthProjection.IsVisibleDepth(-3f));
        Assert.True(StreetDepthProjection.IsVisibleDepth(-2f));
        Assert.True(StreetDepthProjection.IsVisibleDepth(-1f));
        Assert.True(StreetDepthProjection.IsVisibleDepth(0f));
    }

    [Fact]
    public void Project_OffsetsLateralPositionsByTheHorizontalScale()
    {
        (Vector2 centered, _) = StreetDepthProjection.Project(2f, 0f, 640f, 580f);
        (Vector2 offset, Vector2 scale) = StreetDepthProjection.Project(2f, 100f, 640f, 580f);

        Assert.Equal(640f, centered.X);
        Assert.Equal(640f + 100f * scale.X, offset.X);
        Assert.Equal(centered.Y, offset.Y);
    }

    [Fact]
    public void DepthWindow_ContainsThirteenStreetBandsAtAnIntegerAnchor()
    {
        Assert.Equal(13, Enumerable.Range(-3, 15)
            .Count(depth => StreetDepthProjection.IsVisibleDepth(depth)));
        Assert.True(StreetDepthProjection.IsVisibleDepth(10f));
        Assert.False(StreetDepthProjection.IsVisibleDepth(11f));
        Assert.True(
            StreetDepthProjection.RowScreenY(10f, 580f)
            > StreetDepthProjection.RowScreenY(11f, 580f));
    }
}
