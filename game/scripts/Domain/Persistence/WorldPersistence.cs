using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using WorldofGoses.Domain;

namespace WorldofGoses.Domain.Persistence;

/// <summary>
/// Reads and writes <see cref="WorldSave"/> snapshots to JSON and
/// translates between the live <see cref="CityWorld"/> and the
/// persisted shape. Pure logic — depends only on the domain and
/// the BCL, so it is fully testable without booting Godot.
///
/// All disk writes use a temp-file + atomic replace pattern. A
/// crash mid-write leaves the original file (if any) intact and
/// any previous version is preserved as a <c>.bak</c> sidecar.
///
/// The presentation layer drives persistence; the controller has a
/// toggle (<c>PersistenceEnabled</c>) to disable it during model
/// refactors. The legacy single-file fallback was removed in
/// Slice 8 cleanup: saves older than the current shape are simply
/// not supported — re-seed instead.
/// </summary>
public static class WorldPersistence
{
    /// <summary>The single slot the controller uses to keep the world's canonical state.</summary>
    public const int PrimarySaveSlot = 0;

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    public static string SerializeToJson(WorldSave save) =>
        JsonSerializer.Serialize(save, Options);

    public static WorldSave DeserializeFromJson(string json) =>
        JsonSerializer.Deserialize<WorldSave>(json, Options)
            ?? throw new InvalidOperationException("Empty or null save document.");

    public static WorldSave Capture(CityWorld world) =>
        Capture(world, DateTimeOffset.UtcNow);

    public static WorldSave Capture(CityWorld world, DateTimeOffset now)
    {
        var save = new WorldSave
        {
            Version = WorldSave.CurrentVersion,
            CurrentTick = world.CurrentTick,
            LastSeenAtUnixMillis = now.ToUnixTimeMilliseconds(),
        };

        foreach (var building in world.Buildings.Values)
        {
            var bs = new BuildingSave
            {
                Id = building.Id.Value,
                DisplayName = building.DisplayName,
                Kind = building.Kind.ToString(),
                ProducedResourceType = building.ProducedResourceType.ToString(),
                ProducedCompetencyId = building.ProducedCompetencyId.Value,
                ResourceLabel = building.ResourceLabel,
                ResourceUnit = building.ResourceUnit,
                WorkerCapacity = building.WorkerCapacity,
                VisualCapacity = building.VisualCapacity,
                BaseProductionPerWorker = building.BaseProductionPerWorker,
                StorageCapacity = building.StorageCapacity,
                Stock = building.Stock,
                AssignedCitizenIds = new List<int>(building.AssignedCitizenIds.Count),
            };
            foreach (var cid in building.AssignedCitizenIds)
            {
                bs.AssignedCitizenIds.Add(cid.Value);
            }
            save.Buildings.Add(bs);
        }

        foreach (var citizen in world.Citizens.Values)
        {
            var cs = new CitizenSave
            {
                Id = citizen.Id.Value,
                Name = citizen.Name,
                AppearanceSeed = citizen.AppearanceSeed,
                CurrentAssignment = citizen.CurrentAssignment?.Value,
            };
            foreach (var entry in citizen.Competencies.Values)
            {
                cs.Competencies.Add(new CompetencySave
                {
                    Id = entry.Id.Value,
                    Experience = entry.Experience,
                });
            }
            foreach (var role in citizen.Roles)
            {
                cs.Roles.Add(new RoleSave
                {
                    Id = role.Id.Value,
                    GrantedAtTick = role.GrantedAtTick,
                });
            }
            save.Citizens.Add(cs);
        }

        return save;
    }

