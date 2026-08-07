using System.Linq;
using WorldofGoses.Domain;
using WorldofGoses.Domain.Persistence;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// The authored first night (`docs/world-of-goses-design-bible/23_FIRST_NIGHT_AND_FIRE_SPIRIT.md`). It runs
/// from the manifestation to dawn, teaches the founder why the ground materials
/// matter, and must never be losable: no stage expires, nothing depends on the
/// player reading quickly, and no required resource needs a tool.
/// </summary>
public sealed class FirstNightTests
{
    [Fact]
    public void ManifestingTheFounder_OpensTheNightAtMidnightWithNoSpiritYet()
    {
        var world = new CityWorld();
        Assert.Null(world.FirstNight);

        Assert.True(world.TryCreateHero(
            new HeroCreationRequest("Aster", TestHelpers.NewProfile(), GenderId.Masculine))
            .IsSuccess);

        FirstNightState night = Assert.IsType<FirstNightState>(world.FirstNight);
        Assert.Equal(FirstNightStage.Manifested, night.Stage);
        Assert.True(world.IsFirstNightActive);
        Assert.False(night.SpiritIsPresent);
        Assert.Null(night.CurrentDialogueNodeId);
        // Tick 0 is Day 1 00:00 and is already night, so the sequence needs no
        // clock manipulation and no second awakening scene to begin.
        Assert.Equal(0, night.StartedAtTick);
        Assert.False(GameClock.IsDaytime(world.CurrentTick));
    }

    [Fact]
    public void TheNightNeverAdvancesOnTimeAlone()
    {
        CityWorld world = NightWorld();

        for (int tick = 0; tick < FirstNightRules.DawnTick * 2; tick++)
        {
            world.AdvanceWorldTick();
        }

        // Well past both narrative dawn (900) and the first workday hour (1200).
        Assert.True(world.CurrentTick > GameClock.WorkdayStartTick);
        Assert.Equal(FirstNightStage.Manifested, world.FirstNight!.Stage);
        Assert.True(world.IsFirstNightActive);
    }

    [Fact]
    public void ASlowPlayerIsNotChargedARationBehindTheNarration()
    {
        CityWorld world = NightWorld();
        world.SeedStartingForests();

        for (int tick = 0; tick < GameClock.TicksPerInGameDay; tick++)
        {
            world.AdvanceWorldTick();
        }

        Assert.DoesNotContain(
            world.Log.Events,
            evt => evt.Kind == WorldEventKind.FoodRationShortfall);
        Assert.DoesNotContain(world.Log.Events, evt => evt.Kind == WorldEventKind.DayBegan);
        // And the held calendar is released the moment the night concludes.
        world.ConcludeFirstNightForFixtures();
        TestHelpers.AdvanceToNextDawn(world);
        Assert.Contains(
            world.Log.Events,
            evt => evt.Kind == WorldEventKind.FoodRationShortfall);
    }

    [Fact]
    public void TheDisplayedHourParksAtFiveFiftyNineInsteadOfRollingIntoDaylight()
    {
        CityWorld world = NightWorld();
        FirstNightState night = world.FirstNight!;

        Assert.Equal(120, night.DisplayedTick(120));
        Assert.Equal(
            FirstNightRules.LatestDisplayedNightTick,
            night.DisplayedTick(FirstNightRules.DawnTick));
        Assert.Equal(
            FirstNightRules.LatestDisplayedNightTick,
            night.DisplayedTick(GameClock.TicksPerInGameDay));

        world.ConcludeFirstNightForFixtures();
        Assert.Equal(GameClock.TicksPerInGameDay, night.DisplayedTick(GameClock.TicksPerInGameDay));
    }

    [Fact]
    public void TheSpiritArrivesAfterTheManifestationAndLeavesBeforeTheFounderWakes()
    {
        CityWorld world = NightWorld();
        FirstNightState night = world.FirstNight!;

        Assert.False(night.SpiritIsPresent);
        Assert.True(world.TryCloseFirstNightDialogue());
        Assert.Equal(FirstNightStage.SpiritArrived, night.Stage);
        Assert.True(night.SpiritIsPresent);

        DriveNightToSleep(world);

        Assert.Equal(FirstNightStage.Sleeping, night.Stage);
        Assert.False(night.SpiritIsPresent);
    }

