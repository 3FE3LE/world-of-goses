#nullable enable
using System.Collections.Generic;
using System.Linq;
using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Ui;

namespace WorldofGoses;

/// <summary>
/// Expedition preparation panel: the player picks a 1-2 citizen team from
/// the real roster (docs/FIRST_PLAYABLE_LOOP_AUDIT.md §G3) before
/// dispatching a reconnaissance or a prospect-seeking expedition. The panel
/// reuses <see cref="ModalHost"/> for scrim/close semantics.
/// </summary>
[GlobalClass]
public partial class ExpeditionPanel : Control
{
    private static readonly Vector2 PreferredSize = new(520, 380);
    private const float ViewportInset = 32f;

    [Export] public NodePath ControllerPath { get; set; } = "../../../../CityWorldController";
    [Export] public NodePath ModalHostPath { get; set; } = "../ModalHost";
    [Export] public NodePath StatusLabelPath { get; set; } = "Surface/Margin/Layout/StatusLabel";
    [Export] public NodePath TeamHeaderPath { get; set; } = "Surface/Margin/Layout/TeamHeader";
    [Export] public NodePath TeamScrollPath { get; set; } = "Surface/Margin/Layout/TeamScroll";
    [Export] public NodePath TeamListPath { get; set; } = "Surface/Margin/Layout/TeamScroll/TeamList";
    [Export] public NodePath RetreatHeaderPath { get; set; } = "Surface/Margin/Layout/RetreatHeader";
    [Export] public NodePath RetreatPosturePath { get; set; } = "Surface/Margin/Layout/RetreatPosture";
    [Export] public NodePath DispatchButtonPath { get; set; } = "Surface/Margin/Layout/DispatchButton";
    [Export] public NodePath CancelButtonPath { get; set; } = "Surface/Margin/Layout/CancelButton";
    [Export] public NodePath ProspectButtonPath { get; set; } = "Surface/Margin/Layout/ProspectButton";
    [Export] public NodePath CloseButtonPath { get; set; } = "Surface/Margin/Layout/CloseButton";

    private CityWorldController _controller = null!;
    private ModalHost _modalHost = null!;
    private Label _statusLabel = null!;
    private Label _teamHeader = null!;
    private Control _teamScroll = null!;
    private VBoxContainer _teamList = null!;
    private Label _retreatHeader = null!;
    private OptionButton _retreatPosture = null!;
    private Button _dispatchButton = null!;
    private Button _cancelButton = null!;
    private Button _prospectButton = null!;
    private Button _closeButton = null!;

    private readonly List<CitizenId> _selectedMemberIds = new();
    private bool _hasAppliedDefaultSelection;
    private bool _showRecoveryFixture;

    public override void _Ready()
    {
        OverlayLayers.Apply(this, OverlayLayers.Modal);

        _controller = GetNode<CityWorldController>(ControllerPath);
        _modalHost = GetNode<ModalHost>(ModalHostPath);
        _statusLabel = GetNode<Label>(StatusLabelPath);
        _teamHeader = GetNode<Label>(TeamHeaderPath);
        _teamScroll = GetNode<Control>(TeamScrollPath);
        _teamList = GetNode<VBoxContainer>(TeamListPath);
        _retreatHeader = GetNode<Label>(RetreatHeaderPath);
        _retreatPosture = GetNode<OptionButton>(RetreatPosturePath);
        _dispatchButton = GetNode<Button>(DispatchButtonPath);
        _cancelButton = GetNode<Button>(CancelButtonPath);
        _prospectButton = GetNode<Button>(ProspectButtonPath);
        _closeButton = GetNode<Button>(CloseButtonPath);

        _retreatHeader.Text = UiText.Get("ui.expedition.posture_label");
        _retreatPosture.AddItem(
            UiText.Get("ui.expedition.posture.continue"),
            (int)ExpeditionRetreatPosture.ContinueAfterSetback);
        _retreatPosture.AddItem(
            UiText.Get("ui.expedition.posture.retreat"),
            (int)ExpeditionRetreatPosture.RetreatAfterSetback);
        _retreatPosture.Select(1);
        _retreatPosture.TooltipText = UiText.Get("ui.expedition.posture_hint");
        _cancelButton.Text = UiText.Get("ui.expedition.cancel_dispatch");
        _cancelButton.TooltipText = UiText.Get("ui.expedition.cancel_dispatch_hint");

        _dispatchButton.Pressed += OnDispatchPressed;
        _cancelButton.Pressed += OnCancelPressed;
        _prospectButton.Pressed += OnProspectPressed;
        _closeButton.Pressed += OnClosePressed;
        _controller.ExpeditionStateChanged += OnExpeditionStateChanged;
        _controller.CitizensChanged += OnCitizensChanged;
        _controller.BuildingStateChanged += _ => Refresh();
        _controller.WorldTickAdvanced += _ => Refresh();
        GetViewport().SizeChanged += ApplyResponsiveBounds;

        Hide();
        CallDeferred(MethodName.ApplyResponsiveBounds);
    }

