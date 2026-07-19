using System.Collections.Generic;
using Godot;
using WorldofGoses.Domain;

namespace WorldofGoses;

/// <summary>
/// Top-of-screen status strip in the macro view. Shows stock and
/// staffing for every building plus the citizens who remain free.
///
/// Initialization-order note: the label is constructed via a field
/// initializer (not in <c>_Ready()</c>) because the parent macro
/// view's <c>_Ready()</c> calls <see cref="Refresh"/> before this
/// panel's own <c>_Ready</c> has fired.
/// </summary>
public partial class CityStatusPanel : PanelContainer
{
    private readonly Label _label = new()
    {
        Text = "",
        HorizontalAlignment = HorizontalAlignment.Center,
    };

    public override void _Ready()
    {
        AddChild(_label);
    }

    public void Refresh(CityWorldController controller)
    {
        var free = new List<string>();
        foreach (var citizen in controller.Citizens().Values)
        {
            if (!citizen.CurrentAssignment.HasValue) free.Add(citizen.Name);
        }

        if (controller.World.Buildings.Count == 0)
        {
            _label.Text = "City (no buildings — loaded save was empty)";
            return;
        }

        var buildingSummaries = new List<string>(controller.World.Buildings.Count);
        foreach (var building in controller.World.Buildings.Values)
        {
            buildingSummaries.Add(
                $"{building.DisplayName}: {building.Stock}/{building.StorageCapacity} " +
                $"{building.ResourceUnit} · {building.AssignedCount}/{building.WorkerCapacity} workers" +
                DescribeStopCause(building));
        }

        string freeCitizens = free.Count == 0 ? "none" : string.Join(", ", free);
        string clock = DescribeClock(controller);
        string upkeep = DescribeUpkeep(controller);
        string mobilisation = DescribeMobilisation(controller);
        _label.Text = clock
            + (upkeep.Length > 0 ? $"  |  {upkeep}" : string.Empty)
            + $"  |  {mobilisation}"
            + $"  |  {string.Join("  |  ", buildingSummaries)}  |  Free: {freeCitizens}";
    }

    private static string DescribeMobilisation(CityWorldController controller)
    {
        int atWork = 0;
        int atHome = 0;
        foreach (var citizen in controller.Citizens().Values)
        {
            if (citizen.CurrentLocation == CitizenLocation.AtWork) atWork++;
            else atHome++;
        }
        return $"At work: {atWork} · At home: {atHome}";
    }

    /// <summary>
    /// Compact suffix for the per-building summary in the macro strip.
    /// New causes plug in as additional switch arms.
    /// </summary>
    private static string DescribeStopCause(Building building)
    {
        return building.StopCause switch
        {
            ProductionStopCause.Paused => " · ⏸ paused",
            ProductionStopCause.TargetReached => " · ✓ full",
            ProductionStopCause.WorkersExhausted => " · ⏸ exhausted",
            ProductionStopCause.NoWorkers => " · ⚠ no workers",
            ProductionStopCause.Night => " · 🌙 night",
            _ => string.Empty,
        };
    }

    private static string DescribeClock(CityWorldController controller)
    {
        int tick = controller.World.CurrentTick;
        bool day = GameClock.IsDaytime(tick);
        int dayNumber = GameClock.DayNumber(tick);
        int hour = (int)(GameClock.DayFraction(tick) * 24);
        string emoji = day ? "☀" : "🌙";
        return $"Day {dayNumber} · {hour:D2}:00 {emoji}";
    }

    private static string DescribeUpkeep(CityWorldController controller)
    {
        int rate = Upkeep.StonePerTick(controller.Citizens().Count);
        if (rate <= 0) return string.Empty;
        return $"Upkeep: -{rate} stone/tick";
    }
}
