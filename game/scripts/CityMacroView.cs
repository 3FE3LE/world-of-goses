using System;
using System.Linq;
using Godot;
using WorldofGoses.Domain;

namespace WorldofGoses;

/// <summary>
/// Macro city view. A newly founded world legitimately contains one hero and
/// no buildings, so this view renders that empty state without manufacturing
/// production plots or treating the save as broken.
/// </summary>
public partial class CityMacroView : Control
{
    [Export] public NodePath ActivityPath { get; set; } = "MacroCitizenActivity";
    [Export] public NodePath StatusPanelPath { get; set; } = "CityStatusPanel";
    [Export] public NodePath OfflineReportLabelPath { get; set; } = "OfflineReportLabel";
    [Export] public NodePath ConstructionPanelPath { get; set; } = "Center/ConstructionPanel";
    [Export] public NodePath EmptyPanelPath { get; set; } = "Center/EmptyPanel";
    [Export] public NodePath HeroProfileButtonPath { get; set; } =
        "Center/EmptyPanel/Margin/Content/HeroProfileButton";

    private CityWorldController _controller = null!;
    private MacroCitizenActivity _activity = null!;
    private CityStatusPanel _statusPanel = null!;
    private Label _offlineLabel = null!;
    private ConstructionPanel _constructionPanel = null!;
    private PanelContainer _emptyPanel = null!;
    private Button _heroProfileButton = null!;

    public override void _Ready()
    {
        _controller = GetParent().GetNode<CityWorldController>("CityWorldController");
        _activity = GetNode<MacroCitizenActivity>(ActivityPath);
        _statusPanel = GetNode<CityStatusPanel>(StatusPanelPath);
        _offlineLabel = GetNode<Label>(OfflineReportLabelPath);
        _constructionPanel = GetNode<ConstructionPanel>(ConstructionPanelPath);
        _emptyPanel = GetNode<PanelContainer>(EmptyPanelPath);
        _heroProfileButton = GetNode<Button>(HeroProfileButtonPath);

        _controller.BuildingStateChanged += OnAnyBuildingStateChanged;
        _controller.ProjectStateChanged += OnAnyProjectStateChanged;
        _controller.WorldTickAdvanced += OnWorldTickAdvanced;
        _controller.SelectionChanged += OnSelectionChanged;
        _controller.HeroCreated += OnHeroCreated;
        _heroProfileButton.Pressed += OnHeroProfilePressed;

        Visible = !_controller.NeedsOnboarding();
        if (Visible) Refresh();
    }

    public override void _ExitTree()
    {
        if (_controller is not null)
        {
            _controller.BuildingStateChanged -= OnAnyBuildingStateChanged;
            _controller.ProjectStateChanged -= OnAnyProjectStateChanged;
            _controller.WorldTickAdvanced -= OnWorldTickAdvanced;
            _controller.SelectionChanged -= OnSelectionChanged;
            _controller.HeroCreated -= OnHeroCreated;
        }
        if (_heroProfileButton is not null)
        {
            _heroProfileButton.Pressed -= OnHeroProfilePressed;
        }
    }

    public void OnReturnedToCity()
    {
        if (_controller.NeedsOnboarding()) return;
        Show();
        Refresh();
    }

    private void Refresh()
    {
        _statusPanel.Refresh(_controller);
        _activity.Populate(_controller.Citizens().Count);
        _constructionPanel.Refresh();

        bool showConstruction = _controller.World.Projects.Count > 0
            || (_controller.World.Hero is not null
                && !_controller.World.Buildings.Values.Any(b => b.Kind == BuildingKind.Home));
        _emptyPanel.Visible = !showConstruction;
        _constructionPanel.Visible = showConstruction;

        if (_controller.LastOfflineReport is { HadProgression: true } report)
        {
            _offlineLabel.Text = FormatOfflineReport(report);
            _offlineLabel.Visible = true;
        }
        else
        {
            _offlineLabel.Visible = false;
        }
    }

    private void OnHeroCreated(int citizenId)
    {
        Show();
        Refresh();
    }

    private void OnHeroProfilePressed() => _controller.SelectHero();

    private void OnAnyBuildingStateChanged(int buildingId) => Refresh();

    private void OnAnyProjectStateChanged(int projectId) => Refresh();

    private void OnWorldTickAdvanced(int tick) => _statusPanel.Refresh(_controller);

    private void OnSelectionChanged(int selectionState)
    {
        if ((CityWorldController.Selection)selectionState == CityWorldController.Selection.MacroView
            && !_controller.NeedsOnboarding())
        {
            Show();
            Refresh();
        }
        else
        {
            Hide();
        }
    }

    private static string FormatOfflineReport(OfflineProgressionReport report)
    {
        string time = FormatSimulatedTime(report.SimulatedTime);
        return
            $"Welcome back · {time} simulated · " +
            $"{report.TicksApplied} authorized production ticks · " +
            $"+{report.StockAdded} total stock";
    }

    private static string FormatSimulatedTime(TimeSpan time)
    {
        if (time.TotalDays >= 1) return $"{(int)time.TotalDays}d {time.Hours}h";
        if (time.TotalHours >= 1) return $"{(int)time.TotalHours}h {time.Minutes}m";
        if (time.TotalMinutes >= 1) return $"{(int)time.TotalMinutes}m {time.Seconds}s";
        return $"{(int)time.TotalSeconds}s";
    }
}
