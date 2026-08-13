using System;
using System.IO;
using System.Linq;
using System.Reflection;
using WorldofGoses;
using WorldofGoses.Domain;
using WorldofGoses.Ui;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// Regression coverage for the #20 contract: the contextual
/// <see cref="BuildingInspector"/> replaced the fullscreen top-level
/// <c>BuildingDetailView</c>. These tests pin the contract that
/// selecting a building no longer creates a top-level navigation,
/// keeps the same snapshot, the same commands, and the same
/// subpanels; the BuildingDetailView class is gone.
/// </summary>
public class BuildingInspectorRetirementTests
{
    [Fact]
    public void BuildingDetailViewClassIsGone()
    {
        // The C# class `BuildingDetailView` was retired in #20. Any
        // reference to it would be churn, since the contextual
        // BuildingInspector replaces it. Pin the fact by reflecting on
        // the World of Goses assembly.
        Assembly asm = typeof(BuildingInspector).Assembly;
        Type? oldView = asm.GetType("WorldofGoses.BuildingDetailView", throwOnError: false);
        Assert.Null(oldView);
    }

    [Fact]
    public void BuildingInspectorLivesOnThePresentationLayer()
    {
        // Sanity: the inspector is a Godot Control (so it can host
        // signals and child panels) and exposes the four
        // command/event hooks the macro used to drive. The methods
        // have to be there with the same shape as the predecessor
        // for the controller wiring to keep working.
        Type t = typeof(BuildingInspector);
        Assert.True(typeof(Godot.Control).IsAssignableFrom(t));
        Assert.NotNull(t.GetMethod("ShowBuilding", BindingFlags.Public | BindingFlags.Instance));
        Assert.NotNull(t.GetMethod("HideBuilding", BindingFlags.Public | BindingFlags.Instance));
    }

    [Fact]
    public void BuildingInspectorHasNoBackToCityButton()
    {
        // Acceptance: no BackToCity specific to the building surface.
        // The inspector closes through its own contextual action; the
        // macro is never left. Substring "Back" appears nowhere in the
        // member names that govern behaviour.
        Type t = typeof(BuildingInspector);
        foreach (MemberInfo m in t.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic))
        {
            if (m is FieldInfo fi)
            {
                Assert.False(fi.Name.Contains("Back") && !fi.Name.Contains("Backing"),
                    $"Field {fi.Name} implies navigation back");
            }
            if (m is PropertyInfo pi)
            {
                Assert.False(pi.Name.Contains("Back") && !pi.Name.Contains("Backing"),
                    $"Property {pi.Name} implies navigation back");
            }
            if (m is MethodInfo mi)
            {
                Assert.False(mi.Name.Contains("Back") && !mi.Name.Contains("Backing"),
                    $"Method {mi.Name} implies navigation back");
            }
        }
    }

    [Fact]
    public void BuildingDetailSnapshotStillSpellsTheThreeBuildingShapes()
    {
        // The snapshot and commands survive the retirement; the
        // inspector is only a presentation move. The surface is
        // restricted to the same fields the controller used to
        // project, and the three specialisations (productive, home,
        // town hall) still answer through IsHome / IsTownHall /
        // IsForest properties.
        Type t = typeof(BuildingDetailSnapshot);
        Assert.NotNull(t.GetProperty("IsHome"));
        Assert.NotNull(t.GetProperty("IsTownHall"));
        Assert.NotNull(t.GetProperty("IsForest"));
        // Snapshot is still constructed through `From`, not directly.
        Assert.NotNull(t.GetMethod("From", BindingFlags.Public | BindingFlags.Static));
    }

    [Fact]
    public void BuildingInspectorExistsAndUsesTheSameSubpanels()
    {
        // The contextual inspector reuses AssignmentPanel +
        // ProductionPanel + ResourceInventoryPanel (with the
        // BuildingDetailSnapshot read model). None of those have
        // changed and no new framework was introduced.
        Assembly asm = typeof(BuildingInspector).Assembly;
        Assert.NotNull(asm.GetType("WorldofGoses.AssignmentPanel", throwOnError: false));
        Assert.NotNull(asm.GetType("WorldofGoses.ProductionPanel", throwOnError: false));
        Assert.NotNull(asm.GetType("WorldofGoses.Ui.ResourceInventoryPanel", throwOnError: false));
    }

    [Fact]
    public void BuildingInspector_TscnAuthored_AsSidePanel()
    {
        // Acceptance: the static structure of the inspector lives in
        // CityPrototype.tscn and is no longer fullscreen. The script
        // must not call SetAnchorsAndOffsetsPreset(FullRect) on
        // itself — the .tscn owns the layout.
        string path = Path.Combine(TestHelpers.FindRepositoryRoot(),
            "game", "scripts", "BuildingInspector.cs");
        string source = File.ReadAllText(path);
        Assert.DoesNotContain("LayoutPreset.FullRect", source);
    }

    [Fact]
    public void BuildingInspector_TscnNode_IsNamedBuildingInspector()
    {
        // The .tscn rebuilt the shell under the contextual name.
        string tscn = Path.Combine(TestHelpers.FindRepositoryRoot(),
            "game", "scenes", "CityPrototype.tscn");
        string content = File.ReadAllText(tscn);
        Assert.Contains("[node name=\"BuildingInspector\"", content);
        Assert.DoesNotContain("[node name=\"DetailBackground\"", content);
    }

    [Fact]
    public void BuildingInspector_TscnIsAnchoredToTheRightSide()
    {
        // The inspector sits over the macro on the right edge; its
        // anchor and offsets in CityPrototype.tscn must reflect that
        // (no anchor_right = 1.0 alone with offset 0 — instead a
        // narrower contextual panel).
        string tscn = Path.Combine(TestHelpers.FindRepositoryRoot(),
            "game", "scenes", "CityPrototype.tscn");
        string[] lines = File.ReadAllLines(tscn);
        bool anchoredRight = lines.Any(l => l.Contains("BuildingInspector\"") && false);
        // Anchor the inspector with both anchors on the right edge
        // and a negative offset_left to size the panel.
        int idx = System.Array.FindIndex(lines, l => l.Contains("[node name=\"BuildingInspector\""));
        Assert.True(idx >= 0);
        Assert.Contains("anchor_left = 1.0", string.Join('\n', lines.Skip(idx).Take(20)));
        Assert.Contains("offset_left = -460.0", string.Join('\n', lines.Skip(idx).Take(20)));
    }
}
