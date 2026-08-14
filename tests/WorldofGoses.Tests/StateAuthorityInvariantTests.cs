using System;
using System.Linq;
using WorldofGoses.Domain;
using WorldofGoses.Domain.Combat;
using WorldofGoses.Persistence;
using WorldofGoses.Tests.Combat;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// Cross-domain invariants for the state-authority model documented in
/// <c>docs/engineering/state-authority.md</c>. These assert relationships between
/// authorities that no single owner can check on its own — a citizen's
/// commitment against the expedition it names, transit metadata against the
/// location that implies it, a projection against the facts it claims to be
/// derived from, and a terminal state against every command that could
/// reopen it.
/// </summary>
public sealed class StateAuthorityInvariantTests
{
    // ---------------------------------------------------------------------
    // Commitment ↔ Expedition
    // ---------------------------------------------------------------------

    /// <summary>
    /// The commitment and the expedition are two records of one fact, so they
    /// must agree in both directions at every phase of the route: a citizen
    /// committed to an expedition id is listed by that expedition, and every
    /// member of an active expedition is committed to exactly it.
    /// </summary>
    [Fact]
    public void CommitmentAndExpeditionAgreeInBothDirectionsAcrossTheWholeRoute()
    {
        (CityWorld world, ExpeditionId expeditionId) =
            ExpeditionCombatSessionIntegrationTests.StartSpiritTrail();

        while (world.Expeditions[expeditionId].Status == ExpeditionStatus.Active)
        {
            AssertCommitmentExpeditionAgreement(world);
            world.AdvanceWorldTick();
        }

        AssertCommitmentExpeditionAgreement(world);
        Assert.Equal(ExpeditionStatus.Returned, world.Expeditions[expeditionId].Status);
        Assert.Equal(CitizenCommitmentKind.None, world.Hero!.Commitment.Kind);
    }

    private static void AssertCommitmentExpeditionAgreement(CityWorld world)
    {
        foreach (Citizen citizen in world.Citizens.Values)
        {
            if (citizen.Commitment.Kind != CitizenCommitmentKind.Expedition) continue;
            var committedTo = new ExpeditionId(citizen.Commitment.EntityId!.Value);
            Expedition expedition = Assert.Contains(committedTo, world.Expeditions);
            Assert.True(
                expedition.HasMember(citizen.Id),
                $"{citizen.Name} is committed to expedition {committedTo.Value}, which does not list them.");
            Assert.Equal(ExpeditionStatus.Active, expedition.Status);
        }

        foreach (Expedition expedition in world.Expeditions.Values)
        {
            if (expedition.Status != ExpeditionStatus.Active) continue;
            foreach (CitizenId memberId in expedition.MemberIds)
            {
                Citizen member = world.GetCitizen(memberId)!;
                Assert.Equal(CitizenCommitmentKind.Expedition, member.Commitment.Kind);
                Assert.Equal(expedition.Id.Value, member.Commitment.EntityId);
            }
        }
    }

    // ---------------------------------------------------------------------
    // Location ↔ transit metadata
    // ---------------------------------------------------------------------

    /// <summary>
    /// Transit metadata only means something while in transit, and the two
    /// kinds of transit are distinguishable by it. An in-city journey carries
    /// a start tick and therefore a derived arrival; an expedition journey is
    /// timed by the expedition and carries neither, which is precisely what
    /// keeps <c>CompleteDueTravel</c> from settling an absent citizen at home.
    /// </summary>
    private static void AssertTransitMetadataIsCoherent(CityWorld world)
    {
        foreach (Citizen citizen in world.Citizens.Values)
        {
            if (citizen.CurrentLocation != CitizenLocation.InTransit)
            {
                Assert.Null(citizen.TransitStartedAtTick);
                Assert.False(citizen.IsReturningHome);
                Assert.Null(citizen.TravelArrivalTick);
                continue;
            }

            if (citizen.Commitment.Kind == CitizenCommitmentKind.Expedition)
            {
                Assert.Null(citizen.TransitStartedAtTick);
                Assert.False(citizen.IsReturningHome);
                Assert.Null(citizen.TravelArrivalTick);
                continue;
            }

            int startedAt = Assert.IsType<int>(citizen.TransitStartedAtTick);
            Assert.InRange(startedAt, 0, world.CurrentTick);
            Assert.Equal(
                startedAt + CityEconomyRules.AbstractTravelTicks,
                citizen.TravelArrivalTick);
        }
    }