    public override void _ExitTree()
    {
        if (_controller is not null)
        {
            _controller.ExpeditionStateChanged -= OnExpeditionStateChanged;
            _controller.CitizensChanged -= OnCitizensChanged;
        }
        GetViewport().SizeChanged -= ApplyResponsiveBounds;
    }

    public void Open()
    {
        Show();
        _modalHost.Open(this);
        Refresh();
        FocusCurrentAction();
    }

    public void Close()
    {
        _modalHost.Close();
    }

    public void ShowWoundRecoveryForVisualRegression()
    {
        _showRecoveryFixture = true;
        Open();
    }

    private void ApplyResponsiveBounds()
    {
        Vector2 parentSize = GetParentOrNull<Control>()?.Size ?? GetViewportRect().Size;
        Vector2 size = new(
            Mathf.Max(320f, Mathf.Min(PreferredSize.X, parentSize.X - ViewportInset * 2f)),
            Mathf.Max(240f, Mathf.Min(PreferredSize.Y, parentSize.Y - ViewportInset * 2f)));
        CustomMinimumSize = Vector2.Zero;
        SetAnchorsPreset(LayoutPreset.Center);
        OffsetLeft = -Mathf.Round(size.X * 0.5f);
        OffsetTop = -Mathf.Round(size.Y * 0.5f);
        OffsetRight = Mathf.Round(size.X * 0.5f);
        OffsetBottom = Mathf.Round(size.Y * 0.5f);
    }

    private void OnDispatchPressed()
    {
        if (_selectedMemberIds.Count == 0) return;
        var request = ExpeditionRequest.Reconnaissance(
            _selectedMemberIds.ToArray(),
            SelectedRetreatPosture());
        ExpeditionStartResult result = _controller.StartExpedition(request);
        if (!result.IsSuccess)
        {
            Notifier.ShowError(UiText.Format("ui.expedition.dispatch_failed", result.Outcome));
        }
        else
        {
            _selectedMemberIds.Clear();
            _hasAppliedDefaultSelection = false;
        }
        Refresh();
    }

    private void OnCancelPressed()
    {
        ExpeditionId? active = null;
        foreach (Expedition expedition in _controller.World.Expeditions.Values)
        {
            if (expedition.Status == ExpeditionStatus.Active)
            {
                active = expedition.Id;
                break;
            }
        }
        if (active.HasValue)
        {
            _controller.CancelExpedition(active.Value);
        }
        Refresh();
    }

    private void OnProspectPressed()
    {
        if (_selectedMemberIds.Count == 0) return;
        ExpeditionStartResult result = _controller.StartExpedition(
            ExpeditionRequest.SeekProspect(
                _selectedMemberIds.ToArray(),
                SelectedRetreatPosture()));
        if (!result.IsSuccess)
        {
            Notifier.ShowError(UiText.Format("ui.expedition.dispatch_failed", result.Outcome));
        }
        else
        {
            _selectedMemberIds.Clear();
            _hasAppliedDefaultSelection = false;
        }
        Refresh();
    }

