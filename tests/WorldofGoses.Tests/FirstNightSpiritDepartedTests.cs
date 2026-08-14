using System.Linq;
using WorldofGoses.Domain;
using WorldofGoses.Persistence;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// The dawn emits a <see cref="WorldEventKind.SpiritDeparted"/> event
/// exactly once when the night crosses from
/// <see cref="FirstNightStage.Sleeping"/> to
/// <see cref="FirstNightStage.Concluded"/>. The event unlocks the
/// <c>SpiritTrailSearch</c> expedition and is the moment the chronicle
/// marks the spirit's exit (<c>docs/systems/first-night.md</c>
/// §11–12). These tests guard the event's presence, significance,
/// and round-trip persistence.
/// </summary>
public sealed class FirstNightSpiritDepartedTests
{
    [Fact]
    public void ClosingTheNightFromSleepingEmitsExactlyOneSpiritDepartedEvent()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        FirstNightState night = world.FirstNight!;
        world.SeedStartingForests();
        world.SeedStartingOpportunities();

        // Drive the night past Sleeping. DriveNightToSleep stops at the
        // Sleeping stage; the next TryCloseFirstNightDialogue crosses to
        // Concluded and is the moment SpiritDeparted must fire.
        DriveNightToSleep(world);

        int spiritDepartedBefore = CountSpiritDeparted(world);
        Assert.Equal(0, spiritDepartedBefore);

        Assert.True(world.TryCloseFirstNightDialogue());
        Assert.Equal(FirstNightStage.Concluded, night.Stage);

        int spiritDepartedAfter = CountSpiritDeparted(world);
        Assert.Equal(1, spiritDepartedAfter);
    }

    [Fact]
    public void SpiritDepartedIsSignificant()
    {
        // Without this, WorldEventRetention.SelectForPersistence drops the
        // event and the expedition panel can never unlock SpiritTrailSearch.
        Assert.True(WorldEventRetention.IsSignificant(WorldEventKind.SpiritDeparted));
    }

    [Fact]
    public void SpiritDepartedSurvivesRoundTrip()
    {
        CityWorld world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();
        world.SeedStartingOpportunities();
        DriveNightToSleep(world);
        Assert.True(world.TryCloseFirstNightDialogue());

        WorldSave captured = WorldPersistence.Capture(world);
        CityWorld restored = WorldPersistence.FromSave(captured);

        WorldEvent? spiritDeparted = restored.Log.Events
            .FirstOrDefault(evt => evt.Kind == WorldEventKind.SpiritDeparted);
        Assert.NotNull(spiritDeparted);
        Assert.Equal("FireSpirit", spiritDeparted!.Subject.DisplayName);
    }

    [Fact]
    public void ASaveWithoutSpiritDepartedLoadsWithoutError()
    {
        // A v31 save that never ran a night must still load cleanly.
        // The new enum value is only produced by the dawn code path; no
        // existing save will carry it, and the validator must accept its
        // absence.
        CityWorld world = TestHelpers.WorldWithHome();
        WorldSave save = WorldPersistence.Capture(world);
        Assert.DoesNotContain(
            save.Events,
            evt => string.Equals(evt.Kind, WorldEventKind.SpiritDeparted.ToString(), System.StringComparison.Ordinal));
        CityWorld restored = WorldPersistence.FromSave(save);
        Assert.NotNull(restored);
    }

    [Fact]
    public void CampfireAndSpiritDepartedImpliesEmbers()
    {
        // The post-departure embers primitive draws only when the
        // campfire exists AND the dawn has carried the spirit away.
        // Driving the night to Sleeping and closing it leaves the world
        // in the "embers visible" state; a save that never ran the night
        // leaves it in "no embers" state.
        CityWorld withNight = TestHelpers.NewHeroWorld();
        withNight.SeedStartingForests();
        withNight.SeedStartingOpportunities();
        DriveNightToSleep(withNight);
        Assert.True(withNight.TryCloseFirstNightDialogue());
        Assert.True(withNight.HasFoundingSiteModule(FoundingSiteModule.Campfire));
        Assert.Contains(
            withNight.Log.Events,
            evt => evt.Kind == WorldEventKind.SpiritDeparted);

        CityWorld withoutNight = TestHelpers.WorldWithHome();
        Assert.DoesNotContain(withoutNight.Log.Events,
            evt => evt.Kind == WorldEventKind.SpiritDeparted);
    }

    private static int CountSpiritDeparted(CityWorld world) =>
        world.Log.Events.Count(evt => evt.Kind == WorldEventKind.SpiritDeparted);

    private static void DriveNightToSleep(CityWorld world)
    {
        FirstNightState night = world.FirstNight!;
        ConstructionProject? project = null;
        int safety = 32;
        while (night.Stage < FirstNightStage.Sleeping && safety-- > 0)
        {
            if (FirstNightRules.WaitsForModule(night.Stage))
            {
                FoundingSiteModule module = FirstNightRules.ModuleFor(night.Stage);
                if (project is null)
                {
                    project = AuthorizeCampfire(world);
                    CompleteActiveModule(world, project);
                }
                else
                {
                    CompleteModule(world, project, module);
                }
                continue;
            }
            Assert.True(world.TryCloseFirstNightDialogue(), $"Stalled at {night.Stage}.");
        }
    }

    private static ConstructionProject AuthorizeCampfire(CityWorld world)
    {
        DepositCost(world, FoundingSiteModule.Campfire);
        ConstructionAuthorizationResult result =
            world.TryAuthorizeConstruction(ConstructionKind.FoundingSite);
        Assert.True(result.IsSuccess, result.Outcome.ToString());
        return world.Projects.Values.Single();
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
}
