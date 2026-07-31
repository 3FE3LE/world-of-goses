using WorldofGoses.Domain;
using WorldofGoses.Domain.Persistence;
using Xunit;

namespace WorldofGoses.Tests;

public class OnboardingDomainTests
{
    [Fact]
    public void FreshWorld_IsEmptyAndRequiresOnboarding()
    {
        var world = new CityWorld();

        Assert.True(world.NeedsOnboarding);
        Assert.Null(world.Hero);
        Assert.Empty(world.Citizens);
        Assert.Empty(world.Buildings);
    }

    [Fact]
    public void CreateHero_LeavesExactlyOneHeroAndNoBuildings()
    {
        var world = new CityWorld();
        var result = world.TryCreateHero(new HeroCreationRequest("Founder", TestHelpers.NewProfile(LineageId.Vaelun), GenderId.Masculine));

        Assert.True(result.IsSuccess);
        Assert.False(world.NeedsOnboarding);
        Assert.Single(world.Citizens);
        Assert.Empty(world.Buildings);
        Assert.Equal("Founder", world.Hero!.Name);
        Assert.True(world.Hero.IsHero);
        Assert.Equal(CitizenLocation.AtHome, world.Hero.CurrentLocation);
    }

    [Fact]
    public void CreateHero_RejectsInvalidNameAndDoesNotMutateWorld()
    {
        var world = new CityWorld();
        var result = world.TryCreateHero(new HeroCreationRequest("\n", TestHelpers.NewProfile(), GenderId.Feminine));

        Assert.False(result.IsSuccess);
        Assert.Equal(HeroCreationOutcome.InvalidName, result.Outcome);
        Assert.Empty(world.Citizens);
        Assert.Empty(world.Buildings);
    }

    [Fact]
    public void V2Save_AllowsActiveHeroWithNoBuildings()
    {
        var save = WorldPersistence.Capture(TestHelpers.NewHeroWorld());

        WorldPersistence.Validate(save);

        Assert.Empty(save.Buildings);
        Assert.Single(save.Citizens);
    }

    [Fact]
    public void Profile_RoundtripsThroughSave()
    {
        var world = TestHelpers.NewHeroWorld();
        var save = WorldPersistence.Capture(world);
        var restored = CityWorld.FromSave(
            WorldPersistence.DeserializeFromJson(WorldPersistence.SerializeToJson(save)));

        Assert.Equal(world.Hero!.Profile.Lineage, restored.Hero!.Profile.Lineage);
        Assert.Equal(world.Hero.Profile.Aptitudes, restored.Hero.Profile.Aptitudes);
        Assert.Equal(world.Hero.Profile.ProfessionalAffinities, restored.Hero.Profile.ProfessionalAffinities);
        Assert.Equal(world.Hero.Profile.ElementalAffinity, restored.Hero.Profile.ElementalAffinity);
        Assert.Equal(world.Hero.Profile.CombatStyle, restored.Hero.Profile.CombatStyle);
        Assert.Equal(world.Hero.Profile.WeaponPreferences, restored.Hero.Profile.WeaponPreferences);
        Assert.Equal(world.Hero.Profile.PersonalityTraits, restored.Hero.Profile.PersonalityTraits);
        Assert.Equal(world.Hero.Profile.PoliticalOrientation, restored.Hero.Profile.PoliticalOrientation);
        Assert.Equal(world.Hero.Profile.SpiritualPosture, restored.Hero.Profile.SpiritualPosture);
    }

    [Fact]
    public void OfflineProgression_EmptyWorldFastForwardsClockWithoutProduction()
    {
        var world = TestHelpers.NewHeroWorld();

        int tickBefore = world.CurrentTick;
        var report = OfflineProgression.ApplyAll(world, 1000);

        Assert.True(report.HadProgression);
        Assert.Equal(1000, report.TicksApplied);
        // WorldWithHome lands at the configured workday tick (1200)
        // since the 2026-07-30 shift, so absolute tick post-advance
        // is relative.
        Assert.Equal(tickBefore + 1000, world.CurrentTick);
        Assert.Equal(CitizenLocation.AtHome, world.Hero!.CurrentLocation);
    }
}