    private void OnClosePressed() => Close();

    private void OnExpeditionStateChanged(int _) => Refresh();

    private void OnCitizensChanged() => Refresh();

    private void Refresh()
    {
        CityWorld world = _controller.World;
        Expedition? active = null;
        foreach (Expedition expedition in world.Expeditions.Values)
        {
            if (!_showRecoveryFixture && expedition.Status == ExpeditionStatus.Active)
            {
                active = expedition;
                break;
            }
        }

        // Selection only matters while there is no active expedition to
        // prepare a new one against; keep it clean of citizens who became
        // unavailable since the last refresh (e.g. assigned elsewhere by
        // another panel, or an expedition that just returned them home
        // recovering).
        _selectedMemberIds.RemoveAll(id => world.GetCitizen(id) is not { CanJoinExpedition: true });
        if (!_hasAppliedDefaultSelection && active is null)
        {
            _hasAppliedDefaultSelection = true;
            if (_selectedMemberIds.Count == 0 && world.Hero is { CanJoinExpedition: true } hero)
            {
                _selectedMemberIds.Add(hero.Id);
            }
        }

        bool showTeamPicker = active is null;
        _teamHeader.Visible = showTeamPicker;
        _teamScroll.Visible = showTeamPicker;
        _retreatHeader.Visible = showTeamPicker;
        _retreatPosture.Visible = showTeamPicker;
        if (showTeamPicker) PopulateTeamList(world);

        bool canDispatch = active is null && _selectedMemberIds.Count > 0;
        _dispatchButton.Disabled = !canDispatch;
        bool hasTownHall = world.Buildings.Values.Any(building => building.Kind == BuildingKind.TownHall);
        _prospectButton.Disabled = !canDispatch
            || !hasTownHall
            || world.PendingProspect is not null;
        _prospectButton.TooltipText = UiText.Get(!hasTownHall
            ? "ui.expedition.town_hall_required"
            : world.PendingProspect is not null
                ? "ui.expedition.prospect_waiting"
                : "ui.expedition.seek_prospect_hint");
        _cancelButton.Visible = active is not null
            && active.Phase == ExpeditionPhase.Outbound
            && world.CurrentTick == active.StartTick;
        string territoryStatus = DescribeTerritory(world);
        _statusLabel.Text = (active is null
            ? UiText.Get("ui.expedition.team_hint")
            : UiText.Format(
                "ui.expedition.schedule_with_team",
                UiText.Get(active.DisplayName),
                DescribeTeam(world, active),
                SimulationTimeText.FormatLocalized(active.StartTick),
                SimulationTimeText.FormatLocalized(active.EndTick))
                + "\n" + UiText.Format(
                    "ui.expedition.active_posture",
                    UiText.Get(active.RetreatPosture switch
                    {
                        ExpeditionRetreatPosture.RetreatAfterSetback =>
                            "ui.expedition.posture.retreat",
                        _ => "ui.expedition.posture.continue",
                    }))
                + "\n" + DescribePhase(active))
            + "\n" + territoryStatus;
    }

    private static string DescribePhase(Expedition expedition)
    {
        string phaseText = UiText.Get(expedition.Phase switch
        {
            ExpeditionPhase.Outbound => "ui.expedition.phase.outbound",
            ExpeditionPhase.Encounter => "ui.expedition.phase.encounter",
            ExpeditionPhase.Objective => "ui.expedition.phase.objective",
            ExpeditionPhase.Retreating => "ui.expedition.phase.retreating",
            ExpeditionPhase.Returning => "ui.expedition.phase.returning",
            _ => "ui.expedition.phase.resolved",
        });
        if (expedition.EncounterOutcome is not { } outcome) return phaseText;
        string outcomeText = UiText.Get(outcome switch
        {
            ExpeditionEncounterOutcome.FullSuccess => "event.encounter_outcome.full_success",
            ExpeditionEncounterOutcome.PartialSuccess => "event.encounter_outcome.partial_success",
            _ => "event.encounter_outcome.setback",
        });
        return UiText.Format("ui.expedition.phase_with_outcome", phaseText, outcomeText);
    }

