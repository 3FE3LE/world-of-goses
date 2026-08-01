using System.Collections.Generic;

namespace WorldofGoses.Domain;

/// <summary>Bounded EG-3 rules for the first cultivation plot.</summary>
public static class CultivationRules
{
    public const int PreparationWork = 180;
    public const int WorkerCapacity = 1;
    public const int GrowthTicks = GameClock.TicksPerInGameDay * 3;
    public const int SeedFoodCost = 1;
    public const int HarvestFoodYield = 5;
    public const int PlannedWoodExpeditionFoodSupply = 1;

    private static readonly IReadOnlyList<RecipeInput> PreparationInputs =
        new[]
        {
            new RecipeInput(ResourceType.Branches, 1),
            new RecipeInput(ResourceType.SmallStone, 1),
        };

    public static IReadOnlyList<RecipeInput> InputsForPreparation() =>
        PreparationInputs;
}
