using System;
using System.Linq;
using WorldofGoses.Domain;
using WorldofGoses.Domain.Persistence;
using Xunit;

namespace WorldofGoses.Tests;

public sealed class WorldTimeAdvanceTests
{
    [Fact]
    public void Advance_QuiescentStructuredWorld_MatchesCanonicalStepping()
    {
        var source = TestHelpers.WorldWithHome();
        var snapshot = WorldPersistence.Capture(source, DateTimeOffset.UnixEpoch.AddDays(1));
        var canonical = CityWorld.FromSave(snapshot);
        var batched = CityWorld.FromSave(snapshot);
        const int ticks = GameClock.TicksPerInGameDay * 3 + 217;

        for (int i = 0; i < ticks; i++) canonical.AdvanceWorldTick();
        var result = WorldTimeAdvance.Advance(batched, ticks);

        Assert.True(result.BatchedTicks > ticks - 10);
        Assert.True(result.SteppedTicks <= 6);
        Assert.Equal(
            WorldPersistence.SerializeToJson(WorldPersistence.Capture(
                canonical, DateTimeOffset.UnixEpoch.AddDays(2))),
            WorldPersistence.SerializeToJson(WorldPersistence.Capture(
                batched, DateTimeOffset.UnixEpoch.AddDays(2))));
        Assert.Equal(
            canonical.Log.Events.Select(EventIdentity),
            batched.Log.Events.Select(EventIdentity));
    }

    [Fact]
    public void Advance_AssignedWorld_FallsBackToCanonicalStepping()
    {
        var world = TestHelpers.NewProductionWorld();

        var result = WorldTimeAdvance.Advance(world, 25);

        Assert.Equal(0, result.BatchedTicks);
        Assert.Equal(25, result.SteppedTicks);
    }

    private static string EventIdentity(WorldEvent evt) =>
        $"{evt.Tick}|{evt.Kind}|{evt.SubjectName}|{evt.Amount}|{evt.CauseEventId}";
}
