#nullable enable
using System;
using System.Collections.Generic;

namespace WorldofGoses.Domain;

/// <summary>Pure scoring rules for the three complementary Kovari Cube axes.</summary>
public static class CubeScoring
{
    public const int VertexHigh = 60;
    public const int VertexLow = 40;
    public const int MaximumOnboardingShift = 8;

    public const string BodyValueId = "Body";
    public const string BondValueId = "Bond";
    public const string StabilityValueId = "Stability";
    public const string ImpulseValueId = "Impulse";
    public const string DomainValueId = "Domain";
    public const string ReachValueId = "Reach";

    public static FounderCubeProfile ComputeCubeVertex(LineageId lineage)
    {
        if (lineage == LineageId.Ardhen) return Profile(true, true, true);
        if (lineage == LineageId.Eirune) return Profile(true, true, false);
        if (lineage == LineageId.Kovari) return Profile(true, false, true);
        if (lineage == LineageId.Vaelun) return Profile(true, false, false);
        if (lineage == LineageId.Orveth) return Profile(false, true, true);
        if (lineage == LineageId.Myrven) return Profile(false, true, false);
        if (lineage == LineageId.Theryn) return Profile(false, false, true);
        if (lineage == LineageId.Caelith) return Profile(false, false, false);
        throw new ArgumentOutOfRangeException(nameof(lineage), lineage, "Unknown lineage cube vertex.");
    }

    public static FounderCubeProfile ApplyContribution(
        FounderCubeProfile profile,
        ScoreContribution answer)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(answer);
        if (answer.Axis != FounderScoreAxis.Cube) return profile;

