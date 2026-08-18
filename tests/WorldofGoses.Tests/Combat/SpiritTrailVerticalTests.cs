using System.Linq;
using System.Collections.Generic;
using WorldofGoses.Domain;
using WorldofGoses.Domain.Combat;
using WorldofGoses.Persistence;
using Xunit;

namespace WorldofGoses.Tests.Combat;

public sealed class SpiritTrailVerticalTests
{
    public static IEnumerable<object[]> FounderLineages =>
    [
        [LineageId.Ardhen],
        [LineageId.Eirune],
        [LineageId.Kovari],
        [LineageId.Myrven],
        [LineageId.Vaelun],
        [LineageId.Orveth],
        [LineageId.Caelith],
        [LineageId.Theryn],
    ];

    [Theory]
    [MemberData(nameof(FounderLineages))]
    public void ArmedFounderCompletesTutorialEncounterForEveryFounderLineage(LineageId lineage)
    {
        // #26 closure: the vertical builds a founder with a real weapon
        // materialised at creation time, then runs the Spirit Trail through
        // to victory. The legacy assertion `Weapon is null` validated the
        // fallback path; the new Founder is armed, so this test now
        // validates that the armed Founder survives the catastrophic
        // condition (no Shelter, no Food) the fallback used to paper over.
        var world = new CityWorld();
        FounderOnboardingResult onboarding = new(
            lineage,
            ElementalAffinity.Earth,
            CubeScoring.ComputeCubeVertex(lineage),
            FounderNarrativeMemory.Empty);
        CitizenProfile profile = CitizenProfile.CreateFounder(
            onboarding,
            GenderId.Feminine);
        // The two families the founder can materialise. Pick one
        // deterministically so the test asserts the family the choice
        // implies, not whichever the RNG lands on.
        (WeaponFamily chosen, _) = NaturalWeaponFamilies.For(
            CubeExpression.Derive(onboarding.CubeProfile));
        Assert.True(world.TryCreateHero(new HeroCreationRequest(
            "Founder",
            profile,
            profile.Gender,
            onboarding,
            MaterializedWeaponFamily: chosen)).IsSuccess);
        // The materialised weapon is observable from the first frame,
        // not after the opening expedition starts. A regression here would
        // mean the new founder is still born unarmed — exactly the
        // condition the previous null assertion was hiding.
        Assert.NotNull(world.Hero!.EquipmentLoadout.Weapon);
        Assert.Equal(chosen, world.Hero.EquipmentLoadout.Weapon!.Family);
        world.SeedStartingForests();
        world.SeedStartingOpportunities();
        ExpeditionCombatSessionIntegrationTests.DriveNightToDawn(world);
        ResourceOpportunity opportunity = world.ResourceOpportunities.Values.Single(item =>
            item.Kind == ResourceOpportunityKind.SpiritTrailSearch);
        ExpeditionStartResult started = world.StartResourceExpedition(
            opportunity.Id,
            [world.Hero.Id],
            ExpeditionRetreatPosture.ContinueAfterSetback);
        Assert.True(started.IsSuccess);
        Expedition expedition = world.Expeditions[started.ExpeditionId!.Value];

        AdvanceUntil(world, () => expedition.EncounterOutcome.HasValue);

        CombatSessionSnapshot combat = world.GetCombatSessionSnapshot(started.ExpeditionId.Value)!;
        Assert.True(
            expedition.EncounterOutcome == ExpeditionEncounterOutcome.FullSuccess,
            $"{lineage} ended {expedition.EncounterOutcome}; step={combat.Step}; "
            + $"party={string.Join(',', combat.Party.Select(actor => actor.CurrentHealth))}; "
            + $"enemies={string.Join(',', combat.Enemies.Select(actor => actor.CurrentHealth))}; "
            + $"last={combat.Log.LastOrDefault()?.Detail}");
        Assert.Equal(ExpeditionPhase.Objective, expedition.Phase);
        // The weapon survives the round trip; the legacy "null at the end
        // of the tutorial" check would mean the registry lost it.
        Assert.Equal(chosen, world.Hero.EquipmentLoadout.Weapon!.Family);
    }

