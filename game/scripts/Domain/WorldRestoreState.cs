#nullable enable
using System.Collections.Generic;
using WorldofGoses.Domain;
using WorldofGoses.Domain.Combat;

namespace WorldofGoses.Domain;

/// <summary>
/// Semantic, engine-free state required to restore a <see cref="CityWorld"/>.
///
/// <para>Architecture Hardening A7 introduces this contract so
/// Domain never has to accept a JSON-shaped save DTO. Persistence
/// translates the persisted format into a <see cref="WorldRestoreState"/>
/// through its ID mappers and migration chain; Domain consumes the
/// already-decoded, already-validated semantic state. Renaming a C#
/// enum does not change this state, and therefore does not change
/// the save format.</para>
///
/// <para>This type lives in Domain because Domain owns the rules
/// about what constitutes a valid, restorable city. Persistence
/// owns the persisted shape and translates between shapes;
/// Application owns the use cases that orchestrate capture/save and
/// load/restore.</para>
/// </summary>
public sealed class WorldRestoreState
{
    public int CurrentTick { get; init; }
    public int EconomicBalanceVersion { get; init; }
    public long LastSeenAtUnixMillis { get; init; }
    public int? PendingProspectSeed { get; init; }
    public string? PendingProspectName { get; init; }

    public IReadOnlyList<RestoredParcel> Parcels { get; init; } =
        System.Array.Empty<RestoredParcel>();
    public IReadOnlyList<RestoredNaturalResourcePatch> NaturalResourcePatches { get; init; } =
        System.Array.Empty<RestoredNaturalResourcePatch>();
    public IReadOnlyList<RestoredParcelPlacement> ParcelPlacements { get; init; } =
        System.Array.Empty<RestoredParcelPlacement>();
    public IReadOnlyList<RestoredCorridorReservation> CorridorReservations { get; init; } =
        System.Array.Empty<RestoredCorridorReservation>();
    public IReadOnlyList<RestoredBuilding> Buildings { get; init; } =
        System.Array.Empty<RestoredBuilding>();
    public IReadOnlyList<RestoredCitizen> Citizens { get; init; } =
        System.Array.Empty<RestoredCitizen>();
    public IReadOnlyList<RestoredConstructionProject> Projects { get; init; } =
        System.Array.Empty<RestoredConstructionProject>();
    public IReadOnlyList<RestoredCultivationSite> CultivationSites { get; init; } =
        System.Array.Empty<RestoredCultivationSite>();
    public IReadOnlyList<RestoredResourceReservation> ResourceReservations { get; init; } =
        System.Array.Empty<RestoredResourceReservation>();
    public IReadOnlyList<RestoredResourceOpportunity> ResourceOpportunities { get; init; } =
        System.Array.Empty<RestoredResourceOpportunity>();
    public IReadOnlyList<RestoredExpedition> Expeditions { get; init; } =
        System.Array.Empty<RestoredExpedition>();
    public IReadOnlyList<RestoredWorldEvent> Events { get; init; } =
        System.Array.Empty<RestoredWorldEvent>();

    public RestoredEarlyGameMetrics? EarlyGameMetrics { get; init; }
    public RestoredFirstNight? FirstNight { get; init; }

    public IReadOnlyDictionary<ResourceType, int> CityInventory { get; init; } =
        new Dictionary<ResourceType, int>();
    public IReadOnlyList<ToolKind> Tools { get; init; } = System.Array.Empty<ToolKind>();
}

public sealed record RestoredParcel(
    int Id,
    int LogicalColumn,
    int LogicalRow,
    ParcelTerritoryState TerritoryState);

public sealed record RestoredNaturalResourcePatch(
    int Id,
    int ParcelId,
    ResourceType ResourceType,
    IReadOnlyList<int> UnitReserves,
    int? LegacyStorageBuildingId,
    IReadOnlyList<RestoredNaturalResourceUnitPosition> UnitPositions);

public sealed record RestoredNaturalResourceUnitPosition(
    int RowWithinParcel,
    int FrontageColumnWithinParcel);

