#nullable enable
using System;
using System.Collections.Generic;
using WorldofGoses.Domain;
using WorldofGoses.Prototypes;

namespace WorldofGoses.Ui;

/// <summary>
/// Bounded presentation pool that lets the expedition path feel
/// infinite without ever instantiating nodes per travelled chunk.
/// <see cref="ExpeditionPathChunk"/> instances are created once at
/// construction and recycled forever; the pool survives any travel
/// distance and refuses to grow.
///
/// <para>
/// Conceptually:
/// <code>
///   [A][B][C][D][E]
///          party
/// </code>
/// <br/>
/// As the world offset advances, the chunk that fell behind the
/// focus is recycled ahead: it gets a new logical index, a new
/// world-space offset, and re-derives its deterministic dressing
/// from seed + index. Memory stays flat.
/// </para>
///
/// <para>
/// A chunk's logical index is its absolute position on the world
/// grid — <c>OffsetUnits / ChunkWidthUnits</c>, always. That is what
/// makes <c>seed + logicalIndex -> dressing</c> mean something: walk
/// away from a stretch of path and come back, and the same stretch
/// wears the same biome and the same prop count, because it is the
/// same index. An index counted relative to the focus would have
/// re-dressed the world every time the party moved.
/// </para>
///
/// <para>
/// Nothing in here persists across the stage lifetime. Chunks are
/// presentation state and die with the stage (issue #22 acceptance).
/// </para>
/// </summary>
public sealed class ExpeditionPathChunkPool
{
    /// <summary>How wide each chunk is in world units.</summary>
    /// <summary>
    /// One parcel wide, so a chunk boundary is a parcel boundary.
    /// </summary>
    /// <remarks>
    /// This was 256, a round number unrelated to anything the world is built
    /// from: a parcel is nine tiles of <c>TileUnitPx</c>, so 288. With 256 the
    /// seams drifted across the parcel grid and the path could not read as the
    /// city's own blocks laid end to end.
    /// </remarks>
    public const float ChunkWidthUnits =
        ParcelGrid.LotsPerAxis * ParcelGrid.TilesPerStandardLot * MacroViewConstants.TileUnitPx;

    /// <summary>How many chunks the recycler keeps in flight.</summary>
    public const int ChunkCount = 7;

    /// <summary>The chunk holding the world offset focus.</summary>
    public const int FocusChunkIndex = 3;

    private const long ChunkWidth = (long)ChunkWidthUnits;

    private readonly ExpeditionPathChunk[] _chunks;
    private readonly int _seed;
    private long _worldOffsetUnits;

    public ExpeditionPathChunkPool(int seed)
    {
        _seed = seed;
        _chunks = new ExpeditionPathChunk[ChunkCount];
        for (int i = 0; i < ChunkCount; i++)
        {
            long offsetUnits = (i - FocusChunkIndex) * ChunkWidth;
            _chunks[i] = new ExpeditionPathChunk(
                seed: seed,
                logicalIndex: offsetUnits / ChunkWidth,
                offsetUnits: offsetUnits);
        }
        _worldOffsetUnits = 0;
    }

    /// <summary>All chunks, in array order.</summary>
    public IReadOnlyList<ExpeditionPathChunk> Chunks => _chunks;

    /// <summary>The world offset the focus chunk currently owns.</summary>
    public long FocusOffsetUnits => _worldOffsetUnits;

    /// <summary>Logical index of the focus chunk.</summary>
    public long FocusLogicalIndex => _chunks[FocusChunkIndex].LogicalIndex;

    /// <summary>
    /// Drives the recycler. The input is the desired focus chunk
    /// world offset (1D, monotonic with travel progress). However
    /// far the offset jumps, the work is the same: the window is
    /// recentred once, not stepped chunk by chunk.
    /// </summary>
    public void SetWorldOffset(long worldOffsetUnits)
    {
        // Snap to the nearest chunk boundary. Travel.PositionX can be
        // any coordinate; the recycler only owns an exact grid of
        // chunks. Snapping keeps the contract that "two snapshots
        // with the same PositionX produce the same world offset"
        // without inventing partial chunks for sub-boundary offsets.
        // Floor rather than truncate so the grid does not fold around
        // zero on a return leg: -1 belongs to chunk -1, not chunk 0.
        long focusIndex = FloorDiv(worldOffsetUnits, ChunkWidth);
        _worldOffsetUnits = focusIndex * ChunkWidth;
        if (FocusLogicalIndex == focusIndex) return;

        for (int i = 0; i < _chunks.Length; i++)
        {
            long index = focusIndex + (i - FocusChunkIndex);
            _chunks[i].Recycle(_seed, index, index * ChunkWidth);
        }
    }

