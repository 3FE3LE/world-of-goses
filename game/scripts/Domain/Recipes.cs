#nullable enable
using System;
using System.Collections.Generic;

namespace WorldofGoses.Domain;

/// <summary>
/// Opaque, stable identifier for a <see cref="Recipe"/>.
/// Wrapping a domain string keeps the domain free of magic-string
/// residue: every <see cref="ResourceType"/> in <see cref="Recipe.RequiredInputs"/>
/// is already a strongly-typed enum, and the recipe id only appears
/// in the registry and in <c>WorldEvent</c> diagnostics.
/// </summary>
public readonly record struct RecipeId(string Value);

/// <summary>
/// One input line for a <see cref="Recipe"/>: a resource and the
/// amount needed. For construction recipes the amount is the
/// **total** required over the worksite's lifetime; for operating
/// recipes the amount is the **per-tick** cost while the building
/// produces.
/// </summary>
public readonly record struct RecipeInput(ResourceType Resource, int Amount);

/// <summary>
/// Pure data describing what a construction site consumes over its
/// lifetime, or what an operating building consumes per producing
/// tick. The <see cref="Output"/> is a single <see cref="ResourceType"/>
/// for now; future slices can widen this to a list without touching
/// callers that only need the input contract.
/// </summary>
public sealed record Recipe(
    RecipeId Id,
    IReadOnlyList<RecipeInput> RequiredInputs,
    ResourceType Output);

/// <summary>
/// Static catalog of every recipe the world recognises. Mirrors the
/// <see cref="ConstructionRules"/> / <see cref="StaminaRules"/>
/// pattern: a single Godot-free registry that tests and presentation
/// code can both consume.
///
/// Lookups are keyed by <see cref="ConstructionKind"/> for worksite
/// recipes and by <see cref="BuildingKind"/> for operating recipes.
/// A kind with no recipe (e.g. <see cref="BuildingKind.Home"/>) returns
/// <c>null</c>; the simulation treats that as "no material cost".
/// </summary>
public static class Recipes
{
    private static readonly IReadOnlyDictionary<RecipeId, Recipe> Registry =
        new Dictionary<RecipeId, Recipe>
        {
            // Construction-time recipes. The Basic Shelter is the
            // founding home and consumes no materials — the player
            // is a single pioneer with no stockpile to draw from.
            [new RecipeId("construction.basicshelter")] = new Recipe(
                new RecipeId("construction.basicshelter"),
                RequiredInputs: new[]
                {
                    new RecipeInput(ResourceType.Wood, 4),
                },
                Output: ResourceType.Stone),

            [new RecipeId("construction.farm")] = new Recipe(
                new RecipeId("construction.farm"),
                RequiredInputs: new[]
                {
                    new RecipeInput(ResourceType.Wood, 6),
                },
                Output: ResourceType.Food),

            [new RecipeId("construction.quarry")] = new Recipe(
                new RecipeId("construction.quarry"),
                RequiredInputs: new[]
                {
                    new RecipeInput(ResourceType.Wood, 8),
                    new RecipeInput(ResourceType.Food, 4),
                },
                Output: ResourceType.Stone),

            // Farm and Quarry currently pay through labour, stamina and time.
            // Material operating inputs return only when the prototype has a
            // real tools/fuel chain that the player can actually sustain.
        };

    /// <summary>Read-only view of every recipe in the registry.</summary>
    public static IReadOnlyDictionary<RecipeId, Recipe> All => Registry;

    /// <summary>
    /// Looks up the construction-time recipe for the given kind.
    /// Returns <c>null</c> when the kind has no recipe (a future
    /// custom construction may not yet be catalogued).
    /// </summary>
    public static Recipe? ConstructionRecipeFor(ConstructionKind kind) =>
        Registry.TryGetValue(IdFor(kind), out var recipe) ? recipe : null;

    /// <summary>
    /// Looks up the operating-time recipe for the given building kind.
    /// Returns <c>null</c> for buildings that consume no inputs while
    /// running (e.g. <see cref="BuildingKind.Home"/>).
    /// </summary>
    public static Recipe? OperatingRecipeFor(BuildingKind kind) =>
        Registry.TryGetValue(IdFor(kind), out var recipe) ? recipe : null;

    public static RecipeId IdFor(ConstructionKind kind) => kind switch
    {
        ConstructionKind.BasicShelter => new RecipeId("construction.basicshelter"),
        ConstructionKind.Farm => new RecipeId("construction.farm"),
        ConstructionKind.Quarry => new RecipeId("construction.quarry"),
        _ => new RecipeId($"construction.{kind.ToString().ToLowerInvariant()}"),
    };

    public static RecipeId IdFor(BuildingKind kind) => kind switch
    {
        BuildingKind.Home => new RecipeId("operating.home"),
        BuildingKind.Farm => new RecipeId("operating.farm"),
        BuildingKind.Quarry => new RecipeId("operating.quarry"),
        _ => new RecipeId($"operating.{kind.ToString().ToLowerInvariant()}"),
    };
}
