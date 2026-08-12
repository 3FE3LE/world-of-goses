#nullable enable
using System.Collections.Generic;
using WorldofGoses.Domain;
using WorldofGoses.Domain.Combat;

namespace WorldofGoses;

/// <summary>
/// Engine-free use-case facade for a single <see cref="CityWorld"/>.
///
/// <para>Architecture Hardening A5 establishes the real Application pattern.
/// Each public method on this class is one use case: a coordinated sequence
/// of domain operations that would otherwise live as orchestration logic
/// inside <c>CityWorldController</c> (a Godot <c>Node</c>). The controller
/// still owns <see cref="CityWorld"/> for now — A6/A7 will move ownership —
/// and creates one <see cref="CityGameSession"/> per controller instance
/// to mediate every command and query that reaches the domain.</para>
///
/// <para>This class deliberately stays small and explicit:</para>
/// <list type="bullet">
///   <item>No MediatR, no CQRS framework, no DI container, no service
///         locator, no <c>ICommandHandler&lt;T&gt;</c>.</item>
///   <item>No <c>Execute(Action&lt;CityWorld&gt;)</c>, <c>GetWorld()</c>,
///         or <c>WithWorld(...)</c> escape hatch. The world is reached
///         through the explicit use-case methods only.</item>
///   <item>Commands return semantic <c>*Result</c> types already defined by
///         the domain (<see cref="AssignmentResult"/>, <see cref="ToolCraftResult"/>,
///         <see cref="CultivationActionResult"/>, <see cref="ExpeditionStartResult"/>,
///         <see cref="HeroCreationResult"/>, <see cref="HeroIncorporationResult"/>,
///         <see cref="WoundRecoveryResult"/>, <see cref="ConstructionAuthorizationResult"/>).
///         Queries return existing immutable snapshots or scalar values.</item>
/// </list>
///
/// <para>Presentation may construct one of these directly when a non-Godot
/// caller needs the same orchestration (tests, headless tools). The class
/// is sealed; inheritance is not a use case.</para>
/// </summary>
public sealed class CityGameSession
{
    private readonly CityWorld _world;

    /// <summary>
    /// Wraps an existing <see cref="CityWorld"/>. The world is owned
    /// elsewhere (currently <c>CityWorldController</c>); A6/A7 will move
    /// ownership into the session.
    /// </summary>
    public CityGameSession(CityWorld world)
    {
        _world = world ?? throw new System.ArgumentNullException(nameof(world));
    }

    // ------------------------------------------------------------------------
    // Onboarding and citizen lifecycle
    // ------------------------------------------------------------------------

    /// <summary>
    /// Closes the hero onboarding flow. When the flow has already created a
    /// pending hero, this completes by saving and returning the citizen;
    /// otherwise it asks the domain to author the founder, seeds the two
    /// starting forests plus the four rudimentary ground opportunities, and
    /// persists.
    /// </summary>
    public HeroCreationResult CompleteOnboarding(HeroCreationRequest request)
    {
        return _world.TryCreateHero(request);
    }

    /// <summary>
    /// Promotes a recruited citizen to Hero rank. The controller still
    /// drives the persistence side-effect (autosave) and the
    /// <c>CitizensChanged</c> signal.
    /// </summary>
    public HeroIncorporationResult TryIncorporateHero(CitizenId citizenId) =>
        _world.TryIncorporateHero(citizenId);

    /// <summary>
    /// Starts wound recovery at the Basic Shelter for the named citizen.
    /// </summary>
    public WoundRecoveryResult TryBeginWoundRecovery(CitizenId citizenId) =>
        _world.TryBeginWoundRecovery(citizenId);

    /// <summary>
    /// Accepts the prospect currently waiting at the Town Hall and grows
    /// them into a new citizen.
    /// </summary>
    public CityWorld.MigrantResult TryAcceptPendingProspect() =>
        _world.TryAcceptPendingProspect();

    // ------------------------------------------------------------------------
    // Citizen assignment
    // ------------------------------------------------------------------------

