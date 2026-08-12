#nullable enable
using System;
using System.Collections.Generic;
using WorldofGoses.Domain;
using WorldofGoses.Domain.Combat;

namespace WorldofGoses;

/// <summary>
/// Engine-free use-case facade that owns the single live <see cref="CityWorld"/>
/// for one city.
///
/// <para>Architecture Hardening A5 established the real Application pattern.
/// Each public method on this class is one use case: a coordinated sequence
/// of domain operations that would otherwise live as orchestration logic
/// inside <c>CityWorldController</c> (a Godot <c>Node</c>). The controller
/// reduces to a Godot adapter (lifecycle, signals, persistence orchestration)
/// and delegates every gameplay command, snapshot query and world-tick
/// advancement here.</para>
///
/// <para>Architecture Hardening A8 makes this class the **owner** of
/// <see cref="CityWorld"/>: the aggregate is constructed by the default
/// constructor and is only reachable through the explicit use-case methods
/// plus the narrowly-scoped <see cref="World"/> fixture seam. The legacy
/// <c>internal CityWorld World</c> getter that lived on the controller is
/// gone, and so is the controller's own <c>_world</c> field.</para>
///
/// <para>Rules enforced by this slice:</para>
/// <list type="bullet">
///   <item>No MediatR, no CQRS framework, no DI container, no service
///         locator, no <c>ICommandHandler&lt;T&gt;</c>.</item>
///   <item>No <c>Execute(Action&lt;CityWorld&gt;)</c>, <c>GetDomain()</c>,
///         <c>GetCitizenEntity()</c>, or <c>WithWorld(...)</c> escape hatch
///         in the public surface. The world is reached through the explicit
///         use-case methods only, except through the <c>internal</c>
///         fixture seam (visual-regression only — gated by
///         <c>WOG_VISUAL_CAPTURE</c> or command-line flag).</item>
///   <item>Commands return semantic <c>*Result</c> types already defined by
///         the domain (<see cref="AssignmentResult"/>, <see cref="ToolCraftResult"/>,
///         <see cref="CultivationActionResult"/>, <see cref="ExpeditionStartResult"/>,
///         <see cref="HeroCreationResult"/>, <see cref="HeroIncorporationResult"/>,
///         <see cref="WoundRecoveryResult"/>, <see cref="ConstructionAuthorizationResult"/>).
///         Queries return existing immutable snapshots or scalar values.</item>
///   <item>Persistence orchestration stays in the controller because the
///         Application assembly intentionally does not reference the
///         Persistence assembly (A6 rule, enforced by
///         <c>Layer_DoesNotReferencePersistenceAssembly</c>). The
///         controller reaches the session's owned world through the
///         <c>internal</c> <see cref="World"/> getter and writes the slot
///         through <c>WorldPersistence</c>.</item>
/// </list>
///
/// <para>Presentation may construct one of these directly when a non-Godot
/// caller needs the same orchestration (tests, headless tools). The class
/// is sealed; inheritance is not a use case.</para>
/// </summary>
public sealed class CityGameSession
{
    private readonly CityWorld _world;
    private bool _isDirty;
    private DateTimeOffset _lastSimulationProcessedAt;

    /// <summary>
    /// Production entry point: constructs the session over a fresh
    /// <see cref="CityWorld"/>. The controller calls this from its
    /// parameterless constructor.
    /// </summary>
    public CityGameSession()
        : this(new CityWorld())
    {
    }

    /// <summary>
    /// Handle to the owned aggregate. <c>internal</c> to keep the public
    /// surface engine-free and use-case-only. Two callers have a
    /// legitimate reason to reach it:
    /// <list type="bullet">
    ///   <item>The <c>WorldofGoses.Tests</c> assembly, via
    ///         <c>[assembly: InternalsVisibleTo("WorldofGoses.Tests")]</c>
    ///         — tests that author a deterministic world through
    ///         <c>TestHelpers.NewHeroWorld()</c> and want to drive the
    ///         same use-case API as production.</item>
    ///   <item>The Godot presentation assembly, via
    ///         <c>[assembly: InternalsVisibleTo("World of Goses")]</c> —
    ///         the controller subscribes to events on the owned world,
    ///         and the visual-regression fixture seam forwards a
    ///         narrowly-scoped reach to <c>CityPrototype</c>.</item>
    /// </list>
    /// Production code never reads this getter for gameplay; the
    /// controller's snapshot queries and use-case methods cover every
    /// gameplay read. <see cref="ArchitectureBoundaryTests"/>
    /// enforces that rule at the build level.
    /// </summary>
    internal CityWorld World => _world;

