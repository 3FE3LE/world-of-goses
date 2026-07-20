#nullable enable
using System.Collections.Generic;
using System.Linq;
using Godot;
using WorldofGoses.Domain;

namespace WorldofGoses;

/// <summary>
/// Top-of-screen status strip in the macro view. Shows stock and
/// staffing for every building plus the citizens who remain free.
///
/// The visual styling (font sizes, panel borders, colours) comes from
/// the project's default theme; this class only renders the textual
/// summary in a single label that wraps when the window is narrow.
/// </summary>
public partial class CityStatusPanel : PanelContainer
{
    private LineageThemeSignals? _themeSignals;
    private readonly Label _label = new()
    {
        Text = "",
        HorizontalAlignment = HorizontalAlignment.Center,
        AutowrapMode = TextServer.AutowrapMode.WordSmart,
        ThemeTypeVariation = "BodySmall",
    };

    public override void _Ready()
    {
        AddChild(_label);
        AddThemeStyleboxOverride("panel", LineageThemeRegistry.GetStyleBox(LineageThemeRegistry.ComponentPanel));
        _themeSignals = GetNodeOrNull<LineageThemeSignals>("/root/LineageThemeSignals");
        if (_themeSignals is not null)
        {
            _themeSignals.LineageChanged += OnLineageChanged;
        }
    }

    public override void _ExitTree()
    {
        if (_themeSignals is not null) _themeSignals.LineageChanged -= OnLineageChanged;
    }

    private void OnLineageChanged(string lineage) => AddThemeStyleboxOverride(
        "panel", LineageThemeRegistry.GetStyleBox(LineageThemeRegistry.ComponentPanel));

    public void Refresh(CityWorldController controller)
    {
        var parts = new List<string>();
        IReadOnlyDictionary<CitizenId, Citizen> citizens = controller.Citizens();
        var free = new List<string>();
        foreach (var citizen in citizens.Values)
        {
            if (!citizen.CurrentAssignment.HasValue) free.Add(citizen.Name);
        }

        parts.Add(DescribeClock(controller));

        string upkeep = DescribeUpkeep(controller);
        if (upkeep.Length > 0) parts.Add(upkeep);
        parts.Add(DescribeMobilisation(citizens));

        if (controller.World.Projects.Count > 0)
        {
            foreach (var project in controller.World.Projects.Values)
            {
                parts.Add(DescribeProject(project));
            }
        }

        foreach (var building in controller.World.Buildings.Values)
        {
            parts.Add(
                $"{building.DisplayName}: {building.Stock}/{building.StorageCapacity} " +
                $"{building.ResourceUnit} · {building.AssignedCount}/{building.WorkerCapacity} workers" +
                DescribeStopCause(building));
        }

        parts.Add(
            $"Free: {(free.Count == 0 ? "none" : string.Join(", ", free))}");

        if (controller.World.Buildings.Count == 0 && controller.World.Projects.Count == 0)
        {
            string heroName = controller.HeroOrNull()?.Name ?? "not established";
            _label.Text = string.Join("  |  ", parts)
                + "  |  Hero: " + heroName
                + "  |  No buildings yet";
            return;
        }

        _label.Text = string.Join("  |  ", parts);
    }

    private static string DescribeMobilisation(IReadOnlyDictionary<CitizenId, Citizen> citizens)
    {
        int atWork = 0;
        int atHome = 0;
        foreach (var citizen in citizens.Values)
        {
            if (citizen.CurrentLocation == CitizenLocation.AtWork) atWork++;
            else atHome++;
        }
        return $"At work: {atWork} · At home: {atHome}";
    }

    private static string DescribeStopCause(Building building) => building.StopCause switch
    {
        ProductionStopCause.Paused => " · ⏸ paused",
        ProductionStopCause.TargetReached => " · ✓ full",
        ProductionStopCause.WorkersExhausted => " · ⏸ exhausted",
        ProductionStopCause.NoWorkers => " · ⚠ no workers",
        ProductionStopCause.Night => " · 🌙 night",
        _ => string.Empty,
    };

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

    private static string DescribeProject(ConstructionProject project)
    {
        var phase = ConstructionRules.PhaseFor(project.Progress, project.RequiredWork);
        return $"Project: {project.DisplayName} {project.Progress}/{project.RequiredWork} " +
            $"({ConstructionRules.Describe(phase)}) · " +
            $"{project.AssignedCount}/{project.WorkerCapacity} workers" +
            (project.Enabled ? string.Empty : " · ⏸ paused");
    }
}
