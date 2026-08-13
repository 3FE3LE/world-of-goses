#nullable enable
namespace WorldofGoses.Domain;

/// <summary>
/// Where a weapon instance was first created. Today only
/// <see cref="FounderMaterialization"/>; the enum is left open for
/// Loot / Craft / Vendor / Equipment sources the future inventory
/// system will introduce.
/// </summary>
public enum WeaponOrigin
{
    FounderMaterialization = 0,
    Loot = 1,
    Craft = 2,
    Vendor = 3,
}

/// <summary>
/// One materialised weapon item. The runtime has its own identity
/// (<see cref="Id"/>), its own channel profile, and an origin so
/// the inventory/equipment seam can distinguish a Founder's starter
/// from anything picked up later. The store is keyed by
/// <see cref="Id"/>; <see cref="PersonalEquipment"/> owns the
/// equipped id.
/// </summary>
public sealed record WeaponItemInstance(
    ItemInstanceId Id,
    WeaponFamily Family,
    WeaponChannelProfile Channels,
    WeaponOrigin Origin);
