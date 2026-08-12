using System;
using System.Linq;
using WorldofGoses.Domain;
using WorldofGoses.Persistence;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>EG-3 first-plot lifecycle, persistence and offline equivalence.</summary>
public sealed class Eg3CultivationSiteTests
{
    [Fact]
    public void Authorize_RequiresShelterAndPaysFullPreparationCost()
    {
        CityWorld withoutShelter = TestHelpers.NewHeroWorld();
        withoutShelter.Resources.DepositToCityInventory(ResourceType.Branches, 1);
        withoutShelter.Resources.DepositToCityInventory(ResourceType.SmallStone, 1);
        Assert.Equal(
            ConstructionAuthorizationOutcome.HomeRequired,
            withoutShelter.TryAuthorizeConstruction(ConstructionKind.CultivationSite).Outcome);

        CityWorld world = CultivationWorld();
        ConstructionAuthorizationResult result =
            world.TryAuthorizeConstruction(ConstructionKind.CultivationSite);

        Assert.True(result.IsSuccess, result.Outcome.ToString());
        ConstructionProject project = world.GetProject(result.ProjectId!.Value)!;
        Assert.Equal(CultivationRules.PreparationWork, project.RequiredWork);
        Assert.Equal(CultivationRules.WorkerCapacity, project.WorkerCapacity);
        Assert.Equal(0, world.TotalStockOf(ResourceType.Branches));
        Assert.Equal(0, world.TotalStockOf(ResourceType.SmallStone));
    }

    [Fact]
    public void CompletedPreparation_PreservesPlacementAndCreatesPreparedPlot()
    {
        CityWorld world = CultivationWorld();
        BuildingId id = AuthorizeAndCompletePreparation(world);
        ParcelPlacement placement = world.ParcelPlacements[id];

        Assert.Empty(world.Projects);
        CultivationSite site = Assert.Single(world.CultivationSites).Value;
        Assert.Equal(id, site.Id);
        Assert.Equal(CultivationPlotState.Prepared, site.State);
        Assert.Equal(id, placement.EntityId);
        Assert.Null(site.PlantedTick);
        Assert.Null(site.ReadyAtTick);
    }

    [Fact]
    public void Sow_GrowsForExactlyThreeDays_ThenHarvestsFiveFood()
    {
        CityWorld world = CultivationWorld();
        BuildingId id = AuthorizeAndCompletePreparation(world);
        world.Resources.DepositToCityInventory(ResourceType.Food, 20);
        int beforeSow = world.FoodStock;

        CultivationActionResult sow = world.TrySowCultivationSite(id);

        Assert.True(sow.IsSuccess);
        Assert.Equal(beforeSow - CultivationRules.SeedFoodCost, world.FoodStock);
        CultivationSite site = world.GetCultivationSite(id)!;
        Assert.Equal(CultivationPlotState.Sown, site.State);
        Assert.Equal(world.CurrentTick + CultivationRules.GrowthTicks, site.ReadyAtTick);

        for (int tick = 0; tick < CultivationRules.GrowthTicks - 1; tick++)
        {
            world.AdvanceWorldTick();
        }
        Assert.Equal(CultivationPlotState.Growing, site.State);
        Assert.Equal(CultivationActionOutcome.WrongState,
            world.TryHarvestCultivationSite(id).Outcome);

        world.AdvanceWorldTick();
        Assert.Equal(CultivationPlotState.Ready, site.State);
        int beforeHarvest = world.FoodStock;
        CultivationActionResult harvest = world.TryHarvestCultivationSite(id);
        Assert.True(harvest.IsSuccess);
        Assert.Equal(CultivationRules.HarvestFoodYield, harvest.FoodDelta);
        Assert.Equal(beforeHarvest + CultivationRules.HarvestFoodYield, world.FoodStock);
        Assert.Equal(CultivationPlotState.Spent, site.State);
        Assert.Single(world.Log.Events, evt =>
            evt.Kind == WorldEventKind.CropReady && evt.Subject.EntityId == id.Value);
        Assert.Single(world.Log.Events, evt =>
            evt.Kind == WorldEventKind.CropHarvested && evt.Subject.EntityId == id.Value);
    }

