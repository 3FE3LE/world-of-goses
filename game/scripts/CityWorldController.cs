#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Domain.Combat;
using WorldofGoses.Persistence;
using WorldofGoses.Testing;

namespace WorldofGoses;

/// <summary>
/// Godot adapter for a single <see cref="CityGameSession"/>. Translates
/// engine signals (input, lifecycle, frames) into use-case calls and
/// surfaces application outcomes as Godot signals.
///
/// <para>Architecture Hardening A8 closes the last remaining presentation
/// ownership of <see cref="CityWorld"/>: this class no longer holds a
/// <c>_world</c> field, exposes no <c>World</c> getter, and runs no
/// persistence logic of its own. Every gameplay command, snapshot query,
/// world-tick advancement, save, load and reset reaches the
/// <see cref="CityGameSession"/> that owns the aggregate. The seam left
/// here is the engine-specific one: <c>_Process</c> cadence, signal
/// emission, frame-time sampling, autosave gating, and the visual
/// interpolation phase.</para>
///
/// <para>Auto-saves the world on a timer (in <see cref="_Process"/>) and
/// on window close (in <see cref="_Notification"/>). No manual save
/// button — the game philosophy forbids progress loss.</para>
///
/// <para>Load priority: primary slot → current v34 world; an absent or
/// retired slot starts the hero onboarding flow without creating
/// production data.</para>
/// </summary>
public partial class CityWorldController : Node
{
    private const string VisualCaptureEnvironmentVariable = "WOG_VISUAL_CAPTURE";
    private const string VisualCaptureCommandLineArgument = "--wog-visual-capture";
    // S-1.7 frame-time sampling lives in
    // game/scripts/Testing/VisualRegressionProfiler.cs since the
    // A12 move (issue #8); the controller owns no profiling code.
    private bool _onboardingCompletionPending;
    private bool _suppressPersistenceWrites;
    private Selection _currentSelection = Selection.MacroView;
    private ExpeditionId? _currentExpeditionLiveId;

    [Signal]
    public delegate void SelectionChangedEventHandler(int selectionState);

    [Signal]
    public delegate void BuildingStateChangedEventHandler(int buildingId);

    /// <summary>
    /// Fired by <see cref="SelectBuilding"/> together with
    /// <see cref="SelectionChanged"/> so the detail view can open
    /// directly on the right building. <see cref="SelectionChanged"/>
    /// alone carries only the enum value and would leave the detail
    /// view with no way to know which building to show.
    /// </summary>
    [Signal]
    public delegate void BuildingSelectedEventHandler(int buildingId);

    [Signal]
    public delegate void CitizenAssignmentRejectedEventHandler(int reason);

    /// <summary>
    /// Fires once per world tick (after
    /// <see cref="CityGameSession.AdvanceWorldTick"/>).
    /// Used by UI listeners that need to reflect time-of-day or
    /// cumulative counters that change every tick, not only when
    /// production happens.
    /// </summary>
    [Signal]
    public delegate void WorldTickAdvancedEventHandler(int tick);

    [Signal]
    public delegate void HeroCreatedEventHandler(int citizenId);

    [Signal]
    public delegate void ProjectStateChangedEventHandler(int projectId);

    [Signal]
    public delegate void NaturalResourceStateChangedEventHandler(int patchId);

    [Signal]
    public delegate void CultivationSiteStateChangedEventHandler(int siteId);

    [Signal]
    public delegate void ExpeditionStateChangedEventHandler(int expeditionId);

    [Signal]
    public delegate void CitizensChangedEventHandler();

    [Signal]
    public delegate void ObservedCitizenChangedEventHandler(int citizenId);

    /// <summary>
    /// Fired after a successful auto-save. Carries the wall-clock
    /// unix timestamp in milliseconds so the status panel can render
    /// the moment the world was protected.
    /// </summary>
    [Signal]
    public delegate void WorldSavedEventHandler(long unixMillis);

    /// <summary>
    /// Emitted when the player changes the simulation speed. The
    /// status panel listens for this so the chip highlights the
    /// active rate.
    /// </summary>
    [Signal]
    public delegate void SimulationSpeedChangedEventHandler(int speedChoice);

    /// <summary>
    /// Fires whenever the authored first night changes stage: the
    /// spirit arriving, the campfire or shelter being finished, the
    /// night concluding. The signal carries the new stage as an
    /// <c>int</c> so the presentation layer can subscribe without
    /// taking a dependency on <see cref="Domain.FirstNightStage"/>.
    /// </summary>
    [Signal]
    public delegate void FirstNightStageChangedEventHandler(int stage);

    // Architecture Hardening A8: the session owns CityWorld. The
    // controller holds a single reference to it and routes every
    // command, query and persistence call through it. The previous
    // `private readonly CityWorld _world = new();` and
    // `internal CityWorld World => _world;` are gone.
    private readonly CityGameSession _session;

    /// <summary>
    /// Default parameterless constructor required by Godot's source
    /// generator for instantiating the node. Constructs the application
    /// session over a fresh <see cref="CityWorld"/> so every later call
    /// has a single, deterministic facade in front of the domain.
    /// </summary>
    public CityWorldController()
    {
        _session = new CityGameSession();
    }

    /// <summary>
    /// Seconds between periodic auto-saves during gameplay. Tunable.
    /// </summary>
    public double AutoSaveIntervalSeconds { get; set; } =
        SimulationPersistencePolicy.AutoSaveInterval.TotalSeconds;

    private double _autoSaveTimer;
    private double _simulationTimer;
    private CitizenId? _observedCitizenId;
    private int _lastObservedFirstNightStage = -1;

    public double SimulationTickIntervalSeconds { get; set; } = 1.0;

    /// <summary>
    /// How far the renderer currently is into the world tick in progress, in
    /// <c>[0, 1)</c>. Presentation timing, not world state: it lets a view
    /// interpolate between one-second ticks without inventing a second clock,
    /// so a walk paced by domain ticks still moves every frame.
    ///
    /// <para>
    /// Nothing in the domain reads this, and no decision may depend on it — it
    /// only smooths what is already decided.
    /// </para>
    /// </summary>
    public double CurrentTickPhase =>
        SimulationTickIntervalSeconds > 0
            ? Math.Clamp(_simulationTimer / SimulationTickIntervalSeconds, 0.0, 1.0)
            : 0.0;