    /// <summary>
    /// Validates the structural and cross-entity invariants of a
    /// save. Throws <see cref="InvalidOperationException"/> on
    /// failure so the loader can surface a structured error
    /// instead of letting <see cref="Building"/>'s constructor
    /// throw <see cref="ArgumentOutOfRangeException"/> with a
    /// misleading "bad parameter name" message.
    /// </summary>
    public static void Validate(WorldSave save)
    {
        if (save is null)
        {
            throw new InvalidOperationException("Save is null.");
        }
        if (save.Buildings is null)
        {
            throw new InvalidOperationException("Save.Buildings is null.");
        }
        if (save.Citizens is null)
        {
            throw new InvalidOperationException("Save.Citizens is null.");
        }
        if (save.Buildings.Count == 0)
        {
            throw new InvalidOperationException(
                "Save contains no buildings; the current prototype requires its seeded Quarry and Farm.");
        }
        if (save.Version <= 0 || save.Version > WorldSave.CurrentVersion)
        {
            throw new InvalidOperationException($"Unsupported save version {save.Version}.");
        }
        if (save.CurrentTick < 0)
        {
            throw new InvalidOperationException("Save.CurrentTick is negative.");
        }

        var buildingIds = new HashSet<int>();
        foreach (var b in save.Buildings)
        {
            if (b is null)
            {
                throw new InvalidOperationException("Save.Buildings contains a null entry.");
            }
            if (b.WorkerCapacity < 0)
            {
                throw new InvalidOperationException($"Building {b.Id}: WorkerCapacity is negative.");
            }
            if (!buildingIds.Add(b.Id))
            {
                throw new InvalidOperationException($"Duplicate building id {b.Id}.");
            }
            if (b.VisualCapacity < 0 || b.VisualCapacity > b.WorkerCapacity)
            {
                throw new InvalidOperationException(
                    $"Building {b.Id}: VisualCapacity must be between 0 and WorkerCapacity.");
            }
            if (b.BaseProductionPerWorker < 0)
            {
                throw new InvalidOperationException(
                    $"Building {b.Id}: BaseProductionPerWorker is negative.");
            }
            if (b.StorageCapacity < 0)
            {
                throw new InvalidOperationException($"Building {b.Id}: StorageCapacity is negative.");
            }
            if (b.Stock < 0)
            {
                throw new InvalidOperationException($"Building {b.Id}: Stock is negative.");
            }
            if (b.Stock > b.StorageCapacity)
            {
                throw new InvalidOperationException(
                    $"Building {b.Id}: Stock ({b.Stock}) exceeds StorageCapacity ({b.StorageCapacity}).");
            }
            if (b.AssignedCitizenIds is null)
            {
                throw new InvalidOperationException($"Building {b.Id}: AssignedCitizenIds is null.");
            }
            if (b.AssignedCitizenIds.Count > b.WorkerCapacity)
            {
                throw new InvalidOperationException($"Building {b.Id}: assigned citizens exceed capacity.");
            }
            if (b.AssignedCitizenIds.Count != b.AssignedCitizenIds.Distinct().Count())
            {
                throw new InvalidOperationException($"Building {b.Id}: duplicate assigned citizen id.");
            }
        }
        var citizenIds = new HashSet<int>();
        foreach (var c in save.Citizens)
        {
            if (c is null)
            {
                throw new InvalidOperationException("Save.Citizens contains a null entry.");
            }
            if (!citizenIds.Add(c.Id))
            {
                throw new InvalidOperationException($"Duplicate citizen id {c.Id}.");
            }
            if (c.Competencies is null || c.Roles is null)
            {
                throw new InvalidOperationException($"Citizen {c.Id}: attachment collection is null.");
            }
            if (c.Competencies.Any(entry => entry is null
                || string.IsNullOrWhiteSpace(entry.Id)
                || entry.Experience < 0))
            {
                throw new InvalidOperationException($"Citizen {c.Id}: invalid competency entry.");
            }
            if (c.Roles.Any(role => role is null || string.IsNullOrWhiteSpace(role.Id)))
            {
                throw new InvalidOperationException($"Citizen {c.Id}: invalid role entry.");
            }
        }

        // Cross-entity invariants: every AssignedCitizenId must exist
        // as a citizen; every CurrentAssignment must exist as a
        // building. Without these, Restore produces a building whose
        // assigned list and a citizen whose assignment point at
        // different sets — a silent inconsistency.
        foreach (var b in save.Buildings)
        {
            foreach (var cid in b.AssignedCitizenIds)
            {
                if (!citizenIds.Contains(cid))
                {
                    throw new InvalidOperationException(
                        $"Building {b.Id} references unknown citizen {cid}.");
                }
                var citizen = save.Citizens.Single(c => c.Id == cid);
                if (citizen.CurrentAssignment != b.Id)
                {
                    throw new InvalidOperationException(
                        $"Building {b.Id} and citizen {cid} disagree about the assignment.");
                }
            }
        }
        foreach (var c in save.Citizens)
        {
            if (c.CurrentAssignment.HasValue
                && !buildingIds.Contains(c.CurrentAssignment.Value))
            {
                throw new InvalidOperationException(
                    $"Citizen {c.Id} references unknown building {c.CurrentAssignment.Value}.");
            }
            if (c.CurrentAssignment.HasValue)
            {
                var building = save.Buildings.Single(b => b.Id == c.CurrentAssignment.Value);
                if (!building.AssignedCitizenIds.Contains(c.Id))
                {
                    throw new InvalidOperationException(
                        $"Citizen {c.Id} and building {building.Id} disagree about the assignment.");
                }
            }
        }
    }

