#nullable enable
using System;
using System.Collections.Generic;

namespace WorldofGoses.Domain.Combat;

/// <summary>
/// Application seam for the combat expedition slice. It orchestrates the three
/// steps the presentation layer needs — prepare, run, apply — and is the ONLY
/// place that writes consequences back onto a persistent
/// <see cref="Citizen"/>. A scene calls this; a scene never mutates a citizen.
///
/// <para>
/// The party is built from the real citizens: each member's derived statistics
/// come from <see cref="CitizenStatisticsService"/> at preparation time, and each
/// <see cref="CombatantState"/> keeps its <see cref="CitizenId"/> so the result can
/// be written back to the same person. No parallel persistent combatant exists.
/// </para>
/// </summary>
public sealed class CombatExpeditionService
{
    private readonly CitizenStatisticsService _statistics;
    private readonly CompetencyLevelCurve _curve;
    private readonly ExpeditionRun _run;
    private readonly StatisticsBalanceConfig _stats;
    private readonly CombatBalanceConfig _combat;

    public CombatExpeditionService(
        StatisticsBalanceConfig? stats = null,
        CombatBalanceConfig? combat = null)
    {
        _stats = stats ?? StatisticsBalanceConfig.Default;
        _combat = combat ?? CombatBalanceConfig.Default;
        _stats.Validate();
        _combat.Validate();
        _statistics = new CitizenStatisticsService(_stats);
        _curve = new CompetencyLevelCurve(_stats, _combat);
        var statuses = new StatusResolver(_combat);
        var resolver = new TechniqueResolver(
            new DefensiveStatisticsCalculator(_stats),
            statuses,
            _combat);
        _run = new ExpeditionRun(resolver, statuses, _combat);
    }

    /// <summary>
    /// Snapshots a citizen into an encounter-ready combatant. Requires an equipped
    /// weapon: offensive channel power is meaningless without one, and the slice's
    /// preparation step is where the player equips Spear, Staff, Mace or Orb.
    /// </summary>
    public CombatantState PrepareMember(Citizen citizen, double citySupportFactor = 1.0)
        => PrepareMember(citizen, citySupportFactor, oneActiveSkill: false);

    /// <summary>
    /// Prepares the bounded live-view slice: exactly one Active Skill per member.
    /// The broader debug run retains the full modular catalog.
    /// </summary>
    internal CombatantState PrepareSessionMember(
        Citizen citizen,
        double citySupportFactor = 1.0,
        double positionX = 0)
        => PrepareMember(citizen, citySupportFactor, oneActiveSkill: true, positionX);

    private CombatantState PrepareMember(
        Citizen citizen,
        double citySupportFactor,
        bool oneActiveSkill,
        double positionX = 0)
    {
        ArgumentNullException.ThrowIfNull(citizen);
        WeaponChannelProfile weapon = citizen.EquipmentLoadout.Weapon
            ?? throw new InvalidOperationException(
                $"Citizen {citizen.Id.Value} needs an equipped weapon before departing.");

        // Resolve condition from persistent causes so the statistics context is
        // derived, never assigned arbitrarily.
        double maxHealth = MaxHealthOf(citizen, citySupportFactor);
        double currentHealth = citizen.CurrentHealthAndCondition.CurrentHealth ?? maxHealth;
        ConditionFactorBreakdown condition = CombatConditionFactor.Derive(
            currentHealth,
            maxHealth,
            fatigue: 0,
            injuries: Array.Empty<InjuryKind>(),
            _stats,
            _combat);
        citizen.SetCurrentHealthAndCondition(
            new CurrentHealthAndCondition(currentHealth, condition.Value, _stats));

        DerivedStatistics derived = _statistics.Calculate(citizen, citySupportFactor);
        EffectiveCubeProfile effectiveCube = EffectiveCubeProfile.From(
            citizen.CubeProfile,
            citizen.EquipmentLoadout.TotalGearSupport);
        int level = citizen.WeaponSkillLevel(weapon.Family);
        var techniques = new List<TechniqueDefinition>();
        bool activeAdded = false;
        foreach (TechniqueDefinition technique in
            TechniqueCatalog.For(weapon.Family, citizen.CombatNature))
        {
            if (oneActiveSkill && technique.Kind == TechniqueKind.Active)
            {
                if (activeAdded) continue;
                activeAdded = true;
            }
            // Apply the citizen's competency level so evolutions take effect.
            techniques.Add(technique.AtLevel(level, _combat));
        }

        return new CombatantState(
            id: $"citizen.{citizen.Id.Value}",
            displayName: citizen.Name,
            side: CombatSide.Party,
            citizenId: citizen.Id,
            maxHealth: derived.Defense.MaxHealth.Value,
            currentHealth: Math.Min(currentHealth, derived.Defense.MaxHealth.Value),
            physicalChannelPower: derived.Offense.PhysicalChannelPower.Value,
            elementalChannelPower: derived.Offense.ElementalChannelPower.Value,
            physicalMitigation: derived.Defense.PhysicalMitigation.Value,
            elementalMitigation: derived.Defense.ElementalMitigation.Value,
            generalDamageReduction: derived.Defense.GeneralDamageReduction.Value,
            criticalChance: derived.Tempo.CriticalChance.Value,
            attackSpeed: derived.Tempo.AttackSpeed.Value,
            elementalAffinity: citizen.CombatNature.ElementalAffinity,
            physicalExpression: citizen.CombatNature.PhysicalExpression,
            weaponFamily: weapon.Family,
            techniques: techniques,
            spatial: new CombatSpatialState(
                positionX,
                movementSpeed: derived.Tempo.MovementSpeed.Value,
                attackRange: CitizenAttackRange(weapon.Family),
                bodyRadius: _combat.CitizenBodyRadius,
                stability: effectiveCube.Stability,
                impulse: effectiveCube.Impulse,
                facing: CombatFacing.Right));
    }