    /// <summary>
    /// Discrete speed choices the player can pick from the status
    /// panel. The numeric value is the multiplier applied to the
    /// default tick interval (1.0 s). The world always runs; the
    /// player can speed it up but cannot pause it (the city keeps
    /// advancing while the game is closed, per the design bible's
    /// persistence chapter).
    /// </summary>
    public enum SpeedChoice
    {
        Normal = 1,
        Fast = 2,
        Fastest = 4,
    }

    private SpeedChoice _speed = SpeedChoice.Normal;

    public SpeedChoice CurrentSpeed => _speed;

    public CitizenId? ObservedCitizenId
    {
        get
        {
            CitizenId? selected = _observedCitizenId;
            if (selected.HasValue && _session.TryGetCitizenDisplayName(selected.Value, out _))
            {
                return selected;
            }
            return _session.HeroId;
        }
    }

    /// <summary>
    /// Switches the simulation speed and adjusts
    /// <see cref="SimulationTickIntervalSeconds"/> accordingly. The
    /// world always runs; the player can only speed it up. The change
    /// is broadcast via <see cref="SimulationSpeedChanged"/> so
    /// listeners can refresh.
    /// </summary>
    public void SetSimulationSpeed(SpeedChoice speed)
    {
        if (speed is not SpeedChoice.Normal
            and not SpeedChoice.Fast
            and not SpeedChoice.Fastest)
        {
            throw new ArgumentOutOfRangeException(nameof(speed), speed, "Unsupported simulation speed.");
        }

        _speed = speed;
        SimulationTickIntervalSeconds = 1.0 / (int)speed;
        EmitSignal(SignalName.SimulationSpeedChanged, (int)speed);
    }

    public enum Selection
    {
        MacroView = 0,
        BuildingDetail = 1,
        HeroProfile = 2,
        ExpeditionLive = 3,
    }

    /// <summary>
    /// The aggregate facade. No public or internal <c>World</c> getter
    /// remains on the controller — A8 moves ownership into the
    /// <see cref="CityGameSession"/>, and the only remaining reach for
    /// presentation goes through the
    /// <see cref="TryGetHeroForFounderArrival"/> narrow seam, which
    /// itself is consumed by the <c>AstralOnboardingView</c> animation
    /// placeholder still on the A1 allowlist.
    /// </summary>

    public OfflineProgressionReport? LastOfflineReport { get; private set; }

    /// <summary>
    /// The current top-level view selection (macro, building detail,
    /// hero profile, or expedition live perspective). Updated by the selection transition methods and
    /// exposed so input handlers (notably the pause menu's ESC
    /// handler) can branch on whether the macro view is active without
    /// subscribing to the SelectionChanged signal.
    /// </summary>
    public Selection CurrentSelection => _currentSelection;

    /// <summary>
    /// Active expedition observed by the lateral presentation. This is
    /// ephemeral navigation state and is never persisted or simulated.
    /// </summary>
    public ExpeditionId? CurrentExpeditionLiveId => _currentExpeditionLiveId;

    /// <summary>
    /// Toggle for the persistence layer. Enabled by default now that
    /// saves are validated before restore; tests or editor experiments
    /// can disable it to keep the world entirely in memory.
    /// </summary>
    public bool PersistenceEnabled { get; set; } = true;

    private bool PersistenceWritesEnabled =>
        PersistenceEnabled
        && !_suppressPersistenceWrites
        && !Array.Exists(
            OS.GetCmdlineUserArgs(),
            argument => string.Equals(
                argument,
                VisualCaptureCommandLineArgument,
                StringComparison.Ordinal))
        && !string.Equals(
            System.Environment.GetEnvironmentVariable(VisualCaptureEnvironmentVariable),
            "1",
            StringComparison.Ordinal);

    public override void _Ready()
    {
        if (PersistenceEnabled) TryLoadFromDisk();
        // Hook the session's forwarded events for change signals. A8
        // routes every mutation through the session, so subscribing
        // here gives the same coverage as the old
        // `_world.BuildingChanged += …` path without re-introducing a
        // controller-level World field.
        _session.BuildingChanged += OnDomainBuildingChanged;
        _session.ProjectChanged += OnDomainProjectChanged;
        _session.PatchChanged += OnDomainPatchChanged;
        _session.CultivationSiteChanged += OnDomainCultivationSiteChanged;
        _session.ExpeditionChanged += OnDomainExpeditionChanged;
        if (_session.HasHero)
        {
            LineageThemeRegistry.ActiveLineage =
                LineageThemeRegistry.IdOf(_session.HeroLineageId!.Value);
        }
    }

    public override void _Process(double delta)
    {
        // Frame-time sampling runs on VisualRegressionProfiler (issue #8),
        // not here. The controller owns no profiling code.
        if (_session.NeedsOnboarding || _onboardingCompletionPending) return;

        AdvanceLiveSimulation(delta);

        if (!PersistenceWritesEnabled) return;
        if (AutoSaveIntervalSeconds <= 0) return;
        _autoSaveTimer += delta;
        if (_autoSaveTimer >= AutoSaveIntervalSeconds)
        {
            _autoSaveTimer = 0;
            TryAutoSave();
        }
    }

    private void AdvanceLiveSimulation(double delta)
    {
        if (SimulationTickIntervalSeconds <= 0) return;

        _simulationTimer += delta;
        int ticksDue = (int)(_simulationTimer / SimulationTickIntervalSeconds);
        if (ticksDue <= 0) return;

        _simulationTimer -= ticksDue * SimulationTickIntervalSeconds;
        for (int i = 0; i < ticksDue; i++)
        {
            _session.AdvanceWorldTick();
            EmitSignal(SignalName.WorldTickAdvanced, _session.CurrentTick);
        }
        EmitFirstNightStageIfChanged();
    }

