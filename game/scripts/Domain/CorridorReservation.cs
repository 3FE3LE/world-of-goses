using System;

namespace WorldofGoses.Domain;

public sealed record CorridorReservation
{
    public int Id { get; }
    public ConstructionRowId RowId { get; }
    public int StartColumn { get; }
    public int FrontageColumns { get; }
    public int EndColumnExclusive => checked(StartColumn + FrontageColumns);

    public CorridorReservation(
        int id,
        ConstructionRowId rowId,
        int startColumn,
        int frontageColumns)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        ArgumentOutOfRangeException.ThrowIfNegative(startColumn);
        if (frontageColumns <= 0) throw new ArgumentOutOfRangeException(nameof(frontageColumns));
        Id = id;
        RowId = rowId;
        StartColumn = startColumn;
        FrontageColumns = frontageColumns;
    }

    public bool ContainsColumn(int column) =>
        column >= StartColumn && column < EndColumnExclusive;

    public bool Overlaps(CorridorReservation other) =>
        RowId == other.RowId
        && StartColumn < other.EndColumnExclusive
        && EndColumnExclusive > other.StartColumn;
}
