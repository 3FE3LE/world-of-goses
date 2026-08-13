#nullable enable
using System.Collections.Generic;

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
/// Nothing in here persists across the stage lifetime. Chunks are
/// presentation state and die with the stage (issue #22 acceptance).
/// </para>
/// </summary>
public sealed class ExpeditionPathChunkPool
{
    /// <summary>How wide each chunk is in world units.</summary>
    public const float ChunkWidthUnits = 256f;

    /// <summary>How many chunks the recycler keeps in flight.</summary>
    public const int ChunkCount = 7;

    /// <summary>The chunk holding the world offset focus.</summary>
    public const int FocusChunkIndex = 3;

    private readonly ExpeditionPathChunk[] _chunks;
    private long _worldOffsetUnits;
    private long _focusLogicalIndex;

    public ExpeditionPathChunkPool(int seed)
    {
        _chunks = new ExpeditionPathChunk[ChunkCount];
        for (int i = 0; i < ChunkCount; i++)
        {
            long relativeSlots = i - FocusChunkIndex;
            long logicalIndex = -relativeSlots;
            long offsetUnits = relativeSlots * (long)ChunkWidthUnits;
            _chunks[i] = new ExpeditionPathChunk(seed: seed, logicalIndex: logicalIndex, offsetUnits: offsetUnits);
        }
        _focusLogicalIndex = 0;
        _worldOffsetUnits = _chunks[FocusChunkIndex].OffsetUnits;
    }

    /// <summary>All chunks, in array order.</summary>
    public IReadOnlyList<ExpeditionPathChunk> Chunks => _chunks;

    /// <summary>The world offset the focus chunk currently owns.</summary>
    public long FocusOffsetUnits => _worldOffsetUnits;

    /// <summary>Logical index of the focus chunk.</summary>
    public long FocusLogicalIndex => _focusLogicalIndex;

    /// <summary>
    /// Drives the recycler. The input is the desired focus chunk
    /// world offset (1D, monotonic with travel progress). The pool
    /// may move 0, 1 or more chunks at a time depending on how far
    /// the offset advanced.
    /// </summary>
    public void SetWorldOffset(long worldOffsetUnits)
    {
        // Snap to the nearest chunk boundary. Travel.PositionX can be
        // any coordinate; the recycler only owns an exact grid of
        // chunks. Snapping keeps the contract that "two snapshots
        // with the same PositionX produce the same world offset"
        // without inventing partial chunks for sub-boundary offsets.
        long snappedOffset = (worldOffsetUnits / (long)ChunkWidthUnits) * (long)ChunkWidthUnits;
        _worldOffsetUnits = snappedOffset;

        long focusOffset = _chunks[FocusChunkIndex].OffsetUnits;
        long delta = (snappedOffset - focusOffset) / (long)ChunkWidthUnits;
        if (delta == 0) return;
        int sign = delta > 0 ? 1 : -1;
        long steps = System.Math.Abs(delta);
        for (long s = 0; s < steps; s++)
        {
            long step = sign * (long)ChunkWidthUnits;
            for (int i = 0; i < _chunks.Length; i++)
            {
                _chunks[i].OffsetUnits += step;
            }
            _focusLogicalIndex += sign;
            for (int i = 0; i < _chunks.Length; i++)
            {
                long relativeSlots = _chunks[i].OffsetUnits / (long)ChunkWidthUnits;
                _chunks[i].LogicalIndex = _focusLogicalIndex + relativeSlots;
            }
        }
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
    public long LogicalIndex { get; set; }
    public long OffsetUnits { get; set; }
    public int PropCount { get; private set; }

    /// <summary>Stable dressing biome id derived from
    /// <c>(seed, logicalIndex)</c>; issue #25 pins that the same
    /// chunk index on the same seed yields the same biome every
    /// time the chunk is recycled.</summary>
    public int BiomeId { get; private set; }

    public void Reset(int seed, long logicalIndex, long offsetUnits)
    {
        Seed = seed;
        LogicalIndex = logicalIndex;
        OffsetUnits = offsetUnits;
        RecomputeDressing();
    }

    private void RecomputeDressing()
    {
        // Stable hash from seed + logical index. Two chunks with the
        // same seed and index must always yield the same dressing
        // cardinality and the same biome id — this is the seam that
        // lets the chunk pool survive a recycle without ever
        // re-randomising the world.
        int hash = unchecked((int)((Seed * 397) ^ LogicalIndex));
        PropCount = 1 + (hash & 0x3);
        BiomeId = hash & 0x3;
    }
}
