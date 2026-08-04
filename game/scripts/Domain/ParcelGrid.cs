using System;

namespace WorldofGoses.Domain;

/// <summary>
/// Logical parcel geometry. A parcel contributes nine frontage columns to
/// each of three construction rows. Legacy 3×3 lot helpers remain only for
/// resource anchoring and v24 migration.
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
    public const int FrontageColumnsPerParcel =
        LotsPerAxis * TilesPerStandardLot;
    public const int ConstructionRowsPerParcel = LotsPerAxis;

    public static ConstructionRowId ConstructionRow(int parcelRow, int lotRow)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(parcelRow);
        if (lotRow < 0 || lotRow >= ConstructionRowsPerParcel)
        {
            throw new ArgumentOutOfRangeException(nameof(lotRow));
        }
        return new ConstructionRowId(
            checked(parcelRow * ConstructionRowsPerParcel + lotRow));
    }

    public static int GlobalFrontageColumn(
        int parcelColumn,
        int lotColumn,
        int tileColumnWithinLot = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(parcelColumn);
        if (lotColumn < 0 || lotColumn >= LotsPerAxis)
        {
            throw new ArgumentOutOfRangeException(nameof(lotColumn));
        }
        if (tileColumnWithinLot < 0 || tileColumnWithinLot >= TilesPerStandardLot)
        {
            throw new ArgumentOutOfRangeException(nameof(tileColumnWithinLot));
        }
        return checked(
            parcelColumn * FrontageColumnsPerParcel
            + lotColumn * TilesPerStandardLot
            + tileColumnWithinLot);
    }

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

    public static int NaturalResourceFrontageColumn(int parcelColumn, int unitId)
    {
        return checked(NaturalResourceFrontageStartColumn(parcelColumn, unitId) + 1);
    }

    public static int NaturalResourceFrontageStartColumn(int parcelColumn, int unitId)
    {
        (int lotColumn, _) = NaturalResourceLot(unitId);
        return GlobalFrontageColumn(parcelColumn, lotColumn);
    }

    public static PassageClass ClassifyPassage(int widthInHalfTiles)
    {
        if (widthInHalfTiles < 2) return PassageClass.Blocked;
        if (widthInHalfTiles < 4) return PassageClass.NarrowPassage;
        if (widthInHalfTiles < 6) return PassageClass.Path;
        return PassageClass.OpenSpace;
    }

    public static int HorizontalClearance(
        ObstacleFootprintTemplate left,
        int deliberatelyEmptyHalfTiles,
        ObstacleFootprintTemplate right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        ArgumentOutOfRangeException.ThrowIfNegative(deliberatelyEmptyHalfTiles);
        return checked(
            left.RightClearance
            + deliberatelyEmptyHalfTiles
            + right.LeftClearance);
    }
}
