#nullable enable
using System;

namespace WorldofGoses.Domain;

/// <summary>
/// Owns the personal-equipment contract for a citizen. Today the
/// only mutable equipment is the equipped weapon, but the seam
/// already keys on <see cref="ItemInstanceId"/> so the future
/// helmet/chest/etc. slots extend additively.
///
/// <para>
/// The service is the single authority over (1) which item id is the
/// equipped one, (2) which item instances exist, and (3) which
/// <see cref="EquipmentLoadout"/> the rest of the system should
/// consume as the effective projection. The five armor slots remain
/// unchanged because no item instances for armor exist yet; once
/// armor items land, this is the seam they hang on.
/// </para>
/// </summary>
public sealed class CitizenEquipmentService
{
    /// <summary>
    /// Registers an item the citizen now owns. Ownership and equipping are
    /// separate steps because a future inventory has items it owns and does
    /// not wear; today every call is followed by <see cref="EquipWeapon"/>.
    /// </summary>
    public void Acquire(Citizen citizen, WeaponItemInstance weapon)
    {
        ArgumentNullException.ThrowIfNull(citizen);
        ArgumentNullException.ThrowIfNull(weapon);
        citizen.PersonalEquipment.Items[weapon.Id] = weapon;
    }

    /// <summary>
    /// Equips an item id the citizen already owns (else throws), and
    /// republishes the effective loadout.
    /// </summary>
    /// <remarks>
    /// The republish is what makes this the single authority rather than one
    /// of two. <see cref="Citizen.EquipmentLoadout"/> is a projection of the
    /// registry; anything that changed the equipped id without rebuilding it
    /// would leave the two disagreeing, and Statistics/Combat read the
    /// projection. Callers never touch <c>SetEquipmentLoadout</c> themselves.
    /// </remarks>
    public void EquipWeapon(Citizen citizen, ItemInstanceId id)
    {
        ArgumentNullException.ThrowIfNull(citizen);
        if (!citizen.PersonalEquipment.Items.ContainsKey(id))
        {
            throw new InvalidOperationException(
                $"Citizen {citizen.Id.Value} does not own item {id.PersistenceLabel}.");
        }
        citizen.PersonalEquipment.EquippedWeaponId = id;
        PublishLoadout(citizen);
    }

    /// <summary>
    /// Rebuilds the effective <see cref="EquipmentLoadout"/> from the
    /// registry, preserving the five armour slots the loadout still owns
    /// directly — no armour item instances exist yet, and inventing them to
    /// round out the model is exactly the speculative work #26 rules out.
    /// </summary>
    public void PublishLoadout(Citizen citizen)
    {
        ArgumentNullException.ThrowIfNull(citizen);
        EquipmentLoadout current = citizen.EquipmentLoadout;
        citizen.SetEquipmentLoadout(new EquipmentLoadout(
            EffectiveWeapon(citizen),
            current.Helmet,
            current.Chest,
            current.Legs,
            current.Boots,
            current.Gloves));
    }

    /// <summary>
    /// Returns the weapon slot for the effective
    /// <see cref="EquipmentLoadout"/>. <c>null</c> when the citizen
    /// has no equipment.
    /// </summary>
    public WeaponChannelProfile? EffectiveWeapon(Citizen citizen)
    {
        ArgumentNullException.ThrowIfNull(citizen);
        WeaponItemInstance? weapon = citizen.PersonalEquipment?.EquippedWeapon;
        return weapon?.Channels;
    }

    /// <summary>
    /// Materialises the founder's starter weapon and equips it, as one step:
    /// a founder who owned an item without wearing it would be a state the
    /// opening cannot produce and nothing else knows how to resolve.
    /// </summary>
    /// <remarks>
    /// The channels are the neutral prototype pair #26 sanctions. The family
    /// is the real chosen one — that is the half that matters, and the half
    /// <c>OpeningBaselineFor</c> used to invent at random.
    /// </remarks>
    public WeaponItemInstance MaterializeStarterWeapon(Citizen citizen, WeaponFamily family)
    {
        ArgumentNullException.ThrowIfNull(citizen);
        var weapon = new WeaponItemInstance(
            ItemInstanceId.NewId(),
            family,
            new WeaponChannelProfile(
                family,
                PrototypePhysicalTransfer,
                PrototypeElementalResonance),
            WeaponOrigin.FounderMaterialization);
        Acquire(citizen, weapon);
        EquipWeapon(citizen, weapon.Id);
        return weapon;
    }

    /// <summary>
    /// Neutral channels for a materialised prototype. Named here rather than
    /// left inline so the weapon catalogue has one home when upgrades and
    /// crafting arrive, and so no scene ever carries these numbers.
    /// </summary>
    public const double PrototypePhysicalTransfer = 1.0;
    public const double PrototypeElementalResonance = 1.0;
}
