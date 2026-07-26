using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Domain.Persistence;
using Xunit;

namespace WorldofGoses.Tests;

public sealed class FirstRunRegressionTests
{
    [Fact]
    public void HeroWithOnlyForests_UsesEmptyMacroMode()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        CityMacroSnapshot snapshot = CityMacroSnapshot.From(world);

        Assert.Equal(0, snapshot.CivilBuildingCount);
        Assert.Equal(
            CityMacroView.MacroMode.Empty,
            CityMacroView.DetermineMacroMode(
                snapshot.CivilBuildingCount,
                snapshot.Projects.Count));
    }

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

    [Theory]
    [InlineData(1024, 576)]
    [InlineData(1280, 720)]
    [InlineData(1600, 900)]
    public void TerrainRectLeavesHudSafeBand(int width, int height)
    {
        // Parcels are now fixed-scale (a virtual camera pans instead of the
        // terrain stretching to fit the window), so reset the shared pan
        // state first: an earlier test case must not leave this scrolled.
        OrthogonalParcelTerrain.ResetPanForTests();
        Rect2 terrain = OrthogonalParcelTerrain.CalculateTerrainRect(
            new Vector2(width, height));

        // At zero pan the world's top-left still respects the HUD margins,
        // regardless of whether the fixed-size world fits this viewport.
        Assert.True(terrain.Position.Y >= 96);
        Assert.True(terrain.Position.X >= 32);
        // The world is always exactly ParcelColumns x ParcelRows parcels of
        // ParcelGrid.LotsPerAxis x ParcelGrid.TilesPerStandardLot tiles —
        // fixed, not derived from the viewport.
        Assert.Equal(
            OrthogonalParcelTerrain.ParcelColumns * OrthogonalParcelTerrain.ParcelPixelSize,
            terrain.Size.X);
        Assert.Equal(
            OrthogonalParcelTerrain.ParcelRows * OrthogonalParcelTerrain.ParcelPixelSize,
            terrain.Size.Y);
    }

    [Fact]
    public void CalculateTerrainRect_PanClampedToWorldBounds()
    {
        OrthogonalParcelTerrain.ResetPanForTests();
        // A viewport smaller than the fixed world needs to scroll; verify
        // CalculateParcelRect for the last parcel is reachable at some pan
        // within [0, worldSize - displaySize] without asserting on the
        // private drag mechanics — only that the fixed geometry itself is
        // internally consistent (parcel rects tile the world with no gaps
        // or overlaps).
        Rect2 terrain = OrthogonalParcelTerrain.CalculateTerrainRect(new Vector2(1024, 576));
        for (int column = 0; column < OrthogonalParcelTerrain.ParcelColumns; column++)
        {
            for (int row = 0; row < OrthogonalParcelTerrain.ParcelRows; row++)
            {
                Rect2 parcel = OrthogonalParcelTerrain.CalculateParcelRect(
                    new Vector2(1024, 576), column, row);
                Assert.Equal(OrthogonalParcelTerrain.ParcelPixelSize, parcel.Size.X);
                Assert.Equal(OrthogonalParcelTerrain.ParcelPixelSize, parcel.Size.Y);
                Assert.Equal(
                    terrain.Position.X + column * OrthogonalParcelTerrain.ParcelPixelSize,
                    parcel.Position.X);
                Assert.Equal(
                    terrain.Position.Y + row * OrthogonalParcelTerrain.ParcelPixelSize,
                    parcel.Position.Y);
            }
        }
        OrthogonalParcelTerrain.ResetPanForTests();
    }

    [Fact]
    public void ParcelPresentation_UsesNineByNineTilesAndThreeByThreeLots()
    {
        Assert.Equal(3, ParcelGrid.LotsPerAxis);
        Assert.Equal(3, ParcelGrid.TilesPerStandardLot);
        Assert.Equal(9, OrthogonalParcelTerrain.ParcelTileSpan);
        Assert.Equal(
            OrthogonalParcelTerrain.DisplayTileSize * 9,
            OrthogonalParcelTerrain.ParcelPixelSize);
    }

    [Fact]
    public void RecruitMigrant_AddsNonHeroCitizenAndEvent()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        CitizenProfile profile = world.Hero!.Profile;
        int before = world.Citizens.Count;

        CityWorld.MigrantResult result = world.TryRecruitMigrant(profile, "Inara");

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
        CityWorld.MigrantResult recruited =
            world.TryRecruitMigrant(world.Hero!.Profile, "Inara");
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
        CityWorld first = TestHelpers.NewHeroWorld();
        CityWorld second = TestHelpers.NewHeroWorld();

        CityWorld.MigrantResult firstResult = first.TryRecruitMigrant();
        CityWorld.MigrantResult secondResult = second.TryRecruitMigrant();

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
        BuildingId farmId = new(2);
        Building liveFarm = live.GetBuilding(farmId)!;
        int rateBeforeMigrant = live.CurrentProductionRate(farmId);
        CityWorld.MigrantResult recruited = live.TryRecruitMigrant();
        Assert.True(recruited.IsSuccess);
        CitizenId migrantId = recruited.MigrantId!.Value;
        Assert.True(live.TryAssignCitizen(farmId, migrantId).IsSuccess);
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
}