public sealed record RestoredParcelPlacement(
    int EntityId,
    int ParcelId,
    int LotColumn,
    int LotRow,
    int LotWidth,
    int LotHeight,
    int RowId,
    int StartColumn,
    int FrontageColumns,
    int DepthRows,
    int BaseFrontageColumns,
    int LeftExpansionColumns,
    int RightExpansionColumns,
    string FootprintProfileId,
    BuildingOrientation Orientation);

public sealed record RestoredCorridorReservation(
    int Id,
    int RowId,
    int StartColumn,
    int FrontageColumns);

public sealed record RestoredBuilding(
    int Id,
    string DisplayName,
    BuildingKind Kind,
    ResourceType ProducedResourceType,
    string ProducedCompetencyId,
    string ResourceLabel,
    string ResourceUnit,
    int WorkerCapacity,
    int VisualCapacity,
    int BaseProductionPerWorker,
    int StorageCapacity,
    int Stock,
    int IronStock,
    int? WoodReserve,
    IReadOnlyList<int> WoodUnitReserves,
    bool ProductionEnabled,
    int? MinStock,
    int? MaxStock,
    int? Priority,
    IReadOnlyList<int> AssignedCitizenIds,
    IReadOnlyList<FoundingSiteModule> FoundingSiteOriginModules);

public sealed record RestoredCitizen(
    int Id,
    string Name,
    int AppearanceSeed,
    CitizenOrigin Origin,
    string AppearanceVariant,
    RestoredCitizenProfile Profile,
    int? CurrentAssignment,
    RestoredCommitment Commitment,
    RestoredWorkOrder? WorkOrder,
    CitizenVitalStatus VitalStatus,
    int? TransitStartedAtTick,
    CitizenLocation CurrentLocation,
    int ResumeWorkNotBeforeTick,
    bool IsReturningHome,
    RestoredWound? Wound,
    int StaminaCurrent,
    int? StaminaMax,
    int WellFedRemainingTicks,
    RestoredEquipmentLoadout EquipmentLoadout,
    RestoredCurrentHealthAndCondition CurrentHealthAndCondition,
    int? LastVisitedResourceBuildingId,
    int? LastVisitedResourcePatchId,
    int? LastVisitedResourceUnitId,
    int? LastVisitedResourcePositionIndex,
    IReadOnlyList<RestoredCompetency> Competencies,
    IReadOnlyList<RestoredWeaponCompetency> WeaponCompetencies,
    IReadOnlyList<RestoredRole> Roles);

public sealed record RestoredCommitment(
    CitizenCommitmentKind Kind,
    int? EntityId);

public sealed record RestoredWorkOrder(
    CitizenCommitmentKind Kind,
    int TargetId);

public sealed record RestoredWound(
    WoundSeverity Severity,
    int OriginatingEventId,
    int RecoveryTicksRemaining);

public sealed record RestoredCitizenProfile(
    string Lineage,
    GenderId Gender,
    string ElementalAffinity,
    string CombatStyle,
    string PoliticalOrientation,
    string SpiritualPosture,
    IReadOnlyList<string> Aptitudes,
    IReadOnlyList<string> ProfessionalAffinities,
    IReadOnlyList<string> WeaponPreferences,
    IReadOnlyList<string> PersonalityTraits,
    RestoredFounderCubeProfile CubeProfile,
    RestoredFounderNarrativeMemory? FounderNarrativeMemory);

public sealed record RestoredFounderCubeProfile(
    string VertexA,
    string VertexB,
    string VertexC,
    string Domain,
    int DomainLevel,
    string Mastery,
    int MasteryLevel,
    int Sequence,
    string Signature);

public sealed record RestoredFounderNarrativeMemory(
    int Turn,
    RestoredFounderNarrativeQuestion Question,
    string? Resolution);

public sealed record RestoredFounderNarrativeQuestion(
    string Subject,
    string Predicate,
    string Object);