    public override void _ExitTree()
    {
        _session.BuildingChanged -= OnDomainBuildingChanged;
        _session.ProjectChanged -= OnDomainProjectChanged;
        _session.PatchChanged -= OnDomainPatchChanged;
        _session.CultivationSiteChanged -= OnDomainCultivationSiteChanged;
        _session.ExpeditionChanged -= OnDomainExpeditionChanged;
    }

    public override void _Notification(int what)
    {
        if (!PersistenceWritesEnabled) return;
        if (what == WmCloseRequest)
        {
            SaveBeforeExit();
        }
    }

    /// <summary>
    /// Persists the city on the way out, if there is anything worth
    /// persisting. Both exit routes call this — the window's close button
    /// through <see cref="_Notification"/>, and the pause menu's quit action —
    /// so leaving by either means the same thing.
    /// </summary>
    /// <remarks>
    /// It goes through the same dirty/onboarding gate as the autosave loop
    /// rather than calling <see cref="TrySaveNow"/>. Saving unconditionally
    /// would write an empty city over the slot when a player quits during
    /// onboarding, which is the one moment there is no city to keep.
    /// </remarks>
    public void SaveBeforeExit() => TryAutoSave();

    private const int WmCloseRequest = 1006;

    public void SaveNow()
    {
        if (!PersistenceWritesEnabled) return;
        TrySaveNow();
    }

    /// <summary>
    /// A10: the public <c>DrainAllForestsForVisualRegression</c> and
    /// <c>AdvanceWorldTickForVisualRegression</c> entry points lived
    /// here so any external screenshot tooling could call them by
    /// name. The harness now reaches the same operations through the
    /// <c>internal</c> fixture seam
    /// (<see cref="DrainAllForestsForFixture"/> and
    /// <see cref="AdvanceWorldTickForFixture"/>) and gates them on
    /// <see cref="VisualRegressionHarness.IsActive"/>. The public
    /// entries are removed so production APIs do not grow to
    /// accommodate screenshots.
    /// </summary>

    /// <summary>
    /// A10 fixture seam: drains every natural resource patch. The
    /// visual-regression harness is the only legitimate caller.
    /// </summary>
    internal void DrainAllForestsForFixture()
    {
        if (!VisualRegressionHarness.IsActive) return;
        _session.World.DrainAllNaturalResourcesForFixtures();
        // Reflect the new state through the existing signals so the
        // macro view re-renders without an autosave side-effect.
        EmitSignal(SignalName.WorldTickAdvanced, _session.CurrentTick);
    }

    /// <summary>
    /// A10 fixture seam: advances the world by one tick from the
    /// harness. Replaces the previous public
    /// <c>AdvanceWorldTickForVisualRegression</c> entry point.
    /// </summary>
    internal void AdvanceWorldTickForFixtureHarness()
    {
        if (!VisualRegressionHarness.IsActive) return;
        _session.AdvanceWorldTick();
        EmitSignal(SignalName.WorldTickAdvanced, _session.CurrentTick);
    }

    /// <summary>
    /// Fixture command: replaces the live world with the contents of
    /// <paramref name="fixture"/>. Used by <c>CityPrototype</c>'s dev-only
    /// scene builders, which need to seed a deterministic city without
    /// reaching into a presentation-owned aggregate.
    /// <c>internal</c> so Presentation cannot bypass the boundary for
    /// ordinary gameplay; only fixtures and the test seam reach it.
    /// </summary>
    internal void SeedFixtureWorld(CityWorld fixture)
    {
        WorldPersistence.ApplyTo(_session.World, WorldPersistence.Capture(fixture));
        _session.MarkDirty();
    }

    /// <summary>
    /// Fixture command: replaces the live world with an already-prepared
    /// <see cref="WorldSave"/>. Used by fixtures that mutate the save
    /// (e.g. <c>AddTerrariumRowsForVisualRegression</c>) before restoring.
    /// </summary>
    internal void RestoreFixtureWorld(WorldSave save)
    {
        WorldPersistence.ApplyTo(_session.World, save);
        _session.MarkDirty();
    }

    /// <summary>
    /// Fixture command: advances the world by one tick. Same effect as
    /// <see cref="AdvanceWorldTickForVisualRegression"/> but unconditional,
    /// since the fixtures that use it run their own gating.
    /// </summary>
    internal void AdvanceWorldTickForFixture()
    {
        _session.World.AdvanceWorldTick();
        _session.MarkDirty();
        EmitSignal(SignalName.WorldTickAdvanced, _session.CurrentTick);
    }

    /// <summary>
    /// Fixture command: records a wound event on the world log and
    /// applies the wound to the named citizen. Replaces direct
    /// <c>controller.World.Log.Record(...)</c> +
    /// <c>patient.SustainWound(...)</c> calls in the fixtures.
    /// </summary>
    internal void RecordFixtureWoundEvent(CitizenId id, WoundSeverity severity)
    {
        Citizen? citizen = _session.World.GetCitizen(id);
        if (citizen is null) return;
        WorldEvent woundEvent = _session.World.Log.Record(
            _session.World.CurrentTick,
            WorldEventKind.WoundSustained,
            WorldEventSubject.Citizen(citizen.Id, citizen.Name),
            (int)severity);
        citizen.SustainWound(severity, woundEvent.Id);
        _session.MarkDirty();
    }

    /// <summary>
    /// Fixture command: fast-forwards a construction project's progress
    /// by the given amount of work. Negative or excessive values are clamped.
    /// </summary>
    internal void SeedProjectProgressForFixture(BuildingId projectId, int work)
    {
        ConstructionProject? project = _session.World.GetProject(projectId);
        if (project is null) return;
        project.SeedProgressForFixture(work);
        _session.MarkDirty();
    }

    /// <summary>
    /// Fixture command: registers a citizen directly into the world.
    /// Bypasses the regular onboarding because the fixture builders
    /// need to seed named, deterministic citizens without driving them
    /// through the UI.
    /// </summary>
    internal void RegisterFixtureCitizen(Citizen citizen)
    {
        _session.World.RegisterCitizen(citizen);
        _session.MarkDirty();
    }

