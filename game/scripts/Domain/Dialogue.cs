using System;
using System.Collections.Generic;
using System.Threading.Tasks;

#nullable enable

namespace WorldofGoses.Domain;

/// <summary>
/// Snapshot of the world state that a dialogue tree can query when
/// computing branch availability. Immutable so the dialogue runner
/// can hold a reference across awaits without coordinating with the
/// simulation. The actual fields are not yet defined — the surface is
/// declared here so consumers can code against the contract while the
/// first NPC slice fills in the concrete data sources.
/// </summary>
public sealed record DialogueState(
    CitizenId? SpeakerId,
    int CurrentTick,
    string HeroLineage);

/// <summary>
/// One node of a dialogue tree. Carries a translation key for the
/// body text (the UI calls <c>Tr(BodyKey)</c>), a speaker id, an
/// optional linear follow-up, and a list of choices.
/// </summary>
public interface IDialogueNode
{
    /// <summary>Stable id within the dialogue tree. Used by the runner to track position and by tests to assert branching.</summary>
    string Id { get; }

    /// <summary>Id of the speaker (NPC id, faction id, etc.). The UI resolves this to a sprite and a display name.</summary>
    string SpeakerId { get; }

    /// <summary>Translation key for the body text. The dialogue runner resolves it via the active <c>LocaleManager</c>.</summary>
    string BodyKey { get; }

    /// <summary>Available choices at this node. Empty means the dialogue ends here.</summary>
    IReadOnlyList<IDialogueChoice> Choices { get; }

    /// <summary>Optional linear follow-up if no choice is taken. Null when the node terminates or only has choices.</summary>
    IDialogueNode? Next { get; }
}

/// <summary>
/// One choice at a dialogue node. Carries a translation key for the
/// label and a predicate that decides whether the choice is available
/// in the current <see cref="DialogueState"/>.
/// </summary>
public interface IDialogueChoice
{
    /// <summary>Translation key for the choice label.</summary>
    string LabelKey { get; }

    /// <summary>Whether this choice is selectable in the given state. The runner filters choices by this predicate.</summary>
    Func<DialogueState, bool> IsAvailable { get; }

    /// <summary>Node to advance to when the choice is taken. Null means "end the dialogue".</summary>
    IDialogueNode? Target { get; }
}

/// <summary>
/// The result of running a dialogue: where the conversation ended
/// (which node, which choice if any) and any side effects the runner
/// wants to surface (quests accepted, items granted, faction reputation).
/// </summary>
public sealed record DialogueOutcome(
    IDialogueNode? TerminalNode,
    IDialogueChoice? ChosenChoice);

/// <summary>
/// Runs a dialogue tree from a given starting node to a terminal
/// node (or cancellation). The contract is intentionally minimal:
/// any concrete implementation — custom JSON runner,
/// <c>godot_dialogue_manager</c> integration, <c>Dialogic 2</c> — can
/// satisfy it without consumers caring about the choice.
///
/// <para>
/// This is the seam: today no implementation exists. The first NPC
/// slice will add a custom <c>JsonDialogueRunner</c> and (optionally)
/// migrate to a library later. Consumers (NPC portraits, faction UI,
/// chronicle entries) depend only on the interface.
/// </para>
/// </summary>
public interface IDialogueRunner
{
    /// <summary>
    /// Drives the dialogue from <paramref name="start"/> to a terminal
    /// node. Implementations may be async (waiting for player input)
    /// or synchronous (e.g. for tests). The returned <see cref="DialogueOutcome"/>
    /// tells the caller where the conversation ended.
    /// </summary>
    Task<DialogueOutcome> RunAsync(IDialogueNode start, DialogueState state);

    /// <summary>
    /// Cancels an in-flight dialogue. The next call to <see cref="RunAsync"/>
    /// on the same runner should return a cancelled outcome.
    /// </summary>
    void Cancel();
}
