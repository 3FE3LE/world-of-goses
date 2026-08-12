using System.Linq;
using WorldofGoses.Domain;
using WorldofGoses.Persistence;
using Xunit;

namespace WorldofGoses.Tests;

public sealed class WoundRecoveryTests
{
    [Fact]
    public void SetbackReturn_CreatesPersistentWoundSeparateFromStamina()
    {
        CityWorld world = CompleteGuaranteedSetbackExpedition();
        Citizen hero = world.Hero!;

        CitizenWound wound = Assert.IsType<CitizenWound>(hero.Wound);
        Assert.Equal(WoundSeverity.Moderate, wound.Severity);
        Assert.Equal(WoundRules.ModerateEffectiveStaminaPercent, hero.EffectiveMaxStamina);

        hero.RestoreStamina(hero.MaxStamina);

        Assert.NotNull(hero.Wound);
        Assert.Equal(hero.EffectiveMaxStamina, hero.CurrentStamina);
        Assert.False(hero.CanJoinExpedition);
    }

    [Fact]
    public void ShelterTreatment_ConsumesFoodAndCompletesOnlyAfterRequiredTime()
    {
        CityWorld world = TestHelpers.WorldWithHome();
        Citizen hero = world.Hero!;
        hero.SetLocation(CitizenLocation.AtHome);
        WorldEvent origin = world.Log.Record(
            world.CurrentTick,
            WorldEventKind.WoundSustained,
            WorldEventSubject.Citizen(hero.Id, hero.Name),
            (int)WoundSeverity.Moderate);
        hero.SustainWound(WoundSeverity.Moderate, origin.Id);
        world.DepositFood(WoundRules.ModerateFoodCost);
        int foodBefore = world.FoodStock;

        WoundRecoveryResult result = world.TryBeginWoundRecovery(hero.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(foodBefore - WoundRules.ModerateFoodCost, world.FoodStock);
        Assert.Equal(CitizenCommitmentKind.Recovery, hero.Commitment.Kind);
        for (int tick = 1; tick < WoundRules.ModerateRecoveryTicks; tick++)
        {
            world.AdvanceWorldTick();
        }
        Assert.NotNull(hero.Wound);

        world.AdvanceWorldTick();

        Assert.Null(hero.Wound);
        Assert.NotEqual(CitizenCommitmentKind.Recovery, hero.Commitment.Kind);
        Assert.Contains(world.Log.Events, evt =>
            evt.Kind == WorldEventKind.WoundRecoveryCompleted
            && evt.Subject.EntityId == hero.Id.Value);
    }

    [Fact]
    public void CaptureRoundtrip_PreservesWoundAndRecoveryProgress()
    {
        CityWorld world = CompleteGuaranteedSetbackExpedition();
        Citizen hero = world.Hero!;
        WorldSave save = WorldPersistence.Capture(world);

        CityWorld restored = WorldPersistence.FromSave(save);
        Citizen restoredHero = restored.GetCitizen(hero.Id)!;

        Assert.NotNull(restoredHero.Wound);
        Assert.Equal(hero.Wound!.Severity, restoredHero.Wound!.Severity);
        Assert.Equal(
            hero.Wound.RecoveryTicksRemaining,
            restoredHero.Wound.RecoveryTicksRemaining);
        Assert.Equal(
            hero.Wound.OriginatingEventId,
            restoredHero.Wound.OriginatingEventId);
    }

    [Fact]
    public void OfflineAndLiveRecovery_ProduceEquivalentSnapshots()
    {
        CityWorld seed = TestHelpers.WorldWithHome();
        Citizen hero = seed.Hero!;
        hero.SetLocation(CitizenLocation.AtHome);
        WorldEvent origin = seed.Log.Record(
            seed.CurrentTick,
            WorldEventKind.WoundSustained,
            WorldEventSubject.Citizen(hero.Id, hero.Name),
            (int)WoundSeverity.Moderate);
        hero.SustainWound(WoundSeverity.Moderate, origin.Id);
        seed.DepositFood(WoundRules.ModerateFoodCost);
        Assert.True(seed.TryBeginWoundRecovery(hero.Id).IsSuccess);
        WorldSave initial = WorldPersistence.Capture(seed);
        CityWorld live = WorldPersistence.FromSave(initial);
        CityWorld offline = WorldPersistence.FromSave(initial);

        for (int tick = 0; tick < WoundRules.ModerateRecoveryTicks; tick++)
        {
            live.AdvanceWorldTick();
            offline.AdvanceWorldTick();
        }

        var capturedAt = System.DateTimeOffset.UnixEpoch;
        Assert.Equal(
            WorldPersistence.SerializeToJson(WorldPersistence.Capture(live, capturedAt)),
            WorldPersistence.SerializeToJson(WorldPersistence.Capture(offline, capturedAt)));
    }

    private static CityWorld CompleteGuaranteedSetbackExpedition()
    {
        for (int startTick = 0; startTick < 64; startTick++)
        {
            CityWorld world = TestHelpers.NewHeroWorld();
            world.SeedStartingForests();
            world.GatherWood(new BuildingId(100), 2);
            // These tests are about what a wound *is* once the city carries
            // one, so the fixture has to be a city that can carry one: a
            // Basic Shelter and the treatment cost. Without both,
            // WoundRules.CanCityCarryWound refuses the wound outright, which
            // is the progress-liveness rule GitHub #13 introduced and is
            // covered by OpeningProgressLivenessTests.
            GiveTreatmentCapability(world);
            for (int tick = 0; tick < startTick; tick++) world.AdvanceWorldTick();
            Citizen hero = world.Hero!;
            hero.ConsumeStamina(hero.CurrentStamina - 1);
            ExpeditionRequest request = ExpeditionRequest.Reconnaissance(
                hero.Id,
                ExpeditionRetreatPosture.RetreatAfterSetback);
            ExpeditionStartResult result = world.StartExpedition(request);
            Assert.True(result.IsSuccess);
            for (int tick = 0; tick < request.DurationTicks; tick++)
            {
                world.AdvanceWorldTick();
            }
            Expedition expedition = world.Expeditions[result.ExpeditionId!.Value];
            if (expedition.EncounterOutcome == ExpeditionEncounterOutcome.Setback)
            {
                Assert.Equal(ExpeditionStatus.Retreated, expedition.Status);
                return world;
            }
        }
        throw new Xunit.Sdk.XunitException("No deterministic setback vector found.");
    }

    private static void GiveTreatmentCapability(CityWorld world)
    {
        world.RegisterBuilding(new Building(
            id: new BuildingId(900),
            displayName: "Basic Shelter",
            kind: BuildingKind.Home,
            producedResourceType: ResourceType.Stone,
            producedCompetencyId: CompetencyId.Mining,
            workerCapacity: 2,
            visualCapacity: 2,
            baseProductionPerWorker: 0,
            storageCapacity: 0,
            resourceLabel: "Rest",
            resourceUnit: "rest",
            productionEnabled: false));
        world.Resources.DepositToCityInventory(
            ResourceType.Food,
            WoundRules.ModerateFoodCost);
    }
}
