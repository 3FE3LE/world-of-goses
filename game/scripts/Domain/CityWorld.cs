#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using WorldofGoses.Domain.Combat;

namespace WorldofGoses.Domain;

/// <summary>
/// Deterministic, in-memory world state. A new world starts empty and becomes
/// active only when onboarding establishes its principal hero. Citizens and
/// buildings are then composed explicitly through domain operations or a
/// validated persisted snapshot.
///
/// The world exposes events instead of being polled by the presentation layer.
/// The presentation layer never reaches into a building or citizen to mutate
/// state directly.
/// </summary>
public sealed class CityWorld
{
    private static readonly string[] MigrantNames =
    {
        "Inara", "Tovan", "Mirel", "Sada", "Orin", "Veya", "Cael", "Neris",
    };

    // Architecture Hardening A6: these fields are `internal` (not
    // `private`) so the persistence assembly can drive `Restore` from
    // outside Domain. Domain does not depend on Persistence; Persistence
    // reaches these fields through `InternalsVisibleTo`. The fields stay
    // `readonly` for every collection that is conceptually append-only
    // for ordinary gameplay; the persistence restore seam is the only
    // operation that clears or replaces them.
    internal readonly Dictionary<CitizenId, Citizen> _citizens = new();
    internal readonly Dictionary<BuildingId, Building> _buildings = new();
    internal readonly Dictionary<BuildingId, ConstructionProject> _projects = new();
    internal readonly Dictionary<BuildingId, CultivationSite> _cultivationSites = new();
    internal readonly Dictionary<ParcelId, CityParcel> _parcels = new();
    internal readonly Dictionary<int, NaturalResourcePatch> _naturalResourcePatches = new();
    internal readonly Dictionary<BuildingId, ParcelPlacement> _parcelPlacements = new();
    internal readonly Dictionary<int, CorridorReservation> _corridorReservations = new();
    internal readonly WorldEventLog _log = new();
    internal readonly CityInventory _inventory = new();
    internal readonly CityResourceLedger _resources;
    private readonly CitizenAssignmentService _assignments;
    private readonly BuildingProductionSimulation _production;
    private readonly ConstructionSimulation _construction;
    internal readonly Dictionary<ExpeditionId, Expedition> _expeditions = new();
    internal readonly Dictionary<ExpeditionId, CombatSession> _combatSessions = new();
    internal readonly Dictionary<ResourceOpportunityId, ResourceOpportunity>
        _resourceOpportunities = new();
    internal readonly HashSet<ToolKind> _tools = new();
    internal FirstNightState? _firstNight;
    internal int _tick;
    internal int _nextProjectId = 1;
    internal int _nextExpeditionId = 1;
    internal int _nextCorridorReservationId = 1;
    internal readonly EarlyGameMetrics _metrics = new();

    private static readonly CitizenId PrincipalHeroId = new(1);

    /// <summary>A new world is intentionally empty until onboarding creates its hero.</summary>
    public CityWorld()
    {
        _resources = new CityResourceLedger(_buildings, _inventory);
        _resources.ObserveFlows(_metrics);
        _assignments = new CitizenAssignmentService(
            _citizens,
            _buildings,
            _projects,
            RaiseBuildingChanged,
            RaiseProjectChanged,
            IsLaborTime);
        _production = new BuildingProductionSimulation(
            _citizens,
            _log,
            () => _tick,
            TryConsumeOperatingInputs,
            FindCauseEvent,
            RaiseBuildingChanged);
        _construction = new ConstructionSimulation(
            _citizens,
            _log,
            () => _tick,
            TryConsumeResources,
            () => FindCauseEvent(),
            RaiseProjectChanged);
    }

    public int CurrentTick => _tick;
    public IReadOnlyDictionary<CitizenId, Citizen> Citizens => _citizens;
    public CitizenProspect? PendingProspect { get; private set; }
    internal void SetPendingProspectForRestore(CitizenProspect? prospect) => PendingProspect = prospect;
    public IReadOnlyDictionary<BuildingId, Building> Buildings => _buildings;
    public IReadOnlyDictionary<BuildingId, ConstructionProject> Projects => _projects;
    public IReadOnlyDictionary<BuildingId, CultivationSite> CultivationSites =>
        _cultivationSites;
    public IReadOnlyDictionary<ParcelId, CityParcel> Parcels => _parcels;
    public IReadOnlyDictionary<int, NaturalResourcePatch> NaturalResourcePatches =>
        _naturalResourcePatches;
    public IReadOnlyDictionary<BuildingId, ParcelPlacement> ParcelPlacements =>
        _parcelPlacements;
    public IReadOnlyDictionary<int, CorridorReservation> CorridorReservations =>
        _corridorReservations;
    public IReadOnlyDictionary<ExpeditionId, Expedition> Expeditions => _expeditions;
    public IReadOnlyDictionary<ResourceOpportunityId, ResourceOpportunity> ResourceOpportunities =>
        _resourceOpportunities;
    public IReadOnlySet<ToolKind> Tools => _tools;

    /// <summary>
    /// The authored first night, or <c>null</c> before a founder exists. Cities
    /// restored from a pre-v31 save carry a concluded night.
    /// </summary>
    public FirstNightState? FirstNight => _firstNight;

    /// <summary>
    /// Whether the authored first night is still running. Every gate that has to
    /// hold the calendar or keep the spirit present reads this one predicate.
    /// </summary>
    public bool IsFirstNightActive => _firstNight?.IsActive == true;
    // Territory expansion is intentionally suspended while the finite visual
    // boundary is redesigned. Persisted frontier parcels remain valid save
    // data, but no expedition may target or expose them in this increment.
    public CityParcel? NextTerritoryTarget => null;

    /// <summary>Read-only view of the chronological event log.</summary>
    public WorldEventLog Log => _log;
    public CityResourceLedger Resources => _resources;

    /// <summary>
    /// EG-0 measurement of this city's opening. Read-only observation; no rule
    /// reads it. See <see cref="EarlyGameMetrics"/>.
    /// </summary>
    public EarlyGameMetrics Metrics => _metrics;

    public event EventHandler<CityWorldChangedEventArgs>? BuildingChanged;
    public event EventHandler<CityWorldChangedEventArgs>? ProjectChanged;
    public event EventHandler<CityWorldChangedEventArgs>? CultivationSiteChanged;
    public event EventHandler<ExpeditionChangedEventArgs>? ExpeditionChanged;

    /// <summary>
    /// The citizen recognised as the principal hero, or <c>null</c> before
    /// onboarding. Hero status remains a role attached to a regular citizen.
    /// </summary>
    public Citizen? Hero
    {
        get
        {
            // Hero remains the compatibility name for the founding hero.
            // Other citizens may also carry RoleId.Hero after explicit
            // incorporation, but they must never replace the founder in
            // onboarding, gathering, construction, or profile flows.
            if (_citizens.TryGetValue(PrincipalHeroId, out Citizen? founder)
                && founder.IsHero)
            {
                return founder;
            }
            foreach (var citizen in _citizens.Values)
            {
                if (citizen.IsHero) return citizen;
            }
            return null;
        }
    }

    public bool NeedsOnboarding => Hero is null;

    public bool IsCitizenOnActiveExpedition(CitizenId citizenId)
    {
        if (!_citizens.TryGetValue(citizenId, out Citizen? citizen)
            || citizen.Commitment.Kind != CitizenCommitmentKind.Expedition)
        {
            return false;
        }
        foreach (Expedition expedition in _expeditions.Values)
        {
            if (expedition.Status == ExpeditionStatus.Active
                && expedition.HasMember(citizenId)
                && citizen.Commitment.EntityId == expedition.Id.Value)
            {
                return true;
            }
        }
        return false;
    }

    public ExpeditionStartResult StartResourceExpedition(
        ResourceOpportunityId opportunityId,
        IReadOnlyList<CitizenId> memberIds,
        ExpeditionRetreatPosture retreatPosture)
    {
        if (!_resourceOpportunities.TryGetValue(
                opportunityId,
                out ResourceOpportunity? opportunity))
        {
            return ExpeditionStartResult.Fail(ExpeditionStartOutcome.OpportunityNotFound);
        }

        return StartExpedition(ExpeditionRequest.ResourceSortie(
            opportunity,
            memberIds,
            retreatPosture));
    }

    public ExpeditionStartResult StartExpedition(ExpeditionRequest request)
    {
        if (Hero is null) return ExpeditionStartResult.Fail(ExpeditionStartOutcome.NoHero);
        if (request.RewardKind == ExpeditionRewardKind.Migrant
            && (!_buildings.Values.Any(building => building.Kind == BuildingKind.TownHall)
                || PendingProspect is not null))
        {
            return ExpeditionStartResult.Fail(ExpeditionStartOutcome.TownHallUnavailable);
        }

        IReadOnlyList<CitizenId> memberIds = request.MemberIds;
        if (memberIds is null || memberIds.Count == 0 || memberIds.Count > ExpeditionRequest.MaxTeamSize)
        {
            return ExpeditionStartResult.Fail(ExpeditionStartOutcome.InvalidRequest);
        }
        if (memberIds.Distinct().Count() != memberIds.Count)
        {
            return ExpeditionStartResult.Fail(ExpeditionStartOutcome.DuplicateMember);
        }

        var members = new List<Citizen>(memberIds.Count);
        foreach (CitizenId memberId in memberIds)
        {
            if (!_citizens.TryGetValue(memberId, out Citizen? member))
            {
                return ExpeditionStartResult.Fail(ExpeditionStartOutcome.MemberNotFound);
            }
            if (!member.IsHero)
            {
                return ExpeditionStartResult.Fail(ExpeditionStartOutcome.MemberNotHero);
            }
            if (!member.CanJoinExpedition)
            {
                return ExpeditionStartResult.Fail(
                    ExpeditionStartOutcome.MemberUnavailable,
                    member.AvailabilityReason);
            }
            members.Add(member);
        }

        if (request.DurationTicks <= 0
            || !Enum.IsDefined(request.RetreatPosture)
            || string.IsNullOrWhiteSpace(request.DisplayName))
        {
            return ExpeditionStartResult.Fail(ExpeditionStartOutcome.InvalidRequest);
        }

        ResourceOpportunity? resourceOpportunity = null;
        int carryCapacity = 0;
        if (request.ResourceOpportunityId is ResourceOpportunityId opportunityId)
        {
            if (!_resourceOpportunities.TryGetValue(opportunityId, out resourceOpportunity))
            {
                return ExpeditionStartResult.Fail(ExpeditionStartOutcome.OpportunityNotFound);
            }
            bool isSpiritTrail = resourceOpportunity.Kind ==
                ResourceOpportunityKind.SpiritTrailSearch;
            if (!isSpiritTrail
                && (!HasFoundingSiteModule(FoundingSiteModule.Campfire)
                    || !HasFoundingSiteModule(FoundingSiteModule.Cache)))
            {
                return ExpeditionStartResult.Fail(
                    ExpeditionStartOutcome.ResourceSortiesUnavailable);
            }
            if (resourceOpportunity.State != ResourceOpportunityState.Available)
            {
                return ExpeditionStartResult.Fail(ExpeditionStartOutcome.OpportunityUnavailable);
            }
            ResourceExpeditionDefinition definition =
                ResourceExpeditionRules.Definition(resourceOpportunity.Kind);
            if (resourceOpportunity.Kind == ResourceOpportunityKind.SpiritTrailSearch
                && (members.Count != 1 || members[0].Id != Hero.Id))
            {
                return ExpeditionStartResult.Fail(ExpeditionStartOutcome.InvalidRequest);
            }
            if (request.ResourceOpportunityKind != resourceOpportunity.Kind
                || request.DurationTicks != definition.DurationTicks
                || request.SupplyRequirement != definition.SupplyRequirement
                || request.Reward != definition.Reward
                || request.SetbackReturn != definition.SetbackReturn
                || request.PartialReturn != definition.PartialReturn
                || request.RewardAmount != definition.FullReturn)
            {
                return ExpeditionStartResult.Fail(ExpeditionStartOutcome.InvalidRequest);
            }
            int returnHeadroom = definition.Reward.IsMaterial
                ? AvailableFoundingStorageCapacity()
                : 0;
            if (definition.Reward.IsMaterial && returnHeadroom < definition.SetbackReturn)
            {
                return ExpeditionStartResult.Fail(
                    ExpeditionStartOutcome.InsufficientReturnCapacity);
            }
            carryCapacity = definition.Reward.IsMaterial
                ? Math.Min(definition.FullReturn, returnHeadroom)
                : 0;
        }
        else if (request.ResourceOpportunityKind.HasValue
            || request.SetbackReturn != 0
            || request.PartialReturn != 0)
        {
            return ExpeditionStartResult.Fail(ExpeditionStartOutcome.InvalidRequest);
        }
        foreach (Expedition existing in _expeditions.Values)
        {
            if (existing.Status == ExpeditionStatus.Active)
            {
                return ExpeditionStartResult.Fail(ExpeditionStartOutcome.AlreadyActive);
            }
        }

        var id = new ExpeditionId(_nextExpeditionId++);
        ResourceReservation? reservation = null;
        if (!request.SupplyRequirement.IsNone
            && (!_resources.TryReserve(
                    request.SupplyRequirement.Resource!.Value,
                    request.SupplyRequirement.Amount,
                    new ResourceReservationOwner(
                        ResourceReservationOwnerKind.Expedition,
                        id.Value),
                    out reservation)
                || reservation is null))
        {
            return ExpeditionStartResult.Fail(ExpeditionStartOutcome.MissingSupplies);
        }
        if (resourceOpportunity is not null && !resourceOpportunity.TryReserve(id))
        {
            if (reservation is not null) _resources.Release(reservation.Id);
            return ExpeditionStartResult.Fail(ExpeditionStartOutcome.OpportunityUnavailable);
        }

        var expedition = new Expedition(
            id,
            request.DisplayName.Trim(),
            memberIds,
            _tick,
            checked(_tick + request.DurationTicks),
            request.SupplyRequirement,
            request.Reward,
            reservation?.Id,
            retreatPosture: request.RetreatPosture,
            targetParcelId: request.RewardKind == ExpeditionRewardKind.Supplies
                && resourceOpportunity is null
                ? NextTerritoryTarget?.Id
                : null,
            resourceOpportunityId: request.ResourceOpportunityId,
            resourceOpportunityKind: request.ResourceOpportunityKind,
            setbackReturn: request.SetbackReturn,
            partialReturn: request.PartialReturn,
            carryCapacity: carryCapacity);

        // Dispatch is all-or-nothing: if any member becomes unavailable
        // between validation above and here (there is no reentrancy in this
        // synchronous call, but the check stays authoritative rather than
        // trusting the earlier loop), roll back every member already
        // dispatched and release the reservation so no partial team ever
        // exists.
        var dispatched = new List<Citizen>(members.Count);
        foreach (Citizen member in members)
        {
            if (!member.DispatchOnExpedition(id))
            {
                foreach (Citizen toRollBack in dispatched)
                {
                    toRollBack.CancelExpeditionDispatch(id);
                }
                if (reservation is not null) _resources.Release(reservation.Id);
                resourceOpportunity?.Release(id);
                return ExpeditionStartResult.Fail(
                    ExpeditionStartOutcome.MemberUnavailable,
                    member.AvailabilityReason);
            }
            dispatched.Add(member);
        }
        _expeditions.Add(id, expedition);
        _metrics.RecordExpeditionDispatched(_tick);
        WorldEvent dispatchEvent = _log.Record(
            _tick,
            WorldEventKind.ExpeditionDispatched,
            WorldEventSubject.Expedition(id.Value, expedition.DisplayName),
            request.SupplyAmount);
        expedition.SetDispatchEventId(dispatchEvent.Id);
        ExpeditionChanged?.Invoke(
            this,
            new ExpeditionChangedEventArgs(id, expedition.Status));
        return ExpeditionStartResult.Success(id);
    }

    /// <summary>
    /// Incorporates an existing citizen into the expedition-capable hero
    /// role by explicit player authorization. Heroism is an accumulated role
    /// on <see cref="Citizen"/>, never a parallel person type.
    /// </summary>
    public HeroIncorporationResult TryIncorporateHero(CitizenId citizenId)
    {
        if (Hero is null)
        {
            return HeroIncorporationResult.Fail(HeroIncorporationOutcome.NoFounder);
        }
        if (!_citizens.TryGetValue(citizenId, out Citizen? citizen))
        {
            return HeroIncorporationResult.Fail(HeroIncorporationOutcome.CitizenNotFound);
        }
        if (citizen.IsHero)
        {
            return HeroIncorporationResult.Fail(HeroIncorporationOutcome.AlreadyHero);
        }

        citizen.GrantRole(RoleId.Hero, _tick);
        return HeroIncorporationResult.Success(citizenId);
    }

    public bool CancelExpedition(ExpeditionId id)
    {
        if (!_expeditions.TryGetValue(id, out Expedition? expedition)
            || expedition.Status != ExpeditionStatus.Active
            || expedition.Phase != ExpeditionPhase.Outbound
            || _tick != expedition.StartTick)
        {
            return false;
        }
        if (expedition.ReservationId is ResourceReservationId reservationId)
            _resources.Release(reservationId);
        ReleaseResourceOpportunity(expedition);
        expedition.MarkCancelled();
        CancelMemberDispatches(expedition);
        _log.Record(
            _tick,
            WorldEventKind.ExpeditionCancelled,
            WorldEventSubject.Expedition(id.Value, expedition.DisplayName),
            causeEventId: expedition.DispatchEventId);
        ExpeditionChanged?.Invoke(
            this,
            new ExpeditionChangedEventArgs(id, expedition.Status));
        return true;
    }

    public CombatSessionSnapshot? GetCombatSessionSnapshot(ExpeditionId expeditionId) =>
        _combatSessions.TryGetValue(expeditionId, out CombatSession? session)
            ? session.Snapshot()
            : null;

    internal CombatSession? GetCombatSession(ExpeditionId expeditionId) =>
        _combatSessions.TryGetValue(expeditionId, out CombatSession? session)
            ? session
            : null;

    public bool SetCombatAutoSkillsEnabled(ExpeditionId expeditionId, bool enabled)
    {
        if (!_combatSessions.TryGetValue(expeditionId, out CombatSession? session)
            || !session.IsActive)
        {
            return false;
        }
        session.SetAutoSkillsEnabled(enabled);
        return true;
    }

    public bool TryActivateMemberSkill(ExpeditionId expeditionId, int slotIndex) =>
        _combatSessions.TryGetValue(expeditionId, out CombatSession? session)
        && session.TryActivateMemberSkill(slotIndex);

    private void CancelMemberDispatches(Expedition expedition)
    {
        foreach (CitizenId memberId in expedition.MemberIds)
        {
            if (_citizens.TryGetValue(memberId, out Citizen? member))
            {
                member.CancelExpeditionDispatch(expedition.Id);
            }
        }
    }

    /// <summary>S-1.5 follow-up: the FSM counterpart to every expedition end state (returned/failed/cancelled).</summary>
    private void ReturnMembersFromExpedition(
        Expedition expedition,
        ExpeditionEncounterOutcome outcome)
    {
        ApplyCombatSessionConsequences(expedition);
        // EG-0: every return path — objective, retreat, failure — funnels
        // through here, so the absence is counted once no matter how the
        // sortie ended. Per member, because a two-person team costs the city
        // twice the labour a solo trip does.
        _metrics.RecordExpeditionAbsence(
            _tick - expedition.StartTick,
            expedition.MemberIds.Count);
        foreach (CitizenId memberId in expedition.MemberIds)
        {
            if (_citizens.TryGetValue(memberId, out Citizen? member))
            {
                member.ReturnFromExpedition(expedition.Id, _tick);
            }
        }
        if (outcome == ExpeditionEncounterOutcome.Setback)
        {
            ApplyExpeditionWound(expedition);
        }
    }

    private void ApplyCombatSessionConsequences(Expedition expedition)
    {
        if (!_combatSessions.TryGetValue(expedition.Id, out CombatSession? session)) return;
        var statistics = StatisticsBalanceConfig.Default;
        foreach (CombatantState combatant in session.Party)
        {
            if (combatant.CitizenId is not CitizenId citizenId
                || !_citizens.TryGetValue(citizenId, out Citizen? citizen))
            {
                continue;
            }
            ConditionFactorBreakdown condition = CombatConditionFactor.Derive(
                combatant.CurrentHealth,
                combatant.MaxHealth,
                combatant.Fatigue,
                combatant.Injuries,
                statistics,
                CombatBalanceConfig.Default);
            citizen.SetCurrentHealthAndCondition(new CurrentHealthAndCondition(
                combatant.CurrentHealth,
                condition.Value,
                statistics));
        }
    }

