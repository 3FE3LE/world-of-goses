#nullable enable
using System.Collections.Generic;

namespace WorldofGoses.Domain;

/// <summary>
/// One citizen's wearable/equippable items. Today this is the
/// Founder-only seam introduced in issue #26: it owns the id of the
/// equipped weapon and the (small) registry of item instances the
/// citizen possesses. Future inventory/equipment work will extend
/// it with helmet / chest / legs / boots / gloves ids — the seam
/// already keys on <see cref="ItemInstanceId"/> so that addition
/// stays additive, not invasive.
///
/// <para>
/// <see cref="EquipmentLoadout"/> stays unchanged as the effective
/// projection consumed by Statistics/Combat; building it is the
/// responsibility of <see cref="CitizenEquipmentService"/>.
/// </para>
/// </summary>
public sealed class PersonalEquipment
{
    public PersonalEquipment()
    {
        Items = new Dictionary<ItemInstanceId, WeaponItemInstance>();
    }

    /// <summary>Items the citizen owns, keyed by instance id. Today
    /// only weapons live here.</summary>
    public Dictionary<ItemInstanceId, WeaponItemInstance> Items { get; }

    /// <summary>The id of the weapon currently equipped. <c>null</c>
    /// while no weapon is equipped — the future citizen only carries
    /// this state once onboarding lands; after that, equipment is
    /// always present and <c>null</c> means a real, reachable game
    /// state (e.g. dropped / unequipped).</summary>
    public ItemInstanceId? EquippedWeaponId { get; set; }

    /// <summary>The currently equipped weapon instance, or
    /// <c>null</c> when nothing is equipped.</summary>
    public WeaponItemInstance? EquippedWeapon
    {
        get
        {
            if (EquippedWeaponId is ItemInstanceId id && Items.TryGetValue(id, out var weapon))
            {
                return weapon;
            }
            return null;
        }
    }
}
