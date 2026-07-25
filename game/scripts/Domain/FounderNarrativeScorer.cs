#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace WorldofGoses.Domain;

public static class FounderNarrativeScorer
{
    public static FounderNarrativeResult Calculate(FounderNarrativeSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (!session.IsComplete)
        {
            throw new InvalidOperationException("All twelve fragments must be stabilised.");
        }

        var scores = new Dictionary<(FounderScoreAxis Axis, string Value), int>();
        var internalAxes = new Dictionary<FounderScoreAxis, int>();
        var prologue = new List<string>();
        foreach (FounderNarrativeQuestion question in FounderNarrativeCatalog.Questions)
        {
            session.TryGetAnswer(question.Id, out string choiceId);
            FounderNarrativeChoice choice = question.Choices.First(value => value.Id == choiceId);
            if (!string.IsNullOrWhiteSpace(choice.PrologueFragment))
            {
                prologue.Add(choice.PrologueFragment);
            }
            foreach (ScoreContribution contribution in choice.Contributions)
            {
                var key = (contribution.Axis, Normalize(contribution.ValueId));
                scores[key] = scores.GetValueOrDefault(key) + contribution.Weight;
                internalAxes[contribution.Axis] =
                    internalAxes.GetValueOrDefault(contribution.Axis) + contribution.Weight;
            }
        }

        LineageId lineage = Top(
            FounderScoreAxis.Lineage,
            ProfileCatalog.Lineages.Select(value => value.Id),
            value => value.Value,
            scores);
        AptitudeId[] aptitudes = TopMany(
            FounderScoreAxis.Aptitude,
            ProfileCatalog.Aptitudes.Select(value => value.Id),
            value => value.ToString(),
            scores,
            3);
        ProfessionFamilyId[] professions = TopMany(
            FounderScoreAxis.Profession,
            ProfileCatalog.ProfessionFamilies.Select(value => value.Id),
            value => value.ToString(),
            scores,
            3);
        ElementalAffinityId element = Top(
            FounderScoreAxis.Element,
            ProfileCatalog.ElementalAffinities.Select(value => value.Id),
            value => value.ToString(),
            scores);
        CombatStyleId combat = Top(
            FounderScoreAxis.CombatStyle,
            ProfileCatalog.CombatStyles.Select(value => value.Id),
            value => value.ToString(),
            scores);
        WeaponPreferenceId[] weapons = TopMany(
            FounderScoreAxis.Weapon,
            ProfileCatalog.WeaponPreferences.Select(value => value.Id),
            value => value.ToString(),
            scores,
            2);
        PersonalityTraitId[] traits = TopMany(
            FounderScoreAxis.Trait,
            ProfileCatalog.PersonalityTraits.Select(value => value.Id),
            value => value.ToString(),
            scores,
            3);
        SpiritualPostureId spirituality = Top(
            FounderScoreAxis.SpiritualPosture,
            ProfileCatalog.SpiritualPostures.Select(value => value.Id),
            value => value.ToString(),
            scores);
        PoliticalOrientationId politics = Top(
            FounderScoreAxis.PoliticalOrientation,
            ProfileCatalog.PoliticalOrientations.Select(value => value.Id),
            value => value.ToString(),
            scores);

        if (!CitizenProfile.TryCreate(
                lineage,
                GenderId.Feminine,
                aptitudes,
                professions,
                element,
                combat,
                weapons,
                traits,
                politics,
                spirituality,
                out CitizenProfile? profile,
                out string error))
        {
            throw new InvalidOperationException($"Narrative scoring produced an invalid profile: {error}");
        }

        return new FounderNarrativeResult(
            profile!,
            lineage,
            aptitudes,
            professions,
            element,
            combat,
            weapons,
            traits,
            spirituality,
            politics,
            new FounderIdentityProfile(
                TopText(FounderScoreAxis.RiskProfile, "Measured", scores),
                TopText(FounderScoreAxis.LeadershipStyle, "Adaptive", scores),
                internalAxes,
                prologue));
    }

    public static FounderNarrativeResult WithGender(
        FounderNarrativeResult result,
        GenderId gender)
    {
        CitizenProfile.TryCreate(
            result.Lineage,
            gender,
            result.Aptitudes,
            result.ProfessionalAffinities,
            result.Element,
            result.CombatStyle,
            result.WeaponPreferences,
            result.Traits,
            result.PoliticalOrientation,
            result.SpiritualPosture,
            out CitizenProfile? profile,
            out string error);
        return profile is null
            ? throw new InvalidOperationException(error)
            : result with { Profile = profile };
    }

    private static T Top<T>(
        FounderScoreAxis axis,
        IEnumerable<T> values,
        Func<T, string> id,
        IReadOnlyDictionary<(FounderScoreAxis Axis, string Value), int> scores) =>
        values
            .Select((value, index) => new
            {
                Value = value,
                Index = index,
                Score = scores.GetValueOrDefault((axis, Normalize(id(value)))),
            })
            .OrderByDescending(value => value.Score)
            .ThenBy(value => value.Index)
            .First().Value;

    private static T[] TopMany<T>(
        FounderScoreAxis axis,
        IEnumerable<T> values,
        Func<T, string> id,
        IReadOnlyDictionary<(FounderScoreAxis Axis, string Value), int> scores,
        int count) =>
        values
            .Select((value, index) => new
            {
                Value = value,
                Index = index,
                Score = scores.GetValueOrDefault((axis, Normalize(id(value)))),
            })
            .OrderByDescending(value => value.Score)
            .ThenBy(value => value.Index)
            .Take(count)
            .Select(value => value.Value)
            .ToArray();

    private static string TopText(
        FounderScoreAxis axis,
        string fallback,
        IReadOnlyDictionary<(FounderScoreAxis Axis, string Value), int> scores)
    {
        var candidates = scores
            .Where(value => value.Key.Axis == axis)
            .OrderByDescending(value => value.Value)
            .ThenBy(value => value.Key.Value, StringComparer.Ordinal)
            .ToArray();
        return candidates.Length == 0 ? fallback : candidates[0].Key.Value;
    }

    private static string Normalize(string value) =>
        value.Replace("_", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();
}
