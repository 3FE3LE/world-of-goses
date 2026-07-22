#nullable enable
using System.Collections.Generic;
using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Ui;

namespace WorldofGoses;

/// <summary>
/// Three-state panel that drives the first worksite from the
/// macro view: a Blueprint call to action, an Underway view of the
/// progress and contributors, and a Completed view that links to
/// the resulting building.
/// </summary>
public partial class ConstructionPanel : PanelContainer
{
    private static readonly Vector2 PreferredMinimumSize = new(720, 420);
    private const float ViewportMargin = 48f;

    private static readonly PackedScene AssignmentRowScene =
        GD.Load<PackedScene>("res://scenes/Components/AssignmentRow.tscn");

    [Signal] public delegate void AuthorizeRequestedEventHandler(int constructionKind);
    [Signal] public delegate void PauseRequestedEventHandler();
    [Signal] public delegate void ResumeRequestedEventHandler();
    [Signal] public delegate void ViewHeroRequestedEventHandler();
    [Signal] public delegate void ViewCompletedBuildingRequestedEventHandler(int buildingId);
    [Signal] public delegate void AssignToProjectRequestedEventHandler(int projectId, int citizenId);
    [Signal] public delegate void UnassignFromProjectRequestedEventHandler(int projectId, int citizenId);
    [Signal] public delegate void CancelProjectRequestedEventHandler(int projectId);
    /// <summary>
    /// Emitted when the player asks the modal to close — either via the
    /// header X or because the player triggered a route that closes the
    /// modal (authorisation, completed project). The host watches this
    /// to clear its content.
    /// </summary>
    [Signal] public delegate void CloseRequestedEventHandler();

    [Export] public NodePath ControllerPath { get; set; } = "/root/CityPrototype/CityWorldController";

    private enum Mode { Blueprint, Underway, Completed }

    private CityWorldController _controller = null!;
    private Mode _mode = Mode.Blueprint;
    private bool _wasAuthorizeEnabled;
    private bool _wasFarmEnabled;
    private bool _wasQuarryEnabled;
    private Tween? _pulseTween;

    private PanelContainer _body = null!;
    private Label _title = null!;
    private PanelHeader _header = null!;
    private TextureRect _constructionPreview = null!;
    private Label _description = null!;
    private Label _phaseLabel = null!;
    private Label _statusLabel = null!;
    private ProgressBar _progress = null!;
    private Label _contributors = null!;
    private Label _requirementsLabel = null!;
    private VBoxContainer _assignList = null!;
    private VBoxContainer _availableList = null!;
    private IconButton _authorizeButton = null!;
    private IconButton _farmButton = null!;
    private IconButton _quarryButton = null!;
    private IconButton _pauseButton = null!;
    private IconButton _resumeButton = null!;
    private IconButton _cancelButton = null!;
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
        AuthorizeRequested += OnAuthorizeRequested;
        PauseRequested += OnPauseRequested;
        ResumeRequested += OnResumeRequested;
        CancelProjectRequested += OnCancelProjectRequested;
        ViewHeroRequested += OnViewHeroRequested;
        ViewCompletedBuildingRequested += OnViewCompletedBuilding;
        AssignToProjectRequested += OnAssignToProject;
        UnassignFromProjectRequested += OnUnassignFromProject;

