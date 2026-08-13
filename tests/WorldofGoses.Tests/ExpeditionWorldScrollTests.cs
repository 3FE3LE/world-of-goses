using WorldofGoses.Ui;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// #23 contract: <see cref="Travel.PositionX"/> drives the world
/// scroll. Two snapshots with the same PositionX produce the same
/// world offset; the recycler never grows. We exercise the link
/// through the chunk pool (the presentation sink) and through the
/// pure mapping function the stage uses, so the tests stay
/// Godot-free.
/// </summary>
public class ExpeditionWorldScrollTests
{
    [Fact]
    public void TwoSnapshotsSamePositionX_ProduceSameWorldOffset()
    {
        // Same authoritative 1D value should yield the same chunk
        // pool state. A presentation-only mapping must be a pure
        // function of the snapshot.
        long positionA = 640;
        long positionB = 640;
        var poolA = MakeScrolledPool(positionA, seed: 7);
        var poolB = MakeScrolledPool(positionB, seed: 7);
        Assert.Equal(poolA.FocusOffsetUnits, poolB.FocusOffsetUnits);
    }

    [Fact]
    public void WorldOffset_IsMonotonicWithPositionX()
    {
        // Outbound → larger offset; return → smaller. The pool
        // never invents an offset that contradicts the input.
        long initial = 0;
        var pool = new ExpeditionPathChunkPool(seed: 1);
        pool.SetWorldOffset(initial);
        long chunk = (long)ExpeditionPathChunkPool.ChunkWidthUnits;

        long outbound = initial + chunk * 30;
        pool.SetWorldOffset(outbound);
        Assert.True(pool.FocusOffsetUnits > initial);
        Assert.Equal(outbound, pool.FocusOffsetUnits);

        long turningPoint = outbound + chunk * 10;
        pool.SetWorldOffset(turningPoint);
        Assert.Equal(turningPoint, pool.FocusOffsetUnits);

        long returning = turningPoint - chunk * 8;
        pool.SetWorldOffset(returning);
        Assert.Equal(returning, pool.FocusOffsetUnits);
        Assert.True(returning < turningPoint);
    }

    [Fact]
    public void HundredsOfPositionXUpdates_DoNotGrowThePool()
    {
        // The acceptance criterion for #23: assert no growth across
        // a long outbound + multiple chunk recycles + a return.
        var pool = new ExpeditionPathChunkPool(seed: 19);
        long chunk = (long)ExpeditionPathChunkPool.ChunkWidthUnits;
        long initial = pool.FocusOffsetUnits;
        int initialCount = pool.Chunks.Count;
        // Outbound: a thousand chunks of travel.
        for (int step = 1; step <= 1000; step++)
        {
            pool.SetWorldOffset(initial + chunk * step);
        }
        Assert.Equal(initialCount, pool.Chunks.Count);

        long destination = pool.FocusOffsetUnits;
        // Stay at the objective.
        pool.SetWorldOffset(destination);
        Assert.Equal(destination, pool.FocusOffsetUnits);

        // Return: half-way back.
        for (int step = 999; step >= 500; step--)
        {
            pool.SetWorldOffset(initial + chunk * step);
        }
        Assert.Equal(initialCount, pool.Chunks.Count);
    }

    [Fact]
    public void ObjectiveMarker_StaysAtItsLogicalPosition_ThroughTheRecycle()
    {
        // The Spirit Trail objective also maps through PositionX;
        // after a long scroll + a return, asking the same PositionX
        // back must place it on the same chunk index it had on the
        // outbound leg. This protects the markers attached to a
        // chunk from drifting when chunks recycle.
        var pool = new ExpeditionPathChunkPool(seed: 13);
        long chunk = (long)ExpeditionPathChunkPool.ChunkWidthUnits;
        long initial = pool.FocusOffsetUnits;
        long objective = initial + chunk * 25;
        pool.SetWorldOffset(objective);
        long chunkIndexAtObjective = FindChunkIndexForOffset(pool, objective);

        // Scroll past, then return.
        for (int s = 26; s <= 200; s++)
            pool.SetWorldOffset(initial + chunk * s);
        for (int s = 200; s >= 25; s--)
            pool.SetWorldOffset(initial + chunk * s);

        long chunkIndexAfterReturn = FindChunkIndexForOffset(pool, objective);
        Assert.Equal(chunkIndexAtObjective, chunkIndexAfterReturn);
    }

    private static long FindChunkIndexForOffset(ExpeditionPathChunkPool pool, long offsetUnits)
    {
        long relative = offsetUnits / (long)ExpeditionPathChunkPool.ChunkWidthUnits;
        return pool.FocusLogicalIndex + relative;
    }

    private static ExpeditionPathChunkPool MakeScrolledPool(long positionX, int seed)
    {
        var pool = new ExpeditionPathChunkPool(seed);
        pool.SetWorldOffset(positionX);
        return pool;
    }
}
