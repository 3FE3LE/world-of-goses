using System.Collections.Generic;
using WorldofGoses;
using WorldofGoses.Domain;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// Tests for the new presentation primitives introduced alongside
/// the first MVP building placeholders: the texture catalog
/// (<see cref="BuildingArt"/>), the pure diff helper exposed by
/// <see cref="BuildingPlotStage"/>, and the macro-view state-machine
/// function <see cref="CityMacroView.DetermineMacroMode"/>.
///
/// These tests deliberately do not touch Godot nodes — the
/// presentation classes here are exercised through their static
/// pure helpers to match the convention used by the rest of the
/// 232-test suite (domain-only, no engine runtime).
/// </summary>
public class BuildingPlotStageTests
{
    // ---------------- BuildingArt catalog ----------------

    [Fact]
    public void BuildingArt_GetTexturePath_KnownKinds_ReturnsNonNull()
    {
        Assert.NotNull(BuildingArt.GetTexturePath(BuildingKind.Home));
        Assert.NotNull(BuildingArt.GetTexturePath(BuildingKind.Quarry));
        Assert.NotNull(BuildingArt.GetTexturePath(BuildingKind.Farm));
    }

    [Fact]
    public void BuildingArt_GetTexturePath_KindsWithoutArt_ReturnsNull()
    {
        Assert.Null(BuildingArt.GetTexturePath(BuildingKind.Smithy));
        Assert.Null(BuildingArt.GetTexturePath(BuildingKind.PotionLab));
    }

    [Fact]
    public void BuildingArt_GetCanvasSize_KnownKinds_MatchConstants()
    {
        var home = BuildingArt.GetCanvasSize(BuildingKind.Home);
        var quarry = BuildingArt.GetCanvasSize(BuildingKind.Quarry);
        var farm = BuildingArt.GetCanvasSize(BuildingKind.Farm);
        Assert.NotNull(home);
        Assert.NotNull(quarry);
        Assert.NotNull(farm);
        Assert.Equal(new System.Numerics.Vector2(64, 64), home!.Value.ToDotNetVector());
        Assert.Equal(new System.Numerics.Vector2(128, 128), quarry!.Value.ToDotNetVector());
        Assert.Equal(new System.Numerics.Vector2(128, 128), farm!.Value.ToDotNetVector());
    }

    [Fact]
    public void BuildingArt_GetCanvasSize_KindsWithoutArt_ReturnsNull()
    {
        Assert.Null(BuildingArt.GetCanvasSize(BuildingKind.Smithy));
        Assert.Null(BuildingArt.GetCanvasSize(BuildingKind.PotionLab));
    }

    // ---------------- BuildingPlotStage.DiffEntries ----------------

    [Fact]
    public void BuildingPlotStage_DiffEntries_AddsAndRemovesCorrectIds()
    {
        var existing = new List<int> { 1, 2, 3 };
        var wanted = new List<int> { 2, 3, 4 };

        BuildingPlotStage.DiffEntries(existing, wanted, out var toAdd, out var toRemove);

        Assert.Equal(new List<int> { 4 }, toAdd);
        Assert.Equal(new List<int> { 1 }, toRemove);
    }

    [Fact]
    public void BuildingPlotStage_DiffEntries_EmptyExistingAddsAll()
    {
        var existing = new List<int>();
        var wanted = new List<int> { 7, 8 };

        BuildingPlotStage.DiffEntries(existing, wanted, out var toAdd, out var toRemove);

        Assert.Equal(new List<int> { 7, 8 }, toAdd);
        Assert.Empty(toRemove);
    }

    [Fact]
    public void BuildingPlotStage_DiffEntries_EmptyWantedRemovesAll()
    {
        var existing = new List<int> { 1, 2 };
        var wanted = new List<int>();

        BuildingPlotStage.DiffEntries(existing, wanted, out var toAdd, out var toRemove);

        Assert.Empty(toAdd);
        Assert.Equal(new List<int> { 1, 2 }, toRemove);
    }

    // ---------------- CityMacroView.DetermineMacroMode ----------------

    [Theory]
    [InlineData(0, 0, CityMacroView.MacroMode.Empty)]
    [InlineData(0, 1, CityMacroView.MacroMode.Construction)]
    [InlineData(1, 0, CityMacroView.MacroMode.Plots)]
    [InlineData(1, 1, CityMacroView.MacroMode.PlotsAndConstruction)]
    [InlineData(3, 2, CityMacroView.MacroMode.PlotsAndConstruction)]
    public void CityMacroView_DetermineMacroMode_ReturnsExpectedMode(
        int buildings, int projects, CityMacroView.MacroMode expected)
    {
        Assert.Equal(expected, CityMacroView.DetermineMacroMode(buildings, projects));
    }

    // ---------------- ConstructionProject.ResultingKind ----------------

    [Fact]
    public void ConstructionProject_ResultingKind_BasicShelterMapsToHome()
    {
        var project = new ConstructionProject(
            new BuildingId(1), ConstructionKind.BasicShelter, "Basic Shelter",
            requiredWork: 1, workerCapacity: 1);

        Assert.Equal(BuildingKind.Home, project.ResultingKind);
    }
}

/// <summary>
/// Small adapter so the test code can compare Godot <see cref="Godot.Vector2"/>
/// against <see cref="System.Numerics.Vector2"/> without pulling in
/// Godot.Vector2's operators.
/// </summary>
internal static class BuildingPlotStageTestVector
{
    public static System.Numerics.Vector2 ToDotNetVector(this Godot.Vector2 v) =>
        new(v.X, v.Y);
}