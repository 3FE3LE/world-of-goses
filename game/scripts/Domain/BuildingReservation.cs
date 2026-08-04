using System;

namespace WorldofGoses.Domain;

/// <summary>
/// Asset-independent interval of whole frontage columns reserved by one
/// building in a fixed-depth construction row.
/// </summary>
public sealed record BuildingReservation
{
    public const int RequiredDepthRows = 3;
    public const int MinimumFrontageColumns = 3;
    public const int MaximumFrontageColumns = 6;

    public BuildingId BuildingId { get; }
    public ConstructionRowId RowId { get; }
    public int StartColumn { get; }
    public int FrontageColumns { get; }
    public int DepthRows { get; }
    public int BaseFrontageColumns { get; }
    public int LeftExpansionColumns { get; }
    public int RightExpansionColumns { get; }
    public int EndColumnExclusive => checked(StartColumn + FrontageColumns);

    public BuildingReservation(
        BuildingId buildingId,
        ConstructionRowId rowId,
        int startColumn,
        int frontageColumns,
        int depthRows = RequiredDepthRows,
        int baseFrontageColumns = MinimumFrontageColumns,
        int leftExpansionColumns = 0,
        int rightExpansionColumns = 0)
    {
        if (buildingId.Value <= 0) throw new ArgumentOutOfRangeException(nameof(buildingId));
        ArgumentOutOfRangeException.ThrowIfNegative(startColumn);
        if (frontageColumns is < MinimumFrontageColumns or > MaximumFrontageColumns)
        {
            throw new ArgumentOutOfRangeException(nameof(frontageColumns));
        }
        if (depthRows != RequiredDepthRows)
        {
            throw new ArgumentOutOfRangeException(nameof(depthRows));
        }
        if (baseFrontageColumns != MinimumFrontageColumns)
        {
            throw new ArgumentOutOfRangeException(nameof(baseFrontageColumns));
        }
        ArgumentOutOfRangeException.ThrowIfNegative(leftExpansionColumns);
        ArgumentOutOfRangeException.ThrowIfNegative(rightExpansionColumns);
        if (baseFrontageColumns + leftExpansionColumns + rightExpansionColumns
            != frontageColumns)
        {
            throw new ArgumentException(
                "Base frontage plus directional expansions must equal total frontage.");
        }

        BuildingId = buildingId;
        RowId = rowId;
        StartColumn = startColumn;
        FrontageColumns = frontageColumns;
        DepthRows = depthRows;
        BaseFrontageColumns = baseFrontageColumns;
        LeftExpansionColumns = leftExpansionColumns;
        RightExpansionColumns = rightExpansionColumns;
    }

    public bool ContainsColumn(int column) =>
        column >= StartColumn && column < EndColumnExclusive;

    public bool Overlaps(BuildingReservation other) =>
        RowId == other.RowId
        && StartColumn < other.EndColumnExclusive
        && EndColumnExclusive > other.StartColumn;

    public BuildingReservation ExpandLeft() => new(
        BuildingId,
        RowId,
        checked(StartColumn - 1),
        checked(FrontageColumns + 1),
        DepthRows,
        BaseFrontageColumns,
        checked(LeftExpansionColumns + 1),
        RightExpansionColumns);

    public BuildingReservation ExpandRight() => new(
        BuildingId,
        RowId,
        StartColumn,
        checked(FrontageColumns + 1),
        DepthRows,
        BaseFrontageColumns,
        LeftExpansionColumns,
        checked(RightExpansionColumns + 1));
}