    [Fact]
    public void Sow_AcceptsStartingWildFoodAndStatusProjectsProtectedHorizon()
    {
        CityWorld world = CultivationWorld();
        BuildingId id = AuthorizeAndCompletePreparation(world);
        world.Resources.DepositToCityInventory(ResourceType.WildFood, 8);
        CityStatusSnapshot before = CityStatusSnapshot.From(world);

        Assert.Equal(8, before.FoodStock);
        Assert.Equal(8, before.FoodHorizonDays);
        Assert.Equal(5, before.ProtectedFoodTarget);
        Assert.Equal(CultivationRules.GrowthTicks, before.TicksUntilFirstHarvest);

        Assert.True(world.TrySowCultivationSite(id).IsSuccess);
        Assert.Equal(7, world.FoodStock);
        Assert.Equal(7, world.TotalStockOf(ResourceType.WildFood));
    }

    [Fact]
    public void MacroSnapshot_ProjectsPreparedCultivationSiteOnItsStableLot()
    {
        CityWorld world = CultivationWorld();
        BuildingId id = AuthorizeAndCompletePreparation(world);
        ParcelPlacement placement = world.ParcelPlacements[id];

        CityMacroSnapshot.PlotItem item = CityMacroSnapshot.From(world).Buildings
            .Single(plot => plot.Id == id);

        Assert.Equal(BuildingKind.CultivationSite, item.Kind);
        Assert.Equal(CultivationPlotState.Prepared, item.CultivationState);
        Assert.Equal(placement.ParcelId, item.ParcelId);
        Assert.Equal(placement.LotColumn, item.LotColumn);
        Assert.Equal(placement.LotRow, item.LotRow);
        Assert.True(item.Enabled);
    }

    [Fact]
    public void OfflineAndLiveAdvancement_ReachSameReadyBoundaryExactlyOnce()
    {
        CityWorld source = CultivationWorld();
        BuildingId id = AuthorizeAndCompletePreparation(source);
        source.Resources.DepositToCityInventory(ResourceType.Food, 20);
        Assert.True(source.TrySowCultivationSite(id).IsSuccess);
        source.Hero!.RestoreStamina(source.Hero.MaxStamina);
        WorldSave baseline = WorldPersistence.Capture(source);
        CityWorld live = WorldPersistence.FromSave(Clone(baseline));
        CityWorld offline = WorldPersistence.FromSave(Clone(baseline));

        for (int tick = 0; tick < CultivationRules.GrowthTicks; tick++)
        {
            live.AdvanceWorldTick();
        }
        WorldTimeAdvance.Result report =
            WorldTimeAdvance.Advance(offline, CultivationRules.GrowthTicks);

        Assert.Equal(CultivationPlotState.Ready, live.GetCultivationSite(id)!.State);
        Assert.Equal(CultivationPlotState.Ready, offline.GetCultivationSite(id)!.State);
        Assert.Equal(live.CurrentTick, offline.CurrentTick);
        Assert.Equal(live.FoodStock, offline.FoodStock);
        Assert.Equal(1, live.Log.Events.Count(evt => evt.Kind == WorldEventKind.CropReady));
        Assert.Equal(1, offline.Log.Events.Count(evt => evt.Kind == WorldEventKind.CropReady));
        Assert.True(report.BatchedTicks > 0);
    }

    [Fact]
    public void RoundTrip_PreservesGrowingPlotAndSemanticBoundaries()
    {
        CityWorld world = CultivationWorld();
        BuildingId id = AuthorizeAndCompletePreparation(world);
        world.Resources.DepositToCityInventory(ResourceType.Food, 3);
        Assert.True(world.TrySowCultivationSite(id).IsSuccess);
        world.AdvanceWorldTick();

        WorldSave save = WorldPersistence.Capture(world);
        WorldPersistence.Validate(save);
        CityWorld restored = WorldPersistence.FromSave(Clone(save));
        CultivationSite plot = restored.GetCultivationSite(id)!;

        Assert.Equal(CultivationPlotState.Growing, plot.State);
        Assert.Equal(world.GetCultivationSite(id)!.PlantedTick, plot.PlantedTick);
        Assert.Equal(world.GetCultivationSite(id)!.ReadyAtTick, plot.ReadyAtTick);
        Assert.Equal(world.ParcelPlacements[id].EntityId, restored.ParcelPlacements[id].EntityId);
        Assert.Equal(world.ParcelPlacements[id].ParcelId, restored.ParcelPlacements[id].ParcelId);
        Assert.Equal(world.ParcelPlacements[id].LotColumn, restored.ParcelPlacements[id].LotColumn);
        Assert.Equal(world.ParcelPlacements[id].LotRow, restored.ParcelPlacements[id].LotRow);
        WorldPersistence.Validate(WorldPersistence.Capture(restored));
    }

