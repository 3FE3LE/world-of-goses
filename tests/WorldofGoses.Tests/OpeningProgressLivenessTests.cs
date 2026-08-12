using System;
using System.Collections.Generic;
using System.Linq;
using WorldofGoses.Domain;
using WorldofGoses.Domain.Combat;
using WorldofGoses.Persistence;
using WorldofGoses.Tests.Combat;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// GitHub #13. The invariant under test is progress liveness, not combat
/// balance: <em>the first narrative expedition can never leave the only
/// Citizen in a state from which no legal domain action continues the
/// game.</em>
///
/// <para>
/// The trap it regresses was total, not partial. From a real post-first-night
/// world the Founding Site has only Campfire + Bedroll — no
/// <see cref="BuildingKind.Home"/> — and holds zero edible stock, so a
/// Setback on <see cref="ResourceOpportunityKind.SpiritTrailSearch"/> used to
/// record a <see cref="CitizenWound"/> that made
/// <see cref="Citizen.IsAvailable"/> false. That single flag gates gathering,
/// construction assignment and dispatch, while treatment needs a Basic
/// Shelter the city had no way left to build and Food it had no way left to
/// gather: <see cref="WoundRecoveryOutcome.ShelterUnavailable"/> forever.
/// </para>
///
/// <para>
/// The tests deliberately drive the losing branch through a legal domain
/// state — a Founder dispatched unfit, which
/// <c>EARLY_GAME_RESOURCE_AND_EXPEDITION_PROPOSAL.md</c> §8.2 names as one of
/// the two ways a persistent wound is meant to become possible at all — and
/// they sweep the Founder configurations onboarding really produces rather
/// than one fixed affinity per lineage.
/// </para>
/// </summary>
public sealed class OpeningProgressLivenessTests
{
    /// <summary>
    /// Fraction of full health at which the Founder is unfit enough that the
    /// opening encounter resolves to <see cref="ExpeditionEncounterOutcome.Setback"/>
    /// for every lineage and affinity. Verified by
    /// <see cref="UnfitFounderReallyLosesTheOpeningEncounter"/>, so the other
    /// tests are never silently exercising the happy path.
    /// </summary>
    private const double UnfitHealthFraction = 0.02;

    private static readonly LineageId[] Lineages =
    [
        LineageId.Ardhen, LineageId.Eirune, LineageId.Kovari, LineageId.Myrven,
        LineageId.Vaelun, LineageId.Orveth, LineageId.Caelith, LineageId.Theryn,
    ];

    public static IEnumerable<object[]> FounderConfigurations =>
        from lineage in Lineages
        from affinity in Enum.GetValues<ElementalAffinity>()
        select new object[] { lineage, affinity };

    [Theory]
    [MemberData(nameof(FounderConfigurations))]
    public void UnfitFounderReallyLosesTheOpeningEncounter(
        LineageId lineage,
        ElementalAffinity affinity)
    {
        (CityWorld world, ExpeditionId expeditionId) = StartOpeningTrail(lineage, affinity, unfit: true);

        RunToCompletion(world, expeditionId);

        Assert.Equal(
            ExpeditionEncounterOutcome.Setback,
            world.Expeditions[expeditionId].EncounterOutcome);
    }

    /// <summary>
    /// The regression proper: the losing opening leaves the Founder with at
    /// least one legal action that continues the game.
    /// </summary>
    [Theory]
    [MemberData(nameof(FounderConfigurations))]
    public void LostOpeningTrailLeavesALegalRouteForEveryFounderConfiguration(
        LineageId lineage,
        ElementalAffinity affinity)
    {
        (CityWorld world, ExpeditionId expeditionId) = StartOpeningTrail(lineage, affinity, unfit: true);

        RunToCompletion(world, expeditionId);

        AssertGameCanContinue(world);
    }

    /// <summary>
    /// The winning opening keeps the same guarantee. Runs the full route so a
    /// future change that starts wounding on success is caught here too.
    /// </summary>
    [Theory]
    [MemberData(nameof(FounderConfigurations))]
    public void WonOpeningTrailLeavesALegalRouteForEveryFounderConfiguration(
        LineageId lineage,
        ElementalAffinity affinity)
    {
        (CityWorld world, ExpeditionId expeditionId) = StartOpeningTrail(lineage, affinity, unfit: false);

        RunToCompletion(world, expeditionId);

        Assert.Equal(
            ExpeditionEncounterOutcome.FullSuccess,
            world.Expeditions[expeditionId].EncounterOutcome);
        AssertGameCanContinue(world);
    }

