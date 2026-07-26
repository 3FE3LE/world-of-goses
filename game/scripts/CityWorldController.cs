#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using WorldofGoses.Domain;
using WorldofGoses.Domain.Persistence;

namespace WorldofGoses;

/// <summary>
/// Owns the domain <see cref="CityWorld"/> and acts as the single
/// entry point the presentation layer uses to query state and apply
/// domain commands. Translates Godot input into domain calls and
/// raises Godot signals in response to domain events.
///
/// Auto-saves the world on a timer (in <see cref="_Process"/>) and
/// on window close (in <see cref="_Notification"/>). No manual
/// save button — the game philosophy forbids progress loss.
///
/// Load priority: primary slot → current v2 world; an absent or retired
/// slot starts the hero onboarding flow without creating production data.
/// </summary>
public partial class CityWorldController : Node
{
    private const string VisualCaptureEnvironmentVariable = "WOG_VISUAL_CAPTURE";
    private const string VisualCaptureCommandLineArgument = "--wog-visual-capture";
    private bool _onboardingCompletionPending;

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
    /// Fires once per world tick (after <see cref="CityWorld.AdvanceWorldTick"/>).
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
    public delegate void ExpeditionStateChangedEventHandler(int expeditionId);

    [Signal]
    public delegate void CitizensChangedEventHandler();

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

    private readonly CityWorld _world = new();

    /// <summary>
    /// Seconds between periodic auto-saves during gameplay. Tunable.
    /// </summary>
    public double AutoSaveIntervalSeconds { get; set; } = 10.0;

    private double _autoSaveTimer;
    private double _simulationTimer;

    public double SimulationTickIntervalSeconds { get; set; } = 1.0;

    /// <summary>
    /// Discrete speed choices the player can pick from the status
    /// panel. The numeric value is the multiplier applied to the
    /// default tick interval (1.0 s). <see cref="SpeedChoice.Paused"/>
    /// uses a sentinel of zero so the advance loop stops ticking.
    /// </summary>
    public enum SpeedChoice
    {
        Paused = 0,
        Normal = 1,
        Fast = 2,
        Fastest = 4,
    }

    private SpeedChoice _speed = SpeedChoice.Normal;
    private SpeedChoice _lastRunningSpeed = SpeedChoice.Normal;

    public SpeedChoice CurrentSpeed => _speed;
    public SpeedChoice LastRunningSpeed => _lastRunningSpeed;

    /// <summary>
    /// Switches the simulation speed and adjusts
    /// <see cref="SimulationTickIntervalSeconds"/> accordingly. A
    /// value of <see cref="SpeedChoice.Paused"/> halts the world
    /// entirely until the player resumes. The change is broadcast via
    /// <see cref="SimulationSpeedChanged"/> so listeners can refresh.
    /// </summary>
    public void SetSimulationSpeed(SpeedChoice speed)
    {
        if (speed is not SpeedChoice.Paused
            and not SpeedChoice.Normal
            and not SpeedChoice.Fast
            and not SpeedChoice.Fastest)
        {
            throw new ArgumentOutOfRangeException(nameof(speed), speed, "Unsupported simulation speed.");
        }

        _speed = speed;
        if (speed != SpeedChoice.Paused) _lastRunningSpeed = speed;
        SimulationTickIntervalSeconds = speed switch
        {
            SpeedChoice.Paused => 0,
            _ => 1.0 / (int)speed,
        };
        if (speed == SpeedChoice.Paused) _simulationTimer = 0;
        EmitSignal(SignalName.SimulationSpeedChanged, (int)speed);
    }

    public void ToggleSimulationPause() =>
        SetSimulationSpeed(_speed == SpeedChoice.Paused ? _lastRunningSpeed : SpeedChoice.Paused);

    public enum Selection
    {
        MacroView = 0,
        BuildingDetail = 1,
        HeroProfile = 2,
    }

    public CityWorld World => _world;

    public OfflineProgressionReport? LastOfflineReport { get; private set; }

    /// <summary>
    /// Toggle for the persistence layer. Enabled by default now that
    /// saves are validated before restore; tests or editor experiments
    /// can disable it to keep the world entirely in memory.
    /// </summary>
    public bool PersistenceEnabled { get; set; } = true;

