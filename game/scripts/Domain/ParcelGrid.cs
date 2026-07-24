using System;

namespace WorldofGoses.Domain;

/// <summary>
/// Logical parcel geometry. A parcel contains 3×3 standard lots; every lot is
/// 3×3 visual tiles, represented as 6×6 half-tile cells.
/// </summary>
public static class ParcelGrid
{
    public const int LotsPerAxis = 3;
    public const int TilesPerStandardLot = 3;
    public const int HalfTilesPerTile = 2;
    public const int HalfTilesPerStandardLot =
        TilesPerStandardLot * HalfTilesPerTile;
    public const int HalfTilesPerParcel =
        LotsPerAxis * HalfTilesPerStandardLot;

    public static HalfTileRect StandardLot(int column, int row)
    {
        if (column < 0 || column >= LotsPerAxis)
        {
            throw new ArgumentOutOfRangeException(nameof(column));
        }
        if (row < 0 || row >= LotsPerAxis)
        {
            throw new ArgumentOutOfRangeException(nameof(row));
        }
        return new HalfTileRect(
            column * HalfTilesPerStandardLot,
            row * HalfTilesPerStandardLot,
            HalfTilesPerStandardLot,
            HalfTilesPerStandardLot);
    }

    /// <summary>
    /// Stable lot occupied by a natural-resource unit inside its parcel.
    /// Unit identity survives depletion/regeneration, so the mapping must not
    /// depend on how many sibling units are currently visible.
    /// </summary>
    public static (int Column, int Row) NaturalResourceLot(int unitId)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(unitId);
        int lotIndex = unitId % (LotsPerAxis * LotsPerAxis);
        return (lotIndex % LotsPerAxis, lotIndex / LotsPerAxis);
    }

    public static PassageClass ClassifyPassage(int widthInHalfTiles)
    {
        if (widthInHalfTiles < 1) return PassageClass.Blocked;
        if (widthInHalfTiles == 1) return PassageClass.NarrowPassage;
        if (widthInHalfTiles < 4) return PassageClass.Path;
        if (widthInHalfTiles < 6) return PassageClass.Street;
        return PassageClass.OpenSpace;
    }

    public static int HorizontalClearance(
        BuildingFootprintTemplate left,
        int deliberatelyEmptyHalfTiles,
        BuildingFootprintTemplate right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        ArgumentOutOfRangeException.ThrowIfNegative(deliberatelyEmptyHalfTiles);
        return checked(
            left.RightSetback
            + deliberatelyEmptyHalfTiles
            + right.LeftSetback);
    }
}
