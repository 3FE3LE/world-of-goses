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
        or WorldEventKind.ForestDemolished;

    public static IReadOnlyList<WorldEvent> SelectForPersistence(
        IReadOnlyList<WorldEvent> events)
    {
        var selected = new List<WorldEvent>();
        foreach (var evt in events)
        {
            if (!IsSignificant(evt.Kind)) continue;
            bool repeatedState = evt.Kind is WorldEventKind.StockCapped
                or WorldEventKind.WorkersExhausted
                or WorldEventKind.ProductionBlocked;
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

        int removeCount = selected.Count - MaximumPersistedEvents;
        if (removeCount > 0) selected.RemoveRange(0, removeCount);
        return selected;
    }

    private static bool SameIdentity(WorldEventSubject left, WorldEventSubject right) =>
        left.Kind == right.Kind && left.EntityId == right.EntityId;
}