        int weight = Math.Clamp(answer.Weight, -MaximumOnboardingShift, MaximumOnboardingShift);
        return Normalize(answer.ValueId) switch
        {
            "body" => WithBody(profile, profile.Body + weight),
            "bond" => WithBody(profile, profile.Body - weight),
            "stability" => WithStability(profile, profile.Stability + weight),
            "impulse" => WithStability(profile, profile.Stability - weight),
            "domain" => WithDomain(profile, profile.Domain + weight),
            "reach" => WithDomain(profile, profile.Domain - weight),
            _ => throw new ArgumentOutOfRangeException(nameof(answer), answer.ValueId, "Unknown cube value id."),
        };
    }

    /// <summary>
    /// Rebuilds from the lineage vertex and clamps the aggregate onboarding
    /// movement to ±8 on each axis. Previous results are never subtracted.
    /// </summary>
    public static FounderCubeProfile Recalculate(
        LineageId lineage,
        IEnumerable<ScoreContribution> contributions)
    {
        ArgumentNullException.ThrowIfNull(contributions);
        int body = 0;
        int stability = 0;
        int domain = 0;
        foreach (ScoreContribution contribution in contributions)
        {
            if (contribution.Axis != FounderScoreAxis.Cube) continue;
            switch (Normalize(contribution.ValueId))
            {
                case "body": body += contribution.Weight; break;
                case "bond": body -= contribution.Weight; break;
                case "stability": stability += contribution.Weight; break;
                case "impulse": stability -= contribution.Weight; break;
                case "domain": domain += contribution.Weight; break;
                case "reach": domain -= contribution.Weight; break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(contributions), contribution.ValueId, "Unknown cube value id.");
            }
        }

        FounderCubeProfile result = ComputeCubeVertex(lineage);
        result = ApplyContribution(result, Cube(BodyValueId, Math.Clamp(body, -MaximumOnboardingShift, MaximumOnboardingShift)));
        result = ApplyContribution(result, Cube(StabilityValueId, Math.Clamp(stability, -MaximumOnboardingShift, MaximumOnboardingShift)));
        return ApplyContribution(result, Cube(DomainValueId, Math.Clamp(domain, -MaximumOnboardingShift, MaximumOnboardingShift)));
    }

    public static LineageId ComputeNearestVertex(FounderCubeProfile profile) =>
        (profile.Body >= profile.Bond, profile.Stability >= profile.Impulse, profile.Domain >= profile.Reach) switch
        {
            (true, true, true) => LineageId.Ardhen,
            (true, true, false) => LineageId.Eirune,
            (true, false, true) => LineageId.Kovari,
            (true, false, false) => LineageId.Vaelun,
            (false, true, true) => LineageId.Orveth,
            (false, true, false) => LineageId.Myrven,
            (false, false, true) => LineageId.Theryn,
            _ => LineageId.Caelith,
        };

    public static string Signature(LineageId lineage)
    {
        if (lineage == LineageId.Ardhen) return "Anclaje";
        if (lineage == LineageId.Eirune) return "Corola";
        if (lineage == LineageId.Kovari) return "Reconfiguración";
        if (lineage == LineageId.Vaelun) return "Rumbo";
        if (lineage == LineageId.Orveth) return "Custodia";
        if (lineage == LineageId.Myrven) return "Adaptación";
        if (lineage == LineageId.Theryn) return "Resonancia";
        if (lineage == LineageId.Caelith) return "Síntesis";
        throw new ArgumentOutOfRangeException(nameof(lineage), lineage, "Unknown lineage signature.");
    }

    /// <summary>
    /// Produces the cube of an ordinary citizen as a deterministic function
    /// of <paramref name="lineage"/> and <paramref name="seed"/>. The vertex
    /// is shifted by ±8 per axis using FNV-1a, the same stable hash the repo
    /// already uses for layout and appearance seeds (see
    /// <c>NaturalResourceLayoutPlanner.StableScore</c> and
    /// <c>CityWorld.StableAppearanceSeed</c>). The result respects the same
    /// pair-invariant and ±8 cap as the onboarding flow: every axis stays in
    /// 32–68, the highest face is always one of the lineage's three, and
    /// two distinct seeds never produce the same cube.
    /// </summary>
    public static FounderCubeProfile GenerateOrdinaryProfile(LineageId lineage, int seed)
    {
        FounderCubeProfile vertex = ComputeCubeVertex(lineage);
        return ApplyContribution(
            ApplyContribution(
                ApplyContribution(
                    vertex,
                    Cube(BodyValueId, Shift(seed, lineage, 0))),
                Cube(StabilityValueId, Shift(seed, lineage, 1))),
            Cube(DomainValueId, Shift(seed, lineage, 2)));
    }

    /// <summary>
    /// FNV-1a-derived integer in [-8, +8]. Uses the same mixing constants as
    /// <c>NaturalResourceLayoutPlanner.StableScore</c> so a citizen's cube
    /// and their natural-resource layout share one stable vocabulary. The
    /// lineage id is a string, so it folds in one byte at a time.
    /// </summary>
    private static int Shift(int seed, LineageId lineage, int axis)
    {
        unchecked
        {
            uint hash = 2166136261;
            hash = (hash ^ (uint)seed) * 16777619;
            for (int i = 0; i < lineage.Value.Length; i++)
            {
                hash = (hash ^ lineage.Value[i]) * 16777619;
            }
            hash = (hash ^ (uint)axis) * 16777619;
            // Map to [-8, +8] inclusive — 17 values that mirror the
            // onboarding's ±8 cap. FNV-1a is uniform enough that the
            // modulo bias on a 17-step range is invisible at the cube
            // granularity.
            return (int)(hash % 17u) - 8;
        }
    }

    private static FounderCubeProfile Profile(bool body, bool stability, bool domain) =>
        new(
            body ? VertexHigh : VertexLow,
            body ? VertexLow : VertexHigh,
            stability ? VertexHigh : VertexLow,
            stability ? VertexLow : VertexHigh,
            domain ? VertexHigh : VertexLow,
            domain ? VertexLow : VertexHigh);

    private static FounderCubeProfile WithBody(FounderCubeProfile profile, int body)
    {
        body = Math.Clamp(body, 0, 100);
        return new(body, 100 - body, profile.Stability, profile.Impulse, profile.Domain, profile.Reach);
    }

    private static FounderCubeProfile WithStability(FounderCubeProfile profile, int stability)
    {
        stability = Math.Clamp(stability, 0, 100);
        return new(profile.Body, profile.Bond, stability, 100 - stability, profile.Domain, profile.Reach);
    }

    private static FounderCubeProfile WithDomain(FounderCubeProfile profile, int domain)
    {
        domain = Math.Clamp(domain, 0, 100);
        return new(profile.Body, profile.Bond, profile.Stability, profile.Impulse, domain, 100 - domain);
    }

    private static ScoreContribution Cube(string valueId, int weight) =>
        new(FounderScoreAxis.Cube, valueId, weight);

    private static string Normalize(string value) =>
        value.Replace("_", string.Empty, StringComparison.Ordinal).ToLowerInvariant();
}