using System.Collections.Generic;

namespace WorldofGoses.Domain.Persistence;

public sealed class CitizenSave
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int AppearanceSeed { get; set; }
    /// <summary>
    /// The building the citizen is currently assigned to, mirroring
    /// <see cref="Citizen.CurrentAssignment"/>. Named on the DTO
    /// without a domain prefix to keep the wire format compact
    /// and to make the JSON shape match the live citizen field.
    /// </summary>
    public int? CurrentAssignment { get; set; }
    public List<CompetencySave> Competencies { get; set; } = new();
    public List<RoleSave> Roles { get; set; } = new();
}
