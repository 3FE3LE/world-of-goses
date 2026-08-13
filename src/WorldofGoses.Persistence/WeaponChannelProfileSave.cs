using System;
using WorldofGoses.Domain;
namespace WorldofGoses.Persistence;

public sealed class WeaponChannelProfileSave
{
    public string Family { get; set; } = "";
    public double PhysicalTransfer { get; set; }
    public double ElementalResonance { get; set; }

    public static WeaponChannelProfile ToChannel(WeaponChannelProfileSave save)
    {
        if (save is null) return null!;
        return new WeaponChannelProfile(
            Enum.Parse<WeaponFamily>(save.Family),
            save.PhysicalTransfer,
            save.ElementalResonance);
    }

    public static WeaponChannelProfileSave From(WeaponChannelProfile channel)
    {
        return new WeaponChannelProfileSave
        {
            Family = channel.Family.ToString(),
            PhysicalTransfer = channel.PhysicalTransfer,
            ElementalResonance = channel.ElementalResonance,
        };
    }
}
