#nullable enable
using System;
using System.Collections.Generic;

namespace WorldofGoses.Domain.Combat;

/// <summary>The one branching decision this fixed expedition offers.</summary>
public enum ExpeditionRoute
{
    /// <summary>Predictable risk and predictable reward.</summary>
    SafeRoute,

    /// <summary>Higher risk, better reward, more fatigue and more supplies spent.</summary>
    ShortRoute,
}

/// <summary>Ordered phases of the fixed run. Not procedural, not a roguelike.</summary>
public enum ExpeditionRunPhase
{
    Preparation,
    Departure,
    SegmentA,
    EncounterA,
    RouteDecision,
    SegmentB,
    EncounterB,
    Destination,
    Return,
    Result,
}

/// <summary>What the player configures before departure.</summary>
public sealed record ExpeditionRunPlan(
    IReadOnlyList<CitizenId> Members,
    IReadOnlyDictionary<string, CombatantPlan> CombatantPlans,
    ExpeditionRoute Route,
    int Supplies,
    ulong Seed);

/// <summary>Per-member consequence of the run, applied later by the application layer.</summary>
public sealed record ExpeditionMemberResult(
    CitizenId CitizenId,
    string DisplayName,
    bool Survived,
    bool Incapacitated,
    double RemainingHealth,
    double MaxHealth,
    double Fatigue,
    IReadOnlyList<InjuryKind> Injuries,
    WeaponFamily? WeaponFamily,
    double WeaponExperience,
    double SurvivalExperience);

/// <summary>
/// The complete, persistent-facing outcome. The scene never writes state; it reads
/// this and an application use case applies it to the real citizens.
/// </summary>
public sealed record ExpeditionRunResult(
    ExpeditionRoute Route,
    bool ReachedDestination,
    IReadOnlyList<ExpeditionMemberResult> Members,
    int ConsumedSupplies,
    IReadOnlyDictionary<ResourceType, int> AcquiredResources,
    IReadOnlyList<CombatOutcome> EncounterOutcomes,
    IReadOnlyList<IReadOnlyList<CombatLogEntry>> CombatLogs,
    string DiscoveredRouteState)
{
    public IEnumerable<ExpeditionMemberResult> Survivors
    {
        get
        {
            foreach (ExpeditionMemberResult member in Members)
            {
                if (member.Survived) yield return member;
            }
        }
    }
}

/// <summary>
/// Route tuning. PROVISIONAL BALANCE — the roadmap only requires that the chosen
/// route change at least one of enemy composition, fatigue, supplies, reward or
/// return risk. This changes all five, so the decision is legible in the result.
/// </summary>
public sealed record ExpeditionRouteProfile(
    IReadOnlyList<EnemyArchetype> SecondEncounter,
    double FatigueMultiplier,
    int SupplyCost,
    int RewardUnits)
{
    public static ExpeditionRouteProfile For(ExpeditionRoute route) => route switch
    {
        ExpeditionRoute.SafeRoute => new ExpeditionRouteProfile(
            new[] { EnemyArchetype.MeleeEnemy, EnemyArchetype.RangedEnemy },
            FatigueMultiplier: 1.0,
            SupplyCost: 2,
            RewardUnits: 4),
        ExpeditionRoute.ShortRoute => new ExpeditionRouteProfile(
            new[] { EnemyArchetype.ResistantEnemy, EnemyArchetype.SupportEnemy, EnemyArchetype.MeleeEnemy },
            FatigueMultiplier: 1.6,
            SupplyCost: 4,
            RewardUnits: 9),
        _ => throw new ArgumentOutOfRangeException(nameof(route), route, null),
    };
}

/// <summary>
/// Orchestrates the fixed expedition: two encounters, one route decision, one
/// destination, one persistent return. Pure domain — it receives ready
/// <see cref="CombatantState"/> instances and returns a result, touching no scene
/// and no persistence.
/// </summary>
public sealed class ExpeditionRun
{
    private readonly TechniqueResolver _techniques;
    private readonly StatusResolver _statuses;
    private readonly CombatBalanceConfig _balance;

    public ExpeditionRun(
        TechniqueResolver techniques,
        StatusResolver statuses,
        CombatBalanceConfig? balance = null)
    {
        _techniques = techniques ?? throw new ArgumentNullException(nameof(techniques));
        _statuses = statuses ?? throw new ArgumentNullException(nameof(statuses));
        _balance = balance ?? CombatBalanceConfig.Default;
        _balance.Validate();
    }

