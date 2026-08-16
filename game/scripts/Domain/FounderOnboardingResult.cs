#nullable enable
using System;
using System.Collections.Generic;

namespace WorldofGoses.Domain;

/// <summary>The complete and exclusive mechanical output of founder onboarding.</summary>
/// <param name="Aptitudes">
/// The three personal aptitudes the answers scored highest on.
/// </param>
/// <remarks>
/// DEC-0013 lists what onboarding must <em>not</em> produce — weapon
/// preferences, professional affinities, combat style, political orientation,
/// spiritual posture, leadership style, risk profile and traits. Aptitudes are
/// not on that list, and never were: they are how a person learns, not a
/// profession they already hold.
///
/// <para>
/// They were nonetheless swept out with the rest. The questionnaire scores the
/// <see cref="FounderScoreAxis.Aptitude"/> axis in more than thirty places, the
/// scorer accumulated every one of them, and then read only Lineage and
/// Element — so a founder arrived with an empty aptitude list while every
/// generated migrant had three. The founder was the only person in the city who
/// learned nothing faster than anyone else.
/// </para>
/// </remarks>
public sealed record FounderOnboardingResult(
    LineageId Lineage,
    ElementalAffinity ElementalAffinity,
    FounderCubeProfile CubeProfile,
    FounderNarrativeMemory NarrativeMemory,
    IReadOnlyList<AptitudeId>? Aptitudes = null)
{
    /// <summary>How many aptitudes a founder leaves onboarding with.</summary>
    public const int AptitudeCount = 3;

    public IReadOnlyList<AptitudeId> Aptitudes { get; init; } =
        Aptitudes ?? Array.Empty<AptitudeId>();

    /// <summary>
    /// Structural equality over the aptitude list.
    /// </summary>
    /// <remarks>
    /// A record compares a list member by reference, so two runs of the scorer
    /// over the same answers stopped being equal the moment this list existed —
    /// each run allocates its own array. That silently broke the guarantee the
    /// onboarding is built on: the same answers must produce the same founder,
    /// and a save must restore the one it stored.
    /// </remarks>
    public bool Equals(FounderOnboardingResult? other) =>
        other is not null
        && Lineage.Equals(other.Lineage)
        && ElementalAffinity == other.ElementalAffinity
        && EqualityComparer<FounderCubeProfile>.Default.Equals(CubeProfile, other.CubeProfile)
        && EqualityComparer<FounderNarrativeMemory>.Default.Equals(NarrativeMemory, other.NarrativeMemory)
        && AptitudesEqual(Aptitudes, other.Aptitudes);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Lineage);
        hash.Add(ElementalAffinity);
        hash.Add(CubeProfile);
        hash.Add(NarrativeMemory);
        foreach (AptitudeId aptitude in Aptitudes) hash.Add(aptitude);
        return hash.ToHashCode();
    }

    private static bool AptitudesEqual(
        IReadOnlyList<AptitudeId> left,
        IReadOnlyList<AptitudeId> right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left.Count != right.Count) return false;
        for (int index = 0; index < left.Count; index++)
        {
            if (!left[index].Equals(right[index])) return false;
        }
        return true;
    }
}
