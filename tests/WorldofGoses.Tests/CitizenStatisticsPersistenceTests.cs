using System;
using System.Linq;
using WorldofGoses.Domain;
using WorldofGoses.Domain.Persistence;
using Xunit;

namespace WorldofGoses.Tests;

public sealed class CitizenStatisticsPersistenceTests
{
    [Fact]
    public void MigrateV29ToV30_CreatesDeterministicCitizenFallbackWithoutPersistingDerivedStats()
    {
        WorldSave save = WorldPersistence.Capture(TestHelpers.NewHeroWorld());
        save.Version = 29;
        CitizenSave citizen = Assert.Single(save.Citizens);
        citizen.Origin = CitizenOrigin.Mortal.ToString();
        citizen.Profile!.Lineage = LineageId.Kovari.Value;
        citizen.Profile.ElementalAffinity = "fire";
        citizen.Profile.CubeProfile = null;
        citizen.Profile.NarrativeMemory = null;
        citizen.WeaponCompetencies = null!;
        citizen.EquipmentLoadout = null;
        citizen.CurrentHealthAndCondition = null;

        WorldSave migrated = WorldPersistence.MigrateV29ToV30(save);

        Assert.Equal(30, migrated.Version);
        Assert.NotNull(citizen.Profile.CubeProfile);
        Assert.Equal(60, citizen.Profile.CubeProfile!.Body);
        Assert.Equal(60, citizen.Profile.CubeProfile.Impulse);
        Assert.NotNull(citizen.EquipmentLoadout);
        Assert.Empty(citizen.WeaponCompetencies);
        Assert.NotNull(citizen.CurrentHealthAndCondition);
        Assert.Equal(1, citizen.CurrentHealthAndCondition!.ConditionFactor!.Value);

        // MigrateToCurrent for the tail rather than another hand-chained call:
        // FromSave validates against CurrentVersion, and a fixed chain has to be
        // edited on every new schema version.
        CityWorld restored = CityWorld.FromSave(WorldPersistence.MigrateToCurrent(migrated));
        Citizen restoredCitizen = Assert.Single(restored.Citizens.Values);
        Assert.Equal(PhysicalExpression.Stunning, restoredCitizen.CombatNature.PhysicalExpression);
        Assert.Equal(EquipmentLoadout.Empty, restoredCitizen.EquipmentLoadout);
    }

    [Fact]
    public void V30RoundTrip_PreservesSourcesAndRecalculatesSameStatistics()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        Citizen hero = world.Hero!;
        var support = new GearSupportProfile(2, 1, 3, 0, 4, 1);
        var loadout = new EquipmentLoadout(
            new WeaponChannelProfile(WeaponFamily.Whip, 0.95, 1.10),
            support,
            GearSupportProfile.None,
            GearSupportProfile.None,
            GearSupportProfile.None,
            GearSupportProfile.None);
        hero.SetEquipmentLoadout(loadout);
        hero.SetWeaponCompetency(new CompetencyProgress(WeaponFamily.Whip, 10, 47.5));
        hero.SetCurrentHealthAndCondition(new CurrentHealthAndCondition(150, 0.75));
        DerivedStatistics before = new CitizenStatisticsService().Calculate(hero, 1.05);

        string json = WorldPersistence.SerializeToJson(WorldPersistence.Capture(world));
        Assert.DoesNotContain("PhysicalExpression", json);
        Assert.DoesNotContain("PhysicalChannelPower", json);
        CityWorld restored = CityWorld.FromSave(WorldPersistence.DeserializeFromJson(json));
        Citizen restoredHero = restored.Hero!;
        DerivedStatistics after = new CitizenStatisticsService().Calculate(restoredHero, 1.05);

        Assert.Equal(hero.CubeProfile, restoredHero.CubeProfile);
        Assert.Equal(hero.CombatNature, restoredHero.CombatNature);
        Assert.Equal(loadout, restoredHero.EquipmentLoadout);
        CompetencyProgress competency = Assert.Single(restoredHero.WeaponCompetencies).Value;
        Assert.Equal(10, competency.Level);
        Assert.Equal(47.5, competency.Experience);
        Assert.Equal(150, restoredHero.CurrentHealthAndCondition.CurrentHealth!.Value);
        Assert.Equal(before.Offense.PhysicalChannelPower.Value, after.Offense.PhysicalChannelPower.Value, 10);
        Assert.Equal(before.Offense.ElementalChannelPower.Value, after.Offense.ElementalChannelPower.Value, 10);
        Assert.Equal(before.Defense.MaxHealth.Value, after.Defense.MaxHealth.Value, 10);
        Assert.Equal(before.Defense.PhysicalMitigation.Value, after.Defense.PhysicalMitigation.Value, 10);
        Assert.Equal(before.Recovery.HealingAppliedPercent.Value, after.Recovery.HealingAppliedPercent.Value, 10);
        Assert.Equal(before.Tempo.AttackSpeed.Value, after.Tempo.AttackSpeed.Value, 10);
    }

    [Fact]
    public void MigratedHealthUsesCubeAndEmptyLoadoutWithoutReplayingOnboarding()
    {
        WorldSave save = WorldPersistence.Capture(TestHelpers.NewHeroWorld());
        save.Version = 29;
        CitizenSave citizen = Assert.Single(save.Citizens);
        string[] answerIds = citizen.Profile!.NarrativeMemory!.AnswerIds.ToArray();
        citizen.CurrentHealthAndCondition = null;
        citizen.EquipmentLoadout = null;

        WorldSave migrated = WorldPersistence.MigrateV29ToV30(save);
        CityWorld restored = CityWorld.FromSave(WorldPersistence.MigrateToCurrent(migrated));

        Assert.Equal(answerIds, restored.Hero!.Profile.FounderOnboardingResult!.NarrativeMemory.AnswerIds);
        DefensiveStatistics defense = new DefensiveStatisticsCalculator(StatisticsBalanceConfig.Default)
            .Calculate(restored.Hero.CubeProfile, EquipmentLoadout.Empty, new StatCalculationContext(0, 1, 1));
        Assert.Equal(defense.MaxHealth.Value, restored.Hero.CurrentHealthAndCondition.CurrentHealth!.Value, precision: 10);
    }

    [Fact]
    public void MigrateV29ToV30_WoundedCitizenRemainsExplicitlyUnresolved()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        Citizen hero = world.Hero!;
        WorldEvent origin = world.Log.Record(
            world.CurrentTick,
            WorldEventKind.WoundSustained,
            WorldEventSubject.Citizen(hero.Id, hero.Name),
            (int)WoundSeverity.Moderate);
        hero.SustainWound(WoundSeverity.Moderate, origin.Id);
        WorldSave save = WorldPersistence.Capture(world);
        save.Version = 29;
        CitizenSave citizen = Assert.Single(save.Citizens);
        citizen.CurrentHealthAndCondition = null;

        WorldSave migrated = WorldPersistence.MigrateV29ToV30(save);
        CityWorld restored = CityWorld.FromSave(WorldPersistence.MigrateToCurrent(migrated));

        Assert.False(restored.Hero!.CurrentHealthAndCondition.IsResolved);
        Assert.NotNull(restored.Hero.Wound);
        Assert.Throws<InvalidOperationException>(() =>
            new CitizenStatisticsService().CalculateDefense(restored.Hero, 1));
    }
}
