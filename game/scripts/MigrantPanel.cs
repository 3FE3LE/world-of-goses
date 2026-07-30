#nullable enable
using System.Collections.Generic;
using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Ui;

namespace WorldofGoses;

/// <summary>
/// Compact city roster plus the current deterministic recruitment action.
/// Selection is presentation-only: assignments remain owned by the existing
/// building and construction panels.
/// </summary>
[GlobalClass]
public partial class MigrantPanel : Control
{
    private static readonly Vector2 PreferredSize = new(600, 460);
    private const float ViewportInset = 32f;

    [Export] public NodePath ControllerPath { get; set; } = "../../../../CityWorldController";
    [Export] public NodePath ModalHostPath { get; set; } = "../ModalHost";
    [Export] public NodePath RecruitButtonPath { get; set; } = "Surface/Margin/Layout/RecruitButton";
    [Export] public NodePath CloseButtonPath { get; set; } = "Surface/Margin/Layout/CloseButton";
    [Export] public NodePath StatusLabelPath { get; set; } = "Surface/Margin/Layout/StatusLabel";
    [Export] public NodePath CitizenListPath { get; set; } = "Surface/Margin/Layout/BodyScroll/Body/CitizenList/Rows";
    [Export] public NodePath DetailLabelPath { get; set; } = "Surface/Margin/Layout/BodyScroll/Body/DetailLabel";

    private CityWorldController _controller = null!;
    private ModalHost _modalHost = null!;
    private Button _recruitButton = null!;
    private Button _closeButton = null!;
    private Label _statusLabel = null!;
    private VBoxContainer _citizenList = null!;
    private Label _detailLabel = null!;
    private CitizenId? _selectedCitizenId;
    private readonly Dictionary<CitizenId, Button> _citizenButtons = new();

    public override void _Ready()
    {
        OverlayLayers.Apply(this, OverlayLayers.Modal);

        _controller = GetNode<CityWorldController>(ControllerPath);
        _modalHost = GetNode<ModalHost>(ModalHostPath);
        _recruitButton = GetNode<Button>(RecruitButtonPath);
        _closeButton = GetNode<Button>(CloseButtonPath);
        _statusLabel = GetNode<Label>(StatusLabelPath);
        _citizenList = GetNode<VBoxContainer>(CitizenListPath);
        _detailLabel = GetNode<Label>(DetailLabelPath);

        _recruitButton.Pressed += OnRecruitPressed;
        _closeButton.Pressed += OnClosePressed;
        _controller.CitizensChanged += OnCitizensChanged;
        _controller.BuildingStateChanged += OnWorldStateChanged;
        _controller.ProjectStateChanged += OnWorldStateChanged;
        _controller.ExpeditionStateChanged += OnWorldStateChanged;
        GetViewport().SizeChanged += ApplyResponsiveBounds;

        Hide();
        CallDeferred(MethodName.ApplyResponsiveBounds);
    }

    public override void _ExitTree()
    {
        if (_controller is not null)
        {
            _controller.CitizensChanged -= OnCitizensChanged;
            _controller.BuildingStateChanged -= OnWorldStateChanged;
            _controller.ProjectStateChanged -= OnWorldStateChanged;
            _controller.ExpeditionStateChanged -= OnWorldStateChanged;
        }
        GetViewport().SizeChanged -= ApplyResponsiveBounds;
    }

    public void Open()
    {
        Show();
        _modalHost.Open(this);
        Refresh();
        (_recruitButton.Disabled ? _closeButton : _recruitButton).GrabFocus();
    }

    public void ShowForVisualRegression()
    {
        if (_controller.World.Hero is not null
            && _controller.World.Citizens.Count < 2)
        {
            _controller.TryAcceptPendingProspect();
        }
        Open();
    }

    public void Close()
    {
        _modalHost.Close();
    }

    private void ApplyResponsiveBounds()
    {
        Vector2 parentSize = GetParentOrNull<Control>()?.Size ?? Vector2.Zero;
        if (parentSize.X < 100f || parentSize.Y < 100f)
        {
            parentSize = GetViewportRect().Size;
        }
        Vector2 size = new(
            Mathf.Max(360f, Mathf.Min(PreferredSize.X, parentSize.X - ViewportInset * 2f)),
            Mathf.Max(320f, Mathf.Min(PreferredSize.Y, parentSize.Y - ViewportInset * 2f)));
        CustomMinimumSize = Vector2.Zero;
        SetAnchorsPreset(LayoutPreset.Center);
        OffsetLeft = -Mathf.Round(size.X * 0.5f);
        OffsetTop = -Mathf.Round(size.Y * 0.5f);
        OffsetRight = Mathf.Round(size.X * 0.5f);
        OffsetBottom = Mathf.Round(size.Y * 0.5f);
    }

    private void OnRecruitPressed()
    {
        if (_controller.World.Hero is null)
        {
            Notifier.ShowError(UiText.Get("Create a hero first."));
            return;
        }
        CityWorld.MigrantResult result = _controller.TryAcceptPendingProspect();
        if (!result.IsSuccess)
        {
            Notifier.ShowError(UiText.Format("ui.citizens.recruit_failed", result.Outcome));
        }
        Refresh();
    }

    private void OnClosePressed() => Close();

    private void OnCitizensChanged() => Refresh();

    private void OnWorldStateChanged(int _) => Refresh();

