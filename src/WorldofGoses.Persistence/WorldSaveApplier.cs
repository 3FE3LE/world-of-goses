#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using WorldofGoses.Domain;
using WorldofGoses.Domain.Combat;
using WorldofGoses.Persistence.Ids;

namespace WorldofGoses.Persistence;

/// <summary>
/// Replays a <see cref="WorldSave"/> onto a live <see cref="CityWorld"/>.
///
/// <para>Architecture Hardening A6 relocated the restore logic from
/// <c>CityWorld</c> (Domain) into this class (Persistence). Domain does
/// not reference Persistence and therefore cannot accept a
/// <see cref="WorldSave"/> parameter. The applier drives the world
/// through the internal seam exposed on <see cref="CityWorld"/>'s
/// <c>internal</c> fields and helpers (granted via
/// <c>InternalsVisibleTo</c>), which preserves every save/load
/// invariant from before the move.</para>
///
/// <para>The algorithm is unchanged from the pre-A6 implementation: a
/// fresh <c>CityWorld</c> runs <see cref="ApplyTo"/> against the save,
/// validation runs first, then a complete rehydration into an isolated
/// preflight candidate catches deterministic mismatches before the
/// live world is cleared. No JSON shape, schema version, or migration
/// semantic was touched.</para>
/// </summary>
internal static class WorldSaveApplier
{
    /// <summary>
    /// Replaces <paramref name="world"/>'s contents with the contents
    /// of <paramref name="save"/>. Validates the save first, then
    /// performs a complete rehydration on an isolated candidate so
    /// any deterministic mismatch fails before the live world is
    /// cleared.
    /// </summary>
    internal static void ApplyTo(CityWorld world, WorldSave save)
    {
        WorldPersistence.Validate(save);
        // Replay validation depends on reconstructed citizens and combatants.
        // Run the complete rehydration on an isolated candidate first so any
        // deterministic mismatch fails before this live world is cleared.
        var preflight = new CityWorld();
        ApplyValidatedTo(preflight, save);
        ApplyValidatedTo(world, save);
    }

    /// <summary>Builds a fresh <see cref="CityWorld"/> from a validated snapshot.</summary>
    internal static CityWorld FromSave(WorldSave save)
    {
        var world = new CityWorld();
        ApplyTo(world, save);
        return world;
    }

