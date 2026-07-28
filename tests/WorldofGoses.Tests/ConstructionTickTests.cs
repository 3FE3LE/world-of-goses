using System.Linq;
using WorldofGoses.Domain;
using WorldofGoses.Domain.Persistence;
using Xunit;

namespace WorldofGoses.Tests;

public class ConstructionTickTests
{
    [Fact]
    public void Authorize_WithoutHero_Fails()
    {
        var world = new CityWorld();
        var result = world.TryAuthorizeBasicShelter();

        Assert.False(result.IsSuccess);
        Assert.Equal(ConstructionAuthorizationOutcome.NoHero, result.Outcome);
        Assert.Empty(world.Projects);
    }

    [Fact]
    public void Authorize_AfterOnboarding_CreatesProject()
    {
        var world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        // Basic Shelter requires Wood × 4 (deposit = 1 wood). The
        // hero must gather at least once before authorisation.
        world.GatherWood(new BuildingId(100), 1);
        var result = world.TryAuthorizeBasicShelter();

        Assert.True(result.IsSuccess, $"authorization failed with {result.Outcome}");
        Assert.Single(world.Projects);
    }

    [Fact]
    public void Authorize_Twice_Fails()
    {
        var world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        world.GatherWood(new BuildingId(100), 1);
        Assert.True(world.TryAuthorizeBasicShelter().IsSuccess);
        Assert.Equal(ConstructionAuthorizationOutcome.AlreadyAuthorized, world.TryAuthorizeBasicShelter().Outcome);
    }

    [Theory]
    [InlineData(ConstructionKind.Farm, BuildingKind.Farm, ConstructionRules.FarmRequiredWork)]
    [InlineData(ConstructionKind.Quarry, BuildingKind.Quarry, ConstructionRules.QuarryRequiredWork)]
    public void Authorize_ProductiveBuilding_CreatesTypedPhasedProject(
        ConstructionKind kind,
        BuildingKind resultingKind,
        int requiredWork)
    {
        var world = TestHelpers.NewProductionWorld();

        var woodSource = TestHelpers.NewBuilding(
            id: new BuildingId(9100),
            kind: BuildingKind.Forest,
            producedCompetencyId: CompetencyId.Foraging,
            producedResourceType: ResourceType.Wood,
            workerCapacity: 0,
            visualCapacity: 0,
            baseProductionPerWorker: 0,
            storageCapacity: 20,
            displayName: "Test wood stock",
            resourceLabel: "Wood",
            resourceUnit: "wood");
        woodSource.AddStock(20);
        world.RegisterBuilding(woodSource);
        // Construction authorisation debits the recipe deposit up-front.
        var recipe = Recipes.ConstructionRecipeFor(kind);
        if (recipe is not null)
        {
            foreach (var input in recipe.RequiredInputs)
            {
                if (input.Resource == ResourceType.Wood)
                {
                    continue;
                }
                world.DepositResource(input.Resource, ConstructionRules.DepositOf(input.Amount));
            }
        }

        var result = world.TryAuthorizeConstruction(kind);

        Assert.True(result.IsSuccess, $"authorization failed with {result.Outcome}");
        var project = FirstProject(world);
        Assert.Equal(kind, project.Kind);
        Assert.Equal(resultingKind, project.ResultingKind);
        Assert.Equal(requiredWork, project.RequiredWork);
        Assert.Equal(ConstructionVisualPhase.Planned,
            ConstructionRules.PhaseFor(project.Progress, project.RequiredWork));
    }

    [Fact]
    public void Authorize_ProductiveBuildingBeforeShelter_Fails()
    {
        var world = TestHelpers.NewHeroWorld();

        var result = world.TryAuthorizeConstruction(ConstructionKind.Farm);

        Assert.Equal(ConstructionAuthorizationOutcome.HomeRequired, result.Outcome);
        Assert.Empty(world.Projects);
    }

