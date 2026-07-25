#nullable enable
using System;
using System.Collections.Generic;

namespace WorldofGoses.Domain;

public sealed class FounderNarrativeSession
{
    private readonly Dictionary<string, string> _answers = new();

    public IReadOnlyDictionary<string, string> Answers => _answers;

    public void Answer(string questionId, string choiceId)
    {
        FounderNarrativeQuestion question =
            FounderNarrativeCatalog.GetQuestion(questionId);
        bool known = false;
        foreach (FounderNarrativeChoice choice in question.Choices)
        {
            if (choice.Id != choiceId) continue;
            known = true;
            break;
        }
        if (!known)
        {
            throw new ArgumentOutOfRangeException(
                nameof(choiceId), choiceId, "Choice does not belong to question.");
        }
        _answers[questionId] = choiceId;
    }

    public bool TryGetAnswer(string questionId, out string choiceId) =>
        _answers.TryGetValue(questionId, out choiceId!);

    public bool IsComplete
    {
        get
        {
            foreach (FounderNarrativeQuestion question in FounderNarrativeCatalog.Questions)
            {
                if (!_answers.ContainsKey(question.Id)) return false;
            }
            return true;
        }
    }
}