    private void Refresh()
    {
        foreach (Node child in _citizenList.GetChildren())
        {
            _citizenList.RemoveChild(child);
            child.QueueFree();
        }
        _citizenButtons.Clear();

        if (!_selectedCitizenId.HasValue
            || _controller.World.GetCitizen(_selectedCitizenId.Value) is null)
        {
            _selectedCitizenId = _controller.World.Hero?.Id;
        }

        int migrantCount = 0;
        foreach (Citizen citizen in _controller.World.Citizens.Values)
        {
            if (!citizen.IsHero) migrantCount++;
            var button = new Button
            {
                Text = DescribeRosterRow(citizen),
                Alignment = HorizontalAlignment.Left,
                CustomMinimumSize = new Vector2(0, 44),
                ThemeTypeVariation = _selectedCitizenId == citizen.Id
                    ? "ButtonPrimary"
                    : "ButtonText",
            };
            CitizenId id = citizen.Id;
            button.Pressed += () => SelectCitizen(id);
            _citizenList.AddChild(button);
            _citizenButtons[id] = button;
        }

        CityWorld world = _controller.World;
        bool hasHero = world.Hero is not null;
        _recruitButton.Disabled = !hasHero || world.AvailableHousing == 0;
        _recruitButton.TooltipText = !hasHero
            ? UiText.Get("Create a hero first.")
            : world.AvailableHousing == 0
                ? UiText.Get("ui.citizens.housing_full")
                : UiText.Get("ui.citizens.recruit_available");
        _statusLabel.Text = UiText.Format(
            "ui.citizens.count_with_housing",
            world.Citizens.Count,
            migrantCount,
            world.HousingCapacity);
        Citizen? selected = _selectedCitizenId.HasValue
            ? _controller.World.GetCitizen(_selectedCitizenId.Value)
            : null;
        _detailLabel.Text = selected is null
            ? UiText.Get("Recruit the first citizen to begin the roster.")
            : DescribeCitizen(selected);
    }

    private void SelectCitizen(CitizenId id)
    {
        _selectedCitizenId = id;
        _controller.SelectCitizenForObservation(id);
        Refresh();
        CallDeferred(MethodName.FocusSelectedCitizen);
    }

    private void FocusSelectedCitizen()
    {
        if (_selectedCitizenId.HasValue
            && _citizenButtons.TryGetValue(_selectedCitizenId.Value, out Button? button))
        {
            button.GrabFocus();
        }
    }

    private string DescribeRosterRow(Citizen citizen)
    {
        string role = UiText.Get(citizen.IsHero ? "Hero" : "Citizen");
        return UiText.Format("ui.citizens.roster_row", citizen.Name, role, DescribeStatus(citizen));
    }

    private string DescribeCitizen(Citizen citizen)
    {
        string assignment = citizen.CurrentAssignment.HasValue
            ? ResolveAssignmentName(citizen.CurrentAssignment.Value)
            : UiText.Get("None");
        string description = UiText.Format(
            "ui.citizens.detail",
            citizen.Name,
            DescribeStatus(citizen),
            assignment,
            UiText.Get(ProfileCatalog.Get(citizen.Profile.Lineage).DisplayName),
            DescribeAffinities(citizen.Profile),
            citizen.CurrentStamina,
            citizen.MaxStamina);
        if (!OS.IsDebugBuild()) return description;

        CitizenDebugSnapshot? debug = _controller.GetCitizenDebugSnapshot(citizen.Id);
        if (debug is null) return description;
        CitizenRoutineSnapshot routine = debug.Routine;
        return description + "\n\n" + UiText.Format(
            "ui.citizens.debug_context",
            routine.Activity,
            routine.ContextLocation,
            routine.BlockReason,
            debug.AssignedBuildingId?.Value.ToString() ?? "—",
            debug.ShelterId?.Value.ToString() ?? "—",
            FormatOptionalWorldTime(routine.ActivityStartedAtTick),
            FormatOptionalWorldTime(routine.ExpectedCompletionTick),
            FormatOptionalWorldTime(routine.NextTransitionTick),
            System.DateTimeOffset.FromUnixTimeMilliseconds(
                debug.LastSimulationProcessedAtUnixMillis).ToLocalTime().ToString("HH:mm:ss"));
    }

    private static string FormatOptionalWorldTime(int? tick) => tick is int value
        ? SimulationTimeText.FormatLocalized(value)
        : "—";

    private static string DescribeAffinities(CitizenProfile profile) =>
        string.Join(", ",
            UiText.Get(ProfileCatalog.DisplayName(profile.ProfessionalAffinities[0])),
            UiText.Get(ProfileCatalog.DisplayName(profile.ProfessionalAffinities[1])),
            UiText.Get(ProfileCatalog.DisplayName(profile.ProfessionalAffinities[2])));

    private string DescribeStatus(Citizen citizen)
    {
        if (_controller.World.IsCitizenOnActiveExpedition(citizen.Id))
        {
            return UiText.Get("On expedition");
        }
        if (citizen.CurrentAssignment.HasValue)
        {
            return citizen.CurrentLocation == CitizenLocation.AtWork
                ? UiText.Get("Working")
                : UiText.Get("ui.status.assigned");
        }
        return citizen.CurrentLocation == CitizenLocation.AtHome
            ? UiText.Get("At home")
            : citizen.CurrentLocation.ToString();
    }

    private string ResolveAssignmentName(BuildingId assignmentId)
    {
        Building? building = _controller.World.GetBuilding(assignmentId);
        if (building is not null) return UiText.Get(building.DisplayName);
        ConstructionProject? project = _controller.World.GetProject(assignmentId);
        return project is not null
            ? UiText.Format("ui.citizens.construction_assignment", UiText.Get(project.DisplayName))
            : UiText.Get("Unknown");
    }

    private static StyleBoxFlat CreateReadingSurface() =>
        new()
        {
            BgColor = new Color(0.09f, 0.13f, 0.16f, 0.98f),
            BorderColor = new Color(0.78f, 0.64f, 0.32f, 1f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
        };
}