    private ExpeditionRetreatPosture SelectedRetreatPosture() =>
        (ExpeditionRetreatPosture)_retreatPosture.GetSelectedId();

    private void PopulateTeamList(CityWorld world)
    {
        foreach (Node child in _teamList.GetChildren())
        {
            _teamList.RemoveChild(child);
            child.QueueFree();
        }

        bool atCapacity = _selectedMemberIds.Count >= ExpeditionRequest.MaxTeamSize;
        foreach (Citizen citizen in world.Citizens.Values)
        {
            if (_showRecoveryFixture && citizen.Wound is null) continue;

            if (!citizen.IsHero)
            {
                Button incorporateButton = StandardButtons.TextAction(
                    UiText.Format("ui.expedition.incorporate_hero_action", citizen.Name),
                    UiText.Get("ui.expedition.incorporate_hero_hint"));
                incorporateButton.Alignment = HorizontalAlignment.Left;
                incorporateButton.CustomMinimumSize = new Vector2(0, 40);
                CitizenId citizenId = citizen.Id;
                incorporateButton.Pressed += () => OnIncorporateHeroPressed(citizenId);
                _teamList.AddChild(incorporateButton);
                continue;
            }

            if (citizen.Wound is { } wound)
            {
                Button condition = StandardButtons.TextAction(
                    UiText.Format(
                        "ui.expedition.wounded_member",
                        citizen.Name,
                        UiText.Get(wound.Severity == WoundSeverity.Severe
                            ? "ui.wound.severe"
                            : "ui.wound.moderate"),
                        SimulationTimeText.FormatDurationLocalized(
                            wound.RecoveryTicksRemaining)),
                    UiText.Get("ui.expedition.wounded_member_hint"));
                condition.Alignment = HorizontalAlignment.Left;
                condition.CustomMinimumSize = new Vector2(0, 40);
                condition.Disabled = true;
                _teamList.AddChild(condition);
                if (citizen.Commitment.Kind != CitizenCommitmentKind.Recovery)
                {
                    int foodCost = WoundRules.FoodCostFor(wound.Severity);
                    Button treatment = StandardButtons.TextAction(
                        UiText.Format(
                            "ui.expedition.begin_treatment",
                            citizen.Name,
                            foodCost),
                        UiText.Get("ui.expedition.begin_treatment_hint"));
                    CitizenId woundedId = citizen.Id;
                    treatment.Pressed += () => OnBeginTreatmentPressed(woundedId);
                    _teamList.AddChild(treatment);
                }
                continue;
            }

            bool isSelected = _selectedMemberIds.Contains(citizen.Id);
            bool canToggleOn = citizen.CanJoinExpedition && (isSelected || !atCapacity);
            Button button = StandardButtons.TextAction(
                citizen.Name,
                DescribeExpeditionEligibility(world, citizen));
            button.Alignment = HorizontalAlignment.Left;
            button.CustomMinimumSize = new Vector2(0, 40);
            button.ToggleMode = true;
            button.ButtonPressed = isSelected;
            button.Disabled = !canToggleOn;
            button.ThemeTypeVariation = isSelected ? "ButtonPrimary" : "ButtonText";
            CitizenId heroId = citizen.Id;
            button.Toggled += pressed => OnTeamMemberToggled(heroId, pressed);
            _teamList.AddChild(button);
        }
    }

