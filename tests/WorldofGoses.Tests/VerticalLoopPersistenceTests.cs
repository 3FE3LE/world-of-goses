using System;
using System.Linq;
using WorldofGoses.Domain;
using WorldofGoses.Domain.Persistence;
using Xunit;

namespace WorldofGoses.Tests;

public sealed class VerticalLoopPersistenceTests
{
    [Fact]
    public void Expedition_ReloadedAtEveryPhaseBoundary_ResolvesExactlyOnce()
    {
        CityWorld seed = TestHelpers.NewHeroWorld();
        seed.SeedStartingForests();
        seed.GatherWood(new BuildingId(100), 2);
        ExpeditionRequest request = ExpeditionRequest.Reconnaissance(seed.Hero!.Id) with
        {
            DurationTicks = 40,
        };
        ExpeditionStartResult started = seed.StartExpedition(request);
        Assert.True(started.IsSuccess);
        ExpeditionId expeditionId = started.ExpeditionId!.Value;

        CityWorld uninterrupted = Reload(seed);
        CityWorld reloaded = Reload(seed);
        Advance(uninterrupted, request.DurationTicks, offline: false);

        Advance(reloaded, 10, offline: true);
        Assert.Equal(ExpeditionPhase.Encounter, reloaded.Expeditions[expeditionId].Phase);
        reloaded = Reload(reloaded);

        Advance(reloaded, 10, offline: true);
        Assert.Contains(
            reloaded.Expeditions[expeditionId].Phase,
            new[] { ExpeditionPhase.Objective, ExpeditionPhase.Retreating });
        reloaded = Reload(reloaded);

        Advance(reloaded, 10, offline: true);
        Assert.Equal(ExpeditionPhase.Returning, reloaded.Expeditions[expeditionId].Phase);
        reloaded = Reload(reloaded);

        Advance(reloaded, 10, offline: true);
        Assert.Equal(ExpeditionPhase.Resolved, reloaded.Expeditions[expeditionId].Phase);

        Assert.Equal(Snapshot(uninterrupted), Snapshot(reloaded));
        Assert.Single(reloaded.Log.Events, evt =>
            evt.Kind is WorldEventKind.ExpeditionReturned
                or WorldEventKind.ExpeditionRetreated
                or WorldEventKind.ExpeditionFailed);
    }

    [Fact]
    public void Recovery_ReloadedHalfway_ConsumesAndCompletesExactlyOnce()
    {
        CityWorld seed = TestHelpers.WorldWithHome();
        Citizen hero = seed.Hero!;
        hero.SetLocation(CitizenLocation.AtHome);
        WorldEvent woundEvent = seed.Log.Record(
            seed.CurrentTick,
            WorldEventKind.WoundSustained,
            WorldEventSubject.Citizen(hero.Id, hero.Name),
            (int)WoundSeverity.Moderate);
        hero.SustainWound(WoundSeverity.Moderate, woundEvent.Id);
        seed.DepositFood(WoundRules.ModerateFoodCost);
        int foodBeforeTreatment = seed.FoodStock;
        Assert.True(seed.TryBeginWoundRecovery(hero.Id).IsSuccess);
        Assert.Equal(foodBeforeTreatment - WoundRules.ModerateFoodCost, seed.FoodStock);

        CityWorld uninterrupted = Reload(seed);
        CityWorld reloaded = Reload(seed);
        int halfway = WoundRules.ModerateRecoveryTicks / 2;
        Advance(uninterrupted, WoundRules.ModerateRecoveryTicks, offline: false);
        WorldTimeAdvance.Result firstCatchUp = WorldTimeAdvance.Advance(reloaded, halfway);
        Assert.True(firstCatchUp.BatchedTicks > 0);
        Assert.Equal(halfway, firstCatchUp.BatchedTicks + firstCatchUp.SteppedTicks);
        reloaded = Reload(reloaded);
        Assert.Equal(
            WoundRules.ModerateRecoveryTicks - halfway,
            reloaded.Hero!.Wound!.RecoveryTicksRemaining);
        WorldTimeAdvance.Result secondCatchUp = WorldTimeAdvance.Advance(
            reloaded,
            WoundRules.ModerateRecoveryTicks - halfway);
        Assert.True(secondCatchUp.BatchedTicks > 0);
        Assert.Equal(
            WoundRules.ModerateRecoveryTicks - halfway,
            secondCatchUp.BatchedTicks + secondCatchUp.SteppedTicks);

        Assert.Equal(Snapshot(uninterrupted), Snapshot(reloaded));
        Assert.Null(reloaded.Hero!.Wound);
        Assert.Single(reloaded.Log.Events, evt => evt.Kind == WorldEventKind.WoundRecoveryStarted);
        Assert.Single(reloaded.Log.Events, evt => evt.Kind == WorldEventKind.WoundRecoveryCompleted);
        Assert.Equal(foodBeforeTreatment - WoundRules.ModerateFoodCost, reloaded.FoodStock);
    }

    private static void Advance(CityWorld world, int tickCount, bool offline)
    {
        for (int tick = 0; tick < tickCount; tick++)
        {
            if (offline) world.AdvanceOfflineWorldTick();
            else world.AdvanceWorldTick();
        }
    }

    private static CityWorld Reload(CityWorld world) => CityWorld.FromSave(
        WorldPersistence.DeserializeFromJson(Snapshot(world)));

    private static string Snapshot(CityWorld world) =>
        WorldPersistence.SerializeToJson(
            WorldPersistence.Capture(world, DateTimeOffset.UnixEpoch));
}