    [Fact]
    public void TheNightsShelterCostsNoToolAndFitsTheFoundersCarryLimit()
    {
        foreach (FirstNightStage stage in new[]
        {
            FirstNightStage.ColdExplained,
            FirstNightStage.ShelterExplained,
        })
        {
            FoundingSiteModule module = FirstNightRules.ModuleFor(stage);
            var inputs = FoundingSiteRules.InputsFor(module);

            // Tool-free: none of these is Wood, the only tool-gated resource.
            Assert.DoesNotContain(inputs, input => input.Resource == ResourceType.Wood);
            // One carry trip per module, so no Cache is needed to reach either.
            Assert.True(
                inputs.Sum(input => input.Amount) <= FoundingSiteRules.CarriedCapacity,
                $"{module} needs more than one carry trip.");
        }

        // Cache and Canopy are post-dawn consolidation, not the night's shelter.
        Assert.False(FirstNightRules.WaitsForModule(FirstNightStage.ShelterBuilt));
    }

    [Fact]
    public void BuildingTheCampfireAndBedrollCarriesTheNightForward()
    {
        CityWorld world = NightWorld();
        world.SeedStartingOpportunities();
        FirstNightState night = world.FirstNight!;

        Assert.True(world.TryCloseFirstNightDialogue());   // SpiritArrived
        Assert.True(world.TryCloseFirstNightDialogue());   // ColdExplained

        // A module the night is not waiting on must not move it.
        Assert.Equal(FirstNightStage.ColdExplained, night.Stage);

        ConstructionProject project = AuthorizeCampfire(world);
        CompleteActiveModule(world, project);
        Assert.Equal(FirstNightStage.CampfireBuilt, night.Stage);

        Assert.True(world.TryCloseFirstNightDialogue());   // ShelterExplained
        CompleteModule(world, project, FoundingSiteModule.Bedroll);
        Assert.Equal(FirstNightStage.ShelterBuilt, night.Stage);
    }

    [Fact]
    public void SleepRequiresSomewhereToSleep()
    {
        CityWorld world = NightWorld();
        world.SeedStartingOpportunities();
        FirstNightState night = world.FirstNight!;
        Assert.False(world.HasRestingPlace());

        Assert.True(world.TryCloseFirstNightDialogue());
        Assert.True(world.TryCloseFirstNightDialogue());
        ConstructionProject project = AuthorizeCampfire(world);
        CompleteActiveModule(world, project);
        Assert.True(world.TryCloseFirstNightDialogue());

        // Reach the final conversation without ever building the Bedroll by
        // completing the Cache instead — the night must refuse to fall asleep.
        CompleteModule(world, project, FoundingSiteModule.Cache);
        Assert.Equal(FirstNightStage.ShelterExplained, night.Stage);
        Assert.False(world.HasRestingPlace());

        CompleteModule(world, project, FoundingSiteModule.Bedroll);
        Assert.True(world.HasRestingPlace());
        Assert.Equal(FirstNightStage.ShelterBuilt, night.Stage);
    }

    [Fact]
    public void SavingMidConversationResumesOnTheSameLine()
    {
        CityWorld world = NightWorld();
        Assert.True(world.TryCloseFirstNightDialogue());
        Assert.True(world.TryOpenFirstNightDialogue("spirit.cold.body"));

        CityWorld restored = CityWorld.FromSave(WorldPersistence.Capture(world));

        FirstNightState night = Assert.IsType<FirstNightState>(restored.FirstNight);
        Assert.Equal(FirstNightStage.SpiritArrived, night.Stage);
        Assert.Equal("spirit.cold.body", night.CurrentDialogueNodeId);
        Assert.True(restored.IsFirstNightActive);
        Assert.True(night.SpiritIsPresent);
    }

    [Fact]
    public void AConcludedNightRoundTripsAsInert()
    {
        CityWorld world = NightWorld();
        world.ConcludeFirstNightForFixtures();

        CityWorld restored = CityWorld.FromSave(WorldPersistence.Capture(world));

        Assert.Equal(FirstNightStage.Concluded, restored.FirstNight!.Stage);
        Assert.False(restored.IsFirstNightActive);
        Assert.False(restored.FirstNight.SpiritIsPresent);
        Assert.NotNull(restored.FirstNight.ConcludedAtTick);
    }

