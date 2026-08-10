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
    [Export] public NodePath ControllerPath { get; set; } = new("../../../CityWorldController");

    private CityWorldController _controller = null!;
    private LocaleManager _localeManager = null!;
    private ExpeditionStage _stage = null!;
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

    public ExpeditionId? PresentedExpeditionId { get; private set; }
    public Button BackButton => _backButton;

    public override void _Ready()
    {
        _controller = GetNode<CityWorldController>(ControllerPath);
        _localeManager = GetNode<LocaleManager>("/root/LocaleManager");
        _stage = GetNode<ExpeditionStage>("ExpeditionStage");
        _routeSteps = GetNode<HBoxContainer>("ExpeditionRouteStrip/Content/Layout/RouteSteps");
        _viewTitle = GetNode<Label>("ExpeditionRouteStrip/Content/Layout/Header/ViewTitle");
        _expeditionHeader = GetNode<Label>("ExpeditionHud/LeftColumn/ExpeditionSummary/Content/Rows/Header");
        _citizenHeader = GetNode<Label>("ExpeditionHud/LeftColumn/CitizenDetail/Content/Rows/Header");
        _squadHeader = GetNode<Label>("ExpeditionHud/LeftColumn/SquadHeader");
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
        _squadStrip = GetNode<ExpeditionSquadStrip>("ExpeditionHud/LeftColumn/ExpeditionSquadStrip");
        _skillStrip = GetNode<ExpeditionSkillStrip>("ExpeditionHud/ExpeditionSkillStrip");
        _autoButton = GetNode<Button>("ExpeditionHud/CombatCommands/Content/Actions/AutoButton");
        _retreatButton = GetNode<Button>("ExpeditionHud/CombatCommands/Content/Actions/RetreatButton");

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
        _retreatButton.Disabled = true;

        _controller.SelectionChanged += OnSelectionChanged;
        _controller.ExpeditionStateChanged += OnExpeditionStateChanged;
        _controller.WorldTickAdvanced += OnWorldTickAdvanced;
        _localeManager.LocaleChanged += OnLocaleChanged;

        Hide();
        ApplySelection(_controller.CurrentSelection);
    }

    public override void _ExitTree()
    {
        if (_backButton is not null) _backButton.Pressed -= ReturnToCity;
        if (_controller is not null)
        {
            _controller.SelectionChanged -= OnSelectionChanged;
            _controller.ExpeditionStateChanged -= OnExpeditionStateChanged;
            _controller.WorldTickAdvanced -= OnWorldTickAdvanced;
        }
        if (_localeManager is not null) _localeManager.LocaleChanged -= OnLocaleChanged;
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
        _stage.Configure(snapshot.Members.Count, _fixtureShowsTwoEnemies ? 2 : 0);

        _autoButton.Text = UiText.Get("ui.expedition_live.auto");
        _autoButton.TooltipText = UiText.Get("ui.expedition_live.auto.fixture_tooltip");
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
        _encounterName.Text = snapshot.Phase == ExpeditionPhase.Encounter
            ? UiText.Get("ui.expedition_live.encounter.current")
            : UiText.Get("ui.expedition_live.encounter.none");
        _threat.Text = UiText.Get("ui.expedition_live.threat.unmeasured");
        _objective.Text = UiText.Get(ObjectiveKey(snapshot.ObjectiveKind));
        _enemies.Text = _fixtureShowsTwoEnemies
            ? UiText.Format("ui.expedition_live.enemies", 2)
            : UiText.Get("ui.expedition_live.enemies.unknown");
        _progress.Value = snapshot.Progress;
        _progressText.Text = UiText.Format(
            "ui.expedition_live.progress",
            Mathf.RoundToInt(snapshot.Progress * 100));
    }

    private void ReturnToCity() => _controller.ReturnToCity();

    private static string ObjectiveKey(ResourceOpportunityKind? kind) => kind switch
    {
        ResourceOpportunityKind.SpiritTrailSearch => "ui.expedition_live.objective.spirit_trail",
        ResourceOpportunityKind.NearbyFoodForage => "ui.expedition_live.objective.food",
        ResourceOpportunityKind.FallenWoodSearch => "ui.expedition_live.objective.wood",
        _ => "ui.expedition_live.objective.reconnaissance",
    };

    private static string WoundText(WoundSeverity severity) => UiText.Get(
        severity == WoundSeverity.Severe ? "ui.wound.severe" : "ui.wound.moderate");
}
