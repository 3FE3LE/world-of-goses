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
/// Load priority: primary slot → seeded in-memory world.
/// </summary>
public partial class CityWorldController : Node
{
    [Signal]
    public delegate void SelectionChangedEventHandler(int selectionState);

    [Signal]
    public delegate void BuildingStateChangedEventHandler(int buildingId);

    [Signal]
    public delegate void CitizenAssignmentRejectedEventHandler(int reason);

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
    }

    public override void _Process(double delta)
    {
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
            _world.AdvanceWorldProductionTick();
        }
    }

    public override void _ExitTree()
    {
        _world.BuildingChanged -= OnDomainBuildingChanged;
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
        EmitSignal(SignalName.BuildingStateChanged, buildingId.Value);
        return true;
    }

    public void ReturnToCity() =>
        EmitSignal(SignalName.SelectionChanged, (int)Selection.MacroView);

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

    public int CurrentProductionRate(BuildingId buildingId) => _world.CurrentProductionRate(buildingId);

    public IReadOnlyList<Citizen> AvailableCitizens() => _world.AvailableCitizens();

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

    public int AdvanceProduction(BuildingId buildingId) => _world.AdvanceProduction(buildingId);

    public void ConfigureProductionPolicy(BuildingId buildingId, bool enabled, int targetStock) =>
        _world.ConfigureProductionPolicy(buildingId, enabled, targetStock);

    private void TryLoadFromDisk()
    {
        // Single source: the primary slot. The legacy single-file
        // fallback was removed during the Building refactor; saves
        // older than the current shape are simply not supported.
        try
        {
            TryLoadFromPrimarySlot();
        }
        catch (Exception ex)
        {
            LastOfflineReport = null;
            GD.Print($"Save recovery: starting from the seeded world because the primary slot is invalid: {ex.Message}");
            // Replace the incompatible primary snapshot immediately so
            // the same warning does not recur on every launch. The
            // persistence layer preserves the rejected file as `.bak`.
            TryAutoSave();
        }
    }

    private bool TryLoadFromPrimarySlot()
    {
        if (!WorldPersistence.SlotExists(WorldPersistence.PrimarySaveSlot)) return false;
        var save = WorldPersistence.LoadFromSlot(WorldPersistence.PrimarySaveSlot);
        _world.Restore(save);
        AnnounceLoad($"slot {WorldPersistence.PrimarySaveSlot}", save);
        return true;
    }

    private void AnnounceLoad(string source, WorldSave save)
    {
        if (save.LastSeenAtUnixMillis > 0)
        {
            var now = DateTimeOffset.UtcNow;
            var lastSeenAt = DateTimeOffset.FromUnixTimeMilliseconds(save.LastSeenAtUnixMillis);
            var ticks = OfflineProgression.ComputeTicks(now, lastSeenAt);

            // Defensive: if the loaded save produced a world with no
            // buildings (a broken legacy migration, for example),
            // skip offline progression rather than crashing on
            // PrimaryBuilding.
            if (_world.Buildings.Count > 0)
            {
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
                GD.PushWarning(
                    $"Loaded save from {source} has no buildings. " +
                    "If this persists, delete %LOCALAPPDATA%/World of Goses/slots/save_slot_0.json " +
                    "and the legacy save.json to start fresh.");
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
}