    /// <summary>
    /// The specific dead end #13 reported, asserted as the named outcomes
    /// rather than as "something works": before the fix these three were
    /// <see cref="WoundRecoveryOutcome.ShelterUnavailable"/>,
    /// <see cref="NaturalResourceGatherOutcome.HeroUnavailable"/> and
    /// <see cref="ExpeditionStartOutcome.MemberUnavailable"/> simultaneously.
    /// </summary>
    [Fact]
    public void LostOpeningTrailDoesNotRecordAWoundTheCityCannotTreat()
    {
        (CityWorld world, ExpeditionId expeditionId) =
            StartOpeningTrail(LineageId.Ardhen, ElementalAffinity.Earth, unfit: true);

        RunToCompletion(world, expeditionId);

        Assert.Equal(
            ExpeditionEncounterOutcome.Setback,
            world.Expeditions[expeditionId].EncounterOutcome);
        Assert.False(world.CanCarryWound(WoundSeverity.Moderate));
        Assert.DoesNotContain(world.Buildings.Values, building => building.Kind == BuildingKind.Home);
        Assert.Equal(0, world.EdibleStock);
        Assert.False(world.Hero!.IsWounded);
        Assert.DoesNotContain(
            world.Log.Events,
            evt => evt.Kind == WorldEventKind.WoundSustained);
        Assert.Equal(
            WoundRecoveryOutcome.NotWounded,
            world.TryBeginWoundRecovery(world.Hero.Id).Outcome);
    }

    /// <summary>
    /// The guard is a gate on inflicting a wound, not a global removal of the
    /// treatment cost. A city that is already equipped keeps every ordinary
    /// rule: the setback wounds, and recovery still spends
    /// <see cref="WoundRules.ModerateFoodCost"/>.
    /// </summary>
    [Fact]
    public void EquippedCityStillWoundsOnSetbackAndStillPaysTheFoodCost()
    {
        (CityWorld world, ExpeditionId expeditionId) =
            StartOpeningTrail(LineageId.Ardhen, ElementalAffinity.Earth, unfit: true);
        GiveTreatmentCapability(world);
        int edibleBefore = world.EdibleStock;
        Assert.True(world.CanCarryWound(WoundSeverity.Moderate));

        RunToCompletion(world, expeditionId);

        Assert.Equal(
            ExpeditionEncounterOutcome.Setback,
            world.Expeditions[expeditionId].EncounterOutcome);
        Citizen founder = world.Hero!;
        Assert.True(founder.IsWounded);
        Assert.Equal(WoundSeverity.Moderate, founder.Wound!.Severity);
        Assert.Contains(world.Log.Events, evt => evt.Kind == WorldEventKind.WoundSustained);

        WoundRecoveryResult recovery = world.TryBeginWoundRecovery(founder.Id);

        Assert.True(recovery.IsSuccess, recovery.Outcome.ToString());
        Assert.Equal(WoundRules.ModerateFoodCost, recovery.FoodConsumed);
        Assert.Equal(edibleBefore - WoundRules.ModerateFoodCost, world.EdibleStock);
        Assert.Equal(CitizenCommitmentKind.Recovery, founder.Commitment.Kind);
    }

    /// <summary>
    /// A city with the shelter but no edible stock is the other half of the
    /// same rule: treatment would be unpayable, so the wound is not created.
    /// </summary>
    [Fact]
    public void ShelterWithoutEdibleStockStillCannotCarryAWound()
    {
        (CityWorld world, ExpeditionId expeditionId) =
            StartOpeningTrail(LineageId.Ardhen, ElementalAffinity.Earth, unfit: true);
        GiveTreatmentCapability(world, edibleStock: 0);
        Assert.False(world.CanCarryWound(WoundSeverity.Moderate));

        RunToCompletion(world, expeditionId);

        Assert.Equal(
            ExpeditionEncounterOutcome.Setback,
            world.Expeditions[expeditionId].EncounterOutcome);
        Assert.False(world.Hero!.IsWounded);
        AssertGameCanContinue(world);
    }

    /// <summary>
    /// Wild Food counts, exactly as <see cref="CityWorld.TryConsumeFood"/>
    /// already spends it. Otherwise the rule would refuse wounds in a city
    /// whose larder is full of the survival buffer.
    /// </summary>
    [Fact]
    public void WildFoodCountsTowardTreatmentCapability()
    {
        (CityWorld world, _) =
            StartOpeningTrail(LineageId.Ardhen, ElementalAffinity.Earth, unfit: true);
        GiveTreatmentCapability(world, edibleStock: 0);
        Assert.False(world.CanCarryWound(WoundSeverity.Moderate));

        world.Resources.DepositToCityInventory(
            ResourceType.WildFood,
            WoundRules.ModerateFoodCost);

        Assert.True(world.CanCarryWound(WoundSeverity.Moderate));
    }

