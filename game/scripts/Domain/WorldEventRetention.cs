#nullable enable
using System.Collections.Generic;

namespace WorldofGoses.Domain;

/// <summary>Defines the bounded event subset that merits durable history.</summary>
public static class WorldEventRetention
{
    public const int MaximumPersistedEvents = 128;

    public static bool IsSignificant(WorldEventKind kind) => kind is
        WorldEventKind.StockCapped
        or WorldEventKind.WorkersExhausted
        or WorldEventKind.WorkerRecovered
        or WorldEventKind.ProjectPaused
        or WorldEventKind.ProjectResumed
        or WorldEventKind.ProjectCompleted
        or WorldEventKind.BuildingCreated
        or WorldEventKind.WellFedExpired
        or WorldEventKind.ProductionBlocked
        or WorldEventKind.ForestDemolished
        or WorldEventKind.ExpeditionDispatched
        or WorldEventKind.MigrantArrived
        or WorldEventKind.ExpeditionReturned
        or WorldEventKind.ExpeditionFailed
        or WorldEventKind.ExpeditionCancelled
        or WorldEventKind.ExpeditionRetreated
        or WorldEventKind.FoodRationShortfall
        or WorldEventKind.ExpeditionEncounterResolved
        or WorldEventKind.WoundSustained
        or WorldEventKind.WoundRecoveryStarted
        or WorldEventKind.WoundRecoveryCompleted
        or WorldEventKind.TerritoryAdvanced
        or WorldEventKind.CropReady
        or WorldEventKind.CropHarvested
        or WorldEventKind.SpiritDeparted;

    public static IReadOnlyList<WorldEvent> SelectForPersistence(
        IReadOnlyList<WorldEvent> events,
        IReadOnlySet<int>? pinnedEventIds = null)
    {
        var selected = new List<WorldEvent>();
        foreach (var evt in events)
        {
            if (!IsSignificant(evt.Kind)) continue;
            bool repeatedState = evt.Kind is WorldEventKind.StockCapped
                or WorldEventKind.WorkersExhausted
                or WorldEventKind.ProductionBlocked
                or WorldEventKind.FoodRationShortfall;
            if (repeatedState && selected.Count > 0
                && selected[^1].Kind == evt.Kind
                && SameIdentity(selected[^1].Subject, evt.Subject))
            {
                selected[^1] = evt;
            }
            else
            {
                selected.Add(evt);
            }
        }

        for (int index = 0; selected.Count > MaximumPersistedEvents
            && index < selected.Count;)
        {
            if (pinnedEventIds?.Contains(selected[index].Id.Value) == true)
            {
                index++;
                continue;
            }
            selected.RemoveAt(index);
        }
        if (selected.Count > MaximumPersistedEvents)
        {
            selected.RemoveRange(0, selected.Count - MaximumPersistedEvents);
        }
        return selected;
    }

    private static bool SameIdentity(WorldEventSubject left, WorldEventSubject right) =>
        left.Kind == right.Kind && left.EntityId == right.EntityId;
}
