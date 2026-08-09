using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using WorldofGoses.Domain;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// Structural guards for the macro HUD's authored shared surfaces.
/// </summary>
/// <remarks>
/// <para>
/// These assert the scene, not the pixels, because the properties they protect are
/// the ones a screenshot hides. A dock that silently reverts to
/// <c>mouse_filter = 2</c> still looks correct and quietly passes its clicks
/// through to the world behind it. An inspector that loses <c>grow_vertical = 0</c>
/// still renders until its text wraps to a second line.
/// </para>
/// <para>
/// They also cover surfaces no visual-regression fixture reaches, which is the gap
/// that made this pass hard to verify: <c>AssignmentPanel</c> and
/// <c>ProductionPanel</c> hide themselves for homes and the town hall, so every
/// available fixture renders neither.
/// </para>
/// </remarks>
public sealed class HudCompositionTests
{
    [Theory]
    [InlineData("PrimaryNavDock")]
    [InlineData("ContextInspector")]
    [InlineData("ActionDock")]
    [InlineData("CityStatusPanel")]
    [InlineData("CitySummaryPanel")]
    [InlineData("SimulationControls")]
    [InlineData("ExpeditionRail")]
    public void HudSurface_IsAuthoredInTheScene(string nodeName)
    {
        string[] lines = ReadScene();

        Assert.True(
            IndexOfNodeHeader(lines, nodeName) >= 0,
            $"{nodeName} must be authored in CityPrototype.tscn. Constructing a shared "
            + "HUD surface at runtime is what kept the selection panel invisible to the "
            + "editor and drove it to reposition itself every frame.");
    }

    [Fact]
    public void PrimaryNavDock_ClaimsItsOwnPointerInput()
    {
        string[] block = NodeBlock("PrimaryNavDock");

        Assert.DoesNotContain(
            block,
            line => MouseFilterPattern.Match(line) is { Success: true } m && m.Groups[1].Value == "2");
    }

    [Fact]
    public void ContextInspector_DoesNotBlockTheWorldBehindIt()
    {
        string[] block = NodeBlock("ContextInspector");

        Assert.Contains(
            block,
            line => MouseFilterPattern.Match(line) is { Success: true } m && m.Groups[1].Value == "2");
    }

    [Fact]
    public void ContextInspector_GrowsUpwardFromItsBottomAnchor()
    {
        string[] block = NodeBlock("ContextInspector");

        Assert.Contains(block, line => line.Trim() == "anchor_top = 1.0");
        Assert.Contains(block, line => line.Trim() == "anchor_bottom = 1.0");
        // GROW_DIRECTION_BEGIN. Without it the panel is pinned bottom but expands
        // downward off-screen as its detail text wraps, which is the failure the
        // per-frame reposition was compensating for.
        Assert.Contains(block, line => line.Trim() == "grow_vertical = 0");
    }

