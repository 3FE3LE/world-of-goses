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
        string[] action = NodeBlock("ActionDock");
        string[] inspector = NodeBlock("ContextInspector");
        string macro = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(), "game", "scripts", "Prototypes",
            "MacroStreetLiveView.cs"));
        string actionSource = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(), "game", "scripts", "Ui", "ActionDock.cs"));
        string inspectorSource = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(), "game", "scripts", "Ui", "ContextInspector.cs"));

        Assert.All(new[] { summary, rail, primary }, block =>
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
        Assert.Contains("_citySummaryPanel.Hide();", macro, StringComparison.Ordinal);
        Assert.Contains("_expeditionRail.Hide();", macro, StringComparison.Ordinal);
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
        // PlayPauseButton is gone — the world always runs.
        Assert.DoesNotContain("PlayPauseButton", source, StringComparison.Ordinal);
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
        Assert.Contains(rail, line => line.Trim() == "anchor_right = 1.0");
        // The rail spans from the parent's top edge to the bottom edge,
        // mirroring the city-summary panel on the opposite side: anchored
        // to both top and bottom so the chronicle's scroll has a real
        // height to fill. Without anchor_bottom the rect collapses to a
        // 0-px strip and only the collapsible header renders — exactly
        // what shipped before this guard existed.
        Assert.Contains(rail, line => line.Trim() == "anchor_bottom = 1.0");
        Assert.Contains(rail, line => line.Trim() == "offset_left = -244.0");
        Assert.Contains(rail, line => line.Trim() == "offset_right = -8.0");
        Assert.Contains(rail, line => line.Trim() == "offset_top = 8.0");
        Assert.Contains(rail, line => line.Trim() == "offset_bottom = -8.0");
        Assert.DoesNotContain(rail, line => line.Trim() == "offset_bottom = 8.0");
        Assert.DoesNotContain(rail, line => line.Trim() == "offset_bottom = -104.0");
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
    public void SpeedControl_MovedIntoTheStatusBarUtilityCluster()
    {
        // The bottom-right SimulationControls surface is gone. The speed
        // control lives in the CityStatusPanel utility cluster alongside
        // Camera and Menu; the play/pause button is gone entirely.
        string root = TestHelpers.FindRepositoryRoot();
        Assert.False(
            File.Exists(Path.Combine(root, "game", "scripts", "SimulationControls.cs")),
            "SimulationControls.cs is gone; speed control lives in the status bar.");
        Assert.False(
            File.Exists(Path.Combine(root, "game", "scripts", "PlayPauseButton.cs")),
            "PlayPauseButton.cs is gone; the world always runs.");

        string[] scene = File.ReadAllLines(Path.Combine(root, "game", "scenes", "CityPrototype.tscn"));
        Assert.DoesNotContain(scene, line => line.Contains("SimulationControls"));
        Assert.DoesNotContain(scene, line => line.Contains("PlayPauseButton"));

        string statusSource = File.ReadAllText(Path.Combine(
            root, "game", "scripts", "CityStatusPanel.cs"));
        Assert.Contains("new SpeedButton", statusSource, StringComparison.Ordinal);
        Assert.Contains("public SpeedButton SpeedButton", statusSource, StringComparison.Ordinal);
    }

    [Fact]
    public void CityWorldController_NoLongerExposesPause()
    {
        // Pause is no longer a possible speed choice; the simulation
        // always runs. ToggleSimulationPause and the SpeedChoice.Paused
        // sentinel are gone.
        string source = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(),
            "game", "scripts", "CityWorldController.cs"));

        Assert.DoesNotContain("ToggleSimulationPause", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SpeedChoice.Paused", source, StringComparison.Ordinal);
        Assert.DoesNotContain("_lastRunningSpeed", source, StringComparison.Ordinal);
        Assert.DoesNotContain("LastRunningSpeed", source, StringComparison.Ordinal);
        Assert.Contains("public enum SpeedChoice", source, StringComparison.Ordinal);
        Assert.Contains("Normal = 1", source, StringComparison.Ordinal);
        Assert.Contains("Fast = 2", source, StringComparison.Ordinal);
        Assert.Contains("Fastest = 4", source, StringComparison.Ordinal);
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
        // The chronicle now owns a collapsible header that governs the
        // body, mirroring CitySummaryPanel — there is no longer an
        // internal compact/full split; the rows-scroll caps the height
        // when the body unfolds.
        Assert.Contains("CollapsiblePanelHeader", source, StringComparison.Ordinal);
        Assert.DoesNotContain("CompactRows", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ToggleButton", source, StringComparison.Ordinal);
        Assert.DoesNotContain("public bool Slim", source, StringComparison.Ordinal);
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
        Assert.Contains("[WOG-EXPEDITION-RAIL-FOCUS] View -> ui_down -> Details OK", source,
            StringComparison.Ordinal);
        Assert.Contains(
            "[WOG-EXPEDITION-RAIL-EXPAND] real header click revealed VER and the active card OK",
            source,
            StringComparison.Ordinal);
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
        Assert.Contains("public SpeedButton SpeedButton", source, StringComparison.Ordinal);
        Assert.Contains("SizeFlagsHorizontal = SizeFlags.ShrinkEnd", source, StringComparison.Ordinal);
        // Two icon-only buttons inside the cluster: ShowLabel = false must
        // appear at least twice (Camera + Menu). SpeedButton uses its own
        // play-icon stack rather than a ShowLabel toggle.
        int showLabelFalse = Regex.Matches(source, "ShowLabel = false").Count;
        Assert.True(
            showLabelFalse >= 2,
            $"Expected at least two ShowLabel = false sites in CityStatusPanel.cs; found {showLabelFalse}.");
        // PlayPauseButton is gone — the world always runs.
        Assert.DoesNotContain("PlayPauseButton", source, StringComparison.Ordinal);
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
    public void SpeedButton_CentresItsIconsOnTheButton()
    {
        // The previous version used a centred nested FullRect container
        // that left asymmetric visual padding. The current build sizes
        // the button to its content and applies equal left/right margins
        // so the play-icon stack sits on the geometric centre.
        string source = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(), "game", "scripts", "SpeedButton.cs"));

        Assert.Contains("CustomMinimumSize = new Vector2(ButtonWidth, ButtonHeight)",
            source, StringComparison.Ordinal);
        Assert.Contains(
            "_container.AddThemeConstantOverride(\"margin_left\", IconPadding);",
            source, StringComparison.Ordinal);
        Assert.Contains(
            "_container.AddThemeConstantOverride(\"margin_right\", IconPadding);",
            source, StringComparison.Ordinal);
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
    public void ExpeditionRail_CollapsibleHeaderFoldsToSlimResumeOfTheToggles()
    {
        // The rail's CollapsiblePanelHeader governs the whole body:
        // collapsing the rail hides the expedition section AND the
        // scroll AND collapses the chronicle, so only the rail header
        // and the chronicle header stay visible — a slim resume of the
        // toggles that still make sense while the rail is folded.
        string rail = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(), "game", "scripts", "ExpeditionRail.cs"));
        string chronicle = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(), "game", "scripts", "Ui", "ChroniclePanel.cs"));

        Assert.Contains("new CollapsiblePanelHeader", rail, StringComparison.Ordinal);
        Assert.Contains("ExpandedChanged", rail, StringComparison.Ordinal);
        Assert.Contains(
            "_expeditionSection.Visible = expanded", rail, StringComparison.Ordinal);
        Assert.Contains("_scroll.Visible = expanded", rail, StringComparison.Ordinal);
        Assert.Contains("public bool Expanded =>", rail, StringComparison.Ordinal);
        // Accordion: when either header expands, the other surface
        // folds out so only one occupies the rail's column at a time.
        // Expanding the rail collapses the chronicle body; expanding
        // the chronicle hides the expedition scroll. This is the only
        // way to keep both surfaces fitting without overlap given the
        // shared column.
        Assert.Contains("if (expanded)", rail, StringComparison.Ordinal);
        Assert.Contains("_chronicle.Expanded = false", rail, StringComparison.Ordinal);
        Assert.Contains("_expeditionSection.Visible = false", rail, StringComparison.Ordinal);
        Assert.Contains("_scroll.Visible = false", rail, StringComparison.Ordinal);
        Assert.Contains("_header.Expanded = false", rail, StringComparison.Ordinal);
        // The chronicle is added as a direct child of the layout
        // (outside the rail's scroll), not inside _content. Putting it
        // inside the scroll would hide it whenever the rail collapses.
        Assert.Contains("layout.AddChild(_chronicle)", rail, StringComparison.Ordinal);
        Assert.Contains("_layout.AddChild(_chronicle)", rail, StringComparison.Ordinal);
        Assert.DoesNotContain("_content.AddChild(_chronicle)", rail, StringComparison.Ordinal);
        // The rail's layout must re-sort on the next frame so the body
        // actually shrinks when the chronicle hides — without this
        // deferred QueueSort, the rail would stay at its expanded
        // rect with the body simply hidden underneath, and re-expanding
        // the chronicle would overflow the rail and overlap the scroll.
        Assert.Contains("_layout.QueueSort", rail, StringComparison.Ordinal);
        Assert.Contains("_layout.UpdateMinimumSize", rail, StringComparison.Ordinal);
        Assert.Contains("_scroll.ResetSize", rail, StringComparison.Ordinal);
        Assert.Contains("_chronicle.QueueSort", rail, StringComparison.Ordinal);
        // Both header sources must trigger the same layout refresh:
        // rail header for the slim-resume cycle, chronicle header for
        // folding just the chronicle body while the rail stays open.
        Assert.Contains(
            "OnChronicleExpanded(bool expanded)",
            rail, StringComparison.Ordinal);
        Assert.Contains(
            "RequestRailRelayout",
            rail, StringComparison.Ordinal);
        // Chronicle folds with the rail. The chronicle is exposed via
        // its own collapsible header so the rail-level "more" button
        // falls back to that header — clicking it expands the
        // chronicle body exactly like CitySummaryPanel's header.
        Assert.Contains("public CollapsiblePanelHeader Header => _header;",
            chronicle, StringComparison.Ordinal);
        Assert.Contains("MoreButton => _chronicle.Header", rail, StringComparison.Ordinal);
        // Accordion initial state: the chronicle starts collapsed so
        // the expedition rail is the initial protagonist.
        Assert.Contains(
            "new CollapsiblePanelHeader(\n            UiText.Get(\"ui.expedition_rail.activity\"),\n            expanded: false)",
            chronicle, StringComparison.Ordinal);
        Assert.Contains("Visible = false", chronicle, StringComparison.Ordinal);
        // The chronicle flips between ExpandFill (protagonist — body
        // visible, fills the rail column) and ShrinkBegin (collapsed —
        // just the header). When both flags are gone the chronicle
        // stays stuck at the wrong vertical size on toggle.
        Assert.Contains("SizeFlagsVertical = SizeFlags.ShrinkBegin;",
            chronicle, StringComparison.Ordinal);
        Assert.Contains(
            "SizeFlagsVertical = expanded\n            ? SizeFlags.ExpandFill\n            : SizeFlags.ShrinkBegin;",
            chronicle, StringComparison.Ordinal);
        // The rail header counts ONLY active expeditions, never falls
        // back to chronicle events. The two badges must not share a
        // counter or the user confuses which surface they are reading.
        Assert.Contains(
            "int headerCount = _snapshot.ActiveExpeditions.Count;",
            rail, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "ChronicleEventProjection.MeaningfulEvents(_snapshot.Events).Count",
            rail, StringComparison.Ordinal);
        // The chronicle must collapse the moment its own header
        // toggles, so the body is hidden via _body.Visible — the
        // same affordance CitySummaryPanel uses.
        Assert.Contains("_body.Visible = expanded", chronicle, StringComparison.Ordinal);
        // The chronicle's own scroll lives outside the rail scroll, so
        // the rail's _Input must not steal wheel events that land over
        // it — otherwise the rail would scroll the expedition section
        // instead of the chronicle rows the pointer is over.
        Assert.Contains(
            "_chronicle.GetGlobalRect().HasPoint(mouse.GlobalPosition)",
            rail, StringComparison.Ordinal);
        // The body must cap at MaxHeight so a long event history
        // actually scrolls instead of growing the chronicle off-screen.
        // Anchoring top and bottom of the body to the same value with
        // offset_bottom = cap pins the rect — CustomMinimumSize alone
        // is a floor, not a ceiling. Single scroll, not nested.
        Assert.Contains("SetAnchorsAndOffsetsPreset(LayoutPreset.TopWide)",
            chronicle, StringComparison.Ordinal);
        Assert.Contains("_body.OffsetBottom = MaxHeight",
            chronicle, StringComparison.Ordinal);
        Assert.DoesNotContain("_rowsScroll", chronicle, StringComparison.Ordinal);
        // The chronicle must drive the VScrollBar on wheel input —
        // ScrollContainer does not auto-scroll, so the chronicle needs
        // its own ScrollBy that mirrors the rail's wheel handler.
        Assert.Contains("ScrollBy(mouse);", chronicle, StringComparison.Ordinal);
        Assert.Contains("bar.Value += direction * 40d",
            chronicle, StringComparison.Ordinal);
        // The chronicle pins a minimum width (matching the rail's
        // PanelWidth) so it cannot shrink to the chevron+title strip
        // when the body hides — the same trick CitySummaryPanel uses.
        Assert.Contains("CustomMinimumSize = new Vector2(MinWidth, 0)",
            chronicle, StringComparison.Ordinal);
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
    public void PauseMenu_DoesNotDoubleSubscribeTheOpenButton()
    {
        // The open button lives in the city-status utility cluster and
        // is also reached by the macro view's primary navigation. Both
        // call sites used to subscribe `_openButton.Pressed += Toggle`
        // independently, which fired Toggle twice on every click and
        // left the menu closed (open → close). The macro view owns the
        // click now; PauseMenu must NOT also subscribe.
        string pauseSource = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(), "game", "scripts", "PauseMenu.cs"));
        string macroSource = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(), "game", "scripts", "Prototypes",
            "MacroStreetLiveView.cs"));

        Assert.DoesNotContain("_openButton.Pressed += Toggle", pauseSource,
            StringComparison.Ordinal);
        Assert.Contains(
            "_statusPanel.MenuButton.Pressed += OnUtilityClusterMenuPressed",
            macroSource, StringComparison.Ordinal);
        // The ESC path still opens the menu from _UnhandledInput.
        Assert.Contains("if (!@event.IsActionPressed(\"ui_cancel\")) return;",
            pauseSource, StringComparison.Ordinal);
    }

    [Fact]
    public void PauseMenu_OpensOnUiCancelWhenHiddenAndInMacroView()
    {
        // ESC opens the pause menu when nothing else is open AND the
        // controller is on the macro view. If the player is in a hero
        // profile or building detail, PauseMenu deliberately lets the
        // input propagate so CityPrototype can return them to the city.
        string pauseSource = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(), "game", "scripts", "PauseMenu.cs"));
        string protoSource = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(), "game", "scripts", "CityPrototype.cs"));

        // PauseMenu's _UnhandledInput branches on Visible and on the
        // controller selection. Assert the structural pattern.
        Assert.Contains(
            "_controller.CurrentSelection != CityWorldController.Selection.MacroView",
            pauseSource, StringComparison.Ordinal);
        Assert.Contains("Open();", pauseSource, StringComparison.Ordinal);
        // CityPrototype's _UnhandledInput only returns to the city when
        // the selection is NOT the macro view (PauseMenu has already
        // eaten the input otherwise).
        Assert.Contains(
            "if (controller.CurrentSelection == CityWorldController.Selection.MacroView) return;",
            protoSource, StringComparison.Ordinal);
    }

    [Fact]
    public void PrimaryNavDockHandlers_CloseTheCurrentModalBeforeOpeningAnother()
    {
        // Three of the four dock handlers (OnExpeditionMenuPressed,
        // OnPoliciesPressed, OnCitizensPressed) used to skip the
        // close-first branch that OnConstructionMenuPressed had,
        // letting two modals stack when the player clicked a
        // different dock button while one was open.
        string source = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(),
            "game", "scripts", "Prototypes", "MacroStreetLiveView.cs"));

        // Each of the four dock handlers must call _modalHost.Close() when a
        // modal is already open, then open its own panel in the else
        // branch. We verify each handler body by looking for the
        // close-then-open pattern around its panel name.
        string[] handlers = { "ConstructionMenu", "ExpeditionMenu", "Policies", "Citizens" };
        foreach (string handler in handlers)
        {
            int handlerStart = source.IndexOf(
                $"private void On{handler}Pressed()", StringComparison.Ordinal);
            Assert.True(
                handlerStart >= 0,
                $"Could not locate On{handler}Pressed() in MacroStreetLiveView.cs.");

            // Find the end of this handler (next "private void On" or end of class).
            int handlerEnd = source.IndexOf(
                "private void On", handlerStart + 1, StringComparison.Ordinal);
            string body = handlerEnd < 0
                ? source[handlerStart..]
                : source[handlerStart..handlerEnd];

            Assert.Contains("_modalHost.IsOpen", body, StringComparison.Ordinal);
            Assert.Contains("_modalHost.Close()", body, StringComparison.Ordinal);
        }

        // The construction handler additionally calls _modalHost.Open
        // with the panel as a parameter; the others call their own
        // panel's Open() method. Both shapes count as "open the
        // panel" — the close-before-open contract is what matters.
        Assert.Contains("_modalHost.Open(_constructionPanel)", source, StringComparison.Ordinal);
        Assert.Contains("_expeditionPanel.Open()", source, StringComparison.Ordinal);
        Assert.Contains("_policiesPanel.Open()", source, StringComparison.Ordinal);
        Assert.Contains("_citizensPanel.Open()", source, StringComparison.Ordinal);
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

    [Fact]
    public void OctagonalSkillSlot_IsARealEightSidedPackedComponent()
    {
        string root = TestHelpers.FindRepositoryRoot();
        string source = File.ReadAllText(Path.Combine(
            root, "game", "scripts", "Ui", "OctagonalSkillSlot.cs"));
        string scene = File.ReadAllText(Path.Combine(
            root, "game", "scenes", "Components", "OctagonalSkillSlot.tscn"));

        Assert.Contains("public const int SlotWidth = 104", source, StringComparison.Ordinal);
        Assert.Contains("public const int SlotHeight = 164", source, StringComparison.Ordinal);
        Assert.Contains("private const int CornerCut = 20", source, StringComparison.Ordinal);
        Assert.Equal(8, Regex.Matches(source, "^        new\\(", RegexOptions.Multiline).Count);
        Assert.Contains("DrawColoredPolygon(Octagon, fill)", source, StringComparison.Ordinal);
        Assert.Contains("antialiased: false", source, StringComparison.Ordinal);
        Assert.Contains("custom_minimum_size = Vector2(104, 164)", scene, StringComparison.Ordinal);
        Assert.Contains("theme_type_variation = &\"OctagonalSkillSlot\"", scene,
            StringComparison.Ordinal);
        Assert.DoesNotContain("_Process", source, StringComparison.Ordinal);
        Assert.DoesNotContain("TraitDefinition", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooltip", source, StringComparison.Ordinal);
    }

    [Fact]
    public void OctagonalSkillSlot_ReservesEightInvisibleStableTraitSides()
    {
        string scene = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(),
            "game", "scenes", "Components", "OctagonalSkillSlot.tscn"));
        (int X, int Y)[] positions =
        {
            (49, 1), (87, 11), (97, 79), (87, 147),
            (49, 157), (11, 147), (1, 79), (11, 11),
        };

        for (int i = 0; i < positions.Length; i++)
        {
            string header = $"[node name=\"TraitSide{i}\" type=\"Control\" parent=\".\"]";
            int start = scene.IndexOf(header, StringComparison.Ordinal);
            Assert.True(start >= 0, $"Missing independent TraitSide{i} anchor.");
            int next = scene.IndexOf("[node ", start + header.Length, StringComparison.Ordinal);
            string block = next < 0 ? scene[start..] : scene[start..next];
            Assert.Contains("visible = false", block, StringComparison.Ordinal);
            Assert.Contains($"offset_left = {positions[i].X}.0", block, StringComparison.Ordinal);
            Assert.Contains($"offset_top = {positions[i].Y}.0", block, StringComparison.Ordinal);
            Assert.DoesNotContain("tooltip", block, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void ExpeditionStrips_AlwaysAuthorFourPresentationOnlySlots()
    {
        string root = TestHelpers.FindRepositoryRoot();
        string skillScene = File.ReadAllText(Path.Combine(
            root, "game", "scenes", "Components", "ExpeditionSkillStrip.tscn"));
        string squadScene = File.ReadAllText(Path.Combine(
            root, "game", "scenes", "Components", "ExpeditionSquadStrip.tscn"));
        string skillSource = File.ReadAllText(Path.Combine(
            root, "game", "scripts", "Ui", "ExpeditionSkillStrip.cs"));
        string squadSource = File.ReadAllText(Path.Combine(
            root, "game", "scripts", "Ui", "ExpeditionSquadStrip.cs"));

        Assert.Equal(4, Regex.Matches(skillScene, "name=\\\"Slot[1-4]\\\"").Count);
        Assert.Equal(4, Regex.Matches(squadScene, "name=\\\"Slot[1-4]\\\"").Count);
        Assert.Contains("SlotState.Ready", skillSource, StringComparison.Ordinal);
        Assert.Contains("SlotState.Locked", skillSource, StringComparison.Ordinal);
        Assert.Contains("ConfigureFounderFixture", squadSource, StringComparison.Ordinal);
        Assert.Contains("FocusNeighborLeft", skillSource, StringComparison.Ordinal);
        Assert.Contains("FocusNeighborRight", skillSource, StringComparison.Ordinal);
        Assert.Contains("FocusNeighborLeft", squadSource, StringComparison.Ordinal);
        Assert.Contains("FocusNeighborRight", squadSource, StringComparison.Ordinal);
        Assert.DoesNotContain("using WorldofGoses.Domain", skillSource, StringComparison.Ordinal);
        Assert.DoesNotContain("using WorldofGoses.Domain", squadSource, StringComparison.Ordinal);
        Assert.DoesNotContain("_Process", skillSource, StringComparison.Ordinal);
        Assert.DoesNotContain("_Process", squadSource, StringComparison.Ordinal);
    }

    [Fact]
    public void ExpeditionComponentShowcase_CoversStatesCooldownAndBothFocusFixtures()
    {
        string root = TestHelpers.FindRepositoryRoot();
        string source = File.ReadAllText(Path.Combine(
            root, "game", "scripts", "Prototypes", "ExpeditionHudComponentShowcase.cs"));
        string scenePath = Path.Combine(
            root, "game", "scenes", "prototypes", "ExpeditionHudComponentShowcase.tscn");
        string theme = File.ReadAllText(Path.Combine(
            root, "game", "assets", "ui", "default_theme.tres"));

        Assert.True(File.Exists(scenePath));
        foreach (string state in new[] { "Empty", "Locked", "Ready", "Cooldown", "Disabled" })
        {
            Assert.Contains($"SlotState.{state}", source, StringComparison.Ordinal);
        }
        Assert.Contains("cooldownRemaining", source, StringComparison.Ordinal);
        Assert.Contains("expedition-components-focus-keyboard", source, StringComparison.Ordinal);
        Assert.Contains("expedition-components-focus-gamepad", source, StringComparison.Ordinal);
        Assert.Contains("InputEventKey", source, StringComparison.Ordinal);
        Assert.Contains("InputEventJoypadButton", source, StringComparison.Ordinal);
        Assert.Contains("OctagonalSkillSlot/colors/fill_ready", theme, StringComparison.Ordinal);
        Assert.Contains("OctagonalSkillSlot/colors/border_ready", theme, StringComparison.Ordinal);
    }

    [Fact]
    public void ExpeditionLiveView_IsAScreenContentPerspectiveWithoutASecondClock()
    {
        string root = TestHelpers.FindRepositoryRoot();
        string cityScene = File.ReadAllText(Path.Combine(
            root, "game", "scenes", "CityPrototype.tscn"));
        string liveScene = File.ReadAllText(Path.Combine(
            root, "game", "scenes", "expeditions", "ExpeditionLiveView.tscn"));
        string liveSource = File.ReadAllText(Path.Combine(
            root, "game", "scripts", "ExpeditionLiveView.cs"));
        string stageSource = File.ReadAllText(Path.Combine(
            root, "game", "scripts", "Ui", "ExpeditionStage.cs"));

        Assert.Contains(
            "[node name=\"ExpeditionLiveView\" parent=\"GameUiShell/ScreenContent\"",
            cityScene,
            StringComparison.Ordinal);
        foreach (string nodeName in new[]
                 {
                     "ExpeditionStage", "ExpeditionRouteStrip", "ExpeditionHud",
                     "LeftColumn", "ExpeditionSummary", "CitizenDetail",
                     "ExpeditionSquadStrip", "RightColumn", "EncounterSummary",
                     "ExpeditionSkillStrip", "CombatCommands", "AutoButton", "RetreatButton",
                 })
        {
            Assert.Contains($"name=\"{nodeName}\"", liveScene, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("SimulationControls", liveScene, StringComparison.Ordinal);
        Assert.DoesNotContain("PlayPauseButton", liveScene, StringComparison.Ordinal);
        Assert.DoesNotContain("CombatClock", liveScene + liveSource, StringComparison.Ordinal);
        Assert.DoesNotContain("ExpeditionClock", liveScene + liveSource, StringComparison.Ordinal);
        Assert.DoesNotContain("_Process", liveSource + stageSource, StringComparison.Ordinal);
        Assert.DoesNotContain("CombatResolver", stageSource, StringComparison.Ordinal);
        Assert.DoesNotContain("_PhysicsProcess", stageSource, StringComparison.Ordinal);
        Assert.Contains("DrawCombatant", stageSource, StringComparison.Ordinal);
        Assert.Contains("antialiased: false", stageSource, StringComparison.Ordinal);
        Assert.Contains("StageBounds = new(244, 0, 800, 488)", liveSource,
            StringComparison.Ordinal);
        Assert.Contains("LeftColumnBounds = new(8, 8, 228, 464)", liveSource,
            StringComparison.Ordinal);
        Assert.Contains("RightColumnBounds = new(1048, 8, 224, 464)", liveSource,
            StringComparison.Ordinal);
        Assert.Contains("SquadBounds = new(8, 480, 441, 176)", liveSource,
            StringComparison.Ordinal);
        Assert.Contains("SkillBounds = new(448, 472, 456, 180)", liveSource,
            StringComparison.Ordinal);
        Assert.Contains("CommandBounds = new(1048, 472, 224, 180)", liveSource,
            StringComparison.Ordinal);
        Assert.Contains("SetAnchorsAndOffsetsPreset(LayoutPreset.TopLeft)", liveSource,
            StringComparison.Ordinal);
        Assert.Contains("HasReferenceLayout", liveSource, StringComparison.Ordinal);
        Assert.DoesNotContain("_Process", liveSource, StringComparison.Ordinal);
    }

    [Fact]
    public void ExpeditionLiveCombat_UsesInputMapAndOneApplicationCommandPath()
    {
        string root = TestHelpers.FindRepositoryRoot();
        string project = File.ReadAllText(Path.Combine(root, "game", "project.godot"));
        string live = File.ReadAllText(Path.Combine(
            root, "game", "scripts", "ExpeditionLiveView.cs"));
        string controller = File.ReadAllText(Path.Combine(
            root, "game", "scripts", "CityWorldController.cs"));

        for (int index = 1; index <= 4; index++)
        {
            Assert.Contains($"expedition_skill_{index}=", project, StringComparison.Ordinal);
            Assert.Contains($"\"expedition_skill_{index}\"", live, StringComparison.Ordinal);
        }
        Assert.Contains("slot.Activated += OnSkillActivated", live, StringComparison.Ordinal);
        Assert.Contains("TryActivateSkill(slotNumber - 1)", live, StringComparison.Ordinal);
        Assert.Contains("TryActivateSkill(index)", live, StringComparison.Ordinal);
        Assert.Contains("_controller.TryActivateMemberSkill(id, slotIndex)", live,
            StringComparison.Ordinal);
        Assert.Contains("_world.TryActivateMemberSkill(expeditionId, slotIndex)", controller,
            StringComparison.Ordinal);
        Assert.Contains("if (changed) _hasUnsavedChanges = true;", controller,
            StringComparison.Ordinal);
        Assert.Contains("if (accepted) _hasUnsavedChanges = true;", controller,
            StringComparison.Ordinal);
        Assert.Contains("_autoButton.Toggled += OnAutoToggled", live, StringComparison.Ordinal);
        Assert.DoesNotContain("Key1", live, StringComparison.Ordinal);
        Assert.DoesNotContain("SpeedChoice.Paused", live + controller, StringComparison.Ordinal);
    }

    [Fact]
    public void ExpeditionLiveNavigation_UsesTheExistingSelectionRouterAndRailViewAction()
    {
        string root = TestHelpers.FindRepositoryRoot();
        string controller = File.ReadAllText(Path.Combine(
            root, "game", "scripts", "CityWorldController.cs"));
        string rootSource = File.ReadAllText(Path.Combine(
            root, "game", "scripts", "CityPrototype.cs"));
        string rail = File.ReadAllText(Path.Combine(
            root, "game", "scripts", "ExpeditionRail.cs"));

        Assert.Contains("ExpeditionLive = 3", controller, StringComparison.Ordinal);
        Assert.Contains("SelectExpeditionLive(ExpeditionId expeditionId)", controller,
            StringComparison.Ordinal);
        Assert.Contains("_controller.SelectExpeditionLive(id)", rail, StringComparison.Ordinal);
        Assert.Contains("FirstViewButton", rail, StringComparison.Ordinal);
        Assert.Contains("ui.expedition_rail.view", rail, StringComparison.Ordinal);
        Assert.Contains("FirstViewButton ?? FirstDetailsButton", rail, StringComparison.Ordinal);
        Assert.Contains("default focus did not reach View", rootSource, StringComparison.Ordinal);
        Assert.Contains("View -> ui_down -> Details OK", rootSource, StringComparison.Ordinal);
        Assert.Contains("controller.ReturnToCity();", rootSource, StringComparison.Ordinal);
        Assert.Contains(
            "controller.CurrentSelection == CityWorldController.Selection.MacroView",
            rootSource,
            StringComparison.Ordinal);
        Assert.DoesNotContain("SetSimulationSpeed", rail, StringComparison.Ordinal);
    }

    [Fact]
    public void ExpeditionPanel_IsCenteredByTheExistingScreenContainer()
    {
        string root = TestHelpers.FindRepositoryRoot();
        string cityScene = File.ReadAllText(Path.Combine(
            root, "game", "scenes", "CityPrototype.tscn"));
        string panelSource = File.ReadAllText(Path.Combine(
            root, "game", "scripts", "ExpeditionPanel.cs"));

        Assert.Contains(
            "[node name=\"ExpeditionPanel\" parent=\"GameUiShell/ScreenContent/Center\"",
            cityScene,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "[node name=\"ExpeditionPanel\" parent=\"GameUiShell/ScreenContent\"",
            cityScene,
            StringComparison.Ordinal);
        Assert.Contains("GetParent() is Container", panelSource, StringComparison.Ordinal);
        Assert.Contains("CustomMinimumSize = size", panelSource, StringComparison.Ordinal);
    }

    [Fact]
    public void ExpeditionLiveEarlyFixture_UsesRealViewClickAndKeepsGlobalSpeed()
    {
        string source = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(), "game", "scripts", "CityPrototype.cs"));

        Assert.Contains("case \"expedition-live-early\"", source, StringComparison.Ordinal);
        Assert.Contains("case \"expedition-live-escape\"", source, StringComparison.Ordinal);
        Assert.Contains("SendPointerClickForVisualRegression(rail.FirstViewButton)", source,
            StringComparison.Ordinal);
        Assert.Contains("CityWorldController.SpeedChoice.Fast", source, StringComparison.Ordinal);
        Assert.Contains("controller.CurrentSpeed == CityWorldController.SpeedChoice.Fast", source,
            StringComparison.Ordinal);
        Assert.Contains("ShowEarlyFixture", source, StringComparison.Ordinal);
        Assert.Contains("ui.expedition_live.founder_short", File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(), "game", "scripts", "ExpeditionLiveView.cs")),
            StringComparison.Ordinal);
        Assert.Contains("[WOG-EXPEDITION-LIVE-ESC] returned to city", source,
            StringComparison.Ordinal);
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
