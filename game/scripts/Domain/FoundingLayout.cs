namespace WorldofGoses.Domain;

/// <summary>
/// Stable logical arrival anchor for the empty opening. It identifies a cell,
/// not a building lot, and remains protected from procedural resource seeding.
/// </summary>
public static class FoundingLayout
{
    public const int InitialParcelColumn = 2;
    public const int InitialParcelRow = 0;
    public const int FounderRowWithinParcel = 1;
    public const int FounderFrontageColumnWithinParcel = 4;

    public static NaturalResourceUnitPosition FounderLocalPosition { get; } =
        new(FounderRowWithinParcel, FounderFrontageColumnWithinParcel);

    public static bool IsInitialParcel(CityParcel parcel) =>
        parcel.LogicalColumn == InitialParcelColumn
        && parcel.LogicalRow == InitialParcelRow;
}
