using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using WorldofGoses.Ui;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// The geometry the expedition stage actually paints (#22, #23, #25,
/// #27).
///
/// <para>
/// These assertions run against <see cref="ExpeditionPathComposition"/>
/// — the same calls <c>ExpeditionStage._Draw</c> makes, not a
/// re-enactment of them. That distinction is the whole reason this file
/// exists: the previous coverage built its own chunk pool and proved
/// the pool worked, while the renderer beside it drew static bands and
/// never consulted the pool at all. A green suite meant nothing about
/// the picture. Every number below is a screen coordinate.
/// </para>
/// </summary>
public class ExpeditionPathCompositionTests
{
    private static readonly ExpeditionPathAnchor Anchor =
        ExpeditionPathAnchor.For(new Vector2(800f, 460f));
    private const long ChunkWidth = (long)ExpeditionPathChunkPool.ChunkWidthUnits;

    [Fact]
    public void ExactlyOneBandIsPlayable_AndItIsTheOneGameplayStandsOn()
    {
        // #27: the terrain used to mark depth RowCount-1 as the path
        // while party, enemies and the objective resolved their Y from
        // depth 0. Same band, two authorities, opposite ends of the
        // screen. This compares the coordinates, not the constants.
        IReadOnlyList<ExpeditionPathBand> bands = ExpeditionPathComposition.Bands(Anchor);
        ExpeditionPathBand[] playable = bands.Where(b => b.IsPlayable).ToArray();

        Assert.Single(playable);
        Assert.Equal(
            ExpeditionPathRenderer.PlayableScreenY(Anchor),
            playable[0].ScreenYNear,
            precision: 3);
        Assert.Equal(ExpeditionPathLayer.Playable, playable[0].Layer);
    }

    [Fact]
    public void Bands_CoverTheFringeThroughTheHorizon_NearestFirst()
    {
        IReadOnlyList<ExpeditionPathBand> bands = ExpeditionPathComposition.Bands(Anchor);
        Assert.Equal(ExpeditionPathRenderer.RowCount + 1, bands.Count);
        Assert.Equal(ExpeditionPathRenderer.ForegroundDepth, bands[0].Depth);
        for (int i = 1; i < bands.Count; i++)
        {
            Assert.True(
                bands[i].ScreenYNear < bands[i - 1].ScreenYNear,
                "Bands must climb toward the horizon as depth grows.");
        }
    }

    [Fact]
    public void AdvancingTheOffset_SlidesTheChunkSeamsByExactlyThatMuch()
    {
        // The proof that the world moves. Before this was connected,
        // the pool could advance thousands of chunks without a pixel
        // of the path changing.
        var pool = new ExpeditionPathChunkPool(seed: 4);
        pool.SetWorldOffset(0);
        IReadOnlyList<float> atRest =
            ExpeditionPathComposition.ChunkSeams(pool.Chunks, 0, Anchor);
        IReadOnlyList<float> advanced =
            ExpeditionPathComposition.ChunkSeams(pool.Chunks, 120, Anchor);

        Assert.Equal(atRest.Count, advanced.Count);
        for (int i = 0; i < atRest.Count; i++)
        {
            Assert.Equal(atRest[i] - 120f, advanced[i], precision: 3);
        }
    }

    [Fact]
    public void ScrollingAcrossChunks_MovesTheTerrainWithoutGrowingIt()
    {
        var pool = new ExpeditionPathChunkPool(seed: 6);
        pool.SetWorldOffset(0);
        IReadOnlyList<float> start =
            ExpeditionPathComposition.ChunkSeams(pool.Chunks, 0, Anchor);
        int propCeiling = 0;

        for (long offset = 0; offset <= 8 * ChunkWidth; offset += ChunkWidth / 2)
        {
            pool.SetWorldOffset(offset);
            IReadOnlyList<ExpeditionPathProp> props =
                ExpeditionPathComposition.Props(pool.Chunks, offset, Anchor);
            Assert.Equal(
                ExpeditionPathChunkPool.ChunkCount,
                ExpeditionPathComposition.ChunkSeams(pool.Chunks, offset, Anchor).Count);
            propCeiling = System.Math.Max(propCeiling, props.Count);
        }

        // Deliberately not a whole number of chunks: the path is a
        // tiling, so a chunk-aligned offset legitimately looks
        // identical. Between boundaries is where movement shows.
        long unaligned = 8 * ChunkWidth + 90;
        pool.SetWorldOffset(unaligned);
        IReadOnlyList<float> end =
            ExpeditionPathComposition.ChunkSeams(pool.Chunks, unaligned, Anchor);
        Assert.NotEqual(start[0], end[0]);

        // Bounded density: at most PropCount (<=4) rear props plus one
        // fringe prop per chunk, and the chunk count never moves.
        Assert.InRange(propCeiling, 1, ExpeditionPathChunkPool.ChunkCount * 5);
    }

    [Fact]
    public void ReturnLeg_PutsTheWorldBackWhereItWas()
    {
        var pool = new ExpeditionPathChunkPool(seed: 9);
        pool.SetWorldOffset(3 * ChunkWidth);
        IReadOnlyList<float> outbound =
            ExpeditionPathComposition.ChunkSeams(pool.Chunks, 3 * ChunkWidth, Anchor);

        pool.SetWorldOffset(11 * ChunkWidth);
        pool.SetWorldOffset(3 * ChunkWidth);
        IReadOnlyList<float> returned =
            ExpeditionPathComposition.ChunkSeams(pool.Chunks, 3 * ChunkWidth, Anchor);

        Assert.Equal(outbound, returned);
    }