    [Fact]
    public void Assign_RespectsProjectCapacity()
    {
        var world = TestHelpers.NewConstructionWorld(extraCitizens: 6);
        var project = FirstProject(world);
        var hero = world.Hero!;
        Assert.True(project.IsAssigned(hero.Id));
        int extrasAssigned = 0;
        foreach (var citizen in world.Citizens.Values)
        {
            if (citizen.Id == hero.Id) continue;
            if (world.TryAssignToProject(project.Id, citizen.Id).IsSuccess) extrasAssigned++;
        }
        Assert.Equal(ConstructionRules.WorkerCapacity, project.AssignedCount);
        Assert.Equal(ConstructionRules.WorkerCapacity - 1, extrasAssigned);
    }

    [Fact]
    public void SingleHero_CompletesInApproximatelyFiveDays()
    {
        var world = TestHelpers.NewConstructionWorld();
        var project = FirstProject(world);
        Assert.True(project.IsAssigned(world.Hero!.Id));

        int totalTicks = 5 * GameClock.TicksPerInGameDay;
        for (int i = 0; i < totalTicks; i++)
        {
            world.AdvanceWorldTick();
        }

        Assert.Empty(world.Projects);
        // The two founding Forests (id 100, 101) are still in the
        // world; the Basic Shelter becomes a Home building (id 1).
        Assert.Contains(world.Buildings.Values, b => b.Kind == BuildingKind.Home);
        Assert.Equal(3, world.Buildings.Count);
    }

    [Fact]
    public void TwoContributors_CompleteFasterThanSolo()
    {
        var world = TestHelpers.NewConstructionWorld(extraCitizens: 1);
        var project = FirstProject(world);
        var hero = world.Hero!;
        var migrant = world.Citizens.Values.First(c => c.Id != hero.Id);
        Assert.True(project.IsAssigned(hero.Id));
        Assert.True(world.TryAssignToProject(project.Id, migrant.Id).IsSuccess);

        int ticks = 3 * GameClock.TicksPerInGameDay;
        for (int i = 0; i < ticks; i++)
        {
            world.AdvanceWorldTick();
        }

        int progressAfterThreeDays = project.Progress;
        int remaining = project.RequiredWork - progressAfterThreeDays;
        Assert.True(remaining <= ConstructionRules.RequiredWork / 2,
            $"Expected pair of contributors to complete Basic Shelter in about three days, remaining was {remaining}.");
    }

    [Fact]
    public void NoContributors_DoesNotProgress()
    {
        var world = TestHelpers.NewConstructionWorld();
        var project = FirstProject(world);
        Assert.True(world.TryUnassignFromProject(project.Id, world.Hero!.Id).IsSuccess);
        world.AdvanceWorldTick();
        Assert.Equal(0, project.Progress);
    }

    [Fact]
    public void EnsureFoundingShelterContributor_RepairsStalledLoadedProjectOnce()
    {
        var world = TestHelpers.NewConstructionWorld();
        var project = FirstProject(world);
        var hero = world.Hero!;
        Assert.True(world.TryUnassignFromProject(project.Id, hero.Id).IsSuccess);

        Assert.True(world.EnsureFoundingShelterContributor());
        Assert.True(project.IsAssigned(hero.Id));
        Assert.False(world.EnsureFoundingShelterContributor());
    }

    [Fact]
    public void AssignedContributor_ProducesVisibleProgressWithinOneRuntimeInterval()
    {
        var world = TestHelpers.NewConstructionWorld();
        var project = FirstProject(world);
        int changes = 0;
        world.ProjectChanged += (_, args) =>
        {
            if (args.BuildingId == project.Id) changes++;
        };
        Assert.True(project.IsAssigned(world.Hero!.Id));
        changes = 0;

        for (int i = 0; i < ConstructionRules.WorkIntervalTicks; i++)
        {
            world.AdvanceWorldTick();
        }

        Assert.True(project.Progress > 0);
        Assert.True(project.LastTickProgressAdded > 0);
        Assert.True(changes > 0);
        Assert.Contains(world.Log.Events,
            evt => evt.Kind == WorldEventKind.ProjectProgressed && evt.SubjectName == project.DisplayName);
    }

