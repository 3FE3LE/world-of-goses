#nullable enable
using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Ui;

namespace WorldofGoses;

/// <summary>
/// Ephemeral commentary the fire spirit offers when the founder
/// gathers a recognised resource near the unfinished campfire or
/// shelter during the first night
/// (<c>docs/19_FIRST_NIGHT_AND_FIRE_SPIRIT.md</c> §6–8). The
/// commentary reuses the <see cref="Notifier"/> toast: it is short,
/// auto-dismissing, and never captures input.
///
/// <para>
/// The helper translates the supplied message key with
/// <see cref="UiText.Get"/> at call time, so a stale or empty
/// translation surfaces immediately rather than silently rendering
/// the raw key. The caller passes the key — never the body string —
/// so the helper stays free of narrative decisions.
/// </para>
///
/// <para>
/// The trigger lives in the controller: when a gathering event
/// lands on a <see cref="ResourceType"/> the spirit recognises and
/// the founder is within range of an unfinished campfire or
/// shelter, the controller invokes the matching <c>Show*</c>
/// method here.
/// </para>
/// </summary>
public static class FirstNightContextCommentary
{
    /// <summary>
    /// Shows the spirit's commentary when the founder gathers
    /// <see cref="ResourceType.Branches"/> near the unfinished
    /// campfire. Suppressed when no first night is active, when
    /// the campfire is already built, or when the founder is not
    /// near the camp.
    /// </summary>
    public static void ShowBranchesForFire()
    {
        Notifier.Show(UiText.Get(Tr.FirstNight.ContextBranchesForFire));
    }

    /// <summary>
    /// Shows the spirit's commentary when the founder gathers
    /// <see cref="ResourceType.SmallStone"/> near the unfinished
    /// campfire.
    /// </summary>
    public static void ShowSmallStoneForFire()
    {
        Notifier.Show(UiText.Get(Tr.FirstNight.ContextSmallStoneForFire));
    }

    /// <summary>
    /// Shows the spirit's commentary when the founder gathers
    /// <see cref="ResourceType.Branches"/> near the unfinished
    /// shelter. Suppressed when no first night is active, when the
    /// shelter is already built, or when the campfire (which the
    /// spirit inhabits) has not yet been completed.
    /// </summary>
    public static void ShowBranchesForShelter()
    {
        Notifier.Show(UiText.Get(Tr.FirstNight.ContextBranchesForShelter));
    }

    /// <summary>
    /// Shows the spirit's commentary when the founder gathers
    /// <see cref="ResourceType.PlantFiber"/> near the unfinished
    /// shelter.
    /// </summary>
    public static void ShowPlantFiberForShelter()
    {
        Notifier.Show(UiText.Get(Tr.FirstNight.ContextPlantFiberForShelter));
    }
}
