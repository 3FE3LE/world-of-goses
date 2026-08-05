#nullable enable
using System;

namespace WorldofGoses.Domain;

/// <summary>
/// The founder's three complementary Kovari Cube axes. Each pair is a
/// complete integer distribution and therefore must sum to 100 exactly.
/// The v0.1 integer representation has a documented tolerance of zero;
/// fractional equipment support is applied only to transient effective faces.
/// </summary>
public sealed record FounderCubeProfile
{
    public FounderCubeProfile(
        int body,
        int bond,
        int stability,
        int impulse,
        int mastery,
        int reach)
    {
        ValidatePair(body, bond, nameof(body), nameof(bond));
        ValidatePair(stability, impulse, nameof(stability), nameof(impulse));
        ValidatePair(mastery, reach, nameof(mastery), nameof(reach));

        Body = body;
        Bond = bond;
        Stability = stability;
        Impulse = impulse;
        Mastery = mastery;
        Reach = reach;
    }

    public int Body { get; }
    public int Bond { get; }
    public int Stability { get; }
    public int Impulse { get; }
    public int Mastery { get; }
    /// <summary>
    /// Canonical mechanical name used by the statistics system. Mastery is
    /// retained as the onboarding-era English translation of Dominio.
    /// </summary>
    public int Domain => Mastery;
    public int Reach { get; }

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
