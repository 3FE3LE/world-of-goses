using System.Linq;
using WorldofGoses.Ui;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// Regression coverage for the chunk recycler introduced in #22.
/// Contract: the chunk count is constant, logical indices stay
/// continuous as the world offset advances, dressing is a pure
/// function of seed + logical index, and travelling an arbitrary
/// distance never grows the collection.
/// </summary>
public class ExpeditionPathChunkPoolTests
{
    [Fact]
    public void Pool_InitialisesWithTheSameChunkCount()
    {
        var pool = new ExpeditionPathChunkPool(seed: 7);
        Assert.Equal(ExpeditionPathChunkPool.ChunkCount, pool.Chunks.Count);
        Assert.Equal(ExpeditionPathChunkPool.ChunkCount, pool.Chunks.Distinct().Count());
    }

    [Fact]
    public void Pool_ScrollsForward_WithoutGrowingTheCollection()
    {
        // The acceptance criterion for #22: simulate a distance
        // vastly larger than the combined chunk widths and confirm
        // the chunk-array size is unchanged.
        var pool = new ExpeditionPathChunkPool(seed: 11);
        int countBefore = pool.Chunks.Count;
        long chunkWidth = (long)ExpeditionPathChunkPool.ChunkWidthUnits;
        for (int step = 0; step < 2000; step++)
        {
            pool.SetWorldOffset(chunkWidth * step);
        }
        Assert.Equal(countBefore, pool.Chunks.Count);
        Assert.Equal(ExpeditionPathChunkPool.ChunkCount, countBefore);
    }

    [Fact]
    public void Pool_LogicalIndicesStayContinuous()
    {
        var pool = new ExpeditionPathChunkPool(seed: 19);
        long chunkWidth = (long)ExpeditionPathChunkPool.ChunkWidthUnits;
        long initial = pool.FocusOffsetUnits;
        // Run a long forward scroll and confirm the chunks' logical
        // indices always occupy a contiguous range of integers (no
        // gaps, no collisions).
        for (int step = 0; step < 200; step++)
        {
            pool.SetWorldOffset(initial + chunkWidth * step);
            var indices = pool.Chunks.Select(c => c.LogicalIndex).OrderBy(i => i).ToList();
            Assert.Equal(
                indices[0] + ExpeditionPathChunkChunkContinuityRangeLength() - 1,
                indices[^1]);
            for (int i = 1; i < indices.Count; i++)
            {
                Assert.Equal(indices[i - 1] + 1, indices[i]);
            }
        }
    }

    [Fact]
    public void Pool_DressingIsDeterministicAcrossSaves()
    {
        // Same (seed, logicalIndex) pair must yield the same
        // prop count — recycling must not introduce session-only
        // variance.
        var poolA = new ExpeditionPathChunkPool(seed: 23);
        var poolB = new ExpeditionPathChunkPool(seed: 23);
        Assert.Equal(poolA.Chunks.Count, poolB.Chunks.Count);
        for (int i = 0; i < poolA.Chunks.Count; i++)
        {
            Assert.Equal(poolA.Chunks[i].PropCount, poolB.Chunks[i].PropCount);
            Assert.Equal(poolA.Chunks[i].LogicalIndex, poolB.Chunks[i].LogicalIndex);
            Assert.Equal(poolA.Chunks[i].OffsetUnits, poolB.Chunks[i].OffsetUnits);
        }
    }

    [Fact]
    public void Pool_ScrollsBackward_AndRecycles()
    {
        var pool = new ExpeditionPathChunkPool(seed: 5);
        long chunkWidth = (long)ExpeditionPathChunkPool.ChunkWidthUnits;
        long initial = pool.FocusOffsetUnits;
        // Move forward first.
        pool.SetWorldOffset(initial + chunkWidth * 50);
        long farOffset = pool.FocusOffsetUnits;
        int countBefore = pool.Chunks.Count;
        // Now come back to the initial focus and confirm the count
        // stays the same.
        for (int step = 50; step >= 0; step--)
        {
            pool.SetWorldOffset(initial + chunkWidth * step);
        }
        Assert.Equal(countBefore, pool.Chunks.Count);
        Assert.Equal(initial, pool.FocusOffsetUnits);
        Assert.True(farOffset > pool.FocusOffsetUnits);
    }

    [Fact]
    public void Pool_LargeJump_DoesNotAllocateNewChunks()
    {
        var pool = new ExpeditionPathChunkPool(seed: 31);
        int countBefore = pool.Chunks.Count;
        long huge = (long)ExpeditionPathChunkPool.ChunkWidthUnits * 10_000L;
        pool.SetWorldOffset(huge);
        Assert.Equal(countBefore, pool.Chunks.Count);
        Assert.Equal(huge, pool.FocusOffsetUnits);
    }

    [Fact]
    public void Pool_NoChunkLeavesItsLogicalIndexRangeInconsistent()
    {
        var pool = new ExpeditionPathChunkPool(seed: 7);
        long chunkWidth = (long)ExpeditionPathChunkPool.ChunkWidthUnits;
        long initial = pool.FocusOffsetUnits;
        // Forward scroll from the starting focus — logical indices
        // must stay a tight window of size ChunkCount after every
        // step.
        for (int step = 0; step < 200; step++)
        {
            pool.SetWorldOffset(initial + chunkWidth * step);
            long min = pool.Chunks.Min(c => c.LogicalIndex);
            long max = pool.Chunks.Max(c => c.LogicalIndex);
            Assert.Equal(ExpeditionPathChunkPool.ChunkCount - 1, max - min);
        }
    }

    [Fact]
    public void Pool_ZeroAdvanceIsANoOp()
    {
        var pool = new ExpeditionPathChunkPool(seed: 41);
        long firstOffset = pool.FocusOffsetUnits;
        var firstIndices = pool.Chunks.Select(c => c.LogicalIndex).ToList();
        pool.SetWorldOffset(firstOffset);
        Assert.Equal(firstOffset, pool.FocusOffsetUnits);
        Assert.Equal(firstIndices, pool.Chunks.Select(c => c.LogicalIndex).ToList());
    }

    private static int ExpeditionPathChunkChunkContinuityRangeLength() =>
        ExpeditionPathChunkPool.ChunkCount;
}
