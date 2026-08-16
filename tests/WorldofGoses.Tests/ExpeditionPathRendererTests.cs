using Godot;
using WorldofGoses.Prototypes;
using WorldofGoses.Ui;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// Regression coverage for <see cref="ExpeditionPathRenderer"/>: the
/// single authority for which band of the expedition path is playable
/// and how a world coordinate reaches the screen.
///
/// <para>
/// The tests that matter here are the ones about the playable band
/// (#27). It was defined twice — <c>PlayableDepth = 0</c> for anyone
/// who asked, and <c>depth == RowCount - 1</c> inside the stage's own
/// terrain loop — so the worn-path tile was painted on the row nearest
/// the horizon while the party stood on the row nearest the camera.
/// Constants agreeing with each other never caught that; only real
/// coordinates do.
/// </para>
/// </summary>
public class ExpeditionPathRendererTests
{
    private static readonly ExpeditionPathAnchor Anchor =
        ExpeditionPathAnchor.For(new Vector2(800f, 460f));

    [Fact]
    public void RowScreenY_ForwardsToStreetDepthProjection()
    {
        // Through the anchor's own horizon and row spacing, not the macro's
        // absolute ones: the path draws into a Control a fraction of the
        // viewport, so it scales the projection with its window. What this
        // still guards is that it forwards rather than reimplementing.
        float ourY = ExpeditionPathRenderer.RowScreenY(2f, Anchor);
        float sharedY = StreetDepthProjection.RowScreenY(
            2f, Anchor.BaseY, Anchor.HorizonY, Anchor.RowSpacingPx);
        Assert.Equal(sharedY, ourY);
    }

    /// <summary>
    /// The playable band is the calle, with parcel rows on both sides of it.
    /// </summary>
    /// <remarks>
    /// This used to assert that the playable band was the row nearest the
    /// camera, which held while it was depth 0. It is now row 3 — the first
    /// tile of the parcel's second lot row, the band that wears into a path in
    /// the macro — so there are rows in front of the party as well as behind.
    /// What still must never invert is the ordering: everything behind sits
    /// higher on screen and everything in front sits lower, or "playable band"
    /// silently means the horizon and the party walks on the sky.
    /// </remarks>
    [Fact]
    public void PlayableDepth_HasParcelRowsOnBothSidesOfIt()
    {
        float playableY = ExpeditionPathRenderer.PlayableScreenY(Anchor);

        for (float depth = ExpeditionPathRenderer.PlayableDepth + 1f;
            depth < ExpeditionPathRenderer.RowCount;
            depth += 1f)
        {
            Assert.True(
                ExpeditionPathRenderer.RowScreenY(depth, Anchor) < playableY,
                $"Depth {depth} is behind the party and should sit above the band.");
        }

        for (float depth = ExpeditionPathRenderer.ForegroundDepth;
            depth < ExpeditionPathRenderer.PlayableDepth;
            depth += 1f)
        {
            Assert.True(
                ExpeditionPathRenderer.RowScreenY(depth, Anchor) > playableY,
                $"Depth {depth} is in front of the party and should sit below the band.");
        }
    }

    [Fact]
    public void IsPlayableDepth_AnswersForExactlyOneRow()
    {
        int playableRows = 0;
        for (float depth = ExpeditionPathRenderer.ForegroundDepth;
            depth < ExpeditionPathRenderer.RowCount;
            depth += 1f)
        {
            if (ExpeditionPathRenderer.IsPlayableDepth(depth)) playableRows++;
        }
        Assert.Equal(1, playableRows);
        Assert.True(ExpeditionPathRenderer.IsPlayableDepth(
            ExpeditionPathRenderer.PlayableDepth));
        Assert.False(ExpeditionPathRenderer.IsPlayableDepth(
            ExpeditionPathRenderer.RowCount - 1f));
    }

    [Fact]
    public void LayerForDepth_OrdersTheFourPlanes()
    {
        Assert.Equal(
            ExpeditionPathLayer.Foreground,
            ExpeditionPathRenderer.LayerForDepth(ExpeditionPathRenderer.ForegroundDepth));
        Assert.Equal(
            ExpeditionPathLayer.Playable,
            ExpeditionPathRenderer.LayerForDepth(ExpeditionPathRenderer.PlayableDepth));
        // Relative to the band, not the literal 2f this carried: with the
        // playable band moved back to the calle, row 2 is in front of the party
        // and the first rear row is the one immediately behind it.
        Assert.Equal(
            ExpeditionPathLayer.Rear,
            ExpeditionPathRenderer.LayerForDepth(ExpeditionPathRenderer.PlayableDepth + 1f));
        Assert.Equal(
            ExpeditionPathLayer.Distance,
            ExpeditionPathRenderer.LayerForDepth(ExpeditionPathRenderer.RowCount - 1f));
    }

