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
        _worldOffsetUnits = worldOffsetUnits;
        long focusOffset = _chunks[FocusChunkIndex].OffsetUnits;
        while (_chunks[FocusChunkIndex].OffsetUnits != worldOffsetUnits)
        {
            long remaining = worldOffsetUnits - _chunks[FocusChunkIndex].OffsetUnits;
            int sign = remaining > 0 ? 1 : -1;
            long step = sign * (long)ChunkWidthUnits;
            for (int i = 0; i < _chunks.Length; i++)
            {
                _chunks[i].OffsetUnits += step;
            }
            _focusLogicalIndex += sign;
            // The chunk that just left the focus window needs a
            // logical index at the opposite end of the new window.
            // Recompute every chunk's logical index from its offset
            // so the set stays contiguous at all times.
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
        RecomputePropCount();
    }

    public int Seed { get; private set; }
    public long LogicalIndex { get; set; }
    public long OffsetUnits { get; set; }
    public int PropCount { get; private set; }

    public void Reset(int seed, long logicalIndex, long offsetUnits)
    {
        Seed = seed;
        LogicalIndex = logicalIndex;
        OffsetUnits = offsetUnits;
        RecomputePropCount();
    }

    private void RecomputePropCount()
    {
        // Deterministic dressing cardinality: each chunk hosts
        // between one and four decorative props, keyed on a stable
        // hash so the same seed + logical index yields the same
        // count across the player's session and across reloads.
        int hash = unchecked((int)((Seed * 397) ^ LogicalIndex));
        PropCount = 1 + (hash & 0x3);
    }
}
