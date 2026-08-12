using System;
using System.Linq;
using WorldofGoses.Domain;
using WorldofGoses.Persistence;
using Xunit;

namespace WorldofGoses.Tests;

public class HeroIncorporationTests
{
    [Fact]
    public void TryIncorporateHero_RecognisesCitizenWithoutReplacingFounder()
    {
        CityWorld world = TestHelpers.NewProductionWorld();
        Citizen founder = world.Hero!;
        var citizenId = new CitizenId(4);

        HeroIncorporationResult result = world.TryIncorporateHero(citizenId);

        Assert.True(result.IsSuccess);
        Assert.True(world.GetCitizen(citizenId)!.IsHero);
        Assert.Equal(founder.Id, world.Hero!.Id);
    }

    [Fact]
    public void TryIncorporateHero_RejectsUnknownAndExistingHero()
    {
        CityWorld world = TestHelpers.NewHeroWorld();

        Assert.Equal(
            HeroIncorporationOutcome.CitizenNotFound,
            world.TryIncorporateHero(new CitizenId(999)).Outcome);
        Assert.Equal(
            HeroIncorporationOutcome.AlreadyHero,
            world.TryIncorporateHero(world.Hero!.Id).Outcome);
    }

    [Fact]
    public void StartExpedition_RequiresEveryMemberToBeAnIncorporatedHero()
    {
        CityWorld world = TestHelpers.NewProductionWorld();
        var citizenId = new CitizenId(4);
        world.Resources.DepositToCityInventory(ResourceType.Wood, 1);

        ExpeditionStartResult rejected = world.StartExpedition(
            ExpeditionRequest.Reconnaissance(citizenId));
        Assert.Equal(ExpeditionStartOutcome.MemberNotHero, rejected.Outcome);

        Assert.True(world.TryIncorporateHero(citizenId).IsSuccess);
        ExpeditionStartResult accepted = world.StartExpedition(
            ExpeditionRequest.Reconnaissance(citizenId));
        Assert.True(accepted.IsSuccess);
    }

    [Fact]
    public void IncorporatedHero_RoundTripsWithoutChangingPrincipalHero()
    {
        CityWorld world = TestHelpers.NewProductionWorld();
        Citizen founder = world.Hero!;
        var citizenId = new CitizenId(4);
        Assert.True(world.TryIncorporateHero(citizenId).IsSuccess);

        CityWorld restored = WorldPersistence.FromSave(WorldPersistence.Capture(world));

        Assert.True(restored.GetCitizen(citizenId)!.IsHero);
        Assert.Equal(founder.Id, restored.Hero!.Id);
    }

    [Fact]
    public void Validate_RejectsExpeditionMemberWithoutHeroRole()
    {
        CityWorld world = TestHelpers.NewProductionWorld();
        var citizenId = new CitizenId(4);
        world.Resources.DepositToCityInventory(ResourceType.Wood, 1);
        Assert.True(world.TryIncorporateHero(citizenId).IsSuccess);
        Assert.True(world.StartExpedition(
            ExpeditionRequest.Reconnaissance(citizenId)).IsSuccess);
        WorldSave save = WorldPersistence.Capture(world);
        save.Citizens.Single(c => c.Id == citizenId.Value).Roles.RemoveAll(
            role => role.Id == RoleId.Hero.Value);

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(
            () => WorldPersistence.Validate(save));

        Assert.Contains("not an incorporated hero", error.Message);
    }

    [Fact]
    public void MigrateV16ToV17_RecognisesPreviouslyDispatchedMembers()
    {
        CityWorld world = TestHelpers.NewProductionWorld();
        var citizenId = new CitizenId(4);
        world.Resources.DepositToCityInventory(ResourceType.Wood, 1);
        Assert.True(world.TryIncorporateHero(citizenId).IsSuccess);
        Assert.True(world.StartExpedition(
            ExpeditionRequest.Reconnaissance(citizenId)).IsSuccess);
        WorldSave legacy = WorldPersistence.Capture(world);
        legacy.Version = 16;
        legacy.Citizens.Single(c => c.Id == citizenId.Value).Roles.RemoveAll(
            role => role.Id == RoleId.Hero.Value);

        WorldSave migrated = WorldPersistence.MigrateV16ToV17(legacy);

        Assert.Equal(17, migrated.Version);
        Assert.Contains(
            migrated.Citizens.Single(c => c.Id == citizenId.Value).Roles,
            role => role.Id == RoleId.Hero.Value);
        // The step under test is asserted above; the rest of the way to today
        // is the chain's own job, so this test survives future schema bumps.
        WorldSave current = WorldPersistence.MigrateToCurrent(migrated);
        Assert.Equal(WorldSave.CurrentVersion, current.Version);
        WorldPersistence.Validate(current);
    }
}
