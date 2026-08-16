#nullable enable
using System;

namespace WorldofGoses.Domain;

/// <summary>
/// The two universal channel coefficients of every weapon family.
/// </summary>
/// <remarks>
/// <para>
/// Until now every family shipped <c>1.0 / 1.0</c>, which made the twelve
/// families interchangeable in every calculation that mattered: choosing a
/// Hammer over an Orb changed the technique catalogue and nothing else, so the
/// weapon-family axis existed on paper and cost nothing to ignore.
/// </para>
/// <para>
/// Six of the twelve pairs are canon — they are the reference profiles in
/// <c>docs/systems/statistics-and-combat.md</c> §9.1. The other six are
/// provisional and derived here from their canonical partner: within each
/// physical expression the two natural families sit on opposite sides of the
/// same trade, and every value stays inside the sanctioned
/// <c>[MinimumWeaponChannel, MaximumWeaponChannel]</c> band from §4.3. They are
/// marked in the table below and are the first thing to revisit when the weapon
/// catalogue gains real items.
/// </para>
/// <para>
/// This is a trade, never a total: a family that transfers the body well
/// resonates poorly, and the reverse. Nothing here is a free upgrade.
/// </para>
/// </remarks>
public static class WeaponFamilyChannels
{
    /// <summary>
    /// Physical transfer and elemental resonance for <paramref name="family"/>.
    /// </summary>
    /// <remarks>
    /// | Family | Expression | Physical | Elemental | Source |
    /// | --- | --- | ---: | ---: | --- |
    /// | Mace | Stunning | 1.15 | 0.85 | derived from Orb |
    /// | Orb | Stunning | 0.75 | 1.20 | canon §9.1 (Tovan) |
    /// | Sword | Bleeding | 1.10 | 0.90 | derived from Daggers |
    /// | Daggers | Bleeding | 1.05 | 0.95 | canon §9.1 (Neris) |
    /// | Bow | Poisoning | 0.85 | 1.15 | canon §9.1 (Seyra) |
    /// | Darts | Poisoning | 0.80 | 1.20 | derived from Bow |
    /// | Whip | Paralysis | 0.95 | 1.00 | canon §9.1 (Mira) |
    /// | Gauntlets | Paralysis | 1.10 | 0.90 | derived from Whip |
    /// | Hammer | Fracture | 1.20 | 0.75 | canon §9.1 (Aren) |
    /// | Axe | Fracture | 1.15 | 0.80 | derived from Hammer |
    /// | Spear | Knockdown | 1.10 | 1.00 | canon §9.1 (Vael) |
    /// | Staff | Knockdown | 0.85 | 1.15 | derived from Spear |
    /// </remarks>
    public static (double PhysicalTransfer, double ElementalResonance) For(WeaponFamily family) =>
        family switch
        {
            // Stunning — the concussive pair. The Mace lands the shock through
            // mass; the Orb is the same expression carried by resonance.
            WeaponFamily.Mace => (1.15, 0.85),
            WeaponFamily.Orb => (0.75, 1.20),

            // Bleeding — the edged pair, the tightest trade of the twelve.
            // Both cut; the Sword commits more weight to the swing.
            WeaponFamily.Sword => (1.10, 0.90),
            WeaponFamily.Daggers => (1.05, 0.95),

            // Poisoning — delivery over impact. Neither weapon is meant to
            // hurt on contact; what they carry is.
            WeaponFamily.Bow => (0.85, 1.15),
            WeaponFamily.Darts => (0.80, 1.20),

            // Paralysis — reach against grip. The Whip binds at a distance,
            // the Gauntlets seize the body directly.
            WeaponFamily.Whip => (0.95, 1.00),
            WeaponFamily.Gauntlets => (1.10, 0.90),

            // Fracture — the most bodily pair in the game, and the only one
            // where both families sit near the physical ceiling.
            WeaponFamily.Hammer => (1.20, 0.75),
            WeaponFamily.Axe => (1.15, 0.80),

            // Knockdown — leverage. The Spear drives through; the Staff is the
            // same lever in a caster's hands, which is where its resonance
            // comes from.
            WeaponFamily.Spear => (1.10, 1.00),
            WeaponFamily.Staff => (0.85, 1.15),

            _ => throw new ArgumentOutOfRangeException(nameof(family), family, "Unknown weapon family."),
        };

    /// <summary>
    /// Builds the channel profile of a plain, unmodified weapon of this family.
    /// </summary>
    public static WeaponChannelProfile ProfileFor(
        WeaponFamily family,
        StatisticsBalanceConfig? balance = null)
    {
        (double physical, double elemental) = For(family);
        return new WeaponChannelProfile(family, physical, elemental, balance);
    }
}
