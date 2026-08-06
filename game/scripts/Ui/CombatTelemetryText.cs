#nullable enable
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using WorldofGoses.Domain;
using WorldofGoses.Domain.Combat;

namespace WorldofGoses.Presentation;

/// <summary>
/// Formats the combat domain's own telemetry. It reads
/// <see cref="TechniqueResolution"/> and <see cref="ExpeditionRunResult"/> and
/// recomputes nothing: every number printed here was produced by the domain, which
/// is what makes the output an audit rather than a second opinion.
///
/// <para>
/// Deliberately Godot-free and English-only: this is a developer-facing debug
/// surface, so it is exercisable in tests and never routed through the player
/// translation catalog.
/// </para>
/// </summary>
public static class CombatTelemetryText
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    /// <summary>The full per-technique breakdown the roadmap requires.</summary>
    public static string Describe(TechniqueResolution resolution)
    {
        var text = new StringBuilder();
        text.Append("TECHNIQUE ").Append(resolution.TechniqueId)
            .Append("  step ").Append(resolution.Step.ToString(Invariant))
            .Append("  ").Append(resolution.SourceId)
            .Append(" -> ").Append(resolution.TargetId).Append('\n');
        Row(text, "PhysicalChannelPower", resolution.PhysicalChannelPower);
        Row(text, "ElementalChannelPower", resolution.ElementalChannelPower);
        Row(text, "PhysicalCoefficient", resolution.PhysicalCoefficient);
        Row(text, "ElementalCoefficient", resolution.ElementalCoefficient);
        Row(text, "PhysicalContribution", resolution.PhysicalContribution);
        Row(text, "ElementalContribution", resolution.ElementalContribution);
        Row(text, "RawTechniqueResult", resolution.RawTechniqueResult);
        Row(text, "PhysicalMitigation", resolution.PhysicalMitigation);
        Row(text, "ElementalMitigation", resolution.ElementalMitigation);
        Row(text, "GeneralDamageReduction", resolution.GeneralDamageReduction);
        text.Append("  CriticalResult        ")
            .Append(resolution.CriticalResult ? "yes" : "no").Append('\n');
        Row(text, "FinalResult", resolution.FinalResult);
        text.Append("  ElementalNature       ")
            .Append(resolution.ElementalNature.ToString()).Append('\n');
        text.Append("  AppliedStatuses       ")
            .Append(resolution.AppliedStatuses.Count == 0
                ? "none"
                : string.Join(", ", resolution.AppliedStatuses))
            .Append('\n');
        return text.ToString();
    }

    /// <summary>Why a citizen's ConditionFactor is what it is.</summary>
    public static string Describe(ConditionFactorBreakdown condition)
    {
        var text = new StringBuilder();
        text.Append("CONDITION ").Append(Number(condition.Value));
        if (condition.WasClamped) text.Append(" (clamped from ").Append(Number(condition.Raw)).Append(')');
        text.Append('\n');
        Row(text, "health component", condition.HealthComponent);
        Row(text, "fatigue component", condition.FatigueComponent);
        Row(text, "injury component", condition.InjuryComponent);
        text.Append("  causes                ")
            .Append(string.Join(", ", condition.Causes)).Append('\n');
        return text.ToString();
    }

    /// <summary>The whole expedition, including per-member consequences.</summary>
    public static string Describe(ExpeditionRunResult result)
    {
        var text = new StringBuilder();
        text.Append("EXPEDITION  route=").Append(result.Route)
            .Append("  reachedDestination=").Append(result.ReachedDestination ? "yes" : "no")
            .Append('\n');
        text.Append("  routeDecision       ").Append(result.Route).Append('\n');
        text.Append("  discoveredRoute     ").Append(result.DiscoveredRouteState).Append('\n');
        text.Append("  consumedSupplies    ")
            .Append(result.ConsumedSupplies.ToString(Invariant)).Append('\n');
        text.Append("  encounterOutcomes   ")
            .Append(string.Join(", ", result.EncounterOutcomes)).Append('\n');
        text.Append("  acquiredResources   ")
            .Append(DescribeResources(result.AcquiredResources)).Append('\n');

        foreach (ExpeditionMemberResult member in result.Members)
        {
            text.Append("  MEMBER ").Append(member.DisplayName)
                .Append("  citizen=").Append(member.CitizenId.Value.ToString(Invariant))
                .Append('\n');
            text.Append("    weapon            ")
                .Append(member.WeaponFamily?.ToString() ?? "none").Append('\n');
            text.Append("    health            ")
                .Append(Number(member.RemainingHealth)).Append(" / ")
                .Append(Number(member.MaxHealth)).Append('\n');
            text.Append("    fatigue           ").Append(Number(member.Fatigue)).Append('\n');
            text.Append("    injuries          ")
                .Append(member.Injuries.Count == 0 ? "none" : string.Join(", ", member.Injuries))
                .Append('\n');
            text.Append("    survived          ")
                .Append(member.Survived ? "yes" : "no")
                .Append(member.Incapacitated ? " (incapacitated)" : string.Empty)
                .Append('\n');
            text.Append("    weaponExperience  ")
                .Append(Number(member.WeaponExperience)).Append('\n');
            text.Append("    survivalExperience ")
                .Append(Number(member.SurvivalExperience)).Append('\n');
        }
        return text.ToString();
    }

    /// <summary>Every technique resolution in an encounter, in order.</summary>
    public static string DescribeEncounter(IReadOnlyList<CombatLogEntry> log)
    {
        var text = new StringBuilder();
        foreach (CombatLogEntry entry in log)
        {
            if (entry.Resolution is TechniqueResolution resolution)
            {
                text.Append(Describe(resolution));
                continue;
            }
            text.Append('[').Append(entry.Step.ToString(Invariant)).Append("] ")
                .Append(entry.Kind).Append("  ").Append(entry.ActorId);
            if (entry.TargetId is not null) text.Append(" -> ").Append(entry.TargetId);
            text.Append("  ").Append(entry.Detail).Append('\n');
        }
        return text.ToString();
    }

    private static string DescribeResources(IReadOnlyDictionary<ResourceType, int> resources)
    {
        if (resources.Count == 0) return "none";
        var parts = new List<string>(resources.Count);
        foreach ((ResourceType resource, int amount) in resources)
        {
            parts.Add($"{amount.ToString(Invariant)} {resource}");
        }
        return string.Join(" + ", parts);
    }

    /// <summary>
    /// Column width is one wider than the longest label so the longest of them
    /// ("GeneralDamageReduction") still keeps a separating space from its value.
    /// </summary>
    private const int LabelColumnWidth = 23;

    private static void Row(StringBuilder text, string label, double value) =>
        text.Append("  ").Append(label.PadRight(LabelColumnWidth)).Append(Number(value)).Append('\n');

    private static string Number(double value) => value.ToString("0.####", Invariant);
}
