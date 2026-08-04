using System;

namespace WorldofGoses.Domain;

/// <summary>
/// Stable logical row of three-tile-deep construction frontage. Rows span
/// adjacent parcels and remain independent of projected screen coordinates.
/// </summary>
public readonly record struct ConstructionRowId
{
    public int Value { get; }

    public ConstructionRowId(int value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value);
        Value = value;
    }

    public override string ToString() => Value.ToString();
}
