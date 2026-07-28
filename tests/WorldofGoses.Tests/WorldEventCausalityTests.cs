using WorldofGoses.Domain;
using Xunit;

namespace WorldofGoses.Tests;

/// <summary>
/// Causal-event wiring: <see cref="WorldEvent.CauseEventId"/>
/// identifies the prior event that caused the current one.
/// <see cref="WorldEventKind.StockProduced"/> carries the previous
/// <see cref="WorldEventKind.StockProduced"/> for the same building
/// (or the day's <see cref="WorldEventKind.DayBegan"/> for the first
/// one). <see cref="WorldEventKind.ProductionBlocked"/> carries the
/// most recent matching event so the offline report can render a
/// causal chain.
/// </summary>
public class WorldEventCausalityTests
{
    [Fact]
    public void StockProduced_HasCauseFromPreviousStockProduced()
    {
        var world = TestHelpers.NewProductionWorld();
        var quarry = world.GetBuilding(new BuildingId(1))!;

        // Tick 1: first production. Cause should be null (no prior
        // StockProduced for this building yet — DayBegan might be
        // earlier in the day, but FindCauseEvent only considers the
        // subject match; the first of day gets a fresh null).
        TestHelpers.AdvanceToNextProductionCycle(world);
        var firstProduced = FindLastProduced(world, quarry.DisplayName);
        Assert.NotNull(firstProduced);

        // Tick 2: second production. Cause should now reference tick 1.
        TestHelpers.AdvanceToNextProductionCycle(world);
        var secondProduced = FindLastProduced(world, quarry.DisplayName);
        Assert.NotEqual(firstProduced!.Id, secondProduced!.Id);
        Assert.Equal(firstProduced.Id, secondProduced.CauseEventId);
    }

    [Fact]
    public void RemovingPrototypeIron_DoesNotBlockQuarryProduction()
    {
        var world = TestHelpers.NewProductionWorld();
        var quarry = world.GetBuilding(new BuildingId(1))!;

        // Tick 1: produce Stone under the current labour/stamina contract.
        TestHelpers.AdvanceToNextProductionCycle(world);
        var lastProduced = FindLastProduced(world, quarry.DisplayName);
        Assert.NotNull(lastProduced);

        // Iron has no sustainable source in the playable bootstrap and
        // therefore cannot be an invisible operating requirement.
        world.TryConsumeResource(ResourceType.Iron, world.TotalStockOf(ResourceType.Iron));
        TestHelpers.AdvanceToNextProductionCycle(world);
        var nextProduced = FindLastProduced(world, quarry.DisplayName);
        Assert.NotNull(nextProduced);
        Assert.NotEqual(lastProduced!.Id, nextProduced!.Id);
    }

    private static WorldEvent? FindLastProduced(CityWorld world, string subjectName)
    {
        for (int i = world.Log.Events.Count - 1; i >= 0; i--)
        {
            var evt = world.Log.Events[i];
            if (evt.Kind == WorldEventKind.StockProduced && evt.SubjectName == subjectName)
            {
                return evt;
            }
        }
        return null;
    }
}