    private static void ApplyValidatedTo(CityWorld world, WorldSave save)
    {
        // Restoring re-deposits every stored resource through the ledger.
        // Without this the load itself would be booked as gathering, and the
        // figures would grow with every relaunch — exactly the behaviour the
        // EG-0 report cannot tolerate, since the playtest of EG-1+ depends on
        // the metric being accurate across sessions.
        world._resources.ObserveFlows(null);
        world._citizens.Clear();
        world._buildings.Clear();
        world._projects.Clear();
        world._cultivationSites.Clear();
        world._parcels.Clear();
        world._naturalResourcePatches.Clear();
        world._parcelPlacements.Clear();
        world._corridorReservations.Clear();
        world._resourceOpportunities.Clear();
        world._log.Clear();
        world._resources.ClearReservations();
        world._nextProjectId = 1;
        world._nextCorridorReservationId = 1;
        world._tick = save.CurrentTick;

        foreach (ParcelSave parcel in save.Parcels)
        {
            var restoredParcel = new CityParcel(
                new ParcelId(parcel.Id),
                parcel.LogicalColumn,
                parcel.LogicalRow,
                ParcelTerritoryStateSaveIds.TryParse(
                    parcel.TerritoryState,
                    out ParcelTerritoryState territoryState)
                        ? territoryState
                        : parcel.IsUnlocked
                            ? ParcelTerritoryState.Available
                            : ParcelTerritoryState.Locked);
            world._parcels.Add(restoredParcel.Id, restoredParcel);
        }

        foreach (var bs in save.Buildings)
        {
            var kind = BuildingKindSaveIds.TryParse(bs.Kind, out var parsed)
                ? parsed
                : BuildingKind.Quarry;
            var resource = ResourceTypeSaveIds.TryParse(bs.ProducedResourceType, out var pres)
                ? pres
                : ResourceType.Stone;
            var competency = string.IsNullOrEmpty(bs.ProducedCompetencyId)
                ? CompetencyId.Mining
                : new CompetencyId(bs.ProducedCompetencyId);

            int balancedStorageCapacity = save.EconomicBalanceVersion == 0
                ? kind switch
                {
                    BuildingKind.Farm => Math.Max(bs.StorageCapacity, CityEconomyRules.FarmStorageCapacity),
                    BuildingKind.Quarry => Math.Max(bs.StorageCapacity, CityEconomyRules.QuarryStorageCapacity),
                    _ => bs.StorageCapacity,
                }
                : bs.StorageCapacity;
            int balancedBaseProduction = save.EconomicBalanceVersion == 0
                && kind == BuildingKind.Quarry
                ? Math.Min(bs.BaseProductionPerWorker, 1)
                : bs.BaseProductionPerWorker;
            var building = new Building(
                id: new BuildingId(bs.Id),
                displayName: bs.DisplayName,
                kind: kind,
                producedResourceType: resource,
                producedCompetencyId: competency,
                workerCapacity: bs.WorkerCapacity,
                visualCapacity: bs.VisualCapacity,
                baseProductionPerWorker: balancedBaseProduction,
                storageCapacity: balancedStorageCapacity,
                resourceLabel: string.IsNullOrEmpty(bs.ResourceLabel) ? "Resource" : bs.ResourceLabel,
                resourceUnit: string.IsNullOrEmpty(bs.ResourceUnit) ? "units" : bs.ResourceUnit,
                initialStock: bs.Stock,
                productionEnabled: bs.ProductionEnabled);
            // v3 fields default to (0, StorageCapacity, 0) for v2 saves
            // that predate the policy triplet. A legacy TargetStock is
            // treated as MaxStock so old saves behave identically.
            int savedMaxStock = bs.MaxStock ?? bs.TargetStock ?? bs.StorageCapacity;
            int maxStock = savedMaxStock == bs.StorageCapacity
                ? balancedStorageCapacity
                : savedMaxStock;
            int minStock = bs.MinStock ?? 0;
            int priority = bs.Priority ?? 0;
            building.ConfigureProductionPolicy(bs.ProductionEnabled, minStock, maxStock, priority);

            // Old saves predate the wood-gathering slice and have no
            // WoodReserve field; for Forest plots, seed them with
            // the starting reserve so the saving doesn't auto-demolish
            // them on the first tick. Fresh worlds (already carrying
            // a WoodReserve) preserve their state.
            if (kind == BuildingKind.Forest && bs.WoodUnitReserves is { Count: > 0 })
            {
                building.RestoreWoodUnits(bs.WoodUnitReserves);
            }
            else if (kind == BuildingKind.Forest && bs.WoodReserve is null)
            {
                building.SeedWoodReserve(CityWorld.StartingForestWoodReserve);
                if (bs.WorkerCapacity == 0)
                {
                    // Old saves serialised Forest with capacity 0 (a
                    // marker for "non-productive in v2"). Re-apply the
                    // v4 defaults so the player can assign workers.
                    building.ReplaceForestCapacity(
                        workerCapacity: 2,
                        visualCapacity: 2,
                        baseProductionPerWorker: 1);
                }
            }
            else
            {
                building.SeedWoodReserve(bs.WoodReserve ?? 0);
            }
            building.DepositIron(bs.IronStock);
            if (bs.FoundingSiteOriginModules is { Count: > 0 })
            {
                var originModules = new List<FoundingSiteModule>();
                foreach (string savedModule in bs.FoundingSiteOriginModules)
                {
                    if (FoundingSiteModuleSaveIds.TryParse(savedModule, out FoundingSiteModule module))
                    {
                        originModules.Add(module);
                    }
                }
                building.RestoreFoundingSiteOriginModules(originModules);
            }

            world.RegisterBuilding(building, placeIfMissing: false);

            foreach (var cid in bs.AssignedCitizenIds)
            {
                // Building.TryAssign is internal — same-assembly access.
                building.TryAssign(new CitizenId(cid));
            }
        }

        foreach (NaturalResourcePatchSave patch in save.NaturalResourcePatches)
        {
            ResourceType type = ResourceTypeSaveIds.TryParse(
                patch.ResourceType,
                out ResourceType parsedType)
                ? parsedType
                : ResourceType.Wood;
            world.RegisterNaturalResourcePatch(new NaturalResourcePatch(
                patch.Id,
                new ParcelId(patch.ParcelId),
                type,
                patch.UnitReserves,
                patch.LegacyStorageBuildingId.HasValue
                    ? new BuildingId(patch.LegacyStorageBuildingId.Value)
                    : null,
                patch.UnitPositions.Select(position =>
                    new NaturalResourceUnitPosition(
                        position.RowWithinParcel,
                        position.FrontageColumnWithinParcel))));
        }
        world.EnsureFoundingParcels();

        foreach (var cs in save.Citizens)
        {
            // Old saves (no StaminaMax) restore to full stamina;
            // new saves (StaminaMax present) restore the saved current.
            int? maxStamina = cs.StaminaMax;
            int? initialStamina = maxStamina.HasValue ? cs.StaminaCurrent : (int?)null;
            CitizenProfile restoredProfile = WorldPersistence.RestoreProfile(cs.Profile!);
            EquipmentLoadout restoredLoadout = WorldPersistence.RestoreEquipmentLoadout(cs.EquipmentLoadout);
            CurrentHealthAndCondition? restoredHealth = cs.CurrentHealthAndCondition switch
            {
                null => null,
                { CurrentHealth: null, ConditionFactor: null } => CurrentHealthAndCondition.Unresolved,
                { CurrentHealth: double health, ConditionFactor: double factor } =>
                    new CurrentHealthAndCondition(health, factor),
                _ => throw new InvalidOperationException(
                    $"Citizen {cs.Id}: health and condition must both be present or both be unresolved."),
            };
            IEnumerable<CompetencyProgress> restoredWeaponCompetencies =
                (cs.WeaponCompetencies ?? new List<WeaponCompetencySave>()).Select(entry =>
                    new CompetencyProgress(
                        Enum.Parse<WeaponFamily>(entry.Family, ignoreCase: true),
                        entry.Level,
                        entry.Experience));
            var citizen = new Citizen(
                new CitizenId(cs.Id),
                cs.Name,
                cs.AppearanceSeed,
                profile: restoredProfile,
                initialStamina: initialStamina,
                maxStamina: maxStamina,
                initialWellFedTicks: cs.WellFedRemainingTicks,
                appearanceVariant: string.IsNullOrEmpty(cs.AppearanceVariant)
                    ? (AppearanceVariantId?)null
                    : new AppearanceVariantId(cs.AppearanceVariant),
                origin: CitizenOriginSaveIds.TryParse(
                    cs.Origin,
                    out CitizenOrigin origin)
                        ? origin
                        : cs.Roles.Any(role => role.Id == RoleId.Hero.Value)
                            ? CitizenOrigin.AstralFounder
                        : CitizenOrigin.Mortal,
                equipmentLoadout: restoredLoadout,
                currentHealthAndCondition: restoredHealth,
                weaponCompetencies: restoredWeaponCompetencies);
            // #26: the item registry and the equipped id are the durable
            // facts; the loadout above is the projection rebuilt from them.
            // Restoring the registry after construction keeps the citizen's
            // constructor free of an equipment concept it does not own.
            WorldPersistence.RestorePersonalEquipment(citizen, cs.PersonalEquipment);
            CitizenCommitment commitment = RestoreCitizenCommitment(save, cs);
            CitizenVitalStatus vitalStatus = CitizenVitalStatusSaveIds.TryParse(
                cs.VitalStatus,
                out CitizenVitalStatus restoredVitalStatus)
                    ? restoredVitalStatus
                    : CitizenVitalStatus.Stable;
            citizen.RestoreCommitment(
                commitment,
                RestoreCitizenWorkOrder(save, cs),
                vitalStatus,
                cs.ResumeWorkNotBeforeTick);
            if (!string.IsNullOrWhiteSpace(cs.WoundSeverity)
                && cs.WoundOriginatingEventId is int woundEventId
                && WoundSeveritySaveIds.TryParse(
                    cs.WoundSeverity,
                    out WoundSeverity woundSeverity))
            {
                citizen.RestoreWound(new CitizenWound(
                    woundSeverity,
                    new WorldEventId(woundEventId),
                    cs.WoundRecoveryTicksRemaining));
            }
            if (cs.LastVisitedResourceBuildingId.HasValue
                && cs.LastVisitedResourceUnitId.HasValue)
            {
                citizen.VisitResource(
                    new BuildingId(cs.LastVisitedResourceBuildingId.Value),
                    cs.LastVisitedResourceUnitId.Value,
                    cs.LastVisitedResourcePositionIndex
                        ?? world.ResourcePositionIndex(
                            new BuildingId(cs.LastVisitedResourceBuildingId.Value),
                            cs.LastVisitedResourceUnitId.Value));
            }
            else if (cs.LastVisitedResourcePatchId.HasValue
                && cs.LastVisitedResourceUnitId.HasValue
                && cs.LastVisitedResourcePositionIndex.HasValue)
            {
                citizen.VisitResource(
                    cs.LastVisitedResourcePatchId.Value,
                    cs.LastVisitedResourceUnitId.Value,
                    cs.LastVisitedResourcePositionIndex.Value);
            }

            foreach (var entry in cs.Competencies)
            {
                citizen.AddExperience(new CompetencyId(entry.Id), entry.Experience);
            }

            foreach (var role in cs.Roles)
            {
                citizen.GrantRole(new RoleId(role.Id), role.GrantedAtTick);
            }

            world.RegisterCitizen(citizen);
        }

        if (save.Projects is { Count: > 0 })
        {
            foreach (var ps in save.Projects)
            {
                var kind = ConstructionKindSaveIds.TryParse(ps.Kind, out var parsed)
                    ? parsed
                    : ConstructionKind.BasicShelter;
                var project = new ConstructionProject(
                    id: new BuildingId(ps.Id),
                    kind: kind,
                    displayName: string.IsNullOrEmpty(ps.DisplayName) ? "Basic Shelter" : ps.DisplayName,
                    requiredWork: ps.RequiredWork,
                    workerCapacity: ps.WorkerCapacity,
                    enabled: ps.Enabled)
                {
                    Progress = ps.Progress,
                    StopCause = ConstructionStopCause.Paused,
                };
                // Restore material drawdown state. v2 saves without
                // these fields default to "fully spent" (empty) — the
                // resumed project simply runs without any per-interval
                // drawdown, which matches the pre-v3 behaviour exactly.
                var remaining = new List<RecipeInput>();
                if (ps.RemainingInputs is { Count: > 0 })
                {
                    foreach (var pair in ps.RemainingInputs)
                    {
                        if (ResourceTypeSaveIds.TryParse(pair.Key, out var res)
                            && pair.Value > 0)
                        {
                            remaining.Add(new RecipeInput(res, pair.Value));
                        }
                    }
                }
                project.SetRemainingInputs(remaining);
                var deposited = new List<RecipeInput>();
                if (ps.DepositedInputs is { Count: > 0 })
                {
                    foreach (var pair in ps.DepositedInputs)
                    {
                        if (ResourceTypeSaveIds.TryParse(pair.Key, out ResourceType resource)
                            && pair.Value > 0)
                        {
                            deposited.Add(new RecipeInput(resource, pair.Value));
                        }
                    }
                }
                var completedModules = new List<FoundingSiteModule>();
                if (ps.CompletedFoundingModules is { Count: > 0 })
                {
                    foreach (string savedModule in ps.CompletedFoundingModules)
                    {
                        if (FoundingSiteModuleSaveIds.TryParse(savedModule, out FoundingSiteModule module))
                        {
                            completedModules.Add(module);
                        }
                    }
                }
                FoundingSiteModule? activeModule = FoundingSiteModuleSaveIds.TryParse(
                    ps.ActiveFoundingModule,
                    out FoundingSiteModule parsedModule)
                        ? parsedModule
                        : null;
                project.RestoreFoundingState(
                    activeModule,
                    completedModules,
                    ps.PhaseStartedAtTick,
                    deposited);
                world.RegisterProject(project);
                foreach (var cid in ps.AssignedCitizenIds)
                {
                    project.TryAssign(new CitizenId(cid));
                }
                if (ps.Id >= world._nextProjectId) world._nextProjectId = ps.Id + 1;
            }
        }

        foreach (CultivationSiteSave savedSite in save.CultivationSites)
        {
            _ = CultivationPlotStateSaveIds.TryParse(
                savedSite.State,
                out CultivationPlotState state);
            world.RegisterCultivationSite(new CultivationSite(
                new BuildingId(savedSite.Id),
                state,
                savedSite.PlantedTick,
                savedSite.ReadyAtTick));
            if (savedSite.Id >= world._nextProjectId) world._nextProjectId = savedSite.Id + 1;
        }

        foreach (CorridorReservationSave corridor in save.CorridorReservations)
        {
            world.RegisterCorridorReservation(new CorridorReservation(
                corridor.Id,
                new ConstructionRowId(corridor.RowId),
                corridor.StartColumn,
                corridor.FrontageColumns));
        }

        foreach (ParcelPlacementSave placement in save.ParcelPlacements)
        {
            BuildingOrientation orientation = BuildingOrientationSaveIds.TryParse(
                placement.Orientation,
                out BuildingOrientation parsedOrientation)
                ? parsedOrientation
                : BuildingOrientation.South;
            var restoredPlacement = new ParcelPlacement(
                new BuildingId(placement.EntityId),
                new ParcelId(placement.ParcelId),
                new ConstructionRowId(placement.RowId),
                placement.StartColumn,
                placement.FrontageColumns,
                placement.DepthRows,
                placement.BaseFrontageColumns,
                placement.LeftExpansionColumns,
                placement.RightExpansionColumns,
                placement.LotColumn,
                placement.LotRow,
                placement.LotWidth,
                placement.LotHeight,
                placement.FootprintProfileId,
                orientation);
            bool overlapsResource = false;
            for (int column = restoredPlacement.StartColumn;
                 column < restoredPlacement.StartColumn + restoredPlacement.FrontageColumns;
                 column++)
            {
                if (!world.NaturalResourceOccupiesFrontageCell(restoredPlacement.RowId, column)) continue;
                overlapsResource = true;
                break;
            }
            if (overlapsResource)
            {
                restoredPlacement = world.FindFirstAvailablePlacement(
                    restoredPlacement.EntityId,
                    restoredPlacement.FootprintProfileId)
                    ?? throw new InvalidOperationException(
                        $"No resource-free parcel lot is available for entity "
                        + $"{restoredPlacement.EntityId.Value}.");
            }
            world.RegisterParcelPlacement(restoredPlacement);
        }

        // Citizens are constructed with CurrentLocation = AtHome
        // (the default). If the saved tick is mid-cycle — neither
        // exactly at a sunrise nor a sunset — the next mobilisation
        // wouldn't fire until the clock crosses the boundary, leaving
        // everyone visibly at home even though the time-of-day is
        // daytime. Seed the initial location from the saved tick so
        // the visualisation matches reality from the very first frame.
        if (GameClock.IsDaytime(world._tick))
        {
            world.MobiliseForDay();
        }
        else
        {
            world.MobiliseForNight();
        }
        foreach (CitizenSave savedCitizen in save.Citizens)
        {
            if (string.IsNullOrWhiteSpace(savedCitizen.CurrentLocation)
                || !world._citizens.TryGetValue(new CitizenId(savedCitizen.Id), out Citizen? citizen)
                || !CitizenLocationSaveIds.TryParse(
                    savedCitizen.CurrentLocation,
                    out CitizenLocation savedLocation))
            {
                continue;
            }
            // An in-city journey always carries the tick it began on, so the
            // remaining distance is recomputed rather than replayed from the
            // origin. A pre-v-field save that lost it falls back to "now",
            // which costs at most one AbstractTravelTicks of walking.
            //
            // An expedition traveller is a different shape: InTransit with no
            // start tick, because the expedition owns the timing. That absence
            // has to survive the round trip — see Citizen.RestoreLocation for
            // what stamping a start tick onto them used to do.
            bool travelsWithTheCity =
                savedLocation == CitizenLocation.InTransit
                && citizen.Commitment.Kind != CitizenCommitmentKind.Expedition;
            citizen.RestoreLocation(
                savedLocation,
                travelsWithTheCity
                    ? savedCitizen.TransitStartedAtTick ?? world._tick
                    : savedCitizen.TransitStartedAtTick,
                travelsWithTheCity && savedCitizen.IsReturningHome);
        }

        var restoredEvents = new List<WorldEvent>(save.Events.Count);
        foreach (var evt in save.Events)
        {
            _ = WorldEventKindSaveIds.TryParse(evt.Kind, out WorldEventKind kind);
            _ = WorldEventSubjectKindSaveIds.TryParse(evt.SubjectKind, out WorldEventSubjectKind subjectKind);
            restoredEvents.Add(new WorldEvent(
                new WorldEventId(evt.Id),
                evt.Tick,
                kind,
                new WorldEventSubject(subjectKind, evt.SubjectEntityId, evt.SubjectDisplayName),
                evt.Amount,
                evt.CauseEventId is int causeId ? new WorldEventId(causeId) : null));
        }
        world._log.Restore(restoredEvents);

        var restoredReservations = new List<ResourceReservation>(save.ResourceReservations.Count);
        foreach (var reservation in save.ResourceReservations)
        {
            _ = ResourceTypeSaveIds.TryParse(reservation.Resource, out ResourceType resource);
            _ = ResourceReservationOwnerKindSaveIds.TryParse(reservation.OwnerKind,
                out ResourceReservationOwnerKind ownerKind);
            restoredReservations.Add(new ResourceReservation(
                new ResourceReservationId(reservation.Id),
                resource,
                reservation.Amount,
                new ResourceReservationOwner(ownerKind, reservation.OwnerEntityId)));
        }
        world._resources.RestoreReservations(restoredReservations);
        var restoredInventory = new Dictionary<ResourceType, int>();
        foreach ((string key, int amount) in save.CityInventory)
        {
            _ = ResourceTypeSaveIds.TryParse(key, out ResourceType resource);
            restoredInventory[resource] = amount;
        }
        world._inventory.Restore(restoredInventory);
        world._tools.Clear();
        foreach (string savedTool in save.Tools)
        {
            if (ToolKindSaveIds.TryParse(savedTool, out ToolKind tool)) world._tools.Add(tool);
        }

        // Validate() has already rejected an unparseable stage or an
        // inconsistent concluding tick, so this only has to reconstruct.
        world._firstNight = save.FirstNight is { } savedNight
            && Enum.TryParse(savedNight.Stage, true, out FirstNightStage savedStage)
            ? new FirstNightState(
                savedStage,
                savedNight.CurrentDialogueNodeId,
                savedNight.StartedAtTick,
                savedNight.ConcludedAtTick)
            : null;

        foreach (ResourceOpportunitySave opportunity in save.ResourceOpportunities)
        {
            _ = ResourceOpportunityKindSaveIds.TryParse(
                opportunity.Kind,
                out ResourceOpportunityKind kind);
            _ = ResourceOpportunityStateSaveIds.TryParse(
                opportunity.State,
                out ResourceOpportunityState state);
            var id = new ResourceOpportunityId(opportunity.Id);
            world._resourceOpportunities.Add(
                id,
                new ResourceOpportunity(
                    id,
                    kind,
                    state,
                    opportunity.ReservedByExpeditionId is int expeditionId
                        ? new ExpeditionId(expeditionId)
                        : null));
        }

        world._expeditions.Clear();
        world._combatSessions.Clear();
        world._nextExpeditionId = 1;
        foreach (ExpeditionSave expedition in save.Expeditions)
        {
            ExpeditionSupplyRequirement supplyRequirement =
                expedition.SupplyAmount > 0
                && ResourceTypeSaveIds.TryParse(expedition.SupplyResource, out ResourceType supply)
                    ? ExpeditionSupplyRequirement.Required(supply, expedition.SupplyAmount)
                    : ExpeditionSupplyRequirement.None;
            _ = ExpeditionStatusSaveIds.TryParse(expedition.Status, out ExpeditionStatus status);
            _ = ExpeditionRewardKindSaveIds.TryParse(
                string.IsNullOrEmpty(expedition.RewardKind)
                    ? ExpeditionRewardKindSaveIds.SuppliesId
                    : expedition.RewardKind,
                out ExpeditionRewardKind rewardKind);
            ExpeditionReward reward = rewardKind switch
            {
                ExpeditionRewardKind.Supplies
                    when ResourceTypeSaveIds.TryParse(
                        expedition.RewardResource,
                        out ResourceType rewardResource) =>
                    ExpeditionReward.Supplies(rewardResource, expedition.RewardAmount),
                ExpeditionRewardKind.Migrant => ExpeditionReward.Migrant,
                _ => ExpeditionReward.Discovery,
            };
            if (!ExpeditionPhaseSaveIds.TryParse(expedition.Phase, out ExpeditionPhase phase))
            {
                phase = ExpeditionPhase.Outbound;
            }
            ExpeditionEncounterOutcome? encounterOutcome =
                !string.IsNullOrEmpty(expedition.EncounterOutcome)
                && ExpeditionEncounterOutcomeSaveIds.TryParse(expedition.EncounterOutcome, out ExpeditionEncounterOutcome parsedOutcome)
                    ? parsedOutcome
                    : null;
            _ = ExpeditionRetreatPostureSaveIds.TryParse(
                string.IsNullOrEmpty(expedition.RetreatPosture)
                    ? ExpeditionRetreatPostureSaveIds.ContinueAfterSetbackId
                    : expedition.RetreatPosture,
                out ExpeditionRetreatPosture retreatPosture);
            WorldEventId? dispatchEventId = expedition.DispatchEventId is int eventId
                ? new WorldEventId(eventId)
                : null;
            var restored = new Expedition(
                new ExpeditionId(expedition.Id),
                expedition.DisplayName,
                expedition.MemberCitizenIds.Select(id => new CitizenId(id)).ToArray(),
                expedition.StartTick,
                expedition.EndTick,
                supplyRequirement,
                reward,
                expedition.ReservationId is int reservationId
                    ? new ResourceReservationId(reservationId)
                    : null,
                status,
                phase,
                encounterOutcome,
                retreatPosture,
                dispatchEventId,
                expedition.ReturnedAmount,
                expedition.DeliveredMigrantId is int migrantId
                    ? new CitizenId(migrantId)
                    : null,
                expedition.TargetParcelId is int targetParcelId
                    ? new ParcelId(targetParcelId)
                    : null,
                expedition.ResourceOpportunityId is int opportunityId
                    ? new ResourceOpportunityId(opportunityId)
                    : null,
                Enum.TryParse(
                    expedition.ResourceOpportunityKind,
                    true,
                    out ResourceOpportunityKind opportunityKind)
                        ? opportunityKind
                        : null,
                expedition.SetbackReturn,
                expedition.PartialReturn,
                expedition.CarryCapacity,
                expedition.ObjectiveReachedAtTick,
                expedition.CombatRulesVersion);
            world._expeditions.Add(restored.Id, restored);
            if (expedition.HasCombatSession)
            {
                var commands = new List<CombatSessionCommand>(expedition.CombatCommands.Count);
                foreach (CombatSessionCommandSave command in expedition.CombatCommands)
                {
                    _ = CombatSessionCommandKindSaveIds.TryParse(command.Kind, out CombatSessionCommandKind kind);
                    commands.Add(new CombatSessionCommand(command.BeforeStep, kind, command.Value));
                }
                CombatSession fresh = ExpeditionCombatSessionFactory.Create(restored, world._citizens);
                CombatSession restoredSession = CombatSession.Restore(
                    session: fresh,
                    stepsAdvanced: expedition.CombatStepsAdvanced,
                    commands: commands);
                CombatOutcome replayedOutcome = restoredSession.Outcome;
                if ((encounterOutcome is null && replayedOutcome != CombatOutcome.InProgress)
                    || (encounterOutcome.HasValue
                        && (replayedOutcome == CombatOutcome.InProgress
                            || CityWorld.ToExpeditionOutcome(replayedOutcome) != encounterOutcome.Value)))
                {
                    throw new InvalidOperationException(
                        $"Expedition {expedition.Id} combat replay disagrees with its encounter outcome.");
                }
                world._combatSessions.Add(
                    restored.Id,
                    restoredSession);
            }
            if (expedition.Id >= world._nextExpeditionId) world._nextExpeditionId = expedition.Id + 1;
        }
        if (save.PendingProspectSeed is int prospectSeed)
        {
            world.SetPendingProspectForRestore(new CitizenProspect(
                prospectSeed,
                string.IsNullOrWhiteSpace(save.PendingProspectName)
                    ? CityWorld.MigrantNameForSeed(prospectSeed)
                    : save.PendingProspectName,
                CityWorld.CreateMigrantProfile(prospectSeed)));
        }
        else
        {
            world.SetPendingProspectForRestore(null);
        }

        // Rehydration is finished, so resource movement means gameplay again.
        // The measurement is restored from the snapshot rather than rebuilt,
        // because the flows that produced it already happened.
        WorldPersistence.RestoreEarlyGameMetrics(world._metrics, save.EarlyGameMetrics);
        world._resources.ObserveFlows(world._metrics);
    }

