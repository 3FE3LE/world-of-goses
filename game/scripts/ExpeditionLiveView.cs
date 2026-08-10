#nullable enable
using System;
using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Ui;

namespace WorldofGoses;

/// <summary>
/// Structural lateral expedition perspective hosted by GameUiShell/ScreenContent.
/// It projects the active expedition and owns no combat or simulation clock.
/// </summary>
public partial class ExpeditionLiveView : Control
{
    // Reference-derived regions on the fixed 1280x720 canvas. The rendered
    // CityStatusPanel currently consumes the first 56 logical pixels, leaving
    // a 1280x664 ScreenContent; it is not repeated inside this view.
    internal static readonly Rect2I StageBounds = new(244, 0, 800, 488);
    internal static readonly Rect2I RouteBounds = new(360, 8, 560, 92);
    internal static readonly Rect2I LeftColumnBounds = new(8, 8, 228, 464);
    internal static readonly Rect2I SquadBounds = new(8, 480, 441, 176);
    internal static readonly Rect2I RightColumnBounds = new(1048, 8, 224, 464);
    internal static readonly Rect2I SkillBounds = new(448, 472, 456, 180);
    internal static readonly Rect2I CommandBounds = new(1048, 472, 224, 180);

    [Export] public NodePath ControllerPath { get; set; } = new("../../../CityWorldController");

    private CityWorldController _controller = null!;
    private LocaleManager _localeManager = null!;
    private ExpeditionStage _stage = null!;
    private PanelContainer _routeStrip = null!;
    private VBoxContainer _leftColumn = null!;
    private VBoxContainer _squadArea = null!;
    private VBoxContainer _rightColumn = null!;
    private VBoxContainer _combatCommands = null!;
    private HBoxContainer _routeSteps = null!;
    private Label _viewTitle = null!;
    private Label _expeditionHeader = null!;
    private Label _citizenHeader = null!;
    private Label _squadHeader = null!;
    private Label _encounterHeader = null!;
    private Label _expeditionName = null!;
    private Label _phase = null!;
    private Label _citizenName = null!;
    private Label _citizenHealth = null!;
    private Label _citizenStamina = null!;
    private Label _citizenCondition = null!;
    private Label _encounterName = null!;
    private Label _threat = null!;
    private Label _objective = null!;
    private Label _enemies = null!;
    private ProgressBar _progress = null!;
    private Label _progressText = null!;
    private ExpeditionSquadStrip _squadStrip = null!;
    private ExpeditionSkillStrip _skillStrip = null!;
    private Button _backButton = null!;
    private Button _autoButton = null!;
    private Button _retreatButton = null!;
    private bool _fixtureShowsTwoEnemies;
    private bool _updatingCombatControls;

    private static readonly StringName[] SkillActions =
    {
        "expedition_skill_1",
        "expedition_skill_2",
        "expedition_skill_3",
        "expedition_skill_4",
    };

    public ExpeditionId? PresentedExpeditionId { get; private set; }
    public Button BackButton => _backButton;
    public Button AutoButton => _autoButton;

