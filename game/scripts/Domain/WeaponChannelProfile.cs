#nullable enable
using System;

namespace WorldofGoses.Domain;

public sealed record WeaponChannelProfile
{
    public WeaponChannelProfile(
        WeaponFamily family,
        double physicalTransfer,
        double elementalResonance,
        StatisticsBalanceConfig? balance = null)
    {
        StatisticsBalanceConfig config = balance ?? StatisticsBalanceConfig.Default;
        config.Validate();
        if (!Enum.IsDefined(family)) throw new ArgumentOutOfRangeException(nameof(family));
        config.ValidateWeaponChannel(physicalTransfer, nameof(physicalTransfer));
        config.ValidateWeaponChannel(elementalResonance, nameof(elementalResonance));
        Family = family;
        PhysicalTransfer = physicalTransfer;
        ElementalResonance = elementalResonance;
    }

    public WeaponFamily Family { get; }
    public double PhysicalTransfer { get; }
    public double ElementalResonance { get; }
}
