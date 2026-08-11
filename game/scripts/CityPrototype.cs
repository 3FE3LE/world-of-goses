#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Domain.Combat;
using WorldofGoses.Domain.Persistence;
using WorldofGoses.Ui;
using WorldofGoses.Prototypes;

namespace WorldofGoses;

/// <summary>
/// Root of the prototype scene. Composes the macro city view and the
/// building detail view, hosts the <see cref="CityWorldController"/>,
/// and handles top-level input. The actual visual logic lives in
/// the view scripts; this script is intentionally thin.
/// </summary>
public partial class CityPrototype : Node
{
    private bool _expeditionLiveEscapeFixture;
    private ExpeditionLiveFixtureState _expeditionLiveFixtureState;
    /// <summary>
    /// Authored macro composition target for the bottom-centre
    /// <see cref="PrimaryNavDock"/> in the labelled profile. Re-freeze only
    /// after human visual sign-off at both 1280×720 and 1920×1080; the
    /// fixture in <see cref="ValidatePrimaryNavFocusForVisualRegression"/>
    /// asserts against this constant, not a literal, so re-tuning touches
    /// one place.
    /// </summary>
    private static readonly Vector2 PrimaryNavDockSize = new(520, 60);

    /// <summary>
    /// Top-level back key. Iterates the input tree so a single ESC
    /// pulse closes exactly one overlay:
    /// <list type="number">
    /// <item>Topmost modal — <see cref="ModalHost"/> is the leafmost
    /// listener and eats the input via <c>SetInputAsHandled</c> when
    /// <c>IsOpen</c>; no further handler runs.</item>
    /// <item>Pause menu — closes itself when visible; when hidden and
    /// the macro view is active, opens itself; when hidden and the
    /// player is in a hero profile or building detail, deliberately
    /// lets the event propagate so this handler can run.</item>
    /// <item>Hero profile / building detail / expedition live view — this handler at the
    /// scene root returns to <see cref="CityWorldController.Selection.MacroView"/>
    /// via <see cref="CityWorldController.ReturnToCity"/>.</item>
    /// </list>
    /// Without this fallback the player had no way to leave a complete
    /// non-macro perspective with the keyboard once a modal
    /// had been opened and dismissed.
    /// </summary>
    public override void _UnhandledInput(InputEvent @event)
    {
        if (!@event.IsActionPressed("ui_cancel")) return;
        CityWorldController controller = GetNodeOrNull<CityWorldController>("CityWorldController");
        if (controller is null) return;
        // The PauseMenu only opens via ESC when the macro view is the
        // active selection. If the player is in a hero profile or
        // building detail or expedition live view, PauseMenu lets the input propagate so this
        // handler can return them to the macro view.
        if (controller.CurrentSelection == CityWorldController.Selection.MacroView) return;
        controller.ReturnToCity();
        GetViewport().SetInputAsHandled();
    }

    public override void _Ready()
    {
        GD.Print("World of Goses prototype starting.");
        // The path is relative to the FirstNightScene node itself, and
        // AddChild makes it a *child* of CityPrototype — so the controller,
        // a sibling, is one level up. Passing "CityWorldController" resolved
        // to FirstNightScene/CityWorldController, which never exists: the
        // scene then failed to subscribe and the whole authored night stayed
        // inert. The export default on FirstNightScene is already correct, so
        // there is nothing to override here.
        var firstNightScene = new FirstNightScene { Name = "FirstNightScene" };
        AddChild(firstNightScene);
        if (System.Environment.GetEnvironmentVariable("WOG_VISUAL_CAPTURE") == "1")
        {
            CallDeferred(MethodName.ApplyVisualRegressionFixture);
        }
    }

    private void ApplyVisualRegressionFixture()
    {
        const string fixturePrefix = "--wog-visual-fixture=";
        string? fixture = null;
        foreach (string argument in OS.GetCmdlineUserArgs())
        {
            if (!argument.StartsWith(fixturePrefix, StringComparison.Ordinal)) continue;
            fixture = argument[fixturePrefix.Length..];
            break;
        }

        // `biome-<lineage>` builds a fresh city founded by that lineage, so
        // every ground palette can be reviewed without replaying onboarding
        // once per lineage. Handled before the switch because the lineage is
        // part of the name.
        const string biomePrefix = "biome-";
        if (fixture is not null && fixture.StartsWith(biomePrefix, StringComparison.Ordinal))
        {
            ShowBiomeForVisualRegression(fixture[biomePrefix.Length..]);
            return;
        }

        const string primaryNavClickPrefix = "primary-nav-click-";
        if (fixture is not null
            && fixture.StartsWith(primaryNavClickPrefix, StringComparison.Ordinal))
        {
            ExercisePrimaryNavPointerForVisualRegression(
                fixture[primaryNavClickPrefix.Length..]);
            return;
        }

        const string simulationClickPrefix = "simulation-click-";
        if (fixture is not null
            && fixture.StartsWith(simulationClickPrefix, StringComparison.Ordinal))
        {
            ExerciseSimulationPointerForVisualRegression(
                fixture[simulationClickPrefix.Length..]);
            return;
        }

        const string expeditionRailClickPrefix = "expedition-rail-click-";
        if (fixture is not null
            && fixture.StartsWith(expeditionRailClickPrefix, StringComparison.Ordinal))
        {
            ExerciseExpeditionRailPointerForVisualRegression(
                fixture[expeditionRailClickPrefix.Length..]);
            return;
        }

        // Pressed and released, because IsActionPressed only fires on the edge and
        // a stuck action would leak into whatever the next fixture does.
        static void SendCancelForVisualRegression()
        {
            Input.ParseInputEvent(new InputEventAction { Action = "ui_cancel", Pressed = true });
            Input.ParseInputEvent(new InputEventAction { Action = "ui_cancel", Pressed = false });
        }

        static void SendRightForVisualRegression()
        {
            Input.ParseInputEvent(new InputEventJoypadButton
            {
                ButtonIndex = JoyButton.DpadRight,
                Pressed = true,
            });
            Input.ParseInputEvent(new InputEventJoypadButton
            {
                ButtonIndex = JoyButton.DpadRight,
                Pressed = false,
            });
        }

        static void SendArrowRightForVisualRegression(bool pressed)
        {
            Input.ParseInputEvent(new InputEventKey
            {
                Keycode = Key.Right,
                PhysicalKeycode = Key.Right,
                Pressed = pressed,
            });
        }

        static void SendArrowDownForVisualRegression(bool pressed)
        {
            Input.ParseInputEvent(new InputEventKey
            {
                Keycode = Key.Down,
                PhysicalKeycode = Key.Down,
                Pressed = pressed,
            });
        }

        switch (fixture)
        {
            case "macro-hud-default":
                ShowMacroHudForVisualRegression(MacroHudFixtureState.Default);
                break;
            case "macro-hud-selection":
                ShowMacroHudForVisualRegression(MacroHudFixtureState.Selection);
                break;
            case "macro-hud-active-construction":
                ShowMacroHudForVisualRegression(MacroHudFixtureState.ActiveConstruction);
                break;
            case "macro-hud-expedition-active":
                ShowMacroHudForVisualRegression(MacroHudFixtureState.ActiveExpedition);
                break;
            case "expedition-live-early":
                ShowExpeditionLiveForVisualRegression(ExpeditionLiveFixtureState.Encounter);
                break;
            case "expedition-live-escape":
                ShowExpeditionLiveForVisualRegression(
                    ExpeditionLiveFixtureState.Encounter,
                    exitWithCancel: true);
                break;
            case "expedition-live-travel":
                ShowExpeditionLiveForVisualRegression(ExpeditionLiveFixtureState.Travel);
                break;
            case "expedition-live-encounter":
                ShowExpeditionLiveForVisualRegression(ExpeditionLiveFixtureState.Encounter);
                break;
            case "expedition-live-basic-attack":
                ShowExpeditionLiveForVisualRegression(ExpeditionLiveFixtureState.BasicAttack);
                break;
            case "expedition-live-skill-ready":
                ShowExpeditionLiveForVisualRegression(ExpeditionLiveFixtureState.SkillReady);
                break;
            case "expedition-live-skill-cooldown":
                ShowExpeditionLiveForVisualRegression(ExpeditionLiveFixtureState.SkillCooldown);
                break;
            case "expedition-live-auto-on":
                ShowExpeditionLiveForVisualRegression(ExpeditionLiveFixtureState.AutoOn);
                break;
            case "expedition-live-auto-off":
                ShowExpeditionLiveForVisualRegression(ExpeditionLiveFixtureState.AutoOff);
                break;
            case "expedition-live-ranged":
                ShowExpeditionLiveForVisualRegression(ExpeditionLiveFixtureState.Ranged);
                break;
            case "expedition-live-knockback":
                ShowExpeditionLiveForVisualRegression(ExpeditionLiveFixtureState.Knockback);
                break;
            case "expedition-live-objective":
                ShowExpeditionLiveForVisualRegression(ExpeditionLiveFixtureState.Objective);
                break;
            case "expedition-live-return":
                ShowExpeditionLiveForVisualRegression(ExpeditionLiveFixtureState.Return);
                break;
            case "offline-report":
                GetNode<ExpeditionRail>(
                    "GameUiShell/ScreenContent/ExpeditionRail")
                    .ShowVisualRegressionReport(BuildVisualOfflineReport());
                break;
            case "pause-menu":
                GetNode<PauseMenu>("PauseMenu").ShowForVisualRegression(confirmReset: false);
                break;
            case "pause-menu-reset":
                GetNode<PauseMenu>("PauseMenu").ShowForVisualRegression(confirmReset: true);
                break;
            case "primary-nav-focus":
                PrimaryNavDock navDock = GetNode<PrimaryNavDock>(
                    "GameUiShell/ScreenContent/PrimaryNavDock");
                navDock.Show();
                navDock.GrabDefaultFocus();
                Callable.From(() =>
                {
                    SendRightForVisualRegression();
                    GetTree().CreateTimer(0.15).Timeout +=
                        ValidatePrimaryNavFocusForVisualRegression;
                }).CallDeferred();
                break;
            case "macro-arrow-focus-isolation":
                ShowTopStatusForVisualRegression("en");
                PrimaryNavDock arrowDock = GetNode<PrimaryNavDock>(
                    "GameUiShell/ScreenContent/PrimaryNavDock");
                MacroStreetLiveView arrowMacro = GetNode<MacroStreetLiveView>(
                    "GameUiShell/ScreenContent/MacroStreetLiveView");
                arrowDock.HeroButton.GrabFocus();
                float cameraBefore = arrowMacro.CameraLateralForVisualRegression;
                Callable.From(() =>
                {
                    SendArrowRightForVisualRegression(pressed: true);
                    GetTree().CreateTimer(0.2).Timeout += () =>
                    {
                        SendArrowRightForVisualRegression(pressed: false);
                        ValidateMacroArrowFocusIsolationForVisualRegression(cameraBefore);
                    };
                }).CallDeferred();
                break;
            case "pause-arrow-focus":
                ShowTopStatusForVisualRegression("en");
                PauseMenu arrowPause = GetNode<PauseMenu>("PauseMenu");
                MacroStreetLiveView pausedMacro = GetNode<MacroStreetLiveView>(
                    "GameUiShell/ScreenContent/MacroStreetLiveView");
                arrowPause.ShowForVisualRegression(confirmReset: false);
                float pausedCameraBefore = pausedMacro.CameraLateralForVisualRegression;
                Callable.From(() =>
                {
                    SendArrowDownForVisualRegression(pressed: true);
                    GetTree().CreateTimer(0.2).Timeout += () =>
                    {
                        SendArrowDownForVisualRegression(pressed: false);
                        ValidatePauseArrowFocusForVisualRegression(pausedCameraBefore);
                    };
                }).CallDeferred();
                break;
            case "construction-hero-route":
                ShowTopStatusForVisualRegression("en");
                MacroStreetLiveView heroRouteMacro = GetNode<MacroStreetLiveView>(
                    "GameUiShell/ScreenContent/MacroStreetLiveView");
                heroRouteMacro.ShowConstructionForVisualRegression(placement: false);
                GetTree().CreateTimer(0.25).Timeout += () =>
                {
                    ConstructionPanel construction = GetNode<ConstructionPanel>(
                        "GameUiShell/ScreenContent/Center/ConstructionPanel");
                    SendPointerClickForVisualRegression(construction.ViewHeroButtonForVisualRegression);
                    GetTree().CreateTimer(0.5).Timeout +=
                        ValidateConstructionHeroRouteForVisualRegression;
                };
                break;
            case "simulation-controls-focus":
                // Speed control now lives in the status bar's utility
                // cluster; grab focus on it directly.
                CityStatusPanel speedPanel = GetNode<CityStatusPanel>(
                    "GameUiShell/CityStatusPanel");
                speedPanel.SpeedButton.GrabFocus();
                Callable.From(() =>
                {
                    SendRightForVisualRegression();
                    GetTree().CreateTimer(0.15).Timeout +=
                        ValidateSimulationControlsFocusForVisualRegression;
                }).CallDeferred();
                break;
            case "construction-scroll":
                GetNode<MacroStreetLiveView>("GameUiShell/ScreenContent/MacroStreetLiveView")
                    .ShowConstructionForVisualRegression(placement: false);
                break;
            case "construction-placement":
                ShowTopStatusForVisualRegression("en");
                GetNode<MacroStreetLiveView>("GameUiShell/ScreenContent/MacroStreetLiveView")
                    .ShowConstructionForVisualRegression(placement: true);
                break;
            case "action-dock-focus":
                ShowTopStatusForVisualRegression("en");
                GetNode<MacroStreetLiveView>("GameUiShell/ScreenContent/MacroStreetLiveView")
                    .ShowConstructionForVisualRegression(placement: true);
                ActionDock actionDock = GetNode<ActionDock>(
                    "GameUiShell/ScreenContent/ActionDock");
                actionDock.ConfirmButton.GrabFocus();
                Callable.From(() =>
                {
                    SendRightForVisualRegression();
                    GetTree().CreateTimer(0.15).Timeout +=
                        ValidateActionDockFocusForVisualRegression;
                }).CallDeferred();
                break;
            case "construction-placement-escape":
                ShowTopStatusForVisualRegression("en");
                GetNode<MacroStreetLiveView>("GameUiShell/ScreenContent/MacroStreetLiveView")
                    .ShowConstructionForVisualRegression(placement: true);
                // A real ui_cancel through the input queue, not a direct call to
                // CancelPlacement: the point is to prove the key reaches
                // _UnhandledInput now that the action dock sits between the player
                // and the world and its buttons can hold focus. Deferred so the
                // placement state exists before the key arrives.
                Callable.From(() =>
                {
                    SendCancelForVisualRegression();
                    GetTree().CreateTimer(0.15).Timeout += () =>
                        ValidateActionDockExitForVisualRegression(confirm: false, input: "escape");
                }).CallDeferred();
                break;
            case "construction-placement-confirm-click":
                ExerciseActionDockPointerForVisualRegression(confirm: true);
                break;
            case "construction-placement-cancel-click":
                ExerciseActionDockPointerForVisualRegression(confirm: false);
                break;
            case "construction-placement-hover-invalid":
                GetNode<MacroStreetLiveView>("GameUiShell/ScreenContent/MacroStreetLiveView")
                    .ShowConstructionPlacementHoverForVisualRegression(valid: false);
                break;
            case "founding-blueprint":
                ShowFoundingSiteForVisualRegression(moduleChoice: false, blockedCargo: false);
                break;
            case "founding-module-choice":
                ShowFoundingSiteForVisualRegression(moduleChoice: true, blockedCargo: false);
                break;
            case "founding-blocked-cargo":
                ShowFoundingSiteForVisualRegression(moduleChoice: false, blockedCargo: true);
                break;
            case "early-game-resources":
                ShowEarlyGameResourcesForVisualRegression();
                break;
            case "top-status-en":
                ShowTopStatusForVisualRegression("en");
                break;
            case "top-status-es":
                ShowTopStatusForVisualRegression("es");
                break;
            case "city-summary-en":
                ShowCitySummaryForVisualRegression("en", blocked: false);
                break;
            case "city-summary-es-blocked":
                ShowCitySummaryForVisualRegression("es", blocked: true);
                break;
            case "city-summary-es":
                ShowCitySummaryForVisualRegression("es", blocked: false);
                break;
            case "city-summary-low-food":
                ShowCitySummaryLowFoodForVisualRegression("en");
                break;
            case "city-summary-housing-full":
                ShowCitySummaryHousingFullForVisualRegression("en");
                break;
            case "city-summary-no-construction":
                ShowCitySummaryNoConstructionForVisualRegression("en");
                break;
            case "shelter-resources":
                ShowShelterResourcesForVisualRegression();
                break;
            case "cultivation-prepared":
                ShowCultivationForVisualRegression();
                break;
            case "expedition-idle":
                ShowExpeditionForVisualRegression(ExpeditionFixtureState.Idle);
                break;
            case "hero-incorporation":
                ShowHeroIncorporationForVisualRegression();
                break;
            case "wound-recovery":
                ShowWoundRecoveryForVisualRegression();
                break;
            case "world-status-treatment":
                ShowWorldStatusTreatmentForVisualRegression();
                break;
            case "citizen-click-summary":
                ShowCitizenClickSummaryForVisualRegression();
                break;
            case "expedition-active":
                ShowExpeditionForVisualRegression(ExpeditionFixtureState.Active);
                break;
            case "expedition-returned":
                ShowExpeditionForVisualRegression(ExpeditionFixtureState.Returned);
                break;
            case "expedition-rail-empty":
                ShowExpeditionRailForVisualRegression(ExpeditionRailFixtureState.Empty);
                break;
            case "expedition-rail-outbound":
                ShowExpeditionRailForVisualRegression(ExpeditionRailFixtureState.Outbound);
                break;
            case "expedition-rail-outbound-en":
                ShowExpeditionRailForVisualRegression(
                    ExpeditionRailFixtureState.Outbound, "en");
                break;
            case "expedition-rail-outbound-es":
                ShowExpeditionRailForVisualRegression(
                    ExpeditionRailFixtureState.Outbound, "es");
                break;
            case "expedition-rail-encounter":
                ShowExpeditionRailForVisualRegression(ExpeditionRailFixtureState.Encounter);
                break;
            case "expedition-rail-objective":
                ShowExpeditionRailForVisualRegression(ExpeditionRailFixtureState.Objective);
                break;
            case "expedition-rail-returning":
                ShowExpeditionRailForVisualRegression(ExpeditionRailFixtureState.Returning);
                break;
            case "expedition-rail-resolved":
                ShowExpeditionRailForVisualRegression(ExpeditionRailFixtureState.Resolved);
                break;
            case "expedition-rail-cancelled":
                ShowExpeditionRailForVisualRegression(ExpeditionRailFixtureState.Cancelled);
                break;
            case "expedition-rail-focus":
                ShowExpeditionRailForVisualRegression(ExpeditionRailFixtureState.Outbound);
                CallDeferred(MethodName.ExerciseExpeditionRailFocusForVisualRegression);
                break;
            case "expedition-rail-chronicle-roundtrip":
                ShowExpeditionRailForVisualRegression(ExpeditionRailFixtureState.Outbound);
                CallDeferred(MethodName.ExerciseExpeditionRailChronicleRoundTripForVisualRegression);
                break;
            case "expedition-rail-rail-protagonist":
                // Force the expedition section to be the visible
                // protagonist so the visual matrix can prove the cards
                // actually render when the rail accordion opens.
                ShowExpeditionRailForVisualRegression(ExpeditionRailFixtureState.Outbound);
                CallDeferred(MethodName.ForceExpeditionRailProtagonistForVisualRegression);
                break;
            case "expedition-rail-phase-focus":
                ShowExpeditionRailForVisualRegression(ExpeditionRailFixtureState.Outbound);
                CallDeferred(MethodName.ExerciseExpeditionRailPhaseFocusForVisualRegression);
                break;
            case "modal-layout-close":
                ValidateModalLayoutAndClosePaths();
                break;
            case "astral-start":
                GetNode<AstralOnboardingView>("OnboardingView")
                    .ShowForVisualRegression(0);
                break;
            case "astral-ground":
                GetNode<AstralOnboardingView>("OnboardingView")
                    .ShowForVisualRegression(10);
                break;
            case "astral-identity":
                GetNode<AstralOnboardingView>("OnboardingView")
                    .ShowForVisualRegression(12);
                break;
            // One past the naming beat. The founder card is only reachable by
            // confirming a name, which would create a hero, so capture mode
            // needs its own entry point into it.
            case "astral-founder-card":
                GetNode<AstralOnboardingView>("OnboardingView")
                    .ShowForVisualRegression(13);
                break;
            case "founder-arrival":
                ShowFounderArrivalForVisualRegression();
                break;
            case "firstnight-manifested":
                ShowFirstNightForVisualRegression();
                break;
            // The four moments the ambient day/night curve is built
            // around. Reviewing the tint needs a pinned hour: otherwise
            // the captured time is whatever the save happens to hold.
            case "time-midnight":
                PinTimeOfDayForVisualRegression(0.0);
                break;
            case "time-dawn":
                PinTimeOfDayForVisualRegression(0.229);
                break;
            case "time-noon":
                PinTimeOfDayForVisualRegression(0.5);
                break;
            case "time-dusk":
                PinTimeOfDayForVisualRegression(0.771);
                break;
            case "language-selector":
                GetNode<PauseMenu>("PauseMenu").Open();
                break;
            case "forest-depleted":
                GetNode<CityWorldController>("CityWorldController")
                    .DrainAllForestsForVisualRegression();
                break;
            case "citizen-travel":
                ShowCitizenTravelForVisualRegression();
                break;
            case "camera-depth-third-row":
                GetNode<MacroStreetLiveView>("GameUiShell/ScreenContent/MacroStreetLiveView")
                    .ShowThirdStreetDepthForVisualRegression();
                break;
            case "long-terrarium-20-rows":
                ShowLongTerrariumForVisualRegression(additionalRows: 20);
                break;
            case "long-terrarium-16-rows":
                ShowLongTerrariumForVisualRegression(additionalRows: 15);
                break;
            case "terrarium-8x9-window":
                ShowTerrariumWindowForVisualRegression(rows: 8, columns: 9);
                break;
            case "policies":
                GetNode<PoliciesPanel>("GameUiShell/ScreenContent/PoliciesPanel").Open();
                break;
            case "combat-debug":
                ShowCombatDebugForVisualRegression();
                break;
            case "migrant":
                GetNode<MigrantPanel>("GameUiShell/ScreenContent/MigrantPanel")
                    .ShowForVisualRegression();
                break;
            case "migrant-cube":
                ShowMigrantCubeForVisualRegression();
                break;
        }
    }

