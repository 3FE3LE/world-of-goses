#nullable enable
using System.Collections.Generic;

using WorldofGoses.Domain;
namespace WorldofGoses.Persistence;

public sealed class CitizenSave
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int AppearanceSeed { get; set; }
    public string Origin { get; set; } = "";
    /// <summary>
    /// Cosmetic appearance profile id. Old saves deserialize as <c>null</c>;
    /// the loader resolves that to a deterministic default for the citizen.
    /// </summary>
    public string? AppearanceVariant { get; set; }
    public CitizenProfileSave? Profile { get; set; }
    /// <summary>
    /// The building the citizen is currently assigned to, mirroring
    /// <see cref="Citizen.CurrentAssignment"/>. Named on the DTO
    /// without a domain prefix to keep the wire format compact
    /// and to make the JSON shape match the live citizen field.
    /// </summary>
    public int? CurrentAssignment { get; set; }
    /// <summary>
    /// Additive v14 field. Older v14 saves omit it; restore infers the
    /// commitment from CurrentAssignment and active expeditions.
    /// </summary>
    public string? CommitmentKind { get; set; }
    public int? CommitmentEntityId { get; set; }
    /// <summary>Additive v15 standing order preserved across interruptions.</summary>
    public string? WorkOrderKind { get; set; }
    public int? WorkOrderEntityId { get; set; }
    public string? VitalStatus { get; set; }
    public int? TransitStartedAtTick { get; set; }
    public string? CurrentLocation { get; set; }
    public int ResumeWorkNotBeforeTick { get; set; }
    public bool IsReturningHome { get; set; }
    public string? WoundSeverity { get; set; }
    public int? WoundOriginatingEventId { get; set; }
    public int WoundRecoveryTicksRemaining { get; set; }
    public List<CompetencySave> Competencies { get; set; } = new();
    public List<WeaponCompetencySave> WeaponCompetencies { get; set; } = new();
    public EquipmentLoadoutSave? EquipmentLoadout { get; set; }
    public CurrentHealthAndConditionSave? CurrentHealthAndCondition { get; set; }
    public List<RoleSave> Roles { get; set; } = new();

    /// <summary>Additive v35: per-citizen personal-equipment state.
    /// Pre-v35 saves deserialize as <c>null</c>; the loader back-fills
    /// an empty registry and migrates the existing
    /// <see cref="EquipmentLoadoutSave.Weapon"/> into a single
    /// <see cref="WeaponItemInstanceSave"/> so the founder keeps the
    /// same weapon profile, just under an item id.</summary>
    public PersonalEquipmentSave? PersonalEquipment { get; set; }

    /// <summary>
    /// Current stamina for this citizen. Old saves (no
    /// <see cref="StaminaMax"/>) deserialize as 0; the restore path
    /// resolves that to a full stamina bar.
    /// </summary>
    public int StaminaCurrent { get; set; }

    /// <summary>
    /// Maximum stamina. <c>null</c> on old saves so the loader can
    /// detect them and use the prototype default.
    /// </summary>
    public int? StaminaMax { get; set; }

    /// <summary>
    /// Remaining ticks of the WellFed stamina-regen buff. Old
    /// saves (no field) deserialize as 0 — the citizen starts
    /// unbuffed, which is the natural state for a citizen that
    /// hasn't eaten since load.
    /// </summary>
    public int WellFedRemainingTicks { get; set; }
    public int? LastVisitedResourceBuildingId { get; set; }
    /// <summary>
    /// Ground-resource identity used instead of
    /// <see cref="LastVisitedResourceBuildingId"/>. Exactly one of the two
    /// identity fields is present for a recorded resource visit.
    /// </summary>
    public int? LastVisitedResourcePatchId { get; set; }
    public int? LastVisitedResourceUnitId { get; set; }
    public int? LastVisitedResourcePositionIndex { get; set; }
}
