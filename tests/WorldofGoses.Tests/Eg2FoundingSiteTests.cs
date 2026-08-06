using System;
using System.Linq;
using WorldofGoses.Domain;
using WorldofGoses.Domain.Persistence;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>EG-2 Founding Site seam: phase graph, capacity and persistence.</summary>
public sealed class Eg2FoundingSiteTests
{
    [Fact]
    public void AuthorizeFoundingSite_PaysFullCampfireCostAndKeepsOnePlacement()
    {
        CityWorld world = NewFoundingWorld();
        world.Resources.DepositToCityInventory(ResourceType.Branches, 3);
        world.Resources.DepositToCityInventory(ResourceType.SmallStone, 2);

        ConstructionAuthorizationResult result =
            world.TryAuthorizeConstruction(ConstructionKind.FoundingSite);

        Assert.True(result.IsSuccess, result.Outcome.ToString());
        ConstructionProject project = world.GetProject(result.ProjectId!.Value)!;
        Assert.Equal(FoundingSiteModule.Campfire, project.ActiveFoundingModule);
        Assert.Equal(FoundingSiteRules.WorkPerModule, project.RequiredWork);
        Assert.Equal(0, world.TotalStockOf(ResourceType.Branches));
        Assert.Equal(0, world.TotalStockOf(ResourceType.SmallStone));
        Assert.Equal(project.Id, world.ParcelPlacements[project.Id].EntityId);
        Assert.Equal(BuildingFootprintCatalog.StandardFullWidthId,
            world.ParcelPlacements[project.Id].FootprintProfileId);
    }

    [Theory]
    [InlineData(FoundingSiteModule.Bedroll, FoundingSiteModule.Cache)]
    [InlineData(FoundingSiteModule.Cache, FoundingSiteModule.Bedroll)]
    public void FoundingModules_BedrollAndCacheEitherOrder_CanopyNeedsBoth(
        FoundingSiteModule first,
        FoundingSiteModule second)
    {
        CityWorld world = AuthorizeFoundingSite();
        ConstructionProject project = world.Projects.Values.Single();
        CompleteActiveModule(world, project);

        Assert.True(project.HasCompletedFoundingModule(FoundingSiteModule.Campfire));
        Assert.True(world.HasFoundingSiteModule(FoundingSiteModule.Campfire));
        Assert.False(world.CancelProject(project.Id));
        Assert.Equal(ConstructionAuthorizationOutcome.PrerequisitesNotMet,
            world.TryAuthorizeFoundingSiteModule(project.Id, FoundingSiteModule.Canopy).Outcome);

        DepositCost(world, first);
        Assert.True(world.TryAuthorizeFoundingSiteModule(
            project.Id, first).IsSuccess);
        CompleteActiveModule(world, project);
        Assert.Equal(
            first == FoundingSiteModule.Cache
                ? FoundingSiteRules.CacheCapacity
                : FoundingSiteRules.CarriedCapacity,
            world.GroundResourceCapacity());

        DepositCost(world, second);
        Assert.True(world.TryAuthorizeFoundingSiteModule(
            project.Id, second).IsSuccess);
        CompleteActiveModule(world, project);
        Assert.Equal(FoundingSiteRules.CacheCapacity, world.GroundResourceCapacity());
        int gatheredAfterCache = GatherGroundUnits(world, 7);
        Assert.Equal(7, gatheredAfterCache);
        Assert.Equal(7, world.CarriedGroundResourceCount());

        DepositCost(world, FoundingSiteModule.Canopy);
        Assert.True(world.TryAuthorizeFoundingSiteModule(
            project.Id, FoundingSiteModule.Canopy).IsSuccess);
    }

    [Fact]
    public void Canopy_TransformsSameEntityAndPlacementIntoShelterWithOriginFacts()
    {
        CityWorld world = AuthorizeFoundingSite();
        ConstructionProject project = world.Projects.Values.Single();
        BuildingId siteId = project.Id;
        ParcelPlacement originalPlacement = world.ParcelPlacements[siteId];

        CompleteActiveModule(world, project);
        CompleteModule(world, project, FoundingSiteModule.Bedroll);
        CompleteModule(world, project, FoundingSiteModule.Cache);
        CompleteModule(world, project, FoundingSiteModule.Canopy);

        Assert.Empty(world.Projects);
        Building shelter = world.GetBuilding(siteId)!;
        Assert.Equal(BuildingKind.Home, shelter.Kind);
        Assert.Equal(originalPlacement, world.ParcelPlacements[siteId]);
        Assert.Equal(4, shelter.FoundingSiteOriginModules.Count);
        Assert.Contains(FoundingSiteModule.Campfire, shelter.FoundingSiteOriginModules);
        Assert.Contains(FoundingSiteModule.Canopy, shelter.FoundingSiteOriginModules);
        Assert.Equal(FoundingSiteRules.ShelterCapacity, world.GroundResourceCapacity());

        CityWorld restored = CityWorld.FromSave(WorldPersistence.Capture(world));
        Assert.Equal(4, restored.GetBuilding(siteId)!.FoundingSiteOriginModules.Count);
    }

