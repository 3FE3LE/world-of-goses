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
    private const double ProvisionalPhysicalTransfer = 1.0;
    private const double ProvisionalElementalResonance = 1.0;

    /// <summary>
    /// Temporary bridge until the astral onboarding materialises the chosen
    /// weapon. It only fills an empty loadout and persists through the existing
    /// Citizen equipment contract.
    /// </summary>
    public static bool EnsureProvisionalFounderWeapon(
        Citizen founder,
        ExpeditionId expeditionId,
        int startTick)
    {
        ArgumentNullException.ThrowIfNull(founder);
        if (founder.EquipmentLoadout.Weapon is not null) return false;

        EquipmentLoadout current = founder.EquipmentLoadout;
        founder.SetEquipmentLoadout(new EquipmentLoadout(
            ProvisionalWeaponFor(expeditionId, startTick),
            current.Helmet,
            current.Chest,
            current.Legs,
            current.Boots,
            current.Gloves));
        return true;
    }

    internal static WeaponChannelProfile ProvisionalWeaponFor(
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

        var service = new CombatExpeditionService();
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
            CombatantState member = service.PrepareSessionMember(citizen);
            party.Add(member);
            plans[member.Id] = new CombatantPlan(
                index,
                Array.Empty<string>(),
                PreferredTargetId: null,
                RetreatWhenBelowThreshold: false);
        }

        var enemies = new List<CombatantState>
        {
            EnemyCatalog.Create(EnemyArchetype.MeleeEnemy, $"expedition.{expedition.Id.Value}.enemy0"),
            EnemyCatalog.Create(EnemyArchetype.RangedEnemy, $"expedition.{expedition.Id.Value}.enemy1"),
        };
        var balance = CombatBalanceConfig.Default;
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
