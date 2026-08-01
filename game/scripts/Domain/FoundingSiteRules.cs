#nullable enable
using System;
using System.Collections.Generic;

namespace WorldofGoses.Domain;

/// <summary>Bounded EG-2 phase graph for the Founding Site.</summary>
public static class FoundingSiteRules
{
    /// <summary>
    /// Four equal phases preserve the already validated 720-work shelter
    /// budget without inventing a second overall duration.
    /// </summary>
    public const int WorkPerModule = ConstructionRules.RequiredWork / 4;

    public const int CarriedCapacity = 6;
    public const int CacheCapacity = 12;
    public const int ShelterCapacity = 24;

    public static IReadOnlyList<RecipeInput> InputsFor(FoundingSiteModule module) => module switch
    {
        FoundingSiteModule.Campfire => new[]
        {
            new RecipeInput(ResourceType.Branches, 3),
            new RecipeInput(ResourceType.SmallStone, 2),
        },
        FoundingSiteModule.Bedroll => new[]
        {
            new RecipeInput(ResourceType.Branches, 2),
            new RecipeInput(ResourceType.PlantFiber, 3),
        },
        FoundingSiteModule.Cache => new[]
        {
            new RecipeInput(ResourceType.Branches, 2),
            new RecipeInput(ResourceType.PlantFiber, 1),
        },
        FoundingSiteModule.Canopy => new[]
        {
            new RecipeInput(ResourceType.Branches, 4),
            new RecipeInput(ResourceType.PlantFiber, 2),
        },
        _ => Array.Empty<RecipeInput>(),
    };

    public static bool PrerequisitesMet(
        FoundingSiteModule module,
        Func<FoundingSiteModule, bool> isCompleted) => module switch
    {
        FoundingSiteModule.Campfire => true,
        FoundingSiteModule.Bedroll => isCompleted(FoundingSiteModule.Campfire),
        FoundingSiteModule.Cache => isCompleted(FoundingSiteModule.Campfire),
        FoundingSiteModule.Canopy => isCompleted(FoundingSiteModule.Campfire)
            && isCompleted(FoundingSiteModule.Bedroll)
            && isCompleted(FoundingSiteModule.Cache),
        _ => false,
    };

    public static string DisplayNameFor(FoundingSiteModule module) => module switch
    {
        FoundingSiteModule.Campfire => "Campfire",
        FoundingSiteModule.Bedroll => "Bedroll",
        FoundingSiteModule.Cache => "Cache",
        FoundingSiteModule.Canopy => "Canopy",
        _ => "Founding Site module",
    };
}
