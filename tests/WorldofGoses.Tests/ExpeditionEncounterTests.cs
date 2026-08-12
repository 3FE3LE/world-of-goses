using System;
using System.Linq;
using WorldofGoses.Domain;
using WorldofGoses.Persistence;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// An active expedition steps through persisted phases
/// (Outbound/Encounter/Objective/Returning/Resolved) and
/// resolves exactly one deterministic encounter that modulates the reward.
/// </summary>
public class ExpeditionEncounterTests
{
    [Theory]
    [InlineData(1, 0, 31305176)]
    [InlineData(1, 17, 2117532448)]
    [InlineData(42, 999, -1682784552)]
    public void StableExpeditionSeed_HasFixedCrossProcessVectors(
        int expeditionId,
        int startTick,
        int expected)
    {
        Assert.Equal(expected, CityWorld.StableExpeditionSeed(expeditionId, startTick));
    }

    [Theory]
    [InlineData(ExpeditionEncounterOutcome.FullSuccess, 10, 10)]
    [InlineData(ExpeditionEncounterOutcome.PartialSuccess, 10, 5)]
    [InlineData(ExpeditionEncounterOutcome.PartialSuccess, 1, 1)]
    [InlineData(ExpeditionEncounterOutcome.Setback, 10, 0)]
    public void ApplyEncounterOutcomeToReward_ModulatesTheBaseAmount(
        ExpeditionEncounterOutcome outcome, int baseAmount, int expected)
    {
        Assert.Equal(expected, CityWorld.ApplyEncounterOutcomeToReward(baseAmount, outcome));
    }

    [Fact]
    public void Dispatch_StartsInOutboundPhase()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        world.GatherWood(new BuildingId(100), 2);
        ExpeditionStartResult result = world.StartExpedition(ExpeditionRequest.Reconnaissance(world.Hero!.Id));

