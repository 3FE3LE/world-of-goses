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

    /// <summary>
    /// Whether this encounter is the guided opening and therefore carries the
    /// tutorial stat floor. A property of the route, not of the party: the
    /// Spirit Trail is the first thing a founder ever fights, and it protects
    /// them whether they arrive armed or not.
    /// </summary>
    internal static bool IsOpeningTutorialEncounter(Expedition expedition) =>
        expedition.ResourceOpportunityKind == ResourceOpportunityKind.SpiritTrailSearch
        && expedition.CombatRulesVersion >= CurrentRulesVersion;

    /// <summary>
    /// Whether a citizen has to borrow the deterministic non-persistent
    /// profile because they own no weapon. Compatibility only: a founder
    /// created through onboarding materialises a real one, and a legacy save
    /// that was genuinely unarmed stays unarmed rather than growing a weapon
    /// its player never chose.
    /// </summary>
    internal static bool NeedsLegacyWeaponFallback(Citizen citizen) =>
        citizen.EquipmentLoadout.Weapon is null;

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
                // Two independent questions that used to share one answer
                // (#26). "Which weapon profile does this combatant fight
                // with" is about the citizen; "does the opening protect the
                // player" is about the expedition. Keying both on
                // `Weapon is null` meant that giving the founder a real
                // weapon would silently take the tutorial floor away with it
                // — the fallback and the protection were the same branch, so
                // removing the fallback removed the protection.
                openingBaseline: NeedsLegacyWeaponFallback(citizen)
                    ? OpeningBaselineFor(expedition.Id, expedition.StartTick)
                    : null,
                applyOpeningTutorialBaseline:
                    IsOpeningTutorialEncounter(expedition),
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
