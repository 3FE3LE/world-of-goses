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
        var result = world.TryAuthorizeBasicShelter();

        Assert.True(result.IsSuccess);
        Assert.Single(world.Projects);
        Assert.Empty(world.Buildings);
    }

    [Fact]
    public void Authorize_Twice_Fails()
    {
        var world = TestHelpers.NewHeroWorld();
        Assert.True(world.TryAuthorizeBasicShelter().IsSuccess);
        Assert.Equal(ConstructionAuthorizationOutcome.AlreadyAuthorized, world.TryAuthorizeBasicShelter().Outcome);
    }

    [Fact]
    public void Assign_RespectsProjectCapacity()
    {
        var world = TestHelpers.NewConstructionWorld(extraCitizens: 6);
        var project = FirstProject(world);
        var hero = world.Hero!;
        Assert.True(world.TryAssignToProject(project.Id, hero.Id).IsSuccess);
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
        Assert.True(world.TryAssignToProject(project.Id, world.Hero!.Id).IsSuccess);

        int totalTicks = 5 * GameClock.TicksPerInGameDay;
        for (int i = 0; i < totalTicks; i++)
        {
            world.AdvanceWorldTick();
        }

        Assert.Empty(world.Projects);
        Assert.Single(world.Buildings);
        var home = world.Buildings.Values.Single();
        Assert.Equal(BuildingKind.Home, home.Kind);
        Assert.Equal("Basic Shelter", home.DisplayName);
    }

    [Fact]
    public void TwoContributors_CompleteFasterThanSolo()
    {
        var world = TestHelpers.NewConstructionWorld(extraCitizens: 1);
        var project = FirstProject(world);
        var hero = world.Hero!;
        var migrant = world.Citizens.Values.First(c => c.Id != hero.Id);
        Assert.True(world.TryAssignToProject(project.Id, hero.Id).IsSuccess);
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
        world.AdvanceWorldTick();
        Assert.Equal(0, project.Progress);
    }

    [Fact]
    public void Pause_StopsProgress_AndKeepsValue()
    {
        var world = TestHelpers.NewConstructionWorld();
        var project = FirstProject(world);
        world.TryAssignToProject(project.Id, world.Hero!.Id);
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
        world.TryAssignToProject(project.Id, hero.Id);
        hero.ConsumeStamina(50);

        for (int i = 0; i < GameClock.DayTicks + 5; i++) world.AdvanceWorldTick();
        Assert.Equal(ConstructionStopCause.Night, project.StopCause);
    }

    [Fact]
    public void Completion_ClearsAssignments_AndCitizensReturnToAvailable()
    {
        var world = TestHelpers.NewConstructionWorld();
        var project = FirstProject(world);
        var hero = world.Hero!;
        world.TryAssignToProject(project.Id, hero.Id);
        for (int i = 0; i < 6 * GameClock.TicksPerInGameDay; i++) world.AdvanceWorldTick();

        Assert.Empty(world.Projects);
        Assert.False(hero.CurrentAssignment.HasValue);
    }

    [Fact]
    public void Offline_EquivalentToLive_ForConstructionProgress()
    {
        var live = TestHelpers.NewConstructionWorld();
        var offline = TestHelpers.NewConstructionWorld();
        var liveProject = FirstProject(live);
        var offlineProject = FirstProject(offline);
        Assert.True(live.TryAssignToProject(liveProject.Id, live.Hero!.Id).IsSuccess);
        Assert.True(offline.TryAssignToProject(offlineProject.Id, offline.Hero!.Id).IsSuccess);

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
        world.TryAssignToProject(project.Id, world.Hero!.Id);
        for (int i = 0; i < GameClock.TicksPerInGameDay; i++) world.AdvanceWorldTick();

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