    [Fact]
    public void DispatchAfterSpiritDeparted_NeedsNeitherCacheNorFoodNorReservation()
    {
        // Build the founder armed so the test no longer hides the
        // equipping seam in a fixture that exists to prove the dispatch
        // flow runs without a cache.
        (CityWorld world, ResourceOpportunity opportunity) =
            ExpeditionCombatSessionIntegrationTests.PrepareArmedSpiritTrailWorld();
        Assert.False(world.HasFoundingSiteModule(FoundingSiteModule.Cache));
        Assert.Equal(0, world.Resources.Total(ResourceType.Food));

        ExpeditionPlanningSnapshot planning = ExpeditionPlanningSnapshot.From(world);
        ExpeditionPlanningSnapshot.OpportunityItem item = Assert.Single(
            planning.Opportunities,
            candidate => candidate.Kind == ResourceOpportunityKind.SpiritTrailSearch);
        Assert.False(planning.ResourceSortiesUnlocked);
        Assert.True(planning.SpiritTrailUnlocked);
        Assert.True(item.AccessUnlocked);
        Assert.True(item.CanDispatch);

        ExpeditionStartResult result = world.StartResourceExpedition(
            opportunity.Id,
            [world.Hero!.Id],
            ExpeditionRetreatPosture.ContinueAfterSetback);

        Assert.True(result.IsSuccess, result.Outcome.ToString());
        Expedition expedition = world.Expeditions[result.ExpeditionId!.Value];
        Assert.Equal(ExpeditionSupplyRequirement.None, expedition.SupplyRequirement);
        Assert.Equal(ExpeditionReward.Discovery, expedition.Reward);
        Assert.Null(expedition.ReservationId);
        Assert.Empty(world.Resources.Reservations);
        // The weapon is the chosen family — the unarmed/null assertion
        // validated the legacy path, which no longer exists in this slice.
        Assert.NotNull(world.Hero.EquipmentLoadout.Weapon);
    }

    [Fact]
    public void ArmedOpeningPreservesWoundedHealthRatioAndNeverHealsFounder()
    {
        (CityWorld world, ResourceOpportunity opportunity) =
            ExpeditionCombatSessionIntegrationTests.PrepareArmedSpiritTrailWorld();
        const double woundedHealth = 20;
        world.Hero!.SetCurrentHealthAndCondition(new CurrentHealthAndCondition(
            woundedHealth,
            conditionFactor: 0.75,
            StatisticsBalanceConfig.Default));
        ExpeditionStartResult started = world.StartResourceExpedition(
            opportunity.Id,
            [world.Hero.Id],
            ExpeditionRetreatPosture.ContinueAfterSetback);
        Assert.True(started.IsSuccess);

        AdvanceUntil(world, () => world.Expeditions[started.ExpeditionId!.Value]
            .EncounterOutcome.HasValue);

        Assert.True(
            world.Hero.CurrentHealthAndCondition.CurrentHealth <= woundedHealth,
            $"Opening combat healed Founder from {woundedHealth} to "
            + $"{world.Hero.CurrentHealthAndCondition.CurrentHealth}.");
        // The weapon survives the wounded scenario. The old null assertion
        // would now be a regression — the weapon is real and persists.
        Assert.NotNull(world.Hero.EquipmentLoadout.Weapon);
    }

    [Fact]
    public void FirstEncounterStartsAtNamedEarlyMilestoneWithMeleeAndRanged()
    {
        (CityWorld world, ExpeditionId expeditionId) =
            ExpeditionCombatSessionIntegrationTests.StartSpiritTrail();
        Expedition expedition = world.Expeditions[expeditionId];

        WorldTimeAdvance.Advance(
            world,
            ExpeditionTiming.SpiritTrailEncounterOffsetTicks - 1);
        Assert.Equal(ExpeditionPhase.Outbound, expedition.Phase);
        Assert.Null(world.GetCombatSessionSnapshot(expeditionId));

        world.AdvanceWorldTick();

        CombatSessionSnapshot combat = Assert.IsType<CombatSessionSnapshot>(
            world.GetCombatSessionSnapshot(expeditionId));
        Assert.Equal(ExpeditionPhase.Encounter, expedition.Phase);
        Assert.Equal(2, combat.Enemies.Count);
        Assert.Contains(combat.Enemies, enemy => enemy.AttackRange < 100);
        Assert.Contains(combat.Enemies, enemy => enemy.AttackRange > 100);
        Assert.True(
            ExpeditionTiming.SpiritTrailEncounterOffsetTicks
            < ExpeditionTiming.SpiritTrailDurationTicks / 4);
    }

