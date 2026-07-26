#nullable enable
using WorldofGoses.Domain;
using WorldofGoses.Ui;

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

    public static string FormatLocalized(WorldEvent evt) =>
        FormatLocalized(evt.Kind, evt.SubjectName, evt.Amount);

    public static string FormatLocalized(WorldEventKind kind, string subjectName, int amount) => kind switch
    {
        WorldEventKind.StockProduced => UiText.Format("event.stock_produced", subjectName, amount),
        WorldEventKind.StockCapped => UiText.Format("event.stock_capped", subjectName),
        WorldEventKind.WorkersExhausted => UiText.Format("event.workers_exhausted", subjectName),
        WorldEventKind.WorkerRecovered => UiText.Format("event.worker_recovered", subjectName),
        WorldEventKind.DayBegan => UiText.Get("event.day_began"),
        WorldEventKind.NightBegan => UiText.Get("event.night_began"),
        WorldEventKind.ProjectProgressed => UiText.Format("event.project_progressed", subjectName, amount),
        WorldEventKind.ProjectPaused => UiText.Format("event.project_paused", subjectName),
        WorldEventKind.ProjectResumed => UiText.Format("event.project_resumed", subjectName),
        WorldEventKind.ProjectCompleted => UiText.Format("event.project_completed", subjectName),
        WorldEventKind.BuildingCreated => UiText.Format("event.building_created", subjectName),
        WorldEventKind.WellFedExpired => UiText.Format("event.well_fed_expired", subjectName),
        WorldEventKind.ProductionBlocked => UiText.Format("event.production_blocked", subjectName),
        WorldEventKind.ExpeditionDispatched => UiText.Format("event.expedition_dispatched", subjectName, amount),
        WorldEventKind.ExpeditionReturned => UiText.Format("event.expedition_returned", subjectName, amount),
        WorldEventKind.ExpeditionFailed => UiText.Format("event.expedition_failed", subjectName),
        WorldEventKind.ExpeditionCancelled => UiText.Format("event.expedition_cancelled", subjectName),
        WorldEventKind.MigrantArrived => UiText.Format("event.migrant_arrived", subjectName),
        _ => subjectName,
    };
}
