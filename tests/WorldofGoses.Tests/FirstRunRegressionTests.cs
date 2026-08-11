using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Domain.Persistence;
using Xunit;

namespace WorldofGoses.Tests;

public sealed class FirstRunRegressionTests
{
    [Fact]
    public void ConstructionSnapshot_UsesAvailableAfterReservations()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.Resources.DepositToCityInventory(ResourceType.Wood, 4);
        Assert.True(world.Resources.TryReserve(
            ResourceType.Wood,
            3,
            new ResourceReservationOwner(ResourceReservationOwnerKind.Expedition, 1),
            out _));

        ConstructionSnapshot.OptionItem shelter =
            ConstructionSnapshot.From(world).OptionFor(ConstructionKind.BasicShelter);

        Assert.Equal(1, shelter.Materials[0].Available);
        Assert.True(shelter.CanPayDeposit);
    }

    [Fact]
    public void ShelterWaitsForRemainingMaterialsBeforeAssigningFounder()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        world.Resources.DepositToCityInventory(ResourceType.Wood, 1);

        ConstructionAuthorizationResult result =
            world.TryAuthorizeConstruction(ConstructionKind.BasicShelter);

        Assert.True(result.IsSuccess);
        Assert.Null(world.Hero!.CurrentAssignment);

        world.Resources.DepositToCityInventory(ResourceType.Wood, 3);
        Assert.True(world.EnsureFoundingShelterContributor());
        Assert.Equal(result.ProjectId, world.Hero.CurrentAssignment);
    }

    [Fact]
    public void RecruitMigrant_AddsNonHeroCitizenAndEvent()
    {
        CityWorld world = TestHelpers.WorldWithHome();
        AddTownHall(world);
        CitizenProfile profile = world.Hero!.Profile;
        int before = world.Citizens.Count;

        Assert.Equal(CityWorld.MigrantOutcome.Success,
            world.TryHostExpeditionProspect(profile, "Inara"));
        CityWorld.MigrantResult result = world.TryAcceptPendingProspect();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, world.Citizens.Count);
        Citizen migrant = world.Citizens[result.MigrantId!.Value];
        Assert.False(migrant.IsHero);
        Assert.Equal("Inara", migrant.Name);
        Assert.Equal(CitizenLocation.AtHome, migrant.CurrentLocation);
        Assert.Null(migrant.CurrentAssignment);
        Assert.Contains(world.Log.Events,
            evt => evt.Kind == WorldEventKind.MigrantArrived
                && evt.Subject.EntityId == migrant.Id.Value);
    }

    [Fact]
    public void RecruitedCitizen_IsIdentifiableAndAssignableInMacroSnapshot()
    {
        CityWorld world = TestHelpers.NewProductionWorld();
        AddHousingPlace(world);
        AddTownHall(world);
        CityWorld.MigrantResult recruited =
            AcceptProspect(world, world.Hero!.Profile, "Inara");
        Assert.True(recruited.IsSuccess);
        CitizenId migrantId = recruited.MigrantId!.Value;
        BuildingId farmId = world.PrimaryBuilding.Id;

        Assert.True(world.TryAssignCitizen(farmId, migrantId).IsSuccess);

        CityMacroSnapshot.CitizenItem migrant = Assert.Single(
            CityMacroSnapshot.From(world).Citizens,
            item => item.Id == migrantId);
        Assert.Equal("Inara", migrant.Name);
        Assert.False(migrant.IsHero);
        Assert.False(migrant.IsAvailable);
        Assert.Equal(farmId, migrant.CurrentAssignment);
    }

    [Fact]
    public void GeneratedMigrant_HasStableIdentityDistinctFromFounder()
    {
        CityWorld first = TestHelpers.WorldWithHome();
        CityWorld second = TestHelpers.WorldWithHome();
        AddTownHall(first);
        AddTownHall(second);

        CityWorld.MigrantResult firstResult = AcceptProspect(first);
        CityWorld.MigrantResult secondResult = AcceptProspect(second);

        Assert.True(firstResult.IsSuccess);
        Assert.True(secondResult.IsSuccess);
        Citizen firstMigrant = first.GetCitizen(firstResult.MigrantId!.Value)!;
        Citizen secondMigrant = second.GetCitizen(secondResult.MigrantId!.Value)!;
        Assert.Equal(firstMigrant.Name, secondMigrant.Name);
        Assert.Equal(firstMigrant.Profile.Lineage, secondMigrant.Profile.Lineage);
        Assert.NotEqual(first.Hero!.Profile.Lineage, firstMigrant.Profile.Lineage);
        Assert.NotEqual(first.Hero.Name, firstMigrant.Name);
    }

    [Fact]
    public void MigrantProduction_IsEquivalentLiveAndAfterSaveOfflineCatchUp()
    {
        CityWorld live = TestHelpers.NewProductionWorld();
        AddHousingPlace(live);
        AddTownHall(live);
        BuildingId farmId = new(2);
        Building liveFarm = live.GetBuilding(farmId)!;
        int rateBeforeMigrant = live.CurrentProductionRate(farmId);
        CityWorld.MigrantResult recruited = AcceptProspect(live);
        Assert.True(recruited.IsSuccess);
        CitizenId migrantId = recruited.MigrantId!.Value;
        Assert.True(live.TryAssignCitizen(farmId, migrantId).IsSuccess);
        TestHelpers.PlaceAtAssignment(live, migrantId);
        Assert.True(live.CurrentProductionRate(farmId) > rateBeforeMigrant);

        string json = WorldPersistence.SerializeToJson(
            WorldPersistence.Capture(live));
        CityWorld offline = CityWorld.FromSave(
            WorldPersistence.DeserializeFromJson(json));
        Citizen restoredMigrant = offline.GetCitizen(migrantId)!;
        Assert.Equal(farmId, restoredMigrant.CurrentAssignment);

        const int ticks = 12;
        for (int index = 0; index < ticks; index++)
        {
            live.AdvanceWorldTick();
        }
        OfflineProgression.ApplyAll(offline, ticks);

        Assert.Equal(liveFarm.Stock, offline.GetBuilding(farmId)!.Stock);
        Assert.Equal(
            live.GetCitizen(migrantId)!.CurrentStamina,
            offline.GetCitizen(migrantId)!.CurrentStamina);
        Assert.Equal(
            live.GetCitizen(migrantId)!.GetExperience(CompetencyId.Farming),
            offline.GetCitizen(migrantId)!.GetExperience(CompetencyId.Farming));
    }

    [Fact]
    public void RecruitMigrant_WithoutCompletedShelter_IsRejected()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        AddTownHall(world);
        Assert.Equal(CityWorld.MigrantOutcome.Success,
            world.TryHostExpeditionProspect());

        CityWorld.MigrantResult result = world.TryAcceptPendingProspect();

        Assert.Equal(CityWorld.MigrantOutcome.AtCapacity, result.Outcome);
        Assert.Single(world.Citizens);
    }

    [Fact]
    public void RecruitMigrant_StopsWhenShelterHousingIsFull()
    {
        CityWorld world = TestHelpers.WorldWithHome();
        AddTownHall(world);
        int capacity = world.HousingCapacity;

        while (world.Citizens.Count < capacity)
        {
            Assert.True(AcceptProspect(world).IsSuccess);
        }

        Assert.Equal(CityWorld.MigrantOutcome.Success,
            world.TryHostExpeditionProspect());
        CityWorld.MigrantResult result = world.TryAcceptPendingProspect();

        Assert.Equal(CityWorld.MigrantOutcome.AtCapacity, result.Outcome);
        Assert.Equal(capacity, world.Citizens.Count);
        Assert.Equal(0, world.AvailableHousing);
    }

    [Fact]
    public void TownHall_HostsOnlyOnePendingProspect()
    {
        CityWorld world = TestHelpers.WorldWithHome();
        AddTownHall(world);

        Assert.Equal(CityWorld.MigrantOutcome.Success,
            world.TryHostExpeditionProspect("Inara"));
        Assert.Equal(CityWorld.MigrantOutcome.ProspectAlreadyWaiting,
            world.TryHostExpeditionProspect("Second prospect"));
        Assert.Equal("Inara", world.PendingProspect!.Name);
        Assert.Single(world.Citizens);
    }

    [Fact]
    public void PendingProspect_SurvivesSaveAndLoadWithoutBecomingCitizen()
    {
        CityWorld world = TestHelpers.WorldWithHome();
        AddTownHall(world);
        Assert.Equal(CityWorld.MigrantOutcome.Success,
            world.TryHostExpeditionProspect("Inara"));

        CityWorld restored = CityWorld.FromSave(WorldPersistence.DeserializeFromJson(
            WorldPersistence.SerializeToJson(WorldPersistence.Capture(world))));

        CitizenProspect sourceProspect = Assert.IsType<CitizenProspect>(world.PendingProspect);
        CitizenProspect restoredProspect = Assert.IsType<CitizenProspect>(restored.PendingProspect);
        Assert.Equal("Inara", restoredProspect.Name);
        Assert.Equal(sourceProspect.Profile.Lineage, restoredProspect.Profile.Lineage);
        Assert.Single(restored.Citizens);
    }

    [Fact]
    public void ProspectExpedition_ReturnsProspectToTownHallNotCitizenRoster()
    {
        CityWorld world = TestHelpers.NewProductionWorld();
        AddTownHall(world);
        Assert.True(world.TryUnassignCitizen(new BuildingId(1), world.Hero!.Id).IsSuccess);
        world.Resources.DepositToCityInventory(ResourceType.Food, 2);
        ExpeditionRequest request = ExpeditionRequest.SeekProspect(world.Hero!.Id) with
        {
            DurationTicks = 1,
        };

        ExpeditionStartResult started = world.StartExpedition(request);
        Assert.True(started.IsSuccess);
        world.AdvanceWorldTick();

        Expedition expedition = world.Expeditions[started.ExpeditionId!.Value];
        Assert.Equal(ExpeditionStatus.Returned, expedition.Status);
        Assert.NotNull(world.PendingProspect);
        Assert.Equal(5, world.Citizens.Count);
    }

    [Fact]
    public void ProspectExpedition_WithoutTownHallIsRejectedBeforeSpendingFood()
    {
        CityWorld world = TestHelpers.NewProductionWorld();
        Assert.True(world.TryUnassignCitizen(new BuildingId(1), world.Hero!.Id).IsSuccess);
        world.Resources.DepositToCityInventory(ResourceType.Food, 2);
        int foodBefore = world.Resources.Available(ResourceType.Food);

        ExpeditionStartResult result = world.StartExpedition(
            ExpeditionRequest.SeekProspect(world.Hero!.Id));

        Assert.Equal(ExpeditionStartOutcome.TownHallUnavailable, result.Outcome);
        Assert.Equal(foodBefore, world.Resources.Available(ResourceType.Food));
        Assert.Empty(world.Expeditions);
    }

    private static void AddHousingPlace(CityWorld world)
    {
        world.RegisterBuilding(TestHelpers.NewBuilding(
            id: new BuildingId(50),
            kind: BuildingKind.Home,
            workerCapacity: 1,
            visualCapacity: 1,
            baseProductionPerWorker: 0,
            storageCapacity: 0,
            displayName: "Test lodging",
            resourceLabel: "Rest",
            resourceUnit: "rest"));
    }

    private static void AddTownHall(CityWorld world)
    {
        world.RegisterBuilding(TestHelpers.NewBuilding(
            id: new BuildingId(51),
            kind: BuildingKind.TownHall,
            workerCapacity: 0,
            visualCapacity: 0,
            baseProductionPerWorker: 0,
            storageCapacity: 0,
            displayName: "Test Town Hall",
            resourceLabel: "Prospect",
            resourceUnit: "prospect"));
    }

    private static CityWorld.MigrantResult AcceptProspect(
        CityWorld world,
        CitizenProfile? profile = null,
        string? name = null)
    {
        CityWorld.MigrantOutcome hosted = profile is null
            ? world.TryHostExpeditionProspect(name)
            : world.TryHostExpeditionProspect(profile, name ?? "Inara");
        Assert.Equal(CityWorld.MigrantOutcome.Success, hosted);
        return world.TryAcceptPendingProspect();
    }
}