    /// <summary>
    /// Fixture command: returns the next citizen id to allocate for a
    /// fixture citizen.
    /// </summary>
    internal int NextFixtureCitizenId()
    {
        int nextId = 2;
        while (_session.World.GetCitizen(new CitizenId(nextId)) is not null) nextId++;
        return nextId;
    }

    /// <summary>
    /// Fixture command: returns the maximum citizen id currently allocated
    /// plus one (matches the previous <c>citizens.Keys.Max(...)+1</c>
    /// pattern used by the fixtures).
    /// </summary>
    internal int NextFixtureCitizenIdByMax()
    {
        int max = 1;
        foreach (CitizenId id in _session.World.Citizens.Keys)
        {
            if (id.Value > max) max = id.Value;
        }
        return max + 1;
    }

    /// <summary>
    /// Fixture command: writes a custom domain event into the world log.
    /// Takes the tick explicitly so the fixture can choose when the
    /// event fires.
    /// </summary>
    internal WorldEvent RecordFixtureLogEvent(
        int tick,
        WorldEventKind kind,
        WorldEventSubject subject,
        int amount,
        WorldEventId? causeEventId = null)
    {
        WorldEvent evt = _session.World.Log.Record(tick, kind, subject, amount, causeEventId);
        _session.MarkDirty();
        return evt;
    }

    /// <summary>
    /// Fixture command: returns the available amount of a resource
    /// without exposing the ledger.
    /// </summary>
    internal int GetFixtureResourceAvailable(ResourceType resource) =>
        _session.World.Resources.Available(resource);

    /// <summary>
    /// Fixture command: deposits a resource into the city inventory
    /// through the ledger. Used by fixture setups that need to seed
    /// inventory without going through a real gathering action.
    /// </summary>
    internal int DepositToFixtureInventory(ResourceType resource, int amount)
    {
        int deposited = _session.World.Resources.DepositToCityInventory(resource, amount);
        if (deposited > 0) _session.MarkDirty();
        return deposited;
    }

    /// <summary>
    /// Fixture command: returns the founding hero's profile, used by
    /// fixture code that needs to clone the founder into a fresh world.
    /// </summary>
    internal CitizenProfile? GetFixtureHeroProfile() => _session.World.Hero?.Profile;

    /// <summary>
    /// Fixture command: returns the founding hero as a live
    /// <see cref="Citizen"/>. Used by fixtures that hand the founder to
    /// a transient animation (e.g. <c>FounderArrivalSequence.Begin</c>).
    /// </summary>
    internal Citizen? GetFixtureHero() => _session.World.Hero;

    /// <summary>
    /// Narrow replacement for the legacy
    /// <c>_controller.World.Hero!</c> read at
    /// <c>AstralOnboardingView</c>. The view still consumes the live
    /// founder to drive the arrival animation; A8 routes the read
    /// through the session and removes the last
    /// <c>controller.World</c> reach.
    /// </summary>
    internal Citizen? TryGetHeroForFounderArrival() => _session.World.Hero;

    /// <summary>
    /// Fixture command: cancels the first active expedition. Returns
    /// <c>true</c> when one was cancelled.
    /// </summary>
    internal bool CancelFirstActiveExpeditionForFixture() =>
        _session.FirstActiveExpeditionId() is ExpeditionId active
        && _session.CancelExpedition(active);

    private static bool IsVisualCaptureMode =>
        string.Equals(
            System.Environment.GetEnvironmentVariable(VisualCaptureEnvironmentVariable),
            "1",
            StringComparison.Ordinal);