    private static CitizenCommitment RestoreCitizenCommitment(
        WorldSave save,
        CitizenSave citizen)
    {
        if (!string.IsNullOrWhiteSpace(citizen.CommitmentKind)
            && Enum.TryParse(
                citizen.CommitmentKind,
                ignoreCase: true,
                out CitizenCommitmentKind explicitKind))
        {
            return new CitizenCommitment(explicitKind, citizen.CommitmentEntityId);
        }

        if (citizen.CurrentAssignment is int assignmentId)
        {
            CitizenCommitmentKind kind = save.Projects.Any(project => project.Id == assignmentId)
                ? CitizenCommitmentKind.Construction
                : CitizenCommitmentKind.BuildingWork;
            return new CitizenCommitment(kind, assignmentId);
        }

        ExpeditionSave? activeExpedition = save.Expeditions.FirstOrDefault(expedition =>
            expedition.MemberCitizenIds.Contains(citizen.Id)
            && Enum.TryParse(expedition.Status, true, out ExpeditionStatus status)
            && status == ExpeditionStatus.Active);
        return activeExpedition is null
            ? CitizenCommitment.None
            : new CitizenCommitment(
                CitizenCommitmentKind.Expedition,
                activeExpedition.Id);
    }

    private static CitizenWorkOrder? RestoreCitizenWorkOrder(
        WorldSave save,
        CitizenSave citizen)
    {
        if (!string.IsNullOrWhiteSpace(citizen.WorkOrderKind)
            && citizen.WorkOrderEntityId is int explicitEntityId
            && Enum.TryParse(
                citizen.WorkOrderKind,
                ignoreCase: true,
                out CitizenCommitmentKind explicitKind)
            && explicitKind is CitizenCommitmentKind.BuildingWork
                or CitizenCommitmentKind.Construction)
        {
            return new CitizenWorkOrder(explicitKind, new BuildingId(explicitEntityId));
        }
        if (citizen.CurrentAssignment is not int assignmentId) return null;
        CitizenCommitmentKind inferredKind = save.Projects.Any(project => project.Id == assignmentId)
            ? CitizenCommitmentKind.Construction
            : CitizenCommitmentKind.BuildingWork;
        return new CitizenWorkOrder(inferredKind, new BuildingId(assignmentId));
    }
}
