using WorldofGoses.Domain;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// Farm and Quarry currently pay through labour, stamina and time.
/// Iron is reserved for a later sustainable tools/fuel chain and must
/// not silently gate either building today.
/// </summary>
public class BuildingOperatingRecipeTests
{
    [Fact]
    public void FarmAndQuarry_HaveNoMaterialOperatingRecipe()
    {
        Assert.Null(Recipes.OperatingRecipeFor(BuildingKind.Farm));
        Assert.Null(Recipes.OperatingRecipeFor(BuildingKind.Quarry));
    }

    [Fact]
    public void Quarry_ProducesWithoutIronReserve()
    {
        var world = TestHelpers.NewProductionWorld();
        var quarry = world.GetBuilding(new BuildingId(1))!;
        world.TryConsumeResource(ResourceType.Iron, world.TotalStockOf(ResourceType.Iron));
        int stoneBefore = quarry.Stock;

        TestHelpers.AdvanceToNextProductionCycle(world);

        Assert.True(quarry.LastTickProduction > 0);
        Assert.Equal(stoneBefore + quarry.LastTickProduction, quarry.Stock);
    }
}