        BuildShell();
        GetViewport().SizeChanged += ApplyResponsiveMinimumSize;
        ApplyResponsiveMinimumSize();
        if (_controller is not null)
        {
            _controller.HeroCreated += OnHeroCreated;
            _controller.ProjectStateChanged += OnProjectStateChanged;
            _controller.BuildingStateChanged += OnBuildingStateChanged;
            _controller.SelectionChanged += OnSelectionChanged;
            _controller.CitizenAssignmentRejected += OnCitizenAssignmentRejected;
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

    private void OnAuthorizeRequested(int constructionKind)
    {
        // Material payment raises building-change events before the domain
        // publishes the new project. Establish the macro route first so a
        // Forest debit cannot briefly reassert a stale detail selection.
        _controller.ReturnToCity();
        var result = _controller.TryAuthorizeConstruction((ConstructionKind)constructionKind);
        if (result.IsSuccess)
        {
            Refresh();
            return;
        }
        _errorLabel.Text = FormatAuthorizationError(result.Outcome);
        _errorLabel.Visible = true;
    }

    private void OnPauseRequested() => OnPauseResume(true);

    private void OnResumeRequested() => OnPauseResume(false);

    private void OnViewHeroRequested() => _controller.SelectHero();

    private void OnHeroCreated(int citizenId) => ApplyLineageTheme();

    private void OnProjectStateChanged(int projectId) => Refresh();

    private void OnBuildingStateChanged(int buildingId) => Refresh();

    private void OnSelectionChanged(int selectionState) => Refresh();

    private void OnCitizenAssignmentRejected(int reason) => Refresh();

    private void OnPauseResume(bool pause)
    {
        var project = CurrentProject();
        if (project is null) return;
        _controller.SetProjectEnabled(project.Id, !pause);
    }

    private void OnCancelButtonPressed()
    {
        var project = CurrentProject();
        if (project is null) return;
        EmitSignal(SignalName.CancelProjectRequested, project.Id.Value);
    }

    private void OnCancelProjectRequested(int projectId)
    {
        var project = _controller.GetProject(new BuildingId(projectId));
        if (project is null) return;
        if (_controller.CancelProject(new BuildingId(projectId)))
        {
            Notifier.Show($"Cancelled {project.DisplayName}.");
        }
        else
        {
            Notifier.ShowError("Could not cancel the project.");
        }
    }

    private ConstructionSnapshot.ProjectItem? CurrentProject() =>
        _controller.GetConstructionSnapshot().Project;

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

        _header = new PanelHeader { Title = string.Empty };
        _header.CloseRequested += () => EmitSignal(SignalName.CloseRequested);
        shell.AddChild(_header);

        _constructionPreview = new TextureRect
        {
            StretchMode = TextureRect.StretchModeEnum.Keep,
            CustomMinimumSize = new Vector2(0, 80),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Visible = false,
        };
        shell.AddChild(_constructionPreview);

        _title = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Visible = false,
        };
        _title.ThemeTypeVariation = "ScreenTitle";
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

