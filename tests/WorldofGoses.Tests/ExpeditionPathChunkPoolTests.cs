using System.Collections.Generic;
using System.Linq;
using WorldofGoses.Ui;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// The recycler behind the infinite path (#22): a fixed set of chunks,
/// recycled forever, whose dressing is a pure function of where they
/// sit on the world grid.
///
/// <para>
/// The reopening of #22 named what these tests had not been asking.
/// The pool moved a chunk's <c>LogicalIndex</c> without re-deriving the
/// dressing from it, so a recycled chunk kept the biome of the index it
/// used to have — the exact contract the seed-plus-index rule exists to
/// provide. Size and continuity were covered; identity was not.
/// </para>
/// </summary>
public class ExpeditionPathChunkPoolTests
{
    private const long ChunkWidth = (long)ExpeditionPathChunkPool.ChunkWidthUnits;

    [Fact]
    public void Pool_KeepsAFixedNumberOfChunksAcrossAnyDistance()
    {
        var pool = new ExpeditionPathChunkPool(seed: 7);
        ExpeditionPathChunk[] identity = pool.Chunks.ToArray();

        for (long offset = 0; offset < 400_000; offset += 997)
        {
            pool.SetWorldOffset(offset);
            Assert.Equal(ExpeditionPathChunkPool.ChunkCount, pool.Chunks.Count);
        }

        // Not merely the same count: the same objects. A pool that
        // allocated a fresh chunk per step would keep the count and
        // still grow the heap forever.
        Assert.Equal(identity, pool.Chunks.ToArray());
    }

    [Fact]
    public void Chunks_StayContiguousAroundTheFocus()
    {
        var pool = new ExpeditionPathChunkPool(seed: 3);
        pool.SetWorldOffset(12_345);

        IReadOnlyList<ExpeditionPathChunk> chunks = pool.Chunks;
        for (int i = 1; i < chunks.Count; i++)
        {
            Assert.Equal(chunks[i - 1].WorldEndUnits, chunks[i].WorldStartUnits);
            Assert.Equal(chunks[i - 1].LogicalIndex + 1, chunks[i].LogicalIndex);
        }
        Assert.Equal(
            pool.FocusOffsetUnits,
            chunks[ExpeditionPathChunkPool.FocusChunkIndex].WorldStartUnits);
    }

    [Fact]
    public void LogicalIndex_IsTheAbsolutePositionOnTheWorldGrid()
    {
        var pool = new ExpeditionPathChunkPool(seed: 11);
        pool.SetWorldOffset(9 * ChunkWidth);

        foreach (ExpeditionPathChunk chunk in pool.Chunks)
        {
            Assert.Equal(chunk.WorldStartUnits / ChunkWidth, chunk.LogicalIndex);
        }
        Assert.Equal(9, pool.FocusLogicalIndex);
    }

    [Fact]
    public void Recycling_ReDerivesTheDressingInTheSameStep()
    {
        // The bug this closes: LogicalIndex moved and the dressing did
        // not, so a chunk wore the biome of the stretch it had left.
        var pool = new ExpeditionPathChunkPool(seed: 42);
        pool.SetWorldOffset(0);
        var expected = new Dictionary<long, (int Biome, int Props)>();
        foreach (ExpeditionPathChunk chunk in pool.Chunks)
        {
            expected[chunk.LogicalIndex] = (chunk.BiomeId, chunk.PropCount);
        }

        pool.SetWorldOffset(60 * ChunkWidth);
        foreach (ExpeditionPathChunk chunk in pool.Chunks)
        {
            var reference = new ExpeditionPathChunk(
                seed: 42,
                logicalIndex: chunk.LogicalIndex,
                offsetUnits: chunk.WorldStartUnits);
            Assert.Equal(reference.BiomeId, chunk.BiomeId);
            Assert.Equal(reference.PropCount, chunk.PropCount);
        }

        // And coming back restores exactly what was there before.
        pool.SetWorldOffset(0);
        foreach (ExpeditionPathChunk chunk in pool.Chunks)
        {
            (int biome, int props) = expected[chunk.LogicalIndex];
            Assert.Equal(biome, chunk.BiomeId);
            Assert.Equal(props, chunk.PropCount);
        }
    }

    [Fact]
    public void Dressing_IsAPureFunctionOfSeedAndIndex_NotOfTheRouteTaken()
    {
        var walked = new ExpeditionPathChunkPool(seed: 5);
        for (long offset = 0; offset <= 40 * ChunkWidth; offset += ChunkWidth / 4)
        {
            walked.SetWorldOffset(offset);
        }
        var jumped = new ExpeditionPathChunkPool(seed: 5);
        jumped.SetWorldOffset(40 * ChunkWidth);

        Assert.Equal(
            jumped.Chunks.Select(c => (c.LogicalIndex, c.BiomeId, c.PropCount)),
            walked.Chunks.Select(c => (c.LogicalIndex, c.BiomeId, c.PropCount)));
    }

    [Fact]
    public void BiomeAndPropCount_DoNotRestateEachOther()
    {
        // They used to read the same two bits of the same hash, so a
        // biome and a prop count were the same fact twice and the
        // dressing repeated on a four-chunk cycle.
        var pool = new ExpeditionPathChunkPool(seed: 19);
        var pairs = new HashSet<(int Biome, int Props)>();
        for (long index = 0; index < 64; index++)
        {
            pool.SetWorldOffset(index * ChunkWidth);
            ExpeditionPathChunk focus =
                pool.Chunks[ExpeditionPathChunkPool.FocusChunkIndex];
            pairs.Add((focus.BiomeId, focus.PropCount));
        }
        Assert.True(pairs.Count > 4, $"Only {pairs.Count} distinct dressings in 64 chunks.");
    }

    [Fact]
    public void ReturnLeg_DoesNotFoldTheGridAroundZero()
    {
        // Integer division truncates toward zero, so -1 / 256 is 0 and
        // a returning party would have seen chunk 0 twice.
        var pool = new ExpeditionPathChunkPool(seed: 2);
        pool.SetWorldOffset(-1);
        Assert.Equal(-1, pool.FocusLogicalIndex);
        pool.SetWorldOffset(-ChunkWidth);
        Assert.Equal(-1, pool.FocusLogicalIndex);
        pool.SetWorldOffset(-ChunkWidth - 1);
        Assert.Equal(-2, pool.FocusLogicalIndex);
    }

    [Fact]
    public void PropWorldX_SpreadsInsideTheChunkAndRepeatsOnReturn()
    {
        var chunk = new ExpeditionPathChunk(seed: 8, logicalIndex: 4, offsetUnits: 4 * ChunkWidth);
        double previous = chunk.WorldStartUnits;
        for (int i = 0; i < chunk.PropCount; i++)
        {
            double worldX = chunk.PropWorldX(i);
            Assert.True(worldX > previous);
            Assert.True(worldX < chunk.WorldEndUnits);
            previous = worldX;
        }

        chunk.Recycle(seed: 8, logicalIndex: 99, offsetUnits: 99 * ChunkWidth);
        chunk.Recycle(seed: 8, logicalIndex: 4, offsetUnits: 4 * ChunkWidth);
        Assert.Equal(
            new ExpeditionPathChunk(8, 4, 4 * ChunkWidth).PropWorldX(0),
            chunk.PropWorldX(0));
    }
}
