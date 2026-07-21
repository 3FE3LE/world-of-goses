#nullable enable
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
/// The current loader accepts only the current schema. Retired v1
/// prototype saves are left for the controller to replace after the
/// player confirms a new hero profile.
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
                WoodReserve = building.WoodReserve,
                ProductionEnabled = building.ProductionEnabled,
                MinStock = building.MinStock,
                MaxStock = building.MaxStock,
                Priority = building.Priority,
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
                Profile = CaptureProfile(citizen.Profile),
                CurrentAssignment = citizen.CurrentAssignment?.Value,
                StaminaCurrent = citizen.CurrentStamina,
                StaminaMax = citizen.MaxStamina,
                WellFedRemainingTicks = citizen.WellFedRemainingTicks,
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

        foreach (var project in world.Projects.Values)
        {
            var ps = new ConstructionProjectSave
            {
                Id = project.Id.Value,
                Kind = project.Kind.ToString(),
                DisplayName = project.DisplayName,
                Progress = project.Progress,
                RequiredWork = project.RequiredWork,
                WorkerCapacity = project.WorkerCapacity,
                Enabled = project.Enabled,
                AssignedCitizenIds = new List<int>(project.AssignedCitizenIds.Count),
            };
            foreach (var cid in project.AssignedCitizenIds)
            {
                ps.AssignedCitizenIds.Add(cid.Value);
            }
            save.Projects.Add(ps);
        }

        return save;
    }

    internal static CitizenProfileSave CaptureProfile(CitizenProfile profile)
    {
        var save = new CitizenProfileSave
        {
            Lineage = profile.Lineage.Value,
            Gender = profile.Gender.ToString(),
            ElementalAffinity = profile.ElementalAffinity.Value,
            CombatStyle = profile.CombatStyle.Value,
            PoliticalOrientation = profile.PoliticalOrientation.Value,
            SpiritualPosture = profile.SpiritualPosture.Value,
        };
        save.Aptitudes.AddRange(profile.Aptitudes.Select(value => value.Value));
        save.ProfessionalAffinities.AddRange(profile.ProfessionalAffinities.Select(value => value.Value));
        save.WeaponPreferences.AddRange(profile.WeaponPreferences.Select(value => value.Value));
        save.PersonalityTraits.AddRange(profile.PersonalityTraits.Select(value => value.Value));
        return save;
    }

    internal static CitizenProfile RestoreProfile(CitizenProfileSave save)
    {
        ArgumentNullException.ThrowIfNull(save);
        // Pre-v4 saves omit Gender; default to Masculine so legacy
        // heroes still load and the simulation never deserializes a
        // missing enum.
        GenderId gender = GenderId.Masculine;
        if (!string.IsNullOrEmpty(save.Gender)
            && Enum.TryParse(save.Gender, ignoreCase: true, out GenderId parsed))
        {
            gender = parsed;
        }
        if (!CitizenProfile.TryCreate(
                new LineageId(save.Lineage),
                gender,
                save.Aptitudes.Select(value => new AptitudeId(value)),
                save.ProfessionalAffinities.Select(value => new ProfessionFamilyId(value)),
                new ElementalAffinityId(save.ElementalAffinity),
                new CombatStyleId(save.CombatStyle),
                save.WeaponPreferences.Select(value => new WeaponPreferenceId(value)),
                save.PersonalityTraits.Select(value => new PersonalityTraitId(value)),
                new PoliticalOrientationId(save.PoliticalOrientation),
                new SpiritualPostureId(save.SpiritualPosture),
                out CitizenProfile? profile,
                out string error))
        {
            throw new InvalidOperationException($"Invalid citizen profile: {error}");
        }
        return profile!;
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
        if (save.Projects is null)
        {
            throw new InvalidOperationException("Save.Projects is null.");
        }
        if (save.Version != WorldSave.CurrentVersion)
        {
            throw new IncompatibleSaveVersionException(save.Version, WorldSave.CurrentVersion);
        }
        if (save.CurrentTick < 0)
        {
            throw new InvalidOperationException("Save.CurrentTick is negative.");
        }

        var buildingIds = new HashSet<int>();
        var projectIds = new HashSet<int>();
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
            if (b.TargetStock is int legacyTarget
                && (legacyTarget < 0 || legacyTarget > b.StorageCapacity))
            {
                throw new InvalidOperationException(
                    $"Building {b.Id}: legacy TargetStock must be between 0 and StorageCapacity.");
            }
            if (b.MinStock is int minStock
                && (minStock < 0 || minStock > b.StorageCapacity))
            {
                throw new InvalidOperationException(
                    $"Building {b.Id}: MinStock must be between 0 and StorageCapacity.");
            }
            if (b.MaxStock is int maxStock
                && (maxStock < 0 || maxStock > b.StorageCapacity))
            {
                throw new InvalidOperationException(
                    $"Building {b.Id}: MaxStock must be between 0 and StorageCapacity.");
            }
            if (b.MinStock is int minVal
                && b.MaxStock is int maxVal
                && minVal > maxVal)
            {
                throw new InvalidOperationException(
                    $"Building {b.Id}: MinStock ({minVal}) cannot exceed MaxStock ({maxVal}).");
            }
            if (b.Priority is int priority && priority < 0)
            {
                throw new InvalidOperationException(
                    $"Building {b.Id}: Priority must be non-negative (got {priority}).");
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
            if (c.Profile is null)
            {
                throw new InvalidOperationException($"Citizen {c.Id}: profile is missing.");
            }
            if (c.Profile.Aptitudes is null
                || c.Profile.ProfessionalAffinities is null
                || c.Profile.WeaponPreferences is null
                || c.Profile.PersonalityTraits is null)
            {
                throw new InvalidOperationException($"Citizen {c.Id}: profile collection is null.");
            }
            try
            {
                _ = RestoreProfile(c.Profile);
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException($"Citizen {c.Id}: {ex.Message}", ex);
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
            if (c.StaminaCurrent < 0)
            {
                throw new InvalidOperationException(
                    $"Citizen {c.Id}: StaminaCurrent is negative ({c.StaminaCurrent}).");
            }
            if (c.StaminaMax is int smax)
            {
                if (smax <= 0)
                {
                    throw new InvalidOperationException(
                        $"Citizen {c.Id}: StaminaMax must be positive (got {smax}).");
                }
                if (c.StaminaCurrent > smax)
                {
                    throw new InvalidOperationException(
                        $"Citizen {c.Id}: StaminaCurrent ({c.StaminaCurrent}) exceeds StaminaMax ({smax}).");
                }
            }
            if (c.WellFedRemainingTicks < 0)
            {
                throw new InvalidOperationException(
                    $"Citizen {c.Id}: WellFedRemainingTicks is negative ({c.WellFedRemainingTicks}).");
            }
        }

        int heroCount = save.Citizens.Count(c =>
            c.Roles.Any(role => role.Id == RoleId.Hero.Value));
        if (heroCount != 1)
        {
            throw new InvalidOperationException(
                $"Save must contain exactly one hero citizen (found {heroCount}).");
        }

        foreach (var p in save.Projects)
        {
            if (p is null)
            {
                throw new InvalidOperationException("Save.Projects contains a null entry.");
            }
            if (!projectIds.Add(p.Id))
            {
                throw new InvalidOperationException($"Duplicate project id {p.Id}.");
            }
            if (buildingIds.Contains(p.Id))
            {
                throw new InvalidOperationException(
                    $"Project id {p.Id} collides with an existing building.");
            }
            if (p.RequiredWork <= 0)
            {
                throw new InvalidOperationException(
                    $"Project {p.Id}: RequiredWork must be positive (got {p.RequiredWork}).");
            }
            if (p.WorkerCapacity < 0)
            {
                throw new InvalidOperationException(
                    $"Project {p.Id}: WorkerCapacity is negative.");
            }
            if (p.Progress < 0 || p.Progress > p.RequiredWork)
            {
                throw new InvalidOperationException(
                    $"Project {p.Id}: Progress ({p.Progress}) is out of [0, {p.RequiredWork}].");
            }
            if (p.AssignedCitizenIds is null)
            {
                throw new InvalidOperationException($"Project {p.Id}: AssignedCitizenIds is null.");
            }
            if (p.AssignedCitizenIds.Count > p.WorkerCapacity)
            {
                throw new InvalidOperationException(
                    $"Project {p.Id}: assigned citizens exceed capacity.");
            }
            if (p.AssignedCitizenIds.Count != p.AssignedCitizenIds.Distinct().Count())
            {
                throw new InvalidOperationException(
                    $"Project {p.Id}: duplicate assigned citizen id.");
            }
            foreach (var cid in p.AssignedCitizenIds)
            {
                if (!citizenIds.Contains(cid))
                {
                    throw new InvalidOperationException(
                        $"Project {p.Id} references unknown citizen {cid}.");
                }
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
                && !buildingIds.Contains(c.CurrentAssignment.Value)
                && !projectIds.Contains(c.CurrentAssignment.Value))
            {
                throw new InvalidOperationException(
                    $"Citizen {c.Id} references unknown assignment target {c.CurrentAssignment.Value}.");
            }
            if (c.CurrentAssignment.HasValue && buildingIds.Contains(c.CurrentAssignment.Value))
            {
                var building = save.Buildings.Single(b => b.Id == c.CurrentAssignment.Value);
                if (!building.AssignedCitizenIds.Contains(c.Id))
                {
                    throw new InvalidOperationException(
                        $"Citizen {c.Id} and building {building.Id} disagree about the assignment.");
                }
            }
            if (c.CurrentAssignment.HasValue && projectIds.Contains(c.CurrentAssignment.Value))
            {
                var project = save.Projects.Single(p => p.Id == c.CurrentAssignment.Value);
                if (!project.AssignedCitizenIds.Contains(c.Id))
                {
                    throw new InvalidOperationException(
                        $"Citizen {c.Id} and project {project.Id} disagree about the assignment.");
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

    /// <summary>
    /// Upgrades a v2 save to v3 in-place. Missing
    /// <see cref="BuildingSave.MinStock"/>/<see cref="BuildingSave.MaxStock"/>/
    /// <see cref="BuildingSave.Priority"/> fields default to
    /// <c>0</c>/<see cref="BuildingSave.StorageCapacity"/>/<c>0</c>. The legacy
    /// <see cref="BuildingSave.TargetStock"/> field is preserved for
    /// compatibility but no longer drives production. Returns the
    /// upgraded save so the caller can persist it before the next
    /// catch-up cycle.
    /// </summary>
    public static WorldSave MigrateV2ToV3(WorldSave save)
    {
        ArgumentNullException.ThrowIfNull(save);
        if (save.Version != 2)
        {
            throw new InvalidOperationException(
                $"MigrateV2ToV3 expects version 2 but found {save.Version}.");
        }

        foreach (var bs in save.Buildings)
        {
            if (bs is null) continue;
            // Prefer an explicit MaxStock if the v2 saver ever wrote
            // one; otherwise fall back to legacy TargetStock.
            if (bs.MaxStock is null && bs.TargetStock is int legacy)
            {
                bs.MaxStock = legacy;
            }
            bs.MinStock ??= 0;
            bs.Priority ??= 0;
        }

        save.Version = 3;
        return save;
    }

    /// <summary>
    /// Upgrades a v3 save to v4 by defaulting each citizen profile's
    /// <see cref="CitizenProfileSave.Gender"/> to Masculine when the
    /// field is absent. Pre-v4 saves were authored before gender was
    /// an explicit identity choice, so the visual registry picked a
    /// variant from <c>AppearanceSeed</c>; v4 keeps the legacy default
    /// so the same hero loads with the same sprite.
    /// </summary>
    public static WorldSave MigrateV3ToV4(WorldSave save)
    {
        ArgumentNullException.ThrowIfNull(save);
        if (save.Version != 3)
        {
            throw new InvalidOperationException(
                $"MigrateV3ToV4 expects version 3 but found {save.Version}.");
        }

        foreach (var cs in save.Citizens)
        {
            if (cs is null) continue;
            if (cs.Profile is not null && string.IsNullOrEmpty(cs.Profile.Gender))
            {
                cs.Profile.Gender = GenderId.Masculine.ToString();
            }
        }

        save.Version = WorldSave.CurrentVersion;
        return save;
    }
}
