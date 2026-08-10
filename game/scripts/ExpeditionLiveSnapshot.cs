#nullable enable
using System;
using System.Collections.Generic;
using WorldofGoses.Domain;

namespace WorldofGoses;

/// <summary>
/// Read-only projection for the lateral expedition perspective. It exposes
/// only state already owned by the city and deliberately leaves encounter
/// fields nullable while active expeditions have no linked encounter record.
/// </summary>
public sealed record ExpeditionLiveSnapshot(
    ExpeditionId Id,
    string DisplayName,
    ExpeditionPhase Phase,
    ResourceOpportunityKind? ObjectiveKind,
    int CurrentTick,
    int StartTick,
    int EndTick,
    ExpeditionEncounterOutcome? EncounterOutcome,
    bool RetreatTriggered,
    IReadOnlyList<ExpeditionLiveSnapshot.Member> Members)
{
    public enum RouteStepState
    {
        Pending = 0,
        Active = 1,
        Complete = 2,
        Skipped = 3,
    }

    public sealed record Member(
        CitizenId Id,
        string Name,
        double? HealthRatio,
        int CurrentStamina,
        int EffectiveMaxStamina,
        WoundSeverity? WoundSeverity);

    public double Progress
    {
        get
        {
            int duration = EndTick - StartTick;
            return duration <= 0
                ? 1d
                : Math.Clamp((CurrentTick - StartTick) / (double)duration, 0d, 1d);
        }
    }

    /// <summary>
    /// Projects the linear visual milestones without claiming that a
    /// retreated expedition completed its objective.
    /// </summary>
    public IReadOnlyList<RouteStepState> RouteSteps => ProjectRoute(Phase, RetreatTriggered);

    public static IReadOnlyList<RouteStepState> ProjectRoute(
        ExpeditionPhase phase,
        bool retreatTriggered)
    {
        RouteStepState objective = retreatTriggered
            ? RouteStepState.Skipped
            : RouteStepState.Pending;
        return phase switch
        {
            ExpeditionPhase.Outbound =>
                [RouteStepState.Complete, RouteStepState.Active, RouteStepState.Pending,
                    RouteStepState.Pending, RouteStepState.Pending],
            ExpeditionPhase.Encounter =>
                [RouteStepState.Complete, RouteStepState.Complete, RouteStepState.Active,
                    RouteStepState.Pending, RouteStepState.Pending],
            ExpeditionPhase.Objective =>
                [RouteStepState.Complete, RouteStepState.Complete, RouteStepState.Complete,
                    RouteStepState.Active, RouteStepState.Pending],
            ExpeditionPhase.Retreating =>
                [RouteStepState.Complete, RouteStepState.Complete, RouteStepState.Complete,
                    RouteStepState.Skipped, RouteStepState.Active],
            ExpeditionPhase.Returning or ExpeditionPhase.Resolved =>
                [RouteStepState.Complete, RouteStepState.Complete, RouteStepState.Complete,
                    retreatTriggered ? RouteStepState.Skipped : RouteStepState.Complete,
                    RouteStepState.Active],
            _ =>
                [RouteStepState.Complete, RouteStepState.Active, RouteStepState.Pending,
                    objective, RouteStepState.Pending],
        };
    }

    public static ExpeditionLiveSnapshot? From(CityWorld world, ExpeditionId expeditionId)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (!world.Expeditions.TryGetValue(expeditionId, out Expedition? expedition)
            || expedition.Status != ExpeditionStatus.Active)
        {
            return null;
        }

        var members = new List<Member>(expedition.MemberIds.Count);
        var statistics = new CitizenStatisticsService();
        foreach (CitizenId memberId in expedition.MemberIds)
        {
            Citizen? citizen = world.GetCitizen(memberId);
            if (citizen is null) continue;

            double? healthRatio = null;
            if (citizen.CurrentHealthAndCondition is
                { IsResolved: true, CurrentHealth: double currentHealth })
            {
                double maximumHealth = statistics
                    .CalculateDefense(citizen, citySupportFactor: 1d)
                    .MaxHealth.Value;
                healthRatio = maximumHealth <= 0
                    ? null
                    : Math.Clamp(currentHealth / maximumHealth, 0d, 1d);
            }

            members.Add(new Member(
                citizen.Id,
                citizen.Name,
                healthRatio,
                citizen.CurrentStamina,
                citizen.EffectiveMaxStamina,
                citizen.Wound?.Severity));
        }

        return new ExpeditionLiveSnapshot(
            expedition.Id,
            expedition.DisplayName,
            expedition.Phase,
            expedition.ResourceOpportunityKind,
            world.CurrentTick,
            expedition.StartTick,
            expedition.EndTick,
            expedition.EncounterOutcome,
            expedition.RetreatTriggered,
            members);
    }
}
