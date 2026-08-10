#nullable enable
using System;
using System.Collections.Generic;

namespace WorldofGoses.Domain.Combat;

/// <summary>
/// Constructs the first observable expedition encounter from persistent world
/// state. No scene chooses participants, equipment, enemies, seed or rules.
/// </summary>
public static class ExpeditionCombatSessionFactory
{
    public const int LegacyRulesVersion = 1;
    public const int CurrentRulesVersion = 2;
    private const double ProvisionalPhysicalTransfer = 1.0;
    private const double ProvisionalElementalResonance = 1.0;
    private static readonly EnemyCatalog.EncounterTuning OpeningEncounterTuning = new(
        HealthFactor: 0.75,
        PowerFactor: 0.08);

    /// <summary>
    /// Deterministic, non-persistent opening baseline used only when the Founder
    /// has no real weapon. It reproduces from expedition identity for save replay
    /// but never mutates or masquerades as the Citizen's equipment loadout.
    /// A real equipped weapon takes precedence in <see cref="Create"/>.
    /// </summary>
    internal static WeaponChannelProfile OpeningBaselineFor(
        ExpeditionId expeditionId,
        int startTick)
    {
        ulong seed = unchecked((ulong)(uint)CityWorld.StableExpeditionSeed(
            expeditionId.Value,
            startTick));
        var random = new DeterministicRandom(seed);
        IReadOnlyList<WeaponFamily> families = TechniqueCatalog.SliceWeaponFamilies;
        return new WeaponChannelProfile(
            families[random.NextInt(families.Count)],
            ProvisionalPhysicalTransfer,
            ProvisionalElementalResonance);
    }

    public static CombatSession Create(
        Expedition expedition,
        IReadOnlyDictionary<CitizenId, Citizen> citizens)
    {
        ArgumentNullException.ThrowIfNull(expedition);
        ArgumentNullException.ThrowIfNull(citizens);

        var balance = CombatBalanceConfig.Default;
        var service = new CombatExpeditionService(combat: balance);
        var party = new List<CombatantState>(expedition.MemberIds.Count);
        var plans = new Dictionary<string, CombatantPlan>();
        for (int index = 0; index < expedition.MemberIds.Count; index++)
        {
            CitizenId citizenId = expedition.MemberIds[index];
            if (!citizens.TryGetValue(citizenId, out Citizen? citizen))
            {
                throw new InvalidOperationException(
                    $"Expedition {expedition.Id.Value} references missing citizen {citizenId.Value}.");
            }
            CombatantState member = service.PrepareSessionMember(
                citizen,
                openingBaseline: citizen.EquipmentLoadout.Weapon is null
                    ? OpeningBaselineFor(expedition.Id, expedition.StartTick)
                    : null,
                applyOpeningTutorialBaseline:
                    citizen.EquipmentLoadout.Weapon is null
                    && expedition.CombatRulesVersion >= CurrentRulesVersion,
                positionX: balance.PartyStartingX + index * balance.PartyStartingSpacing);
            party.Add(member);
            plans[member.Id] = new CombatantPlan(
                index,
                Array.Empty<string>(),
                PreferredTargetId: null,
                RetreatWhenBelowThreshold: false);
        }

        EnemyCatalog.EncounterTuning tuning = expedition.CombatRulesVersion >= CurrentRulesVersion
            ? OpeningEncounterTuning
            : EnemyCatalog.EncounterTuning.Standard;
        var enemies = new List<CombatantState>
        {
            EnemyCatalog.Create(
                EnemyArchetype.MeleeEnemy,
                $"expedition.{expedition.Id.Value}.enemy0",
                positionX: balance.EnemyMeleeStartingX,
                tuning: tuning),
            EnemyCatalog.Create(
                EnemyArchetype.RangedEnemy,
                $"expedition.{expedition.Id.Value}.enemy1",
                positionX: balance.EnemyRangedStartingX,
                tuning: tuning),
        };
        var statuses = new StatusResolver(balance);
        var resolver = new TechniqueResolver(
            new DefensiveStatisticsCalculator(StatisticsBalanceConfig.Default),
            statuses,
            balance);
        ulong seed = unchecked((ulong)(uint)CityWorld.StableExpeditionSeed(
            expedition.Id.Value,
            expedition.StartTick));
        var encounter = new CombatEncounter(
            $"expedition.{expedition.Id.Value}.encounter",
            party,
            enemies,
            plans,
            resolver,
            statuses,
            new DeterministicRandom(seed),
            balance);
        return new CombatSession(encounter);
    }
}
