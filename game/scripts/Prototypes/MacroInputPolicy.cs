#nullable enable
namespace WorldofGoses.Prototypes;

/// <summary>
/// Pure decision predicates for the macro street view's input gates
/// (GitHub #31).
///
/// <para>Before this helper existed, <c>MacroStreetLiveView</c> had one
/// shared predicate — <c>CanUseWorldNavigationInput</c> — that mixed two
/// distinct questions:</para>
/// <list type="bullet">
///   <item>Can the player still interact with the world normally?</item>
///   <item>Can the player move the camera?</item>
/// </list>
///
/// <para>Construction placement wants a strict "no" to the first and
/// "yes" to the second: a player choosing a lot must still be able to
/// pan the camera to inspect the rest of the city, but a left click
/// during placement must not gather a resource or open a
/// BuildingInspector. Combining both into one predicate meant
/// placement behaved like a modal for camera navigation, forcing the
/// player to cancel placement, pan, and reopen the build dock for
/// every camera move on a wider terrarium.</para>
///
/// <para>This class is a static, allocation-free policy the view reads
/// from. It does not own any state; the view passes its own
/// observable flags. The class is the single home for the new
/// separation, so the four call sites that previously consumed
/// <c>CanUseWorldNavigationInput</c> for camera navigation
/// (arrow keys, W/S/F, A/D held, vertical pan repeat) read
/// <see cref="CanUseCameraNavigationInput"/> instead, and the gates
/// that should still close for placement (left-click routing,
/// hover, world interaction) keep <see cref="CanUseWorldInteraction"/>.</para>
/// </summary>
internal static class MacroInputPolicy
{
    /// <summary>
    /// True when the camera is allowed to move in response to input.
    /// Placement does not block this gate, so the player can pan
    /// during construction.
    /// </summary>
    /// <param name="viewVisible">Whether the macro view is the active
    /// surface. Outside the view, nothing here applies.</param>
    /// <param name="pauseMenuVisible">Pause menu blocks all input
    /// because the game is paused.</param>
    /// <param name="modalHostOpen">A modal is up; the world yields
    /// input to the modal until it closes.</param>
    /// <param name="actionMenuVisible">Resource / context action
    /// menus take precedence over world navigation.</param>
    /// <param name="cultivationActionMenuVisible">The cultivation
    /// site action menu behaves like a transient modal.</param>
    /// <param name="buildingEntryPushActive">A non-interruptible
    /// building-entry camera push owns input until it completes.</param>
    public static bool CanUseCameraNavigationInput(
        bool viewVisible,
        bool pauseMenuVisible,
        bool modalHostOpen,
        bool actionMenuVisible,
        bool cultivationActionMenuVisible,
        bool buildingEntryPushActive) =>
        viewVisible
        && !pauseMenuVisible
        && !modalHostOpen
        && !actionMenuVisible
        && !cultivationActionMenuVisible
        && !buildingEntryPushActive;

    /// <summary>
    /// True when the player can still issue world interactions
    /// (gather, building selection, citizen selection, etc.).
    /// Placement closes this gate; pause and modals close it the
    /// same way they always did.
    /// </summary>
    public static bool CanUseWorldInteraction(
        bool viewVisible,
        bool pauseMenuVisible,
        bool modalHostOpen,
        bool actionMenuVisible,
        bool cultivationActionMenuVisible,
        bool buildingEntryPushActive,
        bool placementActive) =>
        viewVisible
        && !pauseMenuVisible
        && !modalHostOpen
        && !actionMenuVisible
        && !cultivationActionMenuVisible
        && !buildingEntryPushActive
        && !placementActive;
}
