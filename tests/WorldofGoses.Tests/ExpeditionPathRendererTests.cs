using WorldofGoses.Prototypes;
using WorldofGoses.Ui;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// Regression coverage for the new <see cref="ExpeditionPathRenderer"/>
/// introduced in #21. The renderer consumes
/// <see cref="StreetDepthProjection"/> and <see cref="SharedDepthBands"/>
/// without copying either formula, leaves the macro output untouched,
/// and projects the canonical 1D domain coordinates onto the playable
/// band without inventing a second authority.
/// </summary>
public class ExpeditionPathRendererTests
{
    [Fact]
    public void RowCount_IsStatic_ForSlice21()
    {
        // The slice still owns a finite strip; #22 swaps in the
        // recycler.
        Assert.Equal(6, ExpeditionPathRenderer.RowCount);
    }

    [Fact]
    public void RowScreenY_ForwardsToStreetDepthProjection()
    {
        float ourY = ExpeditionPathRenderer.RowScreenY(2f);
        float sharedY = StreetDepthProjection.RowScreenY(2f, ExpeditionPathRenderer.BaseY);
        Assert.Equal(sharedY, ourY);
    }

    [Fact]
    public void PlayableDepth_IsDepthZero()
    {
        Assert.Equal(0f, ExpeditionPathRenderer.PlayableDepth);
    }

    [Fact]
    public void ProjectDomainXToStageX_MonotonicAndClampedToThePlayableBand()
    {
        // The renderer must project the authoritative 1D combat /
        // travel PositionX onto the playable band. Order is preserved
        // and the projection sits inside the stage's horizontal band.
        float left = ExpeditionPathRenderer.ProjectDomainXToStageX(0, 1000, 0);
        float middle = ExpeditionPathRenderer.ProjectDomainXToStageX(0, 1000, 500);
        float right = ExpeditionPathRenderer.ProjectDomainXToStageX(0, 1000, 1000);

        Assert.True(left < middle);
        Assert.True(middle < right);
        float playableHorizontalScale = ExpeditionPathRenderer.PlayableHorizontalScale();
        float halfWidth = ExpeditionPathRenderer.HalfWidthPx * playableHorizontalScale;
        Assert.InRange(left, ExpeditionPathRenderer.CenterX - halfWidth - 0.01f,
            ExpeditionPathRenderer.CenterX - halfWidth + 0.01f);
        Assert.InRange(right, ExpeditionPathRenderer.CenterX + halfWidth - 0.01f,
            ExpeditionPathRenderer.CenterX + halfWidth + 0.01f);
    }

    [Fact]
    public void ProjectDomainXToStageX_ThrowsWhenDomainIsDegenerate()
    {
        // An invalid domain envelope (max <= min) is a programming
        // error; we surface it loudly rather than divide by zero.
        Assert.Throws<System.ArgumentOutOfRangeException>(
            () => ExpeditionPathRenderer.ProjectDomainXToStageX(100, 100, 50));
    }

    [Fact]
    public void IsRowVisible_AcceptsTheStaticWindow()
    {
        Assert.True(ExpeditionPathRenderer.IsRowVisible(0f));
        Assert.True(ExpeditionPathRenderer.IsRowVisible(
            ExpeditionPathRenderer.RowCount - 1f));
        Assert.False(ExpeditionPathRenderer.IsRowVisible(-0.01f));
        Assert.False(ExpeditionPathRenderer.IsRowVisible(
            ExpeditionPathRenderer.RowCount));
    }

    [Fact]
    public void PlayableBandY_IsGreaterThanRearBandY()
    {
        // Rear bands (smaller depth) sit higher on screen (smaller
        // Y) than the playable band — pins the convergence direction
        // the macro established.
        float rearY = ExpeditionPathRenderer.RowScreenY(
            ExpeditionPathRenderer.RowCount - 2f);
        float playableY = ExpeditionPathRenderer.RowScreenY(
            ExpeditionPathRenderer.PlayableDepth);
        Assert.True(rearY < playableY);
    }

    [Fact]
    public void PlayableHorizontalScale_TracksTheSharedProjection()
    {
        Assert.Equal(
            StreetDepthProjection.HorizontalScale(0f),
            ExpeditionPathRenderer.PlayableHorizontalScale());
    }
}
