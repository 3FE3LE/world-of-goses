using System;
using System.Collections.Generic;

namespace WorldofGoses.Domain;

public static class ToolRules
{
    private static readonly RecipeInput[] PrimitiveAxeRecipe =
    {
        new(ResourceType.Branches, 1),
        new(ResourceType.SmallStone, 1),
    };

    public static IReadOnlyList<RecipeInput> InputsFor(ToolKind tool) => tool switch
    {
        ToolKind.PrimitiveAxe => PrimitiveAxeRecipe,
        _ => throw new ArgumentOutOfRangeException(nameof(tool)),
    };
}