    public override void _Ready()
    {
        _controller = GetNode<CityWorldController>(ControllerPath);
        _localeManager = GetNode<LocaleManager>("/root/LocaleManager");
        _stage = GetNode<ExpeditionStage>("ExpeditionStage");
        _routeStrip = GetNode<PanelContainer>("ExpeditionRouteStrip");
        _leftColumn = GetNode<VBoxContainer>("ExpeditionHud/LeftColumn");
        _squadArea = GetNode<VBoxContainer>("ExpeditionHud/SquadArea");
        _rightColumn = GetNode<VBoxContainer>("ExpeditionHud/RightColumn");
        _combatCommands = GetNode<VBoxContainer>("ExpeditionHud/CombatCommands");
        _routeSteps = GetNode<HBoxContainer>("ExpeditionRouteStrip/Content/Layout/RouteSteps");
        _viewTitle = GetNode<Label>("ExpeditionRouteStrip/Content/Layout/Header/ViewTitle");
        _expeditionHeader = GetNode<Label>("ExpeditionHud/LeftColumn/ExpeditionSummary/Content/Rows/Header");
        _citizenHeader = GetNode<Label>("ExpeditionHud/LeftColumn/CitizenDetail/Content/Rows/Header");
        _squadHeader = GetNode<Label>("ExpeditionHud/SquadArea/SquadHeader");
        _encounterHeader = GetNode<Label>("ExpeditionHud/RightColumn/EncounterSummary/Content/Rows/Header");
        _expeditionName = GetNode<Label>("ExpeditionHud/LeftColumn/ExpeditionSummary/Content/Rows/Name");
        _phase = GetNode<Label>("ExpeditionHud/LeftColumn/ExpeditionSummary/Content/Rows/Phase");
        _citizenName = GetNode<Label>("ExpeditionHud/LeftColumn/CitizenDetail/Content/Rows/Name");
        _citizenHealth = GetNode<Label>("ExpeditionHud/LeftColumn/CitizenDetail/Content/Rows/Health");
        _citizenStamina = GetNode<Label>("ExpeditionHud/LeftColumn/CitizenDetail/Content/Rows/Stamina");
        _citizenCondition = GetNode<Label>("ExpeditionHud/LeftColumn/CitizenDetail/Content/Rows/Condition");
        _encounterName = GetNode<Label>("ExpeditionHud/RightColumn/EncounterSummary/Content/Rows/Name");
        _threat = GetNode<Label>("ExpeditionHud/RightColumn/EncounterSummary/Content/Rows/Threat");
        _objective = GetNode<Label>("ExpeditionHud/RightColumn/EncounterSummary/Content/Rows/Objective");
        _enemies = GetNode<Label>("ExpeditionHud/RightColumn/EncounterSummary/Content/Rows/Enemies");
        _progress = GetNode<ProgressBar>("ExpeditionHud/RightColumn/EncounterSummary/Content/Rows/Progress");
        _progressText = GetNode<Label>("ExpeditionHud/RightColumn/EncounterSummary/Content/Rows/ProgressText");
        _squadStrip = GetNode<ExpeditionSquadStrip>("ExpeditionHud/SquadArea/ExpeditionSquadStrip");
        _skillStrip = GetNode<ExpeditionSkillStrip>("ExpeditionHud/ExpeditionSkillStrip");
        _autoButton = GetNode<Button>("ExpeditionHud/CombatCommands/AutoButton");
        _retreatButton = GetNode<Button>("ExpeditionHud/CombatCommands/RetreatButton");

        ApplyReferenceLayout();

        HBoxContainer header = GetNode<HBoxContainer>("ExpeditionRouteStrip/Content/Layout/Header");
        _backButton = StandardButtons.BackToCityButton();
        _backButton.Name = "BackToCityButton";
        _backButton.ThemeTypeVariation = "HudButton";
        _backButton.Pressed += ReturnToCity;
        header.AddChild(_backButton);
        header.MoveChild(_backButton, 0);

        _autoButton.Disabled = true;
        _autoButton.ToggleMode = true;
        _autoButton.ButtonPressed = true;
        _autoButton.Toggled += OnAutoToggled;
        foreach (OctagonalSkillSlot slot in _skillStrip.Slots)
        {
            slot.Activated += OnSkillActivated;
        }
        _retreatButton.Disabled = true;

        _controller.SelectionChanged += OnSelectionChanged;
        _controller.ExpeditionStateChanged += OnExpeditionStateChanged;
        _controller.WorldTickAdvanced += OnWorldTickAdvanced;
        _localeManager.LocaleChanged += OnLocaleChanged;

        Hide();
        ApplySelection(_controller.CurrentSelection);
    }

    internal bool HasReferenceLayout =>
        MatchesBounds(_stage, StageBounds)
        && MatchesBounds(_routeStrip, RouteBounds)
        && MatchesBounds(_leftColumn, LeftColumnBounds)
        && MatchesBounds(_squadArea, SquadBounds)
        && MatchesBounds(_rightColumn, RightColumnBounds)
        && MatchesBounds(_skillStrip, SkillBounds)
        && MatchesBounds(_combatCommands, CommandBounds);

    internal string ReferenceLayoutReport =>
        $"stage={ActualBounds(_stage)}, route={ActualBounds(_routeStrip)}, "
        + $"left={ActualBounds(_leftColumn)}, squad={ActualBounds(_squadArea)}, "
        + $"right={ActualBounds(_rightColumn)}, skills={ActualBounds(_skillStrip)}, "
        + $"commands={ActualBounds(_combatCommands)}";

