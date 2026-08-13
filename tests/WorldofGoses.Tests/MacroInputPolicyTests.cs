#nullable enable
using System;
using System.IO;
using WorldofGoses.Prototypes;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// Pure decision tests for <see cref="MacroInputPolicy"/> (GitHub #31).
///
/// <para>The helper exists because the view's previous single predicate
/// mixed two distinct questions — "can the player still interact with
/// the world?" and "can the player move the camera?" — and answered
/// "no" to both during construction placement. The new helper makes
/// the separation explicit so a future regression that re-merges the
/// gates fails these tests in a single place.</para>
/// </summary>
public sealed class MacroInputPolicyTests
{
    [Fact]
    public void CameraNavigation_IsAllowedDuringPlacement()
    {
        // The headline contract: a player choosing a lot must still
        // be able to pan the camera. Placement closes the world
        // interaction gate, not the camera gate.
        Assert.True(MacroInputPolicy.CanUseCameraNavigationInput(
            viewVisible: true,
            pauseMenuVisible: false,
            modalHostOpen: false,
            actionMenuVisible: false,
            cultivationActionMenuVisible: false,
            buildingEntryPushActive: false));
    }

    [Fact]
    public void WorldInteraction_IsRefusedDuringPlacement()
    {
        // The other half: a left click during placement must NOT
        // reach gather, building selection, or citizen selection.
        Assert.False(MacroInputPolicy.CanUseWorldInteraction(
            viewVisible: true,
            pauseMenuVisible: false,
            modalHostOpen: false,
            actionMenuVisible: false,
            cultivationActionMenuVisible: false,
            buildingEntryPushActive: false,
            placementActive: true));
    }

    [Theory]
    [InlineData(true, false, false, false, false)]
    [InlineData(false, true, false, false, false)]
    [InlineData(false, false, true, false, false)]
    [InlineData(false, false, false, true, false)]
    [InlineData(false, false, false, false, true)]
    public void CameraNavigation_IsRefusedWhilePauseModalOrBuildingEntryDominate(
        bool pauseMenuVisible,
        bool modalHostOpen,
        bool actionMenuVisible,
        bool cultivationActionMenuVisible,
        bool buildingEntryPushActive)
    {
        // Each row flips one signal to "exclusive" and confirms the
        // camera gate refuses input. The view is visible in every
        // case — the helper is the test subject, not visibility.
        Assert.False(MacroInputPolicy.CanUseCameraNavigationInput(
            viewVisible: true,
            pauseMenuVisible: pauseMenuVisible,
            modalHostOpen: modalHostOpen,
            actionMenuVisible: actionMenuVisible,
            cultivationActionMenuVisible: cultivationActionMenuVisible,
            buildingEntryPushActive: buildingEntryPushActive));
    }

    [Theory]
    [InlineData(true, false, false, false, false, false)]
    [InlineData(false, true, false, false, false, false)]
    [InlineData(false, false, true, false, false, false)]
    [InlineData(false, false, false, true, false, false)]
    [InlineData(false, false, false, false, true, false)]
    [InlineData(false, false, false, false, false, true)]
    public void WorldInteraction_IsRefusedForEveryExclusiveState(
        bool pauseMenuVisible,
        bool modalHostOpen,
        bool actionMenuVisible,
        bool cultivationActionMenuVisible,
        bool buildingEntryPushActive,
        bool placementActive)
    {
        // The world-interaction gate is stricter than the camera gate
        // because it also closes during placement. Each row flips one
        // signal to "exclusive" and confirms the gate refuses input.
        // The view is visible in every case.
        Assert.False(MacroInputPolicy.CanUseWorldInteraction(
            viewVisible: true,
            pauseMenuVisible: pauseMenuVisible,
            modalHostOpen: modalHostOpen,
            actionMenuVisible: actionMenuVisible,
            cultivationActionMenuVisible: cultivationActionMenuVisible,
            buildingEntryPushActive: buildingEntryPushActive,
            placementActive: placementActive));
    }

