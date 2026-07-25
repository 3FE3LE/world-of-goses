#nullable enable
using System.Collections.Generic;

namespace WorldofGoses.Domain.Persistence;

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
    public List<CompetencySave> Competencies { get; set; } = new();
    public List<RoleSave> Roles { get; set; } = new();

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
    public int? LastVisitedResourceUnitId { get; set; }
    public int? LastVisitedResourcePositionIndex { get; set; }
}
