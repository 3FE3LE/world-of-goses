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

    [Fact]
    public void Project_OffsetsLateralPositionsByTheHorizontalScale()
    {
        (Vector2 centered, _) = StreetDepthProjection.Project(2f, 0f, 640f, 580f);
        (Vector2 offset, Vector2 scale) = StreetDepthProjection.Project(2f, 100f, 640f, 580f);

        Assert.Equal(640f, centered.X);
        Assert.Equal(640f + 100f * scale.X, offset.X);
        Assert.Equal(centered.Y, offset.Y);
    }
}
