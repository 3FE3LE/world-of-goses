#nullable enable
using System;

namespace WorldofGoses.Domain;

/// <summary>
/// The authored first night's persistent state
/// (`docs/world-of-goses-design-bible/23_FIRST_NIGHT_AND_FIRE_SPIRIT.md`).
///
/// It carries semantic context only. The fire spirit has no persisted street or
/// lateral offset: presentation derives where it stands from
/// <see cref="Stage"/> — beside the founder before there is a flame, in the
/// flame afterwards — exactly as building anchors derive from placement. The
/// city persists no authoritative visual coordinates, and a temporary
/// apparition is no reason to start.
///
/// <see cref="CurrentDialogueNodeId"/> is what makes the sequence resumable.
/// <see cref="DialogueRunner"/> is an <c>async</c> loop holding its position in
/// an <c>await</c>, so a conversation interrupted by a save could not be
/// restored; a persisted node id can. The runner stays untouched for the first
/// branching NPC.
/// </summary>
public sealed class FirstNightState
{
    public FirstNightState(
        FirstNightStage stage = FirstNightStage.Manifested,
        string? currentDialogueNodeId = null,
        int startedAtTick = 0,
        int? concludedAtTick = null)
    {
        if (startedAtTick < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(startedAtTick), startedAtTick, "The first night cannot start before tick 0.");
        }
        if (concludedAtTick is int concluded)
        {
            if (concluded < startedAtTick)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(concludedAtTick), concluded, "The night cannot conclude before it started.");
            }
            if (stage != FirstNightStage.Concluded)
            {
                throw new ArgumentException(
                    "A concluding tick requires the Concluded stage.", nameof(concludedAtTick));
            }
        }
        else if (stage == FirstNightStage.Concluded)
        {
            throw new ArgumentException(
                "The Concluded stage requires a concluding tick.", nameof(stage));
        }

        Stage = stage;
        CurrentDialogueNodeId = currentDialogueNodeId;
        StartedAtTick = startedAtTick;
        ConcludedAtTick = concludedAtTick;
    }

    public FirstNightStage Stage { get; private set; }

    /// <summary>
    /// The main-dialogue node the player is currently reading, or <c>null</c>
    /// when the sequence waits on an action rather than on words.
    /// </summary>
    public string? CurrentDialogueNodeId { get; private set; }

    public int StartedAtTick { get; }
    public int? ConcludedAtTick { get; private set; }

    /// <summary>A concluded night is inert; every gate reads this.</summary>
    public bool IsActive => Stage != FirstNightStage.Concluded;

    public bool SpiritIsPresent => FirstNightRules.SpiritIsPresent(Stage);

    /// <summary>
    /// A night that already happened. Used by the v30→v31 migration: existing
    /// cities are past their opening, and dropping them into the sequence would
    /// trap them behind milestones their world cannot satisfy.
    /// </summary>
    public static FirstNightState AlreadyConcluded(int currentTick) => new(
        FirstNightStage.Concluded,
        currentDialogueNodeId: null,
        startedAtTick: 0,
        concludedAtTick: Math.Max(0, currentTick));

    /// <summary>
    /// Moves to the next stage. Returns <c>false</c> when the night is already
    /// concluded, so callers can advance idempotently from a tick loop.
    /// </summary>
    public bool TryAdvance(int currentTick)
    {
        if (Stage == FirstNightStage.Concluded) return false;
        Stage = FirstNightRules.Next(Stage);
        CurrentDialogueNodeId = null;
        if (Stage == FirstNightStage.Concluded)
        {
            ConcludedAtTick = Math.Max(StartedAtTick, currentTick);
        }
        return true;
    }

    /// <summary>Opens a main-dialogue node for the current stage.</summary>
    public void OpenDialogueNode(string nodeId)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
        {
            throw new ArgumentException("A dialogue node needs an id.", nameof(nodeId));
        }
        CurrentDialogueNodeId = nodeId;
    }

    /// <summary>
    /// The in-game tick the night is willing to display. It parks at `05:59`
    /// instead of rolling into daylight while the spirit is still there: the
    /// alternative is a clock that jumps at conclusion, which reads worse and
    /// tells the player less.
    /// </summary>
    public int DisplayedTick(int currentTick) => IsActive
        ? Math.Min(currentTick, FirstNightRules.LatestDisplayedNightTick)
        : currentTick;
}
