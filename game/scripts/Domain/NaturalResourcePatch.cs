#nullable enable
using System;
using System.Collections.Generic;

namespace WorldofGoses.Domain;

public sealed class NaturalResourcePatch
{
    public const int MaximumUnits =
        ParcelGrid.ConstructionRowsPerParcel * ParcelGrid.FrontageColumnsPerParcel;
    private readonly List<int> _unitReserves = new();
    private readonly List<NaturalResourceUnitPosition> _unitPositions = new();

    public int Id { get; }
    public ParcelId ParcelId { get; }
    public ResourceType ResourceType { get; }
    public BuildingId? LegacyStorageBuildingId { get; }
    public IReadOnlyList<int> UnitReserves => _unitReserves;
    public IReadOnlyList<NaturalResourceUnitPosition> UnitPositions => _unitPositions;
    public int TotalReserve { get; private set; }

    public NaturalResourcePatch(
        int id,
        ParcelId parcelId,
        ResourceType resourceType,
        IEnumerable<int> unitReserves,
        BuildingId? legacyStorageBuildingId = null,
        IEnumerable<NaturalResourceUnitPosition>? unitPositions = null)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        ArgumentNullException.ThrowIfNull(unitReserves);
        Id = id;
        ParcelId = parcelId;
        ResourceType = resourceType;
        LegacyStorageBuildingId = legacyStorageBuildingId;
        foreach (int reserve in unitReserves)
        {
            int validated = Math.Max(0, reserve);
            _unitReserves.Add(validated);
            TotalReserve += validated;
        }
        if (unitPositions is null)
        {
            for (int unitId = 0; unitId < _unitReserves.Count; unitId++)
            {
                (int lotColumn, int lotRow) = ParcelGrid.NaturalResourceLot(unitId);
                _unitPositions.Add(new NaturalResourceUnitPosition(
                    lotRow,
                    lotColumn * ParcelGrid.TilesPerStandardLot + 1));
            }
        }
        else
        {
            foreach (NaturalResourceUnitPosition position in unitPositions)
            {
                _unitPositions.Add(position.Validate());
            }
        }
        if (_unitPositions.Count != _unitReserves.Count)
        {
            throw new ArgumentException(
                "Every natural-resource unit requires exactly one position.",
                nameof(unitPositions));
        }
        if (new HashSet<NaturalResourceUnitPosition>(_unitPositions).Count
            != _unitPositions.Count)
        {
            throw new ArgumentException(
                "Natural-resource unit positions must be unique within a patch.",
                nameof(unitPositions));
        }
    }

    public int GatherUnit(int unitId, int amount)
    {
        if (amount <= 0 || unitId < 0 || unitId >= _unitReserves.Count) return 0;
        int gathered = Math.Min(amount, _unitReserves[unitId]);
        _unitReserves[unitId] -= gathered;
        TotalReserve -= gathered;
        return gathered;
    }

    public int Gather(int amount)
    {
        if (amount <= 0) return 0;
        int remaining = amount;
        int gathered = 0;
        for (int unitId = 0; unitId < _unitReserves.Count && remaining > 0; unitId++)
        {
            int fromUnit = GatherUnit(unitId, remaining);
            gathered += fromUnit;
            remaining -= fromUnit;
        }
        return gathered;
    }

    /// <summary>
    /// Returns explicitly dropped cargo to this opportunity without creating a
    /// new patch. The least-stocked unit receives it so the same authored set
    /// of ground nodes remains stable and no guaranteed opening material is
    /// destroyed.
    /// </summary>
    internal int Return(int amount)
    {
        if (amount <= 0 || _unitReserves.Count == 0) return 0;
        for (int returned = 0; returned < amount; returned++)
        {
            int targetUnit = 0;
            for (int unitId = 1; unitId < _unitReserves.Count; unitId++)
            {
                if (_unitReserves[unitId] < _unitReserves[targetUnit]) targetUnit = unitId;
            }
            _unitReserves[targetUnit] = checked(_unitReserves[targetUnit] + 1);
            TotalReserve = checked(TotalReserve + 1);
        }
        return amount;
    }

    /// <summary>
    /// Regenerates existing eligible units. Creating another obstacle requires
    /// CityWorld to allocate a globally free position through the layout planner.
    /// Returns the total reserve added this boundary.
    /// </summary>
    public int Regenerate(
        int amountPerUnit,
        int unitCapacity,
        Func<int, bool> canGrowAtUnit)
    {
        if (amountPerUnit <= 0) throw new ArgumentOutOfRangeException(nameof(amountPerUnit));
        if (unitCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(unitCapacity));
        ArgumentNullException.ThrowIfNull(canGrowAtUnit);

        int added = 0;
        for (int unitId = 0; unitId < _unitReserves.Count; unitId++)
        {
            if (_unitReserves[unitId] >= unitCapacity || !canGrowAtUnit(unitId)) continue;
            int growth = Math.Min(amountPerUnit, unitCapacity - _unitReserves[unitId]);
            _unitReserves[unitId] += growth;
            TotalReserve += growth;
            added += growth;
        }

        return added;
    }
}