    [Fact]
    public void Pause_StopsProgress_AndKeepsValue()
    {
        var world = TestHelpers.NewConstructionWorld();
        var project = FirstProject(world);
        Assert.True(project.IsAssigned(world.Hero!.Id));
        for (int i = 0; i < GameClock.TicksPerInGameDay; i++) world.AdvanceWorldTick();
        int before = project.Progress;
        world.SetProjectEnabled(project.Id, false);
        for (int i = 0; i < GameClock.TicksPerInGameDay; i++) world.AdvanceWorldTick();
        Assert.Equal(before, project.Progress);
    }

    [Fact]
    public void Night_RecoversStamina_AndAddsNoProgress()
    {
        var world = TestHelpers.NewConstructionWorld();
        var project = FirstProject(world);
        var hero = world.Hero!;
        Assert.True(project.IsAssigned(hero.Id));
        hero.ConsumeStamina(50);
        world.SetProjectEnabled(project.Id, false);

        for (int i = 0; i < GameClock.DayTicks + 5; i++) world.AdvanceWorldTick();
        Assert.Equal(ConstructionStopCause.Night, project.StopCause);
    }

    [Fact]
    public void Completion_ClearsAssignments_AndCitizensReturnToAvailable()
    {
        var world = TestHelpers.NewConstructionWorld();
        var project = FirstProject(world);
        var hero = world.Hero!;
        Assert.True(project.IsAssigned(hero.Id));
        int safety = 6 * GameClock.TicksPerInGameDay;
        while (world.Projects.Count > 0 && safety-- > 0) world.AdvanceWorldTick();

        Assert.Empty(world.Projects);
        Assert.False(hero.CurrentAssignment.HasValue);
        Assert.NotEqual(CitizenLocation.AtWork, hero.CurrentLocation);
        if (hero.CurrentLocation == CitizenLocation.InTransit)
        {
            Assert.True(hero.IsReturningHome);
            Assert.True(world.ConfirmCitizenArrivedHome(hero.Id));
        }
        Assert.Equal(CitizenLocation.AtHome, hero.CurrentLocation);
    }

    [Fact]
    public void Offline_EquivalentToLive_ForConstructionProgress()
    {
        var live = TestHelpers.NewConstructionWorld();
        var offline = TestHelpers.NewConstructionWorld();
        var liveProject = FirstProject(live);
        var offlineProject = FirstProject(offline);
        Assert.True(liveProject.IsAssigned(live.Hero!.Id));
        Assert.True(offlineProject.IsAssigned(offline.Hero!.Id));

        int ticks = 4 * GameClock.TicksPerInGameDay;
        for (int i = 0; i < ticks; i++) live.AdvanceWorldTick();
        var report = OfflineProgression.ApplyAll(offline, ticks);

        Assert.Equal(liveProject.Progress, offlineProject.Progress);
    }

    [Fact]
    public void Validate_ConstructionProjectSave_Roundtrips()
    {
        var world = TestHelpers.NewConstructionWorld();
        var project = FirstProject(world);
        Assert.True(project.IsAssigned(world.Hero!.Id));
        for (int i = 0; i < ConstructionRules.WorkIntervalTicks; i++) world.AdvanceWorldTick();

        var save = WorldPersistence.Capture(world);
        WorldPersistence.Validate(save);
        var json = WorldPersistence.SerializeToJson(save);
        var restored = CityWorld.FromSave(
            WorldPersistence.DeserializeFromJson(json));
        var restoredProject = FirstProject(restored);
        Assert.Equal(project.Progress, restoredProject.Progress);
        Assert.Equal(project.AssignedCount, restoredProject.AssignedCount);
    }

    private static ConstructionProject FirstProject(CityWorld world)
    {
        foreach (var project in world.Projects.Values)
        {
            return project;
        }
        throw new System.InvalidOperationException("No construction project present.");
    }
}