    [Fact]
    public void ParallaxFactors_IncreaseFromTheHorizonToTheFringe()
    {
        float distance = ExpeditionPathRenderer.ParallaxFactorForDepth(
            ExpeditionPathRenderer.RowCount - 1f);
        float rear = ExpeditionPathRenderer.ParallaxFactorForDepth(
            ExpeditionPathRenderer.PlayableDepth + 1f);
        float playable = ExpeditionPathRenderer.ParallaxFactorForDepth(
            ExpeditionPathRenderer.PlayableDepth);
        float foreground = ExpeditionPathRenderer.ParallaxFactorForDepth(
            ExpeditionPathRenderer.ForegroundDepth);

        Assert.True(distance < rear, "The backdrop must crawl behind the rear rows.");
        Assert.True(rear < playable, "Rear rows must trail the band the party stands on.");
        Assert.True(playable < foreground, "The fringe must outrun the playable band.");
    }

    [Fact]
    public void WorldToScreenX_SlidesTheWorldByExactlyTheOffsetOnThePlayableBand()
    {
        // The playable band moves 1:1 with the offset. This is what
        // "the world moves, the party does not" reduces to.
        float atRest = ExpeditionPathRenderer.PlayableScreenX(600, 0, Anchor);
        float advanced = ExpeditionPathRenderer.PlayableScreenX(600, 250, Anchor);
        Assert.Equal(atRest - 250f, advanced, precision: 3);
    }

    [Fact]
    public void PlayableScreenX_KeepsTheTravellerAtTheAnchorCentre()
    {
        // The founder's world X *is* the offset during travel, so the
        // difference the projection draws is zero whatever the number.
        foreach (long positionX in new long[] { 0, 137, 486, 999 })
        {
            Assert.Equal(
                Anchor.CenterX,
                ExpeditionPathRenderer.PlayableScreenX(positionX, positionX, Anchor),
                precision: 3);
        }
    }

    [Fact]
    public void WorldToScreenX_MovesSlowerLayersLessForTheSameTravel()
    {
        const long travelled = 400;
        float playableShift = ShiftFor(ExpeditionPathRenderer.PlayableDepth, travelled);
        float rearShift = ShiftFor(ExpeditionPathRenderer.PlayableDepth + 1f, travelled);
        float distanceShift = ShiftFor(ExpeditionPathRenderer.RowCount - 1f, travelled);
        float foregroundShift = ShiftFor(ExpeditionPathRenderer.ForegroundDepth, travelled);

        Assert.True(distanceShift < rearShift);
        Assert.True(rearShift < playableShift);
        Assert.True(playableShift < foregroundShift);
        Assert.Equal((float)travelled, playableShift, precision: 3);
    }

    [Fact]
    public void IsRowVisible_SpansTheFringeThroughTheHorizon()
    {
        Assert.True(ExpeditionPathRenderer.IsRowVisible(
            ExpeditionPathRenderer.ForegroundDepth));
        Assert.True(ExpeditionPathRenderer.IsRowVisible(
            ExpeditionPathRenderer.PlayableDepth));
        Assert.True(ExpeditionPathRenderer.IsRowVisible(
            ExpeditionPathRenderer.RowCount - 1f));
        Assert.False(ExpeditionPathRenderer.IsRowVisible(
            ExpeditionPathRenderer.ForegroundDepth - 0.01f));
        Assert.False(ExpeditionPathRenderer.IsRowVisible(
            ExpeditionPathRenderer.RowCount));
    }

    [Fact]
    public void PlayableHorizontalScale_TracksTheSharedProjection()
    {
        Assert.Equal(
            StreetDepthProjection.HorizontalScale(ExpeditionPathRenderer.PlayableDepth),
            ExpeditionPathRenderer.PlayableHorizontalScale());
    }

    private static float ShiftFor(float depth, long travelled)
    {
        float factor = ExpeditionPathRenderer.ParallaxFactorForDepth(depth);
        float atRest = ExpeditionPathRenderer.WorldToScreenX(600, 0, factor, Anchor);
        float advanced = ExpeditionPathRenderer.WorldToScreenX(600, travelled, factor, Anchor);
        return atRest - advanced;
    }
}
