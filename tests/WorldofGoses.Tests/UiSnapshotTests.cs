using System;
using System.Collections.Generic;
using System.Linq;
using WorldofGoses.Domain;
using WorldofGoses.Presentation;
using Xunit;

namespace WorldofGoses.Tests;

public sealed class UiSnapshotTests
{
    [Fact]
    public void CityStatusSnapshot_SupportsEmptyCitySummarySections()
    {
        CityStatusSnapshot snapshot = CityStatusSnapshot.From(TestHelpers.NewHeroWorld());

        Assert.Empty(snapshot.Resources);
        Assert.Empty(snapshot.Projects);
        Assert.Equal(1, snapshot.CitizenCount);
        Assert.Equal(0, snapshot.HousingCapacity);
    }

    [Fact]
    public void CityStatusSnapshot_SupportsFewCitySummaryResourcesWithoutInventedDeltas()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.Resources.DepositToCityInventory(ResourceType.Branches, 3);
        world.Resources.DepositToCityInventory(ResourceType.SmallStone, 2);

        CityStatusSnapshot snapshot = CityStatusSnapshot.From(world);

        Assert.Collection(
            snapshot.Resources.OrderBy(item => item.Resource),
            item =>
            {
                Assert.Equal(ResourceType.Branches, item.Resource);
                Assert.Equal(3, item.AvailableAmount);
            },
            item =>
            {
                Assert.Equal(ResourceType.SmallStone, item.Resource);
                Assert.Equal(2, item.AvailableAmount);
            });
    }

    [Fact]
    public void CityStatusSnapshot_SupportsActiveAndBlockedConstructionSummaryStates()
    {
        CityWorld active = TestHelpers.NewConstructionWorld();
        ConstructionProject activeProject = active.Projects.Values.Single();
        active.AdvanceWorldTick();
        CityStatusSnapshot.ProjectItem activeItem =
            CityStatusSnapshot.From(active).Projects.Single();
        Assert.Equal(ConstructionStopCause.Authorized, activeItem.StopCause);

        CityWorld blocked = TestHelpers.NewConstructionWorld();
        ConstructionProject blockedProject = blocked.Projects.Values.Single();
        Assert.True(blocked.TryUnassignFromProject(blockedProject.Id, blocked.Hero!.Id).IsSuccess);
        CityStatusSnapshot.ProjectItem blockedItem =
            CityStatusSnapshot.From(blocked).Projects.Single();
        Assert.Equal(ConstructionStopCause.NoWorkers, blockedItem.StopCause);
        Assert.Equal(activeProject.RequiredWork, blockedItem.RequiredWork);
    }

    [Fact]
    public void CityStatusSnapshot_ProjectsAuthoritativeResourceAvailabilityAndReservations()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.Resources.DepositToCityInventory(ResourceType.Branches, 5);
        Assert.True(world.Resources.TryReserve(
            ResourceType.Branches,
            2,
            new ResourceReservationOwner(ResourceReservationOwnerKind.ConstructionProject, 9),
            out ResourceReservation? reservation));

        CityStatusSnapshot before = CityStatusSnapshot.From(world);
        ResourceInventoryItem branches = Assert.Single(before.Resources);
        Assert.Equal(ResourceType.Branches, branches.Resource);
        Assert.Equal(5, branches.TotalAmount);
        Assert.Equal(3, branches.AvailableAmount);

        Assert.True(world.Resources.Release(reservation!.Id));
        world.Resources.DepositToCityInventory(ResourceType.PlantFiber, 1);
        CityStatusSnapshot after = CityStatusSnapshot.From(world);

        Assert.Equal(3, branches.AvailableAmount);
        Assert.Equal(5, Assert.Single(after.Resources,
            item => item.Resource == ResourceType.Branches).AvailableAmount);
        Assert.Equal(1, Assert.Single(after.Resources,
            item => item.Resource == ResourceType.PlantFiber).AvailableAmount);
        Assert.DoesNotContain(after.Resources, item => item.TotalAmount <= 0);
    }

    [Fact]
    public void CityStatusSnapshot_ProjectsLineageAndTruthfulPopulationCapacity()
    {
        CityWorld world = TestHelpers.WorldWithHome();
        CityStatusSnapshot before = CityStatusSnapshot.From(world);

        Assert.Equal("Ardhen", before.LineageName);
        Assert.Equal(world.Citizens.Count, before.CitizenCount);
        Assert.Equal(world.HousingCapacity, before.HousingCapacity);

        world.RegisterCitizen(TestHelpers.NewCitizen(999));
        CityStatusSnapshot after = CityStatusSnapshot.From(world);

        Assert.Equal(before.CitizenCount + 1, after.CitizenCount);
        Assert.Equal(before.HousingCapacity, after.HousingCapacity);
    }

    [Fact]
    public void PoliciesSnapshot_ExposesCentralWorkdayAndAutomationRules()
    {
        CityWorld world = TestHelpers.NewHeroWorld();

        CityPolicySnapshot snapshot = CityPolicySnapshot.From(world);

        Assert.Equal(GameClock.WorkdayStartTick, snapshot.WorkdayStartTick);
        Assert.Equal(GameClock.WorkdayEndTick, snapshot.WorkdayEndTick);
        Assert.True(snapshot.IsWorkday);
        Assert.True(snapshot.AuthorizedConstructionAdvancesAutomatically);
        Assert.False(snapshot.ConstructionAuthorizationIsAutomatic);
    }

    [Fact]
    public void AutosaveCadence_IsCentralizedAtThreeRealMinutes()
    {
        Assert.Equal(System.TimeSpan.FromMinutes(3), SimulationPersistencePolicy.AutoSaveInterval);
    }

    [Fact]
    public void CityStatusSnapshot_ExposesExplicitHeroOnlyEmptyState()
    {
        var snapshot = CityStatusSnapshot.From(TestHelpers.NewHeroWorld());

        Assert.True(snapshot.IsEmpty);
        Assert.Equal("Aster", snapshot.HeroName);
        Assert.Empty(snapshot.Buildings);
        Assert.Empty(snapshot.Projects);
        Assert.Single(snapshot.FreeCitizenNames);
    }

    [Fact]
    public void ConstructionSnapshot_ExposesActionableProjectState()
    {
        var snapshot = ConstructionSnapshot.From(TestHelpers.NewConstructionWorld());

        Assert.True(snapshot.HasHero);
        Assert.NotNull(snapshot.Project);
        Assert.Single(snapshot.Project!.AssignedCitizens);
        Assert.Equal("Aster", snapshot.Project.AssignedCitizens[0].Name);
        Assert.Contains(snapshot.Project.RemainingInputs,
            input => input.Resource == ResourceType.Wood && input.Amount == 3);
        Assert.DoesNotContain(snapshot.AvailableCitizens, citizen => citizen.Name == "Aster");
    }

    [Fact]
    public void ConstructionSnapshot_ShowsShelterRequirementsAndGatherAction()
    {
        var world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();

        var before = ConstructionSnapshot.From(world);
        var shelter = before.OptionFor(ConstructionKind.BasicShelter);

        var material = Assert.Single(shelter.Materials);
        Assert.Equal(ResourceType.Wood, material.Resource);
        Assert.Equal(4, material.Required);
        Assert.Equal(0, material.Available);
        Assert.Equal(1, material.DepositRequired);
        Assert.False(shelter.CanPayDeposit);
        // Forest gathering is now driven by worker assignment; verify
        // the forest reserve exposes the same end-state through that
        // path instead of a GatherWood snapshot field.
        var forest = world.Buildings.Values.First(b => b.Kind == BuildingKind.Forest);
        world.GatherWood(forest.Id, 2);
        var after = ConstructionSnapshot.From(world).OptionFor(ConstructionKind.BasicShelter);

        Assert.Equal(2, Assert.Single(after.Materials).Available);
        Assert.True(after.CanPayDeposit);
    }

    [Fact]
    public void ConstructionSnapshot_BeforeCacheProjectsFoundersCarriedLoad()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.Resources.DepositToCityInventory(ResourceType.Food, 6);
        world.Resources.DepositToCityInventory(ResourceType.Branches, 2);
        world.Resources.DepositToCityInventory(ResourceType.PlantFiber, 1);

        ConstructionSnapshot snapshot = ConstructionSnapshot.From(world);

        Assert.False(snapshot.HasFoundingCache);
        Assert.Equal(3, snapshot.FoundingStorageCount);
        Assert.Equal(FoundingSiteRules.CarriedCapacity, snapshot.FoundingStorageCapacity);
        Assert.DoesNotContain(snapshot.FoundingResources,
            item => item.Resource == ResourceType.Food);
        Assert.Equal(2, Assert.Single(snapshot.FoundingResources,
            item => item.Resource == ResourceType.Branches).TotalAmount);
        Assert.Equal(1, Assert.Single(snapshot.FoundingResources,
            item => item.Resource == ResourceType.PlantFiber).TotalAmount);
    }

    [Fact]
    public void ConstructionSnapshot_ExplainsUnavailabilityForCitizensCommittedToBuildings()
    {
        var world = TestHelpers.NewProductionWorld();
        var snapshot = ConstructionSnapshot.From(world);

        var quarryWorker = Assert.Single(snapshot.UnavailableCitizens, citizen => citizen.Name == "Aster");
        Assert.Equal(CitizenAvailabilityReason.AssignedToBuilding, quarryWorker.Reason);
        Assert.Equal("Test quarry", quarryWorker.LocationName);

        var farmWorker = Assert.Single(snapshot.UnavailableCitizens, citizen => citizen.Name == "Citizen-3");
        Assert.Equal("Test farm", farmWorker.LocationName);

        Assert.Contains(snapshot.AvailableCitizens, citizen => citizen.Name == "Citizen-4");
        Assert.DoesNotContain(snapshot.UnavailableCitizens, citizen => citizen.Name == "Citizen-4");
    }

    [Fact]
    public void ConstructionSnapshot_DisablesUnavailableFarmAndQuarryDeposits()
    {
        var snapshot = ConstructionSnapshot.From(TestHelpers.WorldWithHome());

        Assert.False(snapshot.OptionFor(ConstructionKind.Farm).CanPayDeposit);
        Assert.False(snapshot.OptionFor(ConstructionKind.Quarry).CanPayDeposit);
    }

    [Fact]
    public void CityMacroSnapshot_ProjectsForestReserveForInteractiveTrees()
    {
        var world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        Building forest = world.Buildings.Values.First(
            building => building.Kind == BuildingKind.Forest);
        world.GatherWood(forest.Id, 2);

        CityMacroSnapshot snapshot = CityMacroSnapshot.From(world);
        CityMacroSnapshot.PlotItem projected = snapshot.Buildings.Single(
            item => item.Id == forest.Id);

        Assert.Equal(CityWorld.StartingForestWoodReserve - 2, projected.WoodReserve);
    }

    [Fact]
    public void CityMacroSnapshot_ProjectsEveryEgA0GroundResourceForInteraction()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        world.SeedStartingOpportunities();

        CityMacroSnapshot snapshot = CityMacroSnapshot.From(world);

        ResourceType[] projectedTypes = snapshot.Buildings
            .Where(item => item.GroundResourceType.HasValue)
            .Select(item => item.GroundResourceType!.Value)
            .Distinct()
            .ToArray();
        Assert.Contains(ResourceType.Wood, projectedTypes);
        Assert.Contains(ResourceType.Branches, projectedTypes);
        Assert.Contains(ResourceType.PlantFiber, projectedTypes);
        Assert.Contains(ResourceType.SmallStone, projectedTypes);
        Assert.Contains(ResourceType.WildFood, projectedTypes);
        Assert.Equal(17, snapshot.Buildings
            .Where(item => item.GroundResourceType is not ResourceType.Wood)
            .Sum(item => item.WoodUnitReserves.Count));
    }

    [Fact]
    public void BuildingDetailSnapshot_ContainsOnlyProjectedCitizenData()
    {
        var world = TestHelpers.NewProductionWorld();
        var snapshot = BuildingDetailSnapshot.From(world, new BuildingId(1));

        Assert.NotNull(snapshot);
        Assert.Equal(BuildingKind.Quarry, snapshot!.Kind);
        Assert.Equal(2, snapshot.AssignedCount);
        Assert.Equal(2, snapshot.VisibleCitizens.Count);
        Assert.All(snapshot.VisibleCitizens, citizen => Assert.False(string.IsNullOrWhiteSpace(citizen.Name)));
    }

    [Fact]
    public void BuildingDetailSnapshot_ExplainsUnavailabilityForCitizensCommittedElsewhere()
    {
        var world = TestHelpers.NewProductionWorld();
        var snapshot = BuildingDetailSnapshot.From(world, new BuildingId(1));

        Assert.NotNull(snapshot);
        var grower = Assert.Single(snapshot!.UnavailableCitizens, citizen => citizen.Name == "Citizen-3");
        Assert.Equal(CitizenAvailabilityReason.AssignedToBuilding, grower.Reason);
        Assert.Equal("Test farm", grower.LocationName);
        Assert.DoesNotContain(snapshot.UnavailableCitizens, citizen => citizen.Name is "Citizen-4" or "Citizen-5");
    }

    [Fact]
    public void BuildingDetailSnapshot_HomeCountsCitizensAtHomeNotAssignedWorkers()
    {
        // The Home's "_assigned" roster stays empty in normal play — the
        // Home has no operating recipe, so citizens are never "assigned"
        // to it. The resting count the detail panel reads must come
        // from the citizens physically at home (VisibleCitizens), not
        // from the building's own assigned-workers triplet
        // (VisibleWorkerCount + HiddenWorkerCount, both derived from
        // _assigned and therefore zero for the Home).
        var world = TestHelpers.NewProductionWorld();
        var home = world.Buildings.Values.Single(building => building.Kind == BuildingKind.Home);

        // Verify the citizen layout the fixture actually produces. The
        // hero and the first two workers were moved to AtWork in
        // NewProductionWorld; the remaining registered citizens stay
        // at their default AtHome location. Count that explicitly so
        // the assertion documents the expected picture rather than
        // hardcoding a number the fixture can drift away from.
        int expectedAtHome = world.Citizens.Values.Count(citizen => citizen.CurrentLocation == CitizenLocation.AtHome);
        Assert.True(expectedAtHome >= 2, "Fixture should produce multiple citizens at home.");

        var snapshot = BuildingDetailSnapshot.From(world, home.Id);

        Assert.NotNull(snapshot);
        Assert.Equal(BuildingKind.Home, snapshot!.Kind);
        Assert.Equal(expectedAtHome, snapshot.VisibleCitizens.Count);
        // The two counters the panel used to read — both zero because
        // no citizen is _assigned_ to the Home as a worker.
        Assert.Equal(0, snapshot.VisibleWorkerCount);
        Assert.Equal(0, snapshot.HiddenWorkerCount);
    }

    [Fact]
    public void BuildingDetailSnapshot_HomeProjectsShelterResourceInventory()
    {
        CityWorld world = TestHelpers.WorldWithHome();
        Building home = world.Buildings.Values.Single(building => building.Kind == BuildingKind.Home);
        world.Resources.DepositToCityInventory(ResourceType.Branches, 2);
        world.Resources.DepositToCityInventory(ResourceType.PlantFiber, 1);

        BuildingDetailSnapshot snapshot = Assert.IsType<BuildingDetailSnapshot>(
            BuildingDetailSnapshot.From(world, home.Id));

        Assert.Equal(world.FoundingStorageCount(), snapshot.FoundingStorageCount);
        Assert.Equal(world.GroundResourceCapacity(), snapshot.FoundingStorageCapacity);
        Assert.Equal(2, Assert.Single(
            snapshot.Resources,
            item => item.Resource == ResourceType.Branches).TotalAmount);
        Assert.Equal(1, Assert.Single(
            snapshot.Resources,
            item => item.Resource == ResourceType.PlantFiber).AvailableAmount);
        Assert.Contains(snapshot.Resources, item =>
            item.Resource == ResourceType.SmallStone && item.TotalAmount == 0);
    }

    [Fact]
    public void Snapshots_DoNotChangeWhenWorldMutates()
    {
        var world = TestHelpers.NewHeroWorld();
        var before = CityStatusSnapshot.From(world);

        world.SeedStartingForests();

        Assert.True(before.IsEmpty);
        Assert.Empty(before.Buildings);
        Assert.Equal(2, CityStatusSnapshot.From(world).Buildings.Count);
    }

    [Fact]
    public void CityMacroSnapshot_ProjectsPlotsAndCopiesEventLog()
    {
        var world = TestHelpers.NewHeroWorld();
        var before = CityMacroSnapshot.From(world);

        world.SeedStartingForests();

        Assert.Equal(1, before.CitizenCount);
        var citizen = Assert.Single(before.Citizens);
        Assert.Equal("Aster", citizen.Name);
        Assert.True(citizen.IsAvailable);
        Assert.NotNull(before.Hero);
        Assert.Empty(before.Buildings);
        Assert.Equal(2, CityMacroSnapshot.From(world).Buildings.Count);
    }

    [Fact]
    public void CityMacroSnapshot_ProjectsPersistentStorageFullState()
    {
        CityWorld world = TestHelpers.NewProductionWorld();
        Building building = world.Buildings.Values.First(candidate => candidate.StorageCapacity > 0);
        building.AddStock(building.StorageCapacity);

        CityMacroSnapshot.PlotItem projected = CityMacroSnapshot.From(world).Buildings.Single(
            item => item.Id == building.Id);

        Assert.Equal(building.Stock, projected.Stock);
        Assert.Equal(building.StorageCapacity, projected.StorageCapacity);
        Assert.True(projected.IsStorageFull);
    }

    [Fact]
    public void CityMacroSnapshot_ProjectsWoundAndTreatmentForHoverStatus()
    {
        CityWorld world = TestHelpers.WorldWithHome();
        Citizen hero = world.Hero!;
        WorldEvent origin = world.Log.Record(
            world.CurrentTick,
            WorldEventKind.WoundSustained,
            WorldEventSubject.Citizen(hero.Id, hero.Name),
            (int)WoundSeverity.Moderate);
        hero.SustainWound(WoundSeverity.Moderate, origin.Id);

        CityMacroSnapshot.CitizenItem wounded = Assert.Single(CityMacroSnapshot.From(world).Citizens);
        Assert.Equal(WoundSeverity.Moderate, wounded.WoundSeverity);
        Assert.False(wounded.IsReceivingWoundTreatment);

        world.DepositFood(WoundRules.ModerateFoodCost);
        Assert.True(world.TryBeginWoundRecovery(hero.Id).IsSuccess);

        CityMacroSnapshot.CitizenItem treating = Assert.Single(CityMacroSnapshot.From(world).Citizens);
        Assert.True(treating.IsReceivingWoundTreatment);
        Assert.Equal(WoundRules.ModerateRecoveryTicks, treating.WoundRecoveryTicksRemaining);
    }

    [Fact]
    public void CityMacroSnapshot_ProjectsNoFoodRecoveryBlockerForHoverStatus()
    {
        CityWorld world = TestHelpers.WorldWithHome();
        Citizen hero = world.Hero!;
        if (hero.CurrentLocation == CitizenLocation.InTransit && hero.IsReturningHome)
        {
            Assert.True(world.ConfirmCitizenArrivedHome(hero.Id));
        }
        hero.MarkFoodBlocked();

        CityMacroSnapshot.CitizenItem projected = Assert.Single(CityMacroSnapshot.From(world).Citizens);

        Assert.Equal(CitizenRoutineActivity.Recovering, projected.Activity);
        Assert.Equal(CitizenRoutineBlockReason.NoFood, projected.BlockReason);
    }

    [Fact]
    public void CityMacroSnapshot_ProjectsConstructionProgress()
    {
        var world = TestHelpers.NewConstructionWorld();
        var project = world.Projects.Values.Single();
        Assert.True(project.IsAssigned(world.Hero!.Id));
        for (int i = 0; i < ConstructionRules.WorkIntervalTicks; i++)
        {
            world.AdvanceWorldTick();
        }

        var projected = Assert.Single(CityMacroSnapshot.From(world).Projects);

        Assert.True(projected.IsUnderConstruction);
        Assert.Equal(project.Progress, projected.Progress);
        Assert.Equal(project.RequiredWork, projected.RequiredWork);
        Assert.True(projected.Progress > 0);
    }

    [Fact]
    public void HeroProfileSnapshot_ProjectsPresentationData()
    {
        var snapshot = HeroProfileSnapshot.From(TestHelpers.NewHeroWorld());

        Assert.NotNull(snapshot);
        Assert.Equal("Aster", snapshot!.Name);
        Assert.False(string.IsNullOrWhiteSpace(snapshot.LineageName));
        Assert.NotEmpty(snapshot.Aptitudes);
        Assert.NotEmpty(snapshot.ProfessionalAffinities);
        Assert.Equal(100, snapshot.CubeProfile.Body + snapshot.CubeProfile.Bond);
        Assert.False(string.IsNullOrWhiteSpace(snapshot.LineageSignature));
        Assert.True(snapshot.MaxStamina > 0);
    }

    [Fact]
    public void EventLog_CompactsOnlyConsecutiveAdditiveEvents()
    {
        var log = new WorldEventLog();
        var forest = WorldEventSubject.Building(new BuildingId(1), "Forest");
        log.Record(1, WorldEventKind.StockProduced, forest, 1);
        log.Record(2, WorldEventKind.StockProduced, forest, 3);
        log.Record(3, WorldEventKind.DayBegan, WorldEventSubject.World("Sun"));
        log.Record(4, WorldEventKind.StockProduced, forest, 4);

        var compacted = ChronicleEventProjection.Compact(log.Events);

        Assert.Equal(3, compacted.Count);
        Assert.Equal(4, compacted[0].Amount);
        Assert.Equal("Forest produced +4", compacted[0].Summary);
        Assert.Equal(1, compacted[0].FirstTick);
        Assert.Equal(2, compacted[0].LastTick);
        Assert.Equal(WorldEventKind.DayBegan, compacted[1].Kind);
        Assert.Equal(4, compacted[2].Amount);
    }

    [Fact]
    public void ChronicleProjection_RemovesResourceGainReportsButKeepsHistory()
    {
        var log = new WorldEventLog();
        log.Record(
            1,
            WorldEventKind.StockProduced,
            WorldEventSubject.Patch(200, ResourceType.PlantFiber.ToString()),
            2);
        log.Record(
            2,
            WorldEventKind.CropHarvested,
            WorldEventSubject.CultivationSite(new BuildingId(20), "Cultivation Site"),
            5);
        log.Record(3, WorldEventKind.DayBegan, WorldEventSubject.World("Sun"));
        log.Record(
            4,
            WorldEventKind.ProjectCompleted,
            WorldEventSubject.ConstructionProject(new BuildingId(2), "Shelter"));

        IReadOnlyList<WorldEvent> visible =
            ChronicleEventProjection.MeaningfulEvents(log.Events);

        Assert.Equal(2, visible.Count);
        Assert.DoesNotContain(visible, evt => evt.Kind == WorldEventKind.StockProduced);
        Assert.DoesNotContain(visible, evt => evt.Kind == WorldEventKind.CropHarvested);
        Assert.Contains(visible, evt => evt.Kind == WorldEventKind.DayBegan);
        Assert.Contains(visible, evt => evt.Kind == WorldEventKind.ProjectCompleted);
    }

    [Fact]
    public void ExpeditionRailSnapshot_ProjectsOnlyTruthfulActiveExpeditionState()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        world.GatherWood(new BuildingId(100), 2);
        ExpeditionStartResult started = world.StartExpedition(
            ExpeditionRequest.Reconnaissance(world.Hero!.Id));

        Assert.True(started.IsSuccess);
        ExpeditionRailSnapshot snapshot = ExpeditionRailSnapshot.From(world);
        ExpeditionRailSnapshot.Item item = Assert.Single(snapshot.ActiveExpeditions);
        Assert.Equal("Reconnaissance", item.DisplayName);
        Assert.Equal(ExpeditionPhase.Outbound, item.Phase);
        Assert.Equal(new[] { "Aster" }, item.MemberNames);
        Assert.Equal(ResourceType.Wood, item.SupplyResource);
        Assert.Equal(1, item.SupplyAmount);
        Assert.True(item.CanCancel);
        Assert.InRange(item.Progress(snapshot.CurrentTick), 0d, 1d);

        Assert.True(world.CancelExpedition(started.ExpeditionId!.Value));
        Assert.Empty(ExpeditionRailSnapshot.From(world).ActiveExpeditions);
    }

    [Fact]
    public void ExpeditionRailSnapshot_EmptyWorldHasNoInventedCardOrQueue()
    {
        ExpeditionRailSnapshot snapshot = ExpeditionRailSnapshot.From(
            TestHelpers.NewHeroWorld());

        Assert.Empty(snapshot.ActiveExpeditions);
        Assert.DoesNotContain(
            typeof(ExpeditionRailSnapshot).GetProperties(),
            property => property.Name.Contains("Queue", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ExpeditionLiveSnapshot_ProjectsActiveRouteAndRealFounderOnly()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        world.GatherWood(new BuildingId(100), 2);
        ExpeditionStartResult started = world.StartExpedition(
            ExpeditionRequest.Reconnaissance(world.Hero!.Id));

        Assert.True(started.IsSuccess);
        ExpeditionLiveSnapshot snapshot = Assert.IsType<ExpeditionLiveSnapshot>(
            ExpeditionLiveSnapshot.From(world, started.ExpeditionId!.Value));

        Assert.Equal(started.ExpeditionId.Value, snapshot.Id);
        Assert.Equal("Reconnaissance", snapshot.DisplayName);
        Assert.Equal(ExpeditionPhase.Outbound, snapshot.Phase);
        ExpeditionLiveSnapshot.Member founder = Assert.Single(snapshot.Members);
        Assert.Equal(world.Hero.Id, founder.Id);
        Assert.Equal("Aster", founder.Name);
        Assert.NotNull(founder.HealthRatio);
        Assert.InRange(founder.HealthRatio!.Value, 0d, 1d);
        Assert.Equal(world.Hero.CurrentStamina, founder.CurrentStamina);
        Assert.InRange(snapshot.Progress, 0d, 1d);
        Assert.False(snapshot.RetreatTriggered);
        Assert.Equal(
            new[]
            {
                ExpeditionLiveSnapshot.RouteStepState.Complete,
                ExpeditionLiveSnapshot.RouteStepState.Active,
                ExpeditionLiveSnapshot.RouteStepState.Pending,
                ExpeditionLiveSnapshot.RouteStepState.Pending,
                ExpeditionLiveSnapshot.RouteStepState.Pending,
            },
            snapshot.RouteSteps);

        Assert.True(world.CancelExpedition(started.ExpeditionId.Value));
        Assert.Null(ExpeditionLiveSnapshot.From(world, started.ExpeditionId.Value));
    }

    [Theory]
    [InlineData(ExpeditionPhase.Outbound, false, 1, false)]
    [InlineData(ExpeditionPhase.Encounter, false, 2, false)]
    [InlineData(ExpeditionPhase.Objective, false, 3, false)]
    [InlineData(ExpeditionPhase.Retreating, true, 4, true)]
    [InlineData(ExpeditionPhase.Returning, false, 4, false)]
    [InlineData(ExpeditionPhase.Returning, true, 4, true)]
    public void ExpeditionLiveRoute_ProjectsSuccessAndRetreatTruthfully(
        ExpeditionPhase phase,
        bool retreatTriggered,
        int activeIndex,
        bool objectiveSkipped)
    {
        IReadOnlyList<ExpeditionLiveSnapshot.RouteStepState> route =
            ExpeditionLiveSnapshot.ProjectRoute(phase, retreatTriggered);

        Assert.Equal(5, route.Count);
        Assert.Equal(ExpeditionLiveSnapshot.RouteStepState.Active, route[activeIndex]);
        Assert.Equal(
            objectiveSkipped
                ? ExpeditionLiveSnapshot.RouteStepState.Skipped
                : phase is ExpeditionPhase.Returning
                    ? ExpeditionLiveSnapshot.RouteStepState.Complete
                    : phase is ExpeditionPhase.Objective
                        ? ExpeditionLiveSnapshot.RouteStepState.Active
                        : ExpeditionLiveSnapshot.RouteStepState.Pending,
            route[3]);
    }

    [Fact]
    public void ActivityFeed_UsesTheChroniclesMeaningfulNewestProjection()
    {
        var log = new WorldEventLog();
        log.Record(1, WorldEventKind.StockProduced, WorldEventSubject.World("Store"), 4);
        log.Record(2, WorldEventKind.DayBegan, WorldEventSubject.World("Sun"));
        log.Record(3, WorldEventKind.ProjectCompleted,
            WorldEventSubject.ConstructionProject(new BuildingId(2), "Shelter"));

        var newest = WorldofGoses.Presentation.ChronicleEventProjection.NewestMeaningful(
            log.Events, 1);

        var item = Assert.Single(newest);
        Assert.Equal(WorldEventKind.ProjectCompleted, item.Kind);
        Assert.Equal(3, item.LastTick);
    }

    [Fact]
    public void EventLog_CompactsRepeatedConsecutiveStateEvents()
    {
        var log = new WorldEventLog();
        var forest = WorldEventSubject.Building(new BuildingId(1), "Forest");
        log.Record(1, WorldEventKind.StockCapped, forest);
        log.Record(2, WorldEventKind.StockCapped, forest);
        log.Record(3, WorldEventKind.DayBegan, WorldEventSubject.World("Sun"));
        log.Record(4, WorldEventKind.StockCapped, forest);

        var compacted = ChronicleEventProjection.Compact(log.Events);

        Assert.Equal(3, compacted.Count);
        Assert.Equal(1, compacted[0].FirstTick);
        Assert.Equal(2, compacted[0].LastTick);
        Assert.Equal(4, compacted[2].LastTick);
    }

    [Theory]
    [InlineData(0, "Day 1 · 00:00")]
    [InlineData(900, "Day 1 · 06:00")]
    [InlineData(4500, "Day 2 · 06:00")]
    public void EventLog_FormatsSimulationTimeForPlayers(int tick, string expected)
    {
        Assert.Equal(expected, SimulationTimeText.Format(tick));
    }

    [Theory]
    [InlineData(0, "Day 1 · 00:00")]
    [InlineData(32400, "Day 10 · 00:00")]
    [InlineData(356400, "Day 100 · 00:00")]
    public void SimulationTimeText_KeepsOneTwoAndThreeDigitDaysAuthoritative(
        int tick,
        string expected)
    {
        Assert.Equal(expected, SimulationTimeText.Format(tick));
    }

    [Theory]
    [InlineData(0, "0 minutes")]
    [InlineData(1, "1 minute")]
    [InlineData(10, "4 minutes")]
    [InlineData(150, "1 hour")]
    [InlineData(160, "1 hour 4 minutes")]
    [InlineData(3599, "1 day")]
    [InlineData(3600, "1 day")]
    [InlineData(7200, "2 days")]
    public void DurationFormatter_NeverExposesSimulationTicks(int ticks, string expected)
    {
        Assert.Equal(expected, SimulationTimeText.FormatDuration(ticks));
        Assert.DoesNotContain("tick", expected, System.StringComparison.OrdinalIgnoreCase);
    }
}