    [Fact]
    public void TransitMetadataStaysCoherentAcrossAFullWorkdayCycle()
    {
        CityWorld world = TestHelpers.NewProductionWorld();

        for (int tick = 0; tick < GameClock.TicksPerInGameDay + 1; tick++)
        {
            AssertTransitMetadataIsCoherent(world);
            world.AdvanceWorldTick();
        }

        AssertTransitMetadataIsCoherent(world);
    }

    /// <summary>
    /// The restore path used to invent an in-city journey for an expedition
    /// traveller: it saw <see cref="CitizenLocation.InTransit"/>, called
    /// <c>BeginTravelToAssignment(currentTick)</c> and so produced a
    /// <see cref="Citizen.TravelArrivalTick"/> the captured world never had.
    /// Thirty ticks later <c>CompleteDueTravel</c> found no standing order to
    /// arrive at and settled the founder at home — still committed to an
    /// expedition that was still running.
    /// </summary>
    [Fact]
    public void ReloadMidExpeditionDoesNotInventAnInCityJourney()
    {
        (CityWorld source, ExpeditionId expeditionId) =
            ExpeditionCombatSessionIntegrationTests.StartSpiritTrail();
        Citizen sourceFounder = source.Hero!;
        Assert.Equal(CitizenLocation.InTransit, sourceFounder.CurrentLocation);
        Assert.Null(sourceFounder.TravelArrivalTick);

        CityWorld restored = WorldPersistence.FromSave(WorldPersistence.Capture(source));
        Citizen founder = restored.Hero!;

        Assert.Equal(CitizenLocation.InTransit, founder.CurrentLocation);
        Assert.Null(founder.TransitStartedAtTick);
        Assert.False(founder.IsReturningHome);
        Assert.Null(founder.TravelArrivalTick);
        AssertTransitMetadataIsCoherent(restored);

        // Well past AbstractTravelTicks, still short of the route's end.
        for (int tick = 0; tick < 3 * CityEconomyRules.AbstractTravelTicks; tick++)
        {
            restored.AdvanceWorldTick();
        }

        Assert.Equal(ExpeditionStatus.Active, restored.Expeditions[expeditionId].Status);
        Assert.Equal(CitizenCommitmentKind.Expedition, founder.Commitment.Kind);
        Assert.Equal(CitizenLocation.InTransit, founder.CurrentLocation);
        AssertCommitmentExpeditionAgreement(restored);
        AssertTransitMetadataIsCoherent(restored);
    }

    // ---------------------------------------------------------------------
    // Work order as a surviving intent
    // ---------------------------------------------------------------------

    /// <summary>
    /// A work order is an intent, not a location: it must outlive the
    /// temporary commitments that displace it and be the thing the citizen
    /// falls back to, including across a reload taken while displaced.
    /// </summary>
    [Fact]
    public void WorkOrderSurvivesAnExpeditionAndAReloadTakenDuringIt()
    {
        CityWorld world = TestHelpers.NewProductionWorld();
        Citizen hero = world.Hero!;
        BuildingId workplace = Assert.IsType<BuildingId>(hero.CurrentAssignment);
        Assert.Equal(CitizenCommitmentKind.BuildingWork, hero.Commitment.Kind);

        world.Resources.DepositToCityInventory(ResourceType.Wood, 4);
        ExpeditionRequest request = ExpeditionRequest.Reconnaissance(hero.Id);
        ExpeditionStartResult started = world.StartExpedition(request);
        Assert.True(started.IsSuccess, started.Outcome.ToString());

        // Displaced, but the order is untouched.
        Assert.Equal(CitizenCommitmentKind.Expedition, hero.Commitment.Kind);
        Assert.Equal(workplace, hero.CurrentAssignment);

        CityWorld restored = WorldPersistence.FromSave(WorldPersistence.Capture(world));
        Citizen restoredHero = restored.Hero!;
        Assert.Equal(CitizenCommitmentKind.Expedition, restoredHero.Commitment.Kind);
        Assert.Equal(workplace, restoredHero.CurrentAssignment);

        WorldTimeAdvance.Advance(
            restored,
            restored.Expeditions[started.ExpeditionId!.Value].EndTick - restored.CurrentTick);

        Assert.NotEqual(CitizenCommitmentKind.Expedition, restoredHero.Commitment.Kind);
        Assert.Equal(workplace, restoredHero.CurrentAssignment);
    }