    [Fact]
    public void VictoryContinuesToPhysicalObjectiveThenVisibleReturnWithoutMaterialReward()
    {
        (CityWorld world, ExpeditionId expeditionId) =
            ExpeditionCombatSessionIntegrationTests.StartSpiritTrail();
        Expedition expedition = world.Expeditions[expeditionId];
        ResourceOpportunity opportunity = world.ResourceOpportunities.Values.Single(item =>
            item.Kind == ResourceOpportunityKind.SpiritTrailSearch);
        int foodBefore = world.Resources.Total(ResourceType.Food);
        int woodBefore = world.Resources.Total(ResourceType.Wood);

        AdvanceUntil(world, () => expedition.EncounterOutcome.HasValue);

        Assert.Equal(ExpeditionEncounterOutcome.FullSuccess, expedition.EncounterOutcome);
        Assert.Equal(ExpeditionPhase.Objective, expedition.Phase);
        Assert.Equal(ExpeditionStatus.Active, expedition.Status);
        ExpeditionLiveSnapshot objective = Assert.IsType<ExpeditionLiveSnapshot>(
            ExpeditionLiveSnapshot.From(world, expeditionId));
        Assert.Null(objective.CombatState);
        Assert.True(objective.TravelState.ObjectiveVisible);
        Assert.False(objective.TravelState.ObjectiveReached);

        WorldTimeAdvance.Advance(
            world,
            // The leg offset is measured in ticks walked, so the wall-clock
            // boundary sits that far past the start plus whatever the road
            // charged on the way — here, the encounter the party just fought.
            expedition.StartTick + expedition.EstimateDeltaTicks
                + ExpeditionTiming.SpiritTrailObjectiveOffsetTicks
                - world.CurrentTick);

        Assert.Equal(ExpeditionPhase.Returning, expedition.Phase);
        Assert.Equal(world.CurrentTick, expedition.ObjectiveReachedAtTick);
        ExpeditionLiveSnapshot returning = Assert.IsType<ExpeditionLiveSnapshot>(
            ExpeditionLiveSnapshot.From(world, expeditionId));
        Assert.Equal(CombatFacing.Left, returning.TravelState.Facing);
        Assert.True(returning.TravelState.ObjectiveVisible);
        Assert.True(returning.TravelState.ObjectiveReached);

        WorldTimeAdvance.Advance(world, expedition.EndTick - world.CurrentTick);

        Assert.Equal(ExpeditionStatus.Returned, expedition.Status);
        Assert.Equal(ExpeditionPhase.Resolved, expedition.Phase);
        Assert.True(world.Hero!.IsAvailable);
        Assert.Equal(ResourceOpportunityState.Depleted, opportunity.State);
        Assert.Equal(0, expedition.ReturnedAmount);
        Assert.Equal(foodBefore, world.Resources.Total(ResourceType.Food));
        Assert.Equal(woodBefore, world.Resources.Total(ResourceType.Wood));
    }

    [Fact]
    public void RepeatedRoundTripsAcrossEveryPhasePreserveSessionAndExactOnceFacts()
    {
        (CityWorld world, ExpeditionId expeditionId) =
            ExpeditionCombatSessionIntegrationTests.StartSpiritTrail();
        world = RoundTrip(world);
        Expedition expedition = world.Expeditions[expeditionId];
        Assert.Equal(ExpeditionPhase.Outbound, expedition.Phase);

        AdvanceUntil(world, () => world.GetCombatSessionSnapshot(expeditionId) is not null);
        Assert.True(world.SetCombatAutoSkillsEnabled(expeditionId, false));
        Assert.True(world.TryActivateMemberSkill(expeditionId, 0));
        AdvanceUntil(world, () => world.GetCombatSessionSnapshot(expeditionId)!.Log.Any(entry =>
            entry.Kind == CombatLogKind.TechniqueResolved
            && entry.ActorId == $"citizen.{world.Hero!.Id.Value}"));
        CombatSessionSnapshot beforeCooldownLoad = world.GetCombatSessionSnapshot(expeditionId)!;
        Assert.Contains(beforeCooldownLoad.MemberSkills, skill => skill.Remaining > 0);
        world = RoundTrip(world);
        AssertSessionEquivalent(
            beforeCooldownLoad,
            Assert.IsType<CombatSessionSnapshot>(world.GetCombatSessionSnapshot(expeditionId)));

        Assert.True(world.SetCombatAutoSkillsEnabled(expeditionId, true));
        expedition = world.Expeditions[expeditionId];
        AdvanceUntil(world, () => expedition.EncounterOutcome.HasValue);
        Assert.Equal(ExpeditionPhase.Objective, expedition.Phase);
        world = RoundTrip(world);

        expedition = world.Expeditions[expeditionId];
        WorldTimeAdvance.Advance(
            world,
            expedition.StartTick + expedition.EstimateDeltaTicks
                + ExpeditionTiming.SpiritTrailObjectiveOffsetTicks
                - world.CurrentTick - 1);
        Assert.Equal(ExpeditionPhase.Objective, expedition.Phase);
        world = RoundTrip(world);
        world.AdvanceWorldTick();
        Assert.Equal(ExpeditionPhase.Returning, world.Expeditions[expeditionId].Phase);
        world = RoundTrip(world);

        expedition = world.Expeditions[expeditionId];
        WorldTimeAdvance.Advance(world, expedition.EndTick - world.CurrentTick);

        Assert.Equal(ExpeditionStatus.Returned, world.Expeditions[expeditionId].Status);
        Assert.Equal(1, world.Log.Events.Count(evt =>
            evt.Kind == WorldEventKind.ExpeditionDispatched));
        Assert.Equal(1, world.Log.Events.Count(evt =>
            evt.Kind == WorldEventKind.ExpeditionEncounterResolved));
        Assert.Equal(1, world.Log.Events.Count(evt =>
            evt.Kind == WorldEventKind.ExpeditionReturned));
        Assert.Empty(world.Resources.Reservations);
    }