    private bool PersistenceWritesEnabled =>
        PersistenceEnabled
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
        _world.BuildingChanged += OnDomainBuildingChanged;
        _world.ProjectChanged += OnDomainProjectChanged;
        _world.ExpeditionChanged += OnDomainExpeditionChanged;
        if (_world.Hero is { } hero)
        {
            LineageThemeRegistry.ActiveLineage = LineageThemeRegistry.IdOf(hero.Profile.Lineage);
        }
    }

    public override void _Process(double delta)
    {
        if (_world.NeedsOnboarding || _onboardingCompletionPending) return;

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
            _world.AdvanceWorldTick();
            EmitSignal(SignalName.WorldTickAdvanced, _world.CurrentTick);
        }
    }

    public override void _ExitTree()
    {
        _world.BuildingChanged -= OnDomainBuildingChanged;
        _world.ProjectChanged -= OnDomainProjectChanged;
        _world.ExpeditionChanged -= OnDomainExpeditionChanged;
    }

    public override void _Notification(int what)
    {
        if (!PersistenceWritesEnabled) return;
        if (what == WmCloseRequest)
        {
            TryAutoSave();
        }
    }

    private const int WmCloseRequest = 1006;

    public void SaveNow()
    {
        if (!PersistenceWritesEnabled) return;
        TryAutoSave();
    }

    /// <summary>
    /// Drains every natural resource patch so the macro view renders
    /// the depleted state (no trees, empty parcel slots, but the
    /// patches themselves remain for spatial indexing). Only callable
    /// during a <c>WOG_VISUAL_CAPTURE</c> run; returns silently in
    /// normal play to keep the visual regression path orthogonal to
    /// game logic.
    /// </summary>
    public void DrainAllForestsForVisualRegression()
    {
        if (!IsVisualCaptureMode) return;
        foreach (var patch in _world.NaturalResourcePatches.Values)
        {
            patch.Gather(int.MaxValue);
        }
        // Reflect the new state through the existing signals so the
        // macro view re-renders without an autosave side-effect.
        EmitSignal(SignalName.WorldTickAdvanced, _world.CurrentTick);
    }

    private static bool IsVisualCaptureMode =>
        string.Equals(
            System.Environment.GetEnvironmentVariable(VisualCaptureEnvironmentVariable),
            "1",
            StringComparison.Ordinal);

    public bool TrySaveNow()
    {
        if (!PersistenceWritesEnabled) return true;
        try
        {
            WorldPersistence.SaveToSlot(_world, WorldPersistence.PrimarySaveSlot);
            EmitSignal(SignalName.WorldSaved, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            return true;
        }
        catch (Exception ex)
        {
            GD.PushWarning($"Save failed: {ex.Message}");
            return false;
        }
    }

    public bool ResetPrimarySlotAndRestart()
    {
        if (!PersistenceWritesEnabled) return false;
        try
        {
            WorldPersistence.DeleteSlot(WorldPersistence.PrimarySaveSlot);
            GetTree().ReloadCurrentScene();
            return true;
        }
        catch (Exception ex)
        {
            GD.PushWarning($"Could not reset the primary slot: {ex.Message}");
            return false;
        }
    }

    public bool ResetCityKeepingFounderAndRestart()
    {
        if (!PersistenceWritesEnabled || _world.Hero is null) return false;
        try
        {
            CityWorld restarted = _world.CreateRestartedCityKeepingHero();
            _world.Restore(WorldPersistence.Capture(restarted));
            WorldPersistence.SaveToSlot(
                _world,
                WorldPersistence.PrimarySaveSlot);
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
        if (_world.NeedsOnboarding) return;

        try
        {
            WorldPersistence.SaveToSlot(_world, WorldPersistence.PrimarySaveSlot);
            EmitSignal(SignalName.WorldSaved, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        }
        catch (Exception ex)
        {
            GD.PushWarning($"Auto-save failed: {ex.Message}");
        }
    }

    public bool SelectBuilding(BuildingId buildingId)
    {
        if (_world.GetBuilding(buildingId) is null) return false;
        EmitSignal(SignalName.SelectionChanged, (int)Selection.BuildingDetail);
        EmitSignal(SignalName.BuildingSelected, buildingId.Value);
        EmitSignal(SignalName.BuildingStateChanged, buildingId.Value);
        return true;
    }

    public void ReturnToCity() =>
        EmitSignal(SignalName.SelectionChanged, (int)Selection.MacroView);

    public bool SelectHero()
    {
        if (_world.Hero is null) return false;
        EmitSignal(SignalName.SelectionChanged, (int)Selection.HeroProfile);
        return true;
    }

    public bool NeedsOnboarding() =>
        _world.NeedsOnboarding || _onboardingCompletionPending;

    public bool HasHero() => _world.Hero is not null;

    public Citizen? HeroOrNull() => _world.Hero;

    public HeroCreationResult TryCompleteOnboarding(HeroCreationRequest request)
    {
        if (_onboardingCompletionPending && _world.Hero is Citizen pendingHero)
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

        var result = _world.TryCreateHero(request);
        if (!result.IsSuccess || !result.CitizenId.HasValue) return result;

        // Drop two forests so the hero has a gathering target before
        // the Basic Shelter can be authorised. The wood-cost gate
        // would otherwise deadlock a fresh world.
        _world.SeedStartingForests();

        _onboardingCompletionPending = true;
        if (!TrySaveNow())
        {
            return HeroCreationResult.Fail(HeroCreationOutcome.SaveFailed);
        }
        _onboardingCompletionPending = false;
        LineageThemeRegistry.ActiveLineage = LineageThemeRegistry.IdOf(request.Profile.Lineage);
        EmitSignal(SignalName.HeroCreated, result.CitizenId.Value.Value);
        return result;
    }

    public Building PrimaryBuilding() => _world.PrimaryBuilding;

    /// <summary>
    /// Same as <see cref="PrimaryBuilding"/> but returns <c>null</c>
    /// when the world has no buildings. Use this from presentation
    /// code that should degrade gracefully (show a fallback label)
    /// rather than crash. Tests and code that asserts non-emptiness
    /// should use the strict <see cref="PrimaryBuilding"/> version.
    /// </summary>
    public Building? PrimaryBuildingOrNull()
    {
        foreach (var b in _world.Buildings.Values) return b;
        return null;
    }

    public IReadOnlyDictionary<CitizenId, Citizen> Citizens() => _world.Citizens;

    public Building? GetBuilding(BuildingId buildingId) => _world.GetBuilding(buildingId);

    public CityStatusSnapshot GetCityStatusSnapshot() =>
        CityStatusSnapshot.From(_world, hasController: true, currentSpeed: (int)_speed);

    public ConstructionSnapshot GetConstructionSnapshot() => ConstructionSnapshot.From(_world);

    public BuildingDetailSnapshot? GetBuildingDetailSnapshot(BuildingId buildingId) =>
        BuildingDetailSnapshot.From(_world, buildingId);

    public CityMacroSnapshot GetCityMacroSnapshot() => CityMacroSnapshot.From(_world);

    public HeroProfileSnapshot? GetHeroProfileSnapshot() => HeroProfileSnapshot.From(_world);

    public int CurrentProductionRate(BuildingId buildingId) => _world.CurrentProductionRate(buildingId);

    public int GatherWood(BuildingId forestId, int amount) =>
        _world.GatherWood(forestId, amount);

    public int GatherWood(BuildingId forestId, int unitId, int amount) =>
        _world.GatherWood(forestId, unitId, amount);

    public IReadOnlyList<Citizen> AvailableCitizens() => _world.AvailableCitizens();

    public IReadOnlyList<Citizen> AvailableCitizensByPriority() => _world.AvailableCitizensByPriority();

    public AssignmentResult TryAssignCitizen(BuildingId buildingId, CitizenId citizenId)
    {
        var result = _world.TryAssignCitizen(buildingId, citizenId);
        if (!result.IsSuccess)
            EmitSignal(SignalName.CitizenAssignmentRejected, (int)result.Outcome);
        return result;
    }

    public AssignmentResult TryUnassignCitizen(BuildingId buildingId, CitizenId citizenId)
    {
        var result = _world.TryUnassignCitizen(buildingId, citizenId);
        if (!result.IsSuccess)
            EmitSignal(SignalName.CitizenAssignmentRejected, (int)result.Outcome);
        return result;
    }

    public AssignmentResult TryAssignCitizenToProject(BuildingId projectId, CitizenId citizenId)
    {
        var result = _world.TryAssignToProject(projectId, citizenId);
        if (!result.IsSuccess)
            EmitSignal(SignalName.CitizenAssignmentRejected, (int)result.Outcome);
        return result;
    }

    public AssignmentResult TryUnassignCitizenFromProject(BuildingId projectId, CitizenId citizenId)
    {
        var result = _world.TryUnassignFromProject(projectId, citizenId);
        if (!result.IsSuccess)
            EmitSignal(SignalName.CitizenAssignmentRejected, (int)result.Outcome);
        return result;
    }

    public ConstructionAuthorizationResult TryAuthorizeBasicShelter()
        => TryAuthorizeConstruction(ConstructionKind.BasicShelter);

    public ConstructionAuthorizationResult TryAuthorizeConstruction(ConstructionKind kind)
        => TryAuthorizeConstruction(kind, selectedLot: null);

    public ConstructionAuthorizationResult TryAuthorizeConstruction(
        ConstructionKind kind,
        ConstructionLot? selectedLot)
    {
        var result = _world.TryAuthorizeConstruction(kind, selectedLot);
        if (result.IsSuccess && result.ProjectId.HasValue)
        {
            SaveNow();
        }
        return result;
    }

    public IReadOnlyList<ConstructionLot> AvailableConstructionLots() =>
        _world.AvailableConstructionLots();

    public void SetProjectEnabled(BuildingId projectId, bool enabled) =>
        _world.SetProjectEnabled(projectId, enabled);

    public bool CancelProject(BuildingId projectId) => _world.CancelProject(projectId);

    public ConstructionProject? GetProject(BuildingId projectId) => _world.GetProject(projectId);

    public IReadOnlyDictionary<BuildingId, ConstructionProject> Projects() => _world.Projects;

    public int AdvanceProduction(BuildingId buildingId) => _world.AdvanceProduction(buildingId);

    public void ConfigureProductionPolicy(BuildingId buildingId, bool enabled, int minStock, int maxStock, int priority) =>
        _world.ConfigureProductionPolicy(buildingId, enabled, minStock, maxStock, priority);

    public void SetProductionEnabled(BuildingId buildingId, bool enabled) =>
        _world.SetProductionEnabled(buildingId, enabled);

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

    internal bool TryLoadFromPrimarySlot(string? slotsDirectoryOverride = null)
    {
        string slotsDirectory = slotsDirectoryOverride ?? WorldPersistence.SlotsDirectory;
        if (!WorldPersistence.SlotExists(WorldPersistence.PrimarySaveSlot, slotsDirectory)) return false;
        // Load raw JSON so the migration helpers can see the original
        // version before Validate rejects it. Validate runs after
        // migration completes.
        var path = System.IO.Path.Combine(
            slotsDirectory,
            $"save_slot_{WorldPersistence.PrimarySaveSlot}.json");
        var save = WorldPersistence.DeserializeFromJson(System.IO.File.ReadAllText(path));
        int originalVersion = save.Version;
        save = WorldPersistence.MigrateToCurrent(save);
        bool migrated = save.Version != originalVersion;
        WorldPersistence.Validate(save);
        _world.Restore(save);
        // Retroactive seed for saves predating the wood-gathering
        // slice: if the world has the founding hero but no Forests,
        // give it two Forests so wood gathering remains reachable.
        // SeedStartingForests is idempotent — it skips when forests
        // already exist or when no hero is present.
        _world.SeedStartingForests();
        _world.EnsureFoundingShelterContributor();
        AnnounceLoad($"slot {WorldPersistence.PrimarySaveSlot}", save);
        if (migrated && PersistenceWritesEnabled)
        {
            WorldPersistence.SaveToSlot(_world, WorldPersistence.PrimarySaveSlot, slotsDirectory);
        }
        return true;
    }

    private void AnnounceLoad(string source, WorldSave save)
    {
        if (save.LastSeenAtUnixMillis > 0)
        {
            var now = DateTimeOffset.UtcNow;
            var lastSeenAt = DateTimeOffset.FromUnixTimeMilliseconds(save.LastSeenAtUnixMillis);
            int ticks = OfflineProgression.ComputeTicks(now, lastSeenAt);
            LastOfflineReport = OfflineProgression.ApplyAll(_world, ticks);

            if (LastOfflineReport.HadProgression)
            {
                GD.Print(
                    $"World loaded from {source} (tick {_world.CurrentTick}). " +
                    $"Offline progression: +{LastOfflineReport.TicksApplied} ticks, " +
                    $"+{LastOfflineReport.StockAdded} stock, " +
                    $"{(int)LastOfflineReport.SimulatedTime.TotalSeconds}s simulated.");
            }
            else
            {
                GD.Print($"World loaded from {source} (tick {_world.CurrentTick}).");
            }
        }
        else
        {
            LastOfflineReport = null;
            GD.Print($"World loaded from {source} (tick {_world.CurrentTick}).");
        }
    }

    private void OnDomainBuildingChanged(object? sender, CityWorldChangedEventArgs e) =>
        EmitSignal(SignalName.BuildingStateChanged, e.BuildingId.Value);

    private void OnDomainProjectChanged(object? sender, CityWorldChangedEventArgs e) =>
        EmitSignal(SignalName.ProjectStateChanged, e.BuildingId.Value);

    public ExpeditionStartResult StartExpedition(ExpeditionRequest request)
    {
        var result = _world.StartExpedition(request);
        if (result.IsSuccess) SaveNow();
        return result;
    }

    public CityWorld.MigrantResult TryRecruitMigrant(CitizenProfile profile, string? name = null)
    {
        var result = _world.TryRecruitMigrant(profile, name);
        if (result.IsSuccess)
        {
            SaveNow();
            EmitSignal(SignalName.CitizensChanged);
        }
        return result;
    }

    public CityWorld.MigrantResult TryRecruitMigrant(string? name = null)
    {
        var result = _world.TryRecruitMigrant(name);
        if (result.IsSuccess)
        {
            SaveNow();
            EmitSignal(SignalName.CitizensChanged);
        }
        return result;
    }

    public bool CancelExpedition(ExpeditionId id)
    {
        if (_world.CancelExpedition(id))
        {
            SaveNow();
            return true;
        }
        return false;
    }

    private void OnDomainExpeditionChanged(object? sender, ExpeditionChangedEventArgs e) =>
        EmitSignal(SignalName.ExpeditionStateChanged, e.ExpeditionId.Value);
}