    /// <summary>
    /// Acceptance criterion: save/load and offline catch-up must not produce a
    /// different exit from the same state. Both worlds start from one captured
    /// save, one steps tick by tick and the other jumps the whole route.
    /// </summary>
    [Fact]
    public void SaveLoadAndOfflineCatchUpReachTheSameLiveState()
    {
        (CityWorld source, ExpeditionId expeditionId) =
            StartOpeningTrail(LineageId.Ardhen, ElementalAffinity.Earth, unfit: true);
        WorldSave captured = WorldPersistence.Capture(source);
        CityWorld live = WorldPersistence.FromSave(captured);
        CityWorld offline = WorldPersistence.FromSave(captured);
        int duration = live.Expeditions[expeditionId].EndTick - live.CurrentTick;

        for (int tick = 0; tick < duration; tick++) live.AdvanceWorldTick();
        WorldTimeAdvance.Advance(offline, duration);

        Expedition liveExpedition = live.Expeditions[expeditionId];
        Expedition offlineExpedition = offline.Expeditions[expeditionId];
        Assert.Equal(ExpeditionEncounterOutcome.Setback, liveExpedition.EncounterOutcome);
        Assert.Equal(liveExpedition.EncounterOutcome, offlineExpedition.EncounterOutcome);
        Assert.Equal(liveExpedition.Status, offlineExpedition.Status);
        Assert.False(live.Hero!.IsWounded);
        Assert.False(offline.Hero!.IsWounded);
        AssertGameCanContinue(live);
        AssertGameCanContinue(offline);

        // The absence of a wound is a fact about the reloaded world too, not
        // only about the world that happened to be in memory when it resolved.
        CityWorld reloaded = WorldPersistence.FromSave(
            WorldPersistence.DeserializeFromJson(
                WorldPersistence.SerializeToJson(WorldPersistence.Capture(live))));
        Assert.False(reloaded.Hero!.IsWounded);
        AssertGameCanContinue(reloaded);
    }

    /// <summary>
    /// The liveness claim itself: name the legal action rather than asserting
    /// a flag. Hand-gathering Wild Food is the route the wounded Founder used
    /// to be locked out of, and it is what funds every later step.
    /// </summary>
    private static void AssertGameCanContinue(CityWorld world)
    {
        Citizen founder = Assert.IsType<Citizen>(world.Hero);
        Assert.False(founder.IsWounded);
        Assert.True(founder.IsAvailable, $"Founder is {founder.AvailabilityReason}.");
        Assert.Equal(CitizenCommitmentKind.None, founder.Commitment.Kind);

        NaturalResourcePatch wildFood = world.NaturalResourcePatches.Values
            .Single(patch => patch.ResourceType == ResourceType.WildFood);
        NaturalResourceGatherResult gathered = world.TryGatherFromPatch(wildFood.Id, 0, 1);

        Assert.Equal(NaturalResourceGatherOutcome.Gathered, gathered.Outcome);
        Assert.True(world.EdibleStock > 0);
    }

    private static (CityWorld World, ExpeditionId ExpeditionId) StartOpeningTrail(
        LineageId lineage,
        ElementalAffinity affinity,
        bool unfit)
    {
        var world = new CityWorld();
        CitizenProfile profile = CitizenProfile.CreateFounder(
            new FounderOnboardingResult(
                lineage,
                affinity,
                CubeScoring.ComputeCubeVertex(lineage),
                FounderNarrativeMemory.Empty),
            GenderId.Feminine);
        Assert.True(world.TryCreateHero(
            new HeroCreationRequest("Founder", profile, profile.Gender)).IsSuccess);
        world.SeedStartingForests();
        world.SeedStartingOpportunities();
        ExpeditionCombatSessionIntegrationTests.DriveNightToDawn(world);

        // The real opening state #13 reproduced from: no Cache, no Basic
        // Shelter, nothing edible.
        Assert.False(world.HasFoundingSiteModule(FoundingSiteModule.Cache));
        Assert.Equal(0, world.EdibleStock);

        if (unfit)
        {
            // "The player sends an unfit member" — the licensed wound path of
            // proposal §8.2, expressed as the durable health the combat
            // service itself reads.
            double current = world.Hero!.CurrentHealthAndCondition.CurrentHealth ?? 0;
            world.Hero.SetCurrentHealthAndCondition(new CurrentHealthAndCondition(
                current * UnfitHealthFraction,
                conditionFactor: 0.5,
                StatisticsBalanceConfig.Default));
        }

        ResourceOpportunity opportunity = world.ResourceOpportunities.Values.Single(
            item => item.Kind == ResourceOpportunityKind.SpiritTrailSearch);
        ExpeditionStartResult started = world.StartResourceExpedition(
            opportunity.Id,
            [world.Hero!.Id],
            ExpeditionRetreatPosture.ContinueAfterSetback);
        Assert.True(started.IsSuccess, started.Outcome.ToString());
        return (world, started.ExpeditionId!.Value);
    }

    private static void GiveTreatmentCapability(
        CityWorld world,
        int edibleStock = WoundRules.ModerateFoodCost)
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
        if (edibleStock > 0)
        {
            world.Resources.DepositToCityInventory(ResourceType.Food, edibleStock);
        }
    }

    private static void RunToCompletion(CityWorld world, ExpeditionId expeditionId)
    {
        Expedition expedition = world.Expeditions[expeditionId];
        int safety = 4 * ExpeditionTiming.SpiritTrailDurationTicks;
        while (expedition.Status == ExpeditionStatus.Active && safety-- > 0)
        {
            world.AdvanceWorldTick();
        }
        Assert.True(safety > 0, "The opening Spirit Trail never resolved.");
    }
}
