using System;
using System.Collections.Generic;
using System.Linq;

namespace WorldofGoses.Domain;

/// <summary>
/// Advances one world across an elapsed tick range. This is the shared seam
/// for live/offline-equivalent time progression: inactive worlds can jump as
/// one batch, while active worlds keep using the canonical per-tick rules until
/// equivalent causal batching is introduced for each subsystem.
/// </summary>
public static class WorldTimeAdvance
{
    public sealed record Result(
        int TicksElapsed,
        int LastActiveTick,
        int StockAdded,
        int BatchedTicks,
        int SteppedTicks,
        IReadOnlyList<WorldEvent> Events)
    {
        public static readonly Result None = new(0, 0, 0, 0, 0, Array.Empty<WorldEvent>());
    }

    public static Result Advance(CityWorld world, int ticksToApply)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (ticksToApply <= 0) return Result.None;

        int eventCursor = world.Log.Events.Count;
        if (world.Buildings.Count == 0
            && world.Projects.Count == 0
            && world.CultivationSites.Count == 0
            && !world.Expeditions.Values.Any(expedition =>
                expedition.Status == ExpeditionStatus.Active))
        {
            world.AdvanceIdleTicks(ticksToApply);
            return new Result(ticksToApply, 0, 0, ticksToApply, 0, Array.Empty<WorldEvent>());
        }

        int stockAdded = 0;
        int lastActiveTick = 0;
        int elapsedTicks = 0;
        int batchedTicks = 0;
        int steppedTicks = 0;
        while (elapsedTicks < ticksToApply)
        {
            int batched = world.TryAdvanceQuiescentTicks(ticksToApply - elapsedTicks);
            if (batched > 0)
            {
                elapsedTicks += batched;
                batchedTicks += batched;
                continue;
            }

            world.AdvanceWorldTick();
            elapsedTicks++;
            steppedTicks++;
            bool anyProduced = false;
            foreach (var building in world.Buildings.Values)
            {
                if (building.LastTickProduction <= 0) continue;
                stockAdded += building.LastTickProduction;
                anyProduced = true;
            }
            if (anyProduced) lastActiveTick = elapsedTicks;
        }

        return new Result(
            ticksToApply,
            lastActiveTick,
            stockAdded,
            batchedTicks,
            steppedTicks,
            EventsSince(world.Log.Events, eventCursor));
    }

    private static IReadOnlyList<WorldEvent> EventsSince(
        IReadOnlyList<WorldEvent> events,
        int cursor)
    {
        if (cursor >= events.Count) return Array.Empty<WorldEvent>();

        var result = new List<WorldEvent>(events.Count - cursor);
        for (int index = cursor; index < events.Count; index++)
        {
            result.Add(events[index]);
        }
        return result;
    }
}