    public ExpeditionRunResult Run(
        IReadOnlyList<CombatantState> party,
        ExpeditionRunPlan plan)
    {
        ArgumentNullException.ThrowIfNull(party);
        ArgumentNullException.ThrowIfNull(plan);
        if (party.Count == 0) throw new ArgumentException("A run needs at least one member.", nameof(party));

        ExpeditionRouteProfile profile = ExpeditionRouteProfile.For(plan.Route);
        var random = new DeterministicRandom(plan.Seed);
        var outcomes = new List<CombatOutcome>();
        var logs = new List<IReadOnlyList<CombatLogEntry>>();
        var techniqueCounts = new Dictionary<string, int>();
        int segmentsTravelled = 0;

        // SegmentA → EncounterA. The first encounter is identical on both routes:
        // the decision comes after it, so it cannot influence this one.
        ApplySegmentFatigue(party, profile, ref segmentsTravelled);
        CombatOutcome first = RunEncounter(
            "EncounterA",
            party,
            new[] { EnemyArchetype.MeleeEnemy, EnemyArchetype.RangedEnemy },
            plan,
            random,
            outcomes,
            logs,
            techniqueCounts);

        bool reachedDestination = false;
        if (first is CombatOutcome.PartyVictory)
        {
            // RouteDecision → SegmentB → EncounterB.
            ApplySegmentFatigue(party, profile, ref segmentsTravelled);
            CombatOutcome second = RunEncounter(
                "EncounterB",
                party,
                profile.SecondEncounter,
                plan,
                random,
                outcomes,
                logs,
                techniqueCounts);
            reachedDestination = second is CombatOutcome.PartyVictory;
        }

        // Return travel still costs the party something, win or lose.
        ApplySegmentFatigue(party, profile, ref segmentsTravelled);
        AssignInjuries(party);

        var resources = new Dictionary<ResourceType, int>();
        if (reachedDestination) resources[ResourceType.Stone] = profile.RewardUnits;

        var members = new List<ExpeditionMemberResult>(party.Count);
        foreach (CombatantState member in party)
        {
            techniqueCounts.TryGetValue(member.Id, out int resolved);
            double weaponExperience = resolved * _balance.ExperiencePerResolvedTechnique
                + CountVictories(outcomes) * _balance.ExperiencePerEncounterCleared;
            members.Add(new ExpeditionMemberResult(
                member.CitizenId ?? default,
                member.DisplayName,
                Survived: member.IsAlive,
                Incapacitated: member.IsDefeated,
                RemainingHealth: member.CurrentHealth,
                MaxHealth: member.MaxHealth,
                Fatigue: member.Fatigue,
                Injuries: new List<InjuryKind>(member.Injuries),
                WeaponFamily: member.WeaponFamily,
                WeaponExperience: member.IsAlive ? weaponExperience : weaponExperience * 0.5,
                SurvivalExperience: segmentsTravelled * _balance.SurvivalExperiencePerSegment));
        }

        return new ExpeditionRunResult(
            plan.Route,
            reachedDestination,
            members,
            profile.SupplyCost,
            resources,
            outcomes,
            logs,
            reachedDestination
                ? $"{plan.Route} surveyed to the destination"
                : $"{plan.Route} broken off before the destination");
    }

    private CombatOutcome RunEncounter(
        string encounterId,
        IReadOnlyList<CombatantState> party,
        IReadOnlyList<EnemyArchetype> composition,
        ExpeditionRunPlan plan,
        IRandomSource random,
        List<CombatOutcome> outcomes,
        List<IReadOnlyList<CombatLogEntry>> logs,
        Dictionary<string, int> techniqueCounts)
    {
        var enemies = new List<CombatantState>(composition.Count);
        for (int index = 0; index < composition.Count; index++)
        {
            enemies.Add(EnemyCatalog.Create(composition[index], $"{encounterId}.enemy{index}"));
        }

        var encounter = new CombatEncounter(
            encounterId,
            party,
            enemies,
            plan.CombatantPlans,
            _techniques,
            _statuses,
            random,
            _balance);
        CombatOutcome outcome = encounter.Resolve();
        outcomes.Add(outcome);
        logs.Add(encounter.Log);

        foreach (CombatLogEntry entry in encounter.Log)
        {
            if (entry.Kind != CombatLogKind.TechniqueResolved) continue;
            techniqueCounts.TryGetValue(entry.ActorId, out int count);
            techniqueCounts[entry.ActorId] = count + 1;
        }

        // Statuses do not travel between encounters; health, fatigue and injuries do.
        foreach (CombatantState member in party)
        {
            member.ReplaceStatuses(Array.Empty<StatusEffect>());
        }
        return outcome;
    }

    private void ApplySegmentFatigue(
        IReadOnlyList<CombatantState> party,
        ExpeditionRouteProfile profile,
        ref int segmentsTravelled)
    {
        segmentsTravelled++;
        foreach (CombatantState member in party)
        {
            if (member.IsDefeated) continue;
            member.AddFatigue(_balance.FatiguePerSegment * profile.FatigueMultiplier);
        }
    }

    /// <summary>
    /// Injuries follow from what the encounters actually did, so they persist on
    /// return. Healing life later will not remove them.
    /// </summary>
    private static void AssignInjuries(IReadOnlyList<CombatantState> party)
    {
        foreach (CombatantState member in party)
        {
            // Fracture is the one expression whose cost is not paid during the
            // encounter. It does nothing to a step; what it does is still be
            // active when the fight ends, and then it follows the citizen out.
            // Checked before health, and independently of it, because a
            // fractured arm does not care that the fight went well.
            bool fractured = false;
            foreach (StatusEffect status in member.Statuses)
            {
                if (status.Id != StatusEffectId.Fracture || !status.IsActive) continue;
                fractured = true;
                break;
            }
            if (fractured) member.AddInjury(InjuryKind.Fracture);

            if (member.IsDefeated)
            {
                member.AddInjury(InjuryKind.TemporaryIncapacitation);
                member.AddInjury(InjuryKind.OpenWound);
                continue;
            }
            if (member.HealthRatio <= 0.35) member.AddInjury(InjuryKind.OpenWound);
            else if (member.HealthRatio < 1.0) member.AddInjury(InjuryKind.Contusion);
        }
    }

    private static int CountVictories(IReadOnlyList<CombatOutcome> outcomes)
    {
        int victories = 0;
        foreach (CombatOutcome outcome in outcomes)
        {
            if (outcome == CombatOutcome.PartyVictory) victories++;
        }
        return victories;
    }
}
