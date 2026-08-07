#nullable enable
using System;

namespace WorldofGoses.Domain;

/// <summary>
/// The founder's three complementary Kovari Cube axes. Each pair is a
/// complete integer distribution and therefore must sum to 100 exactly.
/// The v0.1 integer representation has a documented tolerance of zero;
/// fractional equipment support is applied only to transient effective faces.
///
/// <para>
/// `Domain` is the canonical name of the third axis (DEC-0013 §2). It used
/// to be carried as `Mastery` in onboarding-era English translation; that
/// alias was retired so the name can be reserved for weapon-family skill
/// tiers (see <see cref="WeaponLearning"/>).
/// </para>
/// </summary>
public sealed record FounderCubeProfile
{
    public FounderCubeProfile(
        int body,
        int bond,
        int stability,
        int impulse,
        int domain,
        int reach)
    {
        ValidatePair(body, bond, nameof(body), nameof(bond));
        ValidatePair(stability, impulse, nameof(stability), nameof(impulse));
        ValidatePair(domain, reach, nameof(domain), nameof(reach));

        Body = body;
        Bond = bond;
        Stability = stability;
        Impulse = impulse;
        Domain = domain;
        Reach = reach;
    }

    public int Body { get; }
    public int Bond { get; }
    public int Stability { get; }
    public int Impulse { get; }
    public int Domain { get; }
    public int Reach { get; }

    /// <summary>
    /// Reads one face by name, mirroring <see cref="EffectiveCubeProfile.For"/>.
    /// The two are deliberately separate: this one is the persisted, immutable
    /// cube, and it is the only cube a citizen's physical expression may be
    /// derived from. Deriving from the effective profile would let a helmet
    /// change someone's nature.
    /// </summary>
    public int For(CubeFace face) => face switch
    {
        CubeFace.Body => Body,
        CubeFace.Bond => Bond,
        CubeFace.Stability => Stability,
        CubeFace.Impulse => Impulse,
        CubeFace.Domain => Domain,
        CubeFace.Reach => Reach,
        _ => throw new ArgumentOutOfRangeException(nameof(face), face, null),
    };

    private static void ValidatePair(int first, int second, string firstName, string secondName)
    {
        if (first is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(firstName, first, "Cube stats must be between 0 and 100.");
        }
        if (second is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(secondName, second, "Cube stats must be between 0 and 100.");
        }
        if (first + second != 100)
        {
            throw new ArgumentException($"Cube pair {firstName}/{secondName} must sum to 100.");
        }
    }
}