    /// <summary>
    /// Forwards the world's <see cref="CityWorld.BuildingChanged"/> event.
    /// The controller subscribes here instead of on the world directly,
    /// so the controller never needs a <see cref="CityWorld"/> reference
    /// outside the fixture seam.
    /// </summary>
    public event EventHandler<CityWorldChangedEventArgs>? BuildingChanged;

    /// <summary>Forward of <see cref="CityWorld.ProjectChanged"/>.</summary>
    public event EventHandler<CityWorldChangedEventArgs>? ProjectChanged;

    /// <summary>Forward of <see cref="CityWorld.PatchChanged"/>.</summary>
    public event EventHandler<PatchChangedEventArgs>? PatchChanged;

    /// <summary>Forward of <see cref="CityWorld.CultivationSiteChanged"/>.</summary>
    public event EventHandler<CityWorldChangedEventArgs>? CultivationSiteChanged;

    /// <summary>Forward of <see cref="CityWorld.ExpeditionChanged"/>.</summary>
    public event EventHandler<ExpeditionChangedEventArgs>? ExpeditionChanged;

    /// <summary>
    /// Constructs a session over an externally-built world and wires
    /// the world's events onto this class's forwarders. The constructor
    /// is <c>internal</c> because only the test assembly has a legitimate
    /// reason to bring its own world; production goes through the
    /// parameterless <see cref="CityGameSession()"/>.
    /// </summary>
    internal CityGameSession(CityWorld world)
    {
        _world = world ?? throw new ArgumentNullException(nameof(world));
        _world.BuildingChanged += OnWorldBuildingChanged;
        _world.ProjectChanged += OnWorldProjectChanged;
        _world.PatchChanged += OnWorldPatchChanged;
        _world.CultivationSiteChanged += OnWorldCultivationSiteChanged;
        _world.ExpeditionChanged += OnWorldExpeditionChanged;
    }

    private void OnWorldBuildingChanged(object? sender, CityWorldChangedEventArgs e)
    {
        _isDirty = true;
        BuildingChanged?.Invoke(this, e);
    }

    private void OnWorldProjectChanged(object? sender, CityWorldChangedEventArgs e)
    {
        _isDirty = true;
        ProjectChanged?.Invoke(this, e);
    }

    private void OnWorldPatchChanged(object? sender, PatchChangedEventArgs e)
    {
        _isDirty = true;
        PatchChanged?.Invoke(this, e);
    }

    private void OnWorldCultivationSiteChanged(object? sender, CityWorldChangedEventArgs e)
    {
        _isDirty = true;
        CultivationSiteChanged?.Invoke(this, e);
    }

    private void OnWorldExpeditionChanged(object? sender, ExpeditionChangedEventArgs e)
    {
        _isDirty = true;
        ExpeditionChanged?.Invoke(this, e);
    }

    /// <summary>
    /// True when at least one persistence-affecting mutation has happened
    /// since the last successful save. The controller reads this to gate
    /// the autosave loop and to avoid emitting the <c>WorldSaved</c> signal
    /// when nothing changed.
    /// </summary>
    public bool IsDirty => _isDirty;

    /// <summary>
    /// Wall-clock instant of the last <see cref="AdvanceWorldTick"/>.
    /// Mirrors the value the controller used to keep on
    /// <c>_lastSimulationProcessedAtUnixMillis</c>; the controller now
    /// projects it to milliseconds for the snapshot when it needs to.
    /// </summary>
    public DateTimeOffset LastSimulationProcessedAt => _lastSimulationProcessedAt;

    /// <summary>
    /// Marks the session clean without persisting. Use this from
    /// fixture seeds that bypass the save loop, so the next autosave
    /// tick does not immediately rewrite the slot.
    /// </summary>
    public void MarkClean() => _isDirty = false;