    /// <summary>
    /// Persists the live world to the primary slot. The controller owns
    /// the persistence orchestration because the Application assembly
    /// intentionally does not reference the Persistence assembly (A6
    /// rule, enforced by
    /// <c>Layer_DoesNotReferencePersistenceAssembly</c>). The session
    /// owns the world; the controller owns the slot pipeline.
    /// </summary>
    public bool TrySaveNow()
    {
        if (!PersistenceWritesEnabled) return true;
        try
        {
            WorldPersistence.SaveToSlot(_session.World, WorldPersistence.PrimarySaveSlot);
            WriteEarlyGameMetricsReport();
            _session.MarkClean();
            EmitSignal(SignalName.WorldSaved, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            return true;
        }
        catch (Exception ex)
        {
            GD.PushWarning($"Save failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Writes the EG-0 report next to the save so a play session leaves
    /// the calibration data behind without the player doing anything.
    /// Diagnostic only — losing it must never cost the player their city.
    /// </summary>
    private void WriteEarlyGameMetricsReport()
    {
        try
        {
            string path = Path.Combine(
                WorldPersistence.SaveDirectory,
                "eg0-report.txt");
            File.WriteAllText(path, _session.FormatEarlyGameMetricsReport());
        }
        catch (Exception ex)
        {
            GD.PushWarning($"EG-0 report not written: {ex.Message}");
        }
    }

    public bool ResetPrimarySlotAndRestart()
    {
        if (!PersistenceWritesEnabled) return false;
        try
        {
            // ReloadCurrentScene is deferred by Godot. Suppress every write
            // from this old controller before deleting the slot, otherwise an
            // autosave/close notification in the teardown window can recreate
            // the founder that the full reset just removed.
            _suppressPersistenceWrites = true;
            WorldPersistence.DeleteSlot(WorldPersistence.PrimarySaveSlot);
            GetTree().ReloadCurrentScene();
            return true;
        }
        catch (Exception ex)
        {
            _suppressPersistenceWrites = false;
            GD.PushWarning($"Could not reset the primary slot: {ex.Message}");
            return false;
        }
    }

    public bool ResetCityKeepingFounderAndRestart()
    {
        if (!PersistenceWritesEnabled || !_session.HasHero) return false;
        try
        {
            CityWorld restarted = _session.CreateRestartedCityKeepingHero();
            WorldPersistence.ApplyTo(_session.World, WorldPersistence.Capture(restarted));
            if (!TrySaveNow())
            {
                GD.PushWarning("Could not soft-reset the city: the persistence write failed.");
                return false;
            }
            GetTree().ReloadCurrentScene();
            return true;
        }
        catch (Exception ex)
        {
            GD.PushWarning($"Could not soft-reset the city: {ex.Message}");
            return false;
        }
    }

    private void TryAutoSave()
    {
        if (_session.NeedsOnboarding || !_session.IsDirty) return;
        TrySaveNow();
    }

    public bool SelectBuilding(BuildingId buildingId)
    {
        if (_session.GetBuildingDetailSnapshot(buildingId) is null) return false;
        _currentExpeditionLiveId = null;
        _currentSelection = Selection.BuildingDetail;
        EmitSignal(SignalName.SelectionChanged, (int)Selection.BuildingDetail);
        EmitSignal(SignalName.BuildingSelected, buildingId.Value);
        EmitSignal(SignalName.BuildingStateChanged, buildingId.Value);
        return true;
    }

    public void ReturnToCity()
    {
        _currentExpeditionLiveId = null;
        _currentSelection = Selection.MacroView;
        EmitSignal(SignalName.SelectionChanged, (int)Selection.MacroView);
    }

    public bool SelectHero()
    {
        if (!_session.HasHero) return false;
        SelectCitizenForObservation(_session.HeroId!.Value);
        _currentExpeditionLiveId = null;
        _currentSelection = Selection.HeroProfile;
        EmitSignal(SignalName.SelectionChanged, (int)Selection.HeroProfile);
        return true;
    }

    /// <summary>
    /// Opens an existing active expedition as a presentation perspective.
    /// It does not advance time, change speed or create an expedition clock.
    /// </summary>
    public bool SelectExpeditionLive(ExpeditionId expeditionId)
    {
        if (_session.GetExpeditionLiveSnapshot(expeditionId) is null) return false;

        _currentExpeditionLiveId = expeditionId;
        _currentSelection = Selection.ExpeditionLive;
        EmitSignal(SignalName.SelectionChanged, (int)Selection.ExpeditionLive);
        return true;
    }

    /// <summary>
    /// Changes only the presentation's observation target. Selection never
    /// enables camera follow; the macro camera owns that explicit toggle.
    /// </summary>
    public bool SelectCitizenForObservation(CitizenId citizenId)
    {
        if (!_session.TryGetCitizenDisplayName(citizenId, out _)) return false;
        if (_observedCitizenId == citizenId) return true;
        _observedCitizenId = citizenId;
        EmitSignal(SignalName.ObservedCitizenChanged, citizenId.Value);
        return true;
    }

    public bool NeedsOnboarding() =>
        _session.NeedsOnboarding || _onboardingCompletionPending;

    public bool HasHero() => _session.HasHero;

    public HeroCreationResult TryCompleteOnboarding(HeroCreationRequest request)
    {
        if (_onboardingCompletionPending && GetFixtureHero() is Citizen pendingHero)
        {
            if (!TrySaveNow())
            {
                return HeroCreationResult.Fail(HeroCreationOutcome.SaveFailed);
            }
            _onboardingCompletionPending = false;
            LineageThemeRegistry.ActiveLineage =
                LineageThemeRegistry.IdOf(pendingHero.Profile.Lineage);
            EmitSignal(SignalName.HeroCreated, pendingHero.Id.Value);
            return HeroCreationResult.Success(pendingHero.Id);
        }

        var result = _session.CompleteOnboarding(request);
        if (!result.IsSuccess || !result.CitizenId.HasValue) return result;

        _onboardingCompletionPending = true;
        if (!TrySaveNow())
        {
            return HeroCreationResult.Fail(HeroCreationOutcome.SaveFailed);
        }
        _onboardingCompletionPending = false;
        LineageThemeRegistry.ActiveLineage =
            LineageThemeRegistry.IdOf(request.Profile.Lineage);
        EmitSignal(SignalName.HeroCreated, result.CitizenId.Value.Value);
        return result;
    }

    public CityStatusSnapshot GetCityStatusSnapshot() => _session.GetCityStatusSnapshot();

    public ConstructionSnapshot GetConstructionSnapshot() => _session.GetConstructionSnapshot();

    public BuildingDetailSnapshot? GetBuildingDetailSnapshot(BuildingId buildingId) =>
        _session.GetBuildingDetailSnapshot(buildingId);

    public CityMacroSnapshot GetCityMacroSnapshot() => _session.GetCityMacroSnapshot();

    public ConstructionPlacementSnapshot GetConstructionPlacementSnapshot() =>
        _session.GetConstructionPlacementSnapshot();

    public ExpeditionPlanningSnapshot GetExpeditionPlanningSnapshot() =>
        _session.GetExpeditionPlanningSnapshot();

    public ExpeditionRailSnapshot GetExpeditionRailSnapshot() =>
        _session.GetExpeditionRailSnapshot();

    public ExpeditionLiveSnapshot? GetExpeditionLiveSnapshot(ExpeditionId expeditionId) =>
        _session.GetExpeditionLiveSnapshot(expeditionId);

    public bool SetCombatAutoSkillsEnabled(ExpeditionId expeditionId, bool enabled) =>
        _session.SetCombatAutoSkillsEnabled(expeditionId, enabled);

    public bool TryActivateMemberSkill(ExpeditionId expeditionId, int slotIndex) =>
        _session.TryActivateMemberSkill(expeditionId, slotIndex);

    public CityPolicySnapshot GetCityPolicySnapshot() => _session.GetCityPolicySnapshot();

    public CitizenDebugSnapshot? GetCitizenDebugSnapshot(CitizenId citizenId) =>
        _session.GetCitizenDebugSnapshot(citizenId);

    public HeroProfileSnapshot? GetHeroProfileSnapshot() => _session.GetHeroProfileSnapshot();

    /// <summary>The current world tick as projected by <see cref="CityStatusSnapshot"/>.</summary>
    public int CurrentTick => _session.CurrentTick;

    /// <summary>
    /// Tick projected through <see cref="Domain.FirstNightState.DisplayedTick"/>
    /// so the held first-night clock returns its frozen value rather than the
    /// advancing world tick. Mirrors the value
    /// <see cref="CityStatusSnapshot.From"/> projects.
    /// </summary>
    public int? GetDisplayedTick() => _session.GetDisplayedTick();

    /// <summary>The hero's id, or <c>null</c> when no hero exists.</summary>
    public CitizenId? GetHeroId() => _session.HeroId;

    /// <summary>The hero's lineage id, used by <see cref="FirstNightScene"/> to swap the theme palette.</summary>
    public LineageId? GetHeroLineageId() => _session.HeroLineageId;

    /// <summary>The id of the building that grew out of the founding site, or <c>null</c> while it is still a project.</summary>
    public int? GetFoundingSiteBuildingId() => _session.FoundingSiteBuildingId;

    /// <summary>The first completed Home's id, or <c>null</c>.</summary>
    public BuildingId? GetPrimaryHomeId() => _session.PrimaryHomeId;

    /// <summary>Edible food horizon: stored Food plus gathered Wild Food.</summary>
    public int GetFoodStock() => _session.FoodStock;

    /// <summary>True when at least one Cultivation Site exists.</summary>
    public bool HasCultivationSite() => _session.HasCultivationSite;

    /// <summary>True when at least one Town Hall building exists.</summary>
    public bool HasTownHall() => _session.HasTownHall;

    /// <summary>True when the next migrant cannot be housed.</summary>
    public bool IsHousingFull() => _session.IsHousingFull;

    /// <summary>True when a Town Hall is awaiting an accepted prospect.</summary>
    public bool HasPendingProspect() => _session.HasPendingProspect;

    /// <summary>Read-only projection of one Cultivation Site's lifecycle.</summary>
    public CultivationSiteSnapshot? GetCultivationSiteSnapshot(BuildingId siteId) =>
        _session.GetCultivationSiteSnapshot(siteId);

    /// <summary>Read-only projection of a citizen's current routine.</summary>
    public CitizenRoutineSnapshot? GetCitizenRoutineSnapshot(CitizenId id) =>
        _session.GetCitizenRoutineSnapshot(id);

    /// <summary>Bundled projection used by <see cref="Prototypes.MacroStreetLiveView"/>.</summary>
    public MacroStreetLiveViewState GetMacroStreetViewState() =>
        _session.GetMacroStreetViewState();

    /// <summary>Read-only projection of one combat session.</summary>
    public CombatSessionSnapshot? GetCombatSessionSnapshot(ExpeditionId expeditionId) =>
        _session.GetCombatSessionSnapshot(expeditionId);

    /// <summary>Bundled projection used by the expedition planning panel.</summary>
    public ExpeditionPanelState GetExpeditionPanelState() =>
        _session.GetExpeditionPanelState();

    /// <summary>Roster snapshot used by <c>MigrantPanel</c>.</summary>
    public RosterSnapshot GetRosterSnapshot() => _session.GetRosterSnapshot();

    /// <summary>Compact citizen projection used by the macro view for routing state lookups.</summary>
    public MacroCitizenSnapshot? TryGetMacroCitizenSnapshot(CitizenId id) =>
        _session.TryGetMacroCitizenSnapshot(id);

    /// <summary>The id of the building that grew out of the founding site (alias of <see cref="GetFoundingSiteBuildingId"/>).</summary>
    public BuildingId? GetFoundingStorageBuildingId() => _session.FoundingStorageBuildingId;

    /// <summary>Resolves a citizen's display name without exposing the live entity.</summary>
    public bool TryGetCitizenDisplayName(CitizenId id, out string? name) =>
        _session.TryGetCitizenDisplayName(id, out name);

    /// <summary>Resolves a building's display name without exposing the live entity.</summary>
    public bool TryGetBuildingDisplayName(BuildingId id, out string? name) =>
        _session.TryGetBuildingDisplayName(id, out name);

    /// <summary>Resolves a project's display name without exposing the live entity.</summary>
    public bool TryGetProjectDisplayName(BuildingId id, out string? name) =>
        _session.TryGetProjectDisplayName(id, out name);

    /// <summary>Returns the start tick of an expedition (the cancel-button gate compares to this).</summary>
    public int GetExpeditionStartTick(ExpeditionId id) =>
        _session.GetExpeditionStartTick(id);

    /// <summary>Picks the first active expedition in deterministic order. Returns false when none are active.</summary>
    public bool TryGetActiveExpeditionId(out ExpeditionId id) =>
        _session.TryGetActiveExpeditionId(out id);

    /// <summary>Returns the display names of an expedition's members, or null when the expedition is unknown.</summary>
    public bool TryGetExpeditionMemberDisplayNames(ExpeditionId id, out IReadOnlyList<string>? names) =>
        _session.TryGetExpeditionMemberDisplayNames(id, out names);

    /// <summary>Returns the id of the next territory target parcel, or null when none.</summary>
    public bool TryGetNextTerritoryParcelId(out int? parcelId) =>
        _session.TryGetNextTerritoryParcelId(out parcelId);

    /// <summary>True while the authored first night is still running.</summary>
    public bool IsFirstNightActive() => _session.IsFirstNightActive;

    /// <summary>Current authored first-night stage, or null when no night is staged.</summary>
    public FirstNightStage? GetFirstNightStage() => _session.FirstNightStage;

    /// <summary>True when the world has the named Founding Site module active or completed.</summary>
    public bool HasFoundingSiteModule(FoundingSiteModule module) =>
        _session.HasFoundingSiteModule(module);

    /// <summary>True when the world has logged a Spirit Departed event (used by the first-night embers fade).</summary>
    public bool HasSpiritDepartedEvent() => _session.HasSpiritDepartedEvent();

    public int CurrentProductionRate(BuildingId buildingId) =>
        _session.CurrentProductionRate(buildingId);

    public int GatherFromPatch(int patchId, int unitId, int amount) =>
        _session.GatherFromPatch(patchId, unitId, amount);

    public NaturalResourceGatherResult GetNaturalResourceGatherAvailability(
        int patchId,
        int unitId) =>
        _session.GetNaturalResourceGatherAvailability(patchId, unitId);

    public NaturalResourceGatherResult TryGatherFromPatch(
        int patchId,
        int unitId,
        int amount) =>
        _session.TryGatherFromPatch(patchId, unitId, amount);

    public ToolCraftResult TryCraftTool(ToolKind tool) => _session.TryCraftTool(tool);

    public CultivationActionResult TrySowCultivationSite(BuildingId siteId) =>
        _session.TrySowCultivationSite(siteId);

    public CultivationActionResult TryHarvestCultivationSite(BuildingId siteId) =>
        _session.TryHarvestCultivationSite(siteId);

    public AssignmentResult TryAssignCitizen(BuildingId buildingId, CitizenId citizenId) =>
        _session.TryAssignCitizen(buildingId, citizenId);

    public AssignmentResult TryUnassignCitizen(BuildingId buildingId, CitizenId citizenId) =>
        _session.TryUnassignCitizen(buildingId, citizenId);

    public AssignmentResult TryAssignCitizenToProject(BuildingId projectId, CitizenId citizenId) =>
        _session.TryAssignCitizenToProject(projectId, citizenId);

    public AssignmentResult TryUnassignCitizenFromProject(BuildingId projectId, CitizenId citizenId) =>
        _session.TryUnassignCitizenFromProject(projectId, citizenId);

    public ConstructionAuthorizationResult TryAuthorizeBasicShelter() =>
        TryAuthorizeConstruction(ConstructionKind.BasicShelter);

    public ConstructionAuthorizationResult TryAuthorizeConstruction(ConstructionKind kind) =>
        TryAuthorizeConstruction(kind, selectedLot: null);

    public ConstructionAuthorizationResult TryAuthorizeConstruction(
        ConstructionKind kind,
        ConstructionLot? selectedLot)
    {
        var result = _session.TryAuthorizeConstruction(kind, selectedLot);
        if (result.IsSuccess && result.ProjectId.HasValue)
        {
            SaveNow();
        }
        return result;
    }

    public ConstructionAuthorizationResult TryAuthorizeFoundingSiteModule(
        BuildingId projectId,
        FoundingSiteModule module) =>
        _session.TryAuthorizeFoundingSiteModule(projectId, module);

    public int ReturnFoundingCargo() => _session.ReturnFoundingCargo();

    /// <summary>
    /// Opens the main-dialogue node the spirit speaks at the current
    /// stage. Persists the node id on <see cref="Domain.FirstNightState"/>
    /// so a save interrupted mid-line resumes on the same line. The
    /// spirit only speaks at stages that wait on dialogue — see
    /// <see cref="Domain.FirstNightRules.WaitsForDialogue"/> and the
    /// stage map in <c>docs/world-of-goses-design-bible/23_FIRST_NIGHT_AND_FIRE_SPIRIT.md</c>.
    /// </summary>
    public bool TryOpenFirstNightDialogue(string nodeId)
    {
        bool opened = _session.TryOpenFirstNightDialogue(nodeId);
        if (opened) EmitFirstNightStageIfChanged();
        return opened;
    }

    /// <summary>
    /// Closes the current main-dialogue node and advances the night.
    /// The domain guards prevent advancing before its trigger fires
    /// (a module completion, <see cref="Domain.FirstNightRules.WaitsForModule"/>),
    /// so calling this at the wrong moment is a silent no-op.
    /// </summary>
    public bool TryCloseFirstNightDialogue()
    {
        bool advanced = _session.TryCloseFirstNightDialogue();
        if (advanced) EmitFirstNightStageIfChanged();
        return advanced;
    }

    /// <summary>
    /// Emits <see cref="FirstNightStageChanged"/> when the night moved
    /// between the previous observed stage and the current one.
    /// Called after every tick and after every dialogue close so the
    /// presentation layer never misses a transition even when the
    /// stage advances multiple times in a single tick.
    /// </summary>
    private void EmitFirstNightStageIfChanged()
    {
        // Cities with no FirstNightState never had a night and never
        // will — emitting a fake Concluded stage would mislead the
        // presentation layer into thinking the spirit departed.
        if (!_session.HasFirstNight) return;
        int currentStage = (int)_session.FirstNightStage!;
        if (currentStage == _lastObservedFirstNightStage) return;
        _lastObservedFirstNightStage = currentStage;
        EmitSignal(SignalName.FirstNightStageChanged, currentStage);
    }

    internal IReadOnlyList<ConstructionLot> AvailableConstructionLots() =>
        _session.AvailableConstructionLots();

    public void SetProjectEnabled(BuildingId projectId, bool enabled) =>
        _session.SetProjectEnabled(projectId, enabled);

    public bool CancelProject(BuildingId projectId) => _session.CancelProject(projectId);

    internal ConstructionProject? GetProjectForFixture(BuildingId projectId) =>
        _session.World.GetProject(projectId);

    internal Building? GetBuildingForFixture(BuildingId buildingId) =>
        _session.World.GetBuilding(buildingId);

    /// <summary>
    /// Fixture seam into the session's owned world. <c>CityPrototype</c>
    /// captures this reference once per fixture to author the
    /// deterministic city it needs for a screenshot. Production code
    /// never reaches it: the controller's use-case methods and the
    /// snapshot queries cover every gameplay read.
    /// </summary>
    internal CityWorld GetFixtureWorld() => _session.World;

    public int AdvanceProduction(BuildingId buildingId) => _session.AdvanceProduction(buildingId);

    public void ConfigureProductionPolicy(BuildingId buildingId, bool enabled, int minStock, int maxStock, int priority) =>
        _session.ConfigureProductionPolicy(buildingId, enabled, minStock, maxStock, priority);

    public void SetProductionEnabled(BuildingId buildingId, bool enabled) =>
        _session.SetProductionEnabled(buildingId, enabled);

    private void TryLoadFromDisk()
    {
        try
        {
            TryLoadFromPrimarySlot();
        }
        catch (IncompatibleSaveVersionException ex)
        {
            LastOfflineReport = null;
            GD.Print(
                $"Save schema v{ex.FoundVersion} belongs to the retired prototype. " +
                "Starting hero onboarding; the old slot will remain untouched until confirmation.");
        }
        catch (Exception ex)
        {
            LastOfflineReport = null;
            GD.PushWarning(
                $"Primary slot could not be loaded. Starting hero onboarding without overwriting it: {ex.Message}");
        }
    }

    /// <summary>
    /// Reads, migrates, validates and applies the primary slot, then runs
    /// the offline progression pass and (when a migration occurred)
    /// re-persists so the next load is a vN→vN round-trip.
    /// </summary>
    /// <returns><c>true</c> when the slot existed and was applied;
    /// <c>false</c> when the slot is absent.</returns>
    internal bool TryLoadFromPrimarySlot(string? slotsDirectoryOverride = null)
    {
        string slotsDirectory = slotsDirectoryOverride ?? WorldPersistence.SlotsDirectory;
        if (!WorldPersistence.SlotExists(WorldPersistence.PrimarySaveSlot, slotsDirectory))
        {
            LastOfflineReport = null;
            return false;
        }

        var path = Path.Combine(
            slotsDirectory,
            $"save_slot_{WorldPersistence.PrimarySaveSlot}.json");
        var save = WorldPersistence.DeserializeFromJson(File.ReadAllText(path));
        int originalVersion = save.Version;
        save = WorldPersistence.MigrateToCurrent(save);
        bool migrated = save.Version != originalVersion;
        WorldPersistence.Validate(save);
        WorldPersistence.ApplyTo(_session.World, save);
        _session.SeedStartingForests();
        _session.SeedStartingOpportunities();
        _session.World.EnsureFoundingShelterContributor();

        OfflineProgressionReport? report = null;
        bool hadProgression = false;
        if (save.LastSeenAtUnixMillis > 0)
        {
            var now = DateTimeOffset.UtcNow;
            var lastSeenAt = DateTimeOffset.FromUnixTimeMilliseconds(save.LastSeenAtUnixMillis);
            int ticks = OfflineProgression.ComputeTicks(now, lastSeenAt);
            report = OfflineProgression.ApplyAll(_session.World, ticks);
            if (ticks > 0)
            {
                _session.MarkDirty();
                hadProgression = true;
            }
        }
        LastOfflineReport = report;

        if (migrated)
        {
            WorldPersistence.SaveToSlot(_session.World, WorldPersistence.PrimarySaveSlot, slotsDirectory);
            WriteEarlyGameMetricsReport();
            _session.MarkClean();
        }

        if (hadProgression)
        {
            GD.Print(
                $"World loaded from slot {WorldPersistence.PrimarySaveSlot} " +
                $"(tick {_session.CurrentTick}). " +
                $"Offline progression: +{report!.TicksApplied} ticks, " +
                $"+{report.StockAdded} stock, " +
                $"{(int)report.SimulatedTime.TotalSeconds}s simulated.");
        }
        else
        {
            GD.Print(
                $"World loaded from slot {WorldPersistence.PrimarySaveSlot} " +
                $"(tick {_session.CurrentTick}).");
        }

        return true;
    }

    public ExpeditionStartResult StartExpedition(ExpeditionRequest request)
    {
        ExpeditionStartResult result = _session.StartExpedition(request);
        if (result.IsSuccess) SaveNow();
        return result;
    }

    public ExpeditionStartResult StartResourceExpedition(
        ResourceOpportunityId opportunityId,
        IReadOnlyList<CitizenId> memberIds,
        ExpeditionRetreatPosture retreatPosture)
    {
        ExpeditionStartResult result = _session.StartResourceExpedition(
            opportunityId,
            memberIds,
            retreatPosture);
        if (result.IsSuccess) SaveNow();
        return result;
    }

    public HeroIncorporationResult TryIncorporateHero(CitizenId citizenId)
    {
        HeroIncorporationResult result = _session.TryIncorporateHero(citizenId);
        if (result.IsSuccess)
        {
            SaveNow();
            EmitSignal(SignalName.CitizensChanged);
        }
        return result;
    }

    public WoundRecoveryResult TryBeginWoundRecovery(CitizenId citizenId)
    {
        WoundRecoveryResult result = _session.TryBeginWoundRecovery(citizenId);
        if (result.IsSuccess)
        {
            SaveNow();
            EmitSignal(SignalName.CitizensChanged);
        }
        return result;
    }

    public CityWorld.MigrantResult TryAcceptPendingProspect()
    {
        var result = _session.TryAcceptPendingProspect();
        if (result.IsSuccess)
        {
            SaveNow();
            EmitSignal(SignalName.CitizensChanged);
        }
        return result;
    }

    public bool CancelExpedition(ExpeditionId id)
    {
        if (_session.CancelExpedition(id))
        {
            SaveNow();
            return true;
        }
        return false;
    }

    private void OnDomainBuildingChanged(object? sender, CityWorldChangedEventArgs e)
    {
        _session.MarkDirty();
        EmitSignal(SignalName.BuildingStateChanged, e.BuildingId.Value);
    }

    private void OnDomainProjectChanged(object? sender, CityWorldChangedEventArgs e)
    {
        _session.MarkDirty();
        EmitSignal(SignalName.ProjectStateChanged, e.BuildingId.Value);
    }

    private void OnDomainPatchChanged(object? sender, PatchChangedEventArgs e)
    {
        _session.MarkDirty();
        EmitSignal(SignalName.NaturalResourceStateChanged, e.PatchId);
    }

    private void OnDomainCultivationSiteChanged(
        object? sender,
        CityWorldChangedEventArgs e)
    {
        _session.MarkDirty();
        EmitSignal(SignalName.CultivationSiteStateChanged, e.BuildingId.Value);
    }

    private void OnDomainExpeditionChanged(object? sender, ExpeditionChangedEventArgs e)
    {
        _session.MarkDirty();
        EmitSignal(SignalName.ExpeditionStateChanged, e.ExpeditionId.Value);
    }
}
