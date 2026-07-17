using System;
using Godot;
using WorldofGoses.Domain;

namespace WorldofGoses;

/// <summary>
/// Macro city view. Shows the city as a whole: per-building plots that
/// open the detail view when clicked, decorative macro citizen
/// activity, a status panel, a one-time "welcome back" banner when
/// offline progression ran during load.
///
/// Persistence is the controller's responsibility — saving is silent.
/// </summary>
public partial class CityMacroView : Control
{
    [Export] public NodePath QuarryPlotPath { get; set; } = "QuarryPlot";
    [Export] public NodePath FarmPlotPath { get; set; } = "FarmPlot";
    [Export] public NodePath ActivityPath { get; set; } = "MacroCitizenActivity";
    [Export] public NodePath StatusPanelPath { get; set; } = "CityStatusPanel";
    [Export] public NodePath DetailViewPath { get; set; } = "../BuildingDetailView";
    [Export] public NodePath OfflineReportLabelPath { get; set; } = "OfflineReportLabel";

    private CityWorldController _controller = null!;
    private BuildingPlot _quarryPlot = null!;
    private BuildingPlot _farmPlot = null!;
    private MacroCitizenActivity _activity = null!;
    private CityStatusPanel _statusPanel = null!;
    private BuildingDetailView _detailView = null!;
    private Label _offlineLabel = null!;

    public override void _Ready()
    {
        _controller = GetParent().GetNode<CityWorldController>("CityWorldController");
        _quarryPlot = GetNode<BuildingPlot>(QuarryPlotPath);
        _farmPlot = GetNode<BuildingPlot>(FarmPlotPath);
        _activity = GetNode<MacroCitizenActivity>(ActivityPath);
        _statusPanel = GetNode<CityStatusPanel>(StatusPanelPath);
        _detailView = GetNode<BuildingDetailView>(DetailViewPath);
        _offlineLabel = GetNode<Label>(OfflineReportLabelPath);

        _quarryPlot.BuildingClicked += OnBuildingClicked;
        _farmPlot.BuildingClicked += OnBuildingClicked;
        _controller.BuildingStateChanged += OnAnyBuildingStateChanged;
        _controller.SelectionChanged += OnSelectionChanged;

        _statusPanel.Refresh(_controller);
        _activity.Populate();

        if (_controller.LastOfflineReport is { HadProgression: true } report)
        {
            _offlineLabel.Text = FormatOfflineReport(report);
            _offlineLabel.Visible = true;
        }
    }

    public override void _ExitTree()
    {
        if (_quarryPlot is not null) _quarryPlot.BuildingClicked -= OnBuildingClicked;
        if (_farmPlot is not null) _farmPlot.BuildingClicked -= OnBuildingClicked;
        if (_controller is not null)
        {
            _controller.BuildingStateChanged -= OnAnyBuildingStateChanged;
            _controller.SelectionChanged -= OnSelectionChanged;
        }
    }

    private void OnBuildingClicked(int buildingId)
    {
        GD.Print($"CityMacroView: building clicked, buildingId={buildingId}");
        var id = new BuildingId(buildingId);
        if (_controller.SelectBuilding(id))
        {
            _detailView.ShowBuilding(id);
        }
    }

    private void OnAnyBuildingStateChanged(int buildingId) =>
        _statusPanel.Refresh(_controller);

    private void OnSelectionChanged(int selectionState)
    {
        if ((CityWorldController.Selection)selectionState == CityWorldController.Selection.MacroView)
        {
            Show();
            _activity.Populate();
            _statusPanel.Refresh(_controller);
        }
        else
        {
            Hide();
        }
    }

    public void OnReturnedToCity()
    {
        Show();
        _activity.Populate();
        _statusPanel.Refresh(_controller);
    }

    private static string FormatOfflineReport(OfflineProgressionReport r)
    {
        var time = FormatSimulatedTime(r.SimulatedTime);
        return
            $"Welcome back · {time} simulated · " +
            $"+{r.TicksApplied} ticks · " +
            $"+{r.StockAdded} stock mined · " +
            $"+{r.TicksApplied} exp per assigned worker";
    }

    private static string FormatSimulatedTime(TimeSpan ts)
    {
        if (ts.TotalDays >= 1) return $"{(int)ts.TotalDays}d {ts.Hours}h";
        if (ts.TotalHours >= 1) return $"{(int)ts.TotalHours}h {ts.Minutes}m";
        if (ts.TotalMinutes >= 1) return $"{(int)ts.TotalMinutes}m {ts.Seconds}s";
        return $"{(int)ts.TotalSeconds}s";
    }
}
