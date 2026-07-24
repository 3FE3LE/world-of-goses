#nullable enable
namespace WorldofGoses.Domain.Persistence;

/// <summary>Persisted semantic event. Player-facing text is never serialized.</summary>
public sealed class WorldEventSave
{
    public int Id { get; set; }
    public int Tick { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string SubjectKind { get; set; } = string.Empty;
    public int? SubjectEntityId { get; set; }
    public string SubjectDisplayName { get; set; } = string.Empty;
    public int Amount { get; set; }
    public int? CauseEventId { get; set; }
}
