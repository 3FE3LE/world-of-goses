#nullable enable
using System;

namespace WorldofGoses.Domain;

/// <summary>One weapon channel and the five canonical armor support slots.</summary>
public sealed record EquipmentLoadout
{
    public static EquipmentLoadout Empty { get; } = new(
        null,
        GearSupportProfile.None,
        GearSupportProfile.None,
        GearSupportProfile.None,
        GearSupportProfile.None,
        GearSupportProfile.None);

    public EquipmentLoadout(
        WeaponChannelProfile? weapon,
        GearSupportProfile helmet,
        GearSupportProfile chest,
        GearSupportProfile legs,
        GearSupportProfile boots,
        GearSupportProfile gloves,
        StatisticsBalanceConfig? balance = null)
    {
        ArgumentNullException.ThrowIfNull(helmet);
        ArgumentNullException.ThrowIfNull(chest);
        ArgumentNullException.ThrowIfNull(legs);
        ArgumentNullException.ThrowIfNull(boots);
        ArgumentNullException.ThrowIfNull(gloves);
        StatisticsBalanceConfig config = balance ?? StatisticsBalanceConfig.Default;
        config.Validate();
        GearSupportProfile total = helmet + chest + legs + boots + gloves;
        foreach (CubeFace face in Enum.GetValues<CubeFace>())
        {
            if (total.For(face) > config.MaximumGearSupportPerFace)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(helmet),
                    $"Total {face} support cannot exceed {config.MaximumGearSupportPerFace}.");
            }
        }

        Weapon = weapon;
        Helmet = helmet;
        Chest = chest;
        Legs = legs;
        Boots = boots;
        Gloves = gloves;
        TotalGearSupport = total;
    }

    public WeaponChannelProfile? Weapon { get; }
    public GearSupportProfile Helmet { get; }
    public GearSupportProfile Chest { get; }
    public GearSupportProfile Legs { get; }
    public GearSupportProfile Boots { get; }
    public GearSupportProfile Gloves { get; }
    public GearSupportProfile TotalGearSupport { get; }
}