        Expedition expedition = world.Expeditions[result.ExpeditionId!.Value];
        Assert.Equal(ExpeditionPhase.Outbound, expedition.Phase);
        Assert.Null(expedition.EncounterOutcome);
    }

    [Fact]
    public void Phase_AdvancesThroughAllQuartersInOrder()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        world.GatherWood(new BuildingId(100), 2);
        var request = ExpeditionRequest.Reconnaissance(world.Hero!.Id);
        ExpeditionStartResult result = world.StartExpedition(request);
        Expedition expedition = world.Expeditions[result.ExpeditionId!.Value];

        int quarter = request.DurationTicks / 4;
        for (int i = 0; i < quarter - 1; i++) world.AdvanceWorldTick();
        Assert.Equal(ExpeditionPhase.Outbound, expedition.Phase);

        world.AdvanceWorldTick();
        Assert.Equal(ExpeditionPhase.Encounter, expedition.Phase);
        Assert.NotNull(expedition.EncounterOutcome);

        for (int i = 0; i < quarter; i++) world.AdvanceWorldTick();
        Assert.Equal(ExpeditionPhase.Objective, expedition.Phase);

        for (int i = 0; i < quarter; i++) world.AdvanceWorldTick();
        Assert.Equal(ExpeditionPhase.Returning, expedition.Phase);

        for (int i = 0; i < quarter; i++) world.AdvanceWorldTick();
        Assert.Equal(ExpeditionPhase.Resolved, expedition.Phase);
        Assert.Equal(ExpeditionStatus.Returned, expedition.Status);
    }

    [Fact]
    public void EncounterOutcome_ResolvesOnceAndNeverChangesAfterward()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        world.GatherWood(new BuildingId(100), 2);
        var request = ExpeditionRequest.Reconnaissance(world.Hero!.Id);
        ExpeditionStartResult result = world.StartExpedition(request);
        Expedition expedition = world.Expeditions[result.ExpeditionId!.Value];

        for (int i = 0; i < request.DurationTicks / 4; i++) world.AdvanceWorldTick();
        ExpeditionEncounterOutcome firstRead = expedition.EncounterOutcome!.Value;

        for (int i = 0; i < request.DurationTicks / 4; i++) world.AdvanceWorldTick();

        Assert.Equal(firstRead, expedition.EncounterOutcome);
    }

    [Fact]
    public void EncounterOutcome_IsDeterministicForTheSamePlanAndWorldState()
    {
        // Two identically-built worlds dispatch the identical first
        // expedition (same expedition id, same start tick): the encounter
        // must resolve to the same outcome both times, proving it is a
        // function of persisted state, not incidental engine randomness.
        CityWorld worldA = TestHelpers.NewHeroWorld();
        worldA.SeedStartingForests();
        worldA.GatherWood(new BuildingId(100), 2);
        var requestA = ExpeditionRequest.Reconnaissance(worldA.Hero!.Id);
        ExpeditionStartResult resultA = worldA.StartExpedition(requestA);
        for (int i = 0; i < requestA.DurationTicks / 4; i++) worldA.AdvanceWorldTick();

        CityWorld worldB = TestHelpers.NewHeroWorld();
        worldB.SeedStartingForests();
        worldB.GatherWood(new BuildingId(100), 2);
        var requestB = ExpeditionRequest.Reconnaissance(worldB.Hero!.Id);
        ExpeditionStartResult resultB = worldB.StartExpedition(requestB);
        for (int i = 0; i < requestB.DurationTicks / 4; i++) worldB.AdvanceWorldTick();

        Assert.Equal(
            worldA.Expeditions[resultA.ExpeditionId!.Value].EncounterOutcome,
            worldB.Expeditions[resultB.ExpeditionId!.Value].EncounterOutcome);
    }

    [Fact]
    public void EncounterOutcome_SurvivesSaveAndLoadBeforeItResolves()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        world.GatherWood(new BuildingId(100), 2);
        var request = ExpeditionRequest.Reconnaissance(world.Hero!.Id);
        ExpeditionStartResult result = world.StartExpedition(request);

        // Round-trip while still Outbound (before the encounter tick), then
        // advance the restored world through the same boundary.
        CityWorld restored = WorldPersistence.FromSave(
            WorldPersistence.DeserializeFromJson(
                WorldPersistence.SerializeToJson(WorldPersistence.Capture(world))));
        for (int i = 0; i < request.DurationTicks / 4; i++) restored.AdvanceWorldTick();
        ExpeditionEncounterOutcome fromRestored =
            restored.Expeditions[result.ExpeditionId!.Value].EncounterOutcome!.Value;

        for (int i = 0; i < request.DurationTicks / 4; i++) world.AdvanceWorldTick();
        ExpeditionEncounterOutcome fromOriginal =
            world.Expeditions[result.ExpeditionId!.Value].EncounterOutcome!.Value;

        Assert.Equal(fromOriginal, fromRestored);
    }

    [Fact]
    public void FreshFullStaminaTeam_NeverRollsASetback()
    {
        // A team dispatched at full stamina with zero competency must
        // never roll the worst tier — team condition should widen the odds,
        // not be the only thing standing between a new player and an
        // unrecoverable-feeling first expedition. Sampled across several
        // start ticks (which vary the deterministic seed) to exercise
        // different rolls, not just one lucky draw.
        var outcomes = new System.Collections.Generic.List<ExpeditionEncounterOutcome>();
        for (int tickOffset = 0; tickOffset < 5; tickOffset++)
        {
            CityWorld world = TestHelpers.NewHeroWorld();
            world.SeedStartingForests();
            world.GatherWood(new BuildingId(100), 2);
            for (int i = 0; i < tickOffset * 17; i++) world.AdvanceWorldTick();

            var request = ExpeditionRequest.Reconnaissance(world.Hero!.Id);
            ExpeditionStartResult result = world.StartExpedition(request);
            Assert.True(result.IsSuccess);
            for (int i = 0; i < request.DurationTicks / 4; i++) world.AdvanceWorldTick();

            outcomes.Add(world.Expeditions[result.ExpeditionId!.Value].EncounterOutcome!.Value);
        }

        Assert.DoesNotContain(ExpeditionEncounterOutcome.Setback, outcomes);
    }

    [Fact]
    public void ExpeditionEncounterResolved_EventMatchesTheStoredOutcome()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        world.GatherWood(new BuildingId(100), 2);
        var request = ExpeditionRequest.Reconnaissance(world.Hero!.Id);
        ExpeditionStartResult result = world.StartExpedition(request);

        for (int i = 0; i < request.DurationTicks / 4; i++) world.AdvanceWorldTick();

        Expedition expedition = world.Expeditions[result.ExpeditionId!.Value];
        WorldEvent evt = world.Log.Events.Last(e => e.Kind == WorldEventKind.ExpeditionEncounterResolved);
        Assert.Equal((int)expedition.EncounterOutcome!.Value, evt.Amount);
    }

    [Fact]
    public void RetreatPosture_SetbackReturnsWithoutObjectiveReward()
    {
        (CityWorld world, ExpeditionRequest request, Expedition expedition) =
            StartGuaranteedSetback(ExpeditionRetreatPosture.RetreatAfterSetback);

        Assert.Equal(ExpeditionPhase.Retreating, expedition.Phase);
        Assert.Equal(ExpeditionStatus.Active, expedition.Status);
        Assert.True(expedition.RetreatTriggered);
        Assert.True(world.IsCitizenOnActiveExpedition(world.Hero!.Id));
        Assert.Single(world.Resources.Reservations);
        Assert.False(world.CancelExpedition(expedition.Id));

        int remaining = request.DurationTicks - request.DurationTicks / 4;
        for (int i = 0; i < remaining; i++) world.AdvanceWorldTick();

        Assert.Equal(ExpeditionStatus.Retreated, expedition.Status);
        Assert.Equal(ExpeditionPhase.Resolved, expedition.Phase);
        Assert.Equal(0, expedition.ReturnedAmount);
        Assert.Empty(world.Resources.Reservations);
        Assert.Equal(0, world.Resources.Total(ResourceType.Stone));
        Assert.Equal(1, world.Resources.Total(ResourceType.Wood));
        WorldEvent retreated = world.Log.Events.Last(
            evt => evt.Kind == WorldEventKind.ExpeditionRetreated);
        Assert.Equal(expedition.DispatchEventId, retreated.CauseEventId);
    }

    [Fact]
    public void ContinuePosture_SetbackStillReachesObjective()
    {
        (CityWorld world, ExpeditionRequest request, Expedition expedition) =
            StartGuaranteedSetback(ExpeditionRetreatPosture.ContinueAfterSetback);

        Assert.Equal(ExpeditionPhase.Encounter, expedition.Phase);
        for (int i = 0; i < request.DurationTicks / 4; i++) world.AdvanceWorldTick();

        Assert.Equal(ExpeditionPhase.Objective, expedition.Phase);
        Assert.False(expedition.RetreatTriggered);
    }

    [Fact]
    public void RetreatingExpedition_RoundTripsPlanPhaseAndDispatchCause()
    {
        (CityWorld world, _, Expedition expedition) =
            StartGuaranteedSetback(ExpeditionRetreatPosture.RetreatAfterSetback);

        CityWorld restored = WorldPersistence.FromSave(
            WorldPersistence.DeserializeFromJson(
                WorldPersistence.SerializeToJson(WorldPersistence.Capture(world))));
        Expedition restoredExpedition = restored.Expeditions[expedition.Id];

        Assert.Equal(ExpeditionPhase.Retreating, restoredExpedition.Phase);
        Assert.Equal(
            ExpeditionRetreatPosture.RetreatAfterSetback,
            restoredExpedition.RetreatPosture);
        Assert.Equal(expedition.DispatchEventId, restoredExpedition.DispatchEventId);
        Assert.Equal(CitizenCommitmentKind.Expedition, restored.Hero!.Commitment.Kind);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ActiveExpedition_AdvancesIdenticallyLiveAndOffline(bool structuredCity)
    {
        CityWorld live = NewExpeditionWorld(
            ExpeditionRetreatPosture.RetreatAfterSetback,
            leaveHeroAtLowStamina: true,
            out ExpeditionRequest request,
            out ExpeditionId expeditionId);
        if (structuredCity)
        {
            live.RegisterBuilding(TestHelpers.NewBuilding(new BuildingId(7000)));
        }
        CityWorld offline = WorldPersistence.FromSave(
            WorldPersistence.DeserializeFromJson(
                WorldPersistence.SerializeToJson(WorldPersistence.Capture(live))));
        int liveEventCursor = live.Log.Events.Count;
        int offlineEventCursor = offline.Log.Events.Count;

        for (int i = 0; i < request.DurationTicks; i++) live.AdvanceWorldTick();
        WorldTimeAdvance.Result offlineAdvance =
            WorldTimeAdvance.Advance(offline, request.DurationTicks);
        Assert.True(offlineAdvance.BatchedTicks > 0);

        Expedition liveExpedition = live.Expeditions[expeditionId];
        Expedition offlineExpedition = offline.Expeditions[expeditionId];
        Assert.Equal(liveExpedition.Status, offlineExpedition.Status);
        Assert.Equal(liveExpedition.Phase, offlineExpedition.Phase);
        Assert.Equal(liveExpedition.EncounterOutcome, offlineExpedition.EncounterOutcome);
        Assert.Equal(
            live.Resources.Total(ResourceType.Wood),
            offline.Resources.Total(ResourceType.Wood));
        Assert.Equal(
            live.Resources.Total(ResourceType.Stone),
            offline.Resources.Total(ResourceType.Stone));
        Assert.Equal(
            live.Log.Events.Skip(liveEventCursor).Select(evt => (evt.Kind, evt.CauseEventId)),
            offline.Log.Events.Skip(offlineEventCursor).Select(evt => (evt.Kind, evt.CauseEventId)));
    }

    [Fact]
    public void CancelBeforeFirstTravelTick_ReleasesPlanWithoutRecovery()
    {
        CityWorld world = NewExpeditionWorld(
            ExpeditionRetreatPosture.RetreatAfterSetback,
            leaveHeroAtLowStamina: false,
            out _,
            out ExpeditionId expeditionId);
        Citizen hero = world.Hero!;

        Assert.True(world.CancelExpedition(expeditionId));

        Assert.Equal(CitizenVitalStatus.Stable, hero.VitalStatus);
        Assert.Equal(CitizenCommitment.None, hero.Commitment);
        Assert.Equal(2, world.Resources.Total(ResourceType.Wood));
        Assert.Equal(2, world.Resources.Available(ResourceType.Wood));
        Assert.Equal(ExpeditionStatus.Cancelled, world.Expeditions[expeditionId].Status);
    }

    [Fact]
    public void CancelAfterTravelBegins_IsRejected()
    {
        CityWorld world = NewExpeditionWorld(
            ExpeditionRetreatPosture.RetreatAfterSetback,
            leaveHeroAtLowStamina: false,
            out _,
            out ExpeditionId expeditionId);
        world.AdvanceWorldTick();

        Assert.False(world.CancelExpedition(expeditionId));
        Assert.True(world.IsCitizenOnActiveExpedition(world.Hero!.Id));
        Assert.Single(world.Resources.Reservations);
    }

    [Fact]
    public void MigrationV17_DefaultsLegacyPlanToContinueAndRecoversDispatchCause()
    {
        CityWorld world = NewExpeditionWorld(
            ExpeditionRetreatPosture.RetreatAfterSetback,
            leaveHeroAtLowStamina: false,
            out _,
            out ExpeditionId expeditionId);
        WorldSave legacy = WorldPersistence.Capture(world);
        legacy.Version = 17;
        ExpeditionSave legacyExpedition = Assert.Single(legacy.Expeditions);
        legacyExpedition.RetreatPosture = string.Empty;
        legacyExpedition.DispatchEventId = null;

        WorldSave migrated = WorldPersistence.MigrateV17ToV18(legacy);
        migrated = WorldPersistence.MigrateToCurrent(migrated);

        Assert.Equal(WorldSave.CurrentVersion, migrated.Version);
        Assert.Equal(
            ExpeditionRetreatPosture.ContinueAfterSetback.ToString(),
            migrated.Expeditions[0].RetreatPosture);
        Assert.Equal(
            world.Expeditions[expeditionId].DispatchEventId?.Value,
            migrated.Expeditions[0].DispatchEventId);
        WorldPersistence.Validate(migrated);
    }

    [Fact]
    public void Validation_RejectsRetreatedStatusWithoutMatchingPostureAndOutcome()
    {
        CityWorld world = NewExpeditionWorld(
            ExpeditionRetreatPosture.ContinueAfterSetback,
            leaveHeroAtLowStamina: false,
            out _,
            out _);
        WorldSave save = WorldPersistence.Capture(world);
        ExpeditionSave expedition = Assert.Single(save.Expeditions);
        expedition.Status = ExpeditionStatus.Retreated.ToString();
        expedition.Phase = ExpeditionPhase.Resolved.ToString();
        expedition.ReturnedAmount = 0;

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(
            () => WorldPersistence.Validate(save));

        Assert.Contains("incoherent status", error.Message);
    }

    [Fact]
    public void Validation_RejectsActiveResolvedExpedition()
    {
        CityWorld world = NewExpeditionWorld(
            ExpeditionRetreatPosture.ContinueAfterSetback,
            leaveHeroAtLowStamina: false,
            out _,
            out _);
        WorldSave save = WorldPersistence.Capture(world);
        Assert.Single(save.Expeditions).Phase = ExpeditionPhase.Resolved.ToString();

        Assert.Throws<InvalidOperationException>(() => WorldPersistence.Validate(save));
    }

    [Fact]
    public void ActiveDispatchCause_RemainsPinnedWhenChronicleExceedsRetentionLimit()
    {
        CityWorld world = NewExpeditionWorld(
            ExpeditionRetreatPosture.RetreatAfterSetback,
            leaveHeroAtLowStamina: false,
            out _,
            out ExpeditionId expeditionId);
        Expedition expedition = world.Expeditions[expeditionId];
        int dispatchEventId = expedition.DispatchEventId.GetValueOrDefault().Value;
        Assert.True(dispatchEventId > 0);
        for (int index = 0; index < WorldEventRetention.MaximumPersistedEvents + 20; index++)
        {
            world.Log.Record(
                world.CurrentTick,
                WorldEventKind.ProjectCompleted,
                WorldEventSubject.ConstructionProject(
                    new BuildingId(8000 + index),
                    $"Retention fixture {index}"));
        }

        WorldSave save = WorldPersistence.Capture(world);

        Assert.Equal(WorldEventRetention.MaximumPersistedEvents, save.Events.Count);
        Assert.Contains(save.Events, evt => evt.Id == dispatchEventId);
        Assert.Equal(
            dispatchEventId,
            Assert.Single(save.Expeditions).DispatchEventId);
        CityWorld restored = WorldPersistence.FromSave(save);
        Assert.True(restored.CancelExpedition(expeditionId));
        WorldEvent cancelled = restored.Log.Events.Last(
            evt => evt.Kind == WorldEventKind.ExpeditionCancelled);
        Assert.Equal(expedition.DispatchEventId, cancelled.CauseEventId);
    }

    private static (CityWorld World, ExpeditionRequest Request, Expedition Expedition)
        StartGuaranteedSetback(ExpeditionRetreatPosture posture)
    {
        for (int startTick = 0; startTick < 64; startTick++)
        {
            CityWorld world = TestHelpers.NewHeroWorld();
            world.SeedStartingForests();
            world.GatherWood(new BuildingId(100), 2);
            for (int i = 0; i < startTick; i++) world.AdvanceWorldTick();
            Citizen hero = world.Hero!;
            hero.ConsumeStamina(hero.CurrentStamina - 1);
            ExpeditionRequest request = ExpeditionRequest.Reconnaissance(hero.Id, posture);
            ExpeditionStartResult result = world.StartExpedition(request);
            for (int i = 0; i < request.DurationTicks / 4; i++) world.AdvanceWorldTick();
            Expedition expedition = world.Expeditions[result.ExpeditionId!.Value];
            if (expedition.EncounterOutcome == ExpeditionEncounterOutcome.Setback)
            {
                return (world, request, expedition);
            }
        }
        throw new Xunit.Sdk.XunitException("No deterministic setback vector found.");
    }

    private static CityWorld NewExpeditionWorld(
        ExpeditionRetreatPosture posture,
        bool leaveHeroAtLowStamina,
        out ExpeditionRequest request,
        out ExpeditionId expeditionId)
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        world.GatherWood(new BuildingId(100), 2);
        if (leaveHeroAtLowStamina)
        {
            Citizen hero = world.Hero!;
            hero.ConsumeStamina(hero.CurrentStamina - 1);
        }
        request = ExpeditionRequest.Reconnaissance(world.Hero!.Id, posture);
        ExpeditionStartResult result = world.StartExpedition(request);
        Assert.True(result.IsSuccess);
        expeditionId = result.ExpeditionId!.Value;
        return world;
    }
}
