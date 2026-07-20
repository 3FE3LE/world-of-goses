#nullable enable
using System.Collections.Generic;
using System.Linq;
using Godot;
using WorldofGoses.Domain;

namespace WorldofGoses;

/// <summary>
/// Three-state panel that drives the first worksite from the
/// macro view: a Blueprint call to action, an Underway view of the
/// progress and contributors, and a Completed view that links to
/// the resulting building.
/// </summary>
public partial class ConstructionPanel : PanelContainer
{
    [Signal] public delegate void AuthorizeRequestedEventHandler(int constructionKind);
    [Signal] public delegate void PauseRequestedEventHandler();
    [Signal] public delegate void ResumeRequestedEventHandler();
    [Signal] public delegate void ViewHeroRequestedEventHandler();
    [Signal] public delegate void ViewCompletedBuildingRequestedEventHandler(int buildingId);
    [Signal] public delegate void AssignToProjectRequestedEventHandler(int projectId, int citizenId);
    [Signal] public delegate void UnassignFromProjectRequestedEventHandler(int projectId, int citizenId);

    [Export] public NodePath ControllerPath { get; set; } = "/root/CityPrototype/CityWorldController";

    private enum Mode { Blueprint, Underway, Completed }

    private CityWorldController _controller = null!;
    private Mode _mode = Mode.Blueprint;

    private PanelContainer _body = null!;
    private Label _title = null!;
    private Label _description = null!;
    private Label _phaseLabel = null!;
    private Label _statusLabel = null!;
    private ProgressBar _progress = null!;
    private Label _contributors = null!;
    private VBoxContainer _assignList = null!;
    private VBoxContainer _availableList = null!;
    private IconButton _authorizeButton = null!;
    private IconButton _farmButton = null!;
    private IconButton _quarryButton = null!;
    private IconButton _pauseButton = null!;
    private IconButton _resumeButton = null!;
    private IconButton _viewHeroButton = null!;
    private IconButton _viewBuildingButton = null!;
    private Label _errorLabel = null!;
    private Button _primaryFocus = null!;
    private LineageThemeSignals? _themeSignals;

    public override void _Ready()
    {
        var controllerNode = GetNodeOrNull<CityWorldController>(ControllerPath);
        if (controllerNode is null)
        {
            GD.PushError($"ConstructionPanel: cannot resolve controller at '{ControllerPath}'.");
            return;
        }
        _controller = controllerNode;
        AuthorizeRequested += kind => _controller.TryAuthorizeConstruction((ConstructionKind)kind);
        PauseRequested += () => OnPauseResume(true);
        ResumeRequested += () => OnPauseResume(false);
        ViewHeroRequested += () => _controller.SelectHero();
        ViewCompletedBuildingRequested += OnViewCompletedBuilding;
        AssignToProjectRequested += OnAssignToProject;
        UnassignFromProjectRequested += OnUnassignFromProject;

        BuildShell();
        if (_controller is not null)
        {
            _controller.HeroCreated += _ => ApplyLineageTheme();
            _controller.ProjectStateChanged += _ => Refresh();
            _controller.BuildingStateChanged += _ => Refresh();
            _controller.SelectionChanged += _ => Refresh();
            _controller.CitizenAssignmentRejected += _ => Refresh();
            _themeSignals = GetNodeOrNull<LineageThemeSignals>("/root/LineageThemeSignals");
            if (_themeSignals is not null)
            {
                _themeSignals.LineageChanged += OnLineageChanged;
            }
            ApplyLineageTheme();
            Refresh();
        }
    }

    private void ApplyLineageTheme()
    {
        AddThemeStyleboxOverride("panel", LineageThemeRegistry.GetStyleBox(LineageThemeRegistry.ComponentPanel));
    }

    private void OnLineageChanged(string lineage) => ApplyLineageTheme();

    private void OnPauseResume(bool pause)
    {
        var project = CurrentProject();
        if (project is null) return;
        _controller.SetProjectEnabled(project.Id, !pause);
    }

    private ConstructionProject? CurrentProject()
    {
        foreach (var project in _controller.World.Projects.Values)
        {
            return project;
        }
        return null;
    }

    private void OnViewCompletedBuilding(int buildingId)
    {
        _controller.SelectBuilding(new BuildingId(buildingId));
    }

    private void OnAssignToProject(int projectId, int citizenId)
    {
        var result = _controller.TryAssignCitizenToProject(new BuildingId(projectId), new CitizenId(citizenId));
        if (!result.IsSuccess) _errorLabel.Text = FormatAssignmentError(result.Outcome);
    }

    private void OnUnassignFromProject(int projectId, int citizenId)
    {
        var result = _controller.TryUnassignCitizenFromProject(new BuildingId(projectId), new CitizenId(citizenId));
        if (!result.IsSuccess) _errorLabel.Text = FormatAssignmentError(result.Outcome);
    }

