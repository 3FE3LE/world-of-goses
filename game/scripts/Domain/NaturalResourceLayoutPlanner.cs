#nullable enable
using System;
using System.Collections.Generic;

namespace WorldofGoses.Domain;

/// <summary>
/// Produces deterministic, natural-looking resource scatter without relying on
/// the former three-by-three lot matrix. Results depend only on the persisted
/// founder seed, patch id and occupied cells, so live and restored worlds agree.
/// </summary>
public static class NaturalResourceLayoutPlanner
{
    public static IReadOnlyList<NaturalResourceUnitPosition>? TryAllocate(
        int unitCount,
        int worldSeed,
        int patchId,
        IReadOnlyCollection<NaturalResourceUnitPosition> unavailable)
    {
        if (unitCount < 0) throw new ArgumentOutOfRangeException(nameof(unitCount));
        ArgumentNullException.ThrowIfNull(unavailable);
        if (unitCount == 0) return Array.Empty<NaturalResourceUnitPosition>();

        var blocked = new HashSet<NaturalResourceUnitPosition>(unavailable);
        var candidates = new List<(NaturalResourceUnitPosition Position, int Score)>();
        for (int row = 0; row < ParcelGrid.ConstructionRowsPerParcel; row++)
        {
            for (int column = 0;
                 column < ParcelGrid.FrontageColumnsPerParcel;
                 column++)
            {
                var position = new NaturalResourceUnitPosition(row, column);
                if (blocked.Contains(position)) continue;
                candidates.Add((
                    position,
                    StableScore(worldSeed, patchId, row, column, 0)));
            }
        }
        if (candidates.Count < unitCount) return null;
        candidates.Sort((left, right) =>
        {
            int score = left.Score.CompareTo(right.Score);
            if (score != 0) return score;
            int row = left.Position.RowWithinParcel.CompareTo(right.Position.RowWithinParcel);
            return row != 0
                ? row
                : left.Position.FrontageColumnWithinParcel.CompareTo(
                    right.Position.FrontageColumnWithinParcel);
        });
        var result = new List<NaturalResourceUnitPosition>(unitCount);
        for (int index = 0; index < unitCount; index++)
        {
            result.Add(candidates[index].Position);
        }
        return result;
    }

    public static int ParcelScore(int worldSeed, int patchId, ParcelId parcelId) =>
        StableScore(worldSeed, patchId, parcelId.Value, 0, 0);

    private static int StableScore(int seed, int a, int b, int c, int d)
    {
        unchecked
        {
            uint hash = 2166136261;
            hash = (hash ^ (uint)seed) * 16777619;
            hash = (hash ^ (uint)a) * 16777619;
            hash = (hash ^ (uint)b) * 16777619;
            hash = (hash ^ (uint)c) * 16777619;
            hash = (hash ^ (uint)d) * 16777619;
            return (int)(hash & 0x7fffffff);
        }
    }
}