    // ---------------------------------------------------------------------
    // Wound as an orthogonal condition
    // ---------------------------------------------------------------------

    /// <summary>
    /// A wound is a condition, not a state the citizen is "in". It coexists
    /// with a full stamina bar, with a standing work order and with a
    /// location, and none of those cure it. Only recovery does.
    /// </summary>
    [Fact]
    public void WoundCoexistsWithEveryOtherAuthorityAndOnlyRecoveryClearsIt()
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

        // Orthogonal to stamina: refilling the bar does not remove it, and the
        // wound expresses itself as a lowered ceiling rather than as depletion.
        hero.RestoreStamina(hero.MaxStamina);
        Assert.True(hero.IsWounded);
        Assert.Equal(hero.EffectiveMaxStamina, hero.CurrentStamina);
        Assert.True(hero.EffectiveMaxStamina < hero.MaxStamina);

        // Orthogonal to commitment and location: both are still readable and
        // still their own authority.
        Assert.Equal(CitizenCommitmentKind.None, hero.Commitment.Kind);
        Assert.Equal(CitizenLocation.AtHome, hero.CurrentLocation);
        Assert.Equal(CitizenAvailabilityReason.Wounded, hero.AvailabilityReason);

        world.DepositFood(WoundRules.ModerateFoodCost);
        Assert.True(world.TryBeginWoundRecovery(hero.Id).IsSuccess);
        WorldTimeAdvance.Advance(world, WoundRules.ModerateRecoveryTicks);

