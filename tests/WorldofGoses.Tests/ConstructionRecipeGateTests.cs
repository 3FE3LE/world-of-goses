using WorldofGoses.Domain;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// The construction-authorisation recipe gate: the city must hold
/// enough of every input to cover the up-front deposit, or the
/// authorisation fails atomically with
/// <see cref="ConstructionAuthorizationOutcome.MissingMaterials"/>.
/// Cancellation preserves resource conservation: already consumed inputs
/// remain spent and inputs not yet consumed are never credited.
/// </summary>
public class ConstructionRecipeGateTests
{
    [Fact]
    public void TryAuthorize_WithoutDeposit_ReturnsMissingMaterials()
    {
        var world = TestHelpers.WorldWithHome();

        // No gathered Wood or Food. Quarry's deposit fails.
        var result = world.TryAuthorizeConstruction(ConstructionKind.Quarry);

        Assert.False(result.IsSuccess);
        Assert.Equal(ConstructionAuthorizationOutcome.MissingMaterials, result.Outcome);
        Assert.Empty(world.Projects);
    }

    [Fact]
    public void TryAuthorize_WithDeposit_ConsumesWoodAndFoodAndSeedsRemainder()
    {
        var world = TestHelpers.WorldWithHome();
        world.GatherWood(new BuildingId(100), 2);
        world.DepositResource(ResourceType.Food, 1);

        var result = world.TryAuthorizeConstruction(ConstructionKind.Quarry);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.ProjectId);

        var project = world.GetProject(result.ProjectId!.Value)!;
        // Deposit = ceil(8 * 0.25) = 2 Wood and ceil(4 * 0.25) = 1 Food.
        // Remainder = 6 Wood and 3 Food.
        var byResource = new System.Collections.Generic.Dictionary<ResourceType, int>();
        foreach (var input in project.RemainingInputs)
        {
            byResource[input.Resource] = input.Amount;
        }
        Assert.Equal(6, byResource[ResourceType.Wood]);
        Assert.Equal(3, byResource[ResourceType.Food]);
    }

    [Fact]
    public void TryAuthorize_PartialStockFailure_RollsBackDebits()
    {
        var world = TestHelpers.WorldWithHome();
        // Wood enough for the deposit (2) but no Food. The deposit
        // fails atomically: Wood is NOT consumed.
        world.GatherWood(new BuildingId(100), 2);

        var woodBefore = world.TotalStockOf(ResourceType.Wood);
        var result = world.TryAuthorizeConstruction(ConstructionKind.Quarry);

        Assert.False(result.IsSuccess);
        Assert.Equal(woodBefore, world.TotalStockOf(ResourceType.Wood));
        Assert.Empty(world.Projects);
    }

    [Fact]
    public void CancelProject_DoesNotCreateUnconsumedInputs()
    {
        var world = TestHelpers.WorldWithHome();
        world.GatherWood(new BuildingId(100), 2);
        world.DepositResource(ResourceType.Food, 1);

        var woodBefore = world.TotalStockOf(ResourceType.Wood);
        var foodBefore = world.TotalStockOf(ResourceType.Food);

        var auth = world.TryAuthorizeConstruction(ConstructionKind.Quarry);
        Assert.True(auth.IsSuccess);

        // Deposit took 2 Wood and 1 Food; remainder is 6 Wood and 3 Food.
        Assert.Equal(woodBefore - 2, world.TotalStockOf(ResourceType.Wood));
        Assert.Equal(foodBefore - 1, world.TotalStockOf(ResourceType.Food));

        var cancelResult = world.CancelProject(auth.ProjectId!.Value);
        Assert.True(cancelResult);

        // Only the deposit was consumed. The remainder was never debited,
        // so cancelling must not credit it back into the city.
        Assert.Equal(woodBefore - 2, world.TotalStockOf(ResourceType.Wood));
        Assert.Equal(foodBefore - 1, world.TotalStockOf(ResourceType.Food));
        Assert.Empty(world.Projects);
    }
}