    [Fact]
    public void ContextInspector_StartsHiddenAndUsesItsTypedShowContract()
    {
        string[] block = NodeBlock("ContextInspector");
        string source = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(), "game", "scripts", "Ui", "ContextInspector.cs"));

        Assert.Contains(block, line => line.Trim() == "visible = false");
        Assert.Contains("public void ShowSelection(Texture2D? icon, string title, string detail)",
            source, StringComparison.Ordinal);
        Assert.Contains("Show();", source, StringComparison.Ordinal);
        Assert.DoesNotMatch(@"override\s+void\s+_Process\s*\(", source);
    }

    [Fact]
    public void ActionDock_StartsHidden()
    {
        string[] block = NodeBlock("ActionDock");

        // A contextual tray, not a permanent toolbar: only a mode with an action to
        // offer may reveal it.
        Assert.Contains(block, line => line.Trim() == "visible = false");
    }

    [Fact]
    public void MacroHudSurfaces_AreAuthoredCompactAndOwnedByTheMacroPerspective()
    {
        string[] summary = NodeBlock("CitySummaryPanel");
        string[] rail = NodeBlock("ExpeditionRail");
        string[] primary = NodeBlock("PrimaryNavDock");
        string[] simulation = NodeBlock("SimulationControls");
        string[] action = NodeBlock("ActionDock");
        string[] inspector = NodeBlock("ContextInspector");
        string macro = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(), "game", "scripts", "Prototypes",
            "MacroStreetLiveView.cs"));
        string actionSource = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(), "game", "scripts", "Ui", "ActionDock.cs"));
        string inspectorSource = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(), "game", "scripts", "Ui", "ContextInspector.cs"));

        Assert.All(new[] { summary, rail, primary, simulation }, block =>
            Assert.Contains(block, line => line.Trim() == "visible = false"));
        Assert.Contains(action, line => line.Trim() == "theme_type_variation = &\"HudDock\"");
        Assert.Contains(action, line => line.Trim() == "mouse_filter = 0");
        Assert.Contains(inspector, line => line.Trim() == "theme_type_variation = &\"HudCard\"");
        Assert.Contains("ThemeTypeVariation = \"HudButtonSelected\"", actionSource,
            StringComparison.Ordinal);
        Assert.Contains("ThemeTypeVariation = \"HudButton\"", actionSource,
            StringComparison.Ordinal);
        Assert.DoesNotContain("ButtonText", actionSource, StringComparison.Ordinal);
        Assert.DoesNotContain("OverlayPanel", actionSource, StringComparison.Ordinal);
        Assert.Contains("ThemeTypeVariation = \"HudHeader\"", inspectorSource,
            StringComparison.Ordinal);
        Assert.Contains("ThemeTypeVariation = \"HudCaption\"", inspectorSource,
            StringComparison.Ordinal);
        Assert.Contains("private void ShowMacroHudSurfaces()", macro, StringComparison.Ordinal);
        Assert.Contains("private void HideMacroHudSurfaces()", macro, StringComparison.Ordinal);
        Assert.Contains("_citySummaryPanel.Show();", macro, StringComparison.Ordinal);
        Assert.Contains("_expeditionRail.Show();", macro, StringComparison.Ordinal);
        Assert.Contains("_simulationControls.Show();", macro, StringComparison.Ordinal);
        Assert.Contains("_citySummaryPanel.Hide();", macro, StringComparison.Ordinal);
        Assert.Contains("_expeditionRail.Hide();", macro, StringComparison.Ordinal);
        Assert.Contains("_simulationControls.Hide();", macro, StringComparison.Ordinal);
    }

    [Fact]
    public void MacroActions_IsGone()
    {
        string[] lines = ReadScene();

        Assert.True(
            IndexOfNodeHeader(lines, "MacroActions") < 0,
            "MacroActions was the full-width strip the primary dock replaced. "
            + "Reintroducing it costs the city 42 px of viewport height across its "
            + "whole width for seven buttons.");
    }

    [Fact]
    public void CityStatusPanel_UsesTheCompactEdgeToEdgeHudSurface()
    {
        string[] block = NodeBlock("CityStatusPanel");

        Assert.Contains(block, line => line.Trim() == "custom_minimum_size = Vector2(0, 40)");
        Assert.Contains(block, line => line.Trim() == "theme_type_variation = &\"HudSurface\"");
    }

    [Fact]
    public void CityStatusPanel_HasFixedLogicalCompositionWithoutViewportBranching()
    {
        string source = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(),
            "game",
            "scripts",
            "CityStatusPanel.cs"));

        Assert.Contains("Name = \"BrandBlock\"", source, StringComparison.Ordinal);
        Assert.Contains("Name = \"WorldContext\"", source, StringComparison.Ordinal);
        Assert.Contains("Name = \"ResourceTicker\"", source, StringComparison.Ordinal);
        Assert.Contains("Name = \"Population\"", source, StringComparison.Ordinal);
        Assert.Contains("ThemeTypeVariation = \"HudBrand\"", source, StringComparison.Ordinal);
        Assert.Contains("StatChip.HudIconValue", source, StringComparison.Ordinal);
        Assert.DoesNotContain("DisplayServer.WindowGetSize", source, StringComparison.Ordinal);
        Assert.DoesNotContain("GetVisibleRect", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AddThemeStyleboxOverride", source, StringComparison.Ordinal);
        Assert.Contains("CreateTimer(2.25)", source, StringComparison.Ordinal);
        Assert.Contains("_saveIndicatorVisible = false", source, StringComparison.Ordinal);
        Assert.DoesNotContain("PlayPauseButton", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SpeedButton", source, StringComparison.Ordinal);
    }

    [Fact]
    public void CitySummaryAndInspector_HaveDeterministicNonOverlappingAuthoredSlots()
    {
        string[] summary = NodeBlock("CitySummaryPanel");
        string[] inspector = NodeBlock("ContextInspector");

        Assert.Contains(summary, line => line.Trim() == "offset_left = 8.0");
        Assert.Contains(summary, line => line.Trim() == "offset_right = 248.0");
        Assert.Contains(summary, line => line.Trim() == "offset_top = 8.0");
        Assert.Contains(summary, line => line.Trim() == "theme_type_variation = &\"HudSurface\"");
        Assert.Contains(inspector, line => line.Trim() == "offset_left = 256.0");
        Assert.Contains(inspector, line => line.Trim() == "offset_right = 476.0");
        Assert.Contains(inspector, line => line.Trim() == "offset_bottom = -88.0");
    }

    [Fact]
    public void ExpeditionRail_IsAuthoredRightFixedAndOwnsPointerAndWheelInput()
    {
        string[] rail = NodeBlock("ExpeditionRail");
        string source = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(), "game", "scripts", "ExpeditionRail.cs"));
        string chronicle = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(), "game", "scripts", "Ui", "ChroniclePanel.cs"));

        Assert.Contains(rail, line => line.Trim() == "anchor_left = 1.0");
        Assert.Contains(rail, line => line.Trim() == "anchor_bottom = 1.0");
        Assert.Contains(rail, line => line.Trim() == "offset_left = -244.0");
        Assert.Contains(rail, line => line.Trim() == "offset_right = -8.0");
        Assert.Contains(rail, line => line.Trim() == "offset_top = 8.0");
        Assert.Contains(rail, line => line.Trim() == "offset_bottom = -104.0");
        Assert.Contains(rail, line => line.Trim() == "mouse_filter = 0");
        Assert.Contains(rail, line => line.Trim() == "theme_type_variation = &\"HudSurface\"");
        Assert.Contains("ChronicleEventProjection.MeaningfulEvents", chronicle, StringComparison.Ordinal);
        Assert.Contains("ChronicleEventProjection.Compact", chronicle, StringComparison.Ordinal);
        Assert.Contains("_scroll.AcceptEvent();", source, StringComparison.Ordinal);
        Assert.Contains("GetViewport().SetInputAsHandled();", source, StringComparison.Ordinal);
        Assert.Contains("RestorePendingFocus", source, StringComparison.Ordinal);
        Assert.Contains("if (_refreshQueued) return;", source, StringComparison.Ordinal);
        Assert.Contains("if (focusedIndex >= 0) _pendingFocusIndex = focusedIndex;", source, StringComparison.Ordinal);
        Assert.Contains("_localeManager.LocaleChanged += OnLocaleChanged", source, StringComparison.Ordinal);
        Assert.Contains("FocusNeighborTop", source, StringComparison.Ordinal);
        Assert.Contains("FocusNeighborBottom", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ui.expedition_rail.queue", source, StringComparison.Ordinal);
        Assert.DoesNotMatch(@"override\s+void\s+_Process\s*\(", source);
    }

    [Fact]
    public void ExpeditionRail_ReusesPlanningAndOwnsTheCompleteChronicle()
    {
        string rail = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(), "game", "scripts", "ExpeditionRail.cs"));
        string chronicle = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(), "game", "scripts", "Ui", "ChroniclePanel.cs"));
        string card = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(), "game", "scripts", "Ui", "ExpeditionCompactCard.cs"));

        Assert.Contains("_expeditionPanel.Open(id);", rail, StringComparison.Ordinal);
        Assert.Contains("new ChroniclePanel()", rail, StringComparison.Ordinal);
        Assert.Contains("_controller.LastOfflineReport", rail, StringComparison.Ordinal);
        Assert.Contains("GetExpeditionRailSnapshot", rail, StringComparison.Ordinal);
        Assert.Contains("ChronicleEventProjection.MeaningfulEvents", chronicle, StringComparison.Ordinal);
        Assert.Contains("ChronicleEventProjection.Compact", chronicle, StringComparison.Ordinal);
        Assert.Contains("SimulationTimeText.FormatDurationLocalized", chronicle, StringComparison.Ordinal);
        Assert.Contains("SelectBuilding(buildingId)", chronicle, StringComparison.Ordinal);
        Assert.Contains("evt.Subject.Kind", chronicle, StringComparison.Ordinal);
        Assert.Contains("evt.Subject.EntityId", chronicle, StringComparison.Ordinal);
        Assert.DoesNotContain("_controller.World", chronicle, StringComparison.Ordinal);
        Assert.Contains("ThemeTypeVariation = \"HudCard\"", card, StringComparison.Ordinal);
        Assert.Contains("StatChip.HudIconValue", card, StringComparison.Ordinal);
        Assert.Contains("HudProgressBar", card, StringComparison.Ordinal);
        Assert.Contains("HudButtonDanger", card, StringComparison.Ordinal);
        Assert.Contains("UiText.Get(item.DisplayName)", card, StringComparison.Ordinal);

        string panel = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(), "game", "scripts", "ExpeditionPanel.cs"));
        Assert.Contains("public void Open(ExpeditionId expeditionId)", panel, StringComparison.Ordinal);
        Assert.Contains("PresentedExpeditionId = active?.Id;", panel, StringComparison.Ordinal);
    }

    [Fact]
    public void NavigationRail_IsGoneAfterPrimaryDockMigration()
    {
        Assert.True(IndexOfNodeHeader(ReadScene(), "NavigationRail") < 0);
        string root = TestHelpers.FindRepositoryRoot();
        Assert.False(File.Exists(Path.Combine(root, "game", "scripts", "Ui", "NavigationRail.cs")));
    }

    [Fact]
    public void PrimaryNavDock_IsBottomCentredLabelledAndHorizontallyFocused()
    {
        string[] dock = NodeBlock("PrimaryNavDock");
        string source = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(), "game", "scripts", "Ui", "PrimaryNavDock.cs"));
        string macroSource = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(), "game", "scripts", "Prototypes", "MacroStreetLiveView.cs"));
        string pauseSource = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(), "game", "scripts", "PauseMenu.cs"));

        Assert.Contains(dock, line => line.Trim() == "anchor_left = 0.5");
        Assert.Contains(dock, line => line.Trim() == "anchor_bottom = 1.0");
        Assert.Contains(dock, line => line.Trim() == "offset_bottom = -16.0");
        // The dock widens for the labelled profile. The literal is a visual
        // iteration value: re-tune only after human visual sign-off at both
        // 1280×720 and 1920×1080, then bump the bounds here.
        string? sizeLine = dock.FirstOrDefault(
            line => line.Trim().StartsWith("custom_minimum_size = Vector2("));
        Assert.NotNull(sizeLine);
        var sizeMatch = SizeLiteral.Match(sizeLine!);
        Assert.True(
            sizeMatch.Success,
            $"PrimaryNavDock custom_minimum_size line did not match expected format: {sizeLine}");
        float width = float.Parse(
            sizeMatch.Groups[1].Value, CultureInfo.InvariantCulture);
        float height = float.Parse(
            sizeMatch.Groups[2].Value, CultureInfo.InvariantCulture);
        Assert.InRange(width, 480f, 560f);
        Assert.InRange(height, 56f, 72f);
        Assert.Contains(dock, line => line.Trim() == "theme_type_variation = &\"HudDock\"");
        // Per-button width lives as a named constant on the script so a future
        // re-tune does not silently split this assertion from the visual value.
        Assert.Contains("PerButtonWidth", source, StringComparison.Ordinal);
        Assert.Contains("button.ShowLabel = true", source, StringComparison.Ordinal);
        Assert.Contains("button.ClipText = false", source, StringComparison.Ordinal);
        Assert.Contains("IconPaths.Backpack", source, StringComparison.Ordinal);
        Assert.Contains("IconPaths.ClipboardNote", source, StringComparison.Ordinal);
        Assert.Contains("FocusNeighborLeft", source, StringComparison.Ordinal);
        Assert.Contains("FocusNeighborRight", source, StringComparison.Ordinal);
        Assert.Contains("public IconButton ConstructionButton", source, StringComparison.Ordinal);
        Assert.DoesNotContain("public IconButton CameraButton", source, StringComparison.Ordinal);
        // Menu no longer belongs in the dock; the menu button moved to the
        // top-bar utility cluster in CityStatusPanel.
        Assert.DoesNotContain("public IconButton MenuButton", source, StringComparison.Ordinal);
        Assert.Contains("PrimaryNavDockPath", macroSource, StringComparison.Ordinal);
        Assert.DoesNotContain("NavigationRailPath", macroSource, StringComparison.Ordinal);
        Assert.Contains("_localeManager.LocaleChanged += OnLocaleChanged", macroSource, StringComparison.Ordinal);
        Assert.Contains("_localeManager.LocaleChanged -= OnLocaleChanged", macroSource, StringComparison.Ordinal);
        Assert.Contains("UpdatePrimaryNavigationState();\n        UpdateCameraModeButtonLabel();", Normalize(macroSource), StringComparison.Ordinal);
        Assert.Contains(
            "CityStatusPanel/SafeArea/StatusComposition/UtilityCluster/MenuButton",
            pauseSource,
            StringComparison.Ordinal);
    }

    [Fact]
    public void PrimaryAndContextualDocks_AreMutuallyExclusiveAcrossPlacementAndEscape()
    {
        string source = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(), "game", "scripts", "Prototypes", "MacroStreetLiveView.cs"));
        string actionDockSource = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(), "game", "scripts", "Ui", "ActionDock.cs"));

        Assert.Contains("private void ShowPrimaryNavigation()", source, StringComparison.Ordinal);
        Assert.Contains("_actionDock.Hide();\n        _primaryNavDock.Show();", Normalize(source), StringComparison.Ordinal);
        Assert.Contains("_primaryNavDock.Hide();\n        _actionDock.Show();", Normalize(source), StringComparison.Ordinal);
        Assert.Contains("if (restorePrimaryNavigation) _primaryNavDock.Show();", source, StringComparison.Ordinal);
        Assert.Contains("_placementActive && @event.IsActionPressed(\"ui_cancel\")", source, StringComparison.Ordinal);
        Assert.Contains("CancelPlacement();", source, StringComparison.Ordinal);
        Assert.Contains("_confirmButton.FocusNeighborLeft", actionDockSource, StringComparison.Ordinal);
        Assert.Contains("_confirmButton.FocusNeighborRight", actionDockSource, StringComparison.Ordinal);
        Assert.Contains("_cancelButton.FocusNeighborLeft", actionDockSource, StringComparison.Ordinal);
        Assert.Contains("_cancelButton.FocusNeighborRight", actionDockSource, StringComparison.Ordinal);
    }

    [Fact]
    public void MacroArrowKeys_MoveWorldWithoutMovingHudFocus()
    {
        string source = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(), "game", "scripts", "Prototypes", "MacroStreetLiveView.cs"));

        Assert.Contains("public override void _Input(InputEvent @event)", source, StringComparison.Ordinal);
        Assert.Contains("IsWorldNavigationArrow(key)", source, StringComparison.Ordinal);
        Assert.Contains("GetViewport().SetInputAsHandled();", source, StringComparison.Ordinal);
        Assert.Contains("key.Keycode is Key.Left or Key.Right or Key.Up or Key.Down", source, StringComparison.Ordinal);
        Assert.Contains("|| _pauseMenu.Visible", source, StringComparison.Ordinal);
        Assert.Contains("MotionTick(allowCameraInput: CanUseWorldNavigationInput);", source, StringComparison.Ordinal);
        Assert.Contains("&& !_pauseMenu.Visible", source, StringComparison.Ordinal);
        Assert.Contains("InputEventJoypadButton", File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(), "game", "scripts", "CityPrototype.cs")), StringComparison.Ordinal);
        Assert.Contains("case \"macro-arrow-focus-isolation\":", File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(), "game", "scripts", "CityPrototype.cs")), StringComparison.Ordinal);
        Assert.Contains("case \"pause-arrow-focus\":", File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(), "game", "scripts", "CityPrototype.cs")), StringComparison.Ordinal);
    }

    [Fact]
    public void ConstructionClosesItsModalBeforeOpeningHeroProfile()
    {
        string construction = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(), "game", "scripts", "ConstructionPanel.cs"));
        string macro = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(), "game", "scripts", "Prototypes", "MacroStreetLiveView.cs"));
        int closedBranch = macro.IndexOf("if (_selectHeroAfterModalClose)", StringComparison.Ordinal);
        int hero = macro.IndexOf("_controller.SelectHero();", closedBranch, StringComparison.Ordinal);

        Assert.Contains("EmitSignal(SignalName.ViewHeroRequested)", construction, StringComparison.Ordinal);
        Assert.DoesNotContain("_controller.SelectHero();", construction, StringComparison.Ordinal);
        Assert.Contains("_constructionPanel.ViewHeroRequested += OnConstructionHeroRequested", macro, StringComparison.Ordinal);
        Assert.Contains("_modalHost.Close();", macro[macro.IndexOf("private void OnConstructionHeroRequested", StringComparison.Ordinal)..], StringComparison.Ordinal);
        Assert.True(closedBranch >= 0, "Macro navigation must wait for ModalHost.Closed.");
        Assert.True(hero > closedBranch, "Hero selection must happen from the modal-closed branch.");
        Assert.Contains("case \"construction-hero-route\":", File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(), "game", "scripts", "CityPrototype.cs")), StringComparison.Ordinal);
    }

    [Fact]
    public void SimulationControls_OwnTheExistingPairAndHorizontalFocus()
    {
        string[] block = NodeBlock("SimulationControls");
        string source = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(), "game", "scripts", "SimulationControls.cs"));
        string controllerSource = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(), "game", "scripts", "CityWorldController.cs"));

        Assert.Contains(block, line => line.Trim() == "anchor_left = 1.0");
        Assert.Contains(block, line => line.Trim() == "anchor_bottom = 1.0");
        Assert.Contains(block, line => line.Trim() == "mouse_filter = 0");
        Assert.Contains(block, line => line.Trim() == "theme_type_variation = &\"HudDock\"");
        Assert.Contains("new PlayPauseButton", source, StringComparison.Ordinal);
        Assert.Contains("new SpeedButton", source, StringComparison.Ordinal);
        // Camera mode moved out of the bottom-right surface into the top-bar
        // utility cluster; the SimulationControls script no longer owns an
        // IconButton child or a CameraButton accessor.
        Assert.DoesNotContain("new IconButton", source, StringComparison.Ordinal);
        Assert.DoesNotContain("public IconButton CameraButton", source, StringComparison.Ordinal);
        Assert.DoesNotContain("_cameraButton", source, StringComparison.Ordinal);
        Assert.Contains("FocusNeighborLeft", source, StringComparison.Ordinal);
        Assert.Contains("FocusNeighborRight", source, StringComparison.Ordinal);
        Assert.Contains(
            "SetSimulationSpeed(_speed == SpeedChoice.Paused ? _lastRunningSpeed : SpeedChoice.Paused)",
            controllerSource,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Chronicle_IsIntegratedIntoTheRightRailWithoutLegacySurface()
    {
        string[] scene = ReadScene();
        string source = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(), "game", "scripts", "Ui", "ChroniclePanel.cs"));
        string root = TestHelpers.FindRepositoryRoot();

        Assert.DoesNotContain(scene, line => line.Contains("OfflineReportPanel", StringComparison.Ordinal));
        Assert.False(File.Exists(Path.Combine(root, "game", "scripts", "OfflineReportPanel.cs")));
        Assert.Contains("MaximumRows = 80", source, StringComparison.Ordinal);
        Assert.Contains("CompactRows = 4", source, StringComparison.Ordinal);
        Assert.DoesNotMatch(@"override\s+void\s+_Process\s*\(", source);
    }

    [Fact]
    public void ConnectedMacroMenus_ReuseTheCompactHudThemeWithoutChangingOwnership()
    {
        string root = TestHelpers.FindRepositoryRoot();
        string expeditionScene = File.ReadAllText(Path.Combine(
            root, "game", "scenes", "Components", "ExpeditionPanel.tscn"));
        string citizensScene = File.ReadAllText(Path.Combine(
            root, "game", "scenes", "Components", "MigrantPanel.tscn"));
        string policies = File.ReadAllText(Path.Combine(
            root, "game", "scripts", "PoliciesPanel.cs"));
        string construction = File.ReadAllText(Path.Combine(
            root, "game", "scripts", "ConstructionPanel.cs"));
        string pauseScene = File.ReadAllText(Path.Combine(
            root, "game", "scenes", "PauseMenu.tscn"));
        string pauseSource = File.ReadAllText(Path.Combine(
            root, "game", "scripts", "PauseMenu.cs"));
        string cityScene = string.Join('\n', ReadScene());

        Assert.Contains("theme_type_variation = \"HudSurface\"", expeditionScene, StringComparison.Ordinal);
        Assert.Contains("theme_type_variation = \"HudSurface\"", citizensScene, StringComparison.Ordinal);
        Assert.Contains("ThemeTypeVariation = \"HudSurface\"", policies, StringComparison.Ordinal);
        Assert.Contains("ThemeTypeVariation = \"HudProgress\"", construction, StringComparison.Ordinal);
        Assert.DoesNotContain("AddThemeStyleboxOverride(\"panel\"", construction, StringComparison.Ordinal);
        Assert.DoesNotContain("theme_type_variation = \"OverlayPanel\"", expeditionScene, StringComparison.Ordinal);
        Assert.DoesNotContain("theme_type_variation = \"OverlayPanel\"", citizensScene, StringComparison.Ordinal);
        Assert.Contains("theme_type_variation = \"HudSurface\"", pauseScene, StringComparison.Ordinal);
        Assert.DoesNotContain("theme_type_variation = \"OverlayPanel\"", pauseScene, StringComparison.Ordinal);
        Assert.DoesNotContain("AddThemeStyleboxOverride", pauseSource, StringComparison.Ordinal);
        Assert.Contains("theme_type_variation = &\"HudCard\"", cityScene, StringComparison.Ordinal);
        Assert.Contains("_modalHost.Open(this);", policies, StringComparison.Ordinal);
        Assert.Contains("public void Open(ExpeditionId expeditionId)", File.ReadAllText(Path.Combine(
            root, "game", "scripts", "ExpeditionPanel.cs")), StringComparison.Ordinal);
    }

    [Fact]
    public void NavigationFixtures_UseRealDirectionalAndPointerInputEvents()
    {
        string source = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(), "game", "scripts", "CityPrototype.cs"));

        Assert.Contains("case \"primary-nav-focus\":", source, StringComparison.Ordinal);
        // The dock is a visual iteration value; the comparison in the
        // fixture must use a named constant so re-tuning touches one place.
        Assert.Contains("PrimaryNavDockSize", source, StringComparison.Ordinal);
        Assert.Contains("case \"action-dock-focus\":", source, StringComparison.Ordinal);
        Assert.Contains("case \"simulation-controls-focus\":", source, StringComparison.Ordinal);
        Assert.Contains("ButtonIndex = JoyButton.DpadRight", source, StringComparison.Ordinal);
        Assert.Contains("new InputEventJoypadButton", source, StringComparison.Ordinal);
        Assert.Contains("GrabDefaultFocus();", source, StringComparison.Ordinal);
        Assert.Contains("actionDock.ConfirmButton.GrabFocus();", source, StringComparison.Ordinal);
        Assert.Contains("[WOG-ACTION-DOCK-FOCUS] ui_right -> Cancel OK", source, StringComparison.Ordinal);
        Assert.Contains("[WOG-SIMULATION-FOCUS] ui_right -> Speed OK", source, StringComparison.Ordinal);
        Assert.Contains("primary-nav-click-", source, StringComparison.Ordinal);
        Assert.Contains("construction-placement-confirm-click", source, StringComparison.Ordinal);
        Assert.Contains("construction-placement-cancel-click", source, StringComparison.Ordinal);
        Assert.Contains("simulation-click-", source, StringComparison.Ordinal);
        Assert.Contains("expedition-rail-click-", source, StringComparison.Ordinal);
        Assert.Contains("case \"expedition-rail-focus\":", source, StringComparison.Ordinal);
        Assert.Contains("case \"expedition-rail-phase-focus\":", source, StringComparison.Ordinal);
        Assert.Contains("case \"macro-hud-default\":", source, StringComparison.Ordinal);
        Assert.Contains("case \"macro-hud-selection\":", source, StringComparison.Ordinal);
        Assert.Contains("case \"macro-hud-active-construction\":", source, StringComparison.Ordinal);
        Assert.Contains("case \"macro-hud-expedition-active\":", source, StringComparison.Ordinal);
        Assert.Contains("[WOG-EXPEDITION-RAIL-FOCUS] ui_down -> Cancel OK", source, StringComparison.Ordinal);
        Assert.Contains("[WOG-EXPEDITION-RAIL-PHASE-FOCUS] details focus preserved OK", source, StringComparison.Ordinal);
        Assert.Contains("[WOG-EXPEDITION-RAIL-DETAILS] expedition", source, StringComparison.Ordinal);
        Assert.Contains("[WOG-EXPEDITION-RAIL-CANCEL] active expedition removed OK", source, StringComparison.Ordinal);
        Assert.Contains("[WOG-EXPEDITION-RAIL-MORE] Chronicle opened OK", source, StringComparison.Ordinal);
        Assert.Contains("new InputEventMouseButton", source, StringComparison.Ordinal);
        Assert.Contains("ButtonIndex = MouseButton.Left", source, StringComparison.Ordinal);
        Assert.Contains("DisplayServer.WindowGetSize()", source, StringComparison.Ordinal);
        Assert.Contains("logicalPosition * windowScale", source, StringComparison.Ordinal);
    }

    [Fact]
    public void CitySummary_UsesExistingHudPrimitivesAndSnapshotOnlyState()
    {
        string source = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(), "game", "scripts", "CitySummaryPanel.cs"));
        string itemSource = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(), "game", "scripts", "Ui", "ConstructionQueueItem.cs"));

        Assert.Contains("CollapsiblePanelHeader", source, StringComparison.Ordinal);
        Assert.Contains("HudSectionHeader", source, StringComparison.Ordinal);
        Assert.Contains("HudMetricRow", source, StringComparison.Ordinal);
        Assert.Contains("HudResourceRow", source, StringComparison.Ordinal);
        Assert.Contains("HudProgressBar", source, StringComparison.Ordinal);
        Assert.Contains("ThemeTypeVariation = \"HudSeparator\"", source, StringComparison.Ordinal);
        Assert.Contains("GetCityStatusSnapshot", source, StringComparison.Ordinal);
        Assert.Contains("snapshot.Resources", source, StringComparison.Ordinal);
        Assert.Contains("snapshot.Projects", source, StringComparison.Ordinal);
        Assert.Contains("_body.Visible = expanded", source, StringComparison.Ordinal);
        Assert.DoesNotContain("WorldSave", source, StringComparison.Ordinal);
        Assert.DoesNotMatch(@"override\s+void\s+_Process\s*\(", source);
        Assert.DoesNotContain("DisplayServer.WindowGetSize", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Happiness", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Order", source, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("project.StopCause", itemSource, StringComparison.Ordinal);
        Assert.Contains("HudProgressBar", itemSource, StringComparison.Ordinal);
        Assert.DoesNotContain("ETA", itemSource, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CitySummary_StatusSection_ReadsAuthoritativeFields()
    {
        string source = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(), "game", "scripts", "CitySummaryPanel.cs"));

        // Every metric in the STATUS section must be sourced from an
        // authoritative CityStatusSnapshot field — never invented.
        Assert.Contains("snapshot.FoodHorizonDays", source, StringComparison.Ordinal);
        Assert.Contains("snapshot.CitizensAtWork", source, StringComparison.Ordinal);
        Assert.Contains("snapshot.CitizensAtHome", source, StringComparison.Ordinal);
        Assert.Contains("snapshot.TicksUntilFirstHarvest", source, StringComparison.Ordinal);
        Assert.Contains("snapshot.IsLaborTime", source, StringComparison.Ordinal);
        // Harvest format goes through UiText.Format, not raw interpolation.
        Assert.Contains("ui.city_summary.harvest_format", source, StringComparison.Ordinal);
        Assert.Contains("ui.city_summary.food_horizon_format", source, StringComparison.Ordinal);
        // Warnings stay on the glyph channel — never on colour alone.
        Assert.Contains("IconPaths.Warning", source, StringComparison.Ordinal);
    }

    [Fact]
    public void CitySummary_WarningsAreDefensivelyThresholded()
    {
        string source = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(), "game", "scripts", "CitySummaryPanel.cs"));

        // Only two warning thresholds are allowed in the panel: food
        // exhaustion and harvest missing that threshold. Anything else
        // would be inventing a UI-only threshold that domain logic does
        // not justify.
        Assert.Contains("FoodHorizonDays < 1", source, StringComparison.Ordinal);
        Assert.Contains("HarvestIsLate", source, StringComparison.Ordinal);
        Assert.Contains("ticks > foodRunsOutAt", source, StringComparison.Ordinal);
        // Housing-bar shows capacity but does NOT carry a warning
        // glyph — there is no defensible domain rule for the threshold.
        Assert.DoesNotContain("HousingFullGlyph", source, StringComparison.Ordinal);
        Assert.DoesNotContain("HudMetricRow(... Warning",
            source, StringComparison.Ordinal);
    }

    [Fact]
    public void CitySummary_ResourceSequence_PrioritisesSurvivalThenConstruction()
    {
        // The summary panel and the top-bar ticker share a single priority
        // sequence in `ResourcePriority`. Asserting the source order
        // against the panel alone would let the two drift apart — the
        // shared helper is what the brief asked for.
        string root = TestHelpers.FindRepositoryRoot();
        string source = File.ReadAllText(Path.Combine(
            root, "game", "scripts", "Ui", "ResourcePriority.cs"));

        int foodIndex = source.IndexOf("ResourceType.Food", StringComparison.Ordinal);
        int wildFoodIndex = source.IndexOf("ResourceType.WildFood", StringComparison.Ordinal);
        int woodIndex = source.IndexOf("ResourceType.Wood", StringComparison.Ordinal);
        int ironIndex = source.IndexOf("ResourceType.Iron", StringComparison.Ordinal);
        Assert.True(foodIndex > 0);
        Assert.True(woodIndex > foodIndex, "Wood must come after food in the survival→construction sequence.");
        Assert.True(ironIndex > woodIndex, "Iron (remaining) must come after construction resources.");
        Assert.True(wildFoodIndex > 0);
        // The summary panel must not have re-introduced a private copy of
        // the sequence — the whole point of the shared helper is one
        // canonical source.
        string summary = File.ReadAllText(Path.Combine(
            root, "game", "scripts", "CitySummaryPanel.cs"));
        Assert.DoesNotContain(
            "private static readonly ResourceType[] ResourceSequence",
            summary,
            StringComparison.Ordinal);
        Assert.Contains("ResourcePriority.Prioritize", summary, StringComparison.Ordinal);
        // No synthetic production-rate delta column appears in the panel.
        Assert.DoesNotContain("HudResourceRow(... \"+", summary, StringComparison.Ordinal);
        Assert.DoesNotContain("SetValues(", summary, StringComparison.Ordinal);
    }

    [Fact]
    public void CitySummary_RefreshesOnLocaleChangedWithoutWaitingForSimulation()
    {
        string source = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(), "game", "scripts", "CitySummaryPanel.cs"));

        // Hot-locale support: subscribe in _Ready, unsubscribe in _ExitTree,
        // and Refresh immediately on the event.
        Assert.Contains("_localeManager.LocaleChanged += OnLocaleChanged", source, StringComparison.Ordinal);
        Assert.Contains("_localeManager.LocaleChanged -= OnLocaleChanged", source, StringComparison.Ordinal);
        Assert.Contains("private void OnLocaleChanged(string _) => Refresh(_controller.GetCityStatusSnapshot());",
            source, StringComparison.Ordinal);
    }

    [Fact]
    public void ConstructionQueueItem_LocalizesEveryStopCause()
    {
        string source = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(), "game", "scripts", "Ui", "ConstructionQueueItem.cs"));

        // The original StatusText mixed UiText.Get calls with raw English
        // strings. Every stop-cause row must now route through UiText.
        Assert.Contains("ui.city_summary.paused", source, StringComparison.Ordinal);
        Assert.Contains("ui.city_summary.waiting_contributors", source, StringComparison.Ordinal);
        Assert.Contains("ui.city_summary.contributor_travelling", source, StringComparison.Ordinal);
        Assert.Contains("ui.city_summary.contributors_exhausted", source, StringComparison.Ordinal);
        Assert.Contains("ui.city_summary.resting_night", source, StringComparison.Ordinal);
        Assert.Contains("ui.city_summary.completed", source, StringComparison.Ordinal);
        Assert.Contains("ui.city_summary.awaiting_module", source, StringComparison.Ordinal);
        Assert.Contains("ui.city_summary.no_hero", source, StringComparison.Ordinal);
        // No raw English stop-cause text survives.
        Assert.DoesNotContain("Paused by the player", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Waiting for contributors", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Waiting: contributors exhausted", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Resting during the night", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Contributor travelling to the site", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Awaiting next Founding Site module", source, StringComparison.Ordinal);
        Assert.DoesNotContain("No hero available", source, StringComparison.Ordinal);
    }

    [Fact]
    public void CitySummary_LocaleCatalog_ExposesNewStatusKeysInBothLanguages()
    {
        string root = TestHelpers.FindRepositoryRoot();
        string en = File.ReadAllText(Path.Combine(root, "game", "locale", "en.po"));
        string es = File.ReadAllText(Path.Combine(root, "game", "locale", "es.po"));

        // Every new key added in this pass must appear in both catalogs
        // — a one-sided translation would let the EN-default fallback hide
        // the gap at runtime.
        string[] keys =
        {
            "ui.city_summary.status_food_horizon",
            "ui.city_summary.status_citizens_work",
            "ui.city_summary.status_citizens_home",
            "ui.city_summary.status_next_harvest",
            "ui.city_summary.status_labor",
            "ui.city_summary.labor_active",
            "ui.city_summary.labor_paused",
            "ui.city_summary.food_horizon_format",
            "ui.city_summary.harvest_format",
            "ui.city_summary.no_next_harvest",
            "ui.city_summary.tooltip_food_critical",
            "ui.city_summary.tooltip_harvest_late",
            "ui.city_summary.tooltip_housing_full",
            "ui.city_summary.paused",
            "ui.city_summary.waiting_contributors",
            "ui.city_summary.contributor_travelling",
            "ui.city_summary.contributors_exhausted",
            "ui.city_summary.resting_night",
            "ui.city_summary.completed",
            "ui.city_summary.awaiting_module",
            "ui.city_summary.no_hero",
        };
        foreach (string key in keys)
        {
            Assert.Contains($"msgid \"{key}\"", en, StringComparison.Ordinal);
            Assert.Contains($"msgid \"{key}\"", es, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void PrimaryNavDock_NoLongerOwnsTheMenuButton()
    {
        string root = TestHelpers.FindRepositoryRoot();
        string source = File.ReadAllText(Path.Combine(
            root, "game", "scripts", "Ui", "PrimaryNavDock.cs"));
        string[] sceneLines = ReadScene();

        // The dock is no longer the menu's owner; the menu lives on the
        // right-edge utility cluster of the top status bar.
        Assert.DoesNotContain("public IconButton MenuButton", source, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "RequireButton(\"GameMenuButton\")", source, StringComparison.Ordinal);
        Assert.DoesNotContain(
            sceneLines,
            line => line.Contains("GameMenuButton", StringComparison.Ordinal));
    }

    [Fact]
    public void CityStatusPanel_ExposesUtilityClusterWithCameraAndMenuIconButtons()
    {
        string root = TestHelpers.FindRepositoryRoot();
        string source = File.ReadAllText(Path.Combine(
            root, "game", "scripts", "CityStatusPanel.cs"));

        // The utility cluster lives inside the row, persists across Refresh,
        // and exposes two icon-only IconButtons by typed accessor.
        Assert.Contains("Name = \"UtilityCluster\"", source, StringComparison.Ordinal);
        Assert.Contains("public IconButton CameraButton", source, StringComparison.Ordinal);
        Assert.Contains("public IconButton MenuButton", source, StringComparison.Ordinal);
        Assert.Contains("SizeFlagsHorizontal = SizeFlags.ShrinkEnd", source, StringComparison.Ordinal);
        // Two icon-only buttons inside the cluster: ShowLabel = false must
        // appear at least twice (Camera + Menu).
        int showLabelFalse = Regex.Matches(source, "ShowLabel = false").Count;
        Assert.True(
            showLabelFalse >= 2,
            $"Expected at least two ShowLabel = false sites in CityStatusPanel.cs; found {showLabelFalse}.");
        // The status bar never gains a PlayPauseButton or SpeedButton —
        // those still belong to SimulationControls.
        Assert.DoesNotContain("PlayPauseButton", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SpeedButton", source, StringComparison.Ordinal);
    }

    [Fact]
    public void PauseMenu_OpenButtonPath_RedirectsToUtilityClusterMenu()
    {
        string root = TestHelpers.FindRepositoryRoot();
        string[] sceneLines = ReadScene();

        // The PauseMenu node in CityPrototype.tscn sets open_button_path
        // explicitly so the redirect is discoverable in the scene file.
        int pauseNode = IndexOfNodeHeader(sceneLines, "PauseMenu");
        Assert.True(pauseNode >= 0, "Could not locate PauseMenu node in CityPrototype.tscn.");
        int end = IndexOfNextNodeHeader(sceneLines, pauseNode + 1);
        int blockEnd = end < 0 ? sceneLines.Length : end;
        string block = string.Join(
            "\n", sceneLines[pauseNode..blockEnd]);
        Assert.Contains(
            "open_button_path = NodePath(\"../GameUiShell/CityStatusPanel/SafeArea/StatusComposition/UtilityCluster/MenuButton\")",
            block,
            StringComparison.Ordinal);
    }

    [Fact]
    public void SimulationControls_DropsTheCameraIconButton()
    {
        string root = TestHelpers.FindRepositoryRoot();
        string source = File.ReadAllText(Path.Combine(
            root, "game", "scripts", "SimulationControls.cs"));

        Assert.DoesNotContain("new IconButton", source, StringComparison.Ordinal);
        Assert.DoesNotContain("_cameraButton", source, StringComparison.Ordinal);
        Assert.DoesNotContain("public IconButton CameraButton", source, StringComparison.Ordinal);
        // IconPaths.Camera is no longer a consumer of this script.
        Assert.DoesNotContain("IconPaths.Camera", source, StringComparison.Ordinal);
    }

    [Fact]
    public void CityStatusPanel_ExposesTheResourceOverflowAffordance()
    {
        // The ticker used to clip silently. The iconography / scalability
        // pass replaces that with a deterministic visible cap plus a "+N"
        // chip whose tooltip lists every hidden resource by name and
        // exact amount. The structural contract is a constant on the
        // script and a dedicated chip builder.
        string root = TestHelpers.FindRepositoryRoot();
        string source = File.ReadAllText(Path.Combine(
            root, "game", "scripts", "CityStatusPanel.cs"));

        Assert.Contains("MaxVisibleResourceChips", source, StringComparison.Ordinal);
        Assert.Contains("BuildResourceOverflowChip", source, StringComparison.Ordinal);
        Assert.Contains("BuildOverflowTooltip", source, StringComparison.Ordinal);
        Assert.Contains("ui.status.resource_overflow_label", source, StringComparison.Ordinal);
        Assert.Contains("ui.status.resource_overflow_line", source, StringComparison.Ordinal);
        // The ticker no longer clips silently: it routes through the
        // shared priority order and the shared compact formatter.
        Assert.Contains("ResourcePriority.Prioritize", source, StringComparison.Ordinal);
        Assert.Contains("CompactNumber.Format", source, StringComparison.Ordinal);
        // Tooltips keep the exact amount even when the chip shows the
        // compact form.
        Assert.Contains("CompactNumber.FormatExact", source, StringComparison.Ordinal);
    }

    [Fact]
    public void CityStatusPanel_OverflowKeysExistInBothLocales()
    {
        string root = TestHelpers.FindRepositoryRoot();
        string en = File.ReadAllText(Path.Combine(root, "game", "locale", "en.po"));
        string es = File.ReadAllText(Path.Combine(root, "game", "locale", "es.po"));

        // A one-sided translation would let the EN fallback hide the
        // gap at runtime; both catalogs must carry the overflow keys.
        foreach (string key in new[]
                 {
                     "ui.status.resource_overflow_label",
                     "ui.status.resource_overflow_line",
                 })
        {
            Assert.Contains($"msgid \"{key}\"", en, StringComparison.Ordinal);
            Assert.Contains($"msgid \"{key}\"", es, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ResourceIcon_CoversEveryResourceType()
    {
        // The icon-only ticker needs a distinct silhouette for every
        // ResourceType. A missing case falls through to the generic
        // fallback and turns two resources into one indistinguishable
        // icon — exactly the bug the audit caught.
        string root = TestHelpers.FindRepositoryRoot();
        string source = File.ReadAllText(Path.Combine(
            root, "game", "scripts", "Ui", "ResourceIcon.cs"));

        foreach (ResourceType resource in Enum.GetValues<ResourceType>())
        {
            Assert.Contains(
                $"case ResourceType.{resource}:",
                source,
                StringComparison.Ordinal);
        }
    }

    [Fact]
    public void CitySummaryPanel_SharesTheResourcePrioritySequence()
    {
        // The summary and the ticker must read the same order. A second
        // copy of the sequence in the summary panel would drift the
        // moment a new resource is added; the brief specifically asks
        // for a single priority.
        string root = TestHelpers.FindRepositoryRoot();
        string source = File.ReadAllText(Path.Combine(
            root, "game", "scripts", "CitySummaryPanel.cs"));

        Assert.Contains("ResourcePriority.Prioritize", source, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "private static readonly ResourceType[] ResourceSequence",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ExpeditionRail_UsesCollapsiblePanelHeader()
    {
        // The rail is now a folder: the whole strip is the CollapsiblePanelHeader,
        // a single body holds the cards and the chronicle, and the
        // ExpandedChanged event drives body visibility. Mirrors the
        // CitySummaryPanel pattern.
        string rail = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(), "game", "scripts", "ExpeditionRail.cs"));

        Assert.Contains("new CollapsiblePanelHeader", rail, StringComparison.Ordinal);
        Assert.Contains("ExpandedChanged", rail, StringComparison.Ordinal);
        Assert.Contains("_body.Visible = expanded", rail, StringComparison.Ordinal);
        Assert.Contains("public bool Expanded =>", rail, StringComparison.Ordinal);
    }

    [Fact]
    public void ExpeditionRail_DoesNotPersistCollapseState()
    {
        // The fold is ephemeral HUD state. A persistence round-trip
        // (EditorPrefs/ConfigFile/WorldSave) would silently push the
        // player's preference into a save file the player cannot reset
        // without reloading. The hard rule forbids that without a
        // documented migration.
        string rail = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(), "game", "scripts", "ExpeditionRail.cs"));

        Assert.DoesNotContain("EditorPrefs", rail, StringComparison.Ordinal);
        Assert.DoesNotContain("ConfigFile", rail, StringComparison.Ordinal);
        Assert.DoesNotContain("WorldSave", rail, StringComparison.Ordinal);
    }

    [Fact]
    public void ExpeditionCompactCard_DoesNotRenderAllMemberNamesInline()
    {
        // Member names used to be joined with ", " and printed as a
        // Caption label, which does not scale. The names now live
        // only inside the DetailsButton tooltip and inside the
        // UiText.Get("ui.expedition_rail.members_tooltip") formatter.
        // No `Label { Text = ...MemberNames }` survives.
        string card = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(),
            "game", "scripts", "Ui", "ExpeditionCompactCard.cs"));

        Assert.Contains("ui.expedition_rail.members_tooltip", card, StringComparison.Ordinal);
        Assert.Contains("TooltipText =", card, StringComparison.Ordinal);
        Assert.Contains("string.Join(\", \", item.MemberNames)", card, StringComparison.Ordinal);
        Assert.DoesNotContain("ui.expedition_rail.members.one", card, StringComparison.Ordinal);
        Assert.DoesNotContain("ui.expedition_rail.members.many", card, StringComparison.Ordinal);
    }

    [Fact]
    public void ExpeditionCompactCard_UsesPhaseStateChip()
    {
        // The phase used to be a plain HudCaption Label. It is now a
        // HudStateBadge that carries the phase glyph plus the localized
        // label — colour is no longer the only signal.
        string card = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(),
            "game", "scripts", "Ui", "ExpeditionCompactCard.cs"));

        Assert.Contains("new HudStateBadge", card, StringComparison.Ordinal);
        Assert.Contains("HudStateBadge.IconFor(item.Phase)", card, StringComparison.Ordinal);
    }

    [Fact]
    public void HudStateBadge_HasPhaseMapping()
    {
        // The phase map must lock every ExpeditionPhase to a stable
        // icon path. A missing case would fall through to Resolved,
        // collapsing two unrelated phases into one chip.
        string source = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(),
            "game", "scripts", "Ui", "HudStateBadge.cs"));

        foreach (ExpeditionPhase phase in Enum.GetValues<ExpeditionPhase>())
        {
            Assert.Contains($"ExpeditionPhase.{phase}", source, StringComparison.Ordinal);
        }
    }

    private static string[] NodeBlock(string nodeName)
    {
        string[] lines = ReadScene();
        int start = IndexOfNodeHeader(lines, nodeName);
        Assert.True(start >= 0, $"Could not locate the {nodeName} node header in CityPrototype.tscn.");

        int next = IndexOfNextNodeHeader(lines, start + 1);
        int end = next < 0 ? lines.Length : next;
        return lines[start..end];
    }

    private static string[] ReadScene() => File.ReadAllLines(ResolveScenePath());

    private static string Normalize(string source) => source.Replace("\r\n", "\n", StringComparison.Ordinal);

    private static string ResolveScenePath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            string candidate = Path.Combine(directory.FullName, "game", "scenes", "CityPrototype.tscn");
            if (File.Exists(candidate)) return candidate;
            directory = directory.Parent;
        }
        throw new FileNotFoundException("Could not locate CityPrototype.tscn.");
    }

    private static int IndexOfNodeHeader(string[] lines, string nodeName)
    {
        string prefix = $"[node name=\"{nodeName}\"";
        return Array.FindIndex(lines, line => line.StartsWith(prefix, StringComparison.Ordinal));
    }

    private static int IndexOfNextNodeHeader(string[] lines, int startIndex)
    {
        for (int i = startIndex; i < lines.Length; i++)
        {
            if (lines[i].StartsWith("[node ", StringComparison.Ordinal)) return i;
        }
        return -1;
    }

    private static readonly Regex MouseFilterPattern = new(
        @"^mouse_filter\s*=\s*(\d+)\s*$",
        RegexOptions.CultureInvariant);

    private static readonly Regex SizeLiteral = new(
        @"Vector2\(\s*(\d+(?:\.\d+)?)\s*,\s*(\d+(?:\.\d+)?)\s*\)",
        RegexOptions.CultureInvariant);
}
