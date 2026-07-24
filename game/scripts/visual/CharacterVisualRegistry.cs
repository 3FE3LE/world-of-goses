#nullable enable
using System;
using System.Collections.Generic;
using Godot;
using WorldofGoses.Domain;

namespace WorldofGoses;

/// <summary>Central resolver for the imported lineage character scenes (canonical + appearance variants).</summary>
public static class CharacterVisualRegistry
{
    private const string Root = "res://assets/characters/lineages/";
    private static readonly IReadOnlyDictionary<VisualKey, string> ScenePaths = BuildScenePaths();

    public static string GetScenePath(LineageId lineage, CharacterBodyVariant bodyVariant) =>
        GetScenePath(lineage, AppearanceVariantId.Standard, bodyVariant);

    public static PackedScene LoadScene(LineageId lineage, CharacterBodyVariant bodyVariant) =>
        LoadScene(lineage, AppearanceVariantId.Standard, bodyVariant);

    public static string GetScenePath(LineageId lineage, AppearanceVariantId variant, CharacterBodyVariant bodyVariant)
    {
        var canonical = new VisualKey(lineage, variant, bodyVariant);
        if (ScenePaths.TryGetValue(canonical, out string? path)) return path;
        var fallback = new VisualKey(lineage, AppearanceVariantId.Standard, bodyVariant);
        if (ScenePaths.TryGetValue(fallback, out string? fallbackPath)) return fallbackPath;
        throw new ArgumentOutOfRangeException(
            nameof(lineage), lineage, "No character scene is registered for this lineage.");
    }

    public static PackedScene LoadScene(LineageId lineage, AppearanceVariantId variant, CharacterBodyVariant bodyVariant)
    {
        string path = GetScenePath(lineage, variant, bodyVariant);
        return ResourceLoader.Load<PackedScene>(path)
            ?? throw new InvalidOperationException($"Character scene could not be loaded: {path}");
    }

    public static CharacterBodyVariant ResolveBodyVariant(GenderId gender) =>
        gender switch
        {
            GenderId.Feminine => CharacterBodyVariant.Female,
            GenderId.Masculine => CharacterBodyVariant.Male,
            _ => CharacterBodyVariant.Male,
        };

    private static IReadOnlyDictionary<VisualKey, string> BuildScenePaths()
    {
        var paths = new Dictionary<VisualKey, string>();
        AddCanonical(paths, LineageId.Ardhen);
        AddCanonical(paths, LineageId.Eirune);
        AddCanonical(paths, LineageId.Kovari);
        AddCanonical(paths, LineageId.Myrven);
        AddCanonical(paths, LineageId.Vaelun);
        AddCanonical(paths, LineageId.Orveth);
        AddCanonical(paths, LineageId.Caelith);
        AddCanonical(paths, LineageId.Theryn);
        AddVariants(paths, LineageId.Ardhen);
        AddVariants(paths, LineageId.Eirune);
        AddVariants(paths, LineageId.Kovari);
        AddVariants(paths, LineageId.Myrven);
        AddVariants(paths, LineageId.Vaelun);
        AddVariants(paths, LineageId.Orveth);
        AddVariants(paths, LineageId.Caelith);
        AddVariants(paths, LineageId.Theryn);
        return paths;
    }

    private static void AddCanonical(IDictionary<VisualKey, string> paths, LineageId lineage)
    {
        Add(paths, lineage, AppearanceVariantId.Standard, CharacterBodyVariant.Male, "male", "male");
        Add(paths, lineage, AppearanceVariantId.Standard, CharacterBodyVariant.Female, "female", "female");
    }

    private static void AddVariants(IDictionary<VisualKey, string> paths, LineageId lineage)
    {
        foreach (var variant in ProfileVariantRegistry.VariantIds)
        {
            if (variant == AppearanceVariantId.Standard) continue;
            Add(paths, lineage, variant, CharacterBodyVariant.Male, "variants", "male");
            Add(paths, lineage, variant, CharacterBodyVariant.Female, "variants", "female");
        }
    }

    private static void Add(
        IDictionary<VisualKey, string> paths,
        LineageId lineage,
        AppearanceVariantId variant,
        CharacterBodyVariant bodyVariant,
        string folder,
        string bodyFolder)
    {
        string sceneName = variant == AppearanceVariantId.Standard
            ? lineage.Value + "_" + bodyFolder + ".tscn"
            : lineage.Value + "_" + variant.Value + "_" + bodyFolder + ".tscn";
        if (variant == AppearanceVariantId.Standard)
        {
            paths.Add(new VisualKey(lineage, variant, bodyVariant), Root + lineage.Value + "/" + folder + "/" + sceneName);
        }
        else
        {
            paths.Add(new VisualKey(lineage, variant, bodyVariant), Root + lineage.Value + "/" + folder + "/" + variant.Value + "/" + bodyFolder + "/" + sceneName);
        }
    }

    private readonly record struct VisualKey(LineageId Lineage, AppearanceVariantId Variant, CharacterBodyVariant BodyVariant);
}
