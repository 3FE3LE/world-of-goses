#nullable enable
using System;
using System.Collections.Generic;
using Godot;
using WorldofGoses.Domain;

namespace WorldofGoses;

/// <summary>Central resolver for the sixteen imported lineage character scenes.</summary>
public static class CharacterVisualRegistry
{
    private const string Root = "res://assets/characters/lineages/";
    private static readonly IReadOnlyDictionary<VisualKey, string> ScenePaths = BuildScenePaths();

    public static string GetScenePath(LineageId lineage, CharacterBodyVariant bodyVariant)
    {
        var key = new VisualKey(lineage, bodyVariant);
        if (ScenePaths.TryGetValue(key, out string? path)) return path;
        throw new ArgumentOutOfRangeException(
            nameof(lineage), lineage, "No character scene is registered for this lineage.");
    }

    public static PackedScene LoadScene(LineageId lineage, CharacterBodyVariant bodyVariant)
    {
        string path = GetScenePath(lineage, bodyVariant);
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
        AddLineage(paths, LineageId.Ardhen);
        AddLineage(paths, LineageId.Eirune);
        AddLineage(paths, LineageId.Kovari);
        AddLineage(paths, LineageId.Myrven);
        AddLineage(paths, LineageId.Vaelun);
        AddLineage(paths, LineageId.Orveth);
        AddLineage(paths, LineageId.Caelith);
        AddLineage(paths, LineageId.Theryn);
        return paths;
    }

    private static void AddLineage(IDictionary<VisualKey, string> paths, LineageId lineage)
    {
        Add(paths, lineage, CharacterBodyVariant.Male, "male");
        Add(paths, lineage, CharacterBodyVariant.Female, "female");
    }

    private static void Add(
        IDictionary<VisualKey, string> paths,
        LineageId lineage,
        CharacterBodyVariant bodyVariant,
        string variantFolder)
    {
        string sceneName = lineage.Value + "_" + variantFolder + ".tscn";
        paths.Add(
            new VisualKey(lineage, bodyVariant),
            Root + lineage.Value + "/" + variantFolder + "/" + sceneName);
    }

    private readonly record struct VisualKey(LineageId Lineage, CharacterBodyVariant BodyVariant);
}