    private void ApplyReferenceLayout()
    {
        ApplyBounds(_stage, StageBounds);
        ApplyBounds(_routeStrip, RouteBounds);
        ApplyBounds(_leftColumn, LeftColumnBounds);
        ApplyBounds(_squadArea, SquadBounds);
        ApplyBounds(_rightColumn, RightColumnBounds);
        ApplyBounds(_skillStrip, SkillBounds);
        ApplyBounds(_combatCommands, CommandBounds);
        _stage.QueueRedraw();
    }

    private static void ApplyBounds(Control control, Rect2I bounds)
    {
        control.SetAnchorsAndOffsetsPreset(LayoutPreset.TopLeft);
        control.Position = bounds.Position;
        control.Size = bounds.Size;
    }

    private static bool MatchesBounds(Control control, Rect2I bounds) =>
        control.Position.IsEqualApprox(bounds.Position)
        && control.Size.IsEqualApprox(bounds.Size);

    private static Rect2I ActualBounds(Control control) => new(
        Mathf.RoundToInt(control.Position.X),
        Mathf.RoundToInt(control.Position.Y),
        Mathf.RoundToInt(control.Size.X),
        Mathf.RoundToInt(control.Size.Y));

    public override void _ExitTree()
    {
        if (_backButton is not null) _backButton.Pressed -= ReturnToCity;
        if (_autoButton is not null) _autoButton.Toggled -= OnAutoToggled;
        if (_skillStrip is not null)
        {
            foreach (OctagonalSkillSlot slot in _skillStrip.Slots)
            {
                slot.Activated -= OnSkillActivated;
            }
        }
        if (_controller is not null)
        {
            _controller.SelectionChanged -= OnSelectionChanged;
            _controller.ExpeditionStateChanged -= OnExpeditionStateChanged;
            _controller.WorldTickAdvanced -= OnWorldTickAdvanced;
        }
        if (_localeManager is not null) _localeManager.LocaleChanged -= OnLocaleChanged;
    }

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (!Visible || PresentedExpeditionId is null) return;
        for (int index = 0; index < SkillActions.Length; index++)
        {
            if (!inputEvent.IsActionPressed(SkillActions[index])) continue;
            TryActivateSkill(index);
            GetViewport().SetInputAsHandled();
            return;
        }
    }

    internal void ShowEarlyFixture()
    {
        if (System.Environment.GetEnvironmentVariable("WOG_VISUAL_CAPTURE") != "1") return;
        _fixtureShowsTwoEnemies = true;
        if (Visible) Refresh();
    }

    private void OnSelectionChanged(int selectionState) =>
        ApplySelection((CityWorldController.Selection)selectionState);

    private void ApplySelection(CityWorldController.Selection selection)
    {
        if (selection != CityWorldController.Selection.ExpeditionLive)
        {
            Hide();
            PresentedExpeditionId = null;
            return;
        }

        Show();
        Refresh();
        _backButton.GrabFocus();
    }

    private void OnExpeditionStateChanged(int _) => RefreshIfVisible();
    private void OnWorldTickAdvanced(int _) => RefreshIfVisible();
    private void OnLocaleChanged(string _) => RefreshIfVisible();
    private void RefreshIfVisible()
    {
        if (Visible) Refresh();
    }

    private void Refresh()
    {
        ExpeditionId? selectedId = _controller.CurrentExpeditionLiveId;
        ExpeditionLiveSnapshot? snapshot = selectedId is ExpeditionId id
            ? _controller.GetExpeditionLiveSnapshot(id)
            : null;
        if (snapshot is null)
        {
            PresentedExpeditionId = null;
            if (_controller.CurrentSelection == CityWorldController.Selection.ExpeditionLive)
            {
                _controller.ReturnToCity();
            }
            return;
        }

        PresentedExpeditionId = snapshot.Id;
        ApplyLocalizedChrome();
        _expeditionName.Text = UiText.Get(snapshot.DisplayName);
        _phase.Text = UiText.Format(
            "ui.expedition_live.phase",
            ExpeditionCompactCard.PhaseText(snapshot.Phase));

        ConfigureRoute(snapshot.RouteSteps);
        ConfigureCitizen(snapshot);
        ConfigureSquad(snapshot);
        ConfigureEncounter(snapshot);
        ConfigureCombatControls(snapshot);
        int enemyCount = snapshot.CombatState?.EnemyCount
            ?? (_fixtureShowsTwoEnemies ? 2 : 0);
        _stage.Configure(snapshot.Members.Count, enemyCount);

        _autoButton.Text = UiText.Get("ui.expedition_live.auto");
        _retreatButton.Text = UiText.Get("ui.expedition_live.retreat");
        _retreatButton.TooltipText = UiText.Get("ui.expedition_live.retreat.unavailable_tooltip");
    }

    private void ConfigureRoute(
        System.Collections.Generic.IReadOnlyList<ExpeditionLiveSnapshot.RouteStepState> states)
    {
        string[] keys =
        {
            "ui.expedition_live.route.origin",
            "ui.expedition_live.route.outbound",
            "ui.expedition_live.route.threat",
            "ui.expedition_live.route.objective",
            "ui.expedition_live.route.return",
        };
        for (int i = 0; i < keys.Length; i++)
        {
            Label label = _routeSteps.GetNode<Label>($"Step{i}");
            ExpeditionLiveSnapshot.RouteStepState state = states[i];
            string marker = state switch
            {
                ExpeditionLiveSnapshot.RouteStepState.Complete => "[✓]",
                ExpeditionLiveSnapshot.RouteStepState.Active => "[>]",
                ExpeditionLiveSnapshot.RouteStepState.Skipped => "[—]",
                _ => "[ ]",
            };
            label.Text = $"{marker} {UiText.Get(keys[i])}";
            label.ThemeTypeVariation = state == ExpeditionLiveSnapshot.RouteStepState.Active
                ? "HudHeader"
                : "HudCaption";
        }
    }

    private void ApplyLocalizedChrome()
    {
        _viewTitle.Text = UiText.Get("ui.expedition_live.title");
        _expeditionHeader.Text = UiText.Get("ui.expedition_live.expedition_header");
        _citizenHeader.Text = UiText.Get("ui.expedition_live.citizen_header");
        _squadHeader.Text = UiText.Get("ui.expedition_live.squad_header");
        _encounterHeader.Text = UiText.Get("ui.expedition_live.encounter_header");
    }

    private void ConfigureCitizen(ExpeditionLiveSnapshot snapshot)
    {
        ExpeditionLiveSnapshot.Member? lead = snapshot.Members.Count > 0
            ? snapshot.Members[0]
            : null;
        if (lead is null)
        {
            _citizenName.Text = UiText.Get("ui.expedition_live.unknown");
            _citizenHealth.Text = UiText.Get("ui.expedition_live.health.unknown");
            _citizenStamina.Text = UiText.Get("ui.expedition_live.stamina.unknown");
            _citizenCondition.Text = UiText.Get("ui.expedition_live.condition.none");
            return;
        }

        _citizenName.Text = _fixtureShowsTwoEnemies
            ? UiText.Get("ui.expedition_live.founder_short")
            : lead.Name;
        _citizenHealth.Text = lead.HealthRatio is double healthRatio
            ? UiText.Format("ui.expedition_live.health", Mathf.RoundToInt(healthRatio * 100))
            : UiText.Get("ui.expedition_live.health.unknown");
        _citizenStamina.Text = UiText.Format(
            "ui.expedition_live.stamina",
            lead.CurrentStamina,
            lead.EffectiveMaxStamina);
        _citizenCondition.Text = lead.WoundSeverity is WoundSeverity severity
            ? UiText.Format("ui.expedition_live.condition.wounded", WoundText(severity))
            : UiText.Get("ui.expedition_live.condition.none");
    }

    private void ConfigureSquad(ExpeditionLiveSnapshot snapshot)
    {
        Texture2D? portrait = ResourceLoader.Load<Texture2D>(IconPaths.User);
        for (int i = 0; i < 4; i++)
        {
            if (i < snapshot.Members.Count)
            {
                ExpeditionLiveSnapshot.Member member = snapshot.Members[i];
                double staminaRatio = member.EffectiveMaxStamina <= 0
                    ? 0
                    : member.CurrentStamina / (double)member.EffectiveMaxStamina;
                _squadStrip.ConfigureSlot(
                    i,
                    ExpeditionSquadSlot.SlotState.Active,
                    portrait,
                    _fixtureShowsTwoEnemies && i == 0
                        ? UiText.Get("ui.expedition_live.founder_short")
                        : member.Name,
                    member.HealthRatio,
                    UiText.Get("ui.expedition_live.stamina.short"),
                    staminaRatio,
                    member.WoundSeverity is WoundSeverity severity ? WoundText(severity) : null);
            }
            else
            {
                _squadStrip.ConfigureSlot(i, ExpeditionSquadSlot.SlotState.Locked);
            }
        }
    }

    private void ConfigureEncounter(ExpeditionLiveSnapshot snapshot)
    {
        _encounterName.Text = snapshot.EncounterOutcome is ExpeditionEncounterOutcome outcome
            ? UiText.Get(EncounterOutcomeKey(outcome))
            : snapshot.Phase == ExpeditionPhase.Encounter
                ? UiText.Get("ui.expedition_live.encounter.current")
                : UiText.Get("ui.expedition_live.encounter.none");
        _threat.Text = UiText.Get("ui.expedition_live.threat.unmeasured");
        _objective.Text = UiText.Get(ObjectiveKey(snapshot.ObjectiveKind));
        _enemies.Text = snapshot.CombatState is { } combat
            ? UiText.Format("ui.expedition_live.enemies", combat.EnemyCount)
            : _fixtureShowsTwoEnemies
                ? UiText.Format("ui.expedition_live.enemies", 2)
                : UiText.Get("ui.expedition_live.enemies.unknown");
        _progress.Value = snapshot.Progress;
        _progressText.Text = UiText.Format(
            "ui.expedition_live.progress",
            Mathf.RoundToInt(snapshot.Progress * 100));
    }

    private void ConfigureCombatControls(ExpeditionLiveSnapshot snapshot)
    {
        ExpeditionLiveSnapshot.Combat? combat = snapshot.CombatState;
        _updatingCombatControls = true;
        _autoButton.Disabled = combat?.Active != true;
        _autoButton.ButtonPressed = combat?.AutoSkillsEnabled ?? true;
        _autoButton.TooltipText = UiText.Get(
            combat is null
                ? "ui.expedition_live.auto.fixture_tooltip"
                : "ui.expedition_live.auto");
        _updatingCombatControls = false;

        if (combat is null) return;
        Texture2D? icon = ResourceLoader.Load<Texture2D>(IconPaths.Fire);
        for (int index = 0; index < 4; index++)
        {
            ExpeditionLiveSnapshot.Skill skill = combat.Skills[index];
            OctagonalSkillSlot.SlotState state = skill.Locked
                ? OctagonalSkillSlot.SlotState.Locked
                : !combat.Active
                    ? OctagonalSkillSlot.SlotState.Disabled
                    : skill.Ready
                        ? OctagonalSkillSlot.SlotState.Ready
                        : OctagonalSkillSlot.SlotState.Cooldown;
            _skillStrip.ConfigureSlot(
                index,
                state,
                skill.Locked ? null : icon,
                skill.Remaining,
                skill.Duration);
        }
    }

    private void OnAutoToggled(bool enabled)
    {
        if (_updatingCombatControls || PresentedExpeditionId is not ExpeditionId id) return;
        _controller.SetCombatAutoSkillsEnabled(id, enabled);
        RefreshIfVisible();
    }

    private void OnSkillActivated(int slotNumber) => TryActivateSkill(slotNumber - 1);

    private void TryActivateSkill(int slotIndex)
    {
        if (PresentedExpeditionId is not ExpeditionId id) return;
        _controller.TryActivateMemberSkill(id, slotIndex);
        RefreshIfVisible();
    }

    private void ReturnToCity() => _controller.ReturnToCity();

    private static string ObjectiveKey(ResourceOpportunityKind? kind) => kind switch
    {
        ResourceOpportunityKind.SpiritTrailSearch => "ui.expedition_live.objective.spirit_trail",
        ResourceOpportunityKind.NearbyFoodForage => "ui.expedition_live.objective.food",
        ResourceOpportunityKind.FallenWoodSearch => "ui.expedition_live.objective.wood",
        _ => "ui.expedition_live.objective.reconnaissance",
    };

    private static string EncounterOutcomeKey(ExpeditionEncounterOutcome outcome) => outcome switch
    {
        ExpeditionEncounterOutcome.FullSuccess => "event.encounter_outcome.full_success",
        ExpeditionEncounterOutcome.PartialSuccess => "event.encounter_outcome.partial_success",
        _ => "event.encounter_outcome.setback",
    };

    private static string WoundText(WoundSeverity severity) => UiText.Get(
        severity == WoundSeverity.Severe ? "ui.wound.severe" : "ui.wound.moderate");
}
