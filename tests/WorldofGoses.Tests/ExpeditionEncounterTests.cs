using System.Linq;
using WorldofGoses.Domain;
using WorldofGoses.Domain.Persistence;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// docs/FIRST_PLAYABLE_LOOP_AUDIT.md §G4: an active expedition steps through
/// persisted phases (Outbound/Encounter/Objective/Returning/Resolved) and
/// resolves exactly one deterministic encounter that modulates the reward.
/// </summary>
public class ExpeditionEncounterTests
{
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
        CityWorld restored = CityWorld.FromSave(
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
}