    [Fact]
    public void MigrateV23ToV24_AddsNoInventedCultivationSite()
    {
        WorldSave save = WorldPersistence.Capture(TestHelpers.NewHeroWorld());
        save.Version = 23;
        save.CultivationSites = null!;

        WorldSave migrated = WorldPersistence.MigrateV23ToV24(save);

        Assert.Equal(24, migrated.Version);
        Assert.Empty(migrated.CultivationSites);
        WorldSave current = WorldPersistence.MigrateToCurrent(migrated);
        WorldPersistence.Validate(current);
    }

    [Fact]
    public void Validate_RejectsCompletedSitePlusSecondCultivationProject()
    {
        CityWorld completed = CultivationWorld();
        AuthorizeAndCompletePreparation(completed);
        WorldSave save = WorldPersistence.Capture(completed);
        CityWorld pending = CultivationWorld();
        BuildingId pendingId = pending.TryAuthorizeConstruction(
            ConstructionKind.CultivationSite).ProjectId!.Value;
        WorldSave pendingSave = WorldPersistence.Capture(pending);
        ConstructionProjectSave project = Assert.Single(pendingSave.Projects);
        project.Id = 9999;
        ParcelPlacementSave placement = Assert.Single(
            pendingSave.ParcelPlacements,
            item => item.EntityId == pendingId.Value);
        placement.EntityId = project.Id;
        ParcelSave freeParcel = save.Parcels.First(parcel =>
            parcel.IsUnlocked
            && save.ParcelPlacements.All(existing => existing.ParcelId != parcel.Id));
        placement.ParcelId = freeParcel.Id;
        placement.LotColumn = 0;
        placement.LotRow = 0;
        placement.RowId = ParcelGrid.ConstructionRow(freeParcel.LogicalRow, 0).Value;
        placement.StartColumn = ParcelGrid.GlobalFrontageColumn(freeParcel.LogicalColumn, 0);
        save.Projects.Add(project);
        save.ParcelPlacements.Add(placement);

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(
            () => WorldPersistence.Validate(save));

        Assert.Contains("more than one", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_RejectsUndefinedNumericCultivationState()
    {
        CityWorld world = CultivationWorld();
        AuthorizeAndCompletePreparation(world);
        WorldSave save = WorldPersistence.Capture(world);
        Assert.Single(save.CultivationSites).State = "99";

        Assert.Throws<InvalidOperationException>(() => WorldPersistence.Validate(save));
    }

    private static CityWorld CultivationWorld()
    {
        CityWorld world = TestHelpers.WorldWithHome();
        world.Resources.DepositToCityInventory(ResourceType.Branches, 1);
        world.Resources.DepositToCityInventory(ResourceType.SmallStone, 1);
        return world;
    }

    private static BuildingId AuthorizeAndCompletePreparation(CityWorld world)
    {
        ConstructionAuthorizationResult result =
            world.TryAuthorizeConstruction(ConstructionKind.CultivationSite);
        Assert.True(result.IsSuccess, result.Outcome.ToString());
        BuildingId id = result.ProjectId!.Value;
        ConstructionProject project = world.GetProject(id)!;
        project.Progress = project.RequiredWork;
        world.AdvanceWorldTick();
        return id;
    }

    private static WorldSave Clone(WorldSave save) =>
        WorldPersistence.DeserializeFromJson(WorldPersistence.SerializeToJson(save));
}