    [Fact]
    public void Roundtrip_PreservesActiveModuleCompletedModulesAndDepositedInputs()
    {
        CityWorld world = AuthorizeFoundingSite();
        ConstructionProject project = world.Projects.Values.Single();
        CompleteActiveModule(world, project);
        DepositCost(world, FoundingSiteModule.Cache);
        Assert.True(world.TryAuthorizeFoundingSiteModule(
            project.Id, FoundingSiteModule.Cache).IsSuccess);
        project.Progress = 40;

        WorldSave save = WorldPersistence.Capture(world);
        WorldPersistence.Validate(save);
        CityWorld restored = CityWorld.FromSave(
            WorldPersistence.DeserializeFromJson(WorldPersistence.SerializeToJson(save)));
        ConstructionProject restoredProject = restored.GetProject(project.Id)!;

        Assert.Equal(FoundingSiteModule.Cache, restoredProject.ActiveFoundingModule);
        Assert.True(restoredProject.HasCompletedFoundingModule(FoundingSiteModule.Campfire));
        Assert.Equal(40, restoredProject.Progress);
        Assert.Equal(2, restoredProject.DepositedInputs.Count);
        Assert.Equal(project.Id, restored.ParcelPlacements[project.Id].EntityId);
    }

    [Fact]
    public void OfflineAdvance_CompletesCurrentModuleButDoesNotChooseNextOne()
    {
        CityWorld live = AuthorizeFoundingSite();
        CityWorld offline = AuthorizeFoundingSite();
        ConstructionProject liveProject = live.Projects.Values.Single();
        ConstructionProject offlineProject = offline.Projects.Values.Single();
        Assert.True(live.ConfirmCitizenArrivedAtAssignment(live.Hero!.Id, liveProject.Id));
        Assert.True(offline.ConfirmCitizenArrivedAtAssignment(offline.Hero!.Id, offlineProject.Id));

        for (int tick = 0; tick < 600; tick++) live.AdvanceWorldTick();
        OfflineProgression.ApplyAll(offline, 600);

        Assert.Equal(liveProject.Id, offlineProject.Id);
        Assert.Equal(liveProject.Progress, offlineProject.Progress);
        Assert.Equal(liveProject.ActiveFoundingModule, offlineProject.ActiveFoundingModule);
        Assert.Equal(liveProject.CompletedFoundingModules, offlineProject.CompletedFoundingModules);
        Assert.Equal(liveProject.AssignedCitizenIds, offlineProject.AssignedCitizenIds);
        Assert.Equal(live.Hero!.Commitment, offline.Hero!.Commitment);
        Assert.Equal(live.GroundResourceCapacity(), offline.GroundResourceCapacity());
        ParcelPlacement livePlacement = live.ParcelPlacements[liveProject.Id];
        ParcelPlacement offlinePlacement = offline.ParcelPlacements[offlineProject.Id];
        Assert.Equal(livePlacement.EntityId, offlinePlacement.EntityId);
        Assert.Equal(livePlacement.ParcelId, offlinePlacement.ParcelId);
        Assert.Equal(livePlacement.LotColumn, offlinePlacement.LotColumn);
        Assert.Equal(livePlacement.LotRow, offlinePlacement.LotRow);
        Assert.Equal(livePlacement.FootprintProfileId, offlinePlacement.FootprintProfileId);
        Assert.Equal(livePlacement.Orientation, offlinePlacement.Orientation);
        Assert.True(offlineProject.HasCompletedFoundingModule(FoundingSiteModule.Campfire));
        Assert.Null(offlineProject.ActiveFoundingModule);
        Assert.Equal(ConstructionStopCause.AwaitingModule, offlineProject.StopCause);
        Assert.DoesNotContain(FoundingSiteModule.Bedroll, offlineProject.CompletedFoundingModules);
        Assert.DoesNotContain(FoundingSiteModule.Cache, offlineProject.CompletedFoundingModules);
    }

