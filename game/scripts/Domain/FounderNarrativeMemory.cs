#nullable enable
using System;
using System.Collections.Generic;

namespace WorldofGoses.Domain;

/// <summary>Stable content identifiers retained from the astral onboarding.</summary>
public sealed record FounderNarrativeMemory
{
    public FounderNarrativeMemory(
        IReadOnlyList<string> answerIds,
        string? believedFinalWordId,
        string? preservedDetailId,
        IReadOnlyList<string> echoIds)
    {
        ArgumentNullException.ThrowIfNull(answerIds);
        ArgumentNullException.ThrowIfNull(echoIds);
        AnswerIds = Array.AsReadOnly(Copy(answerIds));
        BelievedFinalWordId = believedFinalWordId;
        PreservedDetailId = preservedDetailId;
        EchoIds = Array.AsReadOnly(Copy(echoIds));
    }

    public IReadOnlyList<string> AnswerIds { get; }
    public string? BelievedFinalWordId { get; }
    public string? PreservedDetailId { get; }
    public IReadOnlyList<string> EchoIds { get; }

    public static FounderNarrativeMemory Empty { get; } =
        new(Array.Empty<string>(), null, null, Array.Empty<string>());

    public bool Equals(FounderNarrativeMemory? other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other is null
            || BelievedFinalWordId != other.BelievedFinalWordId
            || PreservedDetailId != other.PreservedDetailId
            || AnswerIds.Count != other.AnswerIds.Count
            || EchoIds.Count != other.EchoIds.Count)
        {
            return false;
        }
        for (int index = 0; index < AnswerIds.Count; index++)
        {
            if (AnswerIds[index] != other.AnswerIds[index]) return false;
        }
        for (int index = 0; index < EchoIds.Count; index++)
        {
            if (EchoIds[index] != other.EchoIds[index]) return false;
        }
        return true;
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(BelievedFinalWordId, StringComparer.Ordinal);
        hash.Add(PreservedDetailId, StringComparer.Ordinal);
        foreach (string answerId in AnswerIds) hash.Add(answerId, StringComparer.Ordinal);
        foreach (string echoId in EchoIds) hash.Add(echoId, StringComparer.Ordinal);
        return hash.ToHashCode();
    }

    private static string[] Copy(IReadOnlyList<string> source)
    {
        var copy = new string[source.Count];
        for (int index = 0; index < source.Count; index++) copy[index] = source[index];
        return copy;
    }
}
