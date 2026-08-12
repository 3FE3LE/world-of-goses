#nullable enable
using System.Collections.Generic;
using System.Linq;
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
    [Signal] public delegate void PlacementRequestedEventHandler(int constructionKind);
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
    private bool _wasCultivationEnabled;
    private bool _wasQuarryEnabled;
    private bool _wasTownHallEnabled;
    private Tween? _pulseTween;

    private ScrollContainer _bodyScroll = null!;
    private VBoxContainer _bodyContent = null!;
    private Label _title = null!;
    private PanelHeader _header = null!;
    private TextureRect _constructionPreview = null!;
    private Label _description = null!;
    private Label _phaseLabel = null!;
    private Label _statusLabel = null!;
    private ProgressBar _progress = null!;
    private Label _contributors = null!;
    private Label _requirementsLabel = null!;
    private ResourceInventoryPanel _foundingResourcesPanel = null!;
    private VBoxContainer _assignList = null!;
    private VBoxContainer _availableList = null!;
    private VBoxContainer _unavailableList = null!;
    private IconButton _authorizeButton = null!;
    private IconButton _farmButton = null!;
    private IconButton _cultivationButton = null!;
    private IconButton _quarryButton = null!;
    private IconButton _townHallButton = null!;
    private IconButton _bedrollButton = null!;
    private IconButton _cacheButton = null!;
    private IconButton _canopyButton = null!;
    private IconButton _clearCargoButton = null!;
    private IconButton _pauseButton = null!;
    private IconButton _resumeButton = null!;
    private IconButton _cancelButton = null!;
    private IconButton _viewHeroButton = null!;
    private IconButton _viewBuildingButton = null!;
    private Label _errorLabel = null!;
    private Button _primaryFocus = null!;

    internal IconButton ViewHeroButtonForVisualRegression => _viewHeroButton;

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!Visible || _bodyScroll is null) return;
        if (!GetGlobalRect().HasPoint(GetViewport().GetMousePosition())) return;

        int direction = @event switch
        {
            InputEventMouseButton mouse
                when mouse.Pressed && mouse.ButtonIndex == MouseButton.WheelDown => 1,
            InputEventMouseButton mouse
                when mouse.Pressed && mouse.ButtonIndex == MouseButton.WheelUp => -1,
            _ => 0,
        };
        if (direction == 0) return;
        _bodyScroll.ScrollVertical += direction * 56;
        GetViewport().SetInputAsHandled();
    }

    public override void _Ready()
    {
        OverlayLayers.Apply(this, OverlayLayers.Modal);

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
            Refresh();
        }
    }

    private void OnAuthorizeRequested(int constructionKind)
    {
        _controller.ReturnToCity();
        EmitSignal(SignalName.PlacementRequested, constructionKind);
    }

    private void OnPauseRequested() => OnPauseResume(true);

    private void OnResumeRequested() => OnPauseResume(false);

    private void OnHeroCreated(int citizenId) => Refresh();

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
        var buildingId = new BuildingId(projectId);
        if (!_controller.TryGetProjectDisplayName(buildingId, out string? displayName)) return;
        if (_controller.CancelProject(buildingId))
        {
            Notifier.Show(UiText.Format("ui.construction.cancelled", UiText.Get(displayName ?? "")));
        }
        else
        {
            Notifier.ShowError(UiText.Get("Could not cancel the project."));
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
        if (!result.IsSuccess) _errorLabel.Text = AssignmentErrorText.Format(result.Outcome);
    }

    private void OnUnassignFromProject(int projectId, int citizenId)
    {
        var result = _controller.TryUnassignCitizenFromProject(new BuildingId(projectId), new CitizenId(citizenId));
        if (!result.IsSuccess) _errorLabel.Text = AssignmentErrorText.Format(result.Outcome);
    }

    private void BuildShell()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        var margin = new MarginContainer();
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", Tokens.SpacingBlock);
        margin.AddThemeConstantOverride("margin_right", Tokens.SpacingBlock);
        margin.AddThemeConstantOverride("margin_top", Tokens.SpacingSection);
        margin.AddThemeConstantOverride("margin_bottom", Tokens.SpacingSection);
        AddChild(margin);

        var shell = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        shell.AddThemeConstantOverride("separation", Tokens.SpacingRelaxed);
        margin.AddChild(shell);

        _header = new PanelHeader { Title = string.Empty };
        _header.CloseRequested += () => EmitSignal(SignalName.CloseRequested);
        shell.AddChild(_header);

        // Body is wrapped in a ScrollContainer so long descriptions, big
        // assignment lists, or text 50 %+ longer than designed do not push
        // the footer out of the viewport. Header and footer stay fixed.
        _bodyScroll = new ScrollContainer
        {
            Name = "BodyScroll",
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 160),
        };
        shell.AddChild(_bodyScroll);

        _bodyContent = new VBoxContainer
        {
            Name = "BodyContent",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkBegin,
            MouseFilter = MouseFilterEnum.Pass,
        };
        _bodyContent.AddThemeConstantOverride("separation", Tokens.SpacingRelaxed);
        _bodyScroll.AddChild(_bodyContent);

        _constructionPreview = new TextureRect
        {
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            CustomMinimumSize = new Vector2(0, 80),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Visible = false,
        };
        _bodyContent.AddChild(_constructionPreview);

        _title = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Visible = false,
        };
        _title.ThemeTypeVariation = "HudHeader";
        _bodyContent.AddChild(_title);

        _description = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        _description.ThemeTypeVariation = "HudBody";
        _bodyContent.AddChild(_description);

        _phaseLabel = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        _phaseLabel.ThemeTypeVariation = "HudLabel";
        _bodyContent.AddChild(_phaseLabel);

        _progress = new ProgressBar
        {
            MinValue = 0,
            MaxValue = 1,
            Value = 0,
            ShowPercentage = false,
            CustomMinimumSize = new Vector2(0, 24),
        };
        _progress.ThemeTypeVariation = "HudProgress";
        _bodyContent.AddChild(_progress);

        _statusLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        _statusLabel.ThemeTypeVariation = "HudBody";
        _bodyContent.AddChild(_statusLabel);

        _contributors = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        _contributors.ThemeTypeVariation = "HudCaption";
        _bodyContent.AddChild(_contributors);

        _requirementsLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            ThemeTypeVariation = "HudBody",
        };
        _bodyContent.AddChild(_requirementsLabel);

        // Before the Shelter exists, resources are the founder's six-unit
        // load and then the Founding Site Cache. Collapsed by default: expanded,
        // this list filled the short scroll body and pushed the phase, the status
        // and the module costs out of view — exactly the information the player
        // needs while the site waits for the next module. The collapsed header
        // still reports how many resource types are carried, and one click opens
        // the detail.
        _foundingResourcesPanel = new ResourceInventoryPanel(expandedByDefault: false);
        _bodyContent.AddChild(_foundingResourcesPanel);

        var lists = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        lists.AddThemeConstantOverride("separation", Tokens.SpacingWide);
        _bodyContent.AddChild(lists);

        _assignList = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        _assignList.AddThemeConstantOverride("separation", Tokens.SpacingTight);
        lists.AddChild(_assignList);

        _availableList = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        _availableList.AddThemeConstantOverride("separation", Tokens.SpacingTight);
        lists.AddChild(_availableList);

        _unavailableList = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        _unavailableList.AddThemeConstantOverride("separation", Tokens.SpacingTight);
        lists.AddChild(_unavailableList);

        _errorLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        _errorLabel.ThemeTypeVariation = "ErrorText";
        _bodyContent.AddChild(_errorLabel);

        var footer = new HFlowContainer
        {
            Alignment = FlowContainer.AlignmentMode.End,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        footer.AddThemeConstantOverride("separation", Tokens.SpacingBase);
        shell.AddChild(footer);

        _authorizeButton = NewFooterButton(
            iconPath: IconPaths.Check,
            label: UiText.Get("Establish Founding Site"),
            variation: "HudButtonSelected");
        _farmButton = NewFooterButton(
            iconPath: IconPaths.Leaf,
            label: UiText.Get("Build Farm"),
            variation: "HudButtonSelected");
        _cultivationButton = NewFooterButton(
            iconPath: IconPaths.Leaf,
            label: UiText.Get("Prepare Cultivation Site"),
            variation: "HudButtonSelected");
        _quarryButton = NewFooterButton(
            iconPath: IconPaths.Building,
            label: UiText.Get("Build Quarry"),
            variation: "HudButtonSelected");
        _townHallButton = NewFooterButton(
            iconPath: IconPaths.Building,
            label: UiText.Get("Build Town Hall"),
            variation: "HudButtonSelected");
        _bedrollButton = NewFooterButton(
            iconPath: IconPaths.House,
            label: UiText.Get("Build Bedroll"),
            variation: "HudButtonSelected");
        _cacheButton = NewFooterButton(
            iconPath: IconPaths.Building,
            label: UiText.Get("Build Cache"),
            variation: "HudButtonSelected");
        _canopyButton = NewFooterButton(
            iconPath: IconPaths.House,
            label: UiText.Get("Build Canopy"),
            variation: "HudButtonSelected");
        _clearCargoButton = NewFooterButton(
            iconPath: IconPaths.Close,
            label: UiText.Get("Return carried cargo"),
            variation: "HudButton");
        _clearCargoButton.TooltipText = UiText.Get(
            "Returns all carried founding resources to the ground so you can prepare the exact load for the next module.");
        _pauseButton = NewFooterButton(
            iconPath: IconPaths.Pause,
            label: UiText.Get("Pause"),
            variation: "HudButton");
        _resumeButton = NewFooterButton(
            iconPath: IconPaths.Play,
            label: UiText.Get("Resume"),
            variation: "HudButton");
        _cancelButton = NewFooterButton(
            iconPath: IconPaths.Close,
            label: UiText.Get("Cancel project"),
            variation: "HudButtonDanger");
        _cancelButton.TooltipText = UiText.Get("Cancel the project. The deposit is lost and the site is cleared.");
        _viewHeroButton = StandardButtons.ViewHeroButton();
        _viewHeroButton.ThemeTypeVariation = "HudButton";
        _viewBuildingButton = NewFooterButton(
            iconPath: IconPaths.House,
            label: UiText.Get("View shelter"),
            variation: "HudButtonSelected");
        _authorizeButton.Pressed += () => EmitSignal(
            SignalName.AuthorizeRequested, (int)ConstructionKind.FoundingSite);
        _farmButton.Pressed += () => EmitSignal(
            SignalName.AuthorizeRequested, (int)ConstructionKind.Farm);
        _cultivationButton.Pressed += () => EmitSignal(
            SignalName.AuthorizeRequested, (int)ConstructionKind.CultivationSite);
        _quarryButton.Pressed += () => EmitSignal(
            SignalName.AuthorizeRequested, (int)ConstructionKind.Quarry);
        _townHallButton.Pressed += () => EmitSignal(
            SignalName.AuthorizeRequested, (int)ConstructionKind.TownHall);
        _bedrollButton.Pressed += () => OnFoundingModuleRequested(FoundingSiteModule.Bedroll);
        _cacheButton.Pressed += () => OnFoundingModuleRequested(FoundingSiteModule.Cache);
        _canopyButton.Pressed += () => OnFoundingModuleRequested(FoundingSiteModule.Canopy);
        _clearCargoButton.Pressed += OnClearCargoRequested;
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
        footer.AddChild(_cultivationButton);
        footer.AddChild(_farmButton);
        footer.AddChild(_quarryButton);
        footer.AddChild(_townHallButton);
        footer.AddChild(_bedrollButton);
        footer.AddChild(_cacheButton);
        footer.AddChild(_canopyButton);
        footer.AddChild(_clearCargoButton);
        footer.AddChild(_viewBuildingButton);

        _primaryFocus = _authorizeButton;
    }

    private void OnClearCargoRequested()
    {
        int returned = _controller.ReturnFoundingCargo();
        if (returned > 0)
        {
            Notifier.Show(UiText.Format("Returned {0} carried units to the ground.", returned));
        }
        Refresh();
    }

    private void OnFoundingModuleRequested(FoundingSiteModule module)
    {
        ConstructionSnapshot.ProjectItem? project = CurrentProject();
        if (project is null) return;
        ConstructionAuthorizationResult result =
            _controller.TryAuthorizeFoundingSiteModule(project.Id, module);
        if (!result.IsSuccess)
        {
            _errorLabel.Text = FormatAuthorizationError(result.Outcome);
        }
        Refresh(clearError: result.IsSuccess);
    }

    private static IconButton NewFooterButton(string iconPath, string label, string variation) =>
        StandardButtons.IconAction(iconPath, label, variation);

    public override void _ExitTree()
    {
        GetViewport().SizeChanged -= ApplyResponsiveMinimumSize;
        if (_controller is null) return;
        _controller.HeroCreated -= OnHeroCreated;
        _controller.ProjectStateChanged -= OnProjectStateChanged;
        _controller.BuildingStateChanged -= OnBuildingStateChanged;
        _controller.SelectionChanged -= OnSelectionChanged;
        _controller.CitizenAssignmentRejected -= OnCitizenAssignmentRejected;
        AuthorizeRequested -= OnAuthorizeRequested;
        PauseRequested -= OnPauseRequested;
        ResumeRequested -= OnResumeRequested;
        CancelProjectRequested -= OnCancelProjectRequested;
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

    internal void ScrollBodyToEndForVisualRegression()
    {
        if (System.Environment.GetEnvironmentVariable("WOG_VISUAL_CAPTURE") != "1") return;
        CallDeferred(MethodName.ApplyVisualRegressionScroll);
    }

    internal void PressHeaderCloseForVisualRegression()
    {
        if (System.Environment.GetEnvironmentVariable("WOG_VISUAL_CAPTURE") != "1") return;
        _header.PressCloseForVisualRegression();
    }

    private void ApplyVisualRegressionScroll()
    {
        _bodyScroll.ScrollVertical = int.MaxValue;
    }

    private void Render(ConstructionSnapshot snapshot)
    {
        _foundingResourcesPanel.Visible = !snapshot.HasHome;
        if (!snapshot.HasHome)
        {
            _foundingResourcesPanel.Render(
                snapshot.FoundingResources,
                snapshot.FoundingStorageCount,
                snapshot.FoundingStorageCapacity,
                snapshot.HasFoundingCache
                    ? ResourceInventoryOwner.FoundingCache
                    : ResourceInventoryOwner.FounderCargo);
        }
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
        string blueprintTitle = UiText.Get(hasHome ? "Choose the next construction" : "Establish the Founding Site");
        _header.SetTitle(blueprintTitle);
        _title.Text = blueprintTitle;
        // Preview the shelter art so the player knows what they are about to build.
        var shelterArt = BuildingArt.GetTexturePath(ConstructionKind.FoundingSite);
        if (shelterArt is { } path)
        {
            _constructionPreview.Texture = ResourceLoader.Load<Texture2D>(path);
            _constructionPreview.Visible = !hasHome;
        }
        else
        {
            _constructionPreview.Visible = false;
        }
        _description.Text = UiText.Get(hasHome
            ? "Choose a productive building. Its worksite will appear automatically in the city; open Construction progress to assign contributors."
            : "Claim one 3 × 3 site. Build its Campfire first, then choose Bedroll or Cache before adding the Canopy.");
        _phaseLabel.Visible = false;
        _progress.Visible = false;
        _statusLabel.Visible = false;
        _contributors.Visible = false;
        _requirementsLabel.Visible = true;
        _assignList.Visible = false;
        _availableList.Visible = false;
        _unavailableList.Visible = false;
        _errorLabel.Visible = !string.IsNullOrEmpty(_errorLabel.Text);
        bool canAuthorise = snapshot.HasHero && snapshot.Project is null;
        var shelter = snapshot.OptionFor(ConstructionKind.FoundingSite);
        var farm = snapshot.OptionFor(ConstructionKind.Farm);
        var cultivation = snapshot.OptionFor(ConstructionKind.CultivationSite);
        var quarry = snapshot.OptionFor(ConstructionKind.Quarry);
        var townHall = snapshot.OptionFor(ConstructionKind.TownHall);
        _requirementsLabel.Text = hasHome
            ? UiText.Format(
                "ui.construction.requirements_four",
                DescribeMaterials(cultivation),
                DescribeMaterials(farm),
                DescribeMaterials(quarry),
                DescribeMaterials(townHall))
            : UiText.Format("ui.construction.campfire_requirements", DescribeMaterials(shelter));
        _authorizeButton.Visible = !hasHome;
        bool authorizeEnabled = canAuthorise && shelter.CanPayDeposit;
        _authorizeButton.Disabled = !authorizeEnabled;
        _authorizeButton.TooltipText = UiText.Get(authorizeEnabled
            ? "Establish the Founding Site and begin its Campfire."
            : "Gather 3 Branches and 2 Small Stone for the Campfire.");
        _cultivationButton.Visible = true;
        bool cultivationExists = snapshot.HasCultivationSite;
        bool cultivationEnabled = canAuthorise
            && hasHome
            && !cultivationExists
            && cultivation.CanPayDeposit;
        _cultivationButton.Disabled = !cultivationEnabled;
        _cultivationButton.TooltipText = UiText.Get(!hasHome
            ? "Build the Basic Shelter first to prepare a Cultivation Site."
            : cultivationExists
                ? "The first Cultivation Site is already prepared."
                : cultivationEnabled
                    ? "Prepare one plot with 1 Branch and 1 Small Stone."
                    : "Gather 1 Branch and 1 Small Stone to prepare the plot.");
        // Farm and Quarry are now always visible so the player can see
        // the upcoming options. They are disabled until the Basic
        // Shelter exists; the tooltip explains the dependency.
        _farmButton.Visible = true;
        bool farmEnabled = canAuthorise && hasHome && farm.CanPayDeposit;
        _farmButton.Disabled = !farmEnabled;
        _farmButton.TooltipText = UiText.Get(!hasHome
            ? "Build the Basic Shelter first to unlock the Farm."
            : farmEnabled
                ? "Build a Farm."
                : "Not enough materials to authorise a Farm.");
        _quarryButton.Visible = true;
        bool quarryEnabled = canAuthorise && hasHome && quarry.CanPayDeposit;
        _quarryButton.Disabled = !quarryEnabled;
        _quarryButton.TooltipText = UiText.Get(!hasHome
            ? "Build the Basic Shelter first to unlock the Quarry."
            : quarryEnabled
                ? "Build a Quarry."
                : "Not enough materials to authorise a Quarry.");
        _townHallButton.Visible = true;
        bool townHallExists = snapshot.HasTownHall;
        bool townHallEnabled = canAuthorise && hasHome && !townHallExists && townHall.CanPayDeposit;
        _townHallButton.Disabled = !townHallEnabled;
        _townHallButton.TooltipText = UiText.Get(!hasHome
            ? "Build the Basic Shelter first to unlock the Town Hall."
            : townHallExists
                ? "The city already has a Town Hall."
                : townHallEnabled
                    ? "Build a Town Hall to host one expedition prospect."
                    : "Not enough materials to authorise a Town Hall.");
        DetectEnableTransition(authorizeEnabled, ref _wasAuthorizeEnabled, _authorizeButton);
        DetectEnableTransition(cultivationEnabled, ref _wasCultivationEnabled, _cultivationButton);
        DetectEnableTransition(farmEnabled, ref _wasFarmEnabled, _farmButton);
        DetectEnableTransition(quarryEnabled, ref _wasQuarryEnabled, _quarryButton);
        DetectEnableTransition(townHallEnabled, ref _wasTownHallEnabled, _townHallButton);
        _pauseButton.Visible = false;
        _resumeButton.Visible = false;
        _cancelButton.Visible = false;
        _viewBuildingButton.Visible = false;
        _bedrollButton.Visible = false;
        _cacheButton.Visible = false;
        _canopyButton.Visible = false;
        _clearCargoButton.Visible = !hasHome && snapshot.ReturnableFoundingCargoCount > 0;
        _viewHeroButton.Visible = snapshot.HasHero;
        _primaryFocus = !hasHome
            ? authorizeEnabled
                ? _authorizeButton
                : _clearCargoButton.Visible
                    ? _clearCargoButton
                    : _authorizeButton
            : !_cultivationButton.Disabled
                ? _cultivationButton
                : !_farmButton.Disabled
                    ? _farmButton
                : !_quarryButton.Disabled
                    ? _quarryButton
                    : !_townHallButton.Disabled
                        ? _townHallButton
                        : _viewHeroButton;
        if (_primaryFocus.Disabled) _primaryFocus = _viewHeroButton;
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
        bool choosingFoundingModule = project.ActiveFoundingModule is null
            && snapshot.FoundingModuleOptions.Count > 0;
        string activeTitle = project.ActiveFoundingModule is FoundingSiteModule module
            ? UiText.Format(
                "ui.construction.founding_phase",
                UiText.Get("Founding Site"),
                UiText.Get(FoundingSiteRules.DisplayNameFor(module)))
            : project.DisplayName;
        _header.SetTitle(activeTitle);
        _title.Text = activeTitle;
        _description.Text = choosingFoundingModule
            ? UiText.Get("Choose the next Founding Site module. Bedroll and Cache may be completed in either order; Canopy requires both.")
            : project.AssignedCount == 0
            ? project.RemainingInputs.Count > 0
                ? UiText.Format("ui.construction.gather_remaining", DescribeInputs(project.RemainingInputs))
                : UiText.Get("Assign at least one available citizen below. Construction cannot advance without contributors.")
            : UiText.Format(
                "ui.construction.contributors_interval",
                SimulationTimeText.FormatDurationLocalized(
                    ConstructionRules.WorkIntervalTicks));
        _phaseLabel.Visible = true;
        _progress.Visible = !choosingFoundingModule;
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
        // While choosing a module this label is the only place the player can
        // read what each option costs without hovering a disabled button. A
        // Founding Site has no RemainingInputs, so the slot was previously hidden
        // in exactly the state where the cost matters most.
        string moduleChoices = choosingFoundingModule
            ? DescribeModuleChoices(snapshot)
            : string.Empty;
        _requirementsLabel.Visible = choosingFoundingModule
            ? moduleChoices.Length > 0
            : project.RemainingInputs.Count > 0;
        _requirementsLabel.Text = choosingFoundingModule
            ? moduleChoices
            : project.RemainingInputs.Count > 0
                ? UiText.Format("ui.construction.still_needed", DescribeInputs(project.RemainingInputs))
                : string.Empty;
        _assignList.Visible = true;
        _availableList.Visible = true;
        _unavailableList.Visible = true;
        _errorLabel.Visible = !string.IsNullOrEmpty(_errorLabel.Text);
        var phase = ConstructionRules.PhaseFor(project.Progress, project.RequiredWork);
        _phaseLabel.Text = choosingFoundingModule
            ? UiText.Get("Module complete — awaiting next authorization")
            : project.ActiveFoundingModule is FoundingSiteModule activeModule
                ? UiText.Format(
                    "ui.construction.module_progress",
                    UiText.Get(FoundingSiteRules.DisplayNameFor(activeModule)),
                    UiText.Get(ConstructionRules.Describe(phase)))
                : UiText.Get(ConstructionRules.Describe(phase));
        _progress.MinValue = 0;
        _progress.MaxValue = project.RequiredWork;
        _progress.Value = project.Progress;
        _statusLabel.Text = DescribeProjectStatus(project);
        _contributors.Text = UiText.Format(
            "ui.construction.contributors",
            project.AssignedCount,
            project.WorkerCapacity);

        ClearList(_assignList);
        ClearList(_availableList);
        ClearList(_unavailableList);
        PopulateAssigned(project);
        PopulateAvailable(project, snapshot.AvailableCitizens);
        PopulateUnavailable(snapshot.UnavailableCitizens);

        _authorizeButton.Visible = false;
        _farmButton.Visible = false;
        _cultivationButton.Visible = false;
        _quarryButton.Visible = false;
        _townHallButton.Visible = false;
        _clearCargoButton.Visible = snapshot.ReturnableFoundingCargoCount > 0;
        ConfigureFoundingModuleButton(
            _bedrollButton,
            snapshot.FoundingOptionFor(FoundingSiteModule.Bedroll),
            choosingFoundingModule);
        ConfigureFoundingModuleButton(
            _cacheButton,
            snapshot.FoundingOptionFor(FoundingSiteModule.Cache),
            choosingFoundingModule);
        ConfigureFoundingModuleButton(
            _canopyButton,
            snapshot.FoundingOptionFor(FoundingSiteModule.Canopy),
            choosingFoundingModule);
        _viewBuildingButton.Visible = false;
        _pauseButton.Visible = !choosingFoundingModule && project.Enabled;
        _resumeButton.Visible = !choosingFoundingModule && !project.Enabled;
        _cancelButton.Visible = project.CompletedFoundingModules.Count == 0;
        _viewHeroButton.Visible = true;
        _primaryFocus = choosingFoundingModule
            ? !_bedrollButton.Disabled && _bedrollButton.Visible
                ? _bedrollButton
                : !_cacheButton.Disabled && _cacheButton.Visible
                    ? _cacheButton
                    : !_canopyButton.Disabled && _canopyButton.Visible
                        ? _canopyButton
                        : _clearCargoButton.Visible
                            ? _clearCargoButton
                            : _viewHeroButton
            : project.Enabled ? _pauseButton : _resumeButton;
        _primaryFocus.GrabFocus();
    }

    private static void ConfigureFoundingModuleButton(
        IconButton button,
        ConstructionSnapshot.FoundingModuleOptionItem? option,
        bool choosingModule)
    {
        button.Visible = choosingModule && option is { Completed: false, PrerequisitesMet: true };
        button.Disabled = option is null || !option.CanAuthorize;
        button.TooltipText = option is null
            ? string.Empty
            : option.Completed
                ? UiText.Get("Module already completed.")
                : !option.PrerequisitesMet
                    ? UiText.Get("Complete the prerequisite modules first.")
                    : option.CanAuthorize
                        ? UiText.Format(
                            "ui.construction.module_cost",
                            DescribeModuleMaterials(option.Materials))
                        : UiText.Format(
                            "ui.construction.module_missing",
                            DescribeMissingModuleMaterials(option.Materials));
    }

    /// <summary>
    /// One line per authorisable module with its cost, so the player can see why
    /// a module button is disabled without hovering it.
    /// </summary>
    private static string DescribeModuleChoices(ConstructionSnapshot snapshot)
    {
        var lines = new List<string>();
        foreach (ConstructionSnapshot.FoundingModuleOptionItem option in
            snapshot.FoundingModuleOptions)
        {
            if (option.Completed || !option.PrerequisitesMet) continue;
            lines.Add(UiText.Format(
                "ui.construction.module_choice",
                UiText.Get(FoundingSiteRules.DisplayNameFor(option.Module)),
                DescribeModuleMaterials(option.Materials)));
        }
        return string.Join("\n", lines);
    }

    /// <summary>
    /// The full cost of a Founding Site module, always with what the city
    /// actually holds. A disabled module button used to say only "gather the full
    /// module cost first", which never told the player what or how much.
    /// </summary>
    private static string DescribeModuleMaterials(
        IReadOnlyList<ConstructionSnapshot.MaterialItem> materials)
    {
        var parts = new List<string>();
        foreach (ConstructionSnapshot.MaterialItem material in materials)
        {
            parts.Add(UiText.Format(
                "ui.construction.material_full",
                material.Required,
                ResourceTypeLocalizer.Label(material.Resource),
                material.Available));
        }
        return parts.Count == 0 ? UiText.Get("no material cost") : string.Join(" + ", parts);
    }

    private static string DescribeMissingModuleMaterials(
        IReadOnlyList<ConstructionSnapshot.MaterialItem> materials)
    {
        var parts = new List<string>();
        foreach (ConstructionSnapshot.MaterialItem material in materials)
        {
            if (material.Available >= material.Required) continue;
            parts.Add(UiText.Format(
                "ui.construction.material_full",
                material.Required,
                ResourceTypeLocalizer.Label(material.Resource),
                material.Available));
        }
        return parts.Count == 0
            ? DescribeModuleMaterials(materials)
            : string.Join(" + ", parts);
    }

    private void RenderCompleted(ConstructionSnapshot snapshot)
    {
        BuildingId? shelterId = snapshot.HomeBuildingId;
        string completedTitle = UiText.Get("Basic Shelter completed");
        _header.SetTitle(completedTitle);
        _title.Text = completedTitle;
        if (shelterId.HasValue)
        {
            _description.Text = UiText.Get("The Basic Shelter is ready. Open the building to assign it as resting site.");
        }
        else
        {
            _description.Text = UiText.Get("A Basic Shelter is ready.");
        }
        _phaseLabel.Visible = false;
        _progress.Visible = false;
        _statusLabel.Visible = false;
        _contributors.Visible = false;
        _requirementsLabel.Visible = false;
        _assignList.Visible = false;
        _availableList.Visible = false;
        _unavailableList.Visible = false;
        _errorLabel.Visible = false;
        _authorizeButton.Visible = false;
        _farmButton.Visible = false;
        _cultivationButton.Visible = false;
        _quarryButton.Visible = false;
        _townHallButton.Visible = false;
        _bedrollButton.Visible = false;
        _cacheButton.Visible = false;
        _canopyButton.Visible = false;
        _clearCargoButton.Visible = false;
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
        AddSectionHeader(_assignList, UiText.Get("Assigned"));
        if (project.AssignedCount == 0)
        {
            AddListLabel(_assignList, UiText.Get("No contributors yet."));
            return;
        }
        foreach (var citizen in project.AssignedCitizens)
        {
            var row = InstantiateAssignmentRow(
                citizen.Id.Value,
                citizen.Name,
                UiText.Get("Remove"),
                UiText.Format("ui.construction.remove_from_project", citizen.Name));
            row.ActionRequested += id =>
                EmitSignal(SignalName.UnassignFromProjectRequested, project.Id.Value, id);
            _assignList.AddChild(row);
        }
    }

    private void PopulateAvailable(
        ConstructionSnapshot.ProjectItem project,
        IReadOnlyList<ConstructionSnapshot.CitizenItem> availableCitizens)
    {
        AddSectionHeader(_availableList, UiText.Get("Available"));
        bool atCapacity = project.AssignedCount >= project.WorkerCapacity;
        foreach (var citizen in availableCitizens)
        {
            var row = InstantiateAssignmentRow(
                citizen.Id.Value,
                citizen.Name,
                UiText.Get("Assign"),
                UiText.Format("ui.construction.assign_to_project", citizen.Name),
                disabled: atCapacity);
            row.ActionRequested += id =>
                EmitSignal(SignalName.AssignToProjectRequested, project.Id.Value, id);
            _availableList.AddChild(row);
        }
        if (_availableList.GetChildCount() == 1)
        {
            AddListLabel(_availableList, UiText.Get(atCapacity ? "Project at capacity." : "No free citizens."));
        }
    }

    private void PopulateUnavailable(
        IReadOnlyList<ConstructionSnapshot.UnavailableCitizenItem> unavailableCitizens)
    {
        if (unavailableCitizens.Count == 0)
        {
            _unavailableList.Visible = false;
            return;
        }
        _unavailableList.Visible = true;
        AddSectionHeader(_unavailableList, UiText.Get("ui.assignment.unavailable_title"));
        foreach (var citizen in unavailableCitizens)
        {
            string reason = DescribeUnavailabilityReason(citizen);
            var row = InstantiateAssignmentRow(
                citizen.Id.Value,
                UiText.Format("ui.assignment.unavailable_row", citizen.Name, reason),
                UiText.Get("Assign"),
                reason,
                disabled: true);
            _unavailableList.AddChild(row);
        }
    }

    private static string DescribeUnavailabilityReason(ConstructionSnapshot.UnavailableCitizenItem citizen) =>
        citizen.Reason switch
        {
            CitizenAvailabilityReason.AssignedToBuilding =>
                UiText.Format("ui.assignment.reason_building", citizen.LocationName ?? UiText.Get("Unknown")),
            CitizenAvailabilityReason.AssignedToConstruction =>
                UiText.Format("ui.assignment.reason_construction", citizen.LocationName ?? UiText.Get("Unknown")),
            CitizenAvailabilityReason.OnExpedition => UiText.Get("ui.assignment.reason_expedition"),
            CitizenAvailabilityReason.Recovering => UiText.Get("ui.assignment.reason_recovering"),
            _ => UiText.Get("Available"),
        };

    private static void AddSectionHeader(VBoxContainer list, string title)
    {
        var label = new Label { Text = title };
        label.ThemeTypeVariation = "HudLabel";
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
            ThemeTypeVariation = "HudCaption",
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
            UiText.Format(
                "ui.construction.active_interval",
                SimulationTimeText.FormatDurationLocalized(
                    ConstructionRules.WorkIntervalTicks)),
        ConstructionStopCause.Paused => UiText.Get("Paused by the player"),
        ConstructionStopCause.NoWorkers => project.RemainingInputs.Count > 0
            ? UiText.Format("ui.construction.waiting_materials", DescribeInputs(project.RemainingInputs))
            : UiText.Get("Waiting for contributors"),
        ConstructionStopCause.MissingMaterials =>
            UiText.Format("ui.construction.waiting_materials", DescribeInputs(project.RemainingInputs)),
        ConstructionStopCause.WorkersInTransit => UiText.Get("Contributor travelling to the site"),
        ConstructionStopCause.WorkersExhausted => UiText.Get("Waiting: contributors exhausted"),
        ConstructionStopCause.Night => UiText.Get("Resting during the night"),
        ConstructionStopCause.Completed => UiText.Get("Completed"),
        ConstructionStopCause.AwaitingModule => UiText.Get("Awaiting next Founding Site module"),
        ConstructionStopCause.NoHero => UiText.Get("No hero available"),
        _ => project.StopCause.ToString(),
    };

    private static string DescribeMaterials(ConstructionSnapshot.OptionItem option)
    {
        if (option.Materials.Count == 0) return UiText.Get("no material cost");
        var parts = new List<string>();
        foreach (var material in option.Materials)
        {
            string resource = ResourceTypeLocalizer.Label(material.Resource);
            parts.Add(option.Kind is ConstructionKind.FoundingSite
                or ConstructionKind.CultivationSite
                ? UiText.Format(
                    "ui.construction.material_full",
                    material.Required,
                    UiText.Get(resource),
                    material.Available)
                : UiText.Format(
                    "ui.construction.material",
                    material.DepositRequired,
                    UiText.Get(resource),
                    material.Required,
                    material.Available));
        }
        return string.Join(" + ", parts);
    }

    private static string DescribeInputs(IReadOnlyList<RecipeInput> inputs)
    {
        var parts = new List<string>();
        foreach (var input in inputs)
        {
            parts.Add($"{input.Amount} {ResourceTypeLocalizer.Label(input.Resource)}");
        }
        return string.Join(" + ", parts);
    }

    internal static string FormatAuthorizationError(ConstructionAuthorizationOutcome outcome) => outcome switch
    {
        ConstructionAuthorizationOutcome.MissingMaterials =>
            UiText.Get("Missing materials. Check the requirements above."),
        ConstructionAuthorizationOutcome.HomeRequired => UiText.Get("Build the Basic Shelter first."),
        ConstructionAuthorizationOutcome.AlreadyAuthorized => UiText.Get("Finish or cancel the current project first."),
        ConstructionAuthorizationOutcome.NoHero => UiText.Get("Create the founding hero first."),
        ConstructionAuthorizationOutcome.HomeAlreadyBuilt => UiText.Get("The Basic Shelter already exists."),
        ConstructionAuthorizationOutcome.WorldNotEmpty =>
            UiText.Get("The founding shelter can only start in the initial world."),
        ConstructionAuthorizationOutcome.NoAvailableLot =>
            UiText.Get("No unlocked parcel has a free building lot."),
        ConstructionAuthorizationOutcome.InvalidModule =>
            UiText.Get("That Founding Site module is not available."),
        ConstructionAuthorizationOutcome.PrerequisitesNotMet =>
            UiText.Get("Complete the prerequisite Founding Site modules first."),
        _ => UiText.Get("Construction could not be authorized."),
    };
}