    /// <summary>
    /// Marks the session dirty from an external trigger. The fixture
    /// seam uses it after a manual world mutation so the autosave gate
    /// sees the change.
    /// </summary>
    public void MarkDirty() => _isDirty = true;

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
        HeroCreationResult result = _world.TryCreateHero(request);
        if (result.IsSuccess) _isDirty = true;
        return result;
    }

    /// <summary>
    /// Promotes a recruited citizen to Hero rank. The controller still
    /// drives the persistence side-effect (autosave) and the
    /// <c>CitizensChanged</c> signal.
    /// </summary>
    public HeroIncorporationResult TryIncorporateHero(CitizenId citizenId)
    {
        HeroIncorporationResult result = _world.TryIncorporateHero(citizenId);
        if (result.IsSuccess) _isDirty = true;
        return result;
    }

    /// <summary>
    /// Starts wound recovery at the Basic Shelter for the named citizen.
    /// </summary>
    public WoundRecoveryResult TryBeginWoundRecovery(CitizenId citizenId)
    {
        WoundRecoveryResult result = _world.TryBeginWoundRecovery(citizenId);
        if (result.IsSuccess) _isDirty = true;
        return result;
    }

    /// <summary>
    /// Accepts the prospect currently waiting at the Town Hall and grows
    /// them into a new citizen.
    /// </summary>
    public CityWorld.MigrantResult TryAcceptPendingProspect()
    {
        CityWorld.MigrantResult result = _world.TryAcceptPendingProspect();
        if (result.IsSuccess) _isDirty = true;
        return result;
    }

    // ------------------------------------------------------------------------
    // Citizen assignment
    // ------------------------------------------------------------------------

    public AssignmentResult TryAssignCitizen(BuildingId buildingId, CitizenId citizenId)
    {
        AssignmentResult result = _world.TryAssignCitizen(buildingId, citizenId);
        if (result.IsSuccess) _isDirty = true;
        return result;
    }

    public AssignmentResult TryUnassignCitizen(BuildingId buildingId, CitizenId citizenId)
    {
        AssignmentResult result = _world.TryUnassignCitizen(buildingId, citizenId);
        if (result.IsSuccess) _isDirty = true;
        return result;
    }

    public AssignmentResult TryAssignCitizenToProject(BuildingId projectId, CitizenId citizenId)
    {
        AssignmentResult result = _world.TryAssignToProject(projectId, citizenId);
        if (result.IsSuccess) _isDirty = true;
        return result;
    }

    public AssignmentResult TryUnassignCitizenFromProject(BuildingId projectId, CitizenId citizenId)
    {
        AssignmentResult result = _world.TryUnassignFromProject(projectId, citizenId);
        if (result.IsSuccess) _isDirty = true;
        return result;
    }

    // ------------------------------------------------------------------------
    // Construction and production
    // ------------------------------------------------------------------------

    public ConstructionAuthorizationResult TryAuthorizeBasicShelter()
    {
        ConstructionAuthorizationResult result = _world.TryAuthorizeConstruction(ConstructionKind.BasicShelter);
        if (result.IsSuccess) _isDirty = true;
        return result;
    }

    public ConstructionAuthorizationResult TryAuthorizeConstruction(ConstructionKind kind)
    {
        ConstructionAuthorizationResult result = _world.TryAuthorizeConstruction(kind);
        if (result.IsSuccess) _isDirty = true;
        return result;
    }

    public ConstructionAuthorizationResult TryAuthorizeConstruction(
        ConstructionKind kind,
        ConstructionLot? selectedLot)
    {
        ConstructionAuthorizationResult result = _world.TryAuthorizeConstruction(kind, selectedLot);
        if (result.IsSuccess) _isDirty = true;
        return result;
    }

    public ConstructionAuthorizationResult TryAuthorizeFoundingSiteModule(
        BuildingId projectId,
        FoundingSiteModule module)
    {
        ConstructionAuthorizationResult result = _world.TryAuthorizeFoundingSiteModule(projectId, module);
        if (result.IsSuccess) _isDirty = true;
        return result;
    }

    public int ReturnFoundingCargo()
    {
        int returned = _world.ReturnFoundingCargo();
        if (returned > 0) _isDirty = true;
        return returned;
    }

    public void SetProjectEnabled(BuildingId projectId, bool enabled)
    {
        _world.SetProjectEnabled(projectId, enabled);
        _isDirty = true;
    }

    public bool CancelProject(BuildingId projectId)
    {
        bool cancelled = _world.CancelProject(projectId);
        if (cancelled) _isDirty = true;
        return cancelled;
    }

    public void ConfigureProductionPolicy(
        BuildingId buildingId,
        bool enabled,
        int minStock,
        int maxStock,
        int priority)
    {
        _world.ConfigureProductionPolicy(buildingId, enabled, minStock, maxStock, priority);
        _isDirty = true;
    }

    public void SetProductionEnabled(BuildingId buildingId, bool enabled)
    {
        _world.SetProductionEnabled(buildingId, enabled);
        _isDirty = true;
    }

    public int AdvanceProduction(BuildingId buildingId)
    {
        int delta = _world.AdvanceProduction(buildingId);
        if (delta != 0) _isDirty = true;
        return delta;
    }

    // ------------------------------------------------------------------------
    // Cultivation
    // ------------------------------------------------------------------------

    public CultivationActionResult TrySowCultivationSite(BuildingId siteId)
    {
        CultivationActionResult result = _world.TrySowCultivationSite(siteId);
        if (result.IsSuccess) _isDirty = true;
        return result;
    }

    public CultivationActionResult TryHarvestCultivationSite(BuildingId siteId)
    {
        CultivationActionResult result = _world.TryHarvestCultivationSite(siteId);
        if (result.IsSuccess) _isDirty = true;
        return result;
    }

    // ------------------------------------------------------------------------
    // Tools and ground resources
    // ------------------------------------------------------------------------

    public ToolCraftResult TryCraftTool(ToolKind tool)
    {
        ToolCraftResult result = _world.TryCraftTool(tool);
        if (result.IsSuccess) _isDirty = true;
        return result;
    }

    public int GatherFromPatch(int patchId, int unitId, int amount)
    {
        int gathered = _world.GatherFromPatch(patchId, unitId, amount);
        if (gathered > 0) _isDirty = true;
        return gathered;
    }

    public NaturalResourceGatherResult GetNaturalResourceGatherAvailability(
        int patchId,
        int unitId) =>
        _world.NaturalResourceGatherAvailability(patchId, unitId);

    public NaturalResourceGatherResult TryGatherFromPatch(
        int patchId,
        int unitId,
        int amount)
    {
        NaturalResourceGatherResult result = _world.TryGatherFromPatch(patchId, unitId, amount);
        if (result.IsSuccess) _isDirty = true;
        return result;
    }

    // ------------------------------------------------------------------------
    // Expeditions
    // ------------------------------------------------------------------------

    public ExpeditionStartResult StartExpedition(ExpeditionRequest request)
    {
        ExpeditionStartResult result = _world.StartExpedition(request);
        if (result.IsSuccess) _isDirty = true;
        return result;
    }

    public ExpeditionStartResult StartResourceExpedition(
        ResourceOpportunityId opportunityId,
        IReadOnlyList<CitizenId> memberIds,
        ExpeditionRetreatPosture retreatPosture)
    {
        ExpeditionStartResult result = _world.StartResourceExpedition(opportunityId, memberIds, retreatPosture);
        if (result.IsSuccess) _isDirty = true;
        return result;
    }

    public bool CancelExpedition(ExpeditionId id)
    {
        bool cancelled = _world.CancelExpedition(id);
        if (cancelled) _isDirty = true;
        return cancelled;
    }

    public bool SetCombatAutoSkillsEnabled(ExpeditionId expeditionId, bool enabled)
    {
        bool changed = _world.SetCombatAutoSkillsEnabled(expeditionId, enabled);
        if (changed) _isDirty = true;
        return changed;
    }

    public bool TryActivateMemberSkill(ExpeditionId expeditionId, int slotIndex)
    {
        bool accepted = _world.TryActivateMemberSkill(expeditionId, slotIndex);
        if (accepted) _isDirty = true;
        return accepted;
    }

    // ------------------------------------------------------------------------
    // First Night
    // ------------------------------------------------------------------------

    public bool TryOpenFirstNightDialogue(string nodeId)
    {
        bool opened = _world.TryOpenFirstNightDialogue(nodeId);
        if (opened) _isDirty = true;
        return opened;
    }

    public bool TryCloseFirstNightDialogue()
    {
        bool advanced = _world.TryCloseFirstNightDialogue();
        if (advanced) _isDirty = true;
        return advanced;
    }

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
    // World-time use case — presentation keeps the cadence and the signal
    // emission; the session owns the domain tick and the dirty-bit.
    // ------------------------------------------------------------------------

    public void AdvanceWorldTick()
    {
        _world.AdvanceWorldTick();
        _isDirty = true;
        _lastSimulationProcessedAt = DateTimeOffset.UtcNow;
    }

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
