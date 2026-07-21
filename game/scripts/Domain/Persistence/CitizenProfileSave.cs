using System.Collections.Generic;

#nullable enable
namespace WorldofGoses.Domain.Persistence;

/// <summary>Serializable form of a citizen's immutable identity profile.</summary>
public sealed class CitizenProfileSave
{
    public string Lineage { get; set; } = "";
    /// <summary>Nullable for forward-compat reads of pre-v4 saves.</summary>
    public string? Gender { get; set; }
    public List<string> Aptitudes { get; set; } = new();
    public List<string> ProfessionalAffinities { get; set; } = new();
    public string ElementalAffinity { get; set; } = "";
    public string CombatStyle { get; set; } = "";
    public List<string> WeaponPreferences { get; set; } = new();
    public List<string> PersonalityTraits { get; set; } = new();
    public string PoliticalOrientation { get; set; } = "";
    public string SpiritualPosture { get; set; } = "";
}