    [Fact]
    public void FullRouteOfflineMatchesCanonicalTicksIncludingObjectiveArrival()
    {
        (CityWorld source, ExpeditionId expeditionId) =
            ExpeditionCombatSessionIntegrationTests.StartSpiritTrail();
        WorldSave start = WorldPersistence.Capture(source);
        CityWorld live = WorldPersistence.FromSave(start);
        CityWorld offline = WorldPersistence.FromSave(start);
        Expedition liveExpedition = live.Expeditions[expeditionId];
        int duration = liveExpedition.EndTick - live.CurrentTick;

        for (int tick = 0; tick < duration; tick++) live.AdvanceWorldTick();
        WorldTimeAdvance.Advance(offline, duration);

        Expedition offlineExpedition = offline.Expeditions[expeditionId];
        Assert.Equal(liveExpedition.Status, offlineExpedition.Status);
        Assert.Equal(liveExpedition.Phase, offlineExpedition.Phase);
        Assert.Equal(liveExpedition.EncounterOutcome, offlineExpedition.EncounterOutcome);
        Assert.Equal(liveExpedition.ObjectiveReachedAtTick, offlineExpedition.ObjectiveReachedAtTick);
        Assert.Equal(liveExpedition.ReturnedAmount, offlineExpedition.ReturnedAmount);
        Assert.Equal(
            live.Hero!.CurrentHealthAndCondition,
            offline.Hero!.CurrentHealthAndCondition);
    }

    private static CityWorld RoundTrip(CityWorld world) => WorldPersistence.FromSave(
        WorldPersistence.DeserializeFromJson(
            WorldPersistence.SerializeToJson(WorldPersistence.Capture(world))));

    private static void AssertSessionEquivalent(
        CombatSessionSnapshot expected,
        CombatSessionSnapshot actual)
    {
        Assert.Equal(expected.Active, actual.Active);
        Assert.Equal(expected.AutoSkillsEnabled, actual.AutoSkillsEnabled);
        Assert.Equal(expected.Step, actual.Step);
        Assert.Equal(expected.Outcome, actual.Outcome);
        Assert.Equal(expected.Party, actual.Party);
        Assert.Equal(expected.Enemies, actual.Enemies);
        Assert.Equal(expected.MemberSkills, actual.MemberSkills);
        Assert.Equal(
            expected.Log.Select(entry => $"{entry.Step}:{entry.Kind}:{entry.ActorId}:{entry.TargetId}"),
            actual.Log.Select(entry => $"{entry.Step}:{entry.Kind}:{entry.ActorId}:{entry.TargetId}"));
    }

    private static void AdvanceUntil(CityWorld world, System.Func<bool> condition)
    {
        int safety = ExpeditionTiming.SpiritTrailDurationTicks;
        while (!condition() && safety-- > 0) world.AdvanceWorldTick();
        Assert.True(safety > 0, "Spirit Trail transition did not occur within its route duration.");
    }
}
