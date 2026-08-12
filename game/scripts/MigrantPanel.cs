#nullable enable
using System.Collections.Generic;
using System.Linq;
using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Presentation;
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
    // Tall enough for the citizen detail block, which since DEC-0013 carries the
    // cube axes and combat nature. ResizeToViewport clamps this down on small
    // viewports, so a taller preference never overflows the screen.
    private static readonly Vector2 PreferredSize = new(600, 620);
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

    /// <summary>A12: <c>internal</c> and gated on the harness.</summary>
    internal void ShowForVisualRegression()
    {
        if (!WorldofGoses.Testing.VisualRegressionHarness.IsActive) return;
        RosterSnapshot roster = _controller.GetRosterSnapshot();
        if (roster.HeroId.HasValue && roster.CitizenCount < 2)
        {
            _controller.TryAcceptPendingProspect();
        }
        Open();
        // Select the founder so the capture covers the detail block. Without a
        // selection the panel only renders its empty-roster hint, which is what
        // let the DEC-0013 crash in DescribeCitizen ship unseen.
        if (roster.HeroId.HasValue) SelectCitizen(roster.HeroId.Value);
    }

    /// <summary>
    /// Selects the migrant whose cube the <c>DEC-0019</c> capture exists to
    /// show. The world is built by
    /// <c>CityPrototype.ShowMigrantCubeForVisualRegression</c>, which fails
    /// loudly if it cannot produce one.
    ///
    /// There is deliberately no fallback to the founder. The founder's cube is
    /// shaped by the onboarding and a generated citizen's by their id; a
    /// capture that quietly swapped one for the other is what let the first
    /// version of this fixture photograph a bare vertex and read as proof.
    /// </summary>
    /// <summary>A12: <c>internal</c> and gated on the harness.</summary>
    internal void ShowMigrantCubeForVisualRegression(CitizenId migrantId)
    {
        if (!WorldofGoses.Testing.VisualRegressionHarness.IsActive) return;
        Open();
        RosterSnapshot roster = _controller.GetRosterSnapshot();
        RosterSnapshot.RosterEntry? entry = null;
        foreach (var candidate in roster.Entries)
        {
            if (candidate.Id == migrantId)
            {
                entry = candidate;
                break;
            }
        }
        if (entry is null || entry.IsHero)
        {
            GD.PushError(
                $"Migrant cube fixture expected a non-hero citizen {migrantId.Value}; " +
                "refusing to photograph a substitute.");
            return;
        }
        SelectCitizen(entry.Id);
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
        if (!_controller.GetRosterSnapshot().HeroId.HasValue)
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

        RosterSnapshot roster = _controller.GetRosterSnapshot();
        var entriesById = new Dictionary<CitizenId, RosterSnapshot.RosterEntry>();
        foreach (var entry in roster.Entries) entriesById[entry.Id] = entry;

        if (!_selectedCitizenId.HasValue
            || !entriesById.ContainsKey(_selectedCitizenId.Value))
        {
            _selectedCitizenId = roster.HeroId;
        }

        int migrantCount = 0;
        foreach (RosterSnapshot.RosterEntry entry in roster.Entries)
        {
            if (!entry.IsHero) migrantCount++;
            var button = new Button
            {
                Text = DescribeRosterRow(entry),
                Alignment = HorizontalAlignment.Left,
                CustomMinimumSize = new Vector2(0, 44),
                ThemeTypeVariation = _selectedCitizenId == entry.Id
                    ? "HudButtonSelected"
                    : "HudButton",
            };
            CitizenId id = entry.Id;
            button.Pressed += () => SelectCitizen(id);
            _citizenList.AddChild(button);
            _citizenButtons[id] = button;
        }

        bool hasHero = roster.HeroId.HasValue;
        _recruitButton.Disabled = !hasHero || roster.IsHousingFull;
        _recruitButton.TooltipText = !hasHero
            ? UiText.Get("Create a hero first.")
            : roster.IsHousingFull
                ? UiText.Get("ui.citizens.housing_full")
                : UiText.Get("ui.citizens.recruit_available");
        _statusLabel.Text = UiText.Format(
            "ui.citizens.count_with_housing",
            roster.CitizenCount,
            migrantCount,
            roster.HousingCapacity);
        RosterSnapshot.RosterEntry? selected = _selectedCitizenId.HasValue
            && entriesById.TryGetValue(_selectedCitizenId.Value, out var found)
            ? found
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

    private string DescribeRosterRow(RosterSnapshot.RosterEntry citizen)
    {
        string role = UiText.Get(citizen.IsHero ? "Hero" : "Citizen");
        return UiText.Format("ui.citizens.roster_row", citizen.Name, role, DescribeStatus(citizen));
    }

    private string DescribeCitizen(RosterSnapshot.RosterEntry citizen)
    {
        string assignment = citizen.CurrentAssignment.HasValue
            ? ResolveAssignmentName(citizen.CurrentAssignment.Value)
            : UiText.Get("None");
        string description = UiText.Format(
            "ui.citizens.detail",
            citizen.Name,
            DescribeStatus(citizen),
            assignment,
            UiText.Get(ProfileCatalog.Get(citizen.Lineage).DisplayName),
            CitizenNatureText.FormatLocalized(
                citizen.CubeProfile,
                citizen.Lineage,
                citizen.CombatNature),
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

    private string DescribeStatus(RosterSnapshot.RosterEntry citizen)
    {
        if (citizen.IsOnActiveExpedition)
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
        RosterSnapshot roster = _controller.GetRosterSnapshot();
        if (roster.BuildingDisplayNames.TryGetValue(assignmentId, out string? buildingName))
        {
            return UiText.Get(buildingName!);
        }
        if (roster.ProjectDisplayNames.TryGetValue(assignmentId, out string? projectName))
        {
            return UiText.Format("ui.citizens.construction_assignment", UiText.Get(projectName!));
        }
        return UiText.Get("Unknown");
    }

}
