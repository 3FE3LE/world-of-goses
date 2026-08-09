#nullable enable
using System.Collections.Generic;
using WorldofGoses.Domain;

namespace WorldofGoses.Presentation;

/// <summary>Shared meaningful-event and compaction rules for Chronicle surfaces.</summary>
public static class ChronicleEventProjection
{
    public sealed record Item(
        WorldEventKind Kind,
        string SubjectName,
        int Amount,
        int FirstTick,
        int LastTick,
        string Summary);

    public static IReadOnlyList<WorldEvent> MeaningfulEvents(IReadOnlyList<WorldEvent> events)
    {
        var visible = new List<WorldEvent>();
        foreach (WorldEvent evt in events)
        {
            if (evt.Kind is WorldEventKind.StockProduced or WorldEventKind.CropHarvested) continue;
            visible.Add(evt);
        }
        return visible;
    }

    public static IReadOnlyList<Item> Compact(IReadOnlyList<WorldEvent> events)
    {
        var compacted = new List<Item>();
        foreach (WorldEvent evt in events)
        {
            bool additive = evt.Amount > 0
                && evt.Kind is WorldEventKind.StockProduced or WorldEventKind.ProjectProgressed;
            if (additive && compacted.Count > 0
                && compacted[^1].Kind == evt.Kind
                && compacted[^1].SubjectName == evt.SubjectName)
            {
                Item previous = compacted[^1];
                int amount = previous.Amount + evt.Amount;
                compacted[^1] = previous with
                {
                    Amount = amount,
                    LastTick = evt.Tick,
                    Summary = SummariseCompacted(evt.Kind, evt.SubjectName, amount),
                };
                continue;
            }

            bool repeatedState = evt.Kind is WorldEventKind.StockCapped
                or WorldEventKind.WorkersExhausted
                or WorldEventKind.ProductionBlocked;
            if (repeatedState && compacted.Count > 0
                && compacted[^1].Kind == evt.Kind
                && compacted[^1].SubjectName == evt.SubjectName)
            {
                compacted[^1] = compacted[^1] with { LastTick = evt.Tick };
                continue;
            }

            compacted.Add(new Item(
                evt.Kind, evt.SubjectName, evt.Amount, evt.Tick, evt.Tick,
                WorldEventTextFormatter.Format(evt)));
        }
        return compacted;
    }

    public static IReadOnlyList<Item> NewestMeaningful(
        IReadOnlyList<WorldEvent> events,
        int maximum)
    {
        IReadOnlyList<Item> compacted = Compact(MeaningfulEvents(events));
        int take = System.Math.Max(0, maximum);
        int start = System.Math.Max(0, compacted.Count - take);
        var newest = new List<Item>(compacted.Count - start);
        for (int i = start; i < compacted.Count; i++) newest.Add(compacted[i]);
        return newest;
    }

    private static string SummariseCompacted(
        WorldEventKind kind,
        string subjectName,
        int amount) => kind switch
    {
        WorldEventKind.StockProduced => $"{subjectName} produced +{amount}",
        WorldEventKind.ProjectProgressed => $"{subjectName} made +{amount} work",
        _ => subjectName,
    };
}