    [Fact]
    public void RestartingDuringTheNightLeavesACompletableCity()
    {
        CityWorld world = NightWorld();
        world.SeedStartingForests();
        world.SeedStartingOpportunities();

        CityWorld restarted = world.CreateRestartedCityKeepingHero();

        Assert.Equal(FirstNightStage.Manifested, restarted.FirstNight!.Stage);
        foreach (RecipeInput input in FoundingSiteRules.InputsFor(FoundingSiteModule.Campfire))
        {
            Assert.True(
                restarted.NaturalResourcePatches.Values
                    .Where(patch => patch.ResourceType == input.Resource)
                    .Sum(patch => patch.TotalReserve) >= input.Amount,
                $"A restarted city cannot reach {input.Amount} {input.Resource}.");
        }
    }

    /// <summary>
    /// Existing cities are past their opening. Dropping them into the sequence
    /// would ask for Campfire and Bedroll milestones they already passed — or on
    /// a consolidated city can never satisfy again — and would hold the calendar
    /// for a night with no way to end.
    /// </summary>
    [Fact]
    public void MigrateV30ToV31_MarksExistingCitiesAsAlreadyConcluded()
    {
        WorldSave save = WorldPersistence.Capture(TestHelpers.WorldWithHome());
        save.Version = 30;
        save.FirstNight = null;

        WorldSave migrated = WorldPersistence.MigrateV30ToV31(save);

        Assert.Equal(31, migrated.Version);
        Assert.Equal(
            FirstNightStage.Concluded.ToString(),
            migrated.FirstNight!.Stage);
        WorldPersistence.Validate(migrated);

        CityWorld restored = CityWorld.FromSave(migrated);
        Assert.False(restored.IsFirstNightActive);
    }

    /// <summary>
    /// A save with no founder gets no night invented for it: the sequence opens
    /// when the founder manifests. Such a save is not restorable anyway —
    /// validation demands a hero — so this asserts the migration only, which is
    /// the part a hand-built or partially written file can actually reach.
    /// </summary>
    [Fact]
    public void MigrateV30ToV31_LeavesAFounderlessSaveWithoutANight()
    {
        WorldSave save = WorldPersistence.Capture(new CityWorld());
        save.Version = 30;

        WorldSave migrated = WorldPersistence.MigrateV30ToV31(save);

        Assert.Equal(31, migrated.Version);
        Assert.Null(migrated.FirstNight);
    }

    [Fact]
    public void ValidationRejectsAnInconsistentPersistedNight()
    {
        WorldSave concludedWithoutTick = WorldPersistence.Capture(NightWorld());
        concludedWithoutTick.FirstNight!.Stage = FirstNightStage.Concluded.ToString();
        Assert.Throws<System.InvalidOperationException>(
            () => WorldPersistence.Validate(concludedWithoutTick));

        WorldSave unknownStage = WorldPersistence.Capture(NightWorld());
        unknownStage.FirstNight!.Stage = "Daydreaming";
        Assert.Throws<System.InvalidOperationException>(
            () => WorldPersistence.Validate(unknownStage));
    }

    [Fact]
    public void OfflineCatchUpDoesNotAdvanceTheNightEither()
    {
        CityWorld live = NightWorld();
        CityWorld offline = CityWorld.FromSave(WorldPersistence.Capture(live));

        for (int tick = 0; tick < FirstNightRules.DawnTick; tick++) live.AdvanceWorldTick();
        WorldTimeAdvance.Advance(offline, FirstNightRules.DawnTick);

        Assert.Equal(live.FirstNight!.Stage, offline.FirstNight!.Stage);
        Assert.Equal(FirstNightStage.Manifested, offline.FirstNight.Stage);
        Assert.Equal(live.CurrentTick, offline.CurrentTick);
    }

    /// <summary>A founder world still inside its authored first night.</summary>
    private static CityWorld NightWorld()
    {
        var world = new CityWorld();
        HeroCreationResult result = world.TryCreateHero(
            new HeroCreationRequest("Aster", TestHelpers.NewProfile(), GenderId.Masculine));
        Assert.True(result.IsSuccess, result.Outcome.ToString());
        return world;
    }

    private static void DriveNightToSleep(CityWorld world)
    {
        world.SeedStartingOpportunities();
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