    private void OnIncorporateHeroPressed(CitizenId citizenId)
    {
        HeroIncorporationResult result = _controller.TryIncorporateHero(citizenId);
        if (!result.IsSuccess)
        {
            Notifier.ShowError(
                UiText.Format("ui.expedition.incorporation_failed", result.Outcome));
        }
        Refresh();
    }

    private void OnBeginTreatmentPressed(CitizenId citizenId)
    {
        WoundRecoveryResult result = _controller.TryBeginWoundRecovery(citizenId);
        if (!result.IsSuccess)
        {
            Notifier.ShowError(
                UiText.Format("ui.expedition.treatment_failed", result.Outcome));
        }
        Refresh();
    }

    private void OnTeamMemberToggled(CitizenId citizenId, bool pressed)
    {
        if (pressed)
        {
            if (!_selectedMemberIds.Contains(citizenId)
                && _selectedMemberIds.Count < ExpeditionRequest.MaxTeamSize)
            {
                _selectedMemberIds.Add(citizenId);
            }
        }
        else
        {
            _selectedMemberIds.Remove(citizenId);
        }
        Refresh();
    }

    private static string DescribeTeam(CityWorld world, Expedition expedition) =>
        string.Join(", ", expedition.MemberIds.Select(id => world.GetCitizen(id)?.Name ?? "?"));

    private static string DescribeTerritory(CityWorld world)
    {
        CityParcel? target = world.NextTerritoryTarget;
        return target is null
            ? UiText.Get("ui.expedition.territory_available")
            : UiText.Format(
                "ui.expedition.territory_target",
                target.Id.Value,
                UiText.Get(target.TerritoryState switch
                {
                    ParcelTerritoryState.Reconnoitred => "ui.territory.reconnoitred",
                    ParcelTerritoryState.RouteSecured => "ui.territory.route_secured",
                    ParcelTerritoryState.Available => "ui.territory.available",
                    _ => "ui.territory.locked",
                }));
    }

    private static string DescribeExpeditionEligibility(CityWorld world, Citizen citizen)
    {
        if (!citizen.CanJoinExpedition)
        {
            return DescribeUnavailability(world, citizen);
        }
        return citizen.Commitment.Kind is CitizenCommitmentKind.BuildingWork
                or CitizenCommitmentKind.Construction
            ? UiText.Get("ui.expedition.team_member_interrupts_work")
            : UiText.Get("ui.expedition.team_member_hint");
    }

    private static string DescribeUnavailability(CityWorld world, Citizen citizen) =>
        citizen.AvailabilityReason switch
        {
            CitizenAvailabilityReason.AssignedToBuilding =>
                UiText.Format("ui.assignment.reason_building", ResolveLocationName(world, citizen)),
            CitizenAvailabilityReason.AssignedToConstruction =>
                UiText.Format("ui.assignment.reason_construction", ResolveLocationName(world, citizen)),
            CitizenAvailabilityReason.OnExpedition => UiText.Get("ui.assignment.reason_expedition"),
            CitizenAvailabilityReason.Recovering => UiText.Get("ui.assignment.reason_recovering"),
            CitizenAvailabilityReason.Wounded => UiText.Get("ui.assignment.reason_wounded"),
            _ => UiText.Get("Available"),
        };

    private static string ResolveLocationName(CityWorld world, Citizen citizen)
    {
        if (citizen.Commitment.EntityId is not int entityId) return UiText.Get("Unknown");
        var buildingId = new BuildingId(entityId);
        return world.GetBuilding(buildingId)?.DisplayName
            ?? world.GetProject(buildingId)?.DisplayName
            ?? UiText.Get("Unknown");
    }

    private void FocusCurrentAction()
    {
        Button target = _cancelButton.Visible && !_cancelButton.Disabled
            ? _cancelButton
            : !_dispatchButton.Disabled
                ? _dispatchButton
                : !_prospectButton.Disabled
                    ? _prospectButton
                    : _closeButton;
        target.GrabFocus();
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
