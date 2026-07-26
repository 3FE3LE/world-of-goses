using System.Collections.Generic;
using System.Threading.Tasks;

#nullable enable

namespace WorldofGoses.Domain;

/// <summary>
/// In-house implementation of <see cref="IDialogueRunner"/> (S-1.6),
/// built instead of vendoring Dialogic 2 / godot_dialogue_manager sight
/// unseen. Walks a dialogue tree node by node: linear advance via
/// <see cref="IDialogueNode.Next"/> when there are no choices, or a
/// caller-supplied <see cref="ChoicePrompt"/> to pick among the choices
/// whose <see cref="IDialogueChoice.IsAvailable"/> passes for the given
/// <see cref="DialogueState"/>.
///
/// <para>
/// The prompt is injected rather than hard-coded to a UI so this class
/// stays pure domain (no <c>Godot.*</c>, per <c>AGENTS.md §8</c>) and is
/// deterministically testable: a test supplies a scripted or
/// first-available prompt instead of a real player.
/// </para>
/// </summary>
public sealed class DialogueRunner : IDialogueRunner
{
    /// <summary>
    /// Asks for a choice among <paramref name="availableChoices"/> at
    /// <paramref name="node"/>. Returns null to end the dialogue instead
    /// of picking (e.g. the player closed the dialogue window).
    /// </summary>
    public delegate Task<IDialogueChoice?> ChoicePrompt(
        IDialogueNode node,
        IReadOnlyList<IDialogueChoice> availableChoices,
        DialogueState state);

    private readonly ChoicePrompt _prompt;
    private volatile bool _cancelled;

    public DialogueRunner(ChoicePrompt prompt)
    {
        _prompt = prompt;
    }

    public async Task<DialogueOutcome> RunAsync(IDialogueNode start, DialogueState state)
    {
        _cancelled = false;
        IDialogueNode current = start;
        IDialogueChoice? lastChosen = null;

        while (true)
        {
            if (_cancelled) return new DialogueOutcome(current, lastChosen);

            var available = new List<IDialogueChoice>();
            foreach (IDialogueChoice choice in current.Choices)
            {
                if (choice.IsAvailable(state)) available.Add(choice);
            }

            if (available.Count > 0)
            {
                IDialogueChoice? picked = await _prompt(current, available, state);
                if (_cancelled) return new DialogueOutcome(current, lastChosen);
                if (picked is null) return new DialogueOutcome(current, lastChosen);

                lastChosen = picked;
                if (picked.Target is null) return new DialogueOutcome(current, picked);
                current = picked.Target;
                continue;
            }

            if (current.Next is not null)
            {
                current = current.Next;
                continue;
            }

            return new DialogueOutcome(current, lastChosen);
        }
    }

    public void Cancel() => _cancelled = true;
}
