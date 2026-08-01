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
        WorldEventKind.ExpeditionRetreated => $"{subjectName} retreated and returned",
        WorldEventKind.MigrantArrived => $"{subjectName} joined the city",
        WorldEventKind.FoodRationShortfall => $"Food ran short: {amount} residents went unfed",
        WorldEventKind.ExpeditionEncounterResolved =>
            $"{subjectName} encounter: {DescribeEncounterOutcome(amount)}",
        WorldEventKind.WoundSustained => $"{subjectName} returned wounded",
        WorldEventKind.WoundRecoveryStarted => $"{subjectName} began treatment with {amount} Food",
        WorldEventKind.WoundRecoveryCompleted => $"{subjectName} completed treatment",
        WorldEventKind.TerritoryAdvanced => $"{subjectName} advanced to {(ParcelTerritoryState)amount}",
        WorldEventKind.CropReady => $"{subjectName} crop became ready",
        WorldEventKind.CropHarvested => $"{subjectName} yielded +{amount} Food",
        _ => subjectName,
    };

    private static string DescribeEncounterOutcome(int amount) => (ExpeditionEncounterOutcome)amount switch
    {
        ExpeditionEncounterOutcome.FullSuccess => "full success",
        ExpeditionEncounterOutcome.PartialSuccess => "partial success",
        _ => "setback",
    };

    public static string FormatLocalized(WorldEvent evt) =>
        FormatLocalized(evt.Kind, evt.SubjectName, evt.Amount);

    public static string FormatLocalized(WorldEventKind kind, string subjectName, int amount)
    {
        string localizedSubject = UiText.Get(subjectName);
        return kind switch
    {
        WorldEventKind.StockProduced => UiText.Format("event.stock_produced", localizedSubject, amount),
        WorldEventKind.StockCapped => UiText.Format("event.stock_capped", localizedSubject),
        WorldEventKind.WorkersExhausted => UiText.Format("event.workers_exhausted", localizedSubject),
        WorldEventKind.WorkerRecovered => UiText.Format("event.worker_recovered", localizedSubject),
        WorldEventKind.DayBegan => UiText.Get("event.day_began"),
        WorldEventKind.NightBegan => UiText.Get("event.night_began"),
        WorldEventKind.ProjectProgressed => UiText.Format("event.project_progressed", localizedSubject, amount),
        WorldEventKind.ProjectPaused => UiText.Format("event.project_paused", localizedSubject),
        WorldEventKind.ProjectResumed => UiText.Format("event.project_resumed", localizedSubject),
        WorldEventKind.ProjectCompleted => UiText.Format("event.project_completed", localizedSubject),
        WorldEventKind.BuildingCreated => UiText.Format("event.building_created", localizedSubject),
        WorldEventKind.WellFedExpired => UiText.Format("event.well_fed_expired", localizedSubject),
        WorldEventKind.ProductionBlocked => UiText.Format("event.production_blocked", localizedSubject),
        WorldEventKind.ExpeditionDispatched => UiText.Format("event.expedition_dispatched", localizedSubject, amount),
        WorldEventKind.ExpeditionReturned => UiText.Format("event.expedition_returned", localizedSubject, amount),
        WorldEventKind.ExpeditionFailed => UiText.Format("event.expedition_failed", localizedSubject),
        WorldEventKind.ExpeditionCancelled => UiText.Format("event.expedition_cancelled", localizedSubject),
        WorldEventKind.ExpeditionRetreated => UiText.Format("event.expedition_retreated", localizedSubject),
        WorldEventKind.MigrantArrived => UiText.Format("event.migrant_arrived", localizedSubject),
        WorldEventKind.FoodRationShortfall => UiText.Format("event.food_ration_shortfall", amount),
        WorldEventKind.ExpeditionEncounterResolved => UiText.Format(
            "event.expedition_encounter_resolved", localizedSubject, DescribeEncounterOutcomeLocalized(amount)),
        WorldEventKind.WoundSustained => UiText.Format("event.wound_sustained", localizedSubject),
        WorldEventKind.WoundRecoveryStarted => UiText.Format(
            "event.wound_recovery_started", localizedSubject, amount),
        WorldEventKind.WoundRecoveryCompleted => UiText.Format(
            "event.wound_recovery_completed", localizedSubject),
        WorldEventKind.TerritoryAdvanced => UiText.Format(
            "event.territory_advanced", localizedSubject, DescribeTerritoryStateLocalized(amount)),
        WorldEventKind.CropReady => UiText.Format("event.crop_ready", localizedSubject),
        WorldEventKind.CropHarvested => UiText.Format(
            "event.crop_harvested", localizedSubject, amount),
        _ => localizedSubject,
    };
    }

    private static string DescribeEncounterOutcomeLocalized(int amount) => (ExpeditionEncounterOutcome)amount switch
    {
        ExpeditionEncounterOutcome.FullSuccess => UiText.Get("event.encounter_outcome.full_success"),
        ExpeditionEncounterOutcome.PartialSuccess => UiText.Get("event.encounter_outcome.partial_success"),
        _ => UiText.Get("event.encounter_outcome.setback"),
    };

    private static string DescribeTerritoryStateLocalized(int amount) =>
        UiText.Get((ParcelTerritoryState)amount switch
        {
            ParcelTerritoryState.Reconnoitred => "ui.territory.reconnoitred",
            ParcelTerritoryState.RouteSecured => "ui.territory.route_secured",
            ParcelTerritoryState.Available => "ui.territory.available",
            _ => "ui.territory.locked",
        });
}