        Assert.False(hero.IsWounded);
        Assert.Equal(hero.MaxStamina, hero.EffectiveMaxStamina);
    }

    /// <summary>
    /// Recovery is a commitment, so it obeys the same exclusivity every other
    /// commitment does. None of these rejections may leave a half-applied
    /// state behind.
    /// </summary>
    [Fact]
    public void RecoveryCannotProduceImpossibleStates()
    {
        CityWorld world = TestHelpers.WorldWithHome();
        Citizen hero = world.Hero!;
        hero.SetLocation(CitizenLocation.AtHome);

        Assert.Equal(
            WoundRecoveryOutcome.NotWounded,
            world.TryBeginWoundRecovery(hero.Id).Outcome);

        WorldEvent origin = world.Log.Record(
            world.CurrentTick,
            WorldEventKind.WoundSustained,
            WorldEventSubject.Citizen(hero.Id, hero.Name),
            (int)WoundSeverity.Moderate);
        hero.SustainWound(WoundSeverity.Moderate, origin.Id);

        // No food: rejected, and nothing is consumed or committed.
        int edibleBefore = world.EdibleStock;
        Assert.Equal(
            WoundRecoveryOutcome.MissingFood,
            world.TryBeginWoundRecovery(hero.Id).Outcome);
        Assert.Equal(edibleBefore, world.EdibleStock);
        Assert.Equal(CitizenCommitmentKind.None, hero.Commitment.Kind);

        world.DepositFood(WoundRules.ModerateFoodCost * 2);
        Assert.True(world.TryBeginWoundRecovery(hero.Id).IsSuccess);
        int edibleAfterStart = world.EdibleStock;

        // Already recovering: rejected without double-charging.
        Assert.Equal(
            WoundRecoveryOutcome.AlreadyRecovering,
            world.TryBeginWoundRecovery(hero.Id).Outcome);
        Assert.Equal(edibleAfterStart, world.EdibleStock);
        Assert.Equal(CitizenCommitmentKind.Recovery, hero.Commitment.Kind);

        // A recovering citizen cannot be dispatched or reassigned out of it.
        Assert.False(hero.CanJoinExpedition);
        Assert.False(hero.IsAvailable);
    }

    /// <summary>
    /// The complement of <c>OpeningProgressLivenessTests</c>: the liveness
    /// rule is a gate on inflicting a wound, so it must be readable as one
    /// fact rather than re-derived by each caller.
    /// </summary>
    [Fact]
    public void WoundCarryingCapabilityIsOneReadableFact()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        Assert.False(world.CanCarryWound(WoundSeverity.Moderate));

        CityWorld sheltered = TestHelpers.WorldWithHome();
        Assert.False(sheltered.CanCarryWound(WoundSeverity.Moderate));

        sheltered.DepositFood(WoundRules.ModerateFoodCost);
        Assert.True(sheltered.CanCarryWound(WoundSeverity.Moderate));
        Assert.False(sheltered.CanCarryWound(WoundSeverity.Severe));
    }

    // ---------------------------------------------------------------------
    // The routine is a projection, not an authority
    // ---------------------------------------------------------------------

    /// <summary>
    /// Reading the projection must not change anything, and two reads of an
    /// unchanged world must be equal. A projection that drifted between reads
    /// would be a second authority wearing a snapshot's clothes.
    /// </summary>
    [Fact]
    public void RoutineProjectionIsPureAndRepeatable()
    {
        CityWorld world = TestHelpers.NewProductionWorld();
        WorldTimeAdvance.Advance(world, GameClock.TicksPerInGameHour);

        foreach (Citizen citizen in world.Citizens.Values)
        {
            CitizenRoutineSnapshot first = world.GetCitizenRoutine(citizen.Id)!;
            CitizenRoutineSnapshot second = world.GetCitizenRoutine(citizen.Id)!;
            Assert.Equal(first, second);
        }
    }

    /// <summary>
    /// The projection is a function of the persisted facts alone: two worlds
    /// rebuilt from the same save must produce identical routines for every
    /// citizen, at several points along a citizen lifecycle.
    /// </summary>
    [Fact]
    public void RoutineProjectionIsDeterministicFromPersistedFactsAlone()
    {
        CityWorld world = TestHelpers.NewProductionWorld();

        for (int cycle = 0; cycle < 4; cycle++)
        {
            WorldTimeAdvance.Advance(world, GameClock.TicksPerInGameDay / 4);
            WorldSave save = WorldPersistence.Capture(world);
            CityWorld left = WorldPersistence.FromSave(save);
            CityWorld right = WorldPersistence.FromSave(
                WorldPersistence.DeserializeFromJson(WorldPersistence.SerializeToJson(save)));

            foreach (Citizen citizen in world.Citizens.Values)
            {
                CitizenRoutineSnapshot original = world.GetCitizenRoutine(citizen.Id)!;
                Assert.Equal(original, left.GetCitizenRoutine(citizen.Id));
                Assert.Equal(original, right.GetCitizenRoutine(citizen.Id));
            }
        }
    }

    /// <summary>
    /// The projection must never appear on the wire. Persisting it would turn
    /// a derivation into a saved opinion that a rules change could no longer
    /// correct.
    /// </summary>
    [Fact]
    public void NoProjectionVocabularyIsPersisted()
    {
        CityWorld world = TestHelpers.NewProductionWorld();
        WorldTimeAdvance.Advance(world, GameClock.TicksPerInGameHour);
        string json = WorldPersistence.SerializeToJson(
            WorldPersistence.Capture(world, DateTimeOffset.UnixEpoch));

        foreach (string projectionTerm in new[]
        {
            nameof(CitizenRoutineSnapshot),
            nameof(CitizenRoutineActivity),
            nameof(CitizenContextLocation),
            nameof(CitizenRoutineBlockReason),
            "\"Routine\"",
            "\"Activity\"",
            "\"Behavior\"",
            "\"ContextLocation\"",
            "\"BlockReason\"",
        })
        {
            Assert.DoesNotContain(projectionTerm, json, StringComparison.Ordinal);
        }
    }

    // ---------------------------------------------------------------------
    // Live and offline are one set of rules
    // ---------------------------------------------------------------------

    /// <summary>
    /// Stepping a lifecycle tick by tick and jumping it in one catch-up must
    /// land on the same durable state — asserted on the serialized save, so a
    /// divergence in any authority fails, not only the ones this test thought
    /// to name.
    /// </summary>
    [Fact]
    public void LiveAndOfflineAdvanceReachTheSameDurableState()
    {
        (CityWorld source, ExpeditionId expeditionId) =
            ExpeditionCombatSessionIntegrationTests.StartSpiritTrail();
        WorldSave start = WorldPersistence.Capture(source);
        CityWorld live = WorldPersistence.FromSave(start);
        CityWorld offline = WorldPersistence.FromSave(start);
        int duration = live.Expeditions[expeditionId].EndTick - live.CurrentTick;

        for (int tick = 0; tick < duration; tick++) live.AdvanceWorldTick();
        WorldTimeAdvance.Advance(offline, duration);

        Assert.Equal(
            WorldPersistence.SerializeToJson(
                WorldPersistence.Capture(live, DateTimeOffset.UnixEpoch)),
            WorldPersistence.SerializeToJson(
                WorldPersistence.Capture(offline, DateTimeOffset.UnixEpoch)));
    }

    // ---------------------------------------------------------------------
    // Terminal states are absorbing
    // ---------------------------------------------------------------------

    [Fact]
    public void ResolvedExpeditionNeverReturnsToAnActiveState()
    {
        (CityWorld world, ExpeditionId expeditionId) =
            ExpeditionCombatSessionIntegrationTests.StartSpiritTrail();
        WorldTimeAdvance.Advance(
            world,
            world.Expeditions[expeditionId].EndTick - world.CurrentTick);
        Expedition expedition = world.Expeditions[expeditionId];
        Assert.Equal(ExpeditionStatus.Returned, expedition.Status);
        Assert.Equal(ExpeditionPhase.Resolved, expedition.Phase);

        // Neither more time, nor a cancellation, nor a reload reopens it.
        WorldTimeAdvance.Advance(world, GameClock.TicksPerInGameDay);
        Assert.False(world.CancelExpedition(expeditionId));
        CityWorld restored = WorldPersistence.FromSave(WorldPersistence.Capture(world));

        foreach (Expedition resolved in new[] { world.Expeditions[expeditionId], restored.Expeditions[expeditionId] })
        {
            Assert.Equal(ExpeditionStatus.Returned, resolved.Status);
            Assert.Equal(ExpeditionPhase.Resolved, resolved.Phase);
            Assert.False(resolved.IsComplete(world.CurrentTick));
        }
        Assert.Equal(
            1,
            world.Log.Events.Count(evt => evt.Kind == WorldEventKind.ExpeditionReturned));
    }

    [Fact]
    public void DepletedOpportunityNeverBecomesAvailableAgain()
    {
        (CityWorld world, ExpeditionId expeditionId) =
            ExpeditionCombatSessionIntegrationTests.StartSpiritTrail();
        ResourceOpportunity opportunity = world.ResourceOpportunities.Values.Single(
            item => item.Kind == ResourceOpportunityKind.SpiritTrailSearch);
        WorldTimeAdvance.Advance(
            world,
            world.Expeditions[expeditionId].EndTick - world.CurrentTick);
        Assert.Equal(ResourceOpportunityState.Depleted, opportunity.State);

        // Neither the entity's own commands nor a fresh dispatch reopen it.
        var otherExpedition = new ExpeditionId(99);
        Assert.False(opportunity.TryReserve(otherExpedition));
        Assert.False(opportunity.Release(otherExpedition));
        Assert.False(opportunity.Deplete(otherExpedition));

        ExpeditionStartResult retry = world.StartResourceExpedition(
            opportunity.Id,
            [world.Hero!.Id],
            ExpeditionRetreatPosture.ContinueAfterSetback);

        Assert.False(retry.IsSuccess);
        Assert.Equal(ResourceOpportunityState.Depleted, opportunity.State);
        Assert.Null(opportunity.ReservedByExpeditionId);

        CityWorld restored = WorldPersistence.FromSave(WorldPersistence.Capture(world));
        Assert.Equal(
            ResourceOpportunityState.Depleted,
            restored.ResourceOpportunities[opportunity.Id].State);
    }

    [Fact]
    public void SpentCultivationPlotNeverBecomesReadyAgain()
    {
        var site = new CultivationSite(new BuildingId(7));
        Assert.True(site.TrySow(currentTick: 10));
        Assert.True(site.AdvanceTo(10 + CultivationRules.GrowthTicks));
        Assert.Equal(CultivationPlotState.Ready, site.State);
        Assert.True(site.TryHarvest());
        Assert.Equal(CultivationPlotState.Spent, site.State);

        Assert.False(site.TryHarvest());
        Assert.False(site.TrySow(currentTick: 10 + 2 * CultivationRules.GrowthTicks));
        Assert.False(site.AdvanceTo(10 + 10 * CultivationRules.GrowthTicks));
        Assert.Equal(CultivationPlotState.Spent, site.State);
    }

    [Fact]
    public void ConcludedFirstNightNeverRestarts()
    {
        (CityWorld world, _) = ExpeditionCombatSessionIntegrationTests.PrepareSpiritTrailWorld();
        Assert.Equal(FirstNightStage.Concluded, world.FirstNight!.Stage);

        Assert.False(world.TryCloseFirstNightDialogue());
        WorldTimeAdvance.Advance(world, GameClock.TicksPerInGameDay);

        Assert.Equal(FirstNightStage.Concluded, world.FirstNight.Stage);
        Assert.Equal(FirstNightStage.Concluded, FirstNightRules.Next(FirstNightStage.Concluded));
        CityWorld restored = WorldPersistence.FromSave(WorldPersistence.Capture(world));
        Assert.Equal(FirstNightStage.Concluded, restored.FirstNight!.Stage);
    }

    /// <summary>
    /// Every <see cref="ExpeditionPhase"/> pair that is not one of the six
    /// legal hops must be rejected, not merely undocumented. Enumerating the
    /// whole matrix is what makes this a guard rather than a sample.
    /// </summary>
    [Fact]
    public void EveryUndocumentedExpeditionPhaseHopIsRejected()
    {
        (ExpeditionPhase From, ExpeditionPhase To)[] legal =
        [
            (ExpeditionPhase.Outbound, ExpeditionPhase.Encounter),
            (ExpeditionPhase.Encounter, ExpeditionPhase.Objective),
            (ExpeditionPhase.Encounter, ExpeditionPhase.Retreating),
            (ExpeditionPhase.Retreating, ExpeditionPhase.Returning),
            (ExpeditionPhase.Objective, ExpeditionPhase.Returning),
        ];

        foreach (ExpeditionPhase from in Enum.GetValues<ExpeditionPhase>())
        {
            foreach (ExpeditionPhase to in Enum.GetValues<ExpeditionPhase>())
            {
                if (from == to) continue;
                Expedition expedition = NewExpeditionAt(from);
                bool moved = to == ExpeditionPhase.Encounter
                    ? expedition.BeginEncounter()
                    : expedition.TryAdvancePhase(to);

                Assert.Equal(legal.Contains((from, to)), moved);
                Assert.Equal(moved ? to : from, expedition.Phase);
            }
        }
    }

    private static Expedition NewExpeditionAt(ExpeditionPhase phase) => new(
        new ExpeditionId(1),
        "Route",
        [new CitizenId(1)],
        startTick: 0,
        endTick: 100,
        ExpeditionSupplyRequirement.None,
        ExpeditionReward.Discovery,
        reservationId: null,
        status: ExpeditionStatus.Active,
        phase: phase);
}
