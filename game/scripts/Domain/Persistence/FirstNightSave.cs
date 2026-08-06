#nullable enable
namespace WorldofGoses.Domain.Persistence;

/// <summary>
/// The authored first night, persisted as semantic context only (schema v31).
///
/// There is deliberately no spirit position here. The fire spirit's place in the
/// world derives from <see cref="Stage"/>, the way building anchors derive from
/// placement; the city persists no authoritative visual coordinates.
/// </summary>
public sealed class FirstNightSave
{
    /// <summary>The <c>FirstNightStage</c> name, parsed case-insensitively.</summary>
    public string Stage { get; set; } = "";

    /// <summary>
    /// The open main-dialogue node, or null when the night waits on an action.
    /// This is what makes a save taken mid-conversation resume on the same line.
    /// </summary>
    public string? CurrentDialogueNodeId { get; set; }

    public int StartedAtTick { get; set; }
    public int? ConcludedAtTick { get; set; }
}
