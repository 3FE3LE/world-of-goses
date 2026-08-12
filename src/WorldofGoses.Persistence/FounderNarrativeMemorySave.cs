#nullable enable
using System.Collections.Generic;

using WorldofGoses.Domain;
namespace WorldofGoses.Persistence;

/// <summary>Serializable stable IDs retained from founder onboarding.</summary>
public sealed class FounderNarrativeMemorySave
{
    public List<string> AnswerIds { get; set; } = new();
    public string? BelievedFinalWordId { get; set; }
    public string? PreservedDetailId { get; set; }
    public List<string> EchoIds { get; set; } = new();
}
