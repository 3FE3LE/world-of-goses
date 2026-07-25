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
    [Export] public NodePath CitizenListPath { get; set; } = "Surface/Margin/Layout/CitizenList/Rows";
    [Export] public NodePath DetailLabelPath { get; set; } = "Surface/Margin/Layout/DetailLabel";

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
            _controller.TryRecruitMigrant();
        }
        Open();
    }

    public void Close()
    {
        _modalHost.Close();
    }

    private void ApplyResponsiveBounds()
    {
        Vector2 parentSize = GetParentOrNull<Control>()?.Size ?? GetViewportRect().Size;
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
            Notifier.ShowError("Create a hero first.");
            return;
        }
        CityWorld.MigrantResult result = _controller.TryRecruitMigrant();
        if (!result.IsSuccess)
        {
            Notifier.ShowError($"Could not recruit: {result.Outcome}");
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

        _recruitButton.Disabled = _controller.World.Hero is null;
        _statusLabel.Text =
            $"{_controller.World.Citizens.Count} citizens · {migrantCount} non-hero";
        Citizen? selected = _selectedCitizenId.HasValue
            ? _controller.World.GetCitizen(_selectedCitizenId.Value)
            : null;
        _detailLabel.Text = selected is null
            ? "Recruit the first citizen to begin the roster."
            : DescribeCitizen(selected);
    }

    private void SelectCitizen(CitizenId id)
    {
        _selectedCitizenId = id;
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
        string role = citizen.IsHero ? "Hero" : "Citizen";
        return $"{citizen.Name} · {role} · {DescribeStatus(citizen)}";
    }

    private string DescribeCitizen(Citizen citizen)
    {
        string assignment = citizen.CurrentAssignment.HasValue
            ? ResolveAssignmentName(citizen.CurrentAssignment.Value)
            : "None";
        return
            $"{citizen.Name}\n" +
            $"Status: {DescribeStatus(citizen)}\n" +
            $"Assignment: {assignment}\n" +
            $"Lineage: {ProfileCatalog.Get(citizen.Profile.Lineage).DisplayName}\n" +
            $"Affinities: {DescribeAffinities(citizen.Profile)}\n" +
            $"Stamina: {citizen.CurrentStamina}/{citizen.MaxStamina}\n\n" +
            "Open a Farm, Quarry, or construction site to assign an available citizen.";
    }

    private static string DescribeAffinities(CitizenProfile profile) =>
        string.Join(", ",
            ProfileCatalog.DisplayName(profile.ProfessionalAffinities[0]),
            ProfileCatalog.DisplayName(profile.ProfessionalAffinities[1]),
            ProfileCatalog.DisplayName(profile.ProfessionalAffinities[2]));

    private string DescribeStatus(Citizen citizen)
    {
        if (_controller.World.IsCitizenOnActiveExpedition(citizen.Id))
        {
            return "On expedition";
        }
        if (citizen.CurrentAssignment.HasValue)
        {
            return citizen.CurrentLocation == CitizenLocation.AtWork
                ? "Working"
                : "Assigned";
        }
        return citizen.CurrentLocation == CitizenLocation.AtHome
            ? "At home"
            : citizen.CurrentLocation.ToString();
    }

    private string ResolveAssignmentName(BuildingId assignmentId)
    {
        Building? building = _controller.World.GetBuilding(assignmentId);
        if (building is not null) return building.DisplayName;
        ConstructionProject? project = _controller.World.GetProject(assignmentId);
        return project is not null ? $"{project.DisplayName} (construction)" : "Unknown";
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