    public AssignmentResult TryAssignCitizen(BuildingId buildingId, CitizenId citizenId) =>
        _world.TryAssignCitizen(buildingId, citizenId);

    public AssignmentResult TryUnassignCitizen(BuildingId buildingId, CitizenId citizenId) =>
        _world.TryUnassignCitizen(buildingId, citizenId);

    public AssignmentResult TryAssignCitizenToProject(BuildingId projectId, CitizenId citizenId) =>
        _world.TryAssignToProject(projectId, citizenId);

    public AssignmentResult TryUnassignCitizenFromProject(BuildingId projectId, CitizenId citizenId) =>
        _world.TryUnassignFromProject(projectId, citizenId);

    // ------------------------------------------------------------------------
    // Construction and production
    // ------------------------------------------------------------------------

    public ConstructionAuthorizationResult TryAuthorizeBasicShelter() =>
        _world.TryAuthorizeConstruction(ConstructionKind.BasicShelter);

    public ConstructionAuthorizationResult TryAuthorizeConstruction(ConstructionKind kind) =>
        _world.TryAuthorizeConstruction(kind);

    public ConstructionAuthorizationResult TryAuthorizeConstruction(
        ConstructionKind kind,
        ConstructionLot? selectedLot) =>
        _world.TryAuthorizeConstruction(kind, selectedLot);

    public ConstructionAuthorizationResult TryAuthorizeFoundingSiteModule(
        BuildingId projectId,
        FoundingSiteModule module) =>
        _world.TryAuthorizeFoundingSiteModule(projectId, module);

    public int ReturnFoundingCargo() => _world.ReturnFoundingCargo();

    public void SetProjectEnabled(BuildingId projectId, bool enabled) =>
        _world.SetProjectEnabled(projectId, enabled);

    public bool CancelProject(BuildingId projectId) => _world.CancelProject(projectId);

    public void ConfigureProductionPolicy(
        BuildingId buildingId,
        bool enabled,
        int minStock,
        int maxStock,
        int priority) =>
        _world.ConfigureProductionPolicy(buildingId, enabled, minStock, maxStock, priority);

    public void SetProductionEnabled(BuildingId buildingId, bool enabled) =>
        _world.SetProductionEnabled(buildingId, enabled);

    public int AdvanceProduction(BuildingId buildingId) =>
        _world.AdvanceProduction(buildingId);

    // ------------------------------------------------------------------------
    // Cultivation
    // ------------------------------------------------------------------------

    public CultivationActionResult TrySowCultivationSite(BuildingId siteId) =>
        _world.TrySowCultivationSite(siteId);

    public CultivationActionResult TryHarvestCultivationSite(BuildingId siteId) =>
        _world.TryHarvestCultivationSite(siteId);

    // ------------------------------------------------------------------------
    // Tools and ground resources
    // ------------------------------------------------------------------------

    public ToolCraftResult TryCraftTool(ToolKind tool) => _world.TryCraftTool(tool);

    public int GatherFromPatch(int patchId, int unitId, int amount) =>
        _world.GatherFromPatch(patchId, unitId, amount);

    public NaturalResourceGatherResult GetNaturalResourceGatherAvailability(
        int patchId,
        int unitId) =>
        _world.NaturalResourceGatherAvailability(patchId, unitId);

    public NaturalResourceGatherResult TryGatherFromPatch(
        int patchId,
        int unitId,
        int amount) =>
        _world.TryGatherFromPatch(patchId, unitId, amount);

    // ------------------------------------------------------------------------
    // Expeditions
    // ------------------------------------------------------------------------

    public ExpeditionStartResult StartExpedition(ExpeditionRequest request) =>
        _world.StartExpedition(request);

    public ExpeditionStartResult StartResourceExpedition(
        ResourceOpportunityId opportunityId,
        IReadOnlyList<CitizenId> memberIds,
        ExpeditionRetreatPosture retreatPosture) =>
        _world.StartResourceExpedition(opportunityId, memberIds, retreatPosture);

    public bool CancelExpedition(ExpeditionId id) => _world.CancelExpedition(id);

