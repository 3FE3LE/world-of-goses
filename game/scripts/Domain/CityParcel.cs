using System;

namespace WorldofGoses.Domain;

public sealed class CityParcel
{
    public ParcelId Id { get; }
    public int LogicalColumn { get; }
    public int LogicalRow { get; }
    public ParcelTerritoryState TerritoryState { get; private set; }
    public bool IsUnlocked => TerritoryState == ParcelTerritoryState.Available;

    public CityParcel(ParcelId id, int logicalColumn, int logicalRow, bool isUnlocked)
        : this(
            id,
            logicalColumn,
            logicalRow,
            isUnlocked ? ParcelTerritoryState.Available : ParcelTerritoryState.Locked)
    {
    }

    public CityParcel(
        ParcelId id,
        int logicalColumn,
        int logicalRow,
        ParcelTerritoryState territoryState)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(logicalColumn);
        ArgumentOutOfRangeException.ThrowIfNegative(logicalRow);
        Id = id;
        LogicalColumn = logicalColumn;
        LogicalRow = logicalRow;
        if (!Enum.IsDefined(territoryState))
        {
            throw new ArgumentOutOfRangeException(nameof(territoryState));
        }
        TerritoryState = territoryState;
    }

    public bool AdvanceTerritory()
    {
        if (TerritoryState == ParcelTerritoryState.Available) return false;
        TerritoryState++;
        return true;
    }

    public void Unlock() => TerritoryState = ParcelTerritoryState.Available;
}