    private double CitizenAttackRange(WeaponFamily family) => family switch
    {
        WeaponFamily.Staff or WeaponFamily.Orb => _combat.CitizenRangedAttackRange,
        WeaponFamily.Spear => _combat.CitizenSpearAttackRange,
        _ => _combat.CitizenShortAttackRange,
    };

    /// <summary>Prepares, runs and returns the result without touching the citizens.</summary>
    public ExpeditionRunResult Run(
        IReadOnlyList<Citizen> members,
        ExpeditionRunPlan plan,
        double citySupportFactor = 1.0)
    {
        ArgumentNullException.ThrowIfNull(members);
        ArgumentNullException.ThrowIfNull(plan);
        var party = new List<CombatantState>(members.Count);
        foreach (Citizen citizen in members) party.Add(PrepareMember(citizen, citySupportFactor));
        return _run.Run(party, plan);
    }

    /// <summary>
    /// Writes the run's consequences onto the persistent citizens: health, the
    /// derived condition including fatigue and injuries, weapon competency
    /// experience with its level re-derived from the curve, and Survival experience.
    ///
    /// <para>
    /// Identity, competencies and equipment are preserved: nothing here replaces a
    /// citizen or its loadout.
    /// </para>
    /// </summary>
    public void ApplyResult(
        IReadOnlyList<Citizen> members,
        ExpeditionRunResult result)
    {
        ArgumentNullException.ThrowIfNull(members);
        ArgumentNullException.ThrowIfNull(result);

        var byId = new Dictionary<int, Citizen>();
        foreach (Citizen citizen in members) byId[citizen.Id.Value] = citizen;

        foreach (ExpeditionMemberResult member in result.Members)
        {
            if (!byId.TryGetValue(member.CitizenId.Value, out Citizen? citizen)) continue;

            ConditionFactorBreakdown condition = CombatConditionFactor.Derive(
                member.RemainingHealth,
                member.MaxHealth,
                member.Fatigue,
                member.Injuries,
                _stats,
                _combat);
            citizen.SetCurrentHealthAndCondition(new CurrentHealthAndCondition(
                member.RemainingHealth,
                condition.Value,
                _stats));

            if (member.WeaponFamily is WeaponFamily family && member.WeaponExperience > 0)
            {
                CompetencyProgress current =
                    citizen.WeaponCompetencies.TryGetValue(family, out CompetencyProgress? existing)
                        ? existing
                        : new CompetencyProgress(family, _stats.MinimumSkillLevel, 0, _stats);
                citizen.SetWeaponCompetency(current.GrantAndLevel(
                    member.WeaponExperience,
                    citizen.Profile.Lineage,
                    citizen.CombatNature,
                    _curve,
                    learningCeiling: null,
                    _stats));
            }

            if (member.SurvivalExperience > 0)
            {
                // Survival is a profession: no natural/foreign weapon penalty applies.
                citizen.AddExperience(
                    CompetencyId.Survival,
                    (int)Math.Round(member.SurvivalExperience));
            }
        }
    }

    private double MaxHealthOf(Citizen citizen, double citySupportFactor)
    {
        // Health is needed before condition is known, so ask for it at a neutral
        // condition: MaxHealth deliberately ignores the context factors anyway.
        citizen.SetCurrentHealthAndCondition(new CurrentHealthAndCondition(
            citizen.CurrentHealthAndCondition.CurrentHealth ?? _stats.BaseMaxHealth,
            _stats.NeutralConditionFactor,
            _stats));
        return _statistics.CalculateDefense(citizen, citySupportFactor).MaxHealth.Value;
    }
}
