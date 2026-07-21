using System.Linq;
using WorldofGoses.Domain;
using Xunit;

namespace WorldofGoses.Tests;

public sealed class UiSnapshotTests
{
    [Fact]
    public void CityStatusSnapshot_ExposesExplicitHeroOnlyEmptyState()
    {
        var snapshot = CityStatusSnapshot.From(TestHelpers.NewHeroWorld());

        Assert.True(snapshot.IsEmpty);
        Assert.Equal("Aster", snapshot.HeroName);
        Assert.Empty(snapshot.Buildings);
        Assert.Empty(snapshot.Projects);
        Assert.Single(snapshot.FreeCitizenNames);
    }

    [Fact]
    public void ConstructionSnapshot_ExposesActionableProjectState()
    {
        var snapshot = ConstructionSnapshot.From(TestHelpers.NewConstructionWorld());

        Assert.True(snapshot.HasHero);
        Assert.NotNull(snapshot.Project);
        Assert.Equal(ConstructionStopCause.NoWorkers, snapshot.Project!.StopCause);
        Assert.Empty(snapshot.Project.AssignedCitizens);
        Assert.Contains(snapshot.Project.RemainingInputs,
            input => input.Resource == ResourceType.Wood && input.Amount == 3);
        Assert.Contains(snapshot.AvailableCitizens, citizen => citizen.Name == "Aster");
    }

    [Fact]
    public void ConstructionSnapshot_ShowsShelterRequirementsAndGatherAction()
    {
        var world = TestHelpers.NewHeroWorld();
        world.SeedStartingForests();

        var before = ConstructionSnapshot.From(world);
        var shelter = before.OptionFor(ConstructionKind.BasicShelter);

        var material = Assert.Single(shelter.Materials);
        Assert.Equal(ResourceType.Wood, material.Resource);
        Assert.Equal(4, material.Required);
        Assert.Equal(0, material.Available);
        Assert.Equal(1, material.DepositRequired);
        Assert.False(shelter.CanPayDeposit);
        // Forest gathering is now driven by worker assignment; verify
        // the forest reserve exposes the same end-state through that
        // path instead of a GatherWood snapshot field.
        var forest = world.Buildings.Values.First(b => b.Kind == BuildingKind.Forest);
        world.GatherWood(forest.Id, 2);
        var after = ConstructionSnapshot.From(world).OptionFor(ConstructionKind.BasicShelter);

        Assert.Equal(2, Assert.Single(after.Materials).Available);
        Assert.True(after.CanPayDeposit);
    }

    [Fact]
    public void ConstructionSnapshot_DisablesUnavailableFarmAndQuarryDeposits()
    {
        var snapshot = ConstructionSnapshot.From(TestHelpers.WorldWithHome());

        Assert.False(snapshot.OptionFor(ConstructionKind.Farm).CanPayDeposit);
        Assert.False(snapshot.OptionFor(ConstructionKind.Quarry).CanPayDeposit);
    }

    [Fact]
    public void BuildingDetailSnapshot_ContainsOnlyProjectedCitizenData()
    {
        var world = TestHelpers.NewProductionWorld();
        var snapshot = BuildingDetailSnapshot.From(world, new BuildingId(1));

        Assert.NotNull(snapshot);
        Assert.Equal(BuildingKind.Quarry, snapshot!.Kind);
        Assert.Equal(2, snapshot.AssignedCount);
        Assert.Equal(2, snapshot.VisibleCitizens.Count);
        Assert.All(snapshot.VisibleCitizens, citizen => Assert.False(string.IsNullOrWhiteSpace(citizen.Name)));
    }

    [Fact]
    public void Snapshots_DoNotChangeWhenWorldMutates()
    {
        var world = TestHelpers.NewHeroWorld();
        var before = CityStatusSnapshot.From(world);

        world.SeedStartingForests();

        Assert.True(before.IsEmpty);
        Assert.Empty(before.Buildings);
        Assert.Equal(2, CityStatusSnapshot.From(world).Buildings.Count);
    }
}
