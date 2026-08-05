#nullable enable
using System.Collections.Generic;

namespace WorldofGoses.Domain.Persistence;

/// <summary>Serializable stable IDs retained from founder onboarding.</summary>
public sealed class FounderNarrativeMemorySave
{
    public List<string> AnswerIds { get; set; } = new();
    public string? BelievedFinalWordId { get; set; }
    public string? PreservedDetailId { get; set; }
    public List<string> EchoIds { get; set; } = new();
}