    [Fact]
    public void ConstructionSnapshot_ExposesTheBranchWithoutChoosingForPlayer()
    {
        CityWorld world = AuthorizeFoundingSite();
        ConstructionProject project = world.Projects.Values.Single();
        CompleteActiveModule(world, project);

        ConstructionSnapshot snapshot = ConstructionSnapshot.From(world);

        Assert.Null(snapshot.Project!.ActiveFoundingModule);
        Assert.True(snapshot.FoundingOptionFor(FoundingSiteModule.Bedroll)!.PrerequisitesMet);
        Assert.True(snapshot.FoundingOptionFor(FoundingSiteModule.Cache)!.PrerequisitesMet);
        Assert.False(snapshot.FoundingOptionFor(FoundingSiteModule.Canopy)!.PrerequisitesMet);
    }

    [Fact]
    public void MigrateV21ToV22_PreservesLegacyShelterAndProjects()
    {
        WorldSave save = WorldPersistence.Capture(TestHelpers.NewConstructionWorld());
        save.Version = 21;
        int projectCount = save.Projects.Count;

        WorldSave migrated = WorldPersistence.MigrateV21ToV22(save);

        Assert.Equal(22, migrated.Version);
        Assert.Equal(projectCount, migrated.Projects.Count);
        Assert.All(migrated.Projects,
            project => Assert.Equal(ConstructionKind.BasicShelter.ToString(), project.Kind));
        WorldPersistence.Validate(WorldPersistence.MigrateToCurrent(migrated));
    }

    [Fact]
    public void Validate_RejectsFoundingModuleThatSkipsPrerequisites()
    {
        CityWorld world = AuthorizeFoundingSite();
        WorldSave save = WorldPersistence.Capture(world);
        ConstructionProjectSave project = Assert.Single(save.Projects);
        project.ActiveFoundingModule = FoundingSiteModule.Canopy.ToString();

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(
            () => WorldPersistence.Validate(save));

        Assert.Contains("prerequisites", error.Message);
    }

    [Fact]
    public void FullWrongCargo_CanBeDroppedAndCampfireRemainsReachable()
    {
        CityWorld world = NewFoundingWorld();
        int fiberBefore = TotalPatchReserve(world, ResourceType.PlantFiber);
        Assert.Equal(6, GatherType(world, ResourceType.PlantFiber, 6));
        Assert.Equal(FoundingSiteRules.CarriedCapacity, world.CarriedGroundResourceCount());
        Assert.Equal(0, GatherType(world, ResourceType.Branches, 1));
        Assert.Equal(6, ConstructionSnapshot.From(world).ReturnableFoundingCargoCount);

        Assert.Equal(6, world.ReturnFoundingCargo());
        Assert.Equal(0, world.CarriedGroundResourceCount());
        Assert.Equal(fiberBefore, TotalPatchReserve(world, ResourceType.PlantFiber));
        Assert.Equal(3, GatherType(world, ResourceType.Branches, 3));
        Assert.Equal(2, GatherType(world, ResourceType.SmallStone, 2));

        ConstructionAuthorizationResult result =
            world.TryAuthorizeConstruction(ConstructionKind.FoundingSite);
        Assert.True(result.IsSuccess, result.Outcome.ToString());
    }

    [Fact]
    public void FullWrongCargo_CanBeReturnedBetweenEveryFoundingModule()
    {
        CityWorld world = AuthorizeFoundingSite();
        ConstructionProject project = world.Projects.Values.Single();
        CompleteActiveModule(world, project);

        int foodBefore = TotalPatchReserve(world, ResourceType.WildFood);
        Assert.Equal(6, GatherType(world, ResourceType.WildFood, 6));
        Assert.Equal(6, ConstructionSnapshot.From(world).ReturnableFoundingCargoCount);
        Assert.Equal(6, world.ReturnFoundingCargo());
        Assert.Equal(foodBefore, TotalPatchReserve(world, ResourceType.WildFood));
        CompleteModule(world, project, FoundingSiteModule.Bedroll);
        CompleteModule(world, project, FoundingSiteModule.Cache);

        int stoneBefore = TotalPatchReserve(world, ResourceType.SmallStone);
        Assert.Equal(6, GatherType(world, ResourceType.SmallStone, 6));
        Assert.Equal(6, GatherType(world, ResourceType.WildFood, 6));
        Assert.Equal(FoundingSiteRules.CacheCapacity, world.CarriedGroundResourceCount());
        Assert.Equal(12, ConstructionSnapshot.From(world).ReturnableFoundingCargoCount);
        Assert.Equal(12, world.ReturnFoundingCargo());
        Assert.Equal(stoneBefore, TotalPatchReserve(world, ResourceType.SmallStone));
        Assert.Equal(foodBefore, TotalPatchReserve(world, ResourceType.WildFood));

        DepositCost(world, FoundingSiteModule.Canopy);
        Assert.True(world.TryAuthorizeFoundingSiteModule(
            project.Id, FoundingSiteModule.Canopy).IsSuccess);
    }

