using System.Collections.Generic;
using System.Linq;
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

    [Theory]
    [InlineData(0, 960, "Construction · 0%")]
    [InlineData(240, 960, "Construction · 25%")]
    [InlineData(960, 960, "Construction · 100%")]
    public void BuildingPlot_FormatsVisibleConstructionProgress(
        int progress,
        int requiredWork,
        string expected)
    {
        Assert.Equal(expected, BuildingPlot.ConstructionProgressLabel(progress, requiredWork));
    }

    [Fact]
    public void BuildingPlot_InteractionRect_MatchesVisibleArtAndLabel()
    {
        var farm = BuildingPlot.InteractionRect(new Godot.Vector2(128, 128));
        var shelter = BuildingPlot.InteractionRect(new Godot.Vector2(64, 64));
        var forest = BuildingPlot.InteractionRect(null);
        var forestExplicit = BuildingPlot.InteractionRect(new Godot.Vector2(200, 200), isPlaceholder: true);

        Assert.Equal(128, farm.Size.X);
        Assert.True(farm.Position.X > 0);
        Assert.Equal(96, shelter.Size.X);
        Assert.True(shelter.Position.Y > 0);
        // Placeholder hitbox tracks the visible placeholder canvas
        // (192 - 2*24) instead of the full plot size.
        int placeholderSide = PresentationConstants.MacroPlotSize - 48;
        Assert.Equal(placeholderSide, forest.Size.X);
        Assert.Equal(placeholderSide, forest.Size.Y);
        Assert.Equal(24, forest.Position.X);
        Assert.Equal(24, forest.Position.Y);
        // Explicit isPlaceholder also returns the placeholder rect,
        // ignoring the supplied canvas size.
        Assert.Equal(placeholderSide, forestExplicit.Size.X);
        Assert.Equal(placeholderSide, forestExplicit.Size.Y);
    }

    [Fact]
    public void BuildingPlotStage_PlacementRectUsesPersistedParcelLot()
    {
        CityWorld world = TestHelpers.NewProductionWorld();
        CityMacroSnapshot snapshot = CityMacroSnapshot.From(world);
        CityMacroSnapshot.PlotItem first = snapshot.Buildings[0];
        CityMacroSnapshot.PlotItem second = snapshot.Buildings[1];

        Godot.Rect2 firstRect = BuildingPlotStage.CalculatePlacementRect(
            new Godot.Vector2(1280, 720),
            first);
        Godot.Rect2 secondRect = BuildingPlotStage.CalculatePlacementRect(
            new Godot.Vector2(1280, 720),
            second);

        Assert.NotEqual(firstRect.Position, secondRect.Position);
        Assert.Equal(firstRect.Size, secondRect.Size);
        Assert.True(secondRect.Position.X > firstRect.Position.X);
    }

    [Fact]
    public void BuildingPlotStage_SolidRectsPreserveAAndBSetbacks()
    {
        CityMacroSnapshot snapshot =
            CityMacroSnapshot.From(TestHelpers.NewProductionWorld());
        CityMacroSnapshot.PlotItem quarry = snapshot.Buildings.Single(
            item => item.Kind == BuildingKind.Quarry);
        CityMacroSnapshot.PlotItem farm = snapshot.Buildings.Single(
            item => item.Kind == BuildingKind.Farm);
        Godot.Vector2 reservedSize = new(96, 96);

        Godot.Rect2 quarrySolid =
            BuildingPlotStage.CalculateSolidLocalRect(reservedSize, quarry);
        Godot.Rect2 farmSolid =
            BuildingPlotStage.CalculateSolidLocalRect(reservedSize, farm);

        Assert.Equal(new Godot.Rect2(0, 0, 96, 64), quarrySolid);
        Assert.Equal(new Godot.Rect2(16, 0, 64, 64), farmSolid);
    }

    [Fact]
    public void BuildingPlotStage_AdjacentAProfilesLeaveOneTilePath()
    {
        CityMacroSnapshot snapshot =
            CityMacroSnapshot.From(TestHelpers.NewProductionWorld());
        CityMacroSnapshot.PlotItem farm = snapshot.Buildings.Single(
            item => item.Kind == BuildingKind.Farm);
        Godot.Rect2 firstLot = new(0, 0, 96, 96);
        Godot.Rect2 secondLot = new(96, 0, 96, 96);
        Godot.Rect2 solid =
            BuildingPlotStage.CalculateSolidLocalRect(firstLot.Size, farm);
        Godot.Rect2 firstSolid = new(firstLot.Position + solid.Position, solid.Size);
        Godot.Rect2 secondSolid = new(secondLot.Position + solid.Position, solid.Size);

        Assert.Equal(32, secondSolid.Position.X - firstSolid.End.X);
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

    [Fact]
    public void CityMacroView_ResolveModalIntent_PreservesExplicitOpenAcrossTicks()
    {
        bool open = CityMacroView.ResolveModalIntent(
            CityMacroView.MacroMode.Plots,
            CityMacroView.MacroMode.Plots,
            currentIntent: true);

        Assert.True(open);
    }

    [Fact]
    public void CityMacroView_ResolveModalIntent_AutoOpensOnlyOnConstructionTransition()
    {
        Assert.True(CityMacroView.ResolveModalIntent(
            CityMacroView.MacroMode.Empty,
            CityMacroView.MacroMode.Construction,
            currentIntent: false));
        Assert.False(CityMacroView.ResolveModalIntent(
            CityMacroView.MacroMode.Construction,
            CityMacroView.MacroMode.Plots,
            currentIntent: true));
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