    private void ApplyExpeditionWound(Expedition expedition)
    {
        Citizen? wounded = expedition.MemberIds
            .Select(memberId => GetCitizen(memberId))
            .Where(member => member is not null)
            .Select(member => member!)
            .OrderBy(member => member.CurrentStamina * 100 / member.MaxStamina)
            .ThenBy(member => member.Id.Value)
            .FirstOrDefault();
        if (wounded is null) return;

        // VS-3 introduces one recoverable wound tier. The enum and rules are
        // intentionally extensible, but expedition content must earn a
        // harsher tier before the simulation starts creating one.
        WoundSeverity severity = WoundSeverity.Moderate;
        WorldEvent woundEvent = _log.Record(
            _tick,
            WorldEventKind.WoundSustained,
            WorldEventSubject.Citizen(wounded.Id, wounded.Name),
            (int)severity,
            expedition.DispatchEventId);
        wounded.SustainWound(severity, woundEvent.Id);
    }

    public WoundRecoveryResult TryBeginWoundRecovery(CitizenId citizenId)
    {
        if (!_citizens.TryGetValue(citizenId, out Citizen? citizen))
        {
            return WoundRecoveryResult.Fail(WoundRecoveryOutcome.CitizenNotFound);
        }
        if (citizen.Wound is null)
        {
            return WoundRecoveryResult.Fail(WoundRecoveryOutcome.NotWounded);
        }
        if (citizen.Commitment.Kind == CitizenCommitmentKind.Recovery)
        {
            return WoundRecoveryResult.Fail(WoundRecoveryOutcome.AlreadyRecovering);
        }
        if (citizen.Commitment.Kind == CitizenCommitmentKind.Expedition)
        {
            return WoundRecoveryResult.Fail(WoundRecoveryOutcome.OnExpedition);
        }
        Building? shelter = _buildings.Values
            .Where(building => building.Kind == BuildingKind.Home)
            .OrderBy(building => building.Id.Value)
            .FirstOrDefault();
        if (shelter is null)
        {
            return WoundRecoveryResult.Fail(WoundRecoveryOutcome.ShelterUnavailable);
        }

        int foodCost = WoundRules.FoodCostFor(citizen.Wound.Severity);
        if (!TryConsumeFood(foodCost))
        {
            return WoundRecoveryResult.Fail(WoundRecoveryOutcome.MissingFood);
        }
        if (!citizen.BeginWoundRecovery(shelter.Id, _tick))
        {
            throw new InvalidOperationException("Validated wound recovery could not begin.");
        }
        _log.Record(
            _tick,
            WorldEventKind.WoundRecoveryStarted,
            WorldEventSubject.Citizen(citizen.Id, citizen.Name),
            foodCost,
            citizen.Wound.OriginatingEventId);
        return WoundRecoveryResult.Success(citizenId, foodCost);
    }

    public enum MigrantOutcome
    {
        Success = 0,
        AtCapacity = 1,
        InvalidProfile = 2,
        TownHallRequired = 3,
        NoProspect = 4,
        ProspectAlreadyWaiting = 5,
    }

    public readonly record struct MigrantResult(
        MigrantOutcome Outcome,
        CitizenId? MigrantId)
    {
        public bool IsSuccess => Outcome == MigrantOutcome.Success;

        public static MigrantResult Success(CitizenId id) =>
            new(MigrantOutcome.Success, id);

        public static MigrantResult Fail(MigrantOutcome outcome) =>
            new(outcome, null);
    }

    /// <summary>
    /// Provisional housing rule for the vertical slice. Completed Homes expose
    /// their usable resident capacity through the same capacity value that the
    /// Shelter already presents. Construction projects do not count.
    /// </summary>
    public int HousingCapacity => _buildings.Values
        .Where(building => building.Kind == BuildingKind.Home)
        .Sum(building => building.WorkerCapacity);

    public int AvailableHousing => Math.Max(0, HousingCapacity - _citizens.Count);

    /// <summary>
    /// Adds a non-hero citizen to the city. The profile is taken
    /// verbatim (aptitudes, families, lineage, gender). A
    /// <see cref="CitizenId"/> is allocated beyond every existing
    /// hero and migrant; the new citizen is mobilised at home and
    /// not assigned. Hard-codes a deterministic two-line biography
    /// when <paramref name="name"/> is null/empty so test fixtures
    /// do not need a separate name source.
    /// </summary>
    public MigrantResult TryAcceptPendingProspect()
    {
        if (Hero is null)
        {
            return MigrantResult.Fail(MigrantOutcome.InvalidProfile);
        }
        if (AvailableHousing == 0)
        {
            return MigrantResult.Fail(MigrantOutcome.AtCapacity);
        }
        if (PendingProspect is null)
        {
            return MigrantResult.Fail(MigrantOutcome.NoProspect);
        }
        int nextId = NextCitizenId();
        string displayName = PendingProspect.Name;
        var migrant = new Citizen(
            new CitizenId(nextId),
            displayName,
            appearanceSeed: nextId * 7,
            profile: PendingProspect.Profile);
        _citizens.Add(migrant.Id, migrant);
        if (GameClock.IsDaytime(_tick))
        {
            migrant.SetLocation(CitizenLocation.AtHome);
        }
        else
        {
            migrant.SetLocation(CitizenLocation.AtHome);
        }
        _log.Record(
            _tick,
            WorldEventKind.MigrantArrived,
            WorldEventSubject.Citizen(migrant.Id, migrant.Name));
        PendingProspect = null;
        return MigrantResult.Success(migrant.Id);
    }

    /// <summary>
    /// Recruits a deterministic individual rather than cloning the founder.
    /// The allocated citizen id is the stable seed, so the generated identity
    /// is reproducible and becomes ordinary persisted profile data.
    /// </summary>
    public MigrantOutcome TryHostExpeditionProspect(string? name = null)
    {
        if (Hero is null)
        {
            return MigrantOutcome.InvalidProfile;
        }
        int nextId = NextCitizenId();
        CitizenProfile profile = CreateMigrantProfile(nextId);
        string generatedName = string.IsNullOrWhiteSpace(name)
            ? MigrantNameForSeed(nextId)
            : name;
        return TryHostExpeditionProspect(profile, generatedName, nextId);
    }

    /// <summary>
    /// Picks a migrant's display name from <see cref="MigrantNames"/> using a
    /// mix of <paramref name="seed"/> that is intentionally out of phase with
    /// the lineage index (<c>seed % Lineages.Count</c>). The two cycles used
    /// to share a length and a constant shift, so the same seed always paired
    /// the same name with the same lineage — two migrants of one lineage
    /// were statistically the same person.
    ///
    /// The mix runs unsigned so a seed large enough to overflow wraps into a
    /// valid index instead of a negative one. Citizen ids never get near that,
    /// but this is a public entry point and an unguarded index is a crash
    /// waiting for the first caller who does not know that.
    /// </summary>
    public static string MigrantNameForSeed(int seed) =>
        MigrantNames[unchecked((uint)(seed * 11 + 3)) % MigrantNames.Length];