    /// <summary>
    /// A finished intermediate module must hand the founder back. Only the
    /// Canopy used to release contributors, so completing the Campfire left
    /// the lone founder committed to a worksite with no active work: available
    /// became false and the gather action answered HeroUnavailable, with the
    /// next module's materials still on the ground and no obvious way to reach
    /// them.
    /// </summary>
    [Theory]
    [InlineData(FoundingSiteModule.Bedroll)]
    [InlineData(FoundingSiteModule.Cache)]
    public void CompletedIntermediateModule_ReleasesFounderSoGatheringStaysPossible(
        FoundingSiteModule nextModule)
    {
        CityWorld world = AuthorizeFoundingSite();
        ConstructionProject project = world.Projects.Values.Single();
        Citizen founder = world.Hero!;
        Assert.Equal(CitizenCommitmentKind.Construction, founder.Commitment.Kind);

        CompleteActiveModule(world, project);

        Assert.Equal(ConstructionStopCause.AwaitingModule, project.StopCause);
        Assert.Equal(CitizenCommitmentKind.None, founder.Commitment.Kind);
        Assert.Null(founder.WorkOrder);
        Assert.True(founder.IsAvailable);
        Assert.Equal(CitizenLocation.AtHome, founder.CurrentLocation);
        Assert.Equal(0, project.AssignedCount);

        NaturalResourcePatch patch = world.NaturalResourcePatches.Values
            .First(candidate => candidate.ResourceType == ResourceType.Branches);
        Assert.Equal(
            NaturalResourceGatherOutcome.Available,
            world.NaturalResourceGatherAvailability(patch.Id, unitId: 0).Outcome);

        // The released founder is re-assigned by the next authorisation, so the
        // module chain still advances without the player touching assignments.
        DepositCost(world, nextModule);
        Assert.True(world.TryAuthorizeFoundingSiteModule(project.Id, nextModule).IsSuccess);
        Assert.Equal(CitizenCommitmentKind.Construction, founder.Commitment.Kind);
    }

    private static CityWorld NewFoundingWorld()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        world.SeedStartingOpportunities();
        return world;
    }

    private static CityWorld AuthorizeFoundingSite()
    {
        CityWorld world = NewFoundingWorld();
        DepositCost(world, FoundingSiteModule.Campfire);
        ConstructionAuthorizationResult result =
            world.TryAuthorizeConstruction(ConstructionKind.FoundingSite);
        Assert.True(result.IsSuccess, result.Outcome.ToString());
        return world;
    }

    private static void CompleteModule(
        CityWorld world,
        ConstructionProject project,
        FoundingSiteModule module)
    {
        DepositCost(world, module);
        ConstructionAuthorizationResult result =
            world.TryAuthorizeFoundingSiteModule(project.Id, module);
        Assert.True(result.IsSuccess, result.Outcome.ToString());
        CompleteActiveModule(world, project);
    }

    private static void CompleteActiveModule(CityWorld world, ConstructionProject project)
    {
        project.Progress = project.RequiredWork;
        world.AdvanceWorldTick();
    }

    private static void DepositCost(CityWorld world, FoundingSiteModule module)
    {
        foreach (RecipeInput input in FoundingSiteRules.InputsFor(module))
        {
            world.Resources.DepositToCityInventory(input.Resource, input.Amount);
        }
    }

    private static int GatherGroundUnits(CityWorld world, int requested)
    {
        int gathered = 0;
        foreach (NaturalResourcePatch patch in world.NaturalResourcePatches.Values)
        {
            if (patch.ResourceType is not (
                ResourceType.Branches
                or ResourceType.PlantFiber
                or ResourceType.SmallStone
                or ResourceType.WildFood))
            {
                continue;
            }
            for (int unitId = 0; unitId < patch.UnitReserves.Count && gathered < requested; unitId++)
            {
                gathered += world.GatherFromPatch(patch.Id, unitId, 1);
            }
            if (gathered == requested) break;
        }
        return gathered;
    }

    private static int GatherType(CityWorld world, ResourceType type, int requested)
    {
        int gathered = 0;
        foreach (NaturalResourcePatch patch in world.NaturalResourcePatches.Values)
        {
            if (patch.ResourceType != type) continue;
            while (gathered < requested)
            {
                int before = gathered;
                for (int unitId = 0; unitId < patch.UnitReserves.Count && gathered < requested; unitId++)
                {
                    gathered += world.GatherFromPatch(patch.Id, unitId, 1);
                }
                if (gathered == before) break;
            }
            if (gathered == requested) break;
        }
        return gathered;
    }

    private static int TotalPatchReserve(CityWorld world, ResourceType type) =>
        world.NaturalResourcePatches.Values
            .Where(patch => patch.ResourceType == type)
            .Sum(patch => patch.TotalReserve);
}