    private void BuildShell()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        var margin = new MarginContainer();
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 24);
        margin.AddThemeConstantOverride("margin_right", 24);
        margin.AddThemeConstantOverride("margin_top", 20);
        margin.AddThemeConstantOverride("margin_bottom", 20);
        AddChild(margin);

        var shell = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        shell.AddThemeConstantOverride("separation", 10);
        margin.AddChild(shell);

        _title = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        _title.ThemeTypeVariation = "ScreenTitle";
        _title.AddThemeFontSizeOverride("font_size", 36);
        shell.AddChild(_title);

        _description = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        _description.ThemeTypeVariation = "BodyText";
        shell.AddChild(_description);

        _phaseLabel = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        _phaseLabel.ThemeTypeVariation = "SectionTitle";
        _phaseLabel.AddThemeFontSizeOverride("font_size", 22);
        shell.AddChild(_phaseLabel);

        _progress = new ProgressBar
        {
            MinValue = 0,
            MaxValue = 1,
            Value = 0,
            ShowPercentage = false,
            CustomMinimumSize = new Vector2(0, 24),
        };
        shell.AddChild(_progress);

        _statusLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        _statusLabel.ThemeTypeVariation = "BodyText";
        shell.AddChild(_statusLabel);

        _contributors = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        _contributors.ThemeTypeVariation = "BodySmall";
        shell.AddChild(_contributors);

        var lists = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        lists.AddThemeConstantOverride("separation", 16);
        shell.AddChild(lists);

        _assignList = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        _assignList.AddThemeConstantOverride("separation", 4);
        lists.AddChild(_assignList);

        _availableList = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        _availableList.AddThemeConstantOverride("separation", 4);
        lists.AddChild(_availableList);

        _errorLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        _errorLabel.ThemeTypeVariation = "BodySmall";
        _errorLabel.AddThemeColorOverride("font_color", new Color("ef8f7a"));
        shell.AddChild(_errorLabel);

        var footer = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.End,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        footer.AddThemeConstantOverride("separation", 8);
        shell.AddChild(footer);

        _authorizeButton = NewFooterButton(
            iconPath: IconPaths.Check,
            label: "Authorize Basic Shelter",
            variation: "ButtonPrimary");
        _farmButton = NewFooterButton(
            iconPath: IconPaths.Leaf,
            label: "Build Farm",
            variation: "ButtonPrimary");
        _quarryButton = NewFooterButton(
            iconPath: IconPaths.Building,
            label: "Build Quarry",
            variation: "ButtonPrimary");
        _pauseButton = NewFooterButton(
            iconPath: IconPaths.Pause,
            label: "Pause",
            variation: "ButtonText");
        _resumeButton = NewFooterButton(
            iconPath: IconPaths.Play,
            label: "Resume",
            variation: "ButtonText");
        _viewHeroButton = NewFooterButton(
            iconPath: IconPaths.User,
            label: "View hero",
            variation: "ButtonText");
        _viewBuildingButton = NewFooterButton(
            iconPath: IconPaths.House,
            label: "View shelter",
            variation: "ButtonPrimary");
        _authorizeButton.Pressed += () => EmitSignal(
            SignalName.AuthorizeRequested, (int)ConstructionKind.BasicShelter);
        _farmButton.Pressed += () => EmitSignal(
            SignalName.AuthorizeRequested, (int)ConstructionKind.Farm);
        _quarryButton.Pressed += () => EmitSignal(
            SignalName.AuthorizeRequested, (int)ConstructionKind.Quarry);
        _pauseButton.Pressed += () => EmitSignal(SignalName.PauseRequested);
        _resumeButton.Pressed += () => EmitSignal(SignalName.ResumeRequested);
        _viewHeroButton.Pressed += () => EmitSignal(SignalName.ViewHeroRequested);
        _viewBuildingButton.Pressed += () =>
        {
            var project = CurrentProject();
            if (project is not null)
            {
                EmitSignal(SignalName.ViewCompletedBuildingRequested, project.Id.Value);
            }
        };
        footer.AddChild(_viewHeroButton);
        footer.AddChild(_pauseButton);
        footer.AddChild(_resumeButton);
        footer.AddChild(_authorizeButton);
        footer.AddChild(_farmButton);
        footer.AddChild(_quarryButton);
        footer.AddChild(_viewBuildingButton);

        _primaryFocus = _authorizeButton;
    }

    private static IconButton NewFooterButton(string iconPath, string label, string variation) => new()
    {
        IconPath = iconPath,
        Label = label,
        ThemeTypeVariation = variation,
        CustomMinimumSize = new Vector2(180, 44),
        FocusMode = FocusModeEnum.All,
    };

    public override void _ExitTree()
    {
        if (_controller is null) return;
        if (_themeSignals is not null) _themeSignals.LineageChanged -= OnLineageChanged;
        _controller.HeroCreated -= _ => Refresh();
        _controller.ProjectStateChanged -= _ => Refresh();
        _controller.BuildingStateChanged -= _ => Refresh();
        _controller.SelectionChanged -= _ => Refresh();
        _controller.CitizenAssignmentRejected -= _ => Refresh();
    }

    public void Refresh()
    {
        if (_controller is null) return;
        _errorLabel.Text = string.Empty;
        var world = _controller.World;
        if (world.Projects.Count == 0)
        {
            _mode = Mode.Blueprint;
        }
        else
        {
            _mode = Mode.Underway;
        }
        Render();
    }

    private void Render()
    {
        var world = _controller.World;
        switch (_mode)
        {
            case Mode.Blueprint:
                RenderBlueprint(world);
                break;
            case Mode.Underway:
                RenderUnderway(world);
                break;
            case Mode.Completed:
                RenderCompleted(world);
                break;
        }
    }

    private void RenderBlueprint(CityWorld world)
    {
        bool hasHome = world.Buildings.Values.Any(building => building.Kind == BuildingKind.Home);
        _title.Text = hasHome ? "Choose the next construction" : "Build the first shelter";
        _description.Text = hasHome
            ? "Choose a productive building. Its worksite will appear automatically in the city; open Construction progress to assign contributors."
            : "Authorise the Basic Shelter — a modest dwelling that unlocks productive construction.";
        _phaseLabel.Visible = false;
        _progress.Visible = false;
        _statusLabel.Visible = false;
        _contributors.Visible = false;
        _assignList.Visible = false;
        _availableList.Visible = false;
        _errorLabel.Visible = false;
        bool canAuthorise = world.Hero is not null && world.Projects.Count == 0;
        _authorizeButton.Visible = !hasHome;
        _authorizeButton.Disabled = !canAuthorise;
        _farmButton.Visible = hasHome;
        _farmButton.Disabled = !canAuthorise;
        _quarryButton.Visible = hasHome;
        _quarryButton.Disabled = !canAuthorise;
        _pauseButton.Visible = false;
        _resumeButton.Visible = false;
        _viewBuildingButton.Visible = false;
        _viewHeroButton.Visible = world.Hero is not null;
        _primaryFocus = hasHome ? _farmButton : _authorizeButton;
        _primaryFocus.GrabFocus();
    }

    private void RenderUnderway(CityWorld world)
    {
        var project = CurrentProject();
        if (project is null)
        {
            RenderBlueprint(world);
            return;
        }
        _title.Text = project.DisplayName;
        _description.Text = project.AssignedCount == 0
            ? "Assign at least one available citizen below. Construction cannot advance without contributors."
            : $"Contributors add work every {ConstructionRules.WorkIntervalTicks} seconds while the project is active.";
        _phaseLabel.Visible = true;
        _progress.Visible = true;
        _statusLabel.Visible = true;
        _contributors.Visible = true;
        _assignList.Visible = true;
        _availableList.Visible = true;
        _errorLabel.Visible = !string.IsNullOrEmpty(_errorLabel.Text);
        var phase = ConstructionRules.PhaseFor(project.Progress, project.RequiredWork);
        _phaseLabel.Text = ConstructionRules.Describe(phase);
        _progress.MinValue = 0;
        _progress.MaxValue = project.RequiredWork;
        _progress.Value = project.Progress;
        _statusLabel.Text = DescribeProjectStatus(project);
        _contributors.Text = $"Contributors: {project.AssignedCount}/{project.WorkerCapacity}";

        ClearList(_assignList);
        ClearList(_availableList);
        PopulateAssigned(project);
        PopulateAvailable(project);

        _authorizeButton.Visible = false;
        _farmButton.Visible = false;
        _quarryButton.Visible = false;
        _viewBuildingButton.Visible = false;
        _pauseButton.Visible = project.Enabled;
        _resumeButton.Visible = !project.Enabled;
        _viewHeroButton.Visible = true;
        _primaryFocus = project.Enabled ? _pauseButton : _resumeButton;
        _primaryFocus.GrabFocus();
    }

    private void RenderCompleted(CityWorld world)
    {
        Building? shelter = null;
        foreach (var building in world.Buildings.Values)
        {
            if (building.Kind == BuildingKind.Home)
            {
                shelter = building;
                break;
            }
        }
        _title.Text = "Basic Shelter completed";
        if (shelter is not null)
        {
            _description.Text = "The Basic Shelter is ready. Open the building to assign it as resting site.";
        }
        else
        {
            _description.Text = "A Basic Shelter is ready.";
        }
        _phaseLabel.Visible = false;
        _progress.Visible = false;
        _statusLabel.Visible = false;
        _contributors.Visible = false;
        _assignList.Visible = false;
        _availableList.Visible = false;
        _errorLabel.Visible = false;
        _authorizeButton.Visible = false;
        _farmButton.Visible = false;
        _quarryButton.Visible = false;
        _pauseButton.Visible = false;
        _resumeButton.Visible = false;
        _viewHeroButton.Visible = true;
        _viewBuildingButton.Visible = shelter is not null;
        _primaryFocus = shelter is not null ? _viewBuildingButton : _viewHeroButton;
        _primaryFocus.GrabFocus();
    }

    private void PopulateAssigned(ConstructionProject project)
    {
        AddSectionHeader(_assignList, "Assigned");
        if (project.AssignedCount == 0)
        {
            AddListLabel(_assignList, "No contributors yet.");
            return;
        }
        foreach (var cid in project.AssignedCitizenIds)
        {
            var citizen = _controller.World.GetCitizen(cid);
            if (citizen is null) continue;
            var row = new HBoxContainer();
            var name = new Label
            {
                Text = citizen.Name,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                ThemeTypeVariation = "BodyText",
            };
            var button = new Button
            {
                Text = "Remove",
                ThemeTypeVariation = "ButtonText",
                FocusMode = FocusModeEnum.All,
            };
            var captured = cid;
            button.Pressed += () => EmitSignal(SignalName.UnassignFromProjectRequested, project.Id.Value, captured.Value);
            row.AddChild(name);
            row.AddChild(button);
            _assignList.AddChild(row);
        }
    }

    private void PopulateAvailable(ConstructionProject project)
    {
        AddSectionHeader(_availableList, "Available");
        bool atCapacity = project.AssignedCount >= project.WorkerCapacity;
        foreach (var citizen in _controller.World.Citizens.Values)
        {
            if (citizen.CurrentAssignment.HasValue && citizen.CurrentAssignment != project.Id) continue;
            if (project.IsAssigned(citizen.Id)) continue;
            var row = new HBoxContainer();
            var name = new Label
            {
                Text = citizen.Name,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                ThemeTypeVariation = "BodyText",
            };
            var button = new Button
            {
                Text = "Assign",
                ThemeTypeVariation = "ButtonText",
                Disabled = atCapacity,
                FocusMode = FocusModeEnum.All,
            };
            var captured = citizen.Id;
            button.Pressed += () => EmitSignal(SignalName.AssignToProjectRequested, project.Id.Value, captured.Value);
            row.AddChild(name);
            row.AddChild(button);
            _availableList.AddChild(row);
        }
        if (_availableList.GetChildCount() == 1)
        {
            AddListLabel(_availableList, atCapacity ? "Project at capacity." : "No free citizens.");
        }
    }

    private static void AddSectionHeader(VBoxContainer list, string title)
    {
        var label = new Label { Text = title };
        label.ThemeTypeVariation = "SectionTitle";
        label.AddThemeFontSizeOverride("font_size", 22);
        list.AddChild(label);
    }

    private static void AddListLabel(VBoxContainer list, string text)
    {
        var label = new Label
        {
            Text = text,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            ThemeTypeVariation = "BodySmall",
        };
        list.AddChild(label);
    }

    private static void ClearList(VBoxContainer list)
    {
        foreach (var child in list.GetChildren())
        {
            list.RemoveChild(child);
            child.QueueFree();
        }
    }

    private static string DescribeProjectStatus(ConstructionProject project) => project.StopCause switch
    {
        ConstructionStopCause.Authorized =>
            $"Active — next contribution on a {ConstructionRules.WorkIntervalTicks}-tick interval",
        ConstructionStopCause.Paused => "Paused by the player",
        ConstructionStopCause.NoWorkers => "Waiting for contributors",
        ConstructionStopCause.WorkersExhausted => "Waiting: contributors exhausted",
        ConstructionStopCause.Night => "Resting during the night",
        ConstructionStopCause.Completed => "Completed",
        ConstructionStopCause.NoHero => "No hero available",
        _ => project.StopCause.ToString(),
    };

    private static string FormatAssignmentError(AssignmentOutcome outcome) => outcome switch
    {
        AssignmentOutcome.AtCapacity => "Project is at worker capacity.",
        AssignmentOutcome.AlreadyAssigned => "Citizen is already a contributor.",
        AssignmentOutcome.CitizenUnavailable => "Citizen is assigned elsewhere.",
        AssignmentOutcome.NotAssigned => "Citizen is not assigned here.",
        AssignmentOutcome.CitizenNotFound => "Citizen no longer exists.",
        AssignmentOutcome.BuildingNotFound => "Worksite no longer exists.",
        _ => "Assignment rejected.",
    };
}
