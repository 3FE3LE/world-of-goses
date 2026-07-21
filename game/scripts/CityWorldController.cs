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

    private readonly CityWorld _world = new();

    /// <summary>
    /// Seconds between periodic auto-saves during gameplay. Tunable.
    /// </summary>
    public double AutoSaveIntervalSeconds { get; set; } = 10.0;

    private double _autoSaveTimer;
    private double _simulationTimer;

    public double SimulationTickIntervalSeconds { get; set; } = 1.0;

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

    public override void _Ready()
    {
        if (PersistenceEnabled) TryLoadFromDisk();
        _world.BuildingChanged += OnDomainBuildingChanged;
        _world.ProjectChanged += OnDomainProjectChanged;
        if (_world.Hero is { } hero)
        {
            LineageThemeRegistry.ActiveLineage = LineageThemeRegistry.IdOf(hero.Profile.Lineage);
        }
    }

    public override void _Process(double delta)
    {
        if (_world.NeedsOnboarding) return;

        AdvanceLiveSimulation(delta);

        if (!PersistenceEnabled) return;
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
    }

    public override void _Notification(int what)
    {
        if (!PersistenceEnabled) return;
        if (what == WmCloseRequest)
        {
            TryAutoSave();
        }
    }

    private const int WmCloseRequest = 1006;

    public void SaveNow()
    {
        if (!PersistenceEnabled) return;
        TryAutoSave();
    }

    private void TryAutoSave()
    {
        if (_world.NeedsOnboarding) return;

        try
        {
            WorldPersistence.SaveToSlot(_world, WorldPersistence.PrimarySaveSlot);
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

    public bool NeedsOnboarding() => _world.NeedsOnboarding;

    public Citizen? HeroOrNull() => _world.Hero;

    public HeroCreationResult TryCompleteOnboarding(HeroCreationRequest request)
    {
        var result = _world.TryCreateHero(request);
        if (!result.IsSuccess || !result.CitizenId.HasValue) return result;

        // Drop two forests so the hero has a gathering target before
        // the Basic Shelter can be authorised. The wood-cost gate
        // would otherwise deadlock a fresh world.
        _world.SeedStartingForests();

        LineageThemeRegistry.ActiveLineage = LineageThemeRegistry.IdOf(request.Profile.Lineage);
        EmitSignal(SignalName.HeroCreated, result.CitizenId.Value.Value);
        SaveNow();
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

    public CityStatusSnapshot GetCityStatusSnapshot() => CityStatusSnapshot.From(_world);

    public ConstructionSnapshot GetConstructionSnapshot() => ConstructionSnapshot.From(_world);

    public BuildingDetailSnapshot? GetBuildingDetailSnapshot(BuildingId buildingId) =>
        BuildingDetailSnapshot.From(_world, buildingId);

    public int CurrentProductionRate(BuildingId buildingId) => _world.CurrentProductionRate(buildingId);

    public int GatherWood(BuildingId forestId, int amount) =>
        _world.GatherWood(forestId, amount);

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
    {
        var result = _world.TryAuthorizeConstruction(kind);
        if (result.IsSuccess && result.ProjectId.HasValue)
        {
            SaveNow();
        }
        return result;
    }

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

    private bool TryLoadFromPrimarySlot()
    {
        if (!WorldPersistence.SlotExists(WorldPersistence.PrimarySaveSlot)) return false;
        // Load raw JSON so the migration helpers can see the original
        // version before Validate rejects it. Validate runs after
        // migration completes.
        var path = System.IO.Path.Combine(
            WorldPersistence.SlotsDirectory,
            $"save_slot_{WorldPersistence.PrimarySaveSlot}.json");
        var save = WorldPersistence.DeserializeFromJson(System.IO.File.ReadAllText(path));
        // Pre-v4 saves predate the Gender identity field. Walk them
        // through the migration helpers before restore so the load
        // path is non-fatal across schema bumps.
        while (save.Version < WorldSave.CurrentVersion)
        {
            if (save.Version == 2)
            {
                save = WorldPersistence.MigrateV2ToV3(save);
            }
            else if (save.Version == 3)
            {
                save = WorldPersistence.MigrateV3ToV4(save);
            }
            else
            {
                break;
            }
        }
        WorldPersistence.Validate(save);
        _world.Restore(save);
        // Retroactive seed for saves predating the wood-gathering
        // slice: if the world has the founding hero but no Forests,
        // give it two Forests so wood gathering remains reachable.
        // SeedStartingForests is idempotent — it skips when forests
        // already exist or when no hero is present.
        _world.SeedStartingForests();
        AnnounceLoad($"slot {WorldPersistence.PrimarySaveSlot}", save);
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
}
