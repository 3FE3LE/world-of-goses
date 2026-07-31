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
            EconomicBalanceVersion = 1,
            CurrentTick = world.CurrentTick,
            LastSeenAtUnixMillis = now.ToUnixTimeMilliseconds(),
            PendingProspectSeed = world.PendingProspect?.Seed,
            PendingProspectName = world.PendingProspect?.Name,
        };
        foreach ((ResourceType resource, int amount) in world.Resources.Entries()
            .Where(entry => entry.Location.Kind == ResourceLocationKind.CityInventory)
            .Select(entry => (entry.Resource, entry.Amount)))
        {
            save.CityInventory[resource.ToString()] = amount;
        }

        save.EarlyGameMetrics = CaptureEarlyGameMetrics(world.Metrics);

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
                IronStock = building.IronStock,
                WoodReserve = building.WoodReserve,
                WoodUnitReserves = new List<int>(building.WoodUnitReserves),
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

        foreach (CityParcel parcel in world.Parcels.Values)
        {
            save.Parcels.Add(new ParcelSave
            {
                Id = parcel.Id.Value,
                LogicalColumn = parcel.LogicalColumn,
                LogicalRow = parcel.LogicalRow,
                IsUnlocked = parcel.IsUnlocked,
                TerritoryState = parcel.TerritoryState.ToString(),
            });
        }
        foreach (NaturalResourcePatch patch in world.NaturalResourcePatches.Values)
        {
            save.NaturalResourcePatches.Add(new NaturalResourcePatchSave
            {
                Id = patch.Id,
                ParcelId = patch.ParcelId.Value,
                ResourceType = patch.ResourceType.ToString(),
                LegacyStorageBuildingId = patch.LegacyStorageBuildingId?.Value,
                UnitReserves = new List<int>(patch.UnitReserves),
            });
        }
        foreach (ParcelPlacement placement in world.ParcelPlacements.Values)
        {
            save.ParcelPlacements.Add(new ParcelPlacementSave
            {
                EntityId = placement.EntityId.Value,
                ParcelId = placement.ParcelId.Value,
                LotColumn = placement.LotColumn,
                LotRow = placement.LotRow,
                LotWidth = placement.LotWidth,
                LotHeight = placement.LotHeight,
                FootprintProfileId = placement.FootprintProfileId,
                Orientation = placement.Orientation.ToString(),
            });
        }

        foreach (var citizen in world.Citizens.Values)
        {
            var cs = new CitizenSave
            {
                Id = citizen.Id.Value,
                Name = citizen.Name,
                AppearanceSeed = citizen.AppearanceSeed,
                Origin = citizen.Origin.ToString(),
                AppearanceVariant = citizen.AppearanceVariant.Value,
                Profile = CaptureProfile(citizen.Profile),
                CurrentAssignment = citizen.CurrentAssignment?.Value,
                CommitmentKind = citizen.Commitment.Kind.ToString(),
                CommitmentEntityId = citizen.Commitment.EntityId,
                WorkOrderKind = citizen.WorkOrder?.Kind.ToString(),
                WorkOrderEntityId = citizen.WorkOrder?.TargetId.Value,
                VitalStatus = citizen.VitalStatus.ToString(),
                TransitStartedAtTick = citizen.TransitStartedAtTick,
                CurrentLocation = citizen.CurrentLocation.ToString(),
                ResumeWorkNotBeforeTick = citizen.ResumeWorkNotBeforeTick,
                IsReturningHome = citizen.IsReturningHome,
                WoundSeverity = citizen.Wound?.Severity.ToString(),
                WoundOriginatingEventId = citizen.Wound?.OriginatingEventId.Value,
                WoundRecoveryTicksRemaining = citizen.Wound?.RecoveryTicksRemaining ?? 0,
                StaminaCurrent = citizen.CurrentStamina,
                StaminaMax = citizen.MaxStamina,
                WellFedRemainingTicks = citizen.WellFedRemainingTicks,
                LastVisitedResourceBuildingId =
                    citizen.LastVisitedResourceBuildingId?.Value,
                LastVisitedResourceUnitId = citizen.LastVisitedResourceUnitId,
                LastVisitedResourcePositionIndex =
                    citizen.LastVisitedResourcePositionIndex,
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

        var pinnedEventIds = new HashSet<int>(world.Expeditions.Values
            .Where(expedition => expedition.Status == ExpeditionStatus.Active)
            .SelectMany(expedition => expedition.DispatchEventId is WorldEventId eventId
                ? new[] { eventId.Value }
                : Array.Empty<int>())
            .Concat(world.Citizens.Values
                .Where(citizen => citizen.Wound is not null)
                .Select(citizen => citizen.Wound!.OriginatingEventId.Value)));
        IReadOnlyList<WorldEvent> retained =
            WorldEventRetention.SelectForPersistence(world.Log.Events, pinnedEventIds);
        var retainedIds = new HashSet<int>(retained.Select(evt => evt.Id.Value));
        foreach (var evt in retained)
        {
            save.Events.Add(new WorldEventSave
            {
                Id = evt.Id.Value,
                Tick = evt.Tick,
                Kind = evt.Kind.ToString(),
                SubjectKind = evt.Subject.Kind.ToString(),
                SubjectEntityId = evt.Subject.EntityId,
                SubjectDisplayName = evt.Subject.DisplayName,
                Amount = evt.Amount,
                CauseEventId = evt.CauseEventId is WorldEventId cause
                    && retainedIds.Contains(cause.Value) ? cause.Value : null,
            });
        }

        foreach (ResourceReservation reservation in world.Resources.Reservations)
        {
            save.ResourceReservations.Add(new ResourceReservationSave
            {
                Id = reservation.Id.Value,
                Resource = reservation.Resource.ToString(),
                Amount = reservation.Amount,
                OwnerKind = reservation.Owner.Kind.ToString(),
                OwnerEntityId = reservation.Owner.EntityId,
            });
        }

        foreach (Expedition expedition in world.Expeditions.Values)
        {
            save.Expeditions.Add(new ExpeditionSave
            {
                Id = expedition.Id.Value,
                DisplayName = expedition.DisplayName,
                MemberCitizenIds = expedition.MemberIds.Select(member => member.Value).ToList(),
                LeadCitizenId = expedition.LeadCitizenId.Value,
                StartTick = expedition.StartTick,
                EndTick = expedition.EndTick,
                SupplyResource = expedition.SupplyResource.ToString(),
                SupplyAmount = expedition.SupplyAmount,
                RewardResource = expedition.RewardResource.ToString(),
                RewardAmount = expedition.RewardAmount,
                ReservationId = expedition.ReservationId.Value,
                Status = expedition.Status.ToString(),
                ReturnedAmount = expedition.ReturnedAmount,
                RewardKind = expedition.RewardKind.ToString(),
                DeliveredMigrantId = expedition.DeliveredMigrantId?.Value,
                Phase = expedition.Phase.ToString(),
                EncounterOutcome = expedition.EncounterOutcome?.ToString(),
                RetreatPosture = expedition.RetreatPosture.ToString(),
                DispatchEventId = expedition.DispatchEventId is WorldEventId dispatchEventId
                    && retainedIds.Contains(dispatchEventId.Value)
                    ? dispatchEventId.Value
                    : null,
                TargetParcelId = expedition.TargetParcelId?.Value,
            });
        }

        return save;
    }

    internal static EarlyGameMetricsSave CaptureEarlyGameMetrics(EarlyGameMetrics metrics)
    {
        var save = new EarlyGameMetricsSave
        {
            FirstShelterCompletedAtTick = metrics.FirstShelterCompletedAtTick,
            FirstExpeditionDispatchedAtTick = metrics.FirstExpeditionDispatchedAtTick,
            ExpeditionsDispatched = metrics.ExpeditionsDispatched,
            ExpeditionAbsenceTicks = metrics.ExpeditionAbsenceTicks,
            DawnSamples = metrics.DawnSamples,
            IdleCitizenDays = metrics.IdleCitizenDays,
            ObservedCitizenDays = metrics.ObservedCitizenDays,
            MinFoodHorizonTenths = metrics.MinFoodHorizonTenths,
            FoodHorizonTenthsAtFirstShelter = metrics.FoodHorizonTenthsAtFirstShelter,
        };
        foreach (KeyValuePair<ResourceType, int> entry in metrics.Gathered)
        {
            save.Gathered[entry.Key.ToString()] = entry.Value;
        }
        foreach (KeyValuePair<ResourceType, int> entry in metrics.Consumed)
        {
            save.Consumed[entry.Key.ToString()] = entry.Value;
        }
        return save;
    }

    /// <summary>
    /// Rehydrates the EG-0 measurement. A null save, or a resource name this
    /// build no longer knows, degrades to an empty/skipped entry rather than
    /// throwing: losing a measurement must never cost the player their city.
    /// </summary>
    internal static void RestoreEarlyGameMetrics(
        EarlyGameMetrics metrics,
        EarlyGameMetricsSave? save)
    {
        if (save is null) return;
        var gathered = new Dictionary<ResourceType, int>();
        foreach (KeyValuePair<string, int> entry in save.Gathered)
        {
            if (Enum.TryParse(entry.Key, true, out ResourceType resource))
            {
                gathered[resource] = entry.Value;
            }
        }
        var consumed = new Dictionary<ResourceType, int>();
        foreach (KeyValuePair<string, int> entry in save.Consumed)
        {
            if (Enum.TryParse(entry.Key, true, out ResourceType resource))
            {
                consumed[resource] = entry.Value;
            }
        }
        metrics.Restore(
            save.FirstShelterCompletedAtTick,
            save.FirstExpeditionDispatchedAtTick,
            save.ExpeditionsDispatched,
            save.ExpeditionAbsenceTicks,
            save.DawnSamples,
            save.IdleCitizenDays,
            save.ObservedCitizenDays,
            save.MinFoodHorizonTenths,
            save.FoodHorizonTenthsAtFirstShelter,
            gathered,
            consumed);
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
        if (save.Events is null)
        {
            throw new InvalidOperationException("Save.Events is null.");
        }
        if (save.ResourceReservations is null)
        {
            throw new InvalidOperationException("Save.ResourceReservations is null.");
        }
        if (save.Expeditions is null)
        {
            throw new InvalidOperationException("Save.Expeditions is null.");
        }
        if (save.Parcels is null)
        {
            throw new InvalidOperationException("Save.Parcels is null.");
        }
        if (save.NaturalResourcePatches is null)
        {
            throw new InvalidOperationException("Save.NaturalResourcePatches is null.");
        }
        if (save.ParcelPlacements is null)
        {
            throw new InvalidOperationException("Save.ParcelPlacements is null.");
        }
        if (save.CityInventory is null
            || save.CityInventory.Any(pair =>
                pair.Value < 0
                || !Enum.TryParse(pair.Key, true, out ResourceType _)))
        {
            throw new InvalidOperationException("Save.CityInventory is invalid.");
        }
        if (save.Version != WorldSave.CurrentVersion)
        {
            throw new IncompatibleSaveVersionException(save.Version, WorldSave.CurrentVersion);
        }
        if (save.EconomicBalanceVersion < 0 || save.EconomicBalanceVersion > 1)
        {
            throw new InvalidOperationException("Save.EconomicBalanceVersion is unsupported.");
        }
        if (save.CurrentTick < 0)
        {
            throw new InvalidOperationException("Save.CurrentTick is negative.");
        }
        var parcelIds = new HashSet<int>();
        foreach (ParcelSave parcel in save.Parcels)
        {
            if (parcel is null
                || parcel.Id <= 0
                || parcel.LogicalColumn < 0
                || parcel.LogicalRow < 0
                || !Enum.TryParse(
                    parcel.TerritoryState,
                    true,
                    out ParcelTerritoryState parcelState)
                || parcel.IsUnlocked != (parcelState == ParcelTerritoryState.Available)
                || !parcelIds.Add(parcel.Id))
            {
                throw new InvalidOperationException("Save contains an invalid parcel.");
            }
        }
        var patchIds = new HashSet<int>();
        foreach (NaturalResourcePatchSave patch in save.NaturalResourcePatches)
        {
            if (patch is null
                || patch.Id <= 0
                || !patchIds.Add(patch.Id)
                || !parcelIds.Contains(patch.ParcelId)
                || !Enum.TryParse(patch.ResourceType, true, out ResourceType _)
                || patch.UnitReserves is null
                || patch.UnitReserves.Any(value => value < 0)
                || patch.UnitReserves.Count > NaturalResourcePatch.MaximumUnits
                || (patch.ResourceType == ResourceType.Wood.ToString()
                    && patch.UnitReserves.Any(
                        value => value > CityWorld.StartingTreeWoodReserve)))
            {
                throw new InvalidOperationException(
                    "Save contains an invalid natural-resource patch.");
            }
        }
        var placementEntityIds = new HashSet<int>();
        var restoredPlacements = new List<ParcelPlacement>();
        foreach (ParcelPlacementSave placement in save.ParcelPlacements)
        {
            if (placement is null
                || !placementEntityIds.Add(placement.EntityId)
                || !parcelIds.Contains(placement.ParcelId)
                || !Enum.TryParse(
                    placement.Orientation,
                    true,
                    out BuildingOrientation orientation))
            {
                throw new InvalidOperationException("Save contains an invalid parcel placement.");
            }
            ParcelPlacement restored;
            try
            {
                restored = new ParcelPlacement(
                    new BuildingId(placement.EntityId),
                    new ParcelId(placement.ParcelId),
                    placement.LotColumn,
                    placement.LotRow,
                    placement.LotWidth,
                    placement.LotHeight,
                    placement.FootprintProfileId,
                    orientation);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException(
                    "Save contains an invalid parcel placement.",
                    exception);
            }
            if (restoredPlacements.Any(existing => restored.Overlaps(existing)))
            {
                throw new InvalidOperationException("Save contains overlapping parcel placements.");
            }
            restoredPlacements.Add(restored);
        }
        if (save.Events.Count > WorldEventRetention.MaximumPersistedEvents)
        {
            throw new InvalidOperationException("Save.Events exceeds the retention limit.");
        }
        var eventIds = new HashSet<int>();
        foreach (var evt in save.Events)
        {
            if (evt is null) throw new InvalidOperationException("Save.Events contains a null entry.");
            if (evt.Id <= 0 || !eventIds.Add(evt.Id))
            {
                throw new InvalidOperationException($"Invalid or duplicate event id {evt.Id}.");
            }
            if (evt.Tick < 0 || evt.Tick > save.CurrentTick)
            {
                throw new InvalidOperationException($"Event {evt.Id}: tick is outside world time.");
            }
            if (!Enum.TryParse(evt.Kind, true, out WorldEventKind kind)
                || !WorldEventRetention.IsSignificant(kind))
            {
                throw new InvalidOperationException($"Event {evt.Id}: kind is not persistible.");
            }
            if (!Enum.TryParse(evt.SubjectKind, true, out WorldEventSubjectKind subjectKind)
                || string.IsNullOrWhiteSpace(evt.SubjectDisplayName))
            {
                throw new InvalidOperationException($"Event {evt.Id}: subject is invalid.");
            }
            bool needsEntityId = subjectKind != WorldEventSubjectKind.World;
            if (needsEntityId != evt.SubjectEntityId.HasValue)
            {
                throw new InvalidOperationException($"Event {evt.Id}: subject identity is incomplete.");
            }
        }
        var reservationIds = new HashSet<int>();
        var reservedByResource = new Dictionary<ResourceType, int>();
        foreach (ResourceReservationSave reservation in save.ResourceReservations)
        {
            if (reservation is null
                || reservation.Id <= 0
                || !reservationIds.Add(reservation.Id)
                || reservation.Amount <= 0
                || reservation.OwnerEntityId <= 0
                || !Enum.TryParse(reservation.Resource, true, out ResourceType resource)
                || !Enum.TryParse(reservation.OwnerKind, true, out ResourceReservationOwnerKind ownerKind))
            {
                throw new InvalidOperationException("Save contains an invalid resource reservation.");
            }
            if (ownerKind == ResourceReservationOwnerKind.ConstructionProject
                && !save.Projects.Any(project => project.Id == reservation.OwnerEntityId))
            {
                // Construction project reservations are not strictly
                // tied to a project entry in the persistence contract
                // (they are torn down with the project on cancellation
                // anyway). Skipping the check here keeps the validator
                // cheap while still rejecting orphan expedition
                // reservations, which DO need to be tied to a saved
                // expedition.
            }
            if (ownerKind == ResourceReservationOwnerKind.Expedition
                && !save.Expeditions.Any(expedition => expedition.Id == reservation.OwnerEntityId))
            {
                throw new InvalidOperationException(
                    $"Reservation {reservation.Id} references unknown expedition {reservation.OwnerEntityId}.");
            }
            reservedByResource.TryGetValue(resource, out int reserved);
            reservedByResource[resource] = checked(reserved + reservation.Amount);
        }
        var expeditionIds = new HashSet<int>();
        foreach (ExpeditionSave expedition in save.Expeditions)
        {
            if (expedition is null
                || expedition.Id <= 0
                || !expeditionIds.Add(expedition.Id)
                || expedition.LeadCitizenId <= 0
                || expedition.MemberCitizenIds is null
                || expedition.MemberCitizenIds.Count == 0
                || expedition.MemberCitizenIds.Count > ExpeditionRequest.MaxTeamSize
                || expedition.MemberCitizenIds.Any(member => member <= 0)
                || expedition.MemberCitizenIds.Distinct().Count() != expedition.MemberCitizenIds.Count
                || expedition.StartTick < 0
                || expedition.EndTick < expedition.StartTick
                || expedition.SupplyAmount <= 0
                || (expedition.TargetParcelId.HasValue
                    && !parcelIds.Contains(expedition.TargetParcelId.Value))
                || !Enum.TryParse(expedition.SupplyResource, true, out ResourceType _)
                || !Enum.TryParse(expedition.RewardResource, true, out ResourceType _)
                || !Enum.TryParse(expedition.Status, true, out ExpeditionStatus _)
                || !Enum.TryParse(expedition.Phase, true, out ExpeditionPhase _)
                || !Enum.TryParse(expedition.RetreatPosture, true, out ExpeditionRetreatPosture _)
                || (!string.IsNullOrEmpty(expedition.EncounterOutcome)
                    && !Enum.TryParse(expedition.EncounterOutcome, true, out ExpeditionEncounterOutcome _))
                || !Enum.TryParse(
                    string.IsNullOrEmpty(expedition.RewardKind)
                        ? ExpeditionRewardKind.Supplies.ToString()
                        : expedition.RewardKind,
                    true,
                    out ExpeditionRewardKind _)
                || (string.Equals(
                    expedition.RewardKind,
                    ExpeditionRewardKind.Supplies.ToString(),
                    StringComparison.OrdinalIgnoreCase)
                    && expedition.RewardAmount <= 0))
            {
                throw new InvalidOperationException("Save contains an invalid expedition.");
            }

            _ = Enum.TryParse(expedition.Status, true, out ExpeditionStatus status);
            _ = Enum.TryParse(expedition.Phase, true, out ExpeditionPhase phase);
            _ = Enum.TryParse(
                expedition.RetreatPosture,
                true,
                out ExpeditionRetreatPosture retreatPosture);
            ExpeditionEncounterOutcome? encounterOutcome =
                !string.IsNullOrEmpty(expedition.EncounterOutcome)
                && Enum.TryParse(
                    expedition.EncounterOutcome,
                    true,
                    out ExpeditionEncounterOutcome parsedOutcome)
                    ? parsedOutcome
                    : null;
            bool terminal = status != ExpeditionStatus.Active;
            bool retreatTriggered =
                retreatPosture == ExpeditionRetreatPosture.RetreatAfterSetback
                && encounterOutcome == ExpeditionEncounterOutcome.Setback;
            if ((status == ExpeditionStatus.Active && phase == ExpeditionPhase.Resolved)
                || (terminal && phase != ExpeditionPhase.Resolved)
                || (phase == ExpeditionPhase.Retreating && !retreatTriggered)
                || (status == ExpeditionStatus.Retreated
                    && (!retreatTriggered || expedition.ReturnedAmount != 0)))
            {
                throw new InvalidOperationException(
                    $"Expedition {expedition.Id} has an incoherent status, phase, or retreat result.");
            }
            if (expedition.DispatchEventId is int dispatchEventId
                && !eventIds.Contains(dispatchEventId))
            {
                throw new InvalidOperationException(
                    $"Expedition {expedition.Id} references unknown dispatch event {dispatchEventId}.");
            }
        }
        foreach (var evt in save.Events)
        {
            if (evt.CauseEventId is int causeId
                && (!eventIds.Contains(causeId) || causeId >= evt.Id))
            {
                throw new InvalidOperationException($"Event {evt.Id}: cause must reference an earlier retained event.");
            }
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
            if (b.IronStock < 0)
            {
                throw new InvalidOperationException($"Building {b.Id}: IronStock is negative.");
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
            if (b.WoodUnitReserves is null
                || b.WoodUnitReserves.Any(reserve => reserve < 0))
            {
                throw new InvalidOperationException(
                    $"Building {b.Id}: wood-unit reserves are invalid.");
            }
            if (b.WoodReserve != b.WoodUnitReserves.Sum())
            {
                throw new InvalidOperationException(
                    $"Building {b.Id}: aggregate wood reserve does not match its units.");
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
            bool hasWoundSeverity = !string.IsNullOrWhiteSpace(c.WoundSeverity);
            bool hasWoundOrigin = c.WoundOriginatingEventId.HasValue;
            if (hasWoundSeverity != hasWoundOrigin
                || hasWoundSeverity != (c.WoundRecoveryTicksRemaining > 0))
            {
                throw new InvalidOperationException(
                    $"Citizen {c.Id}: wound identity and recovery state are incomplete.");
            }
            if (hasWoundSeverity)
            {
                if (!Enum.TryParse(c.WoundSeverity, true, out WoundSeverity woundSeverity)
                    || c.WoundOriginatingEventId <= 0
                    || !eventIds.Contains(c.WoundOriginatingEventId.GetValueOrDefault())
                    || c.WoundRecoveryTicksRemaining > WoundRules.RecoveryTicksFor(woundSeverity)
                    || (c.StaminaMax is int woundStaminaMax
                        && c.StaminaCurrent > WoundRules.EffectiveStaminaCap(
                            woundStaminaMax,
                            woundSeverity)))
                {
                    throw new InvalidOperationException($"Citizen {c.Id}: wound state is invalid.");
                }
            }
            bool hasVisitedBuilding = c.LastVisitedResourceBuildingId.HasValue;
            bool hasVisitedUnit = c.LastVisitedResourceUnitId.HasValue;
            if (hasVisitedBuilding != hasVisitedUnit)
            {
                throw new InvalidOperationException(
                    $"Citizen {c.Id}: resource visit identity is incomplete.");
            }
            if (hasVisitedBuilding
                && (c.LastVisitedResourceUnitId < 0
                    || c.LastVisitedResourcePositionIndex < 0))
            {
                throw new InvalidOperationException(
                    $"Citizen {c.Id}: resource visit is invalid.");
            }
        }

        foreach (var pair in reservedByResource)
        {
            int cityStored = save.CityInventory
                .Where(entry => Enum.TryParse(
                    entry.Key,
                    ignoreCase: true,
                    out ResourceType resource)
                    && resource == pair.Key)
                .Sum(entry => entry.Value);
            int buildingStored = pair.Key == ResourceType.Iron
                ? save.Buildings.Sum(building => building.IronStock)
                : save.Buildings
                    .Where(building => Enum.TryParse(building.ProducedResourceType, true, out ResourceType produced)
                        && produced == pair.Key)
                    .Sum(building => building.Stock);
            int stored = checked(cityStored + buildingStored);
            if (pair.Value > stored)
            {
                throw new InvalidOperationException(
                    $"Reserved {pair.Key} ({pair.Value}) exceeds stored amount ({stored}).");
            }
        }

        int heroCount = save.Citizens.Count(c =>
            c.Roles.Any(role => role.Id == RoleId.Hero.Value));
        if (heroCount < 1)
        {
            throw new InvalidOperationException(
                "Save must contain at least one hero citizen.");
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
        foreach (int entityId in placementEntityIds)
        {
            if (!buildingIds.Contains(entityId) && !projectIds.Contains(entityId))
            {
                throw new InvalidOperationException(
                    $"Parcel placement references unknown entity {entityId}.");
            }
        }
        foreach (BuildingSave building in save.Buildings)
        {
            if (building.Kind != BuildingKind.Forest.ToString()
                && !placementEntityIds.Contains(building.Id))
            {
                throw new InvalidOperationException(
                    $"Building {building.Id} has no parcel placement.");
            }
        }
        foreach (ConstructionProjectSave project in save.Projects)
        {
            if (!placementEntityIds.Contains(project.Id))
            {
                throw new InvalidOperationException(
                    $"Project {project.Id} has no parcel placement.");
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

            List<ExpeditionSave> activeExpeditions = save.Expeditions
                .Where(expedition => expedition.MemberCitizenIds.Contains(c.Id)
                    && Enum.TryParse(expedition.Status, true, out ExpeditionStatus status)
                    && status == ExpeditionStatus.Active)
                .ToList();
            if (activeExpeditions.Count > 1)
            {
                throw new InvalidOperationException(
                    $"Citizen {c.Id} belongs to more than one active expedition.");
            }
            bool hasCommitmentKind = !string.IsNullOrWhiteSpace(c.CommitmentKind);
            bool hasCommitmentEntity = c.CommitmentEntityId.HasValue;
            if (!hasCommitmentKind && !hasCommitmentEntity)
            {
                continue;
            }
            if (!hasCommitmentKind
                || !Enum.TryParse(
                    c.CommitmentKind,
                    ignoreCase: true,
                    out CitizenCommitmentKind commitmentKind))
            {
                throw new InvalidOperationException(
                    $"Citizen {c.Id} has an invalid commitment kind.");
            }
            if (commitmentKind == CitizenCommitmentKind.None)
            {
                if (hasCommitmentEntity
                    || c.CurrentAssignment.HasValue
                    || activeExpeditions.Count != 0)
                {
                    throw new InvalidOperationException(
                        $"Citizen {c.Id} has an available commitment that conflicts with world state.");
                }
                continue;
            }
            if (!hasCommitmentEntity || c.CommitmentEntityId <= 0)
            {
                throw new InvalidOperationException(
                    $"Citizen {c.Id} has an incomplete commitment.");
            }

            int commitmentEntityId = c.CommitmentEntityId.GetValueOrDefault();
            bool commitmentMatches = commitmentKind switch
            {
                CitizenCommitmentKind.BuildingWork =>
                    buildingIds.Contains(commitmentEntityId)
                    && c.CurrentAssignment == commitmentEntityId,
                CitizenCommitmentKind.Construction =>
                    projectIds.Contains(commitmentEntityId)
                    && c.CurrentAssignment == commitmentEntityId,
                CitizenCommitmentKind.Expedition =>
                    activeExpeditions.Count == 1
                    && activeExpeditions[0].Id == commitmentEntityId,
                CitizenCommitmentKind.Recovery =>
                    c.WoundOriginatingEventId.HasValue
                    && save.Buildings.Any(building =>
                        building.Id == commitmentEntityId
                        && string.Equals(
                            building.Kind,
                            BuildingKind.Home.ToString(),
                            StringComparison.OrdinalIgnoreCase)),
                _ => false,
            };
            if (!commitmentMatches)
            {
                throw new InvalidOperationException(
                    $"Citizen {c.Id} commitment disagrees with world state.");
            }
        }

        foreach (ExpeditionSave expedition in save.Expeditions)
        {
            foreach (int memberId in expedition.MemberCitizenIds)
            {
                CitizenSave? member = save.Citizens.FirstOrDefault(c => c.Id == memberId);
                if (member is null)
                {
                    throw new InvalidOperationException(
                        $"Expedition {expedition.Id} references unknown member citizen {memberId}.");
                }
                if (!member.Roles.Any(role => role.Id == RoleId.Hero.Value))
                {
                    throw new InvalidOperationException(
                        $"Expedition {expedition.Id} member citizen {memberId} is not an incorporated hero.");
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

    public static bool DeleteSlot(int slot) => DeleteSlot(slot, SlotsDirectory);

    public static bool DeleteSlot(int slot, string slotsDirectory)
    {
        if (slot < 0) throw new ArgumentOutOfRangeException(nameof(slot));
        ArgumentException.ThrowIfNullOrWhiteSpace(slotsDirectory);

        string path = Path.Combine(slotsDirectory, $"save_slot_{slot}.json");
        bool existed = File.Exists(path);
        DeleteIfPresent(path);
        DeleteIfPresent(path + ".bak");
        DeleteIfPresent(path + ".tmp");
        return existed;
    }

    private static void DeleteIfPresent(string path)
    {
        if (File.Exists(path)) File.Delete(path);
    }

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
    /// <summary>
    /// Walks a save through every known schema migration. Keeping this
    /// orchestration in the pure persistence layer lets the Godot controller
    /// and non-Godot tests use exactly the same migration path.
    /// </summary>
    public static WorldSave MigrateToCurrent(WorldSave save)
    {
        ArgumentNullException.ThrowIfNull(save);
        while (save.Version < WorldSave.CurrentVersion)
        {
            save = save.Version switch
            {
                2 => MigrateV2ToV3(save),
                3 => MigrateV3ToV4(save),
                4 => MigrateV4ToV5(save),
                5 => MigrateV5ToV6(save),
                6 => MigrateV6ToV7(save),
                7 => MigrateV7ToV8(save),
                8 => MigrateV8ToV9(save),
                9 => MigrateV9ToV10(save),
                10 => MigrateV10ToV11(save),
                11 => MigrateV11ToV12(save),
                12 => MigrateV12ToV13(save),
                13 => MigrateV13ToV14(save),
                14 => MigrateV14ToV15(save),
                15 => MigrateV15ToV16(save),
                16 => MigrateV16ToV17(save),
                17 => MigrateV17ToV18(save),
                18 => MigrateV18ToV19(save),
                19 => MigrateV19ToV20(save),
                20 => MigrateV20ToV21(save),
                _ => throw new IncompatibleSaveVersionException(
                    save.Version,
                    WorldSave.CurrentVersion),
            };
        }
        return save;
    }

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

        save.Version = 4;
        return save;
    }

    /// <summary>Adds an empty durable history to a v4 snapshot.</summary>
    public static WorldSave MigrateV4ToV5(WorldSave save)
    {
        ArgumentNullException.ThrowIfNull(save);
        if (save.Version != 4)
        {
            throw new InvalidOperationException(
                $"MigrateV4ToV5 expects version 4 but found {save.Version}.");
        }
        save.Events ??= new List<WorldEventSave>();
        save.Version = 5;
        return save;
    }

    /// <summary>Adds durable reservation storage to a v5 snapshot.</summary>
    public static WorldSave MigrateV5ToV6(WorldSave save)
    {
        ArgumentNullException.ThrowIfNull(save);
        if (save.Version != 5)
        {
            throw new InvalidOperationException(
                $"MigrateV5ToV6 expects version 5 but found {save.Version}.");
        }
        save.ResourceReservations ??= new List<ResourceReservationSave>();
        save.Version = 6;
        return save;
    }

    public static WorldSave MigrateV6ToV7(WorldSave save)
    {
        ArgumentNullException.ThrowIfNull(save);
        if (save.Version != 6)
        {
            throw new InvalidOperationException(
                $"MigrateV6ToV7 expects version 6 but found {save.Version}.");
        }
        foreach (BuildingSave building in save.Buildings)
        {
            building.WoodUnitReserves ??= new List<int>();
            if (building.Kind == BuildingKind.Forest.ToString()
                && building.WoodUnitReserves.Count == 0)
            {
                int reserve = Math.Max(0, building.WoodReserve ?? 0);
                for (int index = 0; index < reserve; index++)
                {
                    building.WoodUnitReserves.Add(1);
                }
            }
        }
        save.Version = 7;
        return save;
    }

    public static WorldSave MigrateV7ToV8(WorldSave save)
    {
        ArgumentNullException.ThrowIfNull(save);
        if (save.Version != 7)
        {
            throw new InvalidOperationException(
                $"MigrateV7ToV8 expects version 7 but found {save.Version}.");
        }
        save.Parcels ??= new List<ParcelSave>();
        save.NaturalResourcePatches ??= new List<NaturalResourcePatchSave>();
        save.Parcels.Clear();
        save.NaturalResourcePatches.Clear();
        int parcelId = 1;
        foreach (BuildingSave building in save.Buildings)
        {
            if (building.Kind != BuildingKind.Forest.ToString()) continue;
            save.Parcels.Add(new ParcelSave
            {
                Id = parcelId,
                LogicalColumn = parcelId - 1,
                LogicalRow = 0,
                IsUnlocked = true,
            });
            save.NaturalResourcePatches.Add(new NaturalResourcePatchSave
            {
                Id = building.Id,
                ParcelId = parcelId,
                ResourceType = ResourceType.Wood.ToString(),
                LegacyStorageBuildingId = building.Id,
                UnitReserves = new List<int>(building.WoodUnitReserves),
            });
            parcelId++;
        }
        save.Version = 8;
        return save;
    }

    public static WorldSave MigrateV8ToV9(WorldSave save)
    {
        ArgumentNullException.ThrowIfNull(save);
        if (save.Version != 8)
        {
            throw new InvalidOperationException(
                $"MigrateV8ToV9 expects version 8 but found {save.Version}.");
        }
        save.ParcelPlacements ??= new List<ParcelPlacementSave>();
        save.ParcelPlacements.Clear();

        var entities = new List<(int Id, string ProfileId)>();
        foreach (BuildingSave building in save.Buildings)
        {
            if (building.Kind == BuildingKind.Forest.ToString()) continue;
            BuildingKind kind = Enum.TryParse(
                building.Kind,
                ignoreCase: true,
                out BuildingKind parsedKind)
                ? parsedKind
                : BuildingKind.Home;
            entities.Add((building.Id, BuildingFootprintCatalog.ProfileIdFor(kind)));
        }
        foreach (ConstructionProjectSave project in save.Projects)
        {
            ConstructionKind kind = Enum.TryParse(
                project.Kind,
                ignoreCase: true,
                out ConstructionKind parsedKind)
                ? parsedKind
                : ConstructionKind.BasicShelter;
            entities.Add((project.Id, BuildingFootprintCatalog.ProfileIdFor(kind)));
        }

        int requiredParcels = Math.Max(
            1,
            (entities.Count + ParcelGrid.LotsPerAxis * ParcelGrid.LotsPerAxis - 1)
                / (ParcelGrid.LotsPerAxis * ParcelGrid.LotsPerAxis));
        int nextParcelId = save.Parcels.Count == 0
            ? 1
            : save.Parcels.Max(parcel => parcel.Id) + 1;
        while (save.Parcels.Count < requiredParcels)
        {
            save.Parcels.Add(new ParcelSave
            {
                Id = nextParcelId++,
                LogicalColumn = save.Parcels.Count,
                LogicalRow = 0,
                IsUnlocked = true,
            });
        }
        List<ParcelSave> unlocked = save.Parcels
            .Where(parcel => parcel.IsUnlocked)
            .OrderBy(parcel => parcel.Id)
            .ToList();
        while (unlocked.Count < requiredParcels)
        {
            var parcel = new ParcelSave
            {
                Id = nextParcelId++,
                LogicalColumn = save.Parcels.Count,
                LogicalRow = 0,
                IsUnlocked = true,
            };
            save.Parcels.Add(parcel);
            unlocked.Add(parcel);
        }

        for (int index = 0; index < entities.Count; index++)
        {
            int parcelIndex = index / 9;
            int lotIndex = index % 9;
            save.ParcelPlacements.Add(new ParcelPlacementSave
            {
                EntityId = entities[index].Id,
                ParcelId = unlocked[parcelIndex].Id,
                LotColumn = lotIndex % ParcelGrid.LotsPerAxis,
                LotRow = lotIndex / ParcelGrid.LotsPerAxis,
                LotWidth = 1,
                LotHeight = 1,
                FootprintProfileId = entities[index].ProfileId,
                Orientation = BuildingOrientation.South.ToString(),
            });
        }
        save.Version = 9;
        return save;
    }

    public static WorldSave MigrateV9ToV10(WorldSave save)
    {
        ArgumentNullException.ThrowIfNull(save);
        if (save.Version != 9)
        {
            throw new InvalidOperationException(
                $"MigrateV9ToV10 expects version 9 but found {save.Version}.");
        }
        foreach (NaturalResourcePatchSave patch in save.NaturalResourcePatches)
        {
            if (!string.Equals(
                patch.ResourceType,
                ResourceType.Wood.ToString(),
                StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            int remaining = patch.UnitReserves.Sum();
            patch.UnitReserves.Clear();
            while (remaining > 0
                && patch.UnitReserves.Count < NaturalResourcePatch.MaximumUnits)
            {
                int reserve = Math.Min(remaining, CityWorld.StartingTreeWoodReserve);
                patch.UnitReserves.Add(reserve);
                remaining -= reserve;
            }
            if (remaining > 0)
            {
                throw new InvalidOperationException(
                    $"Natural-resource patch {patch.Id} exceeds its physical lot capacity.");
            }
            if (patch.LegacyStorageBuildingId is not int storageId) continue;
            BuildingSave? storage = save.Buildings.FirstOrDefault(
                building => building.Id == storageId);
            if (storage is null) continue;
            storage.WoodUnitReserves = new List<int>(patch.UnitReserves);
            storage.WoodReserve = patch.UnitReserves.Sum();
        }
        save.Version = 10;
        return save;
    }

    public static WorldSave MigrateV13ToV14(WorldSave save)
    {
        if (save.Version != 13)
        {
            throw new InvalidOperationException(
                $"MigrateV13ToV14 expects version 13 but found {save.Version}.");
        }
        // The Forest building entity is a compatibility adapter for
        // pre-v8 saves. Modern cities own wood through
        // NaturalResourcePatch + CityInventory; the building itself
        // only exists because legacy readers expect it. v14 removes
        // it. Patches keep their LegacyStorageBuildingId for a
        // release cycle so older saves still round-trip if a player
        // downgrades; the value is ignored at runtime.
        save.Buildings.RemoveAll(building =>
            string.Equals(building.Kind, BuildingKind.Forest.ToString(),
                StringComparison.OrdinalIgnoreCase));
        save.Version = 14;
        return save;
    }

    /// <summary>
    /// Upgrades a v14 save to v15: expeditions gain a real 1-2 citizen team
    /// instead of a single lead. Every v14 expedition had exactly one
    /// dispatched citizen, so <see cref="ExpeditionSave.MemberCitizenIds"/>
    /// becomes a single-element list built from the legacy
    /// <see cref="ExpeditionSave.LeadCitizenId"/>, which is left in place
    /// (now redundant with the first member) for any tool still reading it.
    /// </summary>
    public static WorldSave MigrateV14ToV15(WorldSave save)
    {
        ArgumentNullException.ThrowIfNull(save);
        if (save.Version != 14)
        {
            throw new InvalidOperationException(
                $"MigrateV14ToV15 expects version 14 but found {save.Version}.");
        }
        foreach (ExpeditionSave expedition in save.Expeditions)
        {
            if (expedition is null) continue;
            if (expedition.MemberCitizenIds is null || expedition.MemberCitizenIds.Count == 0)
            {
                expedition.MemberCitizenIds = new List<int> { expedition.LeadCitizenId };
            }
        }
        save.Version = 15;
        return save;
    }

    /// <summary>
    /// Upgrades a v15 save to v16: expeditions gain a persisted phase and
    /// encounter outcome. Every v15 expedition still active defaults to
    /// <see cref="ExpeditionPhase.Outbound"/> — its encounter simply
    /// resolves (deterministically, from the expedition's own persisted id
    /// and start tick) the next time the world advances, however far along
    /// the journey actually was. An already-finished expedition (Returned/
    /// Failed/Cancelled) defaults to <see cref="ExpeditionPhase.Resolved"/>
    /// since nothing more will ever advance it.
    /// </summary>
    public static WorldSave MigrateV15ToV16(WorldSave save)
    {
        ArgumentNullException.ThrowIfNull(save);
        if (save.Version != 15)
        {
            throw new InvalidOperationException(
                $"MigrateV15ToV16 expects version 15 but found {save.Version}.");
        }
        foreach (ExpeditionSave expedition in save.Expeditions)
        {
            if (expedition is null) continue;
            if (string.IsNullOrEmpty(expedition.Phase))
            {
                bool stillActive = string.Equals(
                    expedition.Status, ExpeditionStatus.Active.ToString(), StringComparison.OrdinalIgnoreCase);
                expedition.Phase = (stillActive ? ExpeditionPhase.Outbound : ExpeditionPhase.Resolved).ToString();
            }
        }
        save.Version = 16;
        return save;
    }

    /// <summary>
    /// Upgrades a v16 save to v17 by recognising every citizen previously
    /// dispatched on an expedition as a hero. Dispatch was already an
    /// explicit player command, so the migration preserves that authorization
    /// instead of invalidating the expedition or silently dropping members.
    /// The earliest retained expedition start tick becomes the role grant.
    /// </summary>
    public static WorldSave MigrateV16ToV17(WorldSave save)
    {
        ArgumentNullException.ThrowIfNull(save);
        if (save.Version != 16)
        {
            throw new InvalidOperationException(
                $"MigrateV16ToV17 expects version 16 but found {save.Version}.");
        }

        foreach (ExpeditionSave expedition in save.Expeditions.OrderBy(e => e.StartTick))
        {
            if (expedition?.MemberCitizenIds is null) continue;
            foreach (int memberId in expedition.MemberCitizenIds)
            {
                CitizenSave? citizen = save.Citizens.FirstOrDefault(c => c.Id == memberId);
                if (citizen is null
                    || citizen.Roles.Any(role => role.Id == RoleId.Hero.Value))
                {
                    continue;
                }
                citizen.Roles.Add(new RoleSave
                {
                    Id = RoleId.Hero.Value,
                    GrantedAtTick = Math.Max(0, expedition.StartTick),
                });
            }
        }

        save.Version = 17;
        return save;
    }

    public static WorldSave MigrateV17ToV18(WorldSave save)
    {
        ArgumentNullException.ThrowIfNull(save);
        if (save.Version != 17)
        {
            throw new InvalidOperationException(
                $"MigrateV17ToV18 expects version 17 but found {save.Version}.");
        }

        foreach (ExpeditionSave expedition in save.Expeditions)
        {
            if (expedition is null) continue;
            expedition.RetreatPosture =
                ExpeditionRetreatPosture.ContinueAfterSetback.ToString();
            WorldEventSave? dispatch = save.Events.LastOrDefault(evt =>
                evt.Tick == expedition.StartTick
                && string.Equals(
                    evt.Kind,
                    WorldEventKind.ExpeditionDispatched.ToString(),
                    StringComparison.OrdinalIgnoreCase)
                && evt.SubjectEntityId == expedition.Id);
            expedition.DispatchEventId = dispatch?.Id;
        }
        save.Version = 18;
        return save;
    }

    /// <summary>
    /// Adds the EG-0 opening measurement. An existing city keeps playing
    /// exactly as before: the measurement starts empty because that city's
    /// opening was never instrumented, and inventing retroactive numbers would
    /// poison the dataset EG-0 exists to collect. Zero
    /// <see cref="EarlyGameMetricsSave.DawnSamples"/> is the marker that
    /// separates a migrated city from a measured one.
    /// </summary>
    public static WorldSave MigrateV19ToV20(WorldSave save)
    {
        ArgumentNullException.ThrowIfNull(save);
        if (save.Version != 19)
        {
            throw new InvalidOperationException(
                $"MigrateV19ToV20 expects version 19 but found {save.Version}.");
        }
        save.EarlyGameMetrics = new EarlyGameMetricsSave();
        save.Version = 20;
        return save;
    }

    /// <summary>
    /// EG-1 resource seam migration. v20 → v21 only bumps the version
    /// number; the four new resource kinds (Branches, Plant Fiber,
    /// Small Stone, Wild Food) live in the existing
    /// <see cref="ResourceType"/> enum and the existing
    /// <see cref="NaturalResourcePatch"/> already carries any of them
    /// after the v21 enum extension. New cities gain the four patches
    /// via <see cref="CityWorld.SeedStartingOpportunities"/> at
    /// load time; legacy cities gain them only if parcels 3–6 are
    /// free, which is the safe default. The carried cap of six units
    /// is enforced at gather time, not at save time, so an existing
    /// save with more than six carried units is honoured on load and
    /// only stopped from growing further.
    /// </summary>
    public static WorldSave MigrateV20ToV21(WorldSave save)
    {
        ArgumentNullException.ThrowIfNull(save);
        if (save.Version != 20)
        {
            throw new InvalidOperationException(
                $"MigrateV20ToV21 expects version 20 but found {save.Version}.");
        }
        save.Version = 21;
        return save;
    }

    public static WorldSave MigrateV18ToV19(WorldSave save)
    {
        ArgumentNullException.ThrowIfNull(save);
        if (save.Version != 18)
        {
            throw new InvalidOperationException(
                $"MigrateV18ToV19 expects version 18 but found {save.Version}.");
        }
        foreach (CitizenSave citizen in save.Citizens)
        {
            citizen.WoundSeverity = null;
            citizen.WoundOriginatingEventId = null;
            citizen.WoundRecoveryTicksRemaining = 0;
        }
        foreach (ParcelSave parcel in save.Parcels)
        {
            parcel.TerritoryState = parcel.IsUnlocked
                ? ParcelTerritoryState.Available.ToString()
                : ParcelTerritoryState.Locked.ToString();
        }
        if (save.Parcels.All(parcel => parcel.IsUnlocked))
        {
            int targetId = save.Parcels.Count == 0
                ? 1
                : save.Parcels.Max(parcel => parcel.Id) + 1;
            int targetColumn = save.Parcels.Count == 0
                ? 0
                : save.Parcels.Max(parcel => parcel.LogicalColumn) + 1;
            save.Parcels.Add(new ParcelSave
            {
                Id = targetId,
                LogicalColumn = targetColumn,
                LogicalRow = 0,
                IsUnlocked = false,
                TerritoryState = ParcelTerritoryState.Locked.ToString(),
            });
        }
        int? firstTargetParcelId = save.Parcels
            .Where(parcel => !parcel.IsUnlocked)
            .OrderBy(parcel => parcel.LogicalRow)
            .ThenBy(parcel => parcel.LogicalColumn)
            .Select(parcel => (int?)parcel.Id)
            .FirstOrDefault();
        foreach (ExpeditionSave expedition in save.Expeditions)
        {
            if (string.Equals(
                expedition.RewardKind,
                ExpeditionRewardKind.Supplies.ToString(),
                StringComparison.OrdinalIgnoreCase))
            {
                expedition.TargetParcelId = firstTargetParcelId;
            }
        }
        save.Version = 19;
        return save;
    }

    public static WorldSave MigrateV10ToV11(WorldSave save)
    {
        ArgumentNullException.ThrowIfNull(save);
        if (save.Version != 10)
        {
            throw new InvalidOperationException(
                $"MigrateV10ToV11 expects version 10 but found {save.Version}.");
        }
        save.CityInventory ??= new Dictionary<string, int>();
        int wood = 0;
        foreach (BuildingSave building in save.Buildings)
        {
            if (!string.Equals(
                building.Kind,
                BuildingKind.Forest.ToString(),
                StringComparison.OrdinalIgnoreCase)
                || building.Stock <= 0)
            {
                continue;
            }
            wood = checked(wood + building.Stock);
            building.Stock = 0;
        }
        if (wood > 0)
        {
            save.CityInventory.TryGetValue(ResourceType.Wood.ToString(), out int existing);
            save.CityInventory[ResourceType.Wood.ToString()] = checked(existing + wood);
        }
        save.Version = 11;
        return save;
    }

    public static WorldSave MigrateV11ToV12(WorldSave save)
    {
        if (save.Version != 11)
        {
            throw new InvalidOperationException(
                $"MigrateV11ToV12 expects version 11 but found {save.Version}.");
        }
        foreach (CitizenSave citizen in save.Citizens)
        {
            citizen.AppearanceVariant = string.IsNullOrEmpty(citizen.AppearanceVariant)
                ? AppearanceVariantId.Standard.Value
                : citizen.AppearanceVariant;
        }
        save.Version = 12;
        return save;
    }

    public static WorldSave MigrateV12ToV13(WorldSave save)
    {
        if (save.Version != 12)
        {
            throw new InvalidOperationException(
                $"MigrateV12ToV13 expects version 12 but found {save.Version}.");
        }
        save.Expeditions ??= new List<ExpeditionSave>();
        save.Version = 13;
        return save;
    }
}
