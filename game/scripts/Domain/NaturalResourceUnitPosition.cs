using System;

namespace WorldofGoses.Domain;

/// <summary>
/// One resource unit's compact position inside its parcel. A unit occupies one
/// frontage cell in one construction row; it never claims the surrounding
/// three-by-three building reservation.
/// </summary>
public readonly record struct NaturalResourceUnitPosition(
    int RowWithinParcel,
    int FrontageColumnWithinParcel)
{
    public NaturalResourceUnitPosition Validate()
    {
        if (RowWithinParcel < 0
            || RowWithinParcel >= ParcelGrid.ConstructionRowsPerParcel)
        {
            throw new ArgumentOutOfRangeException(nameof(RowWithinParcel));
        }
        if (FrontageColumnWithinParcel < 0
            || FrontageColumnWithinParcel >= ParcelGrid.FrontageColumnsPerParcel)
        {
            throw new ArgumentOutOfRangeException(nameof(FrontageColumnWithinParcel));
        }
        return this;
    }

    public ConstructionRowId GlobalRow(CityParcel parcel) =>
        ParcelGrid.ConstructionRow(parcel.LogicalRow, RowWithinParcel);

    public int GlobalFrontageColumn(CityParcel parcel) => checked(
        parcel.LogicalColumn * ParcelGrid.FrontageColumnsPerParcel
        + FrontageColumnWithinParcel);
}