        _requirementsLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            ThemeTypeVariation = "BodyText",
        };
        shell.AddChild(_requirementsLabel);

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
        _errorLabel.ThemeTypeVariation = "ErrorText";
        shell.AddChild(_errorLabel);

        var footer = new HFlowContainer
        {
            Alignment = FlowContainer.AlignmentMode.End,
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
        _cancelButton = NewFooterButton(
            iconPath: IconPaths.Close,
            label: "Cancel project",
            variation: "ButtonText");
        _cancelButton.TooltipText = "Cancel the project. The deposit is lost and the site is cleared.";
        _viewHeroButton = StandardButtons.ViewHeroButton();
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
        _cancelButton.Pressed += OnCancelButtonPressed;
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
        footer.AddChild(_cancelButton);
        footer.AddChild(_authorizeButton);
        footer.AddChild(_farmButton);
        footer.AddChild(_quarryButton);
        footer.AddChild(_viewBuildingButton);

        _primaryFocus = _authorizeButton;
    }

    private static IconButton NewFooterButton(string iconPath, string label, string variation) =>
        StandardButtons.IconAction(iconPath, label, variation);

    public override void _ExitTree()
    {
        GetViewport().SizeChanged -= ApplyResponsiveMinimumSize;
        if (_controller is null) return;
        if (_themeSignals is not null) _themeSignals.LineageChanged -= OnLineageChanged;
        _controller.HeroCreated -= OnHeroCreated;
        _controller.ProjectStateChanged -= OnProjectStateChanged;
        _controller.BuildingStateChanged -= OnBuildingStateChanged;
        _controller.SelectionChanged -= OnSelectionChanged;
        _controller.CitizenAssignmentRejected -= OnCitizenAssignmentRejected;
        AuthorizeRequested -= OnAuthorizeRequested;
        PauseRequested -= OnPauseRequested;
        ResumeRequested -= OnResumeRequested;
        CancelProjectRequested -= OnCancelProjectRequested;
        ViewHeroRequested -= OnViewHeroRequested;
        ViewCompletedBuildingRequested -= OnViewCompletedBuilding;
        AssignToProjectRequested -= OnAssignToProject;
        UnassignFromProjectRequested -= OnUnassignFromProject;
    }

    private void ApplyResponsiveMinimumSize()
    {
        Vector2 availableSize = GetViewportRect().Size - new Vector2(ViewportMargin, ViewportMargin);
        CustomMinimumSize = new Vector2(
            Mathf.Max(0f, Mathf.Min(PreferredMinimumSize.X, availableSize.X)),
            Mathf.Max(0f, Mathf.Min(PreferredMinimumSize.Y, availableSize.Y)));
    }

    public void Refresh(bool clearError = true)
    {
        if (_controller is null) return;
        if (clearError) _errorLabel.Text = string.Empty;
        var snapshot = _controller.GetConstructionSnapshot();
        if (snapshot.Project is null)
        {
            _mode = Mode.Blueprint;
        }
        else
        {
            _mode = Mode.Underway;
        }
        Render(snapshot);
    }

    private void Render(ConstructionSnapshot snapshot)
    {
        switch (_mode)
        {
            case Mode.Blueprint:
                RenderBlueprint(snapshot);
                break;
            case Mode.Underway:
                RenderUnderway(snapshot);
                break;
            case Mode.Completed:
                RenderCompleted(snapshot);
                break;
        }
    }

    private void RenderBlueprint(ConstructionSnapshot snapshot)
    {
        bool hasHome = snapshot.HasHome;
        _header.SetTitle(hasHome ? "Choose the next construction" : "Build the first shelter");
        _title.Text = hasHome ? "Choose the next construction" : "Build the first shelter";
        // Preview the shelter art so the player knows what they are about to build.
        var shelterArt = BuildingArt.GetTexturePath(ConstructionKind.BasicShelter);
        if (shelterArt is { } path)
        {
            _constructionPreview.Texture = ResourceLoader.Load<Texture2D>(path);
            _constructionPreview.Visible = !hasHome;
        }
        else
        {
            _constructionPreview.Visible = false;
        }
        _description.Text = hasHome
            ? "Choose a productive building. Its worksite will appear automatically in the city; open Construction progress to assign contributors."
            : "Authorise the Basic Shelter — a modest dwelling that unlocks productive construction.";
        _phaseLabel.Visible = false;
        _progress.Visible = false;
        _statusLabel.Visible = false;
        _contributors.Visible = false;
        _requirementsLabel.Visible = true;
        _assignList.Visible = false;
        _availableList.Visible = false;
        _errorLabel.Visible = !string.IsNullOrEmpty(_errorLabel.Text);
        bool canAuthorise = snapshot.HasHero && snapshot.Project is null;
        var shelter = snapshot.OptionFor(ConstructionKind.BasicShelter);
        var farm = snapshot.OptionFor(ConstructionKind.Farm);
        var quarry = snapshot.OptionFor(ConstructionKind.Quarry);
        _requirementsLabel.Text = hasHome
            ? $"Farm — {DescribeMaterials(farm)}\nQuarry — {DescribeMaterials(quarry)}"
            : $"Basic Shelter — {DescribeMaterials(shelter)}";
        _authorizeButton.Visible = !hasHome;
        bool authorizeEnabled = canAuthorise && shelter.CanPayDeposit;
        _authorizeButton.Disabled = !authorizeEnabled;
        _authorizeButton.TooltipText = authorizeEnabled
            ? "Authorise the Basic Shelter."
            : "Needs 1 wood — gather from a Forest first.";
        // Farm and Quarry are now always visible so the player can see
        // the upcoming options. They are disabled until the Basic
        // Shelter exists; the tooltip explains the dependency.
        _farmButton.Visible = true;
        bool farmEnabled = canAuthorise && hasHome && farm.CanPayDeposit;
        _farmButton.Disabled = !farmEnabled;
        _farmButton.TooltipText = !hasHome
            ? "Build the Basic Shelter first to unlock the Farm."
            : farmEnabled
                ? "Build a Farm."
                : "Not enough materials to authorise a Farm.";
        _quarryButton.Visible = true;
        bool quarryEnabled = canAuthorise && hasHome && quarry.CanPayDeposit;
        _quarryButton.Disabled = !quarryEnabled;
        _quarryButton.TooltipText = !hasHome
            ? "Build the Basic Shelter first to unlock the Quarry."
            : quarryEnabled
                ? "Build a Quarry."
                : "Not enough materials to authorise a Quarry.";
        DetectEnableTransition(authorizeEnabled, ref _wasAuthorizeEnabled, _authorizeButton);
        DetectEnableTransition(farmEnabled, ref _wasFarmEnabled, _farmButton);
        DetectEnableTransition(quarryEnabled, ref _wasQuarryEnabled, _quarryButton);
        _pauseButton.Visible = false;
        _resumeButton.Visible = false;
        _cancelButton.Visible = false;
        _viewBuildingButton.Visible = false;
        _viewHeroButton.Visible = snapshot.HasHero;
        _primaryFocus = !hasHome
            ? _authorizeButton
            : !_farmButton.Disabled
                ? _farmButton
                : !_quarryButton.Disabled
                    ? _quarryButton
                    : _viewHeroButton;
        _primaryFocus.GrabFocus();
    }

    /// <summary>
    /// Pulses the button when it transitions from disabled to enabled,
    /// so the player notices that a previously blocked action is now
    /// available. Subsequent refreshes do not re-pulse.
    /// </summary>
    private void DetectEnableTransition(bool nowEnabled, ref bool wasEnabled, IconButton button)
    {
        if (nowEnabled && !wasEnabled)
        {
            PulseButton(button);
        }
        wasEnabled = nowEnabled;
    }

    private void PulseButton(IconButton button)
    {
        _pulseTween?.Kill();
        button.Modulate = new Color(1f, 1f, 1f, 1f);
        _pulseTween = CreateTween();
        _pulseTween.TweenProperty(button, "modulate", new Color(0.8f, 1f, 0.8f, 1f), 0.15f);
        _pulseTween.TweenProperty(button, "modulate", new Color(1f, 1f, 1f, 1f), 0.45f);
    }

    private void RenderUnderway(ConstructionSnapshot snapshot)
    {
        var project = snapshot.Project;
        if (project is null)
        {
            RenderBlueprint(snapshot);
            return;
        }
        _header.SetTitle(project.DisplayName);
        _title.Text = project.DisplayName;
        _description.Text = project.AssignedCount == 0
            ? "Assign at least one available citizen below. Construction cannot advance without contributors."
            : $"Contributors add work every {ConstructionRules.WorkIntervalTicks} seconds while the project is active.";
        _phaseLabel.Visible = true;
        _progress.Visible = true;
        _statusLabel.Visible = true;
        _contributors.Visible = true;
        var projectArt = BuildingArt.GetTexturePath(project.ResultingKind);
        if (projectArt is { } path)
        {
            _constructionPreview.Texture = ResourceLoader.Load<Texture2D>(path);
            _constructionPreview.Visible = true;
        }
        else
        {
            _constructionPreview.Visible = false;
        }
        _requirementsLabel.Visible = project.RemainingInputs.Count > 0;
        _requirementsLabel.Text = project.RemainingInputs.Count > 0
            ? $"Still needed — {DescribeInputs(project.RemainingInputs)}"
            : string.Empty;
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
        PopulateAvailable(project, snapshot.AvailableCitizens);

        _authorizeButton.Visible = false;
        _farmButton.Visible = false;
        _quarryButton.Visible = false;
        _viewBuildingButton.Visible = false;
        _pauseButton.Visible = project.Enabled;
        _resumeButton.Visible = !project.Enabled;
        _cancelButton.Visible = true;
        _viewHeroButton.Visible = true;
        _primaryFocus = project.Enabled ? _pauseButton : _resumeButton;
        _primaryFocus.GrabFocus();
    }

    private void RenderCompleted(ConstructionSnapshot snapshot)
    {
        BuildingId? shelterId = snapshot.HomeBuildingId;
        _header.SetTitle("Basic Shelter completed");
        _title.Text = "Basic Shelter completed";
        if (shelterId.HasValue)
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
        _requirementsLabel.Visible = false;
        _assignList.Visible = false;
        _availableList.Visible = false;
        _errorLabel.Visible = false;
        _authorizeButton.Visible = false;
        _farmButton.Visible = false;
        _quarryButton.Visible = false;
        _pauseButton.Visible = false;
        _resumeButton.Visible = false;
        _cancelButton.Visible = false;
        _viewHeroButton.Visible = true;
        _viewBuildingButton.Visible = shelterId.HasValue;
        _primaryFocus = shelterId.HasValue ? _viewBuildingButton : _viewHeroButton;
        _primaryFocus.GrabFocus();
    }

    private void PopulateAssigned(ConstructionSnapshot.ProjectItem project)
    {
        AddSectionHeader(_assignList, "Assigned");
        if (project.AssignedCount == 0)
        {
            AddListLabel(_assignList, "No contributors yet.");
            return;
        }
        foreach (var citizen in project.AssignedCitizens)
        {
            var row = InstantiateAssignmentRow(
                citizen.Id.Value,
                citizen.Name,
                "Remove",
                $"Remove {citizen.Name} from the project");
            row.ActionRequested += id =>
                EmitSignal(SignalName.UnassignFromProjectRequested, project.Id.Value, id);
            _assignList.AddChild(row);
        }
    }

    private void PopulateAvailable(
        ConstructionSnapshot.ProjectItem project,
        IReadOnlyList<ConstructionSnapshot.CitizenItem> availableCitizens)
    {
        AddSectionHeader(_availableList, "Available");
        bool atCapacity = project.AssignedCount >= project.WorkerCapacity;
        foreach (var citizen in availableCitizens)
        {
            var row = InstantiateAssignmentRow(
                citizen.Id.Value,
                citizen.Name,
                "Assign",
                $"Assign {citizen.Name} to the project",
                disabled: atCapacity);
            row.ActionRequested += id =>
                EmitSignal(SignalName.AssignToProjectRequested, project.Id.Value, id);
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
        list.AddChild(label);
    }

    private static AssignmentRow InstantiateAssignmentRow(
        int id,
        string name,
        string actionLabel,
        string tooltip,
        bool disabled = false)
    {
        var row = AssignmentRowScene.Instantiate<AssignmentRow>();
        row.Configure(id, name, actionLabel, tooltip, disabled);
        return row;
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

    private static string DescribeProjectStatus(ConstructionSnapshot.ProjectItem project) => project.StopCause switch
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

    private static string DescribeMaterials(ConstructionSnapshot.OptionItem option)
    {
        if (option.Materials.Count == 0) return "no material cost";
        var parts = new List<string>();
        foreach (var material in option.Materials)
        {
            string resource = material.Resource.ToString().ToLowerInvariant();
            parts.Add($"{material.Required} {resource} ({material.Available} available)");
        }
        return string.Join(" + ", parts);
    }

    private static string DescribeInputs(IReadOnlyList<RecipeInput> inputs)
    {
        var parts = new List<string>();
        foreach (var input in inputs)
        {
            parts.Add($"{input.Amount} {input.Resource.ToString().ToLowerInvariant()}");
        }
        return string.Join(" + ", parts);
    }

    private static string FormatAuthorizationError(ConstructionAuthorizationOutcome outcome) => outcome switch
    {
        ConstructionAuthorizationOutcome.MissingMaterials =>
            "Missing materials. Check the requirements above.",
        ConstructionAuthorizationOutcome.HomeRequired => "Build the Basic Shelter first.",
        ConstructionAuthorizationOutcome.AlreadyAuthorized => "Finish or cancel the current project first.",
        ConstructionAuthorizationOutcome.NoHero => "Create the founding hero first.",
        ConstructionAuthorizationOutcome.HomeAlreadyBuilt => "The Basic Shelter already exists.",
        ConstructionAuthorizationOutcome.WorldNotEmpty =>
            "The founding shelter can only start in the initial world.",
        _ => "Construction could not be authorized.",
    };
}
