using WorldofGoses.Domain;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// Verifies the <see cref="Recipes"/> registry has the seed
/// recipes the slice promises: wood bootstraps Shelter and Farm,
/// Farm food unlocks Quarry, and operating material recipes remain
/// absent until a sustainable tools/fuel chain exists.
/// </summary>
public class RecipesTests
{
    [Fact]
    public void ConstructionRecipe_BasicShelter_RequiresWood()
    {
        var recipe = Recipes.ConstructionRecipeFor(ConstructionKind.BasicShelter);

        Assert.NotNull(recipe);
        var byResource = new System.Collections.Generic.Dictionary<ResourceType, int>();
        foreach (var input in recipe!.RequiredInputs)
        {
            byResource[input.Resource] = input.Amount;
        }
        Assert.Equal(4, byResource[ResourceType.Wood]);
    }

    [Fact]
    public void ConstructionRecipe_Farm_RequiresWood()
    {
        var recipe = Recipes.ConstructionRecipeFor(ConstructionKind.Farm);

        Assert.NotNull(recipe);
        var input = Assert.Single(recipe!.RequiredInputs);
        Assert.Equal(ResourceType.Wood, input.Resource);
        Assert.Equal(6, input.Amount);
        Assert.Equal(ResourceType.Food, recipe.Output);
    }

    [Fact]
    public void ConstructionRecipe_Quarry_RequiresWoodAndFood()
    {
        var recipe = Recipes.ConstructionRecipeFor(ConstructionKind.Quarry);

        Assert.NotNull(recipe);
        var byResource = new System.Collections.Generic.Dictionary<ResourceType, int>();
        foreach (var input in recipe!.RequiredInputs)
        {
            byResource[input.Resource] = input.Amount;
        }
        Assert.Equal(8, byResource[ResourceType.Wood]);
        Assert.Equal(4, byResource[ResourceType.Food]);
        Assert.Equal(ResourceType.Stone, recipe.Output);
    }

    [Fact]
    public void OperatingRecipe_Home_IsNull()
    {
        // Home never consumes inputs while idle. A null recipe is
        // the canonical signal for "no operating cost".
        Assert.Null(Recipes.OperatingRecipeFor(BuildingKind.Home));
    }

    [Fact]
    public void OperatingRecipe_Quarry_IsNullUntilToolsOrFuelExist()
    {
        Assert.Null(Recipes.OperatingRecipeFor(BuildingKind.Quarry));
    }

    [Fact]
    public void OperatingRecipe_Farm_IsNullUntilToolsOrFuelExist()
    {
        Assert.Null(Recipes.OperatingRecipeFor(BuildingKind.Farm));
    }
}