public sealed record RestoredEquipmentLoadout(
    string HelmetId,
    string ChestId,
    string LegsId,
    string BootsId,
    string GlovesId,
    string WeaponId);

public sealed record RestoredCurrentHealthAndCondition(
    double? CurrentHealth,
    double? ConditionFactor);

public sealed record RestoredCompetency(
    string Id,
    int Experience);

public sealed record RestoredWeaponCompetency(
    WeaponFamily Family,
    int Level,
    int Experience);

public sealed record RestoredRole(
    string Id,
    int GrantedAtTick);

public sealed record RestoredConstructionProject(
    int Id,
    ConstructionKind Kind,
    string DisplayName,
    int Progress,
    int RequiredWork,
    int WorkerCapacity,
    bool Enabled,
    IReadOnlyList<int> AssignedCitizenIds,
    FoundingSiteModule? ActiveFoundingModule,
    IReadOnlyList<FoundingSiteModule> CompletedFoundingModules,
    int PhaseStartedAtTick,
    IReadOnlyDictionary<ResourceType, int> DepositedInputs,
    IReadOnlyDictionary<ResourceType, int> RemainingInputs);

public sealed record RestoredCultivationSite(
    int Id,
    CultivationPlotState State,
    int PlantedTick,
    int ReadyAtTick);

public sealed record RestoredResourceReservation(
    int Id,
    ResourceType Resource,
    int Amount,
    ResourceReservationOwnerKind OwnerKind,
    int? OwnerEntityId);

public sealed record RestoredResourceOpportunity(
    int Id,
    ResourceOpportunityKind Kind,
    ResourceOpportunityState State,
    int? ReservedByExpeditionId);

public sealed record RestoredExpedition(
    int Id,
    string DisplayName,
    IReadOnlyList<int> MemberCitizenIds,
    int LeadCitizenId,
    int StartTick,
    int? EndTick,
    ResourceType? SupplyResource,
    int SupplyAmount,
    ResourceType? RewardResource,
    int RewardAmount,
    int? ReservationId,
    ExpeditionStatus Status,
    int ReturnedAmount,
    ExpeditionRewardKind RewardKind,
    int? DeliveredMigrantId,
    ExpeditionPhase Phase,
    ExpeditionEncounterOutcome? EncounterOutcome,
    ExpeditionRetreatPosture RetreatPosture,
    int? DispatchEventId,
    int? TargetParcelId,
    int? ResourceOpportunityId,
    ResourceOpportunityKind? ResourceOpportunityKind,
    int SetbackReturn,
    int PartialReturn,
    int CarryCapacity,
    int? ObjectiveReachedAtTick,
    int? CombatRulesVersion,
    bool HasCombatSession,
    int CombatStepsAdvanced,
    IReadOnlyList<RestoredCombatSessionCommand> CombatCommands,
    IReadOnlyList<RestoredWorldEvent> CombatLog);

public sealed record RestoredCombatSessionCommand(
    int BeforeStep,
    CombatSessionCommandKind Kind,
    int Value);

public sealed record RestoredWorldEvent(
    int Id,
    int Tick,
    WorldEventKind Kind,
    WorldEventSubjectKind SubjectKind,
    int? SubjectEntityId,
    string? SubjectDisplayName,
    int Amount,
    int? CauseEventId);

public sealed record RestoredEarlyGameMetrics(
    int? FirstShelterCompletedAtTick,
    int? FirstExpeditionDispatchedAtTick,
    int ExpeditionsDispatched,
    int ExpeditionAbsenceTicks,
    int DawnSamples,
    int IdleCitizenDays,
    int ObservedCitizenDays,
    int MinFoodHorizonTenths,
    int? FoodHorizonTenthsAtFirstShelter,
    IReadOnlyDictionary<ResourceType, int> Gathered,
    IReadOnlyDictionary<ResourceType, int> Consumed);

public sealed record RestoredFirstNight(
    FirstNightStage Stage,
    string? CurrentDialogueNodeId,
    int StartedAtTick,
    int? ConcludedAtTick);
