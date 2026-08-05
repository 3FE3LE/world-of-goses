using System;
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
    [Obsolete("DEC-0013: retained for tolerant v29 migration only.")]
    public List<string> ProfessionalAffinities { get; set; } = new();
    public string ElementalAffinity { get; set; } = "";
    [Obsolete("DEC-0013: retained for tolerant v29 migration only.")]
    public string CombatStyle { get; set; } = "";
    [Obsolete("DEC-0013: retained for tolerant v29 migration only.")]
    public List<string> WeaponPreferences { get; set; } = new();
    [Obsolete("DEC-0013: retained for tolerant v29 migration only.")]
    public List<string> PersonalityTraits { get; set; } = new();
    [Obsolete("DEC-0013: retained for tolerant v29 migration only.")]
    public string PoliticalOrientation { get; set; } = "";
    [Obsolete("DEC-0013: retained for tolerant v29 migration only.")]
    public string SpiritualPosture { get; set; } = "";
    public FounderCubeProfileSave? CubeProfile { get; set; }
    public FounderNarrativeMemorySave? NarrativeMemory { get; set; }
}
