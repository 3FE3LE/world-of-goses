using System.Collections.Generic;

namespace WorldofGoses.Domain;

/// <summary>
/// Qualitative lineage metadata. It describes common starting paths and
/// cultural context, never a profession lock, permanent ceiling, or direct
/// production modifier.
/// </summary>
public sealed record LineageDefinition(
    LineageId Id,
    string DisplayName,
    string Summary,
    string LearningApproach,
    IReadOnlyList<ProfessionFamilyId> MarkedAffinities,
    IReadOnlyList<ProfessionFamilyId> ModerateAffinities,
    IReadOnlyList<string> ContextualFrictions);