    private static long FloorDiv(long value, long divisor)
    {
        long quotient = value / divisor;
        return value % divisor != 0 && (value < 0) != (divisor < 0) ? quotient - 1 : quotient;
    }
}

/// <summary>
/// One window in the recycler. Holds a deterministic dressing that
/// is a pure function of <c>(seed, logicalIndex)</c>; persists in
/// memory only while the chunk is alive.
/// </summary>
public sealed class ExpeditionPathChunk
{
    public ExpeditionPathChunk(int seed, long logicalIndex, long offsetUnits)
    {
        Seed = seed;
        LogicalIndex = logicalIndex;
        OffsetUnits = offsetUnits;
        RecomputeDressing();
    }

    public int Seed { get; private set; }

    /// <summary>
    /// Absolute chunk index on the world grid. Settable only through
    /// <see cref="Recycle"/>: the index and the dressing derived from
    /// it have to move in the same step, or a recycled chunk wears
    /// the previous index's biome — which is exactly how the
    /// deterministic-dressing contract was being broken.
    /// </summary>
    public long LogicalIndex { get; private set; }

    public long OffsetUnits { get; private set; }

    public int PropCount { get; private set; }

    /// <summary>Stable dressing biome id derived from
    /// <c>(seed, logicalIndex)</c>; issue #25 pins that the same
    /// chunk index on the same seed yields the same biome every
    /// time the chunk is recycled.</summary>
    public int BiomeId { get; private set; }

    /// <summary>Moves this chunk to a new place on the world grid and
    /// re-derives its dressing in the same step.</summary>
    public void Recycle(int seed, long logicalIndex, long offsetUnits)
    {
        Seed = seed;
        LogicalIndex = logicalIndex;
        OffsetUnits = offsetUnits;
        RecomputeDressing();
    }

    /// <summary>World-space X where this chunk starts.</summary>
    public long WorldStartUnits => OffsetUnits;

    /// <summary>World-space X where this chunk ends.</summary>
    public long WorldEndUnits => OffsetUnits + (long)ExpeditionPathChunkPool.ChunkWidthUnits;

    /// <summary>
    /// World-space X of prop <paramref name="propIndex"/> inside this
    /// chunk. Props are spread evenly rather than jittered: the
    /// spacing is a pure function of the index, so a chunk that
    /// leaves the window and comes back puts its props back exactly
    /// where they were.
    /// </summary>
    public double PropWorldX(int propIndex)
    {
        if (propIndex < 0 || propIndex >= PropCount)
        {
            throw new ArgumentOutOfRangeException(nameof(propIndex));
        }
        double step = ExpeditionPathChunkPool.ChunkWidthUnits / (PropCount + 1d);
        return WorldStartUnits + step * (propIndex + 1);
    }

    private void RecomputeDressing()
    {
        // Stable hash from seed + logical index. Two chunks with the
        // same seed and index must always yield the same dressing
        // cardinality and the same biome id — this is the seam that
        // lets the chunk pool survive a recycle without ever
        // re-randomising the world.
        // Biome and prop count read different bits of the hash. Reading
        // the same two bits made PropCount a restatement of BiomeId, so
        // every chunk of a given biome carried exactly the same number
        // of props and the dressing repeated on a four-chunk cycle.
        int hash = unchecked((int)((Seed * 397) ^ LogicalIndex));
        hash = unchecked(hash * -1521134295 ^ (hash >>> 13));
        PropCount = 1 + ((hash >>> 5) & 0x3);
        BiomeId = hash & 0x3;
    }
}
