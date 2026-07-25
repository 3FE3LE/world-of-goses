#nullable enable
using WorldofGoses.Domain;

namespace WorldofGoses.Presentation;

/// <summary>Translates semantic world events into player-facing copy.</summary>
public static class WorldEventTextFormatter
{
    public static string Format(WorldEvent evt) =>
        Format(evt.Kind, evt.SubjectName, evt.Amount);

    public static string Format(WorldEventKind kind, string subjectName, int amount) => kind switch
    {
        WorldEventKind.StockProduced => $"{subjectName} produced +{amount}",
        WorldEventKind.StockCapped => $"{subjectName} reached target stock",
        WorldEventKind.WorkersExhausted => $"{subjectName} stopped: workers exhausted",
        WorldEventKind.WorkerRecovered => $"{subjectName} resumed: workers recovered",
        WorldEventKind.DayBegan => "Sun rose — workers mobilised to their stations",
        WorldEventKind.NightBegan => "Sun set — workers returned home to rest",
        WorldEventKind.ProjectProgressed => $"{subjectName} made +{amount} work",
        WorldEventKind.ProjectPaused => $"{subjectName} paused by the player",
        WorldEventKind.ProjectResumed => $"{subjectName} resumed by the player",
        WorldEventKind.ProjectCompleted => $"{subjectName} completed",
        WorldEventKind.BuildingCreated => $"{subjectName} became a building",
        WorldEventKind.WellFedExpired => $"{subjectName} lost the WellFed buff",
        WorldEventKind.ProductionBlocked => $"{subjectName} waiting: missing inputs",
        WorldEventKind.ExpeditionDispatched => $"{subjectName} dispatched with +{amount} supplies",
        WorldEventKind.ExpeditionReturned => $"{subjectName} returned with +{amount}",
        WorldEventKind.ExpeditionFailed => $"{subjectName} failed to return",
        WorldEventKind.ExpeditionCancelled => $"{subjectName} was cancelled",
        WorldEventKind.MigrantArrived => $"{subjectName} joined the city",
        _ => subjectName,
    };
}