    [Fact]
    public void CameraNavigation_DoesNotConsultPlacement()
    {
        // The structural rule the new helper exists to express: the
        // camera gate's parameter list deliberately has no
        // `placementActive` argument, so callers cannot accidentally
        // reintroduce the bug by passing it. Assert it via the type
        // signature so the contract is enforced by the compiler, not
        // by convention.
        var method = typeof(MacroInputPolicy).GetMethod(
            nameof(MacroInputPolicy.CanUseCameraNavigationInput));
        Assert.NotNull(method);
        var parameters = method!.GetParameters();
        Assert.DoesNotContain(
            parameters,
            parameter => parameter.Name?.Contains("placement", System.StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public void MacroViewInputGuard_DropsPlacementFromArrowPath()
    {
        // The bug GitHub #31 closes: `_Input` used to include
        // `|| _placement.PlacementActive` in its early-return guard,
        // so an arrow key was dropped before reaching
        // `TryHandleCameraNavigationKey`. The split keeps every other
        // exclusive flag in the guard (pause, modal, action menus)
        // and only removes the placement clause.
        string source = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(),
            "game", "scripts", "Prototypes", "MacroStreetLiveView.cs"));
        int inputOverride = source.IndexOf(
            "public override void _Input(InputEvent @event)",
            System.StringComparison.Ordinal);
        Assert.True(inputOverride > 0, "MacroStreetLiveView no longer overrides _Input.");

        int unhandledOverride = source.IndexOf(
            "public override void _UnhandledInput(InputEvent @event)",
            System.StringComparison.Ordinal);
        Assert.True(unhandledOverride > inputOverride, "_UnhandledInput moved above _Input.");

        string inputBody = source[inputOverride..unhandledOverride];
        Assert.Contains("TryHandleCameraNavigationKey(@event)", inputBody, System.StringComparison.Ordinal);
        Assert.Contains("GetViewport().SetInputAsHandled()", inputBody, System.StringComparison.Ordinal);
        // The pause, modal and action-menu clauses are still here —
        // the bug was specifically the placement clause.
        Assert.Contains("|| _pauseMenu.Visible", inputBody, System.StringComparison.Ordinal);
        Assert.Contains("|| _modalHost?.IsOpen == true", inputBody, System.StringComparison.Ordinal);
        Assert.DoesNotContain("|| _placement.PlacementActive", inputBody, System.StringComparison.Ordinal);
    }

    [Fact]
    public void MacroViewMotionTick_StillReadsTheWorldNavigationGate()
    {
        // The held-input half of the fix: `MotionTick` must keep
        // running during placement so A/D polled movement and
        // vertical pan repeat stay active. The flag is the same
        // name it had before — it now aliases to
        // `CanUseCameraNavigationInput`, which deliberately excludes
        // the placement clause.
        string source = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(),
            "game", "scripts", "Prototypes", "MacroStreetLiveView.cs"));
        Assert.Contains(
            "MotionTick(allowCameraInput: CanUseWorldNavigationInput);",
            source,
            System.StringComparison.Ordinal);
    }

    [Fact]
    public void MacroView_InvalidatesPlacementHoverOnCameraChange()
    {
        // Stale hover regression: the view carries the three
        // helpers that detect a camera projection change and clear
        // the hover when it moved (one in the camera sites plus
        // one in the depth transition path). The four camera
        // mutation sites — TryPanCameraLateral, PanCameraStreet,
        // ContinueVerticalCameraPan, AdjustZoom — and the depth
        // transition advance in `_Process` each call the
        // invalidation. The structural check counts the call
        // sites; the per-site enumeration is exercised by hand
        // because the call site in the source is the only
        // meaningful evidence.
        string source = File.ReadAllText(Path.Combine(
            TestHelpers.FindRepositoryRoot(),
            "game", "scripts", "Prototypes", "MacroStreetLiveView.cs"));
        Assert.Contains("ClearStalePlacementHover", source, System.StringComparison.Ordinal);
        Assert.Contains("InvalidatePlacementHoverIfCameraChanged", source, System.StringComparison.Ordinal);
        Assert.Contains("RememberHoverResolvedCamera", source, System.StringComparison.Ordinal);

        // The four camera mutation sites plus the depth-transition
        // tick should each call the invalidation. A regression that
        // drops the call from any of them re-introduces the
        // stale-hover bug.
        int callCount = 0;
        int searchFrom = 0;
        while (true)
        {
            int next = source.IndexOf(
                "InvalidatePlacementHoverIfCameraChanged()",
                searchFrom,
                System.StringComparison.Ordinal);
            if (next < 0) break;
            callCount++;
            searchFrom = next + 1;
        }
        Assert.True(
            callCount >= 5,
            $"Expected at least 5 call sites of InvalidatePlacementHoverIfCameraChanged "
            + $"(4 camera sites + 1 depth transition), found {callCount}.");
    }
}
