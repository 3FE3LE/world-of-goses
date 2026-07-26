using System.Collections.Generic;
using System.Threading.Tasks;
using WorldofGoses.Domain;
using Xunit;

namespace WorldofGoses.Tests;

public sealed class FakeDialogueNode : IDialogueNode
{
    public string Id { get; init; } = "";
    public string SpeakerId { get; init; } = "npc";
    public string BodyKey { get; init; } = "";
    public IReadOnlyList<IDialogueChoice> Choices { get; init; } = System.Array.Empty<IDialogueChoice>();
    public IDialogueNode? Next { get; init; }
}

public sealed class FakeDialogueChoice : IDialogueChoice
{
    public string LabelKey { get; init; } = "";
    public System.Func<DialogueState, bool> IsAvailable { get; init; } = _ => true;
    public IDialogueNode? Target { get; init; }
}

public class DialogueRunnerTests
{
    private static readonly DialogueState State = new(SpeakerId: null, CurrentTick: 0, HeroLineage: "vaelun");

    private static DialogueRunner.ChoicePrompt PickFirst() =>
        (_, choices, _) => Task.FromResult<IDialogueChoice?>(choices[0]);

    [Fact]
    public async Task RunAsync_LinearChain_FollowsNextToTerminal()
    {
        var terminal = new FakeDialogueNode { Id = "end" };
        var middle = new FakeDialogueNode { Id = "middle", Next = terminal };
        var start = new FakeDialogueNode { Id = "start", Next = middle };

        var runner = new DialogueRunner(PickFirst());
        DialogueOutcome outcome = await runner.RunAsync(start, State);

        Assert.Same(terminal, outcome.TerminalNode);
        Assert.Null(outcome.ChosenChoice);
    }

    [Fact]
    public async Task RunAsync_FiltersUnavailableChoices()
    {
        var reachableEnd = new FakeDialogueNode { Id = "reachable" };
        var unavailableChoice = new FakeDialogueChoice
        {
            LabelKey = "no",
            IsAvailable = _ => false,
            Target = new FakeDialogueNode { Id = "unreachable" },
        };
        var availableChoice = new FakeDialogueChoice
        {
            LabelKey = "yes",
            IsAvailable = _ => true,
            Target = reachableEnd,
        };
        var start = new FakeDialogueNode
        {
            Id = "start",
            Choices = new IDialogueChoice[] { unavailableChoice, availableChoice },
        };

        IDialogueChoice? seenOnlyChoice = null;
        DialogueRunner.ChoicePrompt captureAndPickFirst = (_, choices, _) =>
        {
            seenOnlyChoice = choices.Count == 1 ? choices[0] : null;
            return Task.FromResult<IDialogueChoice?>(choices[0]);
        };

        var runner = new DialogueRunner(captureAndPickFirst);
        DialogueOutcome outcome = await runner.RunAsync(start, State);

        Assert.Same(availableChoice, seenOnlyChoice);
        Assert.Same(reachableEnd, outcome.TerminalNode);
        Assert.Same(availableChoice, outcome.ChosenChoice);
    }

    [Fact]
    public async Task RunAsync_ChoiceWithNullTarget_EndsDialogueAtCurrentNode()
    {
        var endChoice = new FakeDialogueChoice { LabelKey = "bye", Target = null };
        var start = new FakeDialogueNode
        {
            Id = "start",
            Choices = new IDialogueChoice[] { endChoice },
        };

        var runner = new DialogueRunner(PickFirst());
        DialogueOutcome outcome = await runner.RunAsync(start, State);

        Assert.Same(start, outcome.TerminalNode);
        Assert.Same(endChoice, outcome.ChosenChoice);
    }

    [Fact]
    public async Task RunAsync_PromptReturnsNull_EndsDialogueWithoutAChoice()
    {
        var choice = new FakeDialogueChoice { LabelKey = "yes", Target = new FakeDialogueNode { Id = "next" } };
        var start = new FakeDialogueNode { Id = "start", Choices = new IDialogueChoice[] { choice } };

        var runner = new DialogueRunner((_, _, _) => Task.FromResult<IDialogueChoice?>(null));
        DialogueOutcome outcome = await runner.RunAsync(start, State);

        Assert.Same(start, outcome.TerminalNode);
        Assert.Null(outcome.ChosenChoice);
    }

    [Fact]
    public async Task Cancel_StopsAtCurrentNodeWithoutAdvancing()
    {
        var next = new FakeDialogueNode { Id = "next" };
        var choice = new FakeDialogueChoice { LabelKey = "go", Target = next };
        var start = new FakeDialogueNode { Id = "start", Choices = new IDialogueChoice[] { choice } };

        DialogueRunner? runnerRef = null;
        DialogueRunner.ChoicePrompt cancelDuringPrompt = (_, choices, _) =>
        {
            runnerRef!.Cancel();
            return Task.FromResult<IDialogueChoice?>(choices[0]);
        };
        var runner = new DialogueRunner(cancelDuringPrompt);
        runnerRef = runner;

        DialogueOutcome outcome = await runner.RunAsync(start, State);

        Assert.Same(start, outcome.TerminalNode);
        Assert.Null(outcome.ChosenChoice);
    }
}