    public MigrantOutcome TryHostExpeditionProspect(CitizenProfile profile, string name)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return TryHostExpeditionProspect(profile, name, NextCitizenId());
    }

    private MigrantOutcome TryHostExpeditionProspect(CitizenProfile profile, string name, int seed)
    {
        if (Hero is null) return MigrantOutcome.InvalidProfile;
        if (!_buildings.Values.Any(building => building.Kind == BuildingKind.TownHall))
        {
            return MigrantOutcome.TownHallRequired;
        }
        if (PendingProspect is not null)
        {
            return MigrantOutcome.ProspectAlreadyWaiting;
        }
        string prospectName = string.IsNullOrWhiteSpace(name) ? $"Prospect {seed}" : name.Trim();
        PendingProspect = new CitizenProspect(seed, prospectName, profile);
        return MigrantOutcome.Success;
    }

    private int NextCitizenId()
    {
        int nextId = 2;
        while (_citizens.ContainsKey(new CitizenId(nextId)))
        {
            nextId++;
        }
        return nextId;
    }

    internal static CitizenProfile CreateMigrantProfile(int seed)
    {
        LineageDefinition lineage =
            ProfileCatalog.Lineages[seed % ProfileCatalog.Lineages.Count];
        GenderId gender = seed % 2 == 0
            ? GenderId.Feminine
            : GenderId.Masculine;
        FounderCubeProfile cube = CubeScoring.GenerateOrdinaryProfile(lineage.Id, seed);
        bool created = CitizenProfile.TryCreate(
            lineage.Id,
            gender,
            SelectThree(ProfileCatalog.Aptitudes, seed),
            SelectThree(ProfileCatalog.ProfessionFamilies, seed + 2),
            ProfileCatalog.ElementalAffinities[
                seed % ProfileCatalog.ElementalAffinities.Count].Id,
            ProfileCatalog.CombatStyles[
                seed % ProfileCatalog.CombatStyles.Count].Id,
            new[]
            {
                ProfileCatalog.WeaponPreferences[
                    seed % ProfileCatalog.WeaponPreferences.Count].Id,
                ProfileCatalog.WeaponPreferences[
                    (seed + 1) % ProfileCatalog.WeaponPreferences.Count].Id,
            },
            SelectThree(ProfileCatalog.PersonalityTraits, seed + 4),
            ProfileCatalog.PoliticalOrientations[
                seed % ProfileCatalog.PoliticalOrientations.Count].Id,
            ProfileCatalog.SpiritualPostures[
                seed % ProfileCatalog.SpiritualPostures.Count].Id,
            cube,
            out CitizenProfile? profile,
            out string error);
        return created
            ? profile!
            : throw new InvalidOperationException(
                $"Generated migrant profile was invalid: {error}");
    }

    private static TId[] SelectThree<TId>(
        IReadOnlyList<ProfileOption<TId>> options,
        int offset)
        where TId : struct
    {
        return new[]
        {
            options[offset % options.Count].Id,
            options[(offset + 1) % options.Count].Id,
            options[(offset + 2) % options.Count].Id,
        };
    }

    public CityWorld CreateRestartedCityKeepingHero()
    {
        Citizen? hero = Hero;
        if (hero is null)
        {
            throw new InvalidOperationException(
                "A city without a founder cannot be soft-reset.");
        }

        var restarted = new CityWorld();
        HeroCreationResult result = restarted.TryCreateHero(
            new HeroCreationRequest(
                hero.Name,
                hero.Profile,
                hero.Profile.Gender));
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(
                $"Could not preserve the founder during soft reset: {result.Outcome}.");
        }
        restarted.SeedStartingForests();
        // Both seeders, exactly as a fresh city gets after onboarding. Seeding
        // only the forests left the restarted world — and the JSON written from
        // it — with no Branches, Plant Fiber, Small Stone or Wild Food and no
        // resource opportunities, so its Campfire (3 Branches + 2 Small Stone,
        // paid in full up front) was unpayable and no expedition existed. The
        // live path happened to recover because TryLoadFromPrimarySlot re-seeds
        // after the scene reload, but the saved city was unwinnable for anyone
        // loading it any other way.
        restarted.SeedStartingOpportunities();
        return restarted;
    }

    /// <summary>
    /// Establishes the only citizen in a fresh world. The profile is
    /// individual: no validation requires it to match common tendencies
    /// of the chosen lineage. The founding forests are not part of this
    /// call — the controller invokes <see cref="SeedStartingForests"/>
    /// separately so test fixtures can opt out of the empty-field
    /// gathering target.
    /// </summary>
    public HeroCreationResult TryCreateHero(HeroCreationRequest request)
    {
        if (Hero is not null)
        {
            return HeroCreationResult.Fail(HeroCreationOutcome.AlreadyExists);
        }
        if (_citizens.Count > 0 || _buildings.Count > 0)
        {
            return HeroCreationResult.Fail(HeroCreationOutcome.WorldNotEmpty);
        }
        if (request is null || request.Profile is null)
        {
            return HeroCreationResult.Fail(HeroCreationOutcome.MissingProfile);
        }

        string name = request.Name?.Trim() ?? string.Empty;
        if (name.Length is < 1 or > 32 || ContainsControlCharacter(name))
        {
            return HeroCreationResult.Fail(HeroCreationOutcome.InvalidName);
        }

        CitizenProfile founderProfile = request.OnboardingResult is { } onboarding
            ? request.Profile.WithFounderOnboardingResult(onboarding)
            : request.Profile.FounderOnboardingResult is not null
                ? request.Profile
                : request.Profile.WithFounderFallback();
        var hero = new Citizen(
            PrincipalHeroId,
            name,
            appearanceSeed: StableAppearanceSeed(name, founderProfile.Lineage),
            profile: founderProfile,
            origin: CitizenOrigin.AstralFounder);
        hero.GrantRole(RoleId.Hero, _tick);
        RegisterCitizen(hero);
        // The authored first night begins where the manifestation ends, with no
        // extra scene and no clock change: a fresh world is already at Day 1
        // 00:00, which is night.
        _firstNight = new FirstNightState(startedAtTick: _tick);
        return HeroCreationResult.Success(hero.Id, founderProfile.FounderOnboardingResult);
    }

    /// <summary>
    /// Number of individually visible trees in each founding resource patch.
    /// </summary>
    public const int StartingForestUnitCount = 3;

    /// <summary>Wood held by each tree in a founding resource patch.</summary>
    public const int StartingTreeWoodReserve = 8;

    /// <summary>Total reserve across one founding resource patch.</summary>
    public const int StartingForestWoodReserve =
        StartingForestUnitCount * StartingTreeWoodReserve;

    /// <summary>Per-forest compatibility-storage capacity for gathered wood.</summary>
    public const int StartingForestStorageCapacity = StartingForestWoodReserve;

    /// <summary>
    /// EG-1 carry cap. Per <c>EARLY_GAME_RESOURCE_AND_EXPEDITION_PROPOSAL.md
    /// §4</c>, the founder can carry at most six collected units of the
    /// four rudimentary ground resources (Branches, Plant Fiber, Small
    /// Stone, Wild Food) before the Cache exists. Wood, Stone, Food and
    /// the other resource kinds ignore this cap because they flow into
    /// per-building storage instead of the carried inventory. EG-2
    /// replaces this with location-aware capacities (Cache = 12, Basic
    /// Shelter = 24) once the Founding Site modules ship; the cap
    /// itself is the only enforcement here, not the policy of where
    /// things sit.
    /// </summary>
    public const int CarriedGroundResourceCapacity = 6;

    private static readonly ResourceType[] CarriedGroundResourceTypes =
    {
        ResourceType.Branches,
        ResourceType.PlantFiber,
        ResourceType.SmallStone,
        ResourceType.WildFood,
    };

    /// <summary>
    /// Total units of <see cref="CarriedGroundResourceTypes"/> currently
    /// in the city inventory, i.e. ready to be spent on a Founding Site
    /// module. EG-2 will move this into a location-aware ledger query;
    /// for EG-1 it is the direct sum of the four type totals.
    /// </summary>
    public int CarriedGroundResourceCount()
    {
        int total = 0;
        foreach (ResourceType type in CarriedGroundResourceTypes)
        {
            total += _resources.Available(type);
        }
        return total;
    }

    /// <summary>
    /// Capacity granted by the current physical founding state: carried before
    /// Cache, bounded Cache storage while the site is open, and the consolidated
    /// shelter capacity after transformation.
    /// </summary>
    public int GroundResourceCapacity()
    {
        foreach (Building building in _buildings.Values)
        {
            if (building.Kind == BuildingKind.Home) return FoundingSiteRules.ShelterCapacity;
        }
        foreach (ConstructionProject project in _projects.Values)
        {
            if (project.Kind == ConstructionKind.FoundingSite
                && project.HasCompletedFoundingModule(FoundingSiteModule.Cache))
            {
                return FoundingSiteRules.CacheCapacity;
            }
        }
        return FoundingSiteRules.CarriedCapacity;
    }

    public int FoundingStorageCount()
    {
        int total = 0;
        foreach ((ResourceType _, int amount) in _inventory.Amounts)
        {
            total = checked(total + amount);
        }
        return total;
    }

    public int AvailableFoundingStorageCapacity(ExpeditionId? excludeExpeditionId = null)
    {
        int reserved = 0;
        foreach (Expedition expedition in _expeditions.Values)
        {
            if (expedition.Status != ExpeditionStatus.Active
                || expedition.ResourceOpportunityId is null
                || expedition.Id == excludeExpeditionId) continue;
            reserved = checked(reserved + expedition.CarryCapacity);
        }
        return Math.Max(0, GroundResourceCapacity() - FoundingStorageCount() - reserved);
    }

    /// <summary>
    /// Capacity available to the four rudimentary resources gathered from the
    /// ground. Before a Cache exists those units are carried by the founder,
    /// so unrelated legacy Food/Wood in city inventory cannot fill the
    /// six-unit personal load. Once a Cache or Shelter exists, every founding
    /// resource shares that physical storage and the aggregate capacity
    /// applies.
    /// </summary>
    private int AvailableGroundGatherCapacity()
    {
        if (!HasFoundingCacheOrShelter())
        {
            return Math.Max(0, FoundingSiteRules.CarriedCapacity
                - CarriedGroundResourceCount());
        }
        return AvailableFoundingStorageCapacity();
    }

    private bool HasFoundingCacheOrShelter()
    {
        foreach (Building building in _buildings.Values)
        {
            if (building.Kind == BuildingKind.Home) return true;
        }
        return HasFoundingSiteModule(FoundingSiteModule.Cache);
    }

    private int DepositToFoundingStorage(
        ResourceType resource,
        int amount,
        ExpeditionId? returningExpeditionId = null)
    {
        int accepted = Math.Min(
            Math.Max(0, amount),
            AvailableFoundingStorageCapacity(returningExpeditionId));
        return accepted <= 0
            ? 0
            : _resources.DepositToCityInventory(resource, accepted);
    }

    /// <summary>Queries persisted Founding Site capabilities without exposing storage details.</summary>
    /// <summary>
    /// Id of the building that grew out of the founding site, or <c>null</c>
    /// while it is still a project. Presentation uses it to anchor the
    /// campfire's embers on the real structure instead of guessing.
    /// </summary>
    public int? FoundingSiteBuildingId()
    {
        foreach (Building building in _buildings.Values)
        {
            if (building.Kind == BuildingKind.Home
                && building.FoundingSiteOriginModules.Count > 0) return building.Id.Value;
        }
        return null;
    }

    public bool HasFoundingSiteModule(FoundingSiteModule module)
    {
        foreach (ConstructionProject project in _projects.Values)
        {
            if (project.Kind == ConstructionKind.FoundingSite
                && project.HasCompletedFoundingModule(module)) return true;
        }
        foreach (Building building in _buildings.Values)
        {
            if (building.Kind == BuildingKind.Home
                && building.FoundingSiteOriginModules.Contains(module)) return true;
        }
        return false;
    }

    public bool HasTool(ToolKind tool) => _tools.Contains(tool);

    public ToolCraftResult ToolCraftAvailability(ToolKind tool)
    {
        if (!Enum.IsDefined(tool))
        {
            return new ToolCraftResult(ToolCraftOutcome.InvalidTool);
        }
        if (!HasCompletedFirstShelter())
        {
            return new ToolCraftResult(ToolCraftOutcome.ShelterRequired);
        }
        if (_tools.Contains(tool))
        {
            return new ToolCraftResult(ToolCraftOutcome.AlreadyOwned);
        }
        foreach (RecipeInput input in ToolRules.InputsFor(tool))
        {
            if (_resources.Available(input.Resource) < input.Amount)
            {
                return new ToolCraftResult(
                    ToolCraftOutcome.MissingResource,
                    input.Resource);
            }
        }
        return new ToolCraftResult(ToolCraftOutcome.Crafted);
    }

    public ToolCraftResult TryCraftTool(ToolKind tool)
    {
        ToolCraftResult availability = ToolCraftAvailability(tool);
        if (!availability.IsSuccess) return availability;
        if (!_resources.TryConsume(ToolRules.InputsFor(tool), out ResourceType? missing))
        {
            return new ToolCraftResult(ToolCraftOutcome.MissingResource, missing);
        }

        _tools.Add(tool);
        Building? shelter = PrimaryHome;
        if (shelter is not null) RaiseBuildingChanged(shelter.Id);
        return new ToolCraftResult(ToolCraftOutcome.Crafted);
    }

    /// <summary>
    /// Explicit recovery action while the Founding Site remains incomplete.
    /// It returns every carried rudimentary unit to its matching authored patch,
    /// allowing the player to gather the exact load for whichever module comes
    /// next without destroying or converting any opening resource.
    /// </summary>
    public int ReturnFoundingCargo()
    {
        if (HasCompletedFirstShelter()) return 0;
        int returned = 0;
        foreach (ResourceType type in CarriedGroundResourceTypes)
        {
            NaturalResourcePatch? returnPatch = null;
            foreach (NaturalResourcePatch patch in _naturalResourcePatches.Values)
            {
                if (patch.ResourceType == type)
                {
                    returnPatch = patch;
                    break;
                }
            }
            if (returnPatch is null) continue;
            if (!_parcels.TryGetValue(returnPatch.ParcelId, out CityParcel? returnParcel)) continue;
            NaturalResourcePatch targetPatch = returnPatch;
            bool CanReturnToUnit(int unitId)
            {
                NaturalResourceUnitPosition position = targetPatch.UnitPositions[unitId];
                return FrontageState(
                    position.GlobalRow(returnParcel),
                    position.GlobalFrontageColumn(returnParcel))
                    is not (FrontageCellState.ReservedByBuilding
                        or FrontageCellState.ReservedAsCorridor);
            }
            if (!returnPatch.CanAcceptReturn(CanReturnToUnit)) continue;
            int carried = _inventory.AmountOf(type);
            if (carried > 0 && _inventory.TryConsume(type, carried))
            {
                int accepted = returnPatch.Return(carried, CanReturnToUnit);
                // The patch is the authority on what it can hold. Anything it
                // refuses goes straight back to the city so cargo is never lost.
                if (accepted < carried) _inventory.Deposit(type, carried - accepted);
                returned += accepted;
                if (accepted > 0) RaisePatchChanged(returnPatch.Id);
            }
        }
        return returned;
    }

    public int ReturnableFoundingCargoCount() =>
        HasCompletedFirstShelter() ? 0 : CarriedGroundResourceCount();

    /// <summary>
    /// Drops two Forests into the world so the hero has a wood source
    /// to gather from. Each Forest starts with
    /// <see cref="StartingForestWoodReserve"/> wood still in it.
    /// IDs are reserved (100, 101) so they never collide with future
    /// player-authorised buildings. Safe to call from any path:
    /// - no-op when no hero exists (no point seeding before founding),
    /// - no-op when the world already has a Forest (idempotent),
    /// - otherwise seeds two Forests. This is intentionally permissive
    ///   about pre-existing non-Forest buildings so a hero who already
    ///   finished the founding step still receives forests when their
    ///   save predated the wood-gathering slice.
    /// </summary>
    public void SeedStartingForests()
    {
        if (_citizens.Count == 0) return;
        foreach (NaturalResourcePatch patch in _naturalResourcePatches.Values)
        {
            if (patch.ResourceType == ResourceType.Wood) return;
        }
        foreach (var b in _buildings.Values)
        {
            if (b.Kind == BuildingKind.Forest) return;
        }

        var forest1 = new Building(
            id: new BuildingId(100),
            displayName: "Forest",
            kind: BuildingKind.Forest,
            producedResourceType: ResourceType.Wood,
            producedCompetencyId: CompetencyId.Foraging,
            workerCapacity: 2,
            visualCapacity: 2,
            baseProductionPerWorker: 1,
            storageCapacity: StartingForestStorageCapacity,
            resourceLabel: "Wood",
            resourceUnit: "wood");
        forest1.RestoreWoodUnits(
            Enumerable.Repeat(StartingTreeWoodReserve, StartingForestUnitCount));

        var forest2 = new Building(
            id: new BuildingId(101),
            displayName: "Forest",
            kind: BuildingKind.Forest,
            producedResourceType: ResourceType.Wood,
            producedCompetencyId: CompetencyId.Foraging,
            workerCapacity: 2,
            visualCapacity: 2,
            baseProductionPerWorker: 1,
            storageCapacity: StartingForestStorageCapacity,
            resourceLabel: "Wood",
            resourceUnit: "wood");
        forest2.RestoreWoodUnits(
            Enumerable.Repeat(StartingTreeWoodReserve, StartingForestUnitCount));

        RegisterBuilding(forest1);
        RegisterBuilding(forest2);
        EnsureFoundingParcels();
        RegisterNaturalResourcePatch(CreateProceduralResourcePatch(
            forest1.Id.Value,
            ResourceType.Wood,
            forest1.WoodUnitReserves,
            forest1.Id));
        RegisterNaturalResourcePatch(CreateProceduralResourcePatch(
            forest2.Id.Value,
            ResourceType.Wood,
            forest2.WoodUnitReserves,
            forest2.Id));
    }

    /// <summary>
    /// EG-1 part two: seeds the four rudimentary ground resources from
    /// <c>EARLY_GAME_RESOURCE_AND_EXPEDITION_PROPOSAL.md §4</c>:
    /// 14 Branches (7 bundles × 2), 6 Plant Fiber (3 × 2), 6 Small
    /// Stone (3 × 2), 8 Wild Food (4 × 2). Units occupy independent frontage
    /// cells in deterministic scatter and may share a parcel with other resource
    /// types; no patch claims an entire building lot. Idempotent:
    /// a save that already has any of the four EG-1 resource types
    /// in its patch set is left alone. Mid-game cities allocate around
    /// existing buildings, corridors and resource cells.
    /// </summary>
    public void SeedStartingOpportunities()
    {
        if (_citizens.Count == 0) return;
        EnsureStartingResourceExpeditionOpportunities();
        foreach (NaturalResourcePatch patch in _naturalResourcePatches.Values)
        {
            if (patch.ResourceType == ResourceType.Branches
                || patch.ResourceType == ResourceType.PlantFiber
                || patch.ResourceType == ResourceType.SmallStone
                || patch.ResourceType == ResourceType.WildFood)
            {
                return;
            }
        }

        EnsureFoundingParcels();

        // Distribute EG-A0 types in declared order through deterministic
        // scatter. Each tuple is
        // (resource type, units-each). Bundles/clusters/patches of "×2"
        // in the proposal mean N entries of 2 units each.
        var plan = new (ResourceType Type, int UnitCount)[]
        {
            (ResourceType.Branches, 7),
            (ResourceType.PlantFiber, 3),
            (ResourceType.SmallStone, 3),
            (ResourceType.WildFood, 4),
        };
        foreach ((ResourceType type, int unitCount) in plan)
        {
            int patchId = NextGroundPatchId();
            var reserves = new int[unitCount];
            for (int i = 0; i < unitCount; i++) reserves[i] = 2;
            RegisterNaturalResourcePatch(CreateProceduralResourcePatch(
                patchId,
                type,
                reserves));
        }
    }

    public void EnsureStartingResourceExpeditionOpportunities()
    {
        if (_citizens.Count == 0) return;
        var foodId = new ResourceOpportunityId(1);
        var woodId = new ResourceOpportunityId(2);
        _resourceOpportunities.TryAdd(
            foodId,
            new ResourceOpportunity(foodId, ResourceOpportunityKind.NearbyFoodForage));
        _resourceOpportunities.TryAdd(
            woodId,
            new ResourceOpportunity(woodId, ResourceOpportunityKind.FallenWoodSearch));

        // The spirit trail opportunity only exists after the dawn has
        // carried the spirit away. `TryAdd` is idempotent: restoring a
        // save that already has the opportunity is a no-op, and a save
        // that already passed the night will pick it up here on the
        // next call.
        if (_log.Events.Any(evt => evt.Kind == WorldEventKind.SpiritDeparted))
        {
            var spiritId = new ResourceOpportunityId(3);
            _resourceOpportunities.TryAdd(
                spiritId,
                new ResourceOpportunity(spiritId, ResourceOpportunityKind.SpiritTrailSearch));
        }
    }

    private NaturalResourcePatch CreateProceduralResourcePatch(
        int patchId,
        ResourceType resourceType,
        IReadOnlyList<int> reserves,
        BuildingId? legacyStorageBuildingId = null)
    {
        int seed = Hero?.AppearanceSeed ?? 0;
        foreach (CityParcel parcel in _parcels.Values
                     .Where(candidate => candidate.IsUnlocked)
                     .OrderBy(candidate => NaturalResourceLayoutPlanner.ParcelScore(
                         seed,
                         patchId,
                         candidate.Id)))
        {
            HashSet<NaturalResourceUnitPosition> unavailable =
                UnavailableResourcePositions(parcel);
            IReadOnlyList<NaturalResourceUnitPosition>? positions =
                NaturalResourceLayoutPlanner.TryAllocate(
                    reserves.Count,
                    seed,
                    patchId,
                    unavailable);
            if (positions is null) continue;
            return new NaturalResourcePatch(
                patchId,
                parcel.Id,
                resourceType,
                reserves,
                legacyStorageBuildingId,
                positions);
        }
        throw new InvalidOperationException(
            $"No compact resource cells are available for patch {patchId}.");
    }

    private HashSet<NaturalResourceUnitPosition> UnavailableResourcePositions(
        CityParcel parcel)
    {
        var unavailable = new HashSet<NaturalResourceUnitPosition>();
        if (FoundingLayout.IsInitialParcel(parcel))
        {
            unavailable.Add(FoundingLayout.FounderLocalPosition);
        }
        foreach (NaturalResourcePatch patch in _naturalResourcePatches.Values)
        {
            if (patch.ParcelId != parcel.Id) continue;
            foreach (NaturalResourceUnitPosition position in patch.UnitPositions)
            {
                unavailable.Add(position);
            }
        }
        foreach (ParcelPlacement placement in _parcelPlacements.Values)
        {
            int parcelRow = placement.RowId.Value / ParcelGrid.ConstructionRowsPerParcel;
            if (parcelRow != parcel.LogicalRow) continue;
            int parcelStart = parcel.LogicalColumn * ParcelGrid.FrontageColumnsPerParcel;
            for (int column = placement.StartColumn;
                 column < placement.StartColumn + placement.FrontageColumns;
                 column++)
            {
                int localColumn = column - parcelStart;
                if (localColumn < 0 || localColumn >= ParcelGrid.FrontageColumnsPerParcel)
                {
                    continue;
                }
                unavailable.Add(new NaturalResourceUnitPosition(
                    placement.RowId.Value % ParcelGrid.ConstructionRowsPerParcel,
                    localColumn));
            }
        }
        foreach (CorridorReservation corridor in _corridorReservations.Values)
        {
            int parcelRow = corridor.RowId.Value / ParcelGrid.ConstructionRowsPerParcel;
            if (parcelRow != parcel.LogicalRow) continue;
            int parcelStart = parcel.LogicalColumn * ParcelGrid.FrontageColumnsPerParcel;
            for (int column = corridor.StartColumn;
                 column < corridor.EndColumnExclusive;
                 column++)
            {
                int localColumn = column - parcelStart;
                if (localColumn < 0 || localColumn >= ParcelGrid.FrontageColumnsPerParcel)
                {
                    continue;
                }
                unavailable.Add(new NaturalResourceUnitPosition(
                    corridor.RowId.Value % ParcelGrid.ConstructionRowsPerParcel,
                    localColumn));
            }
        }
        return unavailable;
    }

    private int NextGroundPatchId()
    {
        int max = 0;
        foreach (NaturalResourcePatch patch in _naturalResourcePatches.Values)
        {
            if (patch.Id > max) max = patch.Id;
        }
        // Keep clear of legacy Forest patch ids (100/101) and the
        // reserved range that <c>CityWorldController</c> uses for the
        // synthetic Forest ids. EG-1 ground patches start at 200.
        return Math.Max(max, 200) + 1;
    }

    internal void EnsureFoundingParcels()
    {
        // A fresh terrarium starts as one readable horizontal strip. Three
        // parcels provide 81 frontage cells: enough for the founding resources,
        // shelter and first cultivation site without presenting a mature city's
        // empty land on day one.
        const int columnCount = 3;
        const int rowCount = 1;
        for (int row = 0; row < rowCount; row++)
        {
            for (int column = 0; column < columnCount; column++)
            {
                var parcelId = new ParcelId(row * columnCount + column + 1);
                _parcels.TryAdd(
                    parcelId,
                    new CityParcel(parcelId, column, row, isUnlocked: true));
            }
        }
    }

    internal void RegisterNaturalResourcePatch(NaturalResourcePatch patch)
    {
        if (!_parcels.TryGetValue(patch.ParcelId, out CityParcel? parcel))
        {
            throw new InvalidOperationException(
                $"Natural resource patch {patch.Id} references unknown parcel {patch.ParcelId.Value}.");
        }
        for (int unitId = 0; unitId < patch.UnitPositions.Count; unitId++)
        {
            NaturalResourceUnitPosition position = patch.UnitPositions[unitId];
            ConstructionRowId rowId = position.GlobalRow(parcel);
            int column = position.GlobalFrontageColumn(parcel);
            foreach (NaturalResourcePatch existing in _naturalResourcePatches.Values)
            {
                if (!_parcels.TryGetValue(existing.ParcelId, out CityParcel? existingParcel))
                {
                    continue;
                }
                for (int existingUnitId = 0;
                     existingUnitId < existing.UnitPositions.Count;
                     existingUnitId++)
                {
                    NaturalResourceUnitPosition existingPosition =
                        existing.UnitPositions[existingUnitId];
                    if (existingPosition.GlobalRow(existingParcel) == rowId
                        && existingPosition.GlobalFrontageColumn(existingParcel) == column)
                    {
                        throw new InvalidOperationException(
                            $"Natural resource patch {patch.Id} overlaps patch {existing.Id}.");
                    }
                }
            }
        }
        if (!_naturalResourcePatches.TryAdd(patch.Id, patch))
        {
            throw new InvalidOperationException($"Natural resource patch id {patch.Id} already exists.");
        }
    }

    internal void RegisterParcelPlacement(ParcelPlacement placement)
    {
        if (!_parcels.TryGetValue(placement.ParcelId, out CityParcel? parcel)
            || !parcel.IsUnlocked)
        {
            throw new InvalidOperationException(
                $"Placement {placement.EntityId.Value} requires an unlocked parcel.");
        }
        for (int column = placement.StartColumn;
             column < placement.StartColumn + placement.FrontageColumns;
             column++)
        {
            if (!TryGetAvailableParcelForFrontageCell(placement.RowId, column, out _))
            {
                throw new InvalidOperationException(
                    $"Placement {placement.EntityId.Value} crosses unavailable territory.");
            }
            if (NaturalResourceOccupiesFrontageCell(placement.RowId, column))
            {
                throw new InvalidOperationException(
                    $"Placement {placement.EntityId.Value} overlaps a natural resource.");
            }
            foreach (CorridorReservation corridor in _corridorReservations.Values)
            {
                if (corridor.RowId == placement.RowId && corridor.ContainsColumn(column))
                {
                    throw new InvalidOperationException(
                        $"Placement {placement.EntityId.Value} overlaps protected corridor {corridor.Id}.");
                }
            }
        }
        foreach (ParcelPlacement existing in _parcelPlacements.Values)
        {
            if (placement.Overlaps(existing))
            {
                throw new InvalidOperationException(
                    $"Placement {placement.EntityId.Value} overlaps {existing.EntityId.Value}.");
            }
        }
        if (!_parcelPlacements.TryAdd(placement.EntityId, placement))
        {
            throw new InvalidOperationException(
                $"Placement for entity {placement.EntityId.Value} already exists.");
        }
    }

    internal ParcelPlacement? FindFirstAvailablePlacement(
        BuildingId entityId,
        string footprintProfileId)
    {
        IReadOnlyList<ConstructionLot> lots = AvailableConstructionLots();
        if (lots.Count == 0) return null;
        ConstructionLot lot = lots[0];
        return CreatePlacement(entityId, lot, footprintProfileId);
    }

    public IReadOnlyList<ConstructionLot> AvailableConstructionLots()
    {
        var lots = new List<ConstructionLot>();
        foreach (CityParcel parcel in _parcels.Values
                     .OrderBy(candidate => candidate.LogicalRow)
                     .ThenBy(candidate => candidate.LogicalColumn))
        {
            if (!parcel.IsUnlocked) continue;
            for (int lotRow = 0; lotRow < ParcelGrid.ConstructionRowsPerParcel; lotRow++)
            {
                ConstructionRowId rowId = ParcelGrid.ConstructionRow(
                    parcel.LogicalRow,
                    lotRow);
                int parcelStart = checked(
                    parcel.LogicalColumn * ParcelGrid.FrontageColumnsPerParcel);
                for (int localStart = 0;
                     localStart < ParcelGrid.FrontageColumnsPerParcel;
                     localStart++)
                {
                    int startColumn = checked(parcelStart + localStart);
                    var lot = new ConstructionLot(
                        parcel.Id,
                        parcel.LogicalColumn,
                        parcel.LogicalRow,
                        rowId,
                        startColumn,
                        BuildingReservation.MinimumFrontageColumns);
                    if (ConstructionLotState(lot) == FrontageCellState.Available)
                    {
                        lots.Add(lot);
                    }
                }
            }
        }
        return lots;
    }

    public FrontageCellState FrontageState(ConstructionRowId rowId, int frontageColumn)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(frontageColumn);
        if (!TryGetAvailableParcelForFrontageCell(rowId, frontageColumn, out _))
        {
            return FrontageCellState.Unavailable;
        }
        foreach (ParcelPlacement placement in _parcelPlacements.Values)
        {
            if (placement.RowId == rowId
                && placement.Reservation.ContainsColumn(frontageColumn))
            {
                return FrontageCellState.ReservedByBuilding;
            }
        }
        foreach (CorridorReservation corridor in _corridorReservations.Values)
        {
            if (corridor.RowId == rowId && corridor.ContainsColumn(frontageColumn))
            {
                return FrontageCellState.ReservedAsCorridor;
            }
        }
        return NaturalResourceOccupiesFrontageCell(rowId, frontageColumn)
            ? FrontageCellState.NaturalResource
            : FrontageCellState.Available;
    }

    public CorridorReservation? TryReserveCorridor(
        ConstructionRowId rowId,
        int startColumn,
        int frontageColumns = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(startColumn);
        if (frontageColumns <= 0) throw new ArgumentOutOfRangeException(nameof(frontageColumns));
        for (int column = startColumn; column < startColumn + frontageColumns; column++)
        {
            if (FrontageState(rowId, column) is not FrontageCellState.Available) return null;
        }
        var reservation = new CorridorReservation(
            _nextCorridorReservationId++,
            rowId,
            startColumn,
            frontageColumns);
        _corridorReservations.Add(reservation.Id, reservation);
        return reservation;
    }

    public bool ReleaseCorridor(int reservationId) =>
        _corridorReservations.Remove(reservationId);

    internal void RegisterCorridorReservation(CorridorReservation reservation)
    {
        for (int column = reservation.StartColumn;
             column < reservation.EndColumnExclusive;
             column++)
        {
            if (FrontageState(reservation.RowId, column) is not FrontageCellState.Available)
            {
                throw new InvalidOperationException(
                    $"Corridor reservation {reservation.Id} overlaps unavailable frontage.");
            }
        }
        if (!_corridorReservations.TryAdd(reservation.Id, reservation))
        {
            throw new InvalidOperationException(
                $"Corridor reservation {reservation.Id} already exists.");
        }
        _nextCorridorReservationId = Math.Max(
            _nextCorridorReservationId,
            reservation.Id + 1);
    }

    /// <summary>
    /// Returns Available only when every frontage cell in the candidate
    /// window can be reserved. Otherwise returns the first blocking state in
    /// deterministic column order so presentation can preview the same reason
    /// before the player confirms.
    /// </summary>
    public FrontageCellState ConstructionLotState(ConstructionLot lot)
    {
        for (int column = lot.StartColumn;
             column < lot.StartColumn + lot.FrontageColumns;
             column++)
        {
            FrontageCellState state = FrontageState(lot.RowId, column);
            if (state is not FrontageCellState.Available)
            {
                return state;
            }
        }
        return FrontageCellState.Available;
    }

    private bool TryGetAvailableParcelForFrontageCell(
        ConstructionRowId rowId,
        int frontageColumn,
        out CityParcel? parcel)
    {
        int parcelRow = rowId.Value / ParcelGrid.ConstructionRowsPerParcel;
        int parcelColumn = frontageColumn / ParcelGrid.FrontageColumnsPerParcel;
        parcel = _parcels.Values.FirstOrDefault(candidate =>
            candidate.LogicalRow == parcelRow
            && candidate.LogicalColumn == parcelColumn
            && candidate.IsUnlocked);
        return parcel is not null;
    }

    private ParcelPlacement CreatePlacement(
        BuildingId entityId,
        ConstructionLot lot,
        string footprintProfileId) =>
        new(
            entityId,
            lot.ParcelId,
            lot.RowId,
            lot.StartColumn,
            lot.FrontageColumns,
            BuildingReservation.RequiredDepthRows,
            BuildingReservation.MinimumFrontageColumns,
            leftExpansionColumns: 0,
            rightExpansionColumns: 0,
            lot.LotColumn,
            lot.LotRow,
            lotWidth: 1,
            lotHeight: 1,
            footprintProfileId,
            BuildingOrientation.South);

    // Architecture Hardening A6: these private helpers are `internal`
    // so the persistence assembly can replay a restore without
    // duplicating the city's mobilisation and placement logic. Domain
    // does not depend on Persistence; Persistence reaches them via
    // InternalsVisibleTo. They remain hidden from Presentation.
    internal bool NaturalResourceOccupiesFrontageCell(
        ConstructionRowId rowId,
        int frontageColumn)
    {
        foreach (NaturalResourcePatch patch in _naturalResourcePatches.Values)
        {
            if (!_parcels.TryGetValue(patch.ParcelId, out CityParcel? parcel)) continue;
            for (int unitId = 0; unitId < patch.UnitReserves.Count; unitId++)
            {
                if (patch.UnitReserves[unitId] <= 0) continue;
                NaturalResourceUnitPosition position = patch.UnitPositions[unitId];
                if (position.GlobalRow(parcel) == rowId
                    && position.GlobalFrontageColumn(parcel) == frontageColumn)
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Drains every natural resource patch to empty. The visual-regression
    /// fixture for the depleted world needs a city whose forests are gone; the
    /// patches themselves survive, because the macro view still indexes them
    /// spatially to lay out the parcel slots.
    ///
    /// <para>Lives here rather than in the controller because
    /// <see cref="NaturalResourcePatch.Gather"/> is <c>internal</c> on
    /// purpose — "no caller outside the domain can drain a patch directly".
    /// The controller used to loop over
    /// <see cref="NaturalResourcePatches"/> and call it anyway. Now the loop
    /// is on the inside, where that rule holds.</para>
    /// </summary>
    public void DrainAllNaturalResourcesForFixtures()
    {
        foreach (NaturalResourcePatch patch in _naturalResourcePatches.Values)
        {
            patch.Gather(int.MaxValue);
        }
    }

    /// <summary>Adds an already-constructed citizen to the city.</summary>
    /// <remarks>Public because admitting a citizen is a genuine domain
    /// command — onboarding, migration and the save migrations all issue it.
    /// It validates the id is free and throws otherwise, so the invariant is
    /// enforced by the method, not by its accessibility.</remarks>
    public void RegisterCitizen(Citizen citizen)
    {
        ArgumentNullException.ThrowIfNull(citizen);
        if (!_citizens.TryAdd(citizen.Id, citizen))
        {
            throw new InvalidOperationException($"Citizen id {citizen.Id.Value} already exists.");
        }
    }

    internal void RegisterBuilding(Building building, bool placeIfMissing = true)
    {
        ArgumentNullException.ThrowIfNull(building);
        if (!_buildings.TryAdd(building.Id, building))
        {
            throw new InvalidOperationException($"Building id {building.Id.Value} already exists.");
        }
        if (placeIfMissing
            && building.Kind != BuildingKind.Forest
            && !_parcelPlacements.ContainsKey(building.Id))
        {
            if (_parcels.Count == 0)
            {
                var parcelId = new ParcelId(1);
                _parcels.Add(parcelId, new CityParcel(parcelId, 0, 0, true));
            }
            ParcelPlacement? placement = FindFirstAvailablePlacement(
                building.Id,
                BuildingFootprintCatalog.ProfileIdFor(building.Kind));
            if (placement is null)
            {
                _buildings.Remove(building.Id);
                throw new InvalidOperationException(
                    $"No available parcel lot for building {building.Id.Value}.");
            }
            RegisterParcelPlacement(placement);
        }
    }

    internal void RegisterProject(ConstructionProject project)
    {
        ArgumentNullException.ThrowIfNull(project);
        if (_buildings.ContainsKey(project.Id) || _cultivationSites.ContainsKey(project.Id))
        {
            throw new InvalidOperationException(
                $"Project id {project.Id.Value} collides with an existing building.");
        }
        if (!_projects.TryAdd(project.Id, project))
        {
            throw new InvalidOperationException(
                $"Project id {project.Id.Value} already exists.");
        }
    }

    public ConstructionProject? GetProject(BuildingId projectId) =>
        _projects.TryGetValue(projectId, out var project) ? project : null;

    public CultivationSite? GetCultivationSite(BuildingId siteId) =>
        _cultivationSites.TryGetValue(siteId, out CultivationSite? site) ? site : null;

    internal void RegisterCultivationSite(CultivationSite site)
    {
        ArgumentNullException.ThrowIfNull(site);
        if (_buildings.ContainsKey(site.Id) || _projects.ContainsKey(site.Id)
            || !_cultivationSites.TryAdd(site.Id, site))
        {
            throw new InvalidOperationException(
                $"Cultivation Site id {site.Id.Value} collides with another entity.");
        }
    }

    /// <summary>True when at least one citizen is assigned to a project or building as a worker.</summary>
    internal bool HasAnyWorkAssignment()
    {
        foreach (var citizen in _citizens.Values)
        {
            if (citizen.CurrentAssignment.HasValue) return true;
        }
        return false;
    }

    /// <summary>
    /// True once at least one Basic Shelter (BuildingKind.Home) has been
    /// completed. The configured workday
    /// (<see cref="GameClock.WorkdayStartTick"/>–<see cref="GameClock.WorkdayEndTick"/>)
    /// is suspended until the founding camp consolidates into a permanent
    /// shelter: solo-survival construction and gathering proceed at any time
    /// of day, and only after the first Home registers does the 08:00–16:00
    /// labour window apply to subsequent buildings and projects. See
    /// docs/EARLY_GAME_RESOURCE_AND_EXPEDITION_PROPOSAL.md §5 — the founding
    /// camp is manual survival labour, not city labour, so a freshly authored
    /// city whose clock starts at midnight can still build its first shelter
    /// before the first dawn.
    /// </summary>
    internal bool HasCompletedFirstShelter()
    {
        foreach (var building in _buildings.Values)
        {
            if (building.Kind == BuildingKind.Home) return true;
        }
        return false;
    }

    /// <summary>
    /// Founding camp bypass: while no Basic Shelter exists, every tick is a
    /// labour tick regardless of <see cref="GameClock.IsDaytime"/>. The
    /// configured workday (08:00–16:00) only governs the city phase that begins
    /// once the founder has built the first Home.
    ///
    /// <para>
    /// Every labour gate must read this, not <see cref="GameClock.IsDaytime"/>
    /// directly. The rule used to live only in the per-tick simulation while
    /// mobilisation and arrival kept their own daytime checks, so authorising the
    /// founding site after 16:00 assigned the founder and then parked them at
    /// home: the project sat on <see cref="ConstructionStopCause.WorkersInTransit"/>
    /// and never progressed.
    /// </para>
    /// </summary>
    /// <remarks>Public because it is a pure query the city status panel
    /// displays ("Labor: Active"). Reading whether the city is working
    /// changes nothing; there is no invariant for <c>internal</c> to protect
    /// here, and it only looked protected while the domain and the HUD shared
    /// one assembly.</remarks>
    public bool IsLaborTime() => GameClock.IsDaytime(_tick) || !HasCompletedFirstShelter();

    private static bool ContainsControlCharacter(string value)
    {
        foreach (char character in value)
        {
            if (char.IsControl(character)) return true;
        }
        return false;
    }

    private static int StableAppearanceSeed(string name, LineageId lineage)
    {
        uint hash = 2166136261;
        foreach (char character in name)
        {
            hash = (hash ^ character) * 16777619;
        }
        foreach (char character in lineage.Value)
        {
            hash = (hash ^ character) * 16777619;
        }
        return (int)(hash & int.MaxValue);
    }

    public Citizen? GetCitizen(CitizenId id) =>
        _citizens.TryGetValue(id, out var citizen) ? citizen : null;

    public Building? GetBuilding(BuildingId id) =>
        _buildings.TryGetValue(id, out var building) ? building : null;

    /// <summary>
    /// Edible stock available to the opening: stored Food plus gathered Wild
    /// Food. The latter is intentionally edible and seed-capable; otherwise
    /// the proposal's eight-unit starting horizon would be decorative.
    /// </summary>
    public int FoodStock
    {
        get
        {
            return _resources.Total(ResourceType.Food)
                + _resources.Total(ResourceType.WildFood);
        }
    }

    /// <summary>Aggregate food capacity across every Farm-kind building.</summary>
    public int MaxFoodStock
    {
        get
        {
            int total = 0;
            foreach (var b in _buildings.Values)
            {
                if (b.Kind == BuildingKind.Farm) total += b.StorageCapacity;
            }
            return total;
        }
    }

    /// <summary>
    /// Total wood available across every Forest-kind building.
    /// Wood lives on each Forest's <see cref="Building.Stock"/>
    /// after the hero gathers it from the Forest's
    /// <see cref="Building.WoodReserve"/>.
    /// </summary>
    public int TotalWood
    {
        get
        {
            return _resources.Total(ResourceType.Wood);
        }
    }

    /// <summary>
    /// Total wood still waiting to be gathered across every Forest.
    /// </summary>
    public int TotalWoodReserve
    {
        get
        {
            int total = 0;
            foreach (NaturalResourcePatch patch in _naturalResourcePatches.Values)
            {
                if (patch.ResourceType == ResourceType.Wood) total += patch.TotalReserve;
            }
            return total;
        }
    }

    /// <summary>
    /// Adds food across Farm-kind buildings in deterministic insertion
    /// order until capacity absorbs the request. Returns the amount
    /// actually deposited.
    /// </summary>
    public int DepositFood(int amount)
    {
        return _resources.Deposit(ResourceType.Food, amount);
    }

    /// <summary>
    /// Atomically removes <paramref name="amount"/> edible stock. Stored Food
    /// is used first, then gathered Wild Food; a failed request changes
    /// neither pool.
    /// </summary>
    public bool TryConsumeFood(int amount)
    {
        int edibleAvailable = _resources.Available(ResourceType.Food)
            + _resources.Available(ResourceType.WildFood);
        if (amount < 0 || edibleAvailable < amount) return false;
        int storedFood = _resources.Available(ResourceType.Food);
        int foodTake = Math.Min(amount, storedFood);
        if (foodTake > 0 && !_resources.TryConsume(ResourceType.Food, foodTake))
        {
            return false;
        }
        int wildTake = amount - foodTake;
        return wildTake <= 0 || _resources.TryConsume(ResourceType.WildFood, wildTake);
    }

    public int DailyFoodRation => Upkeep.FoodPerResidentPerDay(_citizens.Count);

    public int? TicksUntilFirstHarvest
    {
        get
        {
            foreach (CultivationSite site in _cultivationSites.Values)
            {
                return site.State switch
                {
                    CultivationPlotState.Prepared => CultivationRules.GrowthTicks,
                    CultivationPlotState.Sown or CultivationPlotState.Growing =>
                        Math.Max(0, site.ReadyAtTick.GetValueOrDefault() - _tick),
                    CultivationPlotState.Ready => 0,
                    _ => null,
                };
            }
            return null;
        }
    }

    public int FoodHorizonDays => DailyFoodRation <= 0
        ? 0
        : FoodStock / DailyFoodRation;

    public int ProtectedFoodTarget
    {
        get
        {
            int daysUntilHarvest = TicksUntilFirstHarvest is int ticks
                ? (int)Math.Ceiling(ticks / (double)GameClock.TicksPerInGameDay)
                : 0;
            int expeditionFood = 0;
            foreach (ResourceReservation reservation in _resources.Reservations)
            {
                if (reservation.Owner.Kind == ResourceReservationOwnerKind.Expedition
                    && reservation.Resource is ResourceType.Food or ResourceType.WildFood)
                {
                    expeditionFood += reservation.Amount;
                }
            }
            int plannedExpeditionFood = TicksUntilFirstHarvest.HasValue
                ? CultivationRules.PlannedWoodExpeditionFoodSupply
                : 0;
            return DailyFoodRation * (daysUntilHarvest + 1)
                + Math.Max(expeditionFood, plannedExpeditionFood);
        }
    }

    /// <summary>
    /// Returns the first building in the world. Convenience helper
    /// for the prototype: presentation code can default to it when
    /// only one building is in focus.
    /// </summary>
    public Building PrimaryBuilding
    {
        get
        {
            foreach (var building in _buildings.Values)
            {
                return building;
            }
            throw new InvalidOperationException("City world has no building.");
        }
    }

    /// <summary>
    /// Total stock of the given resource across every building that
    /// produces it. Used by the recipe drawdown path to gate
    /// construction authorisation and operating consumption. Iron
    /// is summed from the dedicated <see cref="Building.IronStock"/>
    /// reserve, not from the produced-resource <see cref="Building.Stock"/>.
    /// </summary>
    public int TotalStockOf(ResourceType type)
    {
        return _resources.Total(type);
    }

    /// <summary>
    /// Consumes the requested amount of the resource across every
    /// building that produces it, in insertion order, draining each
    /// up to its stock. Returns <c>false</c> when the city does not
    /// hold enough to satisfy the request; the city is left untouched
    /// on failure (transactional).
    /// </summary>
    public bool TryConsumeResource(ResourceType type, int amount)
    {
        return _resources.TryConsume(type, amount);
    }

    /// <summary>
    /// Consumes the per-tick operating recipe inputs for the given
    /// building. Returns the first missing <see cref="ResourceType"/>
    /// on failure (transactional: no partial drawdown is left
    /// applied). Returns <c>null</c> on success.
    /// </summary>
    private ResourceType? TryConsumeOperatingInputs(Building building, Recipe recipe)
    {
        return _resources.TryConsume(recipe.RequiredInputs, out ResourceType? missing)
            ? null
            : missing;
    }

    /// <summary>
    /// Returns the most recent <see cref="WorldEvent"/> whose
    /// typed subject matches the building identity, or <c>null</c> when none
    /// exists. Used to wire
    /// <see cref="WorldEvent.CauseEventId"/> for causal chains; the
    /// resource filter is intentionally unused here so a blocked
    /// event can still reference the last successful production tick.
    /// </summary>
    public WorldEvent? FindCauseEvent(Building? building = null, ResourceType? resource = null)
    {
        _ = resource; // accepted for future use; not consulted today.
        var events = _log.Events;
        for (int i = events.Count - 1; i >= 0; i--)
        {
            var evt = events[i];
            if (building is not null
                && (evt.Subject.Kind != WorldEventSubjectKind.Building
                    || evt.Subject.EntityId != building.Id.Value)) continue;
            return evt;
        }
        return null;
    }

    /// <summary>
    /// Drains wood from a Forest's remaining reserve and credits it
    /// to the forest's Stock (which the construction recipe gate then
    /// consumes). Returns the amount actually gathered, which may be
    /// less than <paramref name="amount"/> when the reserve runs dry
    /// or the storage capacity is full. Records a
    /// <see cref="WorldEventKind.StockProduced"/> event so the offline
    /// report can surface the gathering activity.
    ///
    /// <para><b>Fixture seam, not a gameplay path.</b> This predates the
    /// forestry gate and does <em>not</em> check
    /// <see cref="ToolKind.PrimitiveAxe"/>, so it can drain mature-tree Wood
    /// that <see cref="TryGatherFromPatch"/> would refuse — contradicting the
    /// cross-domain invariant that mature-tree Wood requires a persisted
    /// forestry capability. It is deliberately <c>internal</c> so no scene,
    /// panel or controller can reach it; the assembly exposes internals to the
    /// test project, which uses this as the cheap way to stock a fixture world.
    /// Gameplay must go through <see cref="TryGatherFromPatch"/>.</para>
    /// </summary>
    internal int GatherWood(BuildingId forestId, int amount)
    {
        return GatherWood(forestId, unitId: null, amount: amount);
    }

    /// <inheritdoc cref="GatherWood(BuildingId, int)"/>
    internal int GatherWood(BuildingId forestId, int? unitId, int amount)
    {
        if (amount <= 0) return 0;
        Citizen? hero = Hero;
        if (hero is null || !hero.IsAvailable)
        {
            return 0;
        }
        if (!_buildings.TryGetValue(forestId, out var forest)) return 0;
        if (forest.Kind != BuildingKind.Forest) return 0;
        int gathered;
        if (_naturalResourcePatches.TryGetValue(
            forestId.Value,
            out NaturalResourcePatch? patch))
        {
            int drained = unitId.HasValue
                ? patch.GatherUnit(unitId.Value, amount)
                : patch.Gather(amount);
            forest.RestoreWoodUnits(patch.UnitReserves);
            gathered = _resources.DepositToCityInventory(
                ResourceType.Wood,
                drained);
        }
        else
        {
            gathered = unitId.HasValue
                ? forest.GatherWoodUnit(unitId.Value, amount)
                : forest.GatherWood(amount);
        }
        if (gathered > 0)
        {
            if (unitId.HasValue)
            {
                hero.VisitResource(
                    forestId,
                    unitId.Value,
                    ResourcePositionIndex(forestId, unitId.Value));
            }
            WorldEventId? cause = FindCauseEvent(forest)?.Id;
            _log.Record(_tick, WorldEventKind.StockProduced,
                WorldEventSubject.Building(forest.Id, forest.DisplayName), gathered, cause);
            RaiseBuildingChanged(forestId);
            EnsureFoundingShelterContributor();
        }
        return gathered;
    }

    /// <summary>
    /// EG-1 generalised gather. Drains up to <paramref name="amount"/>
    /// units from the named <see cref="NaturalResourcePatch"/> and
    /// credits them to the city inventory under the patch's own
    /// <see cref="NaturalResourcePatch.ResourceType"/>. Returns the
    /// amount actually gathered (0 if the patch is depleted, the hero
    /// is unavailable, or the city inventory is full under the
    /// carrying cap). When the patch is the legacy Forest
    /// (<see cref="NaturalResourcePatch.LegacyStorageBuildingId"/>
    /// pointing at a <see cref="BuildingKind.Forest"/>), the Forest's
    /// mirrored WoodUnitReserves stay in sync with the patch so the
    /// legacy building entity never lies about its remaining wood.
    /// </summary>
    public NaturalResourceGatherResult NaturalResourceGatherAvailability(
        int patchId,
        int? unitId)
    {
        Citizen? hero = Hero;
        if (hero is null || !hero.IsAvailable)
        {
            return new NaturalResourceGatherResult(
                NaturalResourceGatherOutcome.HeroUnavailable);
        }
        if (!_naturalResourcePatches.TryGetValue(patchId, out NaturalResourcePatch? patch))
        {
            return new NaturalResourceGatherResult(
                NaturalResourceGatherOutcome.NodeUnavailable);
        }
        if (unitId is int requestedUnit
            && (requestedUnit < 0
                || requestedUnit >= patch.UnitReserves.Count
                || patch.UnitReserves[requestedUnit] <= 0))
        {
            return new NaturalResourceGatherResult(
                NaturalResourceGatherOutcome.NodeUnavailable);
        }
        if (patch.TotalReserve <= 0)
        {
            return new NaturalResourceGatherResult(
                NaturalResourceGatherOutcome.NodeUnavailable);
        }
        if (patch.ResourceType == ResourceType.Wood
            && !HasTool(ToolKind.PrimitiveAxe))
        {
            return new NaturalResourceGatherResult(
                NaturalResourceGatherOutcome.MissingRequiredTool,
                RequiredTool: ToolKind.PrimitiveAxe);
        }

        if (IsCarriedGroundResource(patch.ResourceType))
        {
            int headroom = AvailableGroundGatherCapacity();
            if (headroom <= 0)
            {
                return new NaturalResourceGatherResult(
                    NaturalResourceGatherOutcome.StorageFull);
            }
        }

        return new NaturalResourceGatherResult(NaturalResourceGatherOutcome.Available);
    }

    public NaturalResourceGatherResult TryGatherFromPatch(
        int patchId,
        int? unitId,
        int amount)
    {
        if (amount <= 0)
        {
            return new NaturalResourceGatherResult(
                NaturalResourceGatherOutcome.NodeUnavailable);
        }
        NaturalResourceGatherResult availability =
            NaturalResourceGatherAvailability(patchId, unitId);
        if (!availability.CanGather) return availability;
        NaturalResourcePatch patch = _naturalResourcePatches[patchId];
        Citizen hero = Hero!;

        int requested = amount;
        if (IsCarriedGroundResource(patch.ResourceType))
        {
            int headroom = AvailableGroundGatherCapacity();
            requested = Math.Min(requested, headroom);
        }

        int drained = unitId.HasValue
            ? patch.GatherUnit(unitId.Value, requested)
            : patch.Gather(requested);

        if (drained <= 0)
        {
            return new NaturalResourceGatherResult(
                NaturalResourceGatherOutcome.NodeUnavailable);
        }

        // Legacy mirror: the Forest building keeps WoodUnitReserves for
        // any caller that still reads it (recipe gate, visual regressions).
        if (patch.LegacyStorageBuildingId is BuildingId buildingId
            && _buildings.TryGetValue(buildingId, out var legacyBuilding)
            && legacyBuilding.Kind == BuildingKind.Forest)
        {
            legacyBuilding.RestoreWoodUnits(patch.UnitReserves);
        }

        int gathered = _resources.DepositToCityInventory(patch.ResourceType, drained);

        if (gathered > 0)
        {
            if (unitId.HasValue)
            {
                hero.VisitResource(
                    patchId,
                    unitId.Value,
                    GroundResourcePositionIndex(patchId, unitId.Value));
            }
            _log.Record(_tick, WorldEventKind.StockProduced,
                WorldEventSubject.Patch(patch.Id, patch.ResourceType.ToString()),
                gathered);
            RaisePatchChanged(patchId);
        }
        return gathered > 0
            ? new NaturalResourceGatherResult(
                NaturalResourceGatherOutcome.Gathered,
                gathered)
            : new NaturalResourceGatherResult(
                NaturalResourceGatherOutcome.StorageFull);
    }

    public int GatherFromPatch(int patchId, int? unitId, int amount) =>
        TryGatherFromPatch(patchId, unitId, amount).GatheredAmount;

    private static bool IsCarriedGroundResource(ResourceType type)
    {
        foreach (ResourceType carried in CarriedGroundResourceTypes)
        {
            if (carried == type) return true;
        }
        return false;
    }

    private int GroundResourcePositionIndex(int patchId, int unitId)
    {
        // Linear index over ground patches by their natural-order id,
        // independent of Forest-specific bookkeeping. The macro view
        // uses this to remember where the hero last picked from; the
        // exact pixel offset is recovered from the snapshot.
        int positionIndex = 0;
        foreach (NaturalResourcePatch patch in _naturalResourcePatches.Values)
        {
            if (patch.Id == patchId)
            {
                return positionIndex + unitId;
            }
            positionIndex += patch.UnitReserves.Count;
        }
        return Math.Max(0, unitId);
    }

    private void RaisePatchChanged(int patchId)
    {
        PatchChanged?.Invoke(this, new PatchChangedEventArgs(patchId));
    }

    /// <summary>
    /// Raised whenever a <see cref="NaturalResourcePatch"/>'s
    /// <see cref="NaturalResourcePatch.UnitReserves"/> change so the
    /// presentation layer can refresh the ground-resource overlay.
    /// </summary>
    public event EventHandler<PatchChangedEventArgs>? PatchChanged;

    internal int ResourcePositionIndex(BuildingId forestId, int unitId)
    {
        int positionIndex = 0;
        foreach (Building building in _buildings.Values)
        {
            if (building.Kind != BuildingKind.Forest) continue;
            if (building.Id == forestId)
            {
                return positionIndex + unitId;
            }
            positionIndex += building.WoodUnitReserves.Count;
        }
        return Math.Max(0, unitId);
    }

    /// <summary>
    /// Returns the citizens that are not currently assigned to any
    /// building, in deterministic insertion order. The presentation
    /// layer uses this to populate the assignment panel.
    /// </summary>
    public IReadOnlyList<Citizen> AvailableCitizens()
    {
        var list = new List<Citizen>();
        foreach (var citizen in _citizens.Values)
        {
            if (citizen.IsAvailable)
            {
                list.Add(citizen);
            }
        }
        return list;
    }

    /// <summary>
    /// Same set as <see cref="AvailableCitizens"/> but ordered so the
    /// highest-priority productive building shows first. The domain
    /// owns the policy; consumers (the assignment panel) just render.
    /// When no productive building exists, falls back to insertion
    /// order.
    /// </summary>
    public IReadOnlyList<Citizen> AvailableCitizensByPriority()
    {
        var list = new List<Citizen>(AvailableCitizens());
        int topPriority = -1;
        foreach (var b in _buildings.Values)
        {
            if (b.Priority > topPriority) topPriority = b.Priority;
        }
        // When there is a productive building, the most relevant
        // priority ranks first; the panel renders this order. With
        // no productive building the list stays in insertion order.
        if (topPriority >= 0)
        {
            list.Sort((a, b) => string.Compare(a.Name, b.Name, System.StringComparison.Ordinal));
        }
        return list;
    }

    /// <summary>
    /// Attempts to assign a citizen to a building. The domain
    /// validates the operation end-to-end: the building must exist,
    /// the citizen must exist, the citizen must not already be
    /// assigned elsewhere, and the building must have spare worker
    /// capacity.
    /// </summary>
    public AssignmentResult TryAssignCitizen(BuildingId buildingId, CitizenId citizenId)
        => _assignments.AssignToBuilding(buildingId, citizenId, _tick);

    /// <summary>
    /// Attempts to remove a citizen from a building.
    /// </summary>
    public AssignmentResult TryUnassignCitizen(BuildingId buildingId, CitizenId citizenId)
        => _assignments.UnassignFromBuilding(buildingId, citizenId);

    /// <summary>
    /// Unassigns the citizens who have physically arrived at the building. Used
    /// by the auto-release watch when a building has been at max stock
    /// long enough to rule out a brief production peak. Citizens still
    /// travelling retain their commitment until they arrive; another
    /// worker finishing a batch must not cancel their journey.
    /// </summary>
    private void ReleaseAssignedWorkers(Building building) =>
        _assignments.PauseArrivedWorkers(building, _tick);

    /// <summary>
    /// Attempts to assign a citizen to a worksite. The id is shared
    /// with the future building so <see cref="Citizen.CurrentAssignment"/>
    /// remains a plain <see cref="BuildingId"/>?>.
    /// </summary>
    public AssignmentResult TryAssignToProject(BuildingId projectId, CitizenId citizenId)
        => _assignments.AssignToProject(projectId, citizenId, _tick);

    public AssignmentResult TryUnassignFromProject(BuildingId projectId, CitizenId citizenId)
        => _assignments.UnassignFromProject(projectId, citizenId);

    /// <summary>
    /// Authorises the first worksite — the Basic Shelter. The id is
    /// the next reserved <see cref="BuildingId"/>, distinct from any
    /// existing building or citizen.
    /// </summary>
    public ConstructionAuthorizationResult TryAuthorizeBasicShelter()
        => TryAuthorizeConstruction(ConstructionKind.BasicShelter);

    /// <summary>
    /// Authorises one worksite at a time. Productive buildings become
    /// available after the founding shelter exists; every kind uses
    /// the same phased progress model with its own work requirement.
    /// Material cost is debited up-front as a deposit; the remainder
    /// is drained one unit per work interval while the project is
    /// active. On cancellation, inputs already consumed remain spent;
    /// the recorded remainder was never debited and is simply discarded.
    /// </summary>
    public ConstructionAuthorizationResult TryAuthorizeConstruction(
        ConstructionKind kind,
        ConstructionLot? selectedLot = null)
    {
        if (Hero is null)
        {
            return ConstructionAuthorizationResult.Fail(ConstructionAuthorizationOutcome.NoHero);
        }
        if (_projects.Count > 0)
        {
            return ConstructionAuthorizationResult.Fail(ConstructionAuthorizationOutcome.AlreadyAuthorized);
        }
        bool hasHome = false;
        bool isFoundingConstruction = kind is ConstructionKind.BasicShelter or ConstructionKind.FoundingSite;
        foreach (var building in _buildings.Values)
        {
            if (kind == ConstructionKind.TownHall && building.Kind == BuildingKind.TownHall)
            {
                return ConstructionAuthorizationResult.Fail(
                    ConstructionAuthorizationOutcome.BuildingAlreadyBuilt);
            }
            if (building.Kind == BuildingKind.Home)
            {
                hasHome = true;
                if (isFoundingConstruction)
                {
                    return ConstructionAuthorizationResult.Fail(ConstructionAuthorizationOutcome.HomeAlreadyBuilt);
                }
            }
        }
        if (kind == ConstructionKind.CultivationSite
            && _cultivationSites.Count > 0)
        {
            return ConstructionAuthorizationResult.Fail(
                ConstructionAuthorizationOutcome.BuildingAlreadyBuilt);
        }
        if (isFoundingConstruction && _citizens.Count > 1)
        {
            return ConstructionAuthorizationResult.Fail(ConstructionAuthorizationOutcome.WorldNotEmpty);
        }
        if (!isFoundingConstruction && !hasHome)
        {
            return ConstructionAuthorizationResult.Fail(ConstructionAuthorizationOutcome.HomeRequired);
        }

        var projectId = NextAvailableProjectId();
        string footprintProfileId = BuildingFootprintCatalog.ProfileIdFor(kind);
        ParcelPlacement? placement = selectedLot.HasValue
            && AvailableConstructionLots().Contains(selectedLot.Value)
                ? CreatePlacement(projectId, selectedLot.Value, footprintProfileId)
                : selectedLot.HasValue
                    ? null
                    : FindFirstAvailablePlacement(projectId, footprintProfileId);
        if (placement is null)
        {
            return ConstructionAuthorizationResult.Fail(
                ConstructionAuthorizationOutcome.NoAvailableLot);
        }

        // Recipe gate: Founding Site modules pay their full bounded cost before
        // labour begins so the sole founder is never committed to a phase while
        // still needing to gather its inputs. Legacy buildings retain the 25%
        // deposit plus interval drawdown.
        // (deposit = ceil(total * 0.25)) or the authorisation fails
        // and the city state is unchanged.
        var recipe = Recipes.ConstructionRecipeFor(kind);
        if (recipe is not null && recipe.RequiredInputs.Count > 0)
        {
            var deposits = new List<RecipeInput>();
            foreach (var input in recipe.RequiredInputs)
            {
                int deposit = kind is ConstructionKind.FoundingSite
                    or ConstructionKind.CultivationSite
                    ? input.Amount
                    : ConstructionRules.DepositOf(input.Amount);
                if (deposit > 0) deposits.Add(new RecipeInput(input.Resource, deposit));
            }
            if (TryConsumeResources(deposits) is not null)
            {
                return ConstructionAuthorizationResult.Fail(ConstructionAuthorizationOutcome.MissingMaterials);
            }
        }

        var project = new ConstructionProject(
            id: projectId,
            kind: kind,
            displayName: ConstructionRules.DisplayNameFor(kind),
            requiredWork: ConstructionRules.RequiredWorkFor(kind),
            workerCapacity: kind == ConstructionKind.CultivationSite
                ? CultivationRules.WorkerCapacity
                : ConstructionRules.WorkerCapacity,
            enabled: true)
        {
            StopCause = ConstructionStopCause.NoWorkers,
        };
        if (kind == ConstructionKind.FoundingSite)
        {
            project.BeginFoundingModule(
                FoundingSiteModule.Campfire,
                _tick,
                FoundingSiteRules.InputsFor(FoundingSiteModule.Campfire));
        }
        // Seed the remaining-inputs list from the recipe. Each entry
        // starts at the post-deposit remainder; the simulation drains
        // it 1 unit per work interval.
        if (kind is not (ConstructionKind.FoundingSite or ConstructionKind.CultivationSite)
            && recipe is not null && recipe.RequiredInputs.Count > 0)
        {
            var remaining = new List<RecipeInput>();
            foreach (var input in recipe.RequiredInputs)
            {
                int after = ConstructionRules.RemainderAfterDeposit(input.Amount);
                if (after > 0)
                {
                    remaining.Add(new RecipeInput(input.Resource, after));
                }
            }
            project.SetRemainingInputs(remaining);
        }
        RegisterProject(project);
        RegisterParcelPlacement(placement);
        if (isFoundingConstruction)
        {
            EnsureFoundingShelterContributor();
        }
        RaiseProjectChanged(projectId);
        return ConstructionAuthorizationResult.Success(projectId);
    }

    /// <summary>Starts the next valid phase on the existing Founding Site.</summary>
    public ConstructionAuthorizationResult TryAuthorizeFoundingSiteModule(
        BuildingId projectId,
        FoundingSiteModule module)
    {
        if (!_projects.TryGetValue(projectId, out ConstructionProject? project)
            || project.Kind != ConstructionKind.FoundingSite)
        {
            return ConstructionAuthorizationResult.Fail(
                ConstructionAuthorizationOutcome.InvalidModule);
        }
        if (!project.CanStartFoundingModule(module))
        {
            return ConstructionAuthorizationResult.Fail(
                ConstructionAuthorizationOutcome.PrerequisitesNotMet);
        }

        IReadOnlyList<RecipeInput> inputs = FoundingSiteRules.InputsFor(module);
        if (TryConsumeResources(inputs) is not null)
        {
            return ConstructionAuthorizationResult.Fail(
                ConstructionAuthorizationOutcome.MissingMaterials);
        }
        if (!project.BeginFoundingModule(module, _tick, inputs))
        {
            return ConstructionAuthorizationResult.Fail(
                ConstructionAuthorizationOutcome.InvalidModule);
        }
        // The previous module released its contributors on completion, so the
        // founder has to be re-mobilised here exactly as
        // TryAuthorizeConstruction does for the Campfire. Without this the
        // authorised module would sit on NoWorkers and the player would have to
        // assign themselves through the panel between every module.
        EnsureFoundingShelterContributor();
        RaiseProjectChanged(projectId);
        return ConstructionAuthorizationResult.Success(projectId);
    }

    /// <summary>
    /// Assigns the lone available founder to an in-flight Basic Shelter.
    /// Used on authorisation and once after loading older stalled saves.
    /// It never overrides an existing assignment or a deliberate contributor.
    /// </summary>
    public bool EnsureFoundingShelterContributor()
    {
        Citizen? hero = Hero;
        if (hero is null || !hero.IsAvailable) return false;
        foreach (ConstructionProject project in _projects.Values)
        {
            if (project.Kind is not (ConstructionKind.BasicShelter or ConstructionKind.FoundingSite)
                || project.AssignedCount > 0
                || project.Progress >= project.RequiredWork
                || !project.HasActiveWork
                || !HasRemainingInputsAvailable(project))
            {
                continue;
            }
            return _assignments.AssignToProject(project.Id, hero.Id, _tick).IsSuccess;
        }
        return false;
    }

    private bool HasRemainingInputsAvailable(ConstructionProject project)
    {
        foreach (RecipeInput input in project.RemainingInputs)
        {
            if (_resources.Available(input.Resource) < input.Amount) return false;
        }
        return true;
    }

    /// <summary>
    /// Cancels an in-flight project. Inputs already consumed by the
    /// deposit or subsequent work intervals remain spent. RemainingInputs
    /// represent amounts not yet debited, so cancellation must not deposit
    /// them or it would create resources.
    /// </summary>
    public bool CancelProject(BuildingId projectId)
    {
        if (!_projects.TryGetValue(projectId, out var project)) return false;
        if (project.Kind == ConstructionKind.FoundingSite
            && project.CompletedFoundingModules.Count > 0)
        {
            return false;
        }
        foreach (var cid in project.AssignedCitizenIds)
        {
            if (_citizens.TryGetValue(cid, out var citizen))
            {
                citizen.ReleaseCommitment(
                    CitizenCommitmentKind.Construction,
                    projectId.Value);
            }
        }
        _projects.Remove(projectId);
        _parcelPlacements.Remove(projectId);
        RaiseProjectChanged(projectId);
        return true;
    }

    /// <summary>
    /// Adds the given amount of resource to the city aggregate.
    /// Used by explicit deposit paths such as test setup and future
    /// rewards or expeditions returning with goods.
    /// Iron flows to <see cref="Building.IronStock"/>; everything
    /// else flows to the produced-resource <see cref="Building.Stock"/>.
    /// </summary>
    public void DepositResource(ResourceType type, int amount)
    {
        _resources.Deposit(type, amount);
    }

    private ResourceType? TryConsumeResources(IReadOnlyList<RecipeInput> inputs) =>
        _resources.TryConsume(inputs, out ResourceType? missing) ? null : missing;

    private BuildingId NextAvailableProjectId()
    {
        var candidate = new BuildingId(_nextProjectId);
        while (_buildings.ContainsKey(candidate)
            || _projects.ContainsKey(candidate)
            || _cultivationSites.ContainsKey(candidate))
        {
            candidate = new BuildingId(++_nextProjectId);
        }
        _nextProjectId++;
        return candidate;
    }

    /// <summary>Toggles whether the project continues to accumulate work.</summary>
    public void SetProjectEnabled(BuildingId projectId, bool enabled)
    {
        if (!_projects.TryGetValue(projectId, out var project)) return;
        project.Enabled = enabled;
        RaiseProjectChanged(projectId);
    }

    /// <summary>
    /// Advances the world by one tick and credits the building
    /// with its current production. Returns the amount of stock
    /// actually added (storage capacity can absorb less than
    /// produced when stock is near full). Day/night agnostic —
    /// callers that want the full world tick should use
    /// <see cref="AdvanceWorldTick"/>.
    /// </summary>
    public int AdvanceProduction(BuildingId buildingId)
    {
        if (!_buildings.TryGetValue(buildingId, out var building))
        {
            return 0;
        }
        _tick++;
        int added = SimulateBuildingTick(building);
        if (added > 0
            || building.StopCause == ProductionStopCause.WorkersExhausted)
        {
            RaiseBuildingChanged(building.Id);
        }
        return added;
    }

    /// <summary>
    /// One world tick. Canonical order: clock advance → mobilisation
    /// at day/night boundary → per-building behavior (day: produce;
    /// night: rest) → per-project behaviour (day: contribute at
    /// work intervals; night: rest) → buffs. Upkeep was previously
    /// drained here; that placeholder is dormant until real
    /// building-driven demand exists. Project completion is deferred
    /// to the end of the tick so the project dictionary is not
    /// mutated while iterating.
    ///
    /// <para>
    /// There is exactly one tick. Live play and offline catch-up run this same
    /// method, so a journey ends because world time says so and never because a
    /// sprite did or did not reach an anchor. See <c>DEC-0023</c>.
    /// </para>
    /// </summary>
    public void AdvanceWorldTick()
    {
        int previousTick = _tick;
        _tick++;
        bool dayChanged = DetectAndApplyMobilisation(previousTick, _tick);
        // The authored first night holds the calendar. Its milestones, not the
        // clock, decide when dawn happens, so a player who reads slowly can
        // still be at the campfire when the tick crosses 08:00. Charging a Food
        // ration and announcing daybreak underneath the narration would
        // contradict what the game is showing them, and the ration is the only
        // consequence that can fire inside the night's window at all.
        if (dayChanged && !IsFirstNightActive)
        {
            if (GameClock.IsDaytime(_tick))
            {
                _log.Record(_tick, WorldEventKind.DayBegan, WorldEventSubject.World("Sun"));
                ApplyResidentFoodRation();
            }
            else _log.Record(_tick, WorldEventKind.NightBegan, WorldEventSubject.World("Sun"));
        }
        ProcessCitizenNeedsAndStandingOrders();
        AdvanceWoundRecoveries();
        bool isLaborTime = IsLaborTime();
        foreach (var building in _buildings.Values)
        {
            building.LastTickProduction = 0;
            if (isLaborTime)
            {
                ProductionStopCause previousStopCause = building.StopCause;
                // Reactive resume: a building whose stock has fallen
                // to or below its MinStock since the last MaxStock cap
                // is unblocked and can produce again next tick.
                if (building.Stock <= building.MinStock)
                {
                    building.ResumeIfBelowMin();
                }

                int added = SimulateBuildingTick(building);
                if (added > 0)
                {
                    WorldEventId? cause = FindCauseEvent(building)?.Id;
                    _log.Record(_tick, WorldEventKind.StockProduced,
                        WorldEventSubject.Building(building.Id, building.DisplayName), added, cause);
                }
                if (building.StopCause == ProductionStopCause.WorkersExhausted)
                {
                    _log.Record(_tick, WorldEventKind.WorkersExhausted,
                        WorldEventSubject.Building(building.Id, building.DisplayName));
                }
                if (building.StopCause == ProductionStopCause.TargetReached
                    && previousStopCause != ProductionStopCause.TargetReached)
                {
                    _log.Record(_tick, WorldEventKind.StockCapped,
                        WorldEventSubject.Building(building.Id, building.DisplayName));
                }
                if (added > 0
                    || building.StopCause == ProductionStopCause.WorkersExhausted)
                {
                    RaiseBuildingChanged(building.Id);
                }

                // Auto-release workers after the building has been at
                // max stock long enough to rule out a brief production
                // peak. Any consumption that drops the stock below the
                // cap resets the watch.
                if (building.AssignedCount > 0
                    && HasWorkerAtBuilding(building)
                    && building.TickMaxStockWatch())
                {
                    ReleaseAssignedWorkers(building);
                }
            }
            else
            {
                ApplyNightRest(building);
            }
        }
        bool isWorkInterval = _tick > 0
            && (_tick % ConstructionRules.WorkIntervalTicks == 0);
        int completed = 0;
        foreach (var project in _projects.Values)
        {
            int previousProgress = project.Progress;
            ConstructionStopCause previousStopCause = project.StopCause;
            project.LastTickProgressAdded = 0;
            if (isLaborTime)
            {
                _construction.SimulateTick(project, isWorkInterval);
                if (project.LastTickProgressAdded > 0)
                {
                    _log.Record(_tick, WorldEventKind.ProjectProgressed,
                        WorldEventSubject.ConstructionProject(project.Id, project.DisplayName),
                        project.LastTickProgressAdded);
                }
                if (project.Progress != previousProgress
                    || project.StopCause != previousStopCause)
                {
                    RaiseProjectChanged(project.Id);
                }
            }
            else
            {
                _construction.ApplyNightRest(project);
            }
            if (project.Progress >= project.RequiredWork) completed++;
        }
        for (int i = 0; i < completed; i++)
        {
            // We cannot iterate _projects here, but the deferred
            // completion list would be heavier than two passes;
            // instead we re-query the dictionary of project ids
            // that crossed the threshold this tick. A second pass
            // over the dictionary is O(n) and avoids the iterator
            // mutation hazard.
        }
        InterruptCitizensRequiringRecovery();
        CompleteFinishedProjects();
        AdvanceCultivationSites();
        DemolishDepletedForests();
        DecrementAllWellFed();
        AdvanceExpeditionPhases();
        CompleteFinishedExpeditions();
    }

    private void CompleteFinishedExpeditions()
    {
        if (_expeditions.Count == 0) return;
        ExpeditionId? completed = null;
        foreach (Expedition expedition in _expeditions.Values)
        {
            if (CanCompleteExpedition(expedition))
            {
                completed = expedition.Id;
                break;
            }
        }
        if (completed is null) return;

        foreach (Expedition expedition in _expeditions.Values)
        {
            if (!CanCompleteExpedition(expedition))
            {
                continue;
            }
            ExpeditionEncounterOutcome outcome =
                expedition.EncounterOutcome ?? ExpeditionEncounterOutcome.Setback;
            bool committed = expedition.ReservationId is not ResourceReservationId reservationId
                || _resources.Commit(reservationId);
            if (committed)
            {
                // The encounter always resolves before completion (see
                // AdvanceExpeditionPhases), but a defensively-missing
                // outcome (e.g. a DurationTicks=0 edge no validation should
                // allow) must not silently grant the full reward.
                if (expedition.RetreatTriggered)
                {
                    ReleaseResourceOpportunity(expedition);
                    expedition.MarkRetreated();
                    ReturnMembersFromExpedition(expedition, outcome);
                    _log.Record(
                        _tick,
                        WorldEventKind.ExpeditionRetreated,
                        WorldEventSubject.Expedition(
                            expedition.Id.Value,
                            expedition.DisplayName),
                        causeEventId: expedition.DispatchEventId);
                }
                else if (expedition.RewardKind == ExpeditionRewardKind.Migrant)
                {
                    MigrantOutcome prospect = outcome == ExpeditionEncounterOutcome.Setback
                        ? MigrantOutcome.NoProspect
                        : TryHostExpeditionProspect();
                    if (prospect == MigrantOutcome.Success)
                    {
                        expedition.MarkReturnedProspect();
                        ReturnMembersFromExpedition(expedition, outcome);
                        _log.Record(
                            _tick,
                            WorldEventKind.ExpeditionReturned,
                            WorldEventSubject.Expedition(
                                expedition.Id.Value,
                                expedition.DisplayName),
                            0,
                            expedition.DispatchEventId);
                    }
                    else
                    {
                        expedition.MarkFailed();
                        ReturnMembersFromExpedition(expedition, outcome);
                        _log.Record(
                            _tick,
                            WorldEventKind.ExpeditionFailed,
                            WorldEventSubject.Expedition(
                                expedition.Id.Value,
                                expedition.DisplayName),
                            causeEventId: expedition.DispatchEventId);
                    }
                }
                else if (expedition.RewardKind == ExpeditionRewardKind.Discovery)
                {
                    DepleteResourceOpportunity(expedition);
                    expedition.MarkReturnedDiscovery();
                    ReturnMembersFromExpedition(expedition, outcome);
                    _log.Record(
                        _tick,
                        WorldEventKind.ExpeditionReturned,
                        WorldEventSubject.Expedition(
                            expedition.Id.Value,
                            expedition.DisplayName),
                        0,
                        expedition.DispatchEventId);
                }
                else
                {
                    int reward = expedition.ReturnFor(outcome);
                    if (reward > 0)
                    {
                        reward = expedition.ResourceOpportunityId.HasValue
                            ? DepositToFoundingStorage(
                                expedition.RewardResource!.Value,
                                reward,
                                expedition.Id)
                            : _resources.DepositToCityInventory(
                                expedition.RewardResource!.Value,
                                reward);
                    }
                    DepleteResourceOpportunity(expedition);
                    expedition.MarkReturnedSupplies(reward);
                    AdvanceTerritoryFromExpedition(expedition, outcome);
                    ReturnMembersFromExpedition(expedition, outcome);
                    _log.Record(
                        _tick,
                        WorldEventKind.ExpeditionReturned,
                        WorldEventSubject.Expedition(
                            expedition.Id.Value,
                            expedition.DisplayName),
                        reward,
                        expedition.DispatchEventId);
                }
            }
            else
            {
                if (expedition.ReservationId is ResourceReservationId failedReservationId)
                    _resources.Release(failedReservationId);
                ReleaseResourceOpportunity(expedition);
                expedition.MarkFailed();
                ReturnMembersFromExpedition(expedition, outcome);
                _log.Record(
                    _tick,
                    WorldEventKind.ExpeditionFailed,
                    WorldEventSubject.Expedition(expedition.Id.Value, expedition.DisplayName),
                    causeEventId: expedition.DispatchEventId);
            }
            ExpeditionChanged?.Invoke(
                this,
                new ExpeditionChangedEventArgs(expedition.Id, expedition.Status));
            _combatSessions.Remove(expedition.Id);
        }
    }

    private bool CanCompleteExpedition(Expedition expedition) =>
        expedition.Status == ExpeditionStatus.Active
        && expedition.IsComplete(_tick)
        && (!UsesObservableCombat(expedition) || expedition.EncounterOutcome.HasValue);

    private void ReleaseResourceOpportunity(Expedition expedition)
    {
        if (expedition.ResourceOpportunityId is ResourceOpportunityId opportunityId
            && _resourceOpportunities.TryGetValue(
                opportunityId,
                out ResourceOpportunity? opportunity))
        {
            opportunity.Release(expedition.Id);
        }
    }

    private void DepleteResourceOpportunity(Expedition expedition)
    {
        if (expedition.ResourceOpportunityId is ResourceOpportunityId opportunityId
            && _resourceOpportunities.TryGetValue(
                opportunityId,
                out ResourceOpportunity? opportunity))
        {
            opportunity.Deplete(expedition.Id);
        }
    }

    private void AdvanceTerritoryFromExpedition(
        Expedition expedition,
        ExpeditionEncounterOutcome outcome)
    {
        if (outcome == ExpeditionEncounterOutcome.Setback
            || expedition.TargetParcelId is not ParcelId parcelId
            || !_parcels.TryGetValue(parcelId, out CityParcel? parcel))
        {
            return;
        }
        while (parcel.AdvanceTerritory())
        {
            _log.Record(
                _tick,
                WorldEventKind.TerritoryAdvanced,
                WorldEventSubject.Parcel(parcel.Id, $"Parcel {parcel.Id.Value}"),
                (int)parcel.TerritoryState,
                expedition.DispatchEventId);
        }
    }

    /// <summary>
    /// Reduces (or zeroes) a Supplies-kind reward by the encounter outcome.
    /// A Migrant-kind reward is handled separately in
    /// <see cref="CompleteFinishedExpeditions"/> (a prospect is binary —
    /// there is no "half a prospect").
    /// </summary>
    internal static int ApplyEncounterOutcomeToReward(int baseAmount, ExpeditionEncounterOutcome outcome) =>
        outcome switch
        {
            ExpeditionEncounterOutcome.FullSuccess => baseAmount,
            ExpeditionEncounterOutcome.PartialSuccess => Math.Max(1, baseAmount / 2),
            _ => 0,
        };

    /// <summary>
    /// Steps every active expedition through its phase quarters once per tick,
    /// live and offline equivalently, and resolves the encounter exactly once.
    /// The phase chain — Outbound → Encounter → Objective or Retreating →
    /// Returning → Resolved — is documented in <c>ExpeditionPhase</c> and is
    /// the contract the EG-4 resource sorties will build on.
    /// offline alike (called from the single shared tick body). Uses
    /// sequential ifs re-checking the just-updated phase, not a switch, so
    /// a large offline catch-up jump that lands past more than one boundary
    /// in a single call still cascades through all of them in order rather
    /// than getting stuck one phase behind.
    /// </summary>
    private void AdvanceExpeditionPhases()
    {
        if (_expeditions.Count == 0) return;
        foreach (Expedition expedition in _expeditions.Values)
        {
            if (expedition.Status != ExpeditionStatus.Active) continue;
            int duration = expedition.EndTick - expedition.StartTick;
            if (duration <= 0) continue;
            int elapsed = _tick - expedition.StartTick;

            if (expedition.Phase == ExpeditionPhase.Outbound
                && elapsed >= ExpeditionTiming.EncounterOffsetTicks(expedition))
            {
                expedition.BeginEncounter();
                if (UsesObservableCombat(expedition))
                {
                    _combatSessions[expedition.Id] =
                        ExpeditionCombatSessionFactory.Create(expedition, _citizens);
                }
                else
                {
                    CompleteExpeditionEncounter(
                        expedition,
                        ResolveEncounterOutcome(expedition));
                }
                ExpeditionChanged?.Invoke(
                    this,
                    new ExpeditionChangedEventArgs(expedition.Id, expedition.Status));
            }
            if (expedition.Phase == ExpeditionPhase.Encounter
                && !expedition.EncounterOutcome.HasValue
                && _combatSessions.TryGetValue(expedition.Id, out CombatSession? session))
            {
                CombatAdvanceResult advance = session.Advance();
                if (advance.Outcome != CombatOutcome.InProgress)
                {
                    CompleteExpeditionEncounter(
                        expedition,
                        ToExpeditionOutcome(advance.Outcome));
                }
            }
            if (expedition.Phase == ExpeditionPhase.Encounter
                && expedition.RetreatTriggered)
            {
                expedition.BeginRetreat();
            }
            if (expedition.Phase == ExpeditionPhase.Retreating && elapsed >= duration / 2)
            {
                expedition.TryAdvancePhase(ExpeditionPhase.Returning);
            }
            if (expedition.Phase == ExpeditionPhase.Encounter
                && expedition.EncounterOutcome.HasValue
                && (ExpeditionTiming.IsSpiritTrail(expedition) || elapsed >= duration / 2))
            {
                expedition.TryAdvancePhase(ExpeditionPhase.Objective);
            }
            if (expedition.Phase == ExpeditionPhase.Objective
                && elapsed >= ExpeditionTiming.ObjectiveOffsetTicks(expedition))
            {
                expedition.ReachObjectiveAndBeginReturn(_tick);
            }
        }
    }

    private bool UsesObservableCombat(Expedition expedition) =>
        expedition.ResourceOpportunityKind == ResourceOpportunityKind.SpiritTrailSearch
        && expedition.MemberIds.Count == 1
        && Hero is Citizen founder
        && expedition.MemberIds[0] == founder.Id;

    private void CompleteExpeditionEncounter(
        Expedition expedition,
        ExpeditionEncounterOutcome outcome)
    {
        if (!expedition.CompleteEncounter(outcome)) return;
        _log.Record(
            _tick,
            WorldEventKind.ExpeditionEncounterResolved,
            WorldEventSubject.Expedition(expedition.Id.Value, expedition.DisplayName),
            (int)outcome,
            expedition.DispatchEventId);
        ExpeditionChanged?.Invoke(
            this,
            new ExpeditionChangedEventArgs(expedition.Id, expedition.Status));
    }

    internal static ExpeditionEncounterOutcome ToExpeditionOutcome(CombatOutcome outcome) =>
        outcome switch
        {
            CombatOutcome.PartyVictory => ExpeditionEncounterOutcome.FullSuccess,
            CombatOutcome.Exhausted => ExpeditionEncounterOutcome.PartialSuccess,
            CombatOutcome.PartyDefeated or CombatOutcome.PartyRetreated =>
                ExpeditionEncounterOutcome.Setback,
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null),
        };

    /// <summary>
    /// The expedition's single deterministic encounter. Deterministic from
    /// purely persisted inputs — team condition (average stamina fraction),
    /// each member's single best competency (whichever one they have actually
    /// practiced, "already earned in Farm/Quarry" per §11.2), supplies
    /// committed, and a seed derived from the expedition's own persisted id
    /// and start tick — so re-evaluating it after a save/load reload always
    /// reaches the same result (it never actually re-evaluates: see
    /// <see cref="Expedition.CompleteEncounter"/>, which stores it once).
    /// A healthy, fully-rested team can never roll a Setback; a tired team
    /// can. This keeps the very first expedition from being able to punish
    /// a new player outright while still making team condition matter.
    /// </summary>
    private ExpeditionEncounterOutcome ResolveEncounterOutcome(Expedition expedition)
    {
        int teamCompetency = 0;
        int staminaPercentSum = 0;
        int memberCount = 0;
        foreach (CitizenId memberId in expedition.MemberIds)
        {
            if (!_citizens.TryGetValue(memberId, out Citizen? member)) continue;
            memberCount++;
            int bestCompetency = 0;
            foreach (CompetencyEntry entry in member.Competencies.Values)
            {
                if (entry.Experience > bestCompetency) bestCompetency = entry.Experience;
            }
            teamCompetency += bestCompetency;
            staminaPercentSum += member.MaxStamina > 0
                ? member.CurrentStamina * 100 / member.MaxStamina
                : 0;
        }
        int averageStaminaPercent = memberCount > 0 ? staminaPercentSum / memberCount : 0;

        int seed = StableExpeditionSeed(expedition.Id.Value, expedition.StartTick);
        int roll = new Random(seed).Next(0, 30);

        int score = teamCompetency
            + averageStaminaPercent / 4
            + expedition.SupplyAmount * 5
            + roll;

        if (score >= 50) return ExpeditionEncounterOutcome.FullSuccess;
        if (score >= 30) return ExpeditionEncounterOutcome.PartialSuccess;
        return ExpeditionEncounterOutcome.Setback;
    }

    internal static int StableExpeditionSeed(int expeditionId, int startTick)
    {
        unchecked
        {
            uint value = (uint)expeditionId * 0x9E3779B9u;
            value ^= (uint)startTick + 0x85EBCA6Bu + (value << 6) + (value >> 2);
            value ^= value >> 16;
            value *= 0x7FEB352Du;
            value ^= value >> 15;
            return (int)value;
        }
    }

    /// <summary>
    /// Removes Forests only after both their natural reserve and their
    /// gathered stock are empty. The Forest remains as the owning
    /// storage location while gathered wood is waiting to be consumed;
    /// deleting it when only the reserve reaches zero would destroy
    /// player-owned stock. Other building kinds never trigger this path.
    /// </summary>
    private void DemolishDepletedForests()
    {
        List<BuildingId>? depleted = null;
        foreach (var pair in _buildings)
        {
            if (pair.Value.Kind != BuildingKind.Forest) continue;
            if (_naturalResourcePatches.ContainsKey(pair.Key.Value)) continue;
            if (pair.Value.WoodReserve > 0) continue;
            if (pair.Value.Stock > 0) continue;
            depleted ??= new List<BuildingId>();
            depleted.Add(pair.Key);
        }
        if (depleted is null) return;

        foreach (var id in depleted)
        {
            Building building = _buildings[id];
            RemoveBuildingInternal(id);
            _log.Record(_tick, WorldEventKind.ForestDemolished,
                WorldEventSubject.Building(id, building.DisplayName));
        }
        if (depleted.Count > 0)
        {
            RaiseBuildingChanged(depleted[0]);
        }
    }

    /// <summary>
    /// Once-per-day "mouths to feed" pressure. Every resident, working or idle,
    /// costs one Food at dawn — recruiting a citizen is never free
    /// upkeep-wise, even while they are unassigned. A shortfall never blocks
    /// or hurts anyone directly (that consequence stays owned by the existing
    /// stamina/vital
    /// status path in <see cref="ProcessCitizenNeedsAndStandingOrders"/>);
    /// it only records a causal, visible event so the player can see Food
    /// reserves are not keeping up with the population.
    /// </summary>
    private void ApplyResidentFoodRation()
    {
        SampleEarlyGameDawn();
        int ration = Upkeep.FoodPerResidentPerDay(_citizens.Count);
        if (ration <= 0) return;
        if (TryConsumeFood(ration)) return;
        _log.Record(
            _tick,
            WorldEventKind.FoodRationShortfall,
            WorldEventSubject.World("City"),
            amount: _citizens.Count);
    }

    /// <summary>
    /// EG-0 dawn sample. Taken immediately before the ration is charged, so
    /// the Food horizon reports what the player woke up owning rather than
    /// what was left after breakfast — the former is the number the player
    /// actually plans against.
    ///
    /// <para>Dawn is the only honest sampling point for a per-day quantity:
    /// <see cref="WorldTimeAdvance"/> batches quiescent stretches, so anything
    /// counted per tick would under-report precisely the idle periods this
    /// measurement exists to expose.</para>
    /// </summary>
    private void SampleEarlyGameDawn()
    {
        int idle = 0;
        foreach (Citizen citizen in _citizens.Values)
        {
            if (citizen.Commitment.Kind == CitizenCommitmentKind.None) idle++;
        }
        _metrics.SampleDawn(
            foodStock: _resources.Available(ResourceType.Food),
            residentCount: _citizens.Count,
            idleCitizenCount: idle);
    }

    private void CompleteFinishedProjects()
    {
        if (_projects.Count == 0) return;
        List<BuildingId>? completed = null;
        foreach (var pair in _projects)
        {
            if (pair.Value.Progress >= pair.Value.RequiredWork)
            {
                completed ??= new List<BuildingId>();
                completed.Add(pair.Key);
            }
        }
        if (completed is null) return;
        for (int i = 0; i < completed.Count; i++)
        {
            BuildingId projectId = completed[i];
            if (_projects.TryGetValue(projectId, out ConstructionProject? project)
                && project.Kind == ConstructionKind.FoundingSite)
            {
                FoundingSiteModule? completedModule = project.CompleteActiveFoundingModule();
                if (completedModule == FoundingSiteModule.Canopy)
                {
                    CompleteProject(projectId);
                }
                else if (completedModule.HasValue)
                {
                    ReleaseFoundingModuleContributors(projectId, project);
                    NotifyFirstNightModuleCompleted(completedModule.Value);
                    RaiseProjectChanged(projectId);
                }
                continue;
            }
            if (_projects.TryGetValue(projectId, out project)
                && project.Kind == ConstructionKind.CultivationSite)
            {
                CompleteCultivationSiteProject(projectId);
                continue;
            }
            CompleteProject(projectId);
        }
    }

    /// <summary>
    /// Opens the main-dialogue node the spirit is currently speaking. The id is
    /// persisted, so a save taken mid-conversation resumes on the same line.
    /// </summary>
    public bool TryOpenFirstNightDialogue(string nodeId)
    {
        if (_firstNight is not { IsActive: true } night) return false;
        night.OpenDialogueNode(nodeId);
        return true;
    }

    /// <summary>
    /// Closes the current main-dialogue node and moves the night on. Only the
    /// stages the spirit drives advance this way; the build stages wait on a
    /// finished module instead, which is why this refuses them.
    /// </summary>
    public bool TryCloseFirstNightDialogue()
    {
        if (_firstNight is not { IsActive: true } night) return false;
        if (!FirstNightRules.WaitsForDialogue(night.Stage)
            && night.Stage is not (FirstNightStage.Manifested
                or FirstNightStage.OtherLightTold
                or FirstNightStage.Sleeping))
        {
            return false;
        }
        // The founder cannot fall asleep without somewhere to sleep. This is the
        // Bedroll's first mechanical meaning: until now it was only a cost, a
        // work total and a Canopy prerequisite, and CitizenLocation.AtHome is a
        // citizen's default value rather than proof that a shelter exists.
        if (night.Stage == FirstNightStage.OtherLightTold && !HasRestingPlace())
        {
            return false;
        }
        return AdvanceFirstNight();
    }

    /// <summary>
    /// Ends the authored first night without walking its stages.
    ///
    /// <b>Fixture seam.</b> Production reaches <see cref="FirstNightStage.Concluded"/>
    /// only through the sequence itself, or by restoring a pre-v31 save that the
    /// migration already marked concluded. Tests, however, mostly describe a
    /// city past its opening — rations at dawn, production cycles, expeditions —
    /// and those rules are not about the first night. Without this they would all
    /// run with the calendar held and quietly assert the wrong thing.
    /// No scene or panel may skip the sequence in real play. That used to be
    /// expressed as <c>internal</c>, which stopped meaning anything the moment
    /// the domain became its own assembly and the fixture builders — which
    /// legitimately need it — landed on the other side of the boundary. The
    /// rule is now enforced where it can actually be checked:
    /// <c>ArchitectureBoundaryTests.Presentation_ConcludesFirstNightOnlyInFixtures</c>
    /// pins the call sites, and the <c>ForFixtures</c> suffix keeps the intent
    /// unmissable at every one of them.
    /// </summary>
    public void ConcludeFirstNightForFixtures()
    {
        if (_firstNight is not { IsActive: true } night) return;
        while (night.IsActive) night.TryAdvance(_tick);
    }

    /// <summary>
    /// Whether the city offers anywhere to sleep. A finished Home does, and so
    /// does a Bedroll inside an unfinished Founding Site — the deliberately
    /// rudimentary shelter of the first night, which exists long before the
    /// Canopy consolidates the site into a building.
    /// </summary>
    public bool HasRestingPlace() =>
        HasCompletedFirstShelter() || HasFoundingSiteModule(FoundingSiteModule.Bedroll);

    /// <summary>
    /// Advances the night one stage and records the fact. Kept private so the
    /// only ways forward are a closed dialogue node or a completed module —
    /// never a timer, which is what invariant 8 of the design doc protects.
    /// </summary>
    private bool AdvanceFirstNight()
    {
        if (_firstNight is not { } night) return false;
        FirstNightStage previous = night.Stage;
        if (!night.TryAdvance(_tick)) return false;
        if (night.Stage == FirstNightStage.Concluded)
        {
            // The spirit departs once per night, at the Sleeping → Concluded
            // boundary. Persisted as a significant event so the expedition
            // panel can gate the SpiritTrailSearch opportunity on its
            // presence and the chronicle can show the moment.
            _log.Record(
                _tick,
                WorldEventKind.SpiritDeparted,
                WorldEventSubject.World("FireSpirit"));
            _log.Record(_tick, WorldEventKind.DayBegan, WorldEventSubject.World("Sun"));
            // Surface the spirit-trail opportunity on the same tick so the
            // player can dispatch it immediately after the dawn — without
            // waiting for a subsequent Ensure call.
            EnsureStartingResourceExpeditionOpportunities();
        }
        return night.Stage != previous;
    }

    /// <summary>
    /// Lets a finished Founding Site module carry the night forward. Called
    /// after module completion so the sequence tracks the real worksite rather
    /// than a parallel counter that could disagree with it.
    /// </summary>
    private void NotifyFirstNightModuleCompleted(FoundingSiteModule module)
    {
        if (_firstNight is not { IsActive: true } night) return;
        if (!FirstNightRules.WaitsForModule(night.Stage)) return;
        if (FirstNightRules.ModuleFor(night.Stage) != module) return;
        AdvanceFirstNight();
    }

    /// <summary>
    /// Stop cause for a project the batched quiescent path skipped over. It
    /// keeps <see cref="ConstructionSimulation.SimulateTick"/>'s precedence so
    /// the batch cannot report a cause the per-tick path would never produce:
    /// a Founding Site waiting between modules stays
    /// <see cref="ConstructionStopCause.AwaitingModule"/> instead of being
    /// relabelled <see cref="ConstructionStopCause.NoWorkers"/> just because no
    /// one is assigned while it waits.
    /// </summary>
    private static ConstructionStopCause ResolveQuiescentProjectStopCause(
        ConstructionProject project,
        bool isLaborTime)
    {
        if (!project.Enabled) return ConstructionStopCause.Paused;
        if (!project.HasActiveWork) return ConstructionStopCause.AwaitingModule;
        if (project.Progress >= project.RequiredWork) return ConstructionStopCause.Completed;
        return isLaborTime
            ? ConstructionStopCause.NoWorkers
            : ConstructionStopCause.Night;
    }

    /// <summary>
    /// Frees the contributors of a Founding Site module that just finished
    /// while the site itself is still under way.
    ///
    /// Only the Canopy turns the site into a building, so only the Canopy used
    /// to run <see cref="CompleteProject"/>'s release loop. Campfire, Bedroll
    /// and Cache left every contributor committed to a worksite that had no
    /// active work, which made <see cref="Citizen.IsAvailable"/> false and
    /// therefore made <see cref="NaturalResourceGatherAvailability"/> answer
    /// <see cref="NaturalResourceGatherOutcome.HeroUnavailable"/>. A lone
    /// founder could finish the Campfire and then find the gather action
    /// disabled with no way to reach the next module's materials short of
    /// unassigning themselves through the construction panel.
    ///
    /// The founder is placed at home rather than sent travelling: until the
    /// Canopy there is no Home building to walk to, and
    /// <see cref="CitizenLocation.AtHome"/> is exactly the location gathering
    /// already runs from.
    /// </summary>
    private void ReleaseFoundingModuleContributors(
        BuildingId projectId,
        ConstructionProject project)
    {
        var contributorIds = new List<CitizenId>(project.AssignedCitizenIds);
        foreach (CitizenId citizenId in contributorIds)
        {
            project.TryUnassign(citizenId);
            if (!_citizens.TryGetValue(citizenId, out Citizen? citizen)) continue;
            bool released = citizen.ReleaseCommitment(
                CitizenCommitmentKind.Construction,
                projectId.Value);
            if (!released || citizen.CurrentLocation == CitizenLocation.AtHome) continue;
            if (HasCompletedFirstShelter())
            {
                citizen.BeginTravelHome(_tick);
            }
            else
            {
                citizen.SetLocation(CitizenLocation.AtHome);
            }
        }
    }

    private void CompleteCultivationSiteProject(BuildingId projectId)
    {
        if (!_projects.TryGetValue(projectId, out ConstructionProject? project)) return;
        var contributorIds = new List<CitizenId>(project.AssignedCitizenIds);
        _projects.Remove(projectId);
        RegisterCultivationSite(new CultivationSite(projectId));
        _log.Record(
            _tick,
            WorldEventKind.ProjectCompleted,
            WorldEventSubject.ConstructionProject(projectId, project.DisplayName));
        foreach (CitizenId citizenId in contributorIds)
        {
            project.TryUnassign(citizenId);
            if (_citizens.TryGetValue(citizenId, out Citizen? citizen))
            {
                bool released = citizen.ReleaseCommitment(
                    CitizenCommitmentKind.Construction,
                    projectId.Value);
                if (released && citizen.CurrentLocation != CitizenLocation.AtHome)
                {
                    citizen.BeginTravelHome(_tick);
                }
            }
        }
        RaiseProjectChanged(projectId);
        RaiseCultivationSiteChanged(projectId);
    }

    public CultivationActionResult TrySowCultivationSite(BuildingId siteId)
    {
        if (!_cultivationSites.TryGetValue(siteId, out CultivationSite? site))
        {
            return CultivationActionResult.Fail(CultivationActionOutcome.SiteNotFound);
        }
        if (Hero is not Citizen hero || !hero.IsAvailable)
        {
            return CultivationActionResult.Fail(CultivationActionOutcome.FounderUnavailable);
        }
        if (site.State != CultivationPlotState.Prepared)
        {
            return CultivationActionResult.Fail(CultivationActionOutcome.WrongState);
        }
        if (!TryConsumeFood(CultivationRules.SeedFoodCost))
        {
            return CultivationActionResult.Fail(CultivationActionOutcome.MissingFood);
        }
        if (!site.TrySow(_tick))
        {
            return CultivationActionResult.Fail(CultivationActionOutcome.WrongState);
        }
        RaiseCultivationSiteChanged(siteId);
        return CultivationActionResult.Success(-CultivationRules.SeedFoodCost);
    }

    public CultivationActionResult TryHarvestCultivationSite(BuildingId siteId)
    {
        if (!_cultivationSites.TryGetValue(siteId, out CultivationSite? site))
        {
            return CultivationActionResult.Fail(CultivationActionOutcome.SiteNotFound);
        }
        if (Hero is not Citizen hero || !hero.IsAvailable)
        {
            return CultivationActionResult.Fail(CultivationActionOutcome.FounderUnavailable);
        }
        if (!site.TryHarvest())
        {
            return CultivationActionResult.Fail(CultivationActionOutcome.WrongState);
        }
        int harvested = _resources.DepositToCityInventory(
            ResourceType.Food,
            CultivationRules.HarvestFoodYield);
        _log.Record(
            _tick,
            WorldEventKind.CropHarvested,
            WorldEventSubject.CultivationSite(siteId, "Cultivation Site"),
            harvested);
        RaiseCultivationSiteChanged(siteId);
        return CultivationActionResult.Success(harvested);
    }

    private void AdvanceCultivationSites()
    {
        foreach (CultivationSite site in _cultivationSites.Values)
        {
            if (!site.AdvanceTo(_tick)) continue;
            _log.Record(
                _tick,
                WorldEventKind.CropReady,
                WorldEventSubject.CultivationSite(site.Id, "Cultivation Site"));
            RaiseCultivationSiteChanged(site.Id);
        }
    }

    private void RaiseCultivationSiteChanged(BuildingId siteId) =>
        CultivationSiteChanged?.Invoke(this, new CityWorldChangedEventArgs(siteId));

    /// <summary>
    /// Compares day/night state before and after the tick and
    /// moves citizens to the right place when the boundary
    /// crosses. Called once per world tick from
    /// <see cref="AdvanceWorldTick"/>. Returns <c>true</c> when the
    /// day/night state actually changed so the caller can emit the
    /// corresponding log event without re-deriving the comparison.
    /// </summary>
    private bool DetectAndApplyMobilisation(int previousTick, int currentTick)
    {
        bool wasDay = GameClock.IsDaytime(previousTick);
        bool isDay = GameClock.IsDaytime(currentTick);
        if (wasDay == isDay) return false;
        // Founding camp: there is no Home to return to and IsLaborTime() holds
        // at every hour, so neither boundary may pull the founder off the
        // worksite. The crossing is still reported so the sunrise/sunset event
        // fires and the clock reads correctly.
        if (!HasCompletedFirstShelter()) return true;
        if (wasDay) MobiliseForNight();
        else MobiliseForDay();
        return true;
    }

    /// <summary>
    /// All citizens go to the Home at night — assigned workers
    /// leave their production building to rest; idle citizens stay
    /// at home (they never left). Called on the day→night boundary.
    /// </summary>
    internal void MobiliseForNight()
    {
        foreach (var citizen in _citizens.Values)
        {
            if (citizen.Commitment.Kind != CitizenCommitmentKind.Expedition
                && citizen.CurrentLocation != CitizenLocation.AtHome)
            {
                citizen.BeginTravelHome(_tick);
            }
        }
        // The Home building's slot rendering reads CitizenLocation
        // directly; nothing else needs to fire here. UI listeners
        // re-render via the regular BuildingChanged signals that
        // follow in this tick.
    }

    /// <summary>
    /// Assigned citizens return to their production building;
    /// unassigned citizens stay at home. Called on the night→day
    /// boundary.
    /// </summary>
    internal void MobiliseForDay()
    {
        foreach (var citizen in _citizens.Values)
        {
            citizen.SetLocation(CitizenLocation.AtHome);
        }
    }

    private void ProcessCitizenNeedsAndStandingOrders()
    {
        bool isDaytime = GameClock.IsDaytime(_tick);
        foreach (Citizen citizen in _citizens.Values)
        {
            CompleteDueTravel(citizen);
            if (citizen.CurrentLocation == CitizenLocation.AtHome)
            {
                if (citizen.VitalStatus != CitizenVitalStatus.Stable)
                {
                    if (citizen.WellFedRemainingTicks <= 0)
                    {
                        if (TryConsumeFood(StaminaRules.FoodConsumedPerRegen))
                        {
                            citizen.RestoreStamina(StaminaRules.RegenFromFood(
                                StaminaRules.FoodConsumedPerRegen,
                                citizen));
                            citizen.RefreshWellFedBuff();
                            citizen.MarkFoodReceived();
                        }
                        else
                        {
                            citizen.MarkFoodBlocked();
                        }
                    }
                    citizen.RestoreStamina(citizen.RegenPerTick());
                    citizen.CompleteVitalRecovery();
                }
                else if (!isDaytime)
                {
                    if (CityEconomyRules.IsMealTick(_tick)
                        && citizen.CurrentStamina < citizen.MaxStamina
                        && TryConsumeFood(StaminaRules.FoodConsumedPerRegen))
                    {
                        citizen.RestoreStamina(StaminaRules.RegenFromFood(
                            StaminaRules.FoodConsumedPerRegen,
                            citizen));
                        citizen.RefreshWellFedBuff();
                    }
                    citizen.RestoreStamina(citizen.RegenPerTick());
                }
            }

            // IsLaborTime, not isDaytime: the meal and rest rules above stay on
            // the clock, but re-dispatching a standing order must honour the
            // founding-camp bypass. Otherwise a founder who ends up at home
            // after 16:00 is never sent back to the worksite.
            if (!IsLaborTime()
                || citizen.VitalStatus != CitizenVitalStatus.Stable
                || citizen.IsWounded
                || citizen.Commitment.Kind is CitizenCommitmentKind.Expedition
                    or CitizenCommitmentKind.Recovery
                || citizen.CurrentLocation != CitizenLocation.AtHome
                || _tick < citizen.ResumeWorkNotBeforeTick
                || citizen.WorkOrder is not { } order
                || !IsStandingOrderEligible(order))
            {
                continue;
            }
            citizen.BeginTravelToAssignment(_tick);
        }
    }

    private void AdvanceWoundRecoveries(int tickCount = 1)
    {
        foreach (Citizen citizen in _citizens.Values)
        {
            if (citizen.Wound is not { } wound
                || citizen.Commitment.Kind != CitizenCommitmentKind.Recovery)
            {
                continue;
            }
            WorldEventId originatingEventId = wound.OriginatingEventId;
            if (!citizen.AdvanceWoundRecoveryTicks(tickCount)) continue;
            _log.Record(
                _tick,
                WorldEventKind.WoundRecoveryCompleted,
                WorldEventSubject.Citizen(citizen.Id, citizen.Name),
                causeEventId: originatingEventId);
        }
    }

    private bool IsStandingOrderEligible(CitizenWorkOrder order)
    {
        if (order.Kind == CitizenCommitmentKind.BuildingWork
            && _buildings.TryGetValue(order.TargetId, out Building? building))
        {
            return building.ProductionEnabled && building.Stock < building.MaxStock;
        }
        if (order.Kind == CitizenCommitmentKind.Construction
            && _projects.TryGetValue(order.TargetId, out ConstructionProject? project))
        {
            return project.Enabled && project.Progress < project.RequiredWork;
        }
        return false;
    }

    private void InterruptCitizensRequiringRecovery()
    {
        foreach (Citizen citizen in _citizens.Values)
        {
            if (citizen.CurrentLocation == CitizenLocation.AtWork
                && CitizenNeedsRules.RequiresRecovery(citizen))
            {
                citizen.BeginVitalRecovery(_tick);
            }
        }
    }

    /// <summary>
    /// Citizens physically visible at this building right now.
    /// For production buildings: assigned citizens whose
    /// <see cref="Citizen.CurrentLocation"/> is
    /// <see cref="CitizenLocation.AtWork"/>. For Home: every
    /// citizen whose location is
    /// <see cref="CitizenLocation.AtHome"/>.
    /// </summary>
    public IReadOnlyList<CitizenId> GetCurrentlyVisibleOccupants(Building building)
    {
        var ids = new List<CitizenId>();
        if (building.Kind == BuildingKind.Home)
        {
            foreach (var citizen in _citizens.Values)
            {
                if (citizen.CurrentLocation == CitizenLocation.AtHome)
                {
                    ids.Add(citizen.Id);
                }
            }
        }
        else
        {
            foreach (var citizenId in building.AssignedCitizenIds)
            {
                if (_citizens.TryGetValue(citizenId, out var citizen)
                    && citizen.CurrentLocation == CitizenLocation.AtWork)
                {
                    ids.Add(citizen.Id);
                }
            }
        }
        return ids;
    }

    /// <summary>
    /// Ends a journey whose arrival tick has passed. This is the only place a
    /// citizen stops being <see cref="CitizenLocation.InTransit"/> by travelling,
    /// and it runs inside the one canonical tick — so live play and offline
    /// catch-up reach the same state from the same world time. Presentation
    /// draws the journey and has no vote (<c>DEC-0023</c>).
    ///
    /// <para>
    /// Expedition travel is excluded for free: <see cref="Citizen.DispatchOnExpedition"/>
    /// enters <c>InTransit</c> without a start tick, so it has no arrival tick to
    /// be due. Every other in-transit citizen qualifies, including one with no
    /// commitment left — the previous commitment whitelist stranded exactly those
    /// citizens forever once <see cref="MobiliseForNight"/> had sent them home.
    /// </para>
    /// </summary>
    private void CompleteDueTravel(Citizen citizen)
    {
        if (!citizen.AbstractTravelHasCompleted(_tick)) return;

        if (citizen.IsReturningHome)
        {
            citizen.SetLocation(CitizenLocation.AtHome);
            return;
        }

        BuildingId? assignmentId = citizen.CurrentAssignment;
        if (assignmentId is null)
        {
            // Travelling to work with no standing order left to arrive at. Settle
            // at home rather than leaving an unreachable destination in transit.
            citizen.SetLocation(CitizenLocation.AtHome);
            return;
        }

        if (!IsLaborTime())
        {
            // A journey can come due just after the workday boundary. Do not
            // leave the citizen idle at the threshold: preserve the standing
            // order and reverse the physical journey.
            citizen.BeginTravelHome(_tick);
            RaiseAssignmentChanged(assignmentId.Value);
            return;
        }

        citizen.SetLocation(CitizenLocation.AtWork);
        RaiseAssignmentChanged(assignmentId.Value);
    }

    /// <summary>
    /// An assignment target is either a building or a construction project;
    /// arrival changes what the corresponding panel shows either way.
    /// </summary>
    private void RaiseAssignmentChanged(BuildingId assignmentId)
    {
        if (_buildings.ContainsKey(assignmentId)) RaiseBuildingChanged(assignmentId);
        else if (_projects.ContainsKey(assignmentId)) RaiseProjectChanged(assignmentId);
    }

    private bool HasWorkerAtBuilding(Building building)
    {
        foreach (CitizenId citizenId in building.AssignedCitizenIds)
        {
            if (_citizens.TryGetValue(citizenId, out Citizen? citizen)
                && citizen.CurrentLocation == CitizenLocation.AtWork)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// First Home building in the world, or null if the city has
    /// none. Citizens are mobilised here at night; the UI uses it
    /// as the resting location. Seam: future slices with multiple
    /// homes may return the closest or the one with capacity.
    /// </summary>
    public Building? PrimaryHome
    {
        get
        {
            foreach (var building in _buildings.Values)
            {
                if (building.Kind == BuildingKind.Home) return building;
            }
            return null;
        }
    }

    /// <summary>
    /// Projects the citizen's durable context into one explainable current
    /// routine. This is safe to query with no Godot scene instantiated and is
    /// therefore shared by live UI, diagnostics and post-offline reconstruction.
    /// </summary>
    public CitizenRoutineSnapshot? GetCitizenRoutine(CitizenId citizenId)
    {
        if (!_citizens.TryGetValue(citizenId, out Citizen? citizen)) return null;

        BuildingId? shelterId = PrimaryHome?.Id;
        CitizenWorkOrder? order = citizen.WorkOrder;
        BuildingId? workplaceId = order?.TargetId;
        CitizenRoutineActivity activity;
        CitizenRoutineBlockReason blockReason = CitizenRoutineBlockReason.None;
        int? startedAt = null;
        int? expectedAt = null;
        int? nextTransition = null;
        BuildingId? originId = null;
        BuildingId? destinationId = null;
        CitizenContextLocation contextLocation;
        BuildingId? contextBuildingId;

        if (IsCitizenOnActiveExpedition(citizenId))
        {
            activity = CitizenRoutineActivity.OnExpedition;
            contextLocation = CitizenContextLocation.Unavailable;
            contextBuildingId = null;
        }
        else if (citizen.CurrentLocation == CitizenLocation.InTransit)
        {
            activity = citizen.IsReturningHome
                ? CitizenRoutineActivity.TravellingHome
                : CitizenRoutineActivity.TravellingToWork;
            contextLocation = CitizenContextLocation.InTransit;
            contextBuildingId = null;
            startedAt = citizen.TransitStartedAtTick;
            expectedAt = startedAt + CityEconomyRules.AbstractTravelTicks;
            nextTransition = expectedAt;
            originId = citizen.IsReturningHome ? workplaceId : shelterId;
            destinationId = citizen.IsReturningHome ? shelterId : workplaceId;
        }
        else if (citizen.Commitment.Kind == CitizenCommitmentKind.Recovery
            || citizen.VitalStatus != CitizenVitalStatus.Stable
            || citizen.IsWounded)
        {
            activity = CitizenRoutineActivity.Recovering;
            contextLocation = CitizenContextLocation.AtShelter;
            contextBuildingId = shelterId;
            blockReason = citizen.VitalStatus == CitizenVitalStatus.BlockedNoFood
                ? CitizenRoutineBlockReason.NoFood
                : citizen.IsWounded
                    ? CitizenRoutineBlockReason.Wounded
                    : CitizenRoutineBlockReason.Recovering;
            nextTransition = citizen.Wound is { } wound
                ? _tick + wound.RecoveryTicksRemaining
                : null;
        }
        else if (citizen.CurrentLocation == CitizenLocation.AtWork)
        {
            contextLocation = CitizenContextLocation.AtWorkplace;
            contextBuildingId = workplaceId;
            nextTransition = GameClock.NextWorkdayEnd(_tick);
            (activity, blockReason) = ResolveWorkplaceActivity(order);
        }
        else
        {
            contextLocation = CitizenContextLocation.AtShelter;
            contextBuildingId = shelterId;
            (activity, blockReason) = ResolveAtHomeActivity(order);
            if (!GameClock.IsWorkday(_tick)) nextTransition = GameClock.NextWorkdayStart(_tick);
        }

        return new CitizenRoutineSnapshot(
            citizen.Id,
            activity,
            contextLocation,
            contextBuildingId,
            shelterId,
            originId,
            destinationId,
            startedAt,
            expectedAt,
            nextTransition,
            blockReason,
            citizen.Behavior,
            citizen.CurrentLocation,
            order);
    }

    private (CitizenRoutineActivity Activity, CitizenRoutineBlockReason BlockReason)
        ResolveAtHomeActivity(CitizenWorkOrder? order)
    {
        if (order is null)
        {
            return GameClock.IsWorkday(_tick)
                ? (CitizenRoutineActivity.Leisure, CitizenRoutineBlockReason.NoAssignment)
                : (CitizenRoutineActivity.Resting, CitizenRoutineBlockReason.None);
        }
        if (!GameClock.IsWorkday(_tick))
        {
            return (CitizenRoutineActivity.OffDuty, CitizenRoutineBlockReason.OutsideWorkHours);
        }
        return ResolveWorkplaceActivity(order);
    }

    private (CitizenRoutineActivity Activity, CitizenRoutineBlockReason BlockReason)
        ResolveWorkplaceActivity(CitizenWorkOrder? order)
    {
        if (order is null)
        {
            return (CitizenRoutineActivity.Leisure, CitizenRoutineBlockReason.NoAssignment);
        }
        if (!GameClock.IsWorkday(_tick))
        {
            return (CitizenRoutineActivity.OffDuty, CitizenRoutineBlockReason.OutsideWorkHours);
        }
        if (order.Value.Kind == CitizenCommitmentKind.BuildingWork
            && _buildings.TryGetValue(order.Value.TargetId, out Building? building))
        {
            if (!building.ProductionEnabled)
            {
                return (CitizenRoutineActivity.WorkplaceIdle, CitizenRoutineBlockReason.WorkplacePaused);
            }
            if (building.Stock >= building.MaxStock)
            {
                return (CitizenRoutineActivity.WaitingForStorage, CitizenRoutineBlockReason.StorageFull);
            }
            if (building.StopCause == ProductionStopCause.MissingInputs)
            {
                return (CitizenRoutineActivity.WaitingForResources, CitizenRoutineBlockReason.MissingInputs);
            }
            return (CitizenRoutineActivity.Working, CitizenRoutineBlockReason.None);
        }
        if (order.Value.Kind == CitizenCommitmentKind.Construction
            && _projects.TryGetValue(order.Value.TargetId, out ConstructionProject? project))
        {
            if (!project.Enabled)
            {
                return (CitizenRoutineActivity.WorkplaceIdle, CitizenRoutineBlockReason.WorkplacePaused);
            }
            if (project.StopCause == ConstructionStopCause.MissingMaterials)
            {
                return (CitizenRoutineActivity.WaitingForResources, CitizenRoutineBlockReason.MissingInputs);
            }
            return (CitizenRoutineActivity.Working, CitizenRoutineBlockReason.None);
        }
        return (CitizenRoutineActivity.Unavailable, CitizenRoutineBlockReason.NoAssignment);
    }

    private void ApplyUpkeep()
    {
        // Upkeep is dormant. The seam remains so a future slice can
        // reactivate building-driven demand (e.g. Smithy tool wear,
        // depot maintenance) without re-introducing the placeholder
        // "abstract city upkeep" that previously drained Quarry stone
        // for no playable reason. Re-enable here AND in
        // TryAdvanceQuiescentTicks when real demand exists.
    }

    private void ApplyNightRest(Building building)
    {
        building.StopCause = ProductionStopCause.Night;
        RaiseBuildingChanged(building.Id);
    }

    private void DecrementAllWellFed()
    {
        foreach (var citizen in _citizens.Values)
        {
            citizen.AdvanceWellFedTick();
        }
    }

    /// <summary>
    /// One building tick in isolation. Performs eat / passive
    /// regen (buff-aware) / cost / contributing / produce /
    /// experience, sets the building's
    /// <see cref="Building.StopCause"/>, and returns the stock
    /// added. Does not raise <see cref="BuildingChanged"/> —
    /// callers decide whether to notify the UI.
    /// </summary>
    internal int SimulateBuildingTick(Building building)
        => _production.SimulateTick(building);

    private void CompleteProject(BuildingId projectId)
    {
        if (!_projects.TryGetValue(projectId, out var project)) return;
        var contributorIds = new List<CitizenId>(project.AssignedCitizenIds);

        var building = CreateCompletedBuilding(project);

        RegisterBuilding(building);
        RaiseBuildingChanged(building.Id);
        // EG-0: "time to first shelter" is the headline opening measurement.
        // Recorded here rather than from the event log because retention is
        // bounded to 128 events and a long run would drop the very event the
        // measurement depends on.
        if (building.Kind == BuildingKind.Home)
        {
            _metrics.RecordFirstShelterCompleted(_tick);
        }
        _log.Record(_tick, WorldEventKind.ProjectCompleted,
            WorldEventSubject.ConstructionProject(project.Id, project.DisplayName));
        _log.Record(_tick, WorldEventKind.BuildingCreated,
            WorldEventSubject.Building(building.Id, building.DisplayName));

        foreach (var cid in contributorIds)
        {
            project.TryUnassign(cid);
            if (_citizens.TryGetValue(cid, out var c))
            {
                bool released = c.ReleaseCommitment(
                    CitizenCommitmentKind.Construction,
                    projectId.Value);
                if (released && c.CurrentLocation != CitizenLocation.AtHome)
                {
                    c.BeginTravelHome(_tick);
                }
            }
        }

        _projects.Remove(projectId);
        RaiseProjectChanged(projectId);
    }

    private static Building CreateCompletedBuilding(ConstructionProject project)
    {
        Building building = project.Kind switch
        {
        ConstructionKind.Farm => new Building(
            id: project.Id,
            displayName: project.DisplayName,
            kind: BuildingKind.Farm,
            producedResourceType: ResourceType.Food,
            producedCompetencyId: CompetencyId.Farming,
            workerCapacity: 5,
            visualCapacity: 5,
            baseProductionPerWorker: 1,
            storageCapacity: CityEconomyRules.FarmStorageCapacity,
            resourceLabel: "Food",
            resourceUnit: "food"),
        ConstructionKind.Quarry => new Building(
            id: project.Id,
            displayName: project.DisplayName,
            kind: BuildingKind.Quarry,
            producedResourceType: ResourceType.Stone,
            producedCompetencyId: CompetencyId.Mining,
            workerCapacity: 6,
            visualCapacity: 3,
            baseProductionPerWorker: 1,
            storageCapacity: CityEconomyRules.QuarryStorageCapacity,
            resourceLabel: "Stone",
            resourceUnit: "stone"),
        ConstructionKind.TownHall => new Building(
            id: project.Id,
            displayName: project.DisplayName,
            kind: BuildingKind.TownHall,
            producedResourceType: ResourceType.Stone,
            producedCompetencyId: CompetencyId.Construction,
            workerCapacity: 0,
            visualCapacity: 0,
            baseProductionPerWorker: 0,
            storageCapacity: 0,
            resourceLabel: "Prospect",
            resourceUnit: "prospect",
            productionEnabled: false),
        _ => new Building(
            id: project.Id,
            displayName: project.DisplayName,
            kind: BuildingKind.Home,
            producedResourceType: ResourceType.Stone,
            producedCompetencyId: CompetencyId.Mining,
            workerCapacity: 5,
            visualCapacity: 5,
            baseProductionPerWorker: 0,
            storageCapacity: 0,
            resourceLabel: "Rest",
            resourceUnit: "rest",
            productionEnabled: false),
        };
        if (project.Kind == ConstructionKind.FoundingSite)
        {
            building.RestoreFoundingSiteOriginModules(project.CompletedFoundingModules);
        }
        return building;
    }

    public void ConfigureProductionPolicy(BuildingId buildingId, bool enabled, int minStock, int maxStock, int priority)
    {
        if (!_buildings.TryGetValue(buildingId, out var building))
        {
            return;
        }

        building.ConfigureProductionPolicy(enabled, minStock, maxStock, priority);
        RaiseBuildingChanged(buildingId);
    }

    /// <summary>
    /// Flips a building's <see cref="Building.ProductionEnabled"/>
    /// flag without touching its reactive <c>MinStock</c>/<c>MaxStock</c>/
    /// <c>Priority</c> triplet. The presentation layer uses this when the
    /// player toggles the simple on/off button. Future slices that
    /// expose the triplet as a UI again will revert to
    /// <see cref="ConfigureProductionPolicy"/>.
    /// </summary>
    public void SetProductionEnabled(BuildingId buildingId, bool enabled)
    {
        if (!_buildings.TryGetValue(buildingId, out var building))
        {
            return;
        }

        building.ConfigureProductionPolicy(
            enabled,
            building.MinStock,
            building.MaxStock,
            building.Priority);
        RaiseBuildingChanged(buildingId);
    }

    internal void AdvanceWorldClock(int tickCount)
    {
        if (tickCount > 0) _tick += tickCount;
    }

    /// <summary>
    /// Fast-forwards a world that has no buildings and no
    /// construction projects. Otherwise the caller must step the
    /// world tick by tick so the worksite can advance.
    /// </summary>
    internal void AdvanceIdleTicks(int tickCount)
    {
        if (tickCount <= 0) return;
        if (_buildings.Count != 0
            || _projects.Count != 0
            || _cultivationSites.Count != 0
            || _expeditions.Values.Any(expedition =>
                expedition.Status == ExpeditionStatus.Active))
        {
            throw new InvalidOperationException(
                "Idle fast-forward requires a world with no buildings, projects, or active expeditions.");
        }

        _tick += tickCount;
        foreach (var citizen in _citizens.Values)
        {
            citizen.AdvanceWellFedTicks(tickCount);
        }
        // Same founding-camp exemption as the stepped boundary above.
        if (HasCompletedFirstShelter())
        {
            if (GameClock.IsDaytime(_tick)) MobiliseForDay();
            else MobiliseForNight();
        }
    }

    /// <summary>
    /// Advances a same-phase range for a city that has structures but no work
    /// assignments. Returns the number of ticks consumed, or zero when the
    /// canonical per-tick path is required. Day/night boundaries are excluded
    /// so mobilisation and its causal event remain canonical stepped ticks.
    /// </summary>
    internal int TryAdvanceQuiescentTicks(int maxTickCount)
    {
        if (maxTickCount <= 0
            || HasAnyWorkAssignment()
            || _combatSessions.Values.Any(session => session.IsActive)
            || _citizens.Values.Any(citizen =>
                citizen.Commitment.Kind == CitizenCommitmentKind.Recovery
                && citizen.CurrentLocation != CitizenLocation.AtHome))
        {
            return 0;
        }

        foreach (var building in _buildings.Values)
        {
            if (building.Kind == BuildingKind.Forest
                && building.WoodReserve <= 0
                && building.Stock <= 0)
            {
                return 0;
            }
        }
        foreach (var project in _projects.Values)
        {
            if (project.AssignedCount > 0 || project.Progress >= project.RequiredWork)
            {
                return 0;
            }
        }

        int dayTick = ((_tick % GameClock.TicksPerInGameDay)
            + GameClock.TicksPerInGameDay) % GameClock.TicksPerInGameDay;
        // The configured workday is no longer the whole "first half" of
        // the in-game day. The original formula assumed day = [0, DayTicks),
        // which broke once WorkdayStartTick moved to 1200. Compute the
        // ticks-until-boundary against the ABSOLUTE tick (the helpers
        // return absolute ticks) — subtracting the relative `dayTick`
        // would silently walk past a dawn boundary when _tick straddles
        // the day wrap (e.g. _tick = 4799 has dayTick = 1199 but the
        // next dawn is at absolute tick 4800).
        int lastTickInPhase = GameClock.IsDaytime(_tick)
            ? GameClock.NextWorkdayEnd(_tick) - 1
            : GameClock.NextWorkdayStart(_tick) - 1;
        int ticksBeforeBoundary = lastTickInPhase - _tick;
        int tickCount = Math.Min(maxTickCount, ticksBeforeBoundary);
        int recoveryBoundary = _citizens.Values
            .Where(citizen => citizen.Commitment.Kind == CitizenCommitmentKind.Recovery)
            .Select(citizen => citizen.Wound?.RecoveryTicksRemaining ?? 0)
            .Where(remaining => remaining > 0)
            .DefaultIfEmpty(int.MaxValue)
            .Min();
        tickCount = Math.Min(tickCount, recoveryBoundary);
        int expeditionBoundary = _expeditions.Values
            .Where(expedition => expedition.Status == ExpeditionStatus.Active)
            .Select(ExpeditionTicksUntilNextBoundary)
            .Where(remaining => remaining > 0)
            .DefaultIfEmpty(int.MaxValue)
            .Min();
        tickCount = Math.Min(tickCount, expeditionBoundary);
        int cropBoundary = _cultivationSites.Values
            .Where(site => site.State is CultivationPlotState.Sown
                or CultivationPlotState.Growing)
            .Select(site => site.ReadyAtTick.HasValue
                ? site.ReadyAtTick.Value - _tick
                : int.MaxValue)
            .Where(remaining => remaining > 0)
            .DefaultIfEmpty(int.MaxValue)
            .Min();
        tickCount = Math.Min(tickCount, cropBoundary);
        // A journey in flight is a scheduled state change like any other. The
        // guards above (no work assignment, no travelling patient) cover most
        // travellers, but not one who was sent home after losing their
        // commitment — batching straight over their arrival tick would leave
        // them InTransit for the whole catch-up.
        //
        // Stops one tick *short* of the arrival, unlike the boundaries above:
        // batching moves the clock without running the per-tick rules, so the
        // arrival tick itself has to be reached by a canonical stepped tick or
        // nothing would complete the journey. Landing the batch exactly on the
        // arrival would consume it silently and leave the citizen walking.
        int travelBoundary = int.MaxValue;
        foreach (Citizen citizen in _citizens.Values)
        {
            if (citizen.TravelArrivalTick is not int arrivesAt) continue;
            travelBoundary = Math.Min(travelBoundary, arrivesAt - _tick - 1);
        }
        if (travelBoundary <= 0) return 0;
        tickCount = Math.Min(tickCount, travelBoundary);
        if (tickCount <= 0) return 0;

        ApplyUpkeepBatch(tickCount);
        bool isDaytime = GameClock.IsDaytime(_tick);
        // Stop causes are labour facts, so they read IsLaborTime() exactly like
        // the per-tick loop does. Reading GameClock.IsDaytime directly here used
        // to report `night` for a founding camp that the bypass keeps working at
        // any hour. The citizen meal/rest branch below deliberately stays on the
        // raw clock, mirroring ProcessCitizenNeedsAndStandingOrders.
        bool isLaborTime = IsLaborTime();
        foreach (var building in _buildings.Values)
        {
            building.LastTickProduction = 0;
            building.StopCause = isLaborTime
                ? BuildingProductionSimulation.ResolveStopCauseWhenNotProducing(building)
                : ProductionStopCause.Night;
        }
        foreach (var project in _projects.Values)
        {
            project.LastTickProgressAdded = 0;
            project.StopCause = ResolveQuiescentProjectStopCause(project, isLaborTime);
        }
        foreach (var citizen in _citizens.Values)
        {
            if (!isDaytime
                && citizen.Commitment.Kind != CitizenCommitmentKind.Expedition)
            {
                int fedTicks = Math.Min(tickCount, citizen.WellFedRemainingTicks);
                citizen.RestoreStamina(
                    StaminaRules.BaseRegenPerTick * tickCount
                    + StaminaRules.WellFedRegenBonus * fedTicks);
            }
            citizen.AdvanceWellFedTicks(tickCount);
        }
        _tick += tickCount;
        AdvanceCultivationSites();
        AdvanceWoundRecoveries(tickCount);
        AdvanceExpeditionPhases();
        CompleteFinishedExpeditions();
        return tickCount;
    }

    private int ExpeditionTicksUntilNextBoundary(Expedition expedition)
    {
        int duration = expedition.EndTick - expedition.StartTick;
        int boundaryTick = expedition.Phase switch
        {
            ExpeditionPhase.Outbound => expedition.StartTick
                + ExpeditionTiming.EncounterOffsetTicks(expedition),
            ExpeditionPhase.Encounter or ExpeditionPhase.Retreating =>
                expedition.StartTick + duration / 2,
            ExpeditionPhase.Objective => expedition.StartTick
                + ExpeditionTiming.ObjectiveOffsetTicks(expedition),
            ExpeditionPhase.Returning => expedition.EndTick,
            _ => _tick,
        };
        return Math.Max(0, boundaryTick - _tick);
    }

    private void ApplyUpkeepBatch(int tickCount)
    {
        // Upkeep is dormant. See ApplyUpkeep above for the rationale.
        _ = tickCount;
    }

    /// <summary>
    /// Current production amount per economic cycle for the given building.
    /// </summary>
    public int CurrentProductionRate(BuildingId buildingId)
    {
        if (!_buildings.TryGetValue(buildingId, out var building))
        {
            return 0;
        }

        var presentWorkers = new List<Citizen>();
        foreach (CitizenId citizenId in building.AssignedCitizenIds)
        {
            if (_citizens.TryGetValue(citizenId, out Citizen? citizen)
                && citizen.CurrentLocation == CitizenLocation.AtWork)
            {
                presentWorkers.Add(citizen);
            }
        }
        return BuildingProductionCalculator.ProductionPerTick(presentWorkers, building);
    }

    private void RaiseBuildingChanged(BuildingId buildingId)
    {
        BuildingChanged?.Invoke(this, new CityWorldChangedEventArgs(buildingId));
    }

    /// <summary>
    /// Removes a building from the world without raising an event.
    /// Used by the per-tick depletion sweep that might demolish
    /// several forests in one pass; the caller emits one
    /// <see cref="RaiseBuildingChanged"/> for the batch when needed.
    /// Free any assigned citizens via
    /// <see cref="TryUnassignCitizen"/> first so the world state
    /// stays consistent.
    /// </summary>
    private void RemoveBuildingInternal(BuildingId buildingId)
    {
        if (!_buildings.TryGetValue(buildingId, out var building)) return;

        // Free assigned citizens so they can be re-assigned elsewhere.
        var assignedCopy = new List<CitizenId>(building.AssignedCitizenIds);
        foreach (var citizenId in assignedCopy)
        {
            TryUnassignCitizen(buildingId, citizenId);
        }
        _buildings.Remove(buildingId);
        _parcelPlacements.Remove(buildingId);
    }

    /// <summary>
    /// Public demolition path: removes a building immediately and
    /// notifies subscribers. Used when the player explicitly tears
    /// down a building (future slice). Today's <see cref="DemolishDepletedForests"/>
    /// sweep batches internally to avoid per-building event spam.
    /// </summary>
    public bool RemoveBuilding(BuildingId buildingId)
    {
        if (!_buildings.ContainsKey(buildingId)) return false;
        RemoveBuildingInternal(buildingId);
        RaiseBuildingChanged(buildingId);
        return true;
    }

    private void RaiseProjectChanged(BuildingId projectId)
    {
        ProjectChanged?.Invoke(this, new CityWorldChangedEventArgs(projectId));
    }
}
