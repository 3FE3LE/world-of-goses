#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace WorldofGoses.Domain;

public static class FounderNarrativeScorer
{
    /// <summary>
    /// Calculates the canonical onboarding output. The existing lineage and
    /// elemental scoring paths remain authoritative; the cube is evaluated in
    /// parallel from the selected legacy lineage.
    /// </summary>
    public static FounderOnboardingResult Calculate(
        FounderNarrativeSession session,
        Action<string>? discrepancyLogger = null)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (!session.IsComplete)
        {
            throw new InvalidOperationException("All twelve fragments must be stabilised.");
        }

        var scores = new Dictionary<(FounderScoreAxis Axis, string Value), int>();
        var cubeContributions = new List<ScoreContribution>();
        var answerIds = new List<string>();
        var echoIds = new List<string>();
        string? believedFinalWordId = null;
        string? preservedDetailId = null;

        foreach (FounderNarrativeQuestion question in FounderNarrativeCatalog.Questions)
        {
            session.TryGetAnswer(question.Id, out string choiceId);
            FounderNarrativeChoice choice = question.Choices.First(value => value.Id == choiceId);
            answerIds.Add($"{question.Id}:{choice.Id}");
            if (!string.IsNullOrWhiteSpace(choice.PrologueFragment)) echoIds.Add(choice.PrologueFragment);
            if (question.Id == "word") believedFinalWordId = choice.Id;
            if (question.Id == "detail") preservedDetailId = choice.Id;

            foreach (ScoreContribution contribution in choice.Contributions)
            {
                if (contribution.Axis == FounderScoreAxis.Cube)
                {
                    cubeContributions.Add(contribution);
                    continue;
                }
                var key = (contribution.Axis, Normalize(contribution.ValueId));
                scores[key] = scores.GetValueOrDefault(key) + contribution.Weight;
            }
        }

        // DEC-0013 shadow mode: this is still the exact pre-cube lineage path.
        LineageId lineage = Top(
            FounderScoreAxis.Lineage,
            ProfileCatalog.Lineages.Select(value => value.Id),
            value => value.Value,
            scores);
        ElementalAffinityId legacyElement = Top(
            FounderScoreAxis.Element,
            ProfileCatalog.ElementalAffinities.Select(value => value.Id),
            value => value.ToString(),
            scores);
        ElementalAffinity affinity = CitizenProfile.ToCanonicalAffinity(legacyElement);

        FounderCubeProfile cube = CubeScoring.Recalculate(lineage, cubeContributions);
        LineageId cubeCandidate = CubeScoring.ComputeNearestVertex(cube);
        if (cubeCandidate != lineage)
        {
            string message =
                $"Founder cube shadow mismatch: legacy lineage '{lineage.Value}', cube candidate '{cubeCandidate.Value}'.";
            Trace.TraceWarning(message);
            discrepancyLogger?.Invoke(message);
        }

        return new FounderOnboardingResult(
            lineage,
            affinity,
            cube,
            new FounderNarrativeMemory(
                answerIds,
                believedFinalWordId,
                preservedDetailId,
                echoIds));
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

    private static string Normalize(string value) =>
        value.Replace("_", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();
}