    public bool SetCombatAutoSkillsEnabled(ExpeditionId expeditionId, bool enabled) =>
        _world.SetCombatAutoSkillsEnabled(expeditionId, enabled);

    public bool TryActivateMemberSkill(ExpeditionId expeditionId, int slotIndex) =>
        _world.TryActivateMemberSkill(expeditionId, slotIndex);

    // ------------------------------------------------------------------------
    // First Night
    // ------------------------------------------------------------------------

    public bool TryOpenFirstNightDialogue(string nodeId) =>
        _world.TryOpenFirstNightDialogue(nodeId);

    public bool TryCloseFirstNightDialogue() => _world.TryCloseFirstNightDialogue();

    // ------------------------------------------------------------------------
    // Snapshot queries — return immutable read models
    // ------------------------------------------------------------------------

    public CityStatusSnapshot GetCityStatusSnapshot() => CityStatusSnapshot.From(_world);

    public ConstructionSnapshot GetConstructionSnapshot() => ConstructionSnapshot.From(_world);

    public BuildingDetailSnapshot? GetBuildingDetailSnapshot(BuildingId buildingId) =>
        BuildingDetailSnapshot.From(_world, buildingId);

    public CityMacroSnapshot GetCityMacroSnapshot() => CityMacroSnapshot.From(_world);

    public ConstructionPlacementSnapshot GetConstructionPlacementSnapshot() =>
        ConstructionPlacementSnapshot.From(_world);

    public ExpeditionPlanningSnapshot GetExpeditionPlanningSnapshot() =>
        ExpeditionPlanningSnapshot.From(_world);

    public ExpeditionRailSnapshot GetExpeditionRailSnapshot() =>
        ExpeditionRailSnapshot.From(_world);

    public ExpeditionLiveSnapshot? GetExpeditionLiveSnapshot(ExpeditionId expeditionId) =>
        ExpeditionLiveSnapshot.From(_world, expeditionId);

    public CityPolicySnapshot GetCityPolicySnapshot() => CityPolicySnapshot.From(_world);

    public HeroProfileSnapshot? GetHeroProfileSnapshot() => HeroProfileSnapshot.From(_world);

    public CultivationSiteSnapshot? GetCultivationSiteSnapshot(BuildingId siteId) =>
        CultivationSiteSnapshot.From(_world, siteId);

    public CitizenRoutineSnapshot? GetCitizenRoutineSnapshot(CitizenId id) =>
        _world.GetCitizenRoutine(id);

    public MacroStreetLiveViewState GetMacroStreetViewState() =>
        MacroStreetLiveViewState.From(_world);

    public CombatSessionSnapshot? GetCombatSessionSnapshot(ExpeditionId expeditionId) =>
        _world.GetCombatSessionSnapshot(expeditionId);

    public ExpeditionPanelState GetExpeditionPanelState() =>
        ExpeditionPanelState.From(_world);

    public RosterSnapshot GetRosterSnapshot() => RosterSnapshot.From(_world);

    public MacroCitizenSnapshot? TryGetMacroCitizenSnapshot(CitizenId id) =>
        MacroCitizenSnapshot.From(_world, id);

    // ------------------------------------------------------------------------
    // Value queries — boolean / scalar projections
    // ------------------------------------------------------------------------

    public int CurrentTick => _world.CurrentTick;

    public CitizenId? HeroId => _world.Hero?.Id;

    public LineageId? HeroLineageId => _world.Hero?.Profile.Lineage;

    public bool HasHero => _world.Hero is not null;

    public bool NeedsOnboarding => _world.NeedsOnboarding;

    public int? FoundingSiteBuildingId => _world.FoundingSiteBuildingId();

    public BuildingId? FoundingStorageBuildingId
    {
        get
        {
            int? id = _world.FoundingSiteBuildingId();
            return id.HasValue ? new BuildingId(id.Value) : null;
        }
    }

    public BuildingId? PrimaryHomeId => _world.PrimaryHome?.Id;

    public int FoodStock => _world.FoodStock;

    public bool HasCultivationSite => _world.CultivationSites.Count > 0;