    [Fact]
    public void Layers_MoveByDifferentAmountsForTheSameTravel()
    {
        // Four planes, four rates. Measured as screen displacement of
        // the same world feature, not as a comparison of the factors
        // that produced it.
        var pool = new ExpeditionPathChunkPool(seed: 12);
        pool.SetWorldOffset(0);

        float rearShift = LayerShift(pool, ExpeditionPathLayer.Rear, travelled: 200);
        float foregroundShift = LayerShift(pool, ExpeditionPathLayer.Foreground, travelled: 200);
        float playableShift =
            ExpeditionPathComposition.ChunkSeams(pool.Chunks, 0, Anchor)[0]
            - ExpeditionPathComposition.ChunkSeams(pool.Chunks, 200, Anchor)[0];
        IReadOnlyList<ExpeditionPathProp> before =
            ExpeditionPathComposition.DistanceBlocks(0, 800f, Anchor);
        // A block from the middle of the visible run: one at the edge
        // can legitimately leave the window between the two samples.
        ExpeditionPathProp tracked = before[before.Count / 2];
        float distanceShift = tracked.ScreenX
            - ExpeditionPathComposition.DistanceBlocks(200, 800f, Anchor)
                .First(b => b.LogicalIndex == tracked.LogicalIndex)
                .ScreenX;

        Assert.True(distanceShift < rearShift, "Backdrop must crawl behind the rear dressing.");
        Assert.True(rearShift < playableShift, "Rear dressing must trail the path.");
        Assert.True(playableShift < foregroundShift, "The fringe must outrun the path.");
    }

    [Fact]
    public void Dressing_FollowsTheChunkThatOwnsIt()
    {
        var pool = new ExpeditionPathChunkPool(seed: 21);
        pool.SetWorldOffset(0);
        var byIndex = new Dictionary<long, int>();
        foreach (ExpeditionPathProp prop in
            ExpeditionPathComposition.Props(pool.Chunks, 0, Anchor))
        {
            byIndex[prop.LogicalIndex] = prop.BiomeId;
        }

        pool.SetWorldOffset(30 * ChunkWidth);
        pool.SetWorldOffset(0);
        foreach (ExpeditionPathProp prop in
            ExpeditionPathComposition.Props(pool.Chunks, 0, Anchor))
        {
            Assert.Equal(byIndex[prop.LogicalIndex], prop.BiomeId);
        }
    }

    [Fact]
    public void DistanceBlocks_StayBoundedAndOnScreen()
    {
        for (long offset = 0; offset <= 20_000; offset += 733)
        {
            IReadOnlyList<ExpeditionPathProp> blocks =
                ExpeditionPathComposition.DistanceBlocks(offset, 800f, Anchor);
            Assert.NotEmpty(blocks);
            Assert.InRange(blocks.Count, 1, 64);
            foreach (ExpeditionPathProp block in blocks)
            {
                Assert.InRange(
                    block.ScreenX,
                    -ExpeditionPathComposition.DistanceBlockWidthPx,
                    800f + ExpeditionPathComposition.DistanceBlockWidthPx);
            }
        }
    }

    [Fact]
    public void RearDressing_StaysBehindThePlayableBand()
    {
        // The fringe is allowed in front of the party; the rear
        // dressing is not, or it would occlude the combatants it is
        // supposed to sit behind.
        var pool = new ExpeditionPathChunkPool(seed: 33);
        pool.SetWorldOffset(512);
        float playableY = ExpeditionPathRenderer.PlayableScreenY(Anchor);
        foreach (ExpeditionPathProp prop in
            ExpeditionPathComposition.Props(pool.Chunks, 512, Anchor))
        {
            if (prop.Layer == ExpeditionPathLayer.Rear)
            {
                Assert.True(prop.ScreenBaseY < playableY);
            }
            else
            {
                Assert.True(prop.ScreenBaseY > playableY);
            }
        }
    }

    [Fact]
    public void Stage_DrawsThroughTheComposition_AndKeepsNoSecondBandRule()
    {
        // The reopening's core complaint was that the renderer ignored
        // everything beside it. A unit test of a pure function cannot
        // see that; this can.
        string source = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(),
            "game", "scripts", "Ui", "ExpeditionStage.cs"));

        Assert.Contains("ExpeditionPathComposition.Bands(", source, System.StringComparison.Ordinal);
        Assert.Contains(
            "ExpeditionPathComposition.ChunkSeams(",
            source,
            System.StringComparison.Ordinal);
        Assert.Contains("ExpeditionPathComposition.Props(", source, System.StringComparison.Ordinal);
        Assert.Contains(
            "ExpeditionPathComposition.DistanceBlocks(",
            source,
            System.StringComparison.Ordinal);
        // The parallel playable-band rule that #27 removed.
        Assert.DoesNotContain("RowCount - 1", source, System.StringComparison.Ordinal);
        // And the projection that bypassed the world offset.
        Assert.DoesNotContain(
            "ProjectDomainXToStageX",
            source,
            System.StringComparison.Ordinal);
    }

    private static float LayerShift(
        ExpeditionPathChunkPool pool,
        ExpeditionPathLayer layer,
        long travelled)
    {
        ExpeditionPathProp before = ExpeditionPathComposition
            .Props(pool.Chunks, 0, Anchor).First(p => p.Layer == layer);
        ExpeditionPathProp after = ExpeditionPathComposition
            .Props(pool.Chunks, travelled, Anchor)
            .First(p => p.Layer == layer && p.LogicalIndex == before.LogicalIndex);
        return before.ScreenX - after.ScreenX;
    }
}