    private void ExercisePrimaryNavPointerForVisualRegression(string action)
    {
        MacroStreetLiveView macro = GetNode<MacroStreetLiveView>(
            "GameUiShell/ScreenContent/MacroStreetLiveView");
        macro.ShowEarlyGameResourcesForVisualRegression();
        PrimaryNavDock dock = GetNode<PrimaryNavDock>(
            "GameUiShell/ScreenContent/PrimaryNavDock");
        CityStatusPanel statusPanel = GetNode<CityStatusPanel>(
            "GameUiShell/CityStatusPanel");
        IconButton button = action switch
        {
            "hero" => dock.HeroButton,
            "construction" => dock.ConstructionButton,
            "menu" => statusPanel.MenuButton,
            "expedition" => dock.ExpeditionButton,
            "policies" => dock.PoliciesButton,
            "citizens" => dock.CitizensButton,
            "camera" => statusPanel.CameraButton,
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, "Unknown primary navigation action."),
        };
        dock.Show();
        Callable.From(() =>
        {
            SendPointerClickForVisualRegression(button);
            GetTree().CreateTimer(0.15).Timeout +=
                () => ValidatePrimaryNavPointerForVisualRegression(action);
        }).CallDeferred();
    }

    private static void SendPointerClickForVisualRegression(Control target)
    {
        Vector2 logicalPosition = target.GetGlobalRect().GetCenter();
        Vector2 logicalViewportSize = target.GetViewport().GetVisibleRect().Size;
        Vector2 windowSize = DisplayServer.WindowGetSize();
        Vector2 windowScale = new(
            windowSize.X / logicalViewportSize.X,
            windowSize.Y / logicalViewportSize.Y);
        Vector2 position = logicalPosition * windowScale;
        Input.ParseInputEvent(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Left,
            ButtonMask = MouseButtonMask.Left,
            Position = position,
            GlobalPosition = position,
            Pressed = true,
        });
        Input.ParseInputEvent(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Left,
            Position = position,
            GlobalPosition = position,
            Pressed = false,
        });
    }

    private void ValidatePrimaryNavFocusForVisualRegression()
    {
        PrimaryNavDock dock = GetNode<PrimaryNavDock>(
            "GameUiShell/ScreenContent/PrimaryNavDock");
        CityStatusPanel statusPanel = GetNode<CityStatusPanel>(
            "GameUiShell/CityStatusPanel");
        Rect2 dockRect = dock.GetGlobalRect();
        if (dockRect.Size != PrimaryNavDockSize
            || dockRect.Intersects(statusPanel.GetGlobalRect()))
        {
            GD.PushError(
                $"Primary navigation geometry is {dockRect}; expected {PrimaryNavDockSize} without "
                + "overlapping SimulationControls.");
            return;
        }
        if (GetViewport().GuiGetFocusOwner() != dock.ConstructionButton)
        {
            Control? owner = GetViewport().GuiGetFocusOwner();
            GD.PushError(
                "Primary navigation ui_right fixture did not move focus to Construction; "
                + $"focus is {owner?.Name ?? "<none>"}, hero right is "
                + $"{dock.HeroButton.FocusNeighborRight}.");
            return;
        }
        GD.Print($"[WOG-NAV-FOCUS] ui_right -> Construction OK; dock={dockRect}");
    }

    private void ValidateMacroArrowFocusIsolationForVisualRegression(float cameraBefore)
    {
        PrimaryNavDock dock = GetNode<PrimaryNavDock>(
            "GameUiShell/ScreenContent/PrimaryNavDock");
        MacroStreetLiveView macro = GetNode<MacroStreetLiveView>(
            "GameUiShell/ScreenContent/MacroStreetLiveView");
        Control? focus = GetViewport().GuiGetFocusOwner();
        if (focus != dock.HeroButton || macro.CameraLateralForVisualRegression <= cameraBefore)
        {
            GD.PushError(
                "Macro arrow isolation fixture failed; "
                + $"focus={focus?.Name ?? "<none>"}, camera before={cameraBefore}, "
                + $"after={macro.CameraLateralForVisualRegression}.");
            return;
        }
        GD.Print("[WOG-MACRO-ARROW] Right moved camera and preserved Hero HUD focus OK");
    }

    private void ValidatePauseArrowFocusForVisualRegression(float cameraBefore)
    {
        PauseMenu pause = GetNode<PauseMenu>("PauseMenu");
        MacroStreetLiveView macro = GetNode<MacroStreetLiveView>(
            "GameUiShell/ScreenContent/MacroStreetLiveView");
        IconButton settings = pause.GetNode<IconButton>(
            "Center/Card/Margin/Shell/MainActions/SettingsButton");
        Control? focus = GetViewport().GuiGetFocusOwner();
        if (focus != settings || macro.CameraLateralForVisualRegression != cameraBefore)
        {
            GD.PushError(
                "Pause arrow fixture failed; "
                + $"focus={focus?.Name ?? "<none>"}, camera before={cameraBefore}, "
                + $"after={macro.CameraLateralForVisualRegression}.");
            return;
        }
        GD.Print("[WOG-PAUSE-ARROW] Down moved Pause focus and left camera unchanged OK");
    }

    private void ValidateConstructionHeroRouteForVisualRegression()
    {
        ModalHost modalHost = GetNode<ModalHost>("GameUiShell/ScreenContent/ModalHost");
        ConstructionPanel construction = GetNode<ConstructionPanel>(
            "GameUiShell/ScreenContent/Center/ConstructionPanel");
        HeroProfileView hero = GetNode<HeroProfileView>(
            "GameUiShell/ScreenContent/HeroProfileView");
        if (modalHost.IsOpen || construction.Visible || !hero.Visible)
        {
            GD.PushError(
                "Construction -> Hero route left the modal stacked; "
                + $"modal={modalHost.IsOpen}, construction={construction.Visible}, hero={hero.Visible}.");
            return;
        }
        GD.Print("[WOG-CONSTRUCTION-HERO] modal closed before Hero profile OK");
    }

    private void ValidateActionDockFocusForVisualRegression()
    {
        ActionDock dock = GetNode<ActionDock>("GameUiShell/ScreenContent/ActionDock");
        if (GetViewport().GuiGetFocusOwner() != dock.CancelButton)
        {
            Control? owner = GetViewport().GuiGetFocusOwner();
            GD.PushError(
                "Action dock ui_right fixture did not move focus from Confirm to Cancel; "
                + $"focus is {owner?.Name ?? "<none>"}, confirm right is "
                + $"{dock.ConfirmButton.FocusNeighborRight}.");
            return;
        }
        GD.Print("[WOG-ACTION-DOCK-FOCUS] ui_right -> Cancel OK");
    }

    private void ValidateSimulationControlsFocusForVisualRegression()
    {
        CityStatusPanel speedPanel = GetNode<CityStatusPanel>(
            "GameUiShell/CityStatusPanel");
        if (GetViewport().GuiGetFocusOwner() != speedPanel.SpeedButton)
        {
            Control? owner = GetViewport().GuiGetFocusOwner();
            GD.PushError(
                "Simulation controls ui_right fixture did not move focus to "
                + $"Speed; focus is {owner?.Name ?? "<none>"}.");
            return;
        }
        GD.Print("[WOG-SIMULATION-FOCUS] ui_right -> Speed OK");
    }

    private void ValidatePrimaryNavPointerForVisualRegression(string action)
    {
        ModalHost modalHost = GetNode<ModalHost>("GameUiShell/ScreenContent/ModalHost");
        PrimaryNavDock dock = GetNode<PrimaryNavDock>(
            "GameUiShell/ScreenContent/PrimaryNavDock");
        CityStatusPanel statusPanel = GetNode<CityStatusPanel>(
            "GameUiShell/CityStatusPanel");
        bool passed = action switch
        {
            "hero" => GetNode<HeroProfileView>(
                "GameUiShell/ScreenContent/HeroProfileView").Visible,
            "construction" => modalHost.IsOpen && modalHost.Content?.Name == "ConstructionPanel",
            "menu" => GetNode<PauseMenu>("PauseMenu").Visible,
            "expedition" => modalHost.IsOpen && modalHost.Content?.Name == "ExpeditionPanel",
            "policies" => modalHost.IsOpen && modalHost.Content?.Name == "PoliciesPanel",
            "citizens" => modalHost.IsOpen && modalHost.Content?.Name == "MigrantPanel",
            "camera" => statusPanel.CameraButton.ThemeTypeVariation == "HudButtonSelected",
            _ => false,
        };
        if (!passed)
        {
            GD.PushError(
                $"Primary navigation pointer fixture failed for {action}; "
                + $"camera label={statusPanel.CameraButton.ButtonText}, "
                + $"theme={statusPanel.CameraButton.ThemeTypeVariation}, "
                + $"rect={statusPanel.CameraButton.GetGlobalRect()}.");
            return;
        }
        GD.Print($"[WOG-NAV-CLICK] {action} OK");
    }

    private void ExerciseActionDockPointerForVisualRegression(bool confirm)
    {
        ShowTopStatusForVisualRegression("en");
        MacroStreetLiveView macro = GetNode<MacroStreetLiveView>(
            "GameUiShell/ScreenContent/MacroStreetLiveView");
        if (confirm) macro.PreparePlacementConfirmationForVisualRegression();
        else macro.ShowConstructionForVisualRegression(placement: true);

        ActionDock actionDock = GetNode<ActionDock>("GameUiShell/ScreenContent/ActionDock");
        Control target = confirm ? actionDock.ConfirmButton : actionDock.CancelButton;
        Callable.From(() =>
        {
            SendPointerClickForVisualRegression(target);
            GetTree().CreateTimer(0.15).Timeout +=
                () => ValidateActionDockExitForVisualRegression(confirm, input: "pointer");
        }).CallDeferred();
    }

    private void ValidateActionDockExitForVisualRegression(bool confirm, string input)
    {
        PrimaryNavDock primaryDock = GetNode<PrimaryNavDock>(
            "GameUiShell/ScreenContent/PrimaryNavDock");
        ActionDock actionDock = GetNode<ActionDock>("GameUiShell/ScreenContent/ActionDock");
        ModalHost modalHost = GetNode<ModalHost>("GameUiShell/ScreenContent/ModalHost");
        bool passed = primaryDock.Visible
            && !actionDock.Visible
            && (confirm
                ? !modalHost.IsOpen
                : modalHost.IsOpen && modalHost.Content?.Name == "ConstructionPanel");
        if (!passed)
        {
            GD.PushError(
                $"ActionDock {(confirm ? "confirm" : "cancel")} {input} fixture failed; "
                + $"primary={primaryDock.Visible}, contextual={actionDock.Visible}, "
                + $"modal={modalHost.Content?.Name}.");
            return;
        }
        GD.Print(
            $"[WOG-ACTION-DOCK-EXIT] {(confirm ? "confirm" : "cancel")} {input} OK");
    }

    private void ExerciseSimulationPointerForVisualRegression(string action)
    {
        if (action != "speed")
        {
            throw new ArgumentOutOfRangeException(
                nameof(action), action,
                "Only the 'speed' simulation action remains; pause is gone.");
        }
        CityWorldController controller = GetNode<CityWorldController>("CityWorldController");
        controller.SetSimulationSpeed(CityWorldController.SpeedChoice.Normal);
        CityStatusPanel statusPanel = GetNode<CityStatusPanel>(
            "GameUiShell/CityStatusPanel");
        Control target = statusPanel.SpeedButton;
        Callable.From(() =>
        {
            SendPointerClickForVisualRegression(target);
            GetTree().CreateTimer(0.15).Timeout += () =>
            {
                if (controller.CurrentSpeed != CityWorldController.SpeedChoice.Fast)
                {
                    GD.PushError(
                        $"Simulation speed pointer fixture expected Fast, "
                        + $"got {controller.CurrentSpeed}.");
                    return;
                }
                GD.Print("[WOG-SIMULATION-CLICK] speed OK");
            };
        }).CallDeferred();
    }

    /// <summary>
    /// Adds the combat debug panel at runtime and resolves one expedition, so the
    /// slice is reachable in the engine without editing CityPrototype.tscn. The
    /// panel is developer scaffolding; the player-facing preparation screen belongs
    /// to a later slice.
    /// </summary>
    private void ShowCombatDebugForVisualRegression()
    {
        // Parent to the UI layer so the panel gets a real rect and draws above the
        // HUD. The scene root is not a Control, so anchoring against it yields the
        // panel's minimum size instead of the screen.
        Node host = GetNodeOrNull<Control>("GameUiShell/ScreenContent") ?? (Node)this;
        var panel = new CombatDebugPanel
        {
            Name = "CombatDebugPanel",
            ControllerPath = host == this
                ? "../CityWorldController"
                : "../../../CityWorldController",
        };
        host.AddChild(panel);
        panel.Open();
        panel.RunForVisualRegression();
    }

    private void ShowLongTerrariumForVisualRegression(int additionalRows)
    {
        CityWorldController controller = GetNode<CityWorldController>("CityWorldController");
        Citizen? loadedFounder = controller.World.Hero;
        var fixture = new CityWorld();
        if (loadedFounder is not null)
        {
            HeroCreationResult heroResult = fixture.TryCreateHero(new HeroCreationRequest(
                "Aster",
                loadedFounder.Profile,
                loadedFounder.Profile.Gender));
            if (!heroResult.IsSuccess)
            {
                GD.PushError($"Long-terrarium fixture could not create founder: {heroResult.Outcome}.");
                return;
            }
            fixture.SeedStartingForests();
            fixture.SeedStartingOpportunities();
        }

        WorldSave save = WorldPersistence.Capture(fixture);
        AddTerrariumRowsForVisualRegression(save, additionalRows);
        controller.World.Restore(save);
        GetNode<MacroStreetLiveView>("GameUiShell/ScreenContent/MacroStreetLiveView")
            .ShowLongTerrariumForVisualRegression();
    }

    private void ShowTerrariumWindowForVisualRegression(int rows, int columns)
    {
        CityWorldController controller = GetNode<CityWorldController>("CityWorldController");
        Citizen? loadedFounder = controller.World.Hero;
        var fixture = new CityWorld();
        if (loadedFounder is not null)
        {
            HeroCreationResult heroResult = fixture.TryCreateHero(new HeroCreationRequest(
                "Aster",
                loadedFounder.Profile,
                loadedFounder.Profile.Gender));
            if (!heroResult.IsSuccess)
            {
                GD.PushError($"Terrarium fixture could not create founder: {heroResult.Outcome}.");
                return;
            }
            fixture.SeedStartingForests();
            fixture.SeedStartingOpportunities();
        }

        WorldSave save = WorldPersistence.Capture(fixture);
        ResizeTerrariumForVisualRegression(save, rows, columns);
        controller.World.Restore(save);
        GetNode<MacroStreetLiveView>("GameUiShell/ScreenContent/MacroStreetLiveView")
            .ShowLongTerrariumForVisualRegression();
    }

    internal static void ResizeTerrariumForVisualRegression(
        WorldSave save,
        int rows,
        int columns)
    {
        if (rows <= 0) throw new ArgumentOutOfRangeException(nameof(rows));
        if (columns <= 0) throw new ArgumentOutOfRangeException(nameof(columns));

        int existingColumnCount = save.Parcels
            .Select(parcel => parcel.LogicalColumn)
            .DefaultIfEmpty(-1)
            .Max() + 1;
        if (columns < existingColumnCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(columns),
                "Terrarium fixture cannot discard existing parcel columns.");
        }
        int addedColumns = columns - existingColumnCount;
        if (addedColumns % 2 != 0)
        {
            throw new ArgumentException(
                "Terrarium fixture must add the same number of columns on both sides.",
                nameof(columns));
        }
        int leftParcelColumns = addedColumns / 2;
        if (leftParcelColumns > 0)
        {
            foreach (ParcelSave parcel in save.Parcels)
            {
                parcel.LogicalColumn += leftParcelColumns;
            }
            int leftLotColumns = leftParcelColumns * ParcelGrid.LotsPerAxis;
            int leftFrontageColumns =
                leftParcelColumns * ParcelGrid.FrontageColumnsPerParcel;
            foreach (ParcelPlacementSave placement in save.ParcelPlacements)
            {
                placement.LotColumn += leftLotColumns;
                placement.StartColumn += leftFrontageColumns;
            }
            foreach (CorridorReservationSave corridor in save.CorridorReservations)
            {
                corridor.StartColumn += leftFrontageColumns;
            }
        }

        var existing = save.Parcels
            .ToDictionary(parcel => (parcel.LogicalRow, parcel.LogicalColumn));
        int nextParcelId = save.Parcels
            .Select(parcel => parcel.Id)
            .DefaultIfEmpty(0)
            .Max() + 1;
        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                if (existing.ContainsKey((row, column))) continue;
                save.Parcels.Add(new ParcelSave
                {
                    Id = nextParcelId++,
                    LogicalColumn = column,
                    LogicalRow = row,
                    IsUnlocked = true,
                    TerritoryState = ParcelTerritoryState.Available.ToString(),
                });
            }
        }
    }

    internal static void AddTerrariumRowsForVisualRegression(
        WorldSave save,
        int additionalRows)
    {
        if (additionalRows < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(additionalRows));
        }
        if (additionalRows == 0) return;

        int columnCount = save.Parcels
            .Where(parcel => parcel.LogicalRow == 0)
            .Select(parcel => parcel.LogicalColumn)
            .DefaultIfEmpty(-1)
            .Max() + 1;
        if (columnCount <= 0)
        {
            throw new InvalidOperationException(
                "Long-terrarium fixture requires an initialized founding parcel row.");
        }
        int firstNewRow = save.Parcels
            .Select(parcel => parcel.LogicalRow)
            .DefaultIfEmpty(-1)
            .Max() + 1;
        int nextParcelId = save.Parcels
            .Select(parcel => parcel.Id)
            .DefaultIfEmpty(0)
            .Max() + 1;

        for (int row = firstNewRow; row < firstNewRow + additionalRows; row++)
        {
            for (int column = 0; column < columnCount; column++)
            {
                save.Parcels.Add(new ParcelSave
                {
                    Id = nextParcelId++,
                    LogicalColumn = column,
                    LogicalRow = row,
                    IsUnlocked = true,
                    TerritoryState = ParcelTerritoryState.Available.ToString(),
                });
            }
        }
    }

    private void ShowFoundingSiteForVisualRegression(bool moduleChoice, bool blockedCargo)
    {
        CityWorldController controller = GetNode<CityWorldController>("CityWorldController");
        Citizen? loadedFounder = controller.World.Hero;
        if (loadedFounder is null)
        {
            GD.PushError("Founding Site visual fixture requires a loaded founder profile.");
            return;
        }

        var fixture = new CityWorld();
        HeroCreationResult heroResult = fixture.TryCreateHero(new HeroCreationRequest(
            "Aster",
            loadedFounder.Profile,
            loadedFounder.Profile.Gender));
        if (!heroResult.IsSuccess)
        {
            GD.PushError($"Founding Site visual fixture could not create founder: {heroResult.Outcome}.");
            return;
        }
        fixture.SeedStartingForests();
        fixture.SeedStartingOpportunities();
        fixture.Resources.DepositToCityInventory(ResourceType.Branches, 3);
        fixture.Resources.DepositToCityInventory(ResourceType.SmallStone, 2);

        if (moduleChoice || blockedCargo)
        {
            ConstructionAuthorizationResult authorization =
                fixture.TryAuthorizeConstruction(ConstructionKind.FoundingSite);
            if (!authorization.IsSuccess || authorization.ProjectId is not BuildingId projectId)
            {
                GD.PushError($"Founding Site visual fixture authorization failed: {authorization.Outcome}.");
                return;
            }
            ConstructionProject project = fixture.GetProject(projectId)!;
            project.Progress = project.RequiredWork;
            fixture.AdvanceWorldTick();
            if (blockedCargo)
            {
                fixture.TryUnassignFromProject(project.Id, fixture.Hero!.Id);
                fixture.Resources.DepositToCityInventory(
                    ResourceType.WildFood,
                    FoundingSiteRules.CarriedCapacity);
            }
            else
            {
                fixture.Resources.DepositToCityInventory(ResourceType.Branches, 2);
                fixture.Resources.DepositToCityInventory(ResourceType.PlantFiber, 3);
            }
        }

        controller.World.Restore(WorldPersistence.Capture(fixture));
        GetNode<MacroStreetLiveView>("GameUiShell/ScreenContent/MacroStreetLiveView")
            .ShowConstructionForVisualRegression(placement: false);
    }

    /// <summary>
    /// A brand-new city one instant after the founder manifests: the authored
    /// night is at <see cref="FirstNightStage.Manifested"/> and nothing has
    /// advanced it yet. This is exactly the state a real first run reaches,
    /// and it had no fixture — which is how the whole night shipped inert
    /// behind a mis-resolved NodePath while every domain test passed. The row
    /// this fixture backs asserts that the dialogue strip is actually on
    /// screen, not merely that the domain says the night is active.
    /// </summary>
    private void ShowFirstNightForVisualRegression()
    {
        CityWorldController controller = GetNode<CityWorldController>("CityWorldController");
        CitizenProfile nightProfile = NewFounderProfile(LineageId.Ardhen);
        var fixture = new CityWorld();
        HeroCreationResult heroResult = fixture.TryCreateHero(new HeroCreationRequest(
            "Aster",
            nightProfile,
            nightProfile.Gender));
        if (!heroResult.IsSuccess)
        {
            GD.PushError($"First-night fixture could not create founder: {heroResult.Outcome}.");
            return;
        }
        fixture.SeedStartingForests();
        fixture.SeedStartingOpportunities();
        controller.World.Restore(WorldPersistence.Capture(fixture));
        GetNode<AstralOnboardingView>("OnboardingView").Hide();
    }

    /// <summary>
    /// A founder for a fixture, built the way the game builds one.
    ///
    /// <para>
    /// A founder is born with a lineage, a body, an elemental affinity and a
    /// Cube — and nothing else. Aptitudes, professional affinities, personality
    /// traits, combat style and weapon preferences are <b>earned through a
    /// citizen's history</b>, which is why <see cref="CitizenProfile.CreateFounder"/>
    /// passes empty arrays for every one of them. Fixtures used to reach for
    /// <c>CitizenProfile.TryCreate</c> and hand-pick that list, which injected
    /// into a dummy founder exactly the things a founder is not supposed to
    /// start with — and quietly made test cities unrepresentative of a real
    /// first run.
    /// </para>
    /// </summary>
    private static CitizenProfile NewFounderProfile(
        LineageId lineage,
        GenderId gender = GenderId.Feminine) =>
        CitizenProfile.CreateFounder(
            new FounderOnboardingResult(
                lineage,
                ElementalAffinity.Earth,
                CubeScoring.ComputeCubeVertex(lineage),
                FounderNarrativeMemory.Empty),
            gender);

    /// <summary>
    /// A fresh city founded by <paramref name="lineageName"/>, so the ground
    /// biome that lineage's site uses (DEC-0017) can be reviewed directly.
    /// Reaching the eight palettes otherwise means replaying the twelve-step
    /// onboarding once per lineage and hoping the scorer lands on the one you
    /// want — the founder's lineage is inferred, never chosen.
    /// </summary>
    private void ShowBiomeForVisualRegression(string lineageName)
    {
        CityWorldController controller = GetNode<CityWorldController>("CityWorldController");
        LineageId? lineage = lineageName.ToLowerInvariant() switch
        {
            "ardhen" => LineageId.Ardhen,
            "eirune" => LineageId.Eirune,
            "kovari" => LineageId.Kovari,
            "myrven" => LineageId.Myrven,
            "vaelun" => LineageId.Vaelun,
            "orveth" => LineageId.Orveth,
            "caelith" => LineageId.Caelith,
            "theryn" => LineageId.Theryn,
            _ => null,
        };
        if (lineage is null)
        {
            GD.PushError($"Biome fixture: unknown lineage '{lineageName}'.");
            return;
        }

        CitizenProfile profile = NewFounderProfile(lineage.Value);
        var fixture = new CityWorld();
        HeroCreationResult hero = fixture.TryCreateHero(
            new HeroCreationRequest("Aster", profile, profile.Gender));
        if (!hero.IsSuccess)
        {
            GD.PushError($"Biome fixture: could not create founder: {hero.Outcome}.");
            return;
        }
        fixture.SeedStartingForests();
        fixture.SeedStartingOpportunities();
        controller.World.Restore(WorldPersistence.Capture(fixture));
        GetNode<AstralOnboardingView>("OnboardingView").Hide();
    }

    private void ShowEarlyGameResourcesForVisualRegression()
    {
        CityWorldController controller = GetNode<CityWorldController>("CityWorldController");
        bool profileCreated = CitizenProfile.TryCreate(
            LineageId.Ardhen,
            GenderId.Masculine,
            new[] { AptitudeId.Observation, AptitudeId.Empathy, AptitudeId.ManualPrecision },
            new[] { ProfessionFamilyId.Extraction, ProfessionFamilyId.MedicineCare, ProfessionFamilyId.ResearchEducation },
            ElementalAffinityId.Water,
            CombatStyleId.DefensiveSupport,
            new[] { WeaponPreferenceId.Polearm, WeaponPreferenceId.Shield },
            new[] { PersonalityTraitId.Patient, PersonalityTraitId.Protective, PersonalityTraitId.Reflective },
            PoliticalOrientationId.Communitarian,
            SpiritualPostureId.Contemplative,
            out CitizenProfile? profile,
            out string profileError);
        if (!profileCreated || profile is null)
        {
            GD.PushError($"Early-game resource fixture could not create a profile: {profileError}.");
            return;
        }

        var fixture = new CityWorld();
        HeroCreationResult heroResult = fixture.TryCreateHero(new HeroCreationRequest(
            "Aster",
            profile,
            profile.Gender));
        if (!heroResult.IsSuccess)
        {
            GD.PushError($"Early-game resource fixture could not create founder: {heroResult.Outcome}.");
            return;
        }
        fixture.SeedStartingForests();
        fixture.SeedStartingOpportunities();
        controller.World.Restore(WorldPersistence.Capture(fixture));
        GetNode<AstralOnboardingView>("OnboardingView").Hide();
        GetNode<MacroStreetLiveView>("GameUiShell/ScreenContent/MacroStreetLiveView")
            .ShowEarlyGameResourcesForVisualRegression();
    }

    /// <summary>
    /// Builds a city that actually holds a migrant, so the capture can show the
    /// per-citizen cube variation of <c>DEC-0019</c>.
    ///
    /// Hosting a prospect needs a Town Hall and accepting one needs free
    /// housing, and the default fixture world has neither: the first version of
    /// this fixture silently fell back to selecting the founder, whose cube sits
    /// on the bare vertex, and so photographed the exact thing the change was
    /// meant to move. Every precondition below therefore fails loudly instead.
    /// </summary>
    private void ShowMigrantCubeForVisualRegression()
    {
        CityWorldController controller = GetNode<CityWorldController>("CityWorldController");
        if (controller.World.Hero is not Citizen loadedFounder)
        {
            GD.PushError("Migrant cube fixture requires a loaded founder profile.");
            return;
        }

        var fixture = new CityWorld();
        HeroCreationResult heroResult = fixture.TryCreateHero(new HeroCreationRequest(
            "Aster",
            loadedFounder.Profile,
            loadedFounder.Profile.Gender));
        if (!heroResult.IsSuccess)
        {
            GD.PushError($"Migrant cube fixture could not create founder: {heroResult.Outcome}.");
            return;
        }
        fixture.SeedStartingForests();
        fixture.SeedStartingOpportunities();

        if (!TryCompleteForFixture(fixture, ConstructionKind.BasicShelter, out string shelterError))
        {
            GD.PushError($"Migrant cube fixture could not raise the shelter: {shelterError}.");
            return;
        }
        if (!TryCompleteForFixture(fixture, ConstructionKind.TownHall, out string townHallError))
        {
            GD.PushError($"Migrant cube fixture could not raise the town hall: {townHallError}.");
            return;
        }

        CityWorld.MigrantOutcome hosted = fixture.TryHostExpeditionProspect();
        if (hosted != CityWorld.MigrantOutcome.Success)
        {
            GD.PushError($"Migrant cube fixture could not host a prospect: {hosted}.");
            return;
        }
        CityWorld.MigrantResult accepted = fixture.TryAcceptPendingProspect();
        if (accepted.Outcome != CityWorld.MigrantOutcome.Success
            || accepted.MigrantId is not CitizenId migrantId)
        {
            GD.PushError($"Migrant cube fixture could not accept the prospect: {accepted.Outcome}.");
            return;
        }

        controller.World.Restore(WorldPersistence.Capture(fixture));
        GetNode<AstralOnboardingView>("OnboardingView").Hide();
        GetNode<MigrantPanel>("GameUiShell/ScreenContent/MigrantPanel")
            .ShowMigrantCubeForVisualRegression(migrantId);
    }

    /// <summary>
    /// Authorises a construction and forces it to completion, depositing enough
    /// of every rudimentary resource for the recipe gate to pass. Fixture-only:
    /// the player earns these, and the point here is the state after they did.
    /// </summary>
    private static bool TryCompleteForFixture(
        CityWorld fixture,
        ConstructionKind kind,
        out string error)
    {
        foreach (ResourceType resource in Enum.GetValues<ResourceType>())
        {
            fixture.Resources.DepositToCityInventory(resource, 40);
        }

        ConstructionAuthorizationResult authorization = fixture.TryAuthorizeConstruction(kind);
        if (!authorization.IsSuccess || authorization.ProjectId is not BuildingId projectId)
        {
            error = authorization.Outcome.ToString();
            return false;
        }

        ConstructionProject project = fixture.GetProject(projectId)!;
        project.Progress = project.RequiredWork;
        fixture.AdvanceWorldTick();
        fixture.ConfirmCitizenArrivedHome(fixture.Hero!.Id);
        error = string.Empty;
        return true;
    }

    private void ShowShelterResourcesForVisualRegression()
    {
        CityWorldController controller = GetNode<CityWorldController>("CityWorldController");
        Citizen? loadedFounder = controller.World.Hero;
        if (loadedFounder is null)
        {
            GD.PushError("Shelter resources visual fixture requires a loaded founder profile.");
            return;
        }

        var fixture = new CityWorld();
        HeroCreationResult heroResult = fixture.TryCreateHero(new HeroCreationRequest(
            "Aster",
            loadedFounder.Profile,
            loadedFounder.Profile.Gender));
        if (!heroResult.IsSuccess)
        {
            GD.PushError($"Shelter resources visual fixture could not create founder: {heroResult.Outcome}.");
            return;
        }
        fixture.SeedStartingForests();
        fixture.SeedStartingOpportunities();
        fixture.Resources.DepositToCityInventory(ResourceType.Wood, 4);
        ConstructionAuthorizationResult shelter =
            fixture.TryAuthorizeConstruction(ConstructionKind.BasicShelter);
        if (!shelter.IsSuccess || shelter.ProjectId is not BuildingId shelterId)
        {
            GD.PushError($"Shelter resources visual fixture could not authorize shelter: {shelter.Outcome}.");
            return;
        }
        ConstructionProject shelterProject = fixture.GetProject(shelterId)!;
        shelterProject.Progress = shelterProject.RequiredWork;
        fixture.AdvanceWorldTick();
        fixture.ConfirmCitizenArrivedHome(fixture.Hero!.Id);
        fixture.Resources.DepositToCityInventory(ResourceType.Branches, 3);
        fixture.Resources.DepositToCityInventory(ResourceType.PlantFiber, 2);
        fixture.Resources.DepositToCityInventory(ResourceType.SmallStone, 2);
        fixture.Resources.DepositToCityInventory(ResourceType.WildFood, 4);

        controller.World.Restore(WorldPersistence.Capture(fixture));
        GetNode<AstralOnboardingView>("OnboardingView").Hide();
        controller.SelectBuilding(shelterId);
        GetNode<BuildingDetailView>("GameUiShell/ScreenContent/BuildingDetailView")
            .ExpandShelterResourcesForVisualRegression();
    }

    /// <summary>
    /// Deterministic worst-case status composition: a three-digit day, six real
    /// resource types (including one reservation), and a populated Shelter.
    /// It changes only a capture-owned world and never writes the live slot.
    /// </summary>
    private void ShowTopStatusForVisualRegression(string locale)
    {
        CityWorldController controller = GetNode<CityWorldController>("CityWorldController");
        CitizenProfile profile = NewFounderProfile(LineageId.Kovari);
        var fixture = new CityWorld();
        HeroCreationResult heroResult = fixture.TryCreateHero(new HeroCreationRequest(
            "Aster",
            profile,
            profile.Gender));
        if (!heroResult.IsSuccess)
        {
            GD.PushError($"Top status fixture could not create founder: {heroResult.Outcome}.");
            return;
        }

        fixture.SeedStartingForests();
        fixture.SeedStartingOpportunities();
        fixture.Resources.DepositToCityInventory(ResourceType.Wood, 4);
        ConstructionAuthorizationResult shelter =
            fixture.TryAuthorizeConstruction(ConstructionKind.BasicShelter);
        if (!shelter.IsSuccess || shelter.ProjectId is not BuildingId shelterId)
        {
            GD.PushError($"Top status fixture could not authorize shelter: {shelter.Outcome}.");
            return;
        }
        ConstructionProject shelterProject = fixture.GetProject(shelterId)!;
        shelterProject.Progress = shelterProject.RequiredWork;
        fixture.AdvanceWorldTick();
        fixture.ConfirmCitizenArrivedHome(fixture.Hero!.Id);

        fixture.Resources.DepositToCityInventory(ResourceType.Food, 6);
        fixture.Resources.DepositToCityInventory(ResourceType.Wood, 4);
        fixture.Resources.DepositToCityInventory(ResourceType.Branches, 3);
        fixture.Resources.DepositToCityInventory(ResourceType.PlantFiber, 2);
        fixture.Resources.DepositToCityInventory(ResourceType.SmallStone, 1);
        fixture.Resources.DepositToCityInventory(ResourceType.WildFood, 4);
        fixture.Resources.TryReserve(
            ResourceType.Branches,
            1,
            new ResourceReservationOwner(ResourceReservationOwnerKind.ConstructionProject, 99),
            out _);
        fixture.RegisterCitizen(new Citizen(new CitizenId(2), "Nara", 22, profile));
        fixture.RegisterCitizen(new Citizen(new CitizenId(3), "Ivo", 33, profile));
        fixture.ConcludeFirstNightForFixtures();

        WorldSave save = WorldPersistence.Capture(fixture);
        save.CurrentTick = (123 - 1) * GameClock.TicksPerInGameDay
            + GameClock.TicksPerInGameDay / 2;
        controller.World.Restore(save);
        LineageThemeRegistry.ActiveLineage = LineageThemeRegistry.IdOf(profile.Lineage);
        GetNode<LocaleManager>("/root/LocaleManager")
            .SetLocaleForVisualRegression(locale);
        GetNode<AstralOnboardingView>("OnboardingView").Hide();
        GetNode<CityStatusPanel>("GameUiShell/CityStatusPanel").Refresh(controller);
    }

    /// <summary>
    /// Exercises the persistent city summary with real ledger, housing and
    /// project state, plus the existing typed selection-inspector path.
    /// </summary>
    private void ShowCitySummaryForVisualRegression(string locale, bool blocked)
    {
        ShowTopStatusForVisualRegression(locale);
        CityWorldController controller = GetNode<CityWorldController>("CityWorldController");
        ConstructionAuthorizationResult authorization =
            controller.TryAuthorizeConstruction(ConstructionKind.Farm);
        if (!authorization.IsSuccess || authorization.ProjectId is not BuildingId projectId)
        {
            GD.PushError(
                $"City summary fixture could not authorize farm: {authorization.Outcome}.");
            return;
        }

        ConstructionProject project = controller.World.GetProject(projectId)!;
        project.Progress = project.RequiredWork / 3;
        if (!blocked && controller.World.Hero is Citizen founder)
        {
            controller.TryAssignCitizenToProject(projectId, founder.Id);
            controller.World.ConfirmCitizenArrivedAtAssignment(founder.Id, projectId);
            controller.World.AdvanceWorldTick();
        }

        GetNode<ContextInspector>("GameUiShell/ScreenContent/ContextInspector")
            .ShowSelection(
                ResourceLoader.Load<Texture2D>(IconPaths.User),
                "Inspector",
                UiText.Get("ui.city_summary.inspector_fixture_detail"));
        GetNode<CitySummaryPanel>("GameUiShell/ScreenContent/CitySummaryPanel")
            .Refresh(controller.GetCityStatusSnapshot());
        GetNode<CityStatusPanel>("GameUiShell/CityStatusPanel").Refresh(controller);
    }

    /// <summary>
    /// Composes the CitySummary panel with a near-empty food stock so the
    /// food-horizon warning glyph fires and the at-risk metric reads.
    /// </summary>
    private void ShowCitySummaryLowFoodForVisualRegression(string locale)
    {
        ShowTopStatusForVisualRegression(locale);
        CityWorldController controller = GetNode<CityWorldController>("CityWorldController");
        CityWorld world = controller.World;
        Citizen? hero = world.Hero;
        if (hero is null)
        {
            GD.PushError("City-summary low-food fixture requires a loaded hero.");
            return;
        }
        // Drain Food so the horizon falls under one day of rations.
        while (world.FoodStock > 0)
        {
            world.Resources.TryConsume(ResourceType.Food, 1);
        }
        world.Resources.DepositToCityInventory(ResourceType.WildFood, 1);

        GetNode<CitySummaryPanel>("GameUiShell/ScreenContent/CitySummaryPanel")
            .Refresh(controller.GetCityStatusSnapshot());
        GetNode<CityStatusPanel>("GameUiShell/CityStatusPanel").Refresh(controller);
    }

    /// <summary>
    /// Composes the CitySummary panel with citizens at full housing capacity
    /// so the housing progress bar renders full and the row count matches.
    /// </summary>
    private void ShowCitySummaryHousingFullForVisualRegression(string locale)
    {
        ShowTopStatusForVisualRegression(locale);
        CityWorldController controller = GetNode<CityWorldController>("CityWorldController");
        CityWorld world = controller.World;
        Citizen? hero = world.Hero;
        if (hero is null)
        {
            GD.PushError("City-summary housing-full fixture requires a loaded hero.");
            return;
        }
        int capacity = world.HousingCapacity;
        if (capacity > 0)
        {
            CitizenProfile profile = hero.Profile;
            bool any = world.Citizens.Count > 0;
            int nextId = any
                ? world.Citizens.Keys.Max(id => id.Value) + 1
                : 900;
            for (int i = world.Citizens.Count; i < capacity; i++)
            {
                world.RegisterCitizen(new Citizen(
                    new CitizenId(nextId++),
                    $"Resident {i + 1}",
                    appearanceSeed: nextId * 11,
                    profile: profile));
            }
        }

        GetNode<CitySummaryPanel>("GameUiShell/ScreenContent/CitySummaryPanel")
            .Refresh(controller.GetCityStatusSnapshot());
        GetNode<CityStatusPanel>("GameUiShell/CityStatusPanel").Refresh(controller);
    }

    /// <summary>
    /// Composes the CitySummary panel with no active construction so the
    /// "No active construction" empty-state caption renders.
    /// </summary>
    private void ShowCitySummaryNoConstructionForVisualRegression(string locale)
    {
        ShowTopStatusForVisualRegression(locale);
        CityWorldController controller = GetNode<CityWorldController>("CityWorldController");

        GetNode<CitySummaryPanel>("GameUiShell/ScreenContent/CitySummaryPanel")
            .Refresh(controller.GetCityStatusSnapshot());
        GetNode<CityStatusPanel>("GameUiShell/CityStatusPanel").Refresh(controller);
    }

    private enum MacroHudFixtureState
    {
        Default,
        Selection,
        ActiveConstruction,
        ActiveExpedition,
    }

    /// <summary>
    /// Composes the final authored macro HUD from existing truthful fixtures.
    /// It adds no production-only state: construction and expedition variants
    /// still use the same domain commands as their focused fixture families.
    /// </summary>
    private void ShowMacroHudForVisualRegression(MacroHudFixtureState state)
    {
        ShowTopStatusForVisualRegression("en");
        MacroStreetLiveView macro = GetNode<MacroStreetLiveView>(
            "GameUiShell/ScreenContent/MacroStreetLiveView");
        macro.ShowEarlyGameResourcesForVisualRegression();

        if (state == MacroHudFixtureState.ActiveConstruction)
        {
            ShowCitySummaryForVisualRegression("en", blocked: false);
            GetNode<ContextInspector>("GameUiShell/ScreenContent/ContextInspector").Hide();
            return;
        }

        if (state == MacroHudFixtureState.ActiveExpedition)
        {
            ShowExpeditionRailForVisualRegression(ExpeditionRailFixtureState.Outbound);
        }

        CityWorldController controller = GetNode<CityWorldController>("CityWorldController");
        GetNode<CitySummaryPanel>("GameUiShell/ScreenContent/CitySummaryPanel")
            .Refresh(controller.GetCityStatusSnapshot());
        GetNode<ExpeditionRail>("GameUiShell/ScreenContent/ExpeditionRail").Refresh();

        ContextInspector inspector = GetNode<ContextInspector>(
            "GameUiShell/ScreenContent/ContextInspector");
        if (state == MacroHudFixtureState.Selection)
        {
            inspector.ShowSelection(
                ResourceLoader.Load<Texture2D>(IconPaths.User),
                UiText.Get("ui.city_summary.city"),
                UiText.Get("ui.city_summary.inspector_fixture_detail"));
        }
        else
        {
            inspector.Hide();
        }
    }

    private void ShowCultivationForVisualRegression()
    {
        CityWorldController controller = GetNode<CityWorldController>("CityWorldController");
        Citizen? loadedFounder = controller.World.Hero;
        if (loadedFounder is null)
        {
            GD.PushError("Cultivation visual fixture requires a loaded founder profile.");
            return;
        }

        var fixture = new CityWorld();
        HeroCreationResult heroResult = fixture.TryCreateHero(new HeroCreationRequest(
            "Aster",
            loadedFounder.Profile,
            loadedFounder.Profile.Gender));
        if (!heroResult.IsSuccess)
        {
            GD.PushError($"Cultivation visual fixture could not create founder: {heroResult.Outcome}.");
            return;
        }

        fixture.SeedStartingForests();
        fixture.SeedStartingOpportunities();
        fixture.Resources.DepositToCityInventory(ResourceType.Wood, 4);
        ConstructionAuthorizationResult shelter =
            fixture.TryAuthorizeConstruction(ConstructionKind.BasicShelter);
        if (!shelter.IsSuccess || shelter.ProjectId is not BuildingId shelterId)
        {
            GD.PushError($"Cultivation visual fixture could not authorize shelter: {shelter.Outcome}.");
            return;
        }
        ConstructionProject shelterProject = fixture.GetProject(shelterId)!;
        shelterProject.Progress = shelterProject.RequiredWork;
        fixture.AdvanceWorldTick();
        fixture.ConfirmCitizenArrivedHome(fixture.Hero!.Id);

        fixture.Resources.DepositToCityInventory(ResourceType.Branches, 1);
        fixture.Resources.DepositToCityInventory(ResourceType.SmallStone, 1);
        fixture.Resources.DepositToCityInventory(ResourceType.WildFood, 8);
        ConstructionAuthorizationResult cultivation =
            fixture.TryAuthorizeConstruction(ConstructionKind.CultivationSite);
        if (!cultivation.IsSuccess || cultivation.ProjectId is not BuildingId cultivationId)
        {
            GD.PushError(
                $"Cultivation visual fixture could not authorize site: {cultivation.Outcome}.");
            return;
        }
        ConstructionProject cultivationProject = fixture.GetProject(cultivationId)!;
        cultivationProject.Progress = cultivationProject.RequiredWork;
        fixture.AdvanceWorldTick();
        while (!GameClock.IsDaytime(fixture.CurrentTick)) fixture.AdvanceWorldTick();

        controller.World.Restore(WorldPersistence.Capture(fixture));
        GetNode<AstralOnboardingView>("OnboardingView").Hide();
        GetNode<CityStatusPanel>("GameUiShell/CityStatusPanel").Refresh(controller);
        GetNode<MacroStreetLiveView>("GameUiShell/ScreenContent/MacroStreetLiveView")
            .ShowEarlyGameResourcesForVisualRegression();
    }

    private void ShowCitizenTravelForVisualRegression()
    {
        CityWorldController controller = GetNode<CityWorldController>("CityWorldController");
        MacroStreetLiveView city = GetNode<MacroStreetLiveView>(
            "GameUiShell/ScreenContent/MacroStreetLiveView");
        CityWorld world = controller.World;
        if (world.Hero is not Citizen founder)
        {
            GD.PushError("Citizen travel fixture requires a founded city.");
            return;
        }

        while (!GameClock.IsDaytime(world.CurrentTick)) world.AdvanceWorldTick();
        Building? workplace = world.Buildings.Values
            .Where(building => building.Kind is BuildingKind.Quarry or BuildingKind.Farm)
            .OrderByDescending(building => building.AssignedCitizenIds.Count < building.WorkerCapacity)
            .ThenBy(building => building.Id.Value)
            .FirstOrDefault();
        if (workplace is null || world.PrimaryHome is null)
        {
            GD.PushError("Citizen travel fixture requires a Shelter and a Farm or Quarry.");
            return;
        }
        if (workplace.AssignedCitizenIds.Count >= workplace.WorkerCapacity)
        {
            CitizenId released = workplace.AssignedCitizenIds.Last();
            world.TryUnassignCitizen(workplace.Id, released);
        }
        if (workplace.Stock > 0) workplace.TryConsumeStock(workplace.Stock);
        workplace.ConfigureProductionPolicy(
            enabled: true,
            minStock: 0,
            maxStock: workplace.StorageCapacity,
            priority: workplace.Priority);

        int nextId = world.Citizens.Keys.Max(id => id.Value) + 1;
        var traveller = new Citizen(
            new CitizenId(nextId),
            "Travel proof",
            appearanceSeed: nextId * 11,
            profile: founder.Profile);
        world.RegisterCitizen(traveller);
        AssignmentResult assignment = controller.TryAssignCitizen(workplace.Id, traveller.Id);
        bool activeRoute = city.HasActiveCitizenJourneyForVisualRegression(traveller.Id);
        if (!assignment.IsSuccess
            || traveller.CurrentLocation != CitizenLocation.InTransit
            || !activeRoute)
        {
            GD.PushError(
                $"Citizen travel fixture failed: assignment={assignment.Outcome}, " +
                $"location={traveller.CurrentLocation}, activeRoute={activeRoute}.");
            return;
        }
        GD.Print(
            $"Citizen travel fixture passed: citizen={traveller.Id.Value}, " +
            $"destination={workplace.Id.Value}, activeRoute=true.");
    }

    private async void ValidateModalLayoutAndClosePaths()
    {
        GD.Print("Modal layout/close fixture started.");
        Control city = GetNode<Control>("GameUiShell/ScreenContent");
        ModalHost host = GetNode<ModalHost>(
            "GameUiShell/ScreenContent/ModalHost");
        ExpeditionPanel expedition = GetNode<ExpeditionPanel>(
            "GameUiShell/ScreenContent/Center/ExpeditionPanel");
        ConstructionPanel construction = GetNode<ConstructionPanel>(
            "GameUiShell/ScreenContent/Center/ConstructionPanel");

        expedition.Open();
        await ToSignal(GetTree().CreateTimer(0.2), SceneTreeTimer.SignalName.Timeout);
        ValidateContained("ExpeditionPanel", expedition, city);
        expedition.Close();
        await ToSignal(GetTree().CreateTimer(0.2), SceneTreeTimer.SignalName.Timeout);

        GetNode<MacroStreetLiveView>("GameUiShell/ScreenContent/MacroStreetLiveView")
            .ShowConstructionForVisualRegression(placement: false);
        await ToSignal(GetTree().CreateTimer(0.2), SceneTreeTimer.SignalName.Timeout);
        ValidateContained("ConstructionPanel", construction, city);
        construction.PressHeaderCloseForVisualRegression();
        await ToSignal(GetTree().CreateTimer(0.2), SceneTreeTimer.SignalName.Timeout);
        if (host.IsOpen || construction.Visible)
        {
            GD.PushError("Modal close fixture: construction X did not close ModalHost.");
        }
        else
        {
            GD.Print("Modal layout/close fixture passed.");
        }
    }

    private static void ValidateContained(
        string label,
        Control content,
        Control parent)
    {
        Rect2 parentRect = parent.GetGlobalRect();
        Rect2 contentRect = content.GetGlobalRect();
        if (!parentRect.Encloses(contentRect))
        {
            GD.PushError(
                $"{label} escaped macro viewport. Parent={parentRect}, content={contentRect}.");
        }
    }

    private void PinTimeOfDayForVisualRegression(double dayFraction)
    {
        GetNode<TimeOfDayFilter>("GameUiShell/ScreenContent/TimeOfDayFilter")
            .PinDayFractionForVisualRegression(dayFraction);
    }

    private void ShowFounderArrivalForVisualRegression()
    {
        CityWorldController controller = GetNode<CityWorldController>("CityWorldController");
        MacroStreetLiveView city = GetNode<MacroStreetLiveView>(
            "GameUiShell/ScreenContent/MacroStreetLiveView");
        if (controller.World.Hero is not Citizen founder) return;
        city.PrepareFounderArrival();
        var arrival = new FounderArrivalSequence();
        AddChild(arrival);
        arrival.Begin(founder, city.GetFoundingArrivalGlobalPosition());
    }

    private enum ExpeditionFixtureState { Idle, Active, Returned }
    private enum ExpeditionRailFixtureState
    {
        Empty,
        Outbound,
        Encounter,
        Objective,
        Returning,
        Resolved,
        Cancelled,
    }

    private enum ExpeditionLiveFixtureState
    {
        Travel,
        Encounter,
        BasicAttack,
        SkillReady,
        SkillCooldown,
        AutoOn,
        AutoOff,
        Ranged,
        Knockback,
        Objective,
        Return,
    }

    private void ShowHeroIncorporationForVisualRegression()
    {
        CityWorldController controller = GetNode<CityWorldController>("CityWorldController");
        if (controller.World.Hero is not Citizen founder) return;
        if (controller.World.Citizens.Values.All(citizen => citizen.IsHero))
        {
            int nextId = controller.World.Citizens.Keys.Max(id => id.Value) + 1;
            controller.World.RegisterCitizen(new Citizen(
                new CitizenId(nextId),
                "Expedition candidate",
                appearanceSeed: nextId * 11,
                profile: founder.Profile));
        }
        ShowExpeditionForVisualRegression(ExpeditionFixtureState.Idle);
    }

    private void ShowWoundRecoveryForVisualRegression()
    {
        CityWorldController controller = GetNode<CityWorldController>("CityWorldController");
        if (controller.World.Hero is not Citizen founder) return;
        int nextId = controller.World.Citizens.Keys.Max(id => id.Value) + 1;
        var patient = new Citizen(
            new CitizenId(nextId),
            "Tamara",
            appearanceSeed: nextId * 11,
            profile: founder.Profile);
        controller.World.RegisterCitizen(patient);
        controller.World.TryIncorporateHero(patient.Id);
        WorldEvent woundEvent = controller.World.Log.Record(
            controller.World.CurrentTick,
            WorldEventKind.WoundSustained,
            WorldEventSubject.Citizen(patient.Id, patient.Name),
            (int)WoundSeverity.Moderate);
        patient.SustainWound(WoundSeverity.Moderate, woundEvent.Id);
        controller.World.Resources.DepositToCityInventory(ResourceType.Food, 2);
        GetNode<ExpeditionPanel>("GameUiShell/ScreenContent/Center/ExpeditionPanel")
            .ShowWoundRecoveryForVisualRegression();
    }

    private void ShowWorldStatusTreatmentForVisualRegression()
    {
        CityWorldController controller = GetNode<CityWorldController>("CityWorldController");
        CityWorld world = controller.World;
        if (world.Hero is not Citizen founder || world.PrimaryHome is null) return;

        int nextId = world.Citizens.Keys.Max(id => id.Value) + 1;
        var patient = new Citizen(
            new CitizenId(nextId),
            "Tamara",
            appearanceSeed: nextId * 11,
            profile: founder.Profile);
        world.RegisterCitizen(patient);
        world.TryIncorporateHero(patient.Id);
        WorldEvent woundEvent = world.Log.Record(
            world.CurrentTick,
            WorldEventKind.WoundSustained,
            WorldEventSubject.Citizen(patient.Id, patient.Name),
            (int)WoundSeverity.Moderate);
        patient.SustainWound(WoundSeverity.Moderate, woundEvent.Id);
        world.Resources.DepositToCityInventory(ResourceType.Food, WoundRules.ModerateFoodCost);
        if (!world.TryBeginWoundRecovery(patient.Id).IsSuccess) return;

        GetNode<MacroStreetLiveView>("GameUiShell/ScreenContent/MacroStreetLiveView")
            .ShowCitizenStatusForVisualRegression(patient.Id);
    }

    private void ShowCitizenClickSummaryForVisualRegression()
    {
        // The bubble fixture above already drives the hover path; this one
        // exercises the dedicated click path (TryClick → SelectCitizen →
        // ContextInspector) so the regression matrix proves both the
        // pointer overlay and the at-a-glance summary arrive when the
        // player clicks a citizen — not just when the macro view paints
        // the bubble for a known citizen by hand.
        CityWorldController controller = GetNode<CityWorldController>("CityWorldController");
        CityWorld world = controller.World;
        if (world.Hero is not Citizen founder || world.PrimaryHome is null) return;

        int nextId = world.Citizens.Keys.Max(id => id.Value) + 1;
        var inspector = new Citizen(
            new CitizenId(nextId),
            "Inspector",
            appearanceSeed: nextId * 11,
            profile: founder.Profile);
        world.RegisterCitizen(inspector);
        MacroStreetLiveView city = GetNode<MacroStreetLiveView>(
            "GameUiShell/ScreenContent/MacroStreetLiveView");
        city.TriggerCitizenClickForVisualRegression(inspector.Id);
    }

    private void ShowExpeditionForVisualRegression(ExpeditionFixtureState state)
    {
        CityWorldController controller = GetNode<CityWorldController>("CityWorldController");
        ExpeditionPanel panel = GetNode<ExpeditionPanel>(
            "GameUiShell/ScreenContent/Center/ExpeditionPanel");
        if (controller.World.Hero?.CurrentAssignment is BuildingId assignment)
        {
            AssignmentResult result = controller.World.TryUnassignCitizen(assignment, controller.World.Hero.Id);
            if (!result.IsSuccess) controller.World.TryUnassignFromProject(assignment, controller.World.Hero.Id);
        }
        if (controller.World.Resources.Available(ResourceType.Wood) < 1)
        {
            controller.World.Resources.DepositToCityInventory(ResourceType.Wood, 1);
        }
        foreach (Expedition expedition in controller.World.Expeditions.Values)
        {
            if (expedition.Status == ExpeditionStatus.Active)
            {
                controller.CancelExpedition(expedition.Id);
                break;
            }
        }
        if (state == ExpeditionFixtureState.Idle)
        {
            OpenAndValidateExpeditionPanel(panel);
            return;
        }
        ExpeditionRequest request = ExpeditionRequest.Reconnaissance(controller.World.Hero!.Id);
        if (state == ExpeditionFixtureState.Returned) request = request with { DurationTicks = 1 };
        if (!controller.StartExpedition(request).IsSuccess) return;
        if (state == ExpeditionFixtureState.Returned) controller.World.AdvanceWorldTick();
        OpenAndValidateExpeditionPanel(panel);
    }

    private void OpenAndValidateExpeditionPanel(ExpeditionPanel panel)
    {
        panel.Open();
        GetTree().CreateTimer(0.2).Timeout += ValidateExpeditionPanelContained;
    }

    private void ValidateExpeditionPanelContained()
    {
        Control screen = GetNode<Control>("GameUiShell/ScreenContent");
        ExpeditionPanel panel = GetNode<ExpeditionPanel>(
            "GameUiShell/ScreenContent/Center/ExpeditionPanel");
        Rect2 screenRect = screen.GetGlobalRect();
        Rect2 panelRect = panel.GetGlobalRect();
        if (!panel.IsVisibleInTree() || !screenRect.Encloses(panelRect))
        {
            GD.PushError(
                $"[WOG-EXPEDITION-PANEL] panel escaped screen; "
                + $"visible={panel.IsVisibleInTree()}, screen={screenRect}, panel={panelRect}.");
            return;
        }
        GD.Print(
            $"[WOG-EXPEDITION-PANEL] visible and contained; "
            + $"screen={screenRect}, rect={panelRect}.");
    }

    private void ShowExpeditionRailForVisualRegression(
        ExpeditionRailFixtureState state,
        string? locale = null,
        string? displayName = null,
        int durationTicks = 8,
        bool prepareObservableCombat = false)
    {
        if (locale is not null)
        {
            GetNode<LocaleManager>("/root/LocaleManager")
                .SetLocaleForVisualRegression(locale);
        }
        CityWorldController controller = GetNode<CityWorldController>("CityWorldController");
        SeedHermeticFounderForExpeditionRailFixture(controller);
        CityWorld world = controller.World;
        ExpeditionRail rail = GetNode<ExpeditionRail>(
            "GameUiShell/ScreenContent/ExpeditionRail");

        if (world.Hero?.CurrentAssignment is BuildingId assignment)
        {
            AssignmentResult unassigned = world.TryUnassignCitizen(assignment, world.Hero.Id);
            if (!unassigned.IsSuccess) world.TryUnassignFromProject(assignment, world.Hero.Id);
        }
        foreach (Expedition expedition in world.Expeditions.Values.ToArray())
        {
            if (expedition.Status == ExpeditionStatus.Active) controller.CancelExpedition(expedition.Id);
        }
        if (state == ExpeditionRailFixtureState.Empty)
        {
            rail.Refresh();
            return;
        }
        if (world.Resources.Available(ResourceType.Wood) < 1)
        {
            world.Resources.DepositToCityInventory(ResourceType.Wood, 1);
        }
        _ = prepareObservableCombat;

        ExpeditionRequest request = ExpeditionRequest.Reconnaissance(world.Hero!.Id) with
        {
            DurationTicks = durationTicks,
            DisplayName = displayName ?? "Reconnaissance",
        };
        ExpeditionStartResult started = controller.StartExpedition(request);
        if (!started.IsSuccess)
        {
            // Never return silently here. A fixture that cannot seed its
            // expedition still renders a perfectly valid-looking frame — of
            // the macro view, with no cards and no VER — and the capture
            // harness exits 0 and writes it. A human signing off that PNG
            // approves the wrong screen. Naming the outcome turns an
            // invisible failure into a one-line diagnosis.
            GD.PushError(
                "[WOG-EXPEDITION-RAIL-FIXTURE] StartExpedition failed: "
                + $"outcome={started.Outcome}, "
                + $"unavailableReason={started.UnavailableReason}, "
                + $"tick={controller.World.CurrentTick}.");
            return;
        }

        int ticks = state switch
        {
            ExpeditionRailFixtureState.Encounter => durationTicks / 4,
            ExpeditionRailFixtureState.Objective => durationTicks / 2,
            ExpeditionRailFixtureState.Returning => durationTicks * 3 / 4,
            ExpeditionRailFixtureState.Resolved => durationTicks,
            _ => 0,
        };
        for (int i = 0; i < ticks; i++) world.AdvanceWorldTick();
        if (state == ExpeditionRailFixtureState.Cancelled)
        {
            controller.CancelExpedition(started.ExpeditionId!.Value);
        }
        rail.Refresh();
    }

    private void ShowExpeditionLiveForVisualRegression(
        ExpeditionLiveFixtureState state,
        bool exitWithCancel = false)
    {
        _expeditionLiveEscapeFixture = exitWithCancel;
        _expeditionLiveFixtureState = state;
        CityWorldController controller = GetNode<CityWorldController>("CityWorldController");
        if (!PrepareSpiritTrailVisualFixture(controller, out ExpeditionId expeditionId)) return;
        AdvanceSpiritTrailVisualFixture(controller.World, expeditionId, state);
        controller.SetSimulationSpeed(CityWorldController.SpeedChoice.Fast);
        ExpeditionLiveView liveView = GetNode<ExpeditionLiveView>(
            "GameUiShell/ScreenContent/ExpeditionLiveView");
        liveView.UseStableFounderLabelForVisualRegression();
        ExpeditionRail rail = GetNode<ExpeditionRail>(
            "GameUiShell/ScreenContent/ExpeditionRail");
        rail.Refresh();
        if (exitWithCancel)
        {
            if (!controller.SelectExpeditionLive(expeditionId))
            {
                GD.PushError("[WOG-EXPEDITION-LIVE-ESC] could not select the prepared Spirit Trail.");
                return;
            }
            GetTree().CreateTimer(0.15).Timeout += ValidateExpeditionLiveForVisualRegression;
            return;
        }
        rail.SetExpandedForVisualRegression(expanded: false);
        rail.SetExpandedForVisualRegression(expanded: true);
        GetTree().CreateTimer(0.1).Timeout += () =>
        {
            if (rail.FirstViewButton is null)
            {
                GD.PushError("[WOG-EXPEDITION-LIVE] active Spirit Trail has no View action.");
                return;
            }
            SendPointerClickForVisualRegression(rail.FirstViewButton);
            GetTree().CreateTimer(0.15).Timeout += ValidateExpeditionLiveForVisualRegression;
        };
    }

    private static bool PrepareSpiritTrailVisualFixture(
        CityWorldController controller,
        out ExpeditionId expeditionId)
    {
        expeditionId = default;
        CitizenProfile profile = NewFounderProfile(LineageId.Kovari);
        var fixture = new CityWorld();
        HeroCreationResult heroResult = fixture.TryCreateHero(new HeroCreationRequest(
            "Aster",
            profile,
            profile.Gender));
        if (!heroResult.IsSuccess)
        {
            GD.PushError($"Spirit Trail fixture could not create Founder: {heroResult.Outcome}.");
            return false;
        }
        fixture.SeedStartingForests();
        fixture.SeedStartingOpportunities();
        if (!DriveFirstNightToDawnForVisualFixture(fixture)) return false;
        ResourceOpportunity? opportunity = fixture.ResourceOpportunities.Values.FirstOrDefault(item =>
            item.Kind == ResourceOpportunityKind.SpiritTrailSearch);
        if (opportunity is null)
        {
            GD.PushError("Spirit Trail fixture did not surface its opportunity at dawn.");
            return false;
        }
        ExpeditionStartResult started = fixture.StartResourceExpedition(
            opportunity.Id,
            [fixture.Hero!.Id],
            ExpeditionRetreatPosture.ContinueAfterSetback);
        if (!started.IsSuccess || started.ExpeditionId is not ExpeditionId startedId)
        {
            GD.PushError($"Spirit Trail fixture could not dispatch: {started.Outcome}.");
            return false;
        }
        controller.World.Restore(WorldPersistence.Capture(fixture));
        expeditionId = startedId;
        return true;
    }

    private static bool DriveFirstNightToDawnForVisualFixture(CityWorld world)
    {
        FirstNightState night = world.FirstNight!;
        ConstructionProject? project = null;
        int safety = 32;
        while (night.Stage < FirstNightStage.Sleeping && safety-- > 0)
        {
            if (FirstNightRules.WaitsForModule(night.Stage))
            {
                FoundingSiteModule module = FirstNightRules.ModuleFor(night.Stage);
                foreach (RecipeInput input in FoundingSiteRules.InputsFor(module))
                {
                    world.Resources.DepositToCityInventory(input.Resource, input.Amount);
                }
                ConstructionAuthorizationResult authorization = project is null
                    ? world.TryAuthorizeConstruction(ConstructionKind.FoundingSite)
                    : world.TryAuthorizeFoundingSiteModule(project.Id, module);
                if (!authorization.IsSuccess)
                {
                    GD.PushError($"Spirit Trail fixture stalled at {night.Stage}: {authorization.Outcome}.");
                    return false;
                }
                project ??= world.Projects.Values.Single();
                project.Progress = project.RequiredWork;
                world.AdvanceWorldTick();
            }
            else if (!world.TryCloseFirstNightDialogue())
            {
                GD.PushError($"Spirit Trail fixture dialogue stalled at {night.Stage}.");
                return false;
            }
        }
        return night.Stage == FirstNightStage.Sleeping && world.TryCloseFirstNightDialogue();
    }

    private static void AdvanceSpiritTrailVisualFixture(
        CityWorld world,
        ExpeditionId expeditionId,
        ExpeditionLiveFixtureState state)
    {
        Expedition expedition = world.Expeditions[expeditionId];
        if (state == ExpeditionLiveFixtureState.Travel) return;
        AdvanceUntilVisualFixture(world, () => world.GetCombatSessionSnapshot(expeditionId) is not null);
        if (state == ExpeditionLiveFixtureState.AutoOff)
            world.SetCombatAutoSkillsEnabled(expeditionId, false);
        if (state == ExpeditionLiveFixtureState.SkillCooldown)
        {
            world.SetCombatAutoSkillsEnabled(expeditionId, false);
            world.TryActivateMemberSkill(expeditionId, 0);
            AdvanceUntilVisualFixture(world, () => world.GetCombatSessionSnapshot(expeditionId)!
                .MemberSkills.Any(skill => skill.Remaining > 0));
        }
        if (state == ExpeditionLiveFixtureState.BasicAttack)
        {
            world.SetCombatAutoSkillsEnabled(expeditionId, false);
            AdvanceUntilVisualFixture(world, () => world.GetCombatSessionSnapshot(expeditionId)!
                .Log.Any(entry => entry.Kind == CombatLogKind.BasicAttackResolved));
        }
        if (state == ExpeditionLiveFixtureState.Knockback)
        {
            AdvanceUntilVisualFixture(world, () => world.GetCombatSessionSnapshot(expeditionId)!
                .Log.Any(entry => entry.Kind == CombatLogKind.KnockbackApplied));
        }
        if (state is ExpeditionLiveFixtureState.Objective or ExpeditionLiveFixtureState.Return)
            AdvanceUntilVisualFixture(world, () => expedition.EncounterOutcome.HasValue);
        if (state == ExpeditionLiveFixtureState.Return)
            AdvanceUntilVisualFixture(world, () => expedition.Phase == ExpeditionPhase.Returning);
    }

    private static void AdvanceUntilVisualFixture(CityWorld world, Func<bool> condition)
    {
        int safety = ExpeditionTiming.SpiritTrailDurationTicks;
        while (!condition() && safety-- > 0) world.AdvanceWorldTick();
        if (safety <= 0) GD.PushError("Spirit Trail visual fixture missed its requested state.");
    }

    private void ValidateExpeditionLiveForVisualRegression()
    {
        CityWorldController controller = GetNode<CityWorldController>("CityWorldController");
        ExpeditionLiveView liveView = GetNode<ExpeditionLiveView>(
            "GameUiShell/ScreenContent/ExpeditionLiveView");
        MacroStreetLiveView macro = GetNode<MacroStreetLiveView>(
            "GameUiShell/ScreenContent/MacroStreetLiveView");
        CombatSessionSnapshot? combat = liveView.PresentedExpeditionId is ExpeditionId expeditionId
            ? controller.World.GetCombatSessionSnapshot(expeditionId)
            : null;
        bool expectsCombat = _expeditionLiveFixtureState is not ExpeditionLiveFixtureState.Travel
            and not ExpeditionLiveFixtureState.Objective
            and not ExpeditionLiveFixtureState.Return;
        bool expectsAuto = _expeditionLiveFixtureState is not ExpeditionLiveFixtureState.AutoOff
            and not ExpeditionLiveFixtureState.BasicAttack
            and not ExpeditionLiveFixtureState.SkillCooldown;
        bool passed = liveView.Visible
            && liveView.PresentedExpeditionId.HasValue
            && !macro.Visible
            && !GetNode<CitySummaryPanel>("GameUiShell/ScreenContent/CitySummaryPanel").Visible
            && !GetNode<ExpeditionRail>("GameUiShell/ScreenContent/ExpeditionRail").Visible
            && !GetNode<PrimaryNavDock>("GameUiShell/ScreenContent/PrimaryNavDock").Visible
            && !GetNode<ContextInspector>("GameUiShell/ScreenContent/ContextInspector").Visible
            && !GetNode<ActionDock>("GameUiShell/ScreenContent/ActionDock").Visible
            && GetNode<CityStatusPanel>("GameUiShell/CityStatusPanel").Visible
            && GetNode<CityStatusPanel>("GameUiShell/CityStatusPanel").SpeedButton.IsVisibleInTree()
            && GetViewport().GuiGetFocusOwner() == liveView.BackButton
            && liveView.HasReferenceLayout
            && (expectsCombat
                ? combat is { Active: true }
                    && combat.AutoSkillsEnabled == expectsAuto
                    && !liveView.AutoButton.Disabled
                : (combat is null || !combat.Active) && liveView.AutoButton.Disabled)
            && controller.CurrentSpeed == CityWorldController.SpeedChoice.Fast;
        if (!passed)
        {
            GD.PushError(
                $"[WOG-EXPEDITION-LIVE] routing failed; live={liveView.Visible}, "
                + $"macro={macro.Visible}, selection={controller.CurrentSelection}, "
                + $"speed={controller.CurrentSpeed}, layout={liveView.HasReferenceLayout}, "
                + $"combat={combat?.Outcome}, active={combat?.Active}, auto={combat?.AutoSkillsEnabled}, "
                + $"expectedCombat={expectsCombat}, expectedAuto={expectsAuto}, "
                + $"focus={GetViewport().GuiGetFocusOwner()?.Name}, "
                + $"autoDisabled={liveView.AutoButton.Disabled}, "
                + $"bounds={liveView.ReferenceLayoutReport}.");
            return;
        }
        GD.Print(
            $"[WOG-EXPEDITION-LIVE] real Spirit Trail {_expeditionLiveFixtureState} at global 2x; "
            + $"step={combat?.Step}, enemies={combat?.EnemyCount}, auto={combat?.AutoSkillsEnabled}; "
            + "stage=800x488, sides=228/224, squad=441, skills=456.");
        if (!_expeditionLiveEscapeFixture) return;

        Input.ParseInputEvent(new InputEventAction { Action = "ui_cancel", Pressed = true });
        Input.ParseInputEvent(new InputEventAction { Action = "ui_cancel", Pressed = false });
        GetTree().CreateTimer(0.15).Timeout += ValidateExpeditionLiveEscapeForVisualRegression;
    }

    private void ValidateExpeditionLiveEscapeForVisualRegression()
    {
        CityWorldController controller = GetNode<CityWorldController>("CityWorldController");
        bool passed = controller.CurrentSelection == CityWorldController.Selection.MacroView
            && GetNode<MacroStreetLiveView>(
                "GameUiShell/ScreenContent/MacroStreetLiveView").Visible
            && !GetNode<ExpeditionLiveView>(
                "GameUiShell/ScreenContent/ExpeditionLiveView").Visible
            && !GetNode<PauseMenu>("PauseMenu").Visible
            && controller.CurrentSpeed == CityWorldController.SpeedChoice.Fast
            && controller.GetExpeditionRailSnapshot().ActiveExpeditions.Count == 1;
        if (!passed)
        {
            GD.PushError(
                $"[WOG-EXPEDITION-LIVE-ESC] failed; selection={controller.CurrentSelection}, "
                + $"speed={controller.CurrentSpeed}, active="
                + $"{controller.GetExpeditionRailSnapshot().ActiveExpeditions.Count}.");
            return;
        }
        GD.Print("[WOG-EXPEDITION-LIVE-ESC] returned to city without menu, pause, speed change or resolution.");
    }

    /// <summary>
    /// Replaces the loaded slot with a deterministic city whose founder is
    /// fresh, unwounded and uncommitted, so every expedition-rail fixture
    /// starts from the same state.
    /// </summary>
    /// <remarks>
    /// These fixtures used to seed themselves on top of whatever save slot 0
    /// happened to contain, which made them a coin flip. They failed for four
    /// sessions running with no diagnosable reason, and the reason turned out
    /// to be <c>MemberUnavailable / Wounded</c>: the combat vertical now
    /// really does hurt the Founder, so a slot where he had fought could not
    /// dispatch him again. World maturity, time of day, resources and injuries
    /// all fed the same coin flip. Reusing the fresh-world pattern the biome
    /// and early-game fixtures already use removes every one of those inputs.
    /// </remarks>
    private static void SeedHermeticFounderForExpeditionRailFixture(
        CityWorldController controller)
    {
        CitizenProfile profile = NewFounderProfile(LineageId.Ardhen);
        var fixture = new CityWorld();
        HeroCreationResult hero = fixture.TryCreateHero(
            new HeroCreationRequest("Aster", profile, profile.Gender));
        if (!hero.IsSuccess)
        {
            GD.PushError(
                "[WOG-EXPEDITION-RAIL-FIXTURE] could not create the fixture "
                + $"founder: {hero.Outcome}.");
            return;
        }
        fixture.SeedStartingForests();
        fixture.SeedStartingOpportunities();
        // Past the authored first night. A fresh world opens on it, and its
        // dialogue is a real modal that swallows the injected pointer clicks
        // these fixtures depend on — the rail rendered correctly underneath
        // while the click never reached the chronicle header. These fixtures
        // are about the rail, not the opening narrative.
        fixture.ConcludeFirstNightForFixtures();
        controller.World.Restore(WorldPersistence.Capture(fixture));
    }

    private void ExerciseExpeditionRailPointerForVisualRegression(string action)
    {
        ShowExpeditionRailForVisualRegression(ExpeditionRailFixtureState.Outbound);
        ExpeditionRail rail = GetNode<ExpeditionRail>(
            "GameUiShell/ScreenContent/ExpeditionRail");
        CallDeferred(MethodName.SendExpeditionRailPointerForVisualRegression, action, rail);
    }

    private void SendExpeditionRailPointerForVisualRegression(string action, ExpeditionRail rail)
    {
        Control? target = action switch
        {
            "details" => rail.FirstDetailsButton,
            "view" => rail.FirstViewButton,
            "cancel" => rail.FirstCancelButton,
            "more" => rail.MoreButton,
            _ => null,
        };
        if (target is null)
        {
            GD.PushError($"[WOG-EXPEDITION-RAIL-CLICK] missing target for {action}.");
            return;
        }
        SendPointerClickForVisualRegression(target);
        GD.Print($"[WOG-EXPEDITION-RAIL-CLICK] {action} dispatched.");
        if (action == "details" && rail.FirstExpeditionId is ExpeditionId id)
        {
            CallDeferred(MethodName.ValidateExpeditionRailDetailsForVisualRegression, id.Value);
        }
        else if (action == "cancel")
        {
            CallDeferred(MethodName.DeferExpeditionRailCancelValidation);
        }
        else if (action == "more")
        {
            CallDeferred(MethodName.ValidateExpeditionRailMoreForVisualRegression);
        }
    }

    private void ExerciseExpeditionRailFocusForVisualRegression()
    {
        ExpeditionRail rail = GetNode<ExpeditionRail>(
            "GameUiShell/ScreenContent/ExpeditionRail");
        rail.SetExpandedForVisualRegression(expanded: false);
        rail.SetExpandedForVisualRegression(expanded: true);
        CallDeferred(MethodName.ExerciseExpandedExpeditionRailFocusForVisualRegression);
    }

    private void ExerciseExpandedExpeditionRailFocusForVisualRegression()
    {
        ExpeditionRail rail = GetNode<ExpeditionRail>(
            "GameUiShell/ScreenContent/ExpeditionRail");
        rail.GrabDefaultFocus();
        if (GetViewport().GuiGetFocusOwner() != rail.FirstViewButton)
        {
            GD.PushError("[WOG-EXPEDITION-RAIL-FOCUS] default focus did not reach View.");
            return;
        }
        Input.ParseInputEvent(new InputEventAction { Action = "ui_down", Pressed = true });
        Input.ParseInputEvent(new InputEventAction { Action = "ui_down", Pressed = false });
        GetTree().CreateTimer(0.15).Timeout += ValidateExpeditionRailFocusForVisualRegression;
    }

    private void ValidateExpeditionRailFocusForVisualRegression()
    {
        ExpeditionRail rail = GetNode<ExpeditionRail>(
            "GameUiShell/ScreenContent/ExpeditionRail");
        Control? focused = GetViewport().GuiGetFocusOwner();
        if (focused != rail.FirstDetailsButton)
        {
            GD.PushError(
                "[WOG-EXPEDITION-RAIL-FOCUS] ui_down did not reach Details; "
                + $"focused={focused?.Name ?? "<none>"}.");
            return;
        }
        GD.Print("[WOG-EXPEDITION-RAIL-FOCUS] View -> ui_down -> Details OK");
    }

    private void ValidateExpeditionRailDetailsForVisualRegression(int expectedId)
    {
        ExpeditionPanel panel = GetNode<ExpeditionPanel>(
            "GameUiShell/ScreenContent/Center/ExpeditionPanel");
        if (panel.PresentedExpeditionId?.Value != expectedId)
        {
            GD.PushError("[WOG-EXPEDITION-RAIL-DETAILS] clicked ID was not presented.");
            return;
        }
        GD.Print($"[WOG-EXPEDITION-RAIL-DETAILS] expedition {expectedId} OK");
    }

    private void DeferExpeditionRailCancelValidation() =>
        CallDeferred(MethodName.ValidateExpeditionRailCancelForVisualRegression);

    private void ValidateExpeditionRailCancelForVisualRegression()
    {
        ExpeditionRail rail = GetNode<ExpeditionRail>(
            "GameUiShell/ScreenContent/ExpeditionRail");
        if (GetNode<CityWorldController>("CityWorldController")
                .GetExpeditionRailSnapshot().ActiveExpeditions.Count != 0
            || rail.FirstDetailsButton is not null)
        {
            GD.PushError("[WOG-EXPEDITION-RAIL-CANCEL] active card remained after cancel.");
            return;
        }
        GD.Print("[WOG-EXPEDITION-RAIL-CANCEL] active expedition removed OK");
    }

    private void ValidateExpeditionRailMoreForVisualRegression()
    {
        ExpeditionRail rail = GetNode<ExpeditionRail>(
            "GameUiShell/ScreenContent/ExpeditionRail");
        if (!rail.Visible || !rail.ChronicleExpanded)
        {
            GD.PushError("[WOG-EXPEDITION-RAIL-MORE] Chronicle did not open.");
            return;
        }
        GD.Print("[WOG-EXPEDITION-RAIL-MORE] Chronicle opened OK");
    }

    private void ExerciseExpeditionRailChronicleRoundTripForVisualRegression()
    {
        ExpeditionRail rail = GetNode<ExpeditionRail>(
            "GameUiShell/ScreenContent/ExpeditionRail");
        // Wait a frame before the first click. The fixture has just reseeded
        // the world, so the rail rebuilt its cards this frame and its children
        // have not been sorted yet; clicking immediately dispatches at a
        // pre-layout rect. In a real window that stale coordinate landed on
        // VER instead of the chronicle header and opened the live view, which
        // then reported as "Chronicle did not restore expeditions" — a
        // misdirected click wearing the costume of a layout bug.
        GetTree().CreateTimer(0.1).Timeout += () =>
        {
            SendPointerClickForVisualRegression(rail.MoreButton);
            GetTree().CreateTimer(0.1).Timeout += () =>
            {
                if (!rail.ChronicleExpanded)
                {
                    GD.PushError("[WOG-EXPEDITION-RAIL-ROUNDTRIP] Chronicle did not open.");
                    return;
                }
                // The first edge already exercised real pointer dispatch. Emit the
                // same Button signal for the return edge so the fixture validates
                // accordion state rather than depending on a relayout race in the
                // headless pointer coordinate transform.
                rail.MoreButton.EmitSignal(BaseButton.SignalName.Pressed);
                GetTree().CreateTimer(0.1).Timeout +=
                    ValidateExpeditionRailChronicleRoundTripForVisualRegression;
            };
        };
    }

    private void ValidateExpeditionRailChronicleRoundTripForVisualRegression()
    {
        ExpeditionRail rail = GetNode<ExpeditionRail>(
            "GameUiShell/ScreenContent/ExpeditionRail");
        // Height, not visibility, and a threshold with meaning. The bug this
        // fixture exists to catch left the cards Visible with their own 25 px
        // height while the body around them was squeezed to 2 px: laid out
        // nowhere, drawn not at all. `> 0` would have called that a pass. The
        // body must be tall enough to actually show its first card.
        Rect2 body = rail.ExpeditionBodyRectForVisualRegression;
        float cardHeight = rail.FirstDetailsButton?.GetGlobalRect().Size.Y ?? 0f;
        bool passed = rail.Expanded
            && !rail.ChronicleExpanded
            && rail.FirstViewButton?.IsVisibleInTree() == true
            && cardHeight > 0f
            && body.Size.Y >= cardHeight;
        if (!passed)
        {
            GD.PushError(
                "[WOG-EXPEDITION-RAIL-ROUNDTRIP] closing Chronicle did not restore expeditions; "
                + $"rail={rail.Expanded}, chronicle={rail.ChronicleExpanded}, "
                + $"view={rail.FirstViewButton?.IsVisibleInTree()}, "
                + $"bodyRect={body}, cardHeight={cardHeight}.");
            return;
        }
        GD.Print(
            "[WOG-EXPEDITION-RAIL-ROUNDTRIP] Chronicle closed and expedition content returned; "
            + $"bodyHeight={body.Size.Y}, cardHeight={cardHeight}.");
    }

    private void ExerciseExpeditionRailPhaseFocusForVisualRegression()
    {
        ExpeditionRail rail = GetNode<ExpeditionRail>(
            "GameUiShell/ScreenContent/ExpeditionRail");
        rail.GrabDefaultFocus();
        CityWorldController controller = GetNode<CityWorldController>("CityWorldController");
        controller.AdvanceWorldTickForVisualRegression();
        controller.AdvanceWorldTickForVisualRegression();
        CallDeferred(MethodName.DeferExpeditionRailPhaseFocusValidation);
    }

    /// <summary>
    /// Programmatically opens the rail header so the expedition
    /// section is the protagonist of the accordion. Used by the
    /// visual matrix to verify the cards actually render when the
    /// rail wins the column.
    /// </summary>
    private void ForceExpeditionRailProtagonistForVisualRegression()
    {
        ExpeditionRail rail = GetNode<ExpeditionRail>(
            "GameUiShell/ScreenContent/ExpeditionRail");
        // Collapse the chronicle body so the accordion hands the
        // column back to the expedition scroll, then expand the rail
        // header so the cards become visible.
        if (rail.ChronicleExpanded)
        {
            // Toggle until collapsed (the chronicle header is the
            // public surface for its own state).
            while (rail.ChronicleExpanded)
            {
                rail.MoreButton.EmitSignal(Button.SignalName.Pressed);
            }
        }
        // Exercise the player path, not a property setter: start folded,
        // then send an actual pointer click to the rail header.
        rail.SetExpandedForVisualRegression(expanded: false);
        SendPointerClickForVisualRegression(rail.HeaderForVisualRegression);
        GetTree().CreateTimer(0.15).Timeout +=
            ValidateExpeditionRailProtagonistForVisualRegression;
    }

    private void ValidateExpeditionRailProtagonistForVisualRegression()
    {
        ExpeditionRail rail = GetNode<ExpeditionRail>(
            "GameUiShell/ScreenContent/ExpeditionRail");
        if (!rail.Expanded
            || rail.FirstViewButton is null
            || !rail.FirstViewButton.IsVisibleInTree()
            || rail.FirstDetailsButton is null
            || !rail.FirstDetailsButton.IsVisibleInTree())
        {
            GD.PushError(
                "[WOG-EXPEDITION-RAIL-EXPAND] real header click did not reveal VER and the active card; "
                + $"expanded={rail.Expanded}, railVisible={rail.IsVisibleInTree()}, "
                + $"headerRect={rail.HeaderForVisualRegression.GetGlobalRect()}, "
                + $"viewVisible={rail.FirstViewButton?.IsVisibleInTree()}, "
                + $"detailsVisible={rail.FirstDetailsButton?.IsVisibleInTree()}.");
            return;
        }
        GD.Print("[WOG-EXPEDITION-RAIL-EXPAND] real header click revealed VER and the active card OK");
    }

    private void DeferExpeditionRailPhaseFocusValidation() =>
        CallDeferred(MethodName.ValidateExpeditionRailPhaseFocusForVisualRegression);

    private void ValidateExpeditionRailPhaseFocusForVisualRegression()
    {
        ExpeditionRail rail = GetNode<ExpeditionRail>(
            "GameUiShell/ScreenContent/ExpeditionRail");
        if (GetViewport().GuiGetFocusOwner() != rail.FirstDetailsButton)
        {
            GD.PushError("[WOG-EXPEDITION-RAIL-PHASE-FOCUS] focus lost on phase tick.");
            return;
        }
        GD.Print("[WOG-EXPEDITION-RAIL-PHASE-FOCUS] details focus preserved OK");
    }

    private static OfflineProgressionReport BuildVisualOfflineReport()
    {
        const int rowCount = 80;
        var events = new List<WorldEvent>(rowCount);
        WorldEventSubject farm = WorldEventSubject.Building(new BuildingId(2), "Farm");
        WorldEventSubject quarry = WorldEventSubject.Building(new BuildingId(3), "Quarry");
        for (int index = 0; index < rowCount; index++)
        {
            WorldEventKind kind = index % 9 == 0
                ? WorldEventKind.ProductionBlocked
                : index % 2 == 0
                    ? WorldEventKind.StockProduced
                    : WorldEventKind.WorkerRecovered;
            WorldEventSubject subject = index % 3 == 0 ? quarry : farm;
            events.Add(new WorldEvent(
                new WorldEventId(index + 1),
                181_000 + index * 12,
                kind,
                subject,
                index % 5 + 1,
                index == 0 ? null : new WorldEventId(index)));
        }

        return new OfflineProgressionReport(
            ticksApplied: 960,
            stockAdded: 144,
            stockWasted: 12,
            simulatedTime: TimeSpan.FromHours(8),
            events);
    }
}