    public bool HasTownHall
    {
        get
        {
            foreach (var building in _world.Buildings.Values)
            {
                if (building.Kind == BuildingKind.TownHall) return true;
            }
            return false;
        }
    }

    public bool IsHousingFull => _world.AvailableHousing == 0;

    public bool HasPendingProspect => _world.PendingProspect is not null;

    public int CurrentProductionRate(BuildingId buildingId) =>
        _world.CurrentProductionRate(buildingId);

    public bool IsFirstNightActive => _world.IsFirstNightActive;

    public FirstNightStage? FirstNightStage =>
        _world.FirstNight is { } night ? night.Stage : null;

    /// <summary>
    /// True when the world carries a <see cref="Domain.FirstNightState"/>.
    /// Distinct from <see cref="IsFirstNightActive"/>: the night can be
    /// staged but already concluded, in which case the presentation
    /// never needs to emit a stage transition.
    /// </summary>
    public bool HasFirstNight => _world.FirstNight is not null;

    public bool HasFoundingSiteModule(FoundingSiteModule module) =>
        _world.HasFoundingSiteModule(module);

    public bool HasSpiritDepartedEvent()
    {
        foreach (WorldEvent evt in _world.Log.Events)
        {
            if (evt.Kind == WorldEventKind.SpiritDeparted) return true;
        }
        return false;
    }

    public int? GetDisplayedTick() =>
        _world.FirstNight?.DisplayedTick(_world.CurrentTick);

    public bool TryGetCitizenDisplayName(CitizenId id, out string? name)
    {
        Citizen? citizen = _world.GetCitizen(id);
        name = citizen?.Name;
        return citizen is not null;
    }

    public bool TryGetBuildingDisplayName(BuildingId id, out string? name)
    {
        Building? building = _world.GetBuilding(id);
        name = building?.DisplayName;
        return building is not null;
    }

    public bool TryGetProjectDisplayName(BuildingId id, out string? name)
    {
        ConstructionProject? project = _world.GetProject(id);
        name = project?.DisplayName;
        return project is not null;
    }

    public int GetExpeditionStartTick(ExpeditionId id)
    {
        return _world.Expeditions.TryGetValue(id, out Expedition? expedition)
            ? expedition.StartTick
            : 0;
    }

    public bool TryGetActiveExpeditionId(out ExpeditionId id)
    {
        foreach (Expedition expedition in _world.Expeditions.Values)
        {
            if (expedition.Status == ExpeditionStatus.Active)
            {
                id = expedition.Id;
                return true;
            }
        }
        id = default;
        return false;
    }

    public bool TryGetExpeditionMemberDisplayNames(
        ExpeditionId id,
        out IReadOnlyList<string>? names)
    {
        if (!_world.Expeditions.TryGetValue(id, out Expedition? expedition))
        {
            names = null;
            return false;
        }
        var resolved = new List<string>(expedition.MemberIds.Count);
        foreach (CitizenId memberId in expedition.MemberIds)
        {
            Citizen? citizen = _world.GetCitizen(memberId);
            resolved.Add(citizen?.Name ?? "?");
        }
        names = resolved;
        return true;
    }

    public bool TryGetNextTerritoryParcelId(out int? parcelId)
    {
        CityParcel? target = _world.NextTerritoryTarget;
        parcelId = target?.Id.Value;
        return target is not null;
    }

    // ------------------------------------------------------------------------
    // World-time use case — presentation keeps the cadence, the signal
    // emission, and the dirty-bit; the session owns the domain tick.
    // ------------------------------------------------------------------------

    public void AdvanceWorldTick() => _world.AdvanceWorldTick();

    /// <summary>
    /// Seeds the two starting forests and the four rudimentary ground
    /// opportunities required by an onboarding flow. Idempotent and
    /// silent when the world already has forests or no hero.
    /// </summary>
    public void SeedStartingForests() => _world.SeedStartingForests();

    /// <summary>
    /// Seeds the four rudimentary ground resources on free parcels.
    /// Idempotent and silent when no free parcel is available.
    /// </summary>
    public void SeedStartingOpportunities() => _world.SeedStartingOpportunities();
}
