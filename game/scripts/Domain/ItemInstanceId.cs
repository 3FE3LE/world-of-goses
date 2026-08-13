#nullable enable
using System;

namespace WorldofGoses.Domain;

/// <summary>
/// Stable identifier for a single materialised item instance. The id
/// is the runtime anchor of the future inventory/equipment seam: it
/// survives save/load, persists with the founder, and lets
/// <see cref="PersonalEquipment"/> swap the equipped weapon without
/// losing the previous item's identity. The first concrete use is
/// the founder's materialised weapon introduced in issue #26.
/// </summary>
public readonly record struct ItemInstanceId
{
    public ItemInstanceId(Guid value) { Value = value; }
    public Guid Value { get; }

    /// <summary>Stable label used by the persistence/migration
    /// layer; survives the natural <see cref="Guid"/> form.</summary>
    public string PersistenceLabel => Value.ToString("N");

    public static ItemInstanceId NewId() => new(Guid.NewGuid());

    public static ItemInstanceId From(string persistence)
    {
        if (Guid.TryParseExact(persistence, "N", out var parsed))
        {
            return new ItemInstanceId(parsed);
        }
        return new ItemInstanceId(Guid.Empty);
    }
}