    /// <summary>
    /// Writes the save as pretty-printed JSON. Atomic via temp-file +
    /// replace: a crash mid-write leaves the original file (if any)
    /// intact and preserves the previous version as a <c>.bak</c>.
    /// Uses <see cref="File.Replace(string, string, string?)"/> when
    /// the destination exists and <see cref="File.Move(string, string,
    /// bool)"/> with overwrite otherwise — the latter avoids a
    /// TOCTOU window where a concurrent writer could create the
    /// destination between the <see cref="File.Exists"/> check and
    /// the move call.
    /// </summary>
    public static void WriteToFile(WorldSave save, string path)
    {
        Validate(save);
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var tmpPath = path + ".tmp";
        var bakPath = path + ".bak";

        try
        {
            File.WriteAllText(tmpPath, SerializeToJson(save));

            if (File.Exists(path))
            {
                try
                {
                    File.Replace(tmpPath, path, bakPath);
                }
                catch (UnauthorizedAccessException)
                {
                    ReplaceWithPortableFallback(tmpPath, path, bakPath);
                }
                catch (PlatformNotSupportedException)
                {
                    ReplaceWithPortableFallback(tmpPath, path, bakPath);
                }
            }
            else
            {
                File.Move(tmpPath, path, overwrite: true);
            }
        }
        finally
        {
            if (File.Exists(tmpPath)) File.Delete(tmpPath);
        }
    }

    private static void ReplaceWithPortableFallback(string tmpPath, string path, string bakPath)
    {
        File.Copy(path, bakPath, overwrite: true);
        File.Move(tmpPath, path, overwrite: true);
    }

    public static WorldSave ReadFromFile(string path) =>
        DeserializeFromJson(File.ReadAllText(path));

    public static bool SaveFileExists(string path) => File.Exists(path);

    public static string SaveDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "World of Goses");

    public static string SlotsDirectory => Path.Combine(SaveDirectory, "slots");

    public static string SlotPath(int slot) =>
        Path.Combine(SlotsDirectory, $"save_slot_{slot}.json");

    public static bool SlotExists(int slot) => SlotExists(slot, SlotsDirectory);

    public static bool SlotExists(int slot, string slotsDirectory) =>
        File.Exists(Path.Combine(slotsDirectory, $"save_slot_{slot}.json"));

    public static void SaveToSlot(CityWorld world, int slot) =>
        SaveToSlot(world, slot, SlotsDirectory);

    public static void SaveToSlot(CityWorld world, int slot, string slotsDirectory)
    {
        var save = Capture(world);
        WriteToFile(save, Path.Combine(slotsDirectory, $"save_slot_{slot}.json"));
    }

    public static WorldSave LoadFromSlot(int slot) =>
        LoadFromSlot(slot, SlotsDirectory);

    public static WorldSave LoadFromSlot(int slot, string slotsDirectory)
    {
        var path = Path.Combine(slotsDirectory, $"save_slot_{slot}.json");
        var save = ReadFromFile(path);
        Validate(save);
        return save;
    }
}
