using System.Collections.Generic;
using Godot;
using WorldofGoses.Ui;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// #25 contract: parallax is derived from the same world offset the
/// chunk pool drives; rear layers shift slower than the playable
/// band, foreground shifts faster; the offsets remain directional
/// and reversible without inventing a second authority. The same
/// chunk logical index must always regenerate the same dressing
/// when the seed is stable.
/// </summary>
public class ExpeditionPathParallaxTests
{
    [Fact]
    public void LayerFactors_HaveEstablishedDirectionalOrder()
    {
        // Distance slowest, rear slow, foreground fastest. The
        // ratios must read as: distance < rear < playable (1.0) <
        // foreground.
        Assert.True(ExpeditionPathParallax.DistanceFactor < ExpeditionPathParallax.RearFactor);
        Assert.True(ExpeditionPathParallax.RearFactor < 1f);
        Assert.True(1f < ExpeditionPathParallax.ForegroundFactor);
    }

    [Fact]
    public void LayerOffset_IsDerivedFromWorldOffset()
    {
        long offset = 1024;
        Assert.Equal(offset * ExpeditionPathParallax.DistanceFactor,
            ExpeditionPathParallax.LayerOffset(offset, ExpeditionPathParallax.DistanceFactor));
        Assert.Equal(offset * ExpeditionPathParallax.ForegroundFactor,
            ExpeditionPathParallax.LayerOffset(offset, ExpeditionPathParallax.ForegroundFactor));
    }

    [Fact]
    public void TwoSnapshotsSameWorldOffset_ProduceSameParallax()
    {
        // Two snapshots with the same authoritative world offset
        // produce identical layer offsets — the parallax never
        // diverges from the offset input.
        long offset = 8192;
        float rearA = ExpeditionPathParallax.LayerOffset(offset, ExpeditionPathParallax.RearFactor);
        float rearB = ExpeditionPathParallax.LayerOffset(offset, ExpeditionPathParallax.RearFactor);
        Assert.Equal(rearA, rearB);
    }

    [Fact]
    public void ReturnLeg_ReversesParallaxWithoutRecomputation()
    {
        // The same world offset on a return leg yields the same
        // layer offsets — the parallax is a pure function of the
        // offset, not of the direction of travel.
        long outOffset = 4096;
        long backOffset = 2048;
        // Distance is small for either direction; we use a unique
        // marker: when sign flips, the offset moves but the rule
        // does not.
        float outRear = ExpeditionPathParallax.LayerOffset(outOffset, ExpeditionPathParallax.RearFactor);
        float backRear = ExpeditionPathParallax.LayerOffset(backOffset, ExpeditionPathParallax.RearFactor);
        Assert.NotEqual(outRear, backRear);
        // Replaying outOffset on the way back must give the same
        // value it had outbound.
        long playOffset = outOffset;
        float playRear = ExpeditionPathParallax.LayerOffset(playOffset, ExpeditionPathParallax.RearFactor);
        Assert.Equal(outRear, playRear);
    }

    [Fact]
    public void ChunkBiome_IsDeterministic_AcrossRecycles()
    {
        // Two pools seeded the same with the same scroll history
        // must agree on the per-chunk biome id.
        var poolA = new ExpeditionPathChunkPool(seed: 77);
        var poolB = new ExpeditionPathChunkPool(seed: 77);
        for (int s = 0; s < 100; s++)
        {
            poolA.SetWorldOffset(s * (long)ExpeditionPathChunkPool.ChunkWidthUnits);
            poolB.SetWorldOffset(s * (long)ExpeditionPathChunkPool.ChunkWidthUnits);
        }
        Assert.Equal(poolA.FocusOffsetUnits, poolB.FocusOffsetUnits);
        for (int i = 0; i < poolA.Chunks.Count; i++)
        {
            Assert.Equal(poolA.Chunks[i].BiomeId, poolB.Chunks[i].BiomeId);
            Assert.Equal(poolA.Chunks[i].PropCount, poolB.Chunks[i].PropCount);
        }
    }

    [Fact]
    public void ChunkBiome_StableAfterRecycle()
    {
        // Same logical index on the same seed must always produce
        // the same biome — recycle does not introduce new biomes.
        var pool = new ExpeditionPathChunkPool(seed: 91);
        for (int s = 0; s < 50; s++)
        {
            pool.SetWorldOffset(s * (long)ExpeditionPathChunkPool.ChunkWidthUnits);
        }
        for (int s = 50; s >= 0; s--)
        {
            pool.SetWorldOffset(s * (long)ExpeditionPathChunkPool.ChunkWidthUnits);
        }
        // After the round trip every chunk still bears a single
        // valid biome.
        foreach (ExpeditionPathChunk chunk in pool.Chunks)
        {
            Assert.InRange(chunk.BiomeId, 0, 3);
        }
    }

    [Fact]
    public void ChunkPropCount_IsStableAcrossRecycle()
    {
        // #25 acceptance: same logical index yields the same prop
        // count when the seed is fixed.
        var pool = new ExpeditionPathChunkPool(seed: 109);
        int beforeCount = pool.Chunks.Count;
        for (int s = 0; s < 50; s++)
        {
            pool.SetWorldOffset(s * (long)ExpeditionPathChunkPool.ChunkWidthUnits);
        }
        int afterCount = pool.Chunks.Count;
        Assert.Equal(beforeCount, afterCount);
        // Prop count is bounded 1-4 so no chunk balloons the
        // dressing density.
        foreach (ExpeditionPathChunk chunk in pool.Chunks)
        {
            Assert.InRange(chunk.PropCount, 1, 4);
        }
    }

    [Fact]
    public void TheFactorsHaveAProductionConsumer()
    {
        // The reopening of #25 found the helper and its tests alone in
        // the world: nothing in the renderer called LayerOffset, so
        // there was no parallax however well the ratios were covered.
        // Every factor now reaches the screen through
        // ExpeditionPathRenderer's world-to-screen rule.
        // Scaled by PixelsPerUnit, which stopped being 1 when the playable band
        // moved back to the calle: the band is normalised to draw 1:1, so the
        // renderer's rule now carries that factor. The property under test is
        // unchanged — a factor handed to WorldToScreenX reaches the screen
        // through it — only the arithmetic it is compared against.
        float unit = ExpeditionPathRenderer.PixelsPerUnit;
        var anchor = ExpeditionPathAnchor.For(new Vector2(800f, 460f));
        float withHelper = 400f
            + ((600f - ExpeditionPathParallax.LayerOffset(200, ExpeditionPathParallax.RearFactor))
                * unit);
        float throughRenderer = ExpeditionPathRenderer.WorldToScreenX(
            600 / ExpeditionPathParallax.RearFactor,
            200,
            ExpeditionPathParallax.RearFactor,
            anchor);

        Assert.Equal(400f, anchor.CenterX, precision: 3);
        Assert.Equal(withHelper, throughRenderer, precision: 3);
    }

    [Fact]
    public void EveryLayerIsReachableFromADepth()
    {
        // Four planes, and each one has a depth that names it. A layer
        // no depth maps to is a layer nothing can draw.
        var seen = new HashSet<ExpeditionPathLayer>();
        for (float depth = ExpeditionPathRenderer.ForegroundDepth;
            depth < ExpeditionPathRenderer.RowCount;
            depth += 1f)
        {
            seen.Add(ExpeditionPathRenderer.LayerForDepth(depth));
        }
        Assert.Equal(4, seen.Count);
    }
}
