using System;

namespace WorldofGoses.Domain;

public sealed class CityParcel
{
    public ParcelId Id { get; }
    public int LogicalColumn { get; }
    public int LogicalRow { get; }
    public bool IsUnlocked { get; private set; }

    public CityParcel(ParcelId id, int logicalColumn, int logicalRow, bool isUnlocked)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(logicalColumn);
        ArgumentOutOfRangeException.ThrowIfNegative(logicalRow);
        Id = id;
        LogicalColumn = logicalColumn;
        LogicalRow = logicalRow;
        IsUnlocked = isUnlocked;
    }

    public void Unlock() => IsUnlocked = true;
}